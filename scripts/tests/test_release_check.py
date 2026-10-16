import importlib.util
import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch
import xml.etree.ElementTree as ET
import zipfile
from types import SimpleNamespace
import contextlib
import io
import sys


SCRIPT = Path(__file__).resolve().parents[1] / "release_check.py"
spec = importlib.util.spec_from_file_location("release_check", SCRIPT)
check = importlib.util.module_from_spec(spec)
spec.loader.exec_module(check)


class VersionAliasChecks(unittest.TestCase):
    def test_default_alias_uses_shared_stable_calculator(self):
        with patch.object(sys, "argv", ["release_check.py", "version"]), patch.object(check.subprocess, "run") as run:
            check.main()
        run.assert_called_once_with([sys.executable, str(check.ROOT / "scripts/release_version.py"),
                                     "--channel", "stable", "--bump", "auto"], check=True)

    def test_alias_forwards_explicit_channel_version_and_bump(self):
        with patch.object(sys, "argv", ["release_check.py", "version", "--channel", "stable", "--bump", "minor",
                                        "--version", "2.0.0"]), patch.object(check.subprocess, "run") as run:
            check.main()
        run.assert_called_once_with([sys.executable, str(check.ROOT / "scripts/release_version.py"),
                                     "--channel", "stable", "--bump", "minor", "--version", "2.0.0"], check=True)


class PackageChecks(unittest.TestCase):
    def setUp(self):
        self.directory = tempfile.TemporaryDirectory()
        self.addCleanup(self.directory.cleanup)
        self.path = Path(self.directory.name) / "MessageTransit.1.0.0-dev.1.nupkg"

    def package(self, dependencies="", asset="lib/net10.0/MessageTransit.dll", contents=None,
                repository=f'url="{check.PROJECT_URL}" commit="abc"'):
        xml = f'''<package><metadata><id>MessageTransit</id><version>1.0.0-dev.1</version>
<authors>Jacob Chwastek</authors><license type="expression">Apache-2.0</license>
<projectUrl>{check.PROJECT_URL}</projectUrl><readme>NuGet.README.md</readme>
<repository type="git" {repository}/>{dependencies}</metadata></package>'''
        with zipfile.ZipFile(self.path, "w") as archive:
            archive.writestr("MessageTransit.nuspec", xml)
            for name in check.REQUIRED_FILES:
                archive.writestr(name, (check.ROOT / name).read_bytes() if contents is None else contents)
            archive.writestr(asset, b"test assembly")
        failures = []
        result = check.inspect_package(self.path, "1.0.0-dev.1", "abc", ["MessageTransit", "MessageTransit.Abstractions"], failures)
        return result, failures

    def test_repository_url_with_or_without_git_suffix_passes(self):
        for url in (check.PROJECT_URL, check.PROJECT_URL + ".git"):
            with self.subTest(url=url):
                _, failures = self.package(repository=f'url="{url}" commit="abc"')
                self.assertFalse(any("repository is" in failure for failure in failures), failures)

    def test_other_repository_or_commit_is_rejected(self):
        for repository in ('url="https://github.com/MassTransit/MassTransit.git" commit="abc"',
                           'url="https://github.com/JacobChwastek/MessageTransit.git/" commit="abc"',
                           'url="https://github.com/JacobChwastek/MessageTransit-Other" commit="abc"',
                           f'url="{check.PROJECT_URL}" commit="def"',
                           f'url="{check.PROJECT_URL}"'):
            with self.subTest(repository=repository):
                _, failures = self.package(repository=repository)
                self.assertTrue(any("repository is" in failure for failure in failures), failures)

    def test_missing_transitive_dependency_is_rejected(self):
        _, failures = self.package()
        self.assertTrue(any("missing dependency MessageTransit.Abstractions" in failure for failure in failures), failures)

    def test_placeholder_is_not_an_assembly(self):
        _, failures = self.package(asset="lib/net10.0/_._")
        self.assertTrue(any("missing assembly" in failure for failure in failures), failures)

    def test_empty_notice_is_rejected(self):
        _, failures = self.package(contents=b"")
        self.assertTrue(any("NOTICE" in failure for failure in failures), failures)

    def test_flat_lowercase_dependency_cannot_bypass_held_check(self):
        _, failures = self.package('<dependencies><dependency id="messagetransit.Held" version="1.0.0-dev.1"/></dependencies>')
        self.assertTrue(any("held package" in failure for failure in failures), failures)

    def test_inspection_without_source_link_cannot_approve(self):
        self.package()
        approved = Path(self.directory.name) / "approved.txt"
        args = SimpleNamespace(packages=self.directory.name, version="1.0.0-dev.1", commit="abc",
                               skip_smoke=True, report=None, approved=str(approved))
        with patch.object(check, "load_manifest", return_value=(["MessageTransit"], {})), contextlib.redirect_stdout(io.StringIO()):
            with self.assertRaises(SystemExit):
                check.inspect(args)
        self.assertFalse(approved.exists())


