import importlib.util
import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch
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
    def test_default_alias_uses_shared_preview_calculator(self):
        with patch.object(sys, "argv", ["release_check.py", "version"]), patch.object(check.subprocess, "run") as run:
            check.main()
        run.assert_called_once_with([sys.executable, str(check.ROOT / "scripts/release_version.py"),
                                     "--channel", "preview", "--bump", "auto"], check=True)

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

    def package(self, dependencies="", asset="lib/net10.0/MessageTransit.dll", contents=None):
        xml = f'''<package><metadata><id>MessageTransit</id><version>1.0.0-dev.1</version>
<authors>Jacob Chwastek</authors><license type="expression">Apache-2.0</license>
<projectUrl>{check.PROJECT_URL}</projectUrl><readme>NuGet.README.md</readme>
<repository url="{check.REPOSITORY_URL}" commit="abc"/>{dependencies}</metadata></package>'''
        with zipfile.ZipFile(self.path, "w") as archive:
            archive.writestr("MessageTransit.nuspec", xml)
            for name in check.REQUIRED_FILES:
                archive.writestr(name, (check.ROOT / name).read_bytes() if contents is None else contents)
            archive.writestr(asset, b"test assembly")
        failures = []
        result = check.inspect_package(self.path, "1.0.0-dev.1", "abc", ["MessageTransit", "MessageTransit.Abstractions"], failures)
        return result, failures

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

    def test_diagnostics_preserve_consumer_build_errors(self):
        args = SimpleNamespace(version="1.0.0-dev.1", commit="abc")
        report = check.render_report(args, [], {}, [], None, ["smoke core: build failed\nNU1301: example feed unavailable"])
        self.assertIn("NU1301", report)


class SourceExpectations(unittest.TestCase):
    def test_unsigned_project_remains_unsigned(self):
        packages = check.source_packages()
        self.assertEqual("", packages["MessageTransit.Interop.NServiceBus"]["token"])
        self.assertEqual(check.FORK_TOKEN, packages["MessageTransit.Abstractions"]["token"])


if __name__ == "__main__":
    unittest.main()
