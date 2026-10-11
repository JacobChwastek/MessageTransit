import datetime
import importlib.util
from pathlib import Path
import unittest

ROOT = Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location("release_check", ROOT / "scripts/release_check.py")
check = importlib.util.module_from_spec(spec)
spec.loader.exec_module(check)


class WorkflowTests(unittest.TestCase):
    def test_publish_requires_full_ci_and_uses_identity_publisher(self):
        release = (ROOT / '.github/workflows/release.yml').read_text()
        self.assertIn('uses: ./.github/workflows/build.yml', release)
        self.assertIn('      - full-ci\n', release)
        self.assertIn('scripts/release_publish.py publish', release)
        self.assertNotIn('--skip-duplicate', release)
        self.assertIn('      - approval-gate\n', release)
        self.assertLess(release.index('\n  approval-gate:\n'), release.index('\n  publish:\n'))
        self.assertNotIn('environments/nuget-org', release[:release.index('\n  approval-gate:\n')])
        build = (ROOT / '.github/workflows/build.yml').read_text()
        self.assertIn('python3 -B -m unittest discover -s scripts/tests', build)
        self.assertEqual(build.count("      - 'scripts/**'"), 2)
        self.assertEqual(build.count("      - 'release/**'"), 2)

    def test_reusable_checks_take_explicit_candidate_commit(self):
        for name in ('build', 'integration'):
            with self.subTest(name=name):
                workflow = (ROOT / f'.github/workflows/{name}.yml').read_text()
                self.assertIn('  workflow_call:\n', workflow)
                checkouts = workflow.count('uses: actions/checkout@v5')
                self.assertGreater(checkouts, 0)
                self.assertEqual(checkouts, workflow.count('ref: ${{ inputs.ref || github.sha }}'))
                self.assertIn("inputs.ref && github.run_id || 'branch'", workflow)

    def test_recovery_downloads_original_run_artifact_and_preview_is_default(self):
        release = (ROOT / '.github/workflows/release.yml').read_text()
        self.assertIn('default: preview', release)
        self.assertIn('run-id: ${{ inputs.candidate_run }}', release)
        self.assertIn('name: release-candidate\n', release)
        self.assertIn('scripts/release_publish.py verify', release)
        self.assertIn('scripts/release_publish.py prepare', release)


class NuGetAuditTests(unittest.TestCase):
    def test_every_project_fails_restore_on_high_and_critical_advisories(self):
        for name in ('Directory.Build.props', 'scripts/release_symbols/Directory.Build.props'):
            with self.subTest(name=name):
                props = (ROOT / name).read_text()
                self.assertIn('<NuGetAudit>true</NuGetAudit>', props)
                self.assertIn('<NuGetAuditMode>all</NuGetAuditMode>', props)
                self.assertIn('<NuGetAuditLevel>low</NuGetAuditLevel>', props)
                self.assertIn('<WarningsAsErrors>$(WarningsAsErrors);NU1903;NU1904</WarningsAsErrors>', props)
        for name in ('src/Directory.Build.props', 'tests/Directory.Build.props'):
            with self.subTest(name=name):
                self.assertIn('<Import Project="..\\Directory.Build.props"/>', (ROOT / name).read_text())

    def test_projects_follow_the_audit_policy(self):
        failures = []
        check.audit_policy(failures, today=datetime.date.min)
        self.assertEqual([], failures)


if __name__ == '__main__':
    unittest.main()