class ConsumerChecks(unittest.TestCase):
    def test_unexpected_inspection_error_removes_stale_approval(self):
        with tempfile.TemporaryDirectory() as directory:
            approved = Path(directory) / "approved.txt"
            approved.write_text("previous-candidate.nupkg\n")
            args = SimpleNamespace(approved=str(approved))
            with patch.object(check, "load_manifest", side_effect=ValueError("malformed manifest")):
                with self.assertRaisesRegex(ValueError, "malformed manifest"):
                    check.inspect(args)
            self.assertFalse(approved.exists())

    def test_skipped_smoke_cannot_write_approval_manifest(self):
        with tempfile.TemporaryDirectory() as directory:
            package = Path(directory) / "MessageTransit.1.0.0-dev.1.nupkg"
            package.write_bytes(b"fixture")
            args = SimpleNamespace(packages=directory, version="1.0.0-dev.1", commit="abc", skip_smoke=True,
                                   report=None, approved=str(Path(directory) / "approved.txt"))
            valid = {"id": "MessageTransit", "license": "Apache-2.0", "frameworks": ["net10.0"],
                     "dependencies": [], "external_dependencies": [], "asset": "lib/net10.0/MessageTransit.dll"}
            with patch.object(check, "load_manifest", return_value=(["MessageTransit"], {})), \
                    patch.object(check, "inspect_package", return_value=valid), \
                    patch.object(check, "inspect_symbols", return_value=[]), contextlib.redirect_stdout(io.StringIO()):
                with self.assertRaises(SystemExit):
                    check.inspect(args)
            self.assertFalse(Path(args.approved).exists())

    def test_complete_known_scenarios_pass(self):
        recipes = check.consumer_recipes(check.load_manifest()[0])
        responses = []
        for name, packages, external, sources, constants, expected in recipes:
            responses += [(0, "AnalyzerProbe.cs(11,9): warning MTA0001: missing await"),
                          (0, "\n".join("pass\t" + assertion for assertion in sorted(expected)))]
        failures = []
        with patch.object(check, "run_captured", side_effect=responses):
            smoke = check.smoke_test(Path("/tmp"), "1.0.0-dev.1", check.load_manifest()[0], failures)
        self.assertEqual([], failures)
        self.assertTrue(smoke["built"])

    def test_empty_scenario_is_rejected(self):
        responses = iter([(0, "AnalyzerProbe.cs(11,9): warning MTA0001: missing await"), (0, ""),
                          (0, f"MessageTransit\t{check.FORK_TOKEN}\n")])
        failures = []
        with patch.object(check, "run_captured", side_effect=lambda *args: next(responses, (1, "stop"))):
            check.smoke_test(Path("/tmp"), "1.0.0-dev.1", check.load_manifest()[0], failures)
        self.assertTrue(any("scenario" in failure for failure in failures), failures)

    def test_core_consumer_has_only_top_level_core_reference(self):
        references = []
        def run(arguments, cwd):
            if arguments[1] == "build":
                references.append((Path(cwd) / "ReleaseSmoke.csproj").read_text())
                return 1, "stop after inspecting generated references"
            return 1, "stop"
        with patch.object(check, "run_captured", side_effect=run):
            check.smoke_test(Path("/tmp"), "1.0.0-dev.1", check.load_manifest()[0], [])
        self.assertTrue(any('Include="MessageTransit"' in project and 'Include="MessageTransit.Abstractions"' not in project
                            and 'Include="MessageTransit.Newtonsoft"' not in project for project in references), references)

    def test_consumers_audit_the_candidate_graph(self):
        generated = []
        def run(arguments, cwd):
            generated.append(((Path(cwd) / "Directory.Build.props").read_text(), (Path(cwd) / "nuget.config").read_text()))
            return 1, "stop after inspecting generated settings"
        with patch.object(check, "run_captured", side_effect=run):
            check.smoke_test(Path("/tmp"), "1.0.0-dev.1", check.load_manifest()[0], [])
        self.assertEqual(len(check.consumer_recipes(check.load_manifest()[0])), len(generated))
        for props, config in generated:
            self.assertIn("<NuGetAuditMode>all</NuGetAuditMode>", props)
            self.assertIn("<WarningsAsErrors>NU1900;NU1903;NU1904;NU1905</WarningsAsErrors>", props)
            sources = ET.fromstring(config).find("auditSources")
            self.assertIsNotNone(sources)
            self.assertEqual(["clear", "add"], [child.tag for child in sources])
            self.assertEqual("https://api.nuget.org/v3/index.json", sources.find("add").get("value"))

    def test_audit_errors_are_reported_without_build_noise(self):
        output = ("restoring\n/tmp/c/ReleaseSmoke.csproj : error NU1904: Warning As Error: Package 'Example' 1.0.0 has a known critical severity vulnerability\n"
                  "/tmp/c/ReleaseSmoke.csproj : warning NU1902: Package 'Other' 1.0.0 has a known moderate severity vulnerability\n")
        failures = []
        with patch.object(check, "run_captured", return_value=(1, output)):
            check.smoke_test(Path("/tmp"), "1.0.0-dev.1", check.load_manifest()[0], failures)
        self.assertTrue(failures[0].startswith("smoke co-install: NuGet audit failed\n"), failures[0])
        self.assertIn("error NU1904", failures[0])
        self.assertNotIn("NU1902", failures[0])

    def test_restore_errors_are_kept_when_audit_data_is_unavailable(self):
        output = ("/tmp/c/ReleaseSmoke.csproj : error NU1301: Unable to load the service index for source https://api.nuget.org/v3/index.json.\n"
                  "/tmp/c/ReleaseSmoke.csproj : error NU1900: Warning As Error: Error occurred while getting package vulnerability data\n")
        failures = []
        with patch.object(check, "run_captured", return_value=(1, output)):
            check.smoke_test(Path("/tmp"), "1.0.0-dev.1", check.load_manifest()[0], failures)
        self.assertTrue(failures[0].startswith("smoke co-install: build failed\n"), failures[0])
        self.assertIn("error NU1301", failures[0])

    def test_consumers_accept_the_repository_accepted_advisories(self):
        generated = []
        def run(arguments, cwd):
            generated.append((Path(cwd) / "Directory.Build.props").read_text())
            return 1, "stop after inspecting generated settings"
        with patch.object(check, "run_captured", side_effect=run):
            check.smoke_test(Path("/tmp"), "1.0.0-dev.1", check.load_manifest()[0], [], ["https://github.com/advisories/GHSA-0000"])
        for props in generated:
            item = ET.fromstring(props).find("ItemGroup/NuGetAuditSuppress")
            self.assertEqual("https://github.com/advisories/GHSA-0000", item.get("Include"))

    def test_diagnostics_preserve_consumer_build_errors(self):
        args = SimpleNamespace(version="1.0.0-dev.1", commit="abc")
        report = check.render_report(args, [], {}, [], None, ["smoke core: build failed\nNU1301: example feed unavailable"])
        self.assertIn("NU1301", report)


