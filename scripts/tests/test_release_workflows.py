import datetime
import fnmatch
import importlib.util
import json
from pathlib import Path
import subprocess
import unittest
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location("release_check", ROOT / "scripts/release_check.py")
check = importlib.util.module_from_spec(spec)
spec.loader.exec_module(check)


def workflow_paths(workflow, event):
    """Read the paths filter of one trigger event without a YAML parser."""
    lines = workflow.splitlines()
    paths, in_paths = [], False
    for line in lines[lines.index(f'  {event}:') + 1:]:
        if not line.startswith('    '):
            break
        if not line.startswith('     '):
            in_paths = line.strip() == 'paths:'
        elif in_paths and line.strip().startswith('- '):
            paths.append(line.strip()[2:].strip("'"))
    return paths


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

    def test_build_checks_the_dependency_inventory(self):
        build = (ROOT / '.github/workflows/build.yml').read_text()
        self.assertIn('run: python3 scripts/dependency_inventory.py --check', build)
        self.assertLess(build.index('name: Run release regressions'), build.index('name: Check the dependency inventory'))

    def test_inventory_inputs_trigger_build_checks(self):
        build = (ROOT / '.github/workflows/build.yml').read_text()
        inputs = json.loads((ROOT / 'docs/architecture/dependency-inventory.json').read_text())['input_sha256']
        for event in ('push', 'pull_request'):
            with self.subTest(event=event):
                paths = workflow_paths(build, event)
                self.assertIn('docs/architecture/dependency-inventory.json', paths)
                self.assertEqual([], [name for name in inputs if not any(fnmatch.fnmatch(name, path) for path in paths)])

    def test_reusable_checks_take_explicit_candidate_commit(self):
        for name in ('build', 'integration'):
            with self.subTest(name=name):
                workflow = (ROOT / f'.github/workflows/{name}.yml').read_text()
                self.assertIn('  workflow_call:\n', workflow)
                checkouts = workflow.count('uses: actions/checkout@v5')
                self.assertGreater(checkouts, 0)
                self.assertEqual(checkouts, workflow.count('ref: ${{ inputs.ref || github.sha }}'))
                self.assertIn("inputs.ref && github.run_id || 'branch'", workflow)

    def test_recovery_downloads_original_run_artifact_and_stable_is_default(self):
        release = (ROOT / '.github/workflows/release.yml').read_text()
        self.assertIn('default: stable', release)
        self.assertIn("CHANNEL: ${{ inputs.channel || 'stable' }}", release)
        self.assertIn('run-id: ${{ inputs.candidate_run }}', release)
        self.assertIn('name: release-candidate\n', release)
        self.assertIn('scripts/release_publish.py verify', release)
        self.assertIn('scripts/release_publish.py prepare', release)

    def test_pull_requests_publish_previews_only_to_github_packages(self):
        release = (ROOT / '.github/workflows/release.yml').read_text()
        self.assertIn('  pull_request:\n    branches:\n      - master\n', release)
        self.assertIn("github.event_name == 'pull_request' && github.run_number || ''", release)
        preview = release[release.index('\n  preview:\n'):release.index('\n  publish:\n')]
        self.assertIn('github.event.pull_request.head.repo.full_name == github.repository', preview)
        self.assertIn('https://nuget.pkg.github.com/', preview)
        self.assertIn('packages: write', preview)
        self.assertNotIn('id-token', preview)
        self.assertNotIn('environment:', preview)
        self.assertLess(preview.index('dotnet nuget add source "$FEED" --name github-previews'), preview.index('release_publish.py preview'))
        self.assertIn('--password "$GH_PACKAGES_TOKEN"', preview)
        self.assertIn('--source github-previews', preview)
        self.assertNotIn('dotnet nuget push', preview)
        self.assertNotIn('nuget.org', preview.lower().replace('publish to nuget.org', ''))
        publish = release[release.index('\n  publish:\n'):]
        self.assertIn("github.ref == 'refs/heads/master'", publish)


class NuGetAuditTests(unittest.TestCase):
    def test_every_project_fails_restore_on_high_and_critical_advisories(self):
        props = ET.parse(ROOT / 'NuGetAudit.props').getroot()
        for name, value in (('NuGetAudit', 'true'), ('NuGetAuditMode', 'all'), ('NuGetAuditLevel', 'low'),
                            ('WarningsAsErrors', '$(WarningsAsErrors);NU1903;NU1904')):
            with self.subTest(property=name):
                self.assertEqual(value, props.findtext(f'PropertyGroup/{name}'))
        self.assertEqual({'NuGetAudit.props'}, check.AUDIT_SETTINGS_FILES)
        # the two top-level props import the audit settings; every nested one must import a parent props
        audit_imports = {'Directory.Build.props': 'NuGetAudit.props', 'scripts/release_symbols/Directory.Build.props': '../../NuGetAudit.props'}
        tracked = subprocess.run(['git', 'ls-files', '*Directory.Build.props'], cwd=ROOT, check=True, text=True,
                                 stdout=subprocess.PIPE).stdout.split()
        self.assertLessEqual(set(audit_imports), set(tracked))
        for name in tracked:
            with self.subTest(name=name):
                props = ROOT / name
                imports = [element.get('Project').replace('\\', '/') for element in ET.parse(props).getroot().iter('Import')]
                if name in audit_imports:
                    self.assertIn(audit_imports[name], imports)
                else:
                    parents = [(props.parent / path).resolve() for path in imports]
                    self.assertTrue(any(parent.name == 'Directory.Build.props' and parent.is_file() and parent != props.resolve()
                                        and props.parent.resolve().is_relative_to(parent.parent) for parent in parents), imports)

    def test_projects_follow_the_audit_policy(self):
        failures = []
        check.audit_policy(failures, today=datetime.date.min)
        self.assertEqual([], failures)


if __name__ == '__main__':
    unittest.main()