class AuditPolicyChecks(unittest.TestCase):
    def suppressions(self, items, today="2026-11-30"):
        return self.policy(f"<Project><ItemGroup>{items}</ItemGroup></Project>", today)

    def policy(self, project, today="2026-11-30"):
        with tempfile.TemporaryDirectory() as directory:
            (Path(directory) / "Example.props").write_text(project, encoding="utf-8")
            failures = []
            with patch.object(check, "ROOT", Path(directory)), \
                    patch.object(check.subprocess, "run", return_value=SimpleNamespace(stdout="Example.props\n")):
                suppressions = check.audit_policy(failures, check.datetime.date.fromisoformat(today))
            return suppressions, failures

    def test_unreadable_project_file_is_a_failure(self):
        _, failures = self.policy("<Project><PropertyGroup>")
        self.assertTrue(any("cannot check the NuGet audit policy" in failure for failure in failures), failures)

    def test_missing_project_file_is_a_failure(self):
        failures = []
        with tempfile.TemporaryDirectory() as directory, patch.object(check, "ROOT", Path(directory)), \
                patch.object(check.subprocess, "run", return_value=SimpleNamespace(stdout="missing.props\n")):
            self.assertEqual([], check.audit_policy(failures))
        self.assertTrue(any("missing.props: cannot check the NuGet audit policy" in failure for failure in failures), failures)

    def test_unavailable_git_is_a_failure(self):
        for error in (OSError("git not found"), check.subprocess.CalledProcessError(128, "git")):
            with self.subTest(error=type(error).__name__):
                failures = []
                with patch.object(check.subprocess, "run", side_effect=error):
                    self.assertEqual([], check.audit_policy(failures))
                self.assertTrue(any("cannot list project files" in failure for failure in failures), failures)

    def test_accepted_advisory_with_reason_and_review_date_passes(self):
        suppressions, failures = self.suppressions(
            '<NuGetAuditSuppress Include="https://github.com/advisories/GHSA-0000" Reason="Test-only" ReviewBy="2026-12-01" />', "2026-11-30")
        self.assertEqual([], failures)
        self.assertEqual("Test-only", suppressions[0]["reason"])
        self.assertTrue(suppressions[0]["accepted"])

    def test_metadata_elements_are_read(self):
        _, failures = self.suppressions('<NuGetAuditSuppress Include="https://github.com/advisories/GHSA-0000">'
                                        '<Reason>Test-only</Reason><ReviewBy>2026-12-01</ReviewBy></NuGetAuditSuppress>', "2026-11-30")
        self.assertEqual([], failures)

    def test_missing_reason_or_review_date_fails(self):
        _, failures = self.suppressions('<NuGetAuditSuppress Include="https://github.com/advisories/GHSA-0000" />', "2026-11-30")
        self.assertTrue(any("has no Reason" in failure for failure in failures), failures)
        self.assertTrue(any("needs ReviewBy" in failure for failure in failures), failures)

    def test_expired_review_date_fails(self):
        suppressions, failures = self.suppressions(
            '<NuGetAuditSuppress Include="https://github.com/advisories/GHSA-0000" Reason="Test-only" ReviewBy="2026-12-01" />', "2026-12-02")
        self.assertTrue(any("passed its review date 2026-12-01" in failure for failure in failures), failures)
        self.assertFalse(suppressions[0]["accepted"])

    def test_removing_a_suppression_is_not_an_accepted_advisory(self):
        suppressions, failures = self.suppressions('<NuGetAuditSuppress Remove="https://github.com/advisories/GHSA-0000" />')
        self.assertEqual(([], []), (suppressions, failures))

    def test_attribute_metadata_is_trimmed(self):
        _, failures = self.suppressions(
            '<NuGetAuditSuppress Include="https://github.com/advisories/GHSA-0000" Reason="Test-only" ReviewBy=" 2026-12-01 " />')
        self.assertEqual([], failures)

    def test_hidden_audit_warnings_fail(self):
        for project in ('<Project><PropertyGroup><NoWarn>$(NoWarn);NU1903</NoWarn></PropertyGroup></Project>',
                        '<Project><PropertyGroup><WarningsNotAsErrors>NU1904</WarningsNotAsErrors></PropertyGroup></Project>',
                        '<Project><ItemGroup><PackageReference Include="Example" NoWarn="NU1903" /></ItemGroup></Project>',
                        '<Project><ItemGroup><PackageReference Include="Example"><NoWarn>NU1904</NoWarn></PackageReference></ItemGroup></Project>'):
            with self.subTest(project=project):
                _, failures = self.policy(project)
                self.assertTrue(any("hides NuGet audit warnings" in failure for failure in failures), failures)

    def test_weakened_audit_settings_fail(self):
        for project, expected in (('<Project><PropertyGroup><NuGetAudit>false</NuGetAudit></PropertyGroup></Project>', "sets NuGetAudit"),
                                  ('<Project><PropertyGroup><NuGetAuditLevel>critical</NuGetAuditLevel></PropertyGroup></Project>', "sets NuGetAuditLevel"),
                                  ('<Project><PropertyGroup><WarningsAsErrors>CS0168</WarningsAsErrors></PropertyGroup></Project>', "replaces WarningsAsErrors")):
            with self.subTest(project=project):
                _, failures = self.policy(project)
                self.assertTrue(any(expected in failure for failure in failures), failures)

    def test_extending_warnings_as_errors_passes(self):
        _, failures = self.policy('<Project><PropertyGroup><WarningsAsErrors>$(WarningsAsErrors);CS0168</WarningsAsErrors></PropertyGroup></Project>')
        self.assertEqual([], failures)


class SourceExpectations(unittest.TestCase):
    def test_unsigned_project_remains_unsigned(self):
        packages = check.source_packages()
        self.assertEqual("", packages["MessageTransit.Interop.NServiceBus"]["token"])
        self.assertEqual(check.FORK_TOKEN, packages["MessageTransit.Abstractions"]["token"])


if __name__ == "__main__":
    unittest.main()
