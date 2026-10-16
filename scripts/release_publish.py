#!/usr/bin/env python3
"""Bind inspected packages to a reserved version and safely resume partial publication."""

import argparse
import base64
import hashlib
import io
import json
import os
from pathlib import Path
import re
import subprocess
import tempfile
import time
import urllib.error
import urllib.request
import zipfile

ROOT = Path(__file__).resolve().parents[1]
VERSION = re.compile(r'^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(?:-preview\.([1-9]\d*))?$')
HEX = re.compile(r'^[0-9a-f]{64}$')
COMMIT = re.compile(r'^[0-9a-f]{40}$')
PACKAGE_ID = re.compile(r'^MessageTransit(?:\.[A-Za-z0-9_]+)*$')
TAG_PREFIX = 'messagetransit-v'


def sha256(data):
    return hashlib.sha256(data).hexdigest()


def payload_hash(data):
    """Hash sorted ZIP names and uncompressed bytes, excluding only NuGet's root signature."""
    digest = hashlib.sha256()
    with zipfile.ZipFile(io.BytesIO(data)) as archive:
        names = archive.namelist()
        if len(names) != len(set(names)):
            raise ValueError('Package has duplicate ZIP entries')
        for name in sorted(names):
            if name == '.signature.p7s':
                continue
            encoded = name.encode('utf-8')
            content = archive.read(name)
            digest.update(len(encoded).to_bytes(8, 'big'))
            digest.update(encoded)
            digest.update(len(content).to_bytes(8, 'big'))
            digest.update(content)
    return digest.hexdigest()


def canonical(manifest):
    return json.dumps(manifest, sort_keys=True, separators=(',', ':'))


def prepare_candidate(version, commit, packages, approved, output, report):
    if not VERSION.fullmatch(version) or not COMMIT.fullmatch(commit):
        raise ValueError('Invalid candidate version or commit')
    if packages.resolve() != output.parent.resolve() / 'packages' or report.resolve() != output.parent.resolve() / 'report.md':
        raise ValueError('Candidate must contain packages/ and report.md beside its manifest')
    files = approved.read_text(encoding='utf-8').splitlines()
    if not files or len(files) != len(set(files)):
        raise ValueError('Approval list is empty or contains duplicates')
    entries = []
    for name in sorted(files):
        suffix = f'.{version}.nupkg'
        package_id = name.removesuffix(suffix)
        if Path(name).name != name or '\\' in name or not name.endswith(suffix) or not PACKAGE_ID.fullmatch(package_id):
            raise ValueError('Invalid approved package path or identity')
        data = (packages / name).read_bytes()
        entries.append({'id': package_id, 'file': name, 'sha256': sha256(data), 'payload_sha256': payload_hash(data)})
    manifest = {'schema': 1, 'version': version, 'commit': commit, 'packages': entries, 'report_sha256': sha256(report.read_bytes())}
    output.write_text(canonical(manifest) + '\n', encoding='utf-8')
    verify_candidate(output)
    return manifest


def verify_candidate(path):
    manifest = json.loads(path.read_text(encoding='utf-8'))
    if not isinstance(manifest, dict) or set(manifest) != {'schema', 'version', 'commit', 'packages', 'report_sha256'}:
        raise ValueError('Invalid candidate manifest fields')
    if type(manifest['schema']) is not int or manifest['schema'] != 1 or not isinstance(manifest['version'], str) or not VERSION.fullmatch(manifest['version']) or not isinstance(manifest['commit'], str) or not COMMIT.fullmatch(manifest['commit']):
        raise ValueError('Invalid candidate version or commit')
    if not isinstance(manifest['report_sha256'], str) or not HEX.fullmatch(manifest['report_sha256']):
        raise ValueError('Invalid report checksum')
    if path.is_symlink() or (path.parent / 'packages').is_symlink() or (path.parent / 'report.md').is_symlink():
        raise ValueError('Candidate artifact paths cannot be symbolic links')
    entries = manifest['packages']
    if not isinstance(entries, list) or not entries:
        raise ValueError('Candidate package set is empty')
    seen = set()
    for entry in entries:
        if not isinstance(entry, dict) or set(entry) != {'id', 'file', 'sha256', 'payload_sha256'} or not all(isinstance(value, str) for value in entry.values()):
            raise ValueError('Invalid package manifest fields')
        expected = f"{entry['id']}.{manifest['version']}.nupkg"
        if not PACKAGE_ID.fullmatch(entry['id']) or entry['file'] != expected or entry['file'] in seen:
            raise ValueError('Invalid or repeated package path/identity')
        seen.add(entry['file'])
        if not HEX.fullmatch(entry['sha256']) or not HEX.fullmatch(entry['payload_sha256']):
            raise ValueError('Invalid package checksum')
        package_path = path.parent / 'packages' / entry['file']
        if package_path.is_symlink():
            raise ValueError('Candidate packages cannot be symbolic links')
        data = package_path.read_bytes()
        if sha256(data) != entry['sha256'] or payload_hash(data) != entry['payload_sha256']:
            raise ValueError(f"Candidate package changed: {entry['file']}")
    if sha256((path.parent / 'report.md').read_bytes()) != manifest['report_sha256']:
        raise ValueError('Candidate inspection report changed')
    return manifest


def validate_recovery_run(run, repository, manifest):
    if run.get('repository', {}).get('full_name') != repository or run.get('path') != '.github/workflows/release.yml' or run.get('head_sha') != manifest['commit']:
        raise ValueError('Recovery must use the original release workflow run in this repository at the candidate commit')


def release_notes(manifest, report, nuget_user, run_url):
    """The inspection report followed by where and by whom the approved packages were published."""
    lines = [report.rstrip(), '', '## Published packages', '']
    account = f' by the NuGet.org account `{nuget_user}`' if nuget_user else ''
    run = f' from [this workflow run]({run_url})' if run_url else ''
    lines += [f'Published{account}{run}.', '']
    lines += ['| Package | NuGet.org |', '| --- | --- |']
    lines += [f"| {entry['id']} | https://www.nuget.org/packages/{entry['id']}/{manifest['version']} |" for entry in manifest['packages']]
    return '\n'.join(lines) + '\n'


def publish_candidate(path, backend):
    manifest = verify_candidate(path)
    backend.reserve(manifest)
    for entry in manifest['packages']:
        existing = backend.fetch_package(entry, manifest['version'])
        # the feed reports a conflict for a package that is uploaded but still being validated and indexed
        if existing is None and not backend.push_package(path.parent / 'packages' / entry['file']):
            existing = backend.wait_for_package(entry, manifest['version'])
        if existing is None:
            continue
        if payload_hash(existing) != entry['payload_sha256']:
            raise ValueError(f"Published payload conflicts with approved candidate: {entry['file']}")
        print(f"Verified existing package: {entry['file']}")
    backend.finalize(manifest, path)


class Backend:
    source = 'https://api.nuget.org/v3/index.json'
    feed_name = 'NuGet.org'

    def __init__(self, repository, api_key, nuget_user=None, run_url=None, wait_seconds=1200, poll_seconds=30):
        if not re.fullmatch(r'[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+', repository):
            raise ValueError('Invalid GitHub repository')
        self.repository = repository
        self.api_key = api_key
        self.nuget_user = nuget_user
        self.run_url = run_url
        self.wait_seconds = wait_seconds
        self.poll_seconds = poll_seconds

    def run(self, *arguments, check=True):
        return subprocess.run(arguments, cwd=ROOT, text=True, capture_output=True, check=check)

    def reserve(self, manifest):
        tag = TAG_PREFIX + manifest['version']
        if self.run('git', 'rev-parse', 'HEAD').stdout.strip() != manifest['commit']:
            raise ValueError('Publisher checkout must match the candidate commit')
        remote = self.run('git', 'ls-remote', '--tags', 'origin', f'refs/tags/{tag}').stdout.strip()
        if remote:
            self.run('git', 'fetch', '--no-tags', 'origin', f'refs/tags/{tag}:refs/tags/{tag}')
            recorded = self.run('git', 'for-each-ref', '--format=%(contents)', f'refs/tags/{tag}').stdout.strip()
            target = self.run('git', 'rev-parse', f'{tag}^{{commit}}').stdout.strip()
            if target != manifest['commit'] or recorded != canonical(manifest):
                raise ValueError(f'{tag} is reserved for a different candidate; recover its original artifact or choose a new version')
            print(f'Resuming reserved candidate {tag}')
            return
        self.run('git', 'fetch', 'origin', 'master')
        latest = self.run('git', 'rev-parse', 'FETCH_HEAD').stdout.strip()
        if latest != manifest['commit']:
            raise ValueError('New reservations require the latest master commit; generate and approve a fresh candidate')
        with tempfile.NamedTemporaryFile(mode='w', encoding='utf-8') as message:
            message.write(canonical(manifest) + '\n')
            message.flush()
            self.run('git', '-c', 'user.name=MessageTransit Release', '-c', 'user.email=release@users.noreply.github.com', 'tag', '--no-sign', '-a', tag, manifest['commit'], '--file', message.name)
        self.run('git', 'push', 'origin', f'refs/tags/{tag}')
        print(f'Reserved {tag} before package publication')

    def fetch_package(self, entry, version):
        package_id = entry['id'].lower()
        version = version.lower()
        url = f'https://api.nuget.org/v3-flatcontainer/{package_id}/{version}/{package_id}.{version}.nupkg'
        try:
            with urllib.request.urlopen(url, timeout=60) as response:
                return response.read()
        except urllib.error.HTTPError as error:
            if error.code == 404:
                return None
            raise

    def push_package(self, path):
        """Return False when the feed already has this version; the caller must then verify the remote payload."""
        result = self.run('dotnet', 'nuget', 'push', str(path), '--api-key', self.api_key, '--source', self.source, check=False)
        if result.returncode:
            # Avoid exposing the command line or API key through CalledProcessError.
            if re.search(r'\b409\b', f'{result.stdout or ""}{result.stderr or ""}'):
                print(f'Package already exists on the feed: {path.name}')
                return False
            raise RuntimeError(f'Package upload failed for {path.name}; resume the original candidate artifact after checking {self.feed_name} status')
        print(f'Submitted package: {path.name}')
        return True

    def wait_for_package(self, entry, version):
        deadline = time.monotonic() + self.wait_seconds
        while True:
            existing = self.fetch_package(entry, version)
            if existing is not None:
                return existing
            if time.monotonic() >= deadline:
                raise RuntimeError(f"{entry['file']} exists on {self.feed_name} but is not downloadable yet; resume the original candidate artifact later")
            time.sleep(self.poll_seconds)

    def finalize(self, manifest, manifest_path):
        tag = TAG_PREFIX + manifest['version']
        assets = [str(manifest_path), str(manifest_path.parent / 'report.md')]
        assets += [str(manifest_path.parent / 'packages' / entry['file']) for entry in manifest['packages']]
        report = (manifest_path.parent / 'report.md').read_text(encoding='utf-8')
        with tempfile.NamedTemporaryFile(mode='w', encoding='utf-8', suffix='.md') as notes:
            notes.write(release_notes(manifest, report, self.nuget_user, self.run_url))
            notes.flush()
            view = self.run('gh', 'release', 'view', tag, '--repo', self.repository, '--json', 'isDraft', check=False)
            if view.returncode:
                arguments = ['gh', 'release', 'create', tag, '--repo', self.repository, '--verify-tag', '--draft', '--title', f"MessageTransit {manifest['version']}", '--notes-file', notes.name]
                if '-preview.' in manifest['version']:
                    arguments.append('--prerelease')
                self.run(*arguments)
            self.run('gh', 'release', 'upload', tag, *assets, '--repo', self.repository, '--clobber')
            self.run('gh', 'release', 'edit', tag, '--repo', self.repository, '--notes-file', notes.name, '--draft=false', f"--prerelease={str('-preview.' in manifest['version']).lower()}")
        print(f'Recorded release {tag}')


class PreviewBackend(Backend):
    """Publish pull request previews to the repository owner's GitHub Packages feed; previews reserve no tag and record no release."""
    feed_name = 'GitHub Packages'

    def __init__(self, repository, api_key, user, source, wait_seconds=600, poll_seconds=15):
        super().__init__(repository, api_key, wait_seconds=wait_seconds, poll_seconds=poll_seconds)
        if not user:
            raise ValueError('GitHub Packages requires a user name for the feed credentials')
        self.user = user
        # the push uses this configured source name, which carries the feed credentials
        self.source = source
        self.index_url = f"https://nuget.pkg.github.com/{repository.split('/')[0]}/index.json"
        self.base_address = None

    def get(self, url):
        token = base64.b64encode(f'{self.user}:{self.api_key}'.encode()).decode()
        request = urllib.request.Request(url, headers={'Authorization': f'Basic {token}'})
        with urllib.request.urlopen(request, timeout=60) as response:
            return response.read()

    def reserve(self, manifest):
        if '-preview.' not in manifest['version']:
            raise ValueError('Only preview versions are published to GitHub Packages')

    def fetch_package(self, entry, version):
        if self.base_address is None:
            resources = json.loads(self.get(self.index_url)).get('resources', [])
            address = next((resource.get('@id') for resource in resources if str(resource.get('@type', '')).startswith('PackageBaseAddress/3.0.0')), None)
            if not address:
                raise ValueError('The GitHub Packages service index has no package download address')
            self.base_address = address.rstrip('/') + '/'
        package_id = entry['id'].lower()
        version = version.lower()
        try:
            return self.get(f'{self.base_address}{package_id}/{version}/{package_id}.{version}.nupkg')
        except urllib.error.HTTPError as error:
            if error.code == 404:
                return None
            raise

    def finalize(self, manifest, manifest_path):
        print(f"Published preview {manifest['version']} to {self.feed_name}")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    commands = parser.add_subparsers(dest='command', required=True)
    prepare = commands.add_parser('prepare')
    prepare.add_argument('--version', required=True)
    prepare.add_argument('--commit', required=True)
    prepare.add_argument('--packages', required=True, type=Path)
    prepare.add_argument('--approved', required=True, type=Path)
    prepare.add_argument('--output', required=True, type=Path)
    prepare.add_argument('--report', required=True, type=Path)
    verify = commands.add_parser('verify')
    verify.add_argument('--manifest', required=True, type=Path)
    verify.add_argument('--github-output', action='store_true')
    verify.add_argument('--recovery-run', help='Original Actions release run ID')
    verify.add_argument('--repository', help='Expected repository for recovery provenance')
    publish = commands.add_parser('publish')
    publish.add_argument('--manifest', required=True, type=Path)
    publish.add_argument('--repository', required=True)
    publish.add_argument('--api-key-env', default='NUGET_API_KEY')
    publish.add_argument('--nuget-user', help='NuGet.org account recorded as the publisher')
    preview = commands.add_parser('preview', help='Publish a pull request preview to GitHub Packages, skipping packages already uploaded from this candidate')
    preview.add_argument('--manifest', required=True, type=Path)
    preview.add_argument('--repository', required=True)
    preview.add_argument('--source', required=True, help='Configured NuGet source name that holds the feed credentials')
    preview.add_argument('--user', required=True, help='User name for the feed credentials')
    preview.add_argument('--api-key-env', default='GH_PACKAGES_TOKEN')
    args = parser.parse_args()
    if args.command == 'prepare':
        manifest = prepare_candidate(args.version, args.commit, args.packages, args.approved, args.output, args.report)
    elif args.command == 'verify':
        manifest = verify_candidate(args.manifest)
        if args.recovery_run:
            if not re.fullmatch(r'[1-9]\d{0,19}', args.recovery_run) or not args.repository or not re.fullmatch(r'[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+', args.repository):
                raise ValueError('Recovery requires a numeric run ID and repository')
            run = subprocess.check_output(['gh', 'api', f'repos/{args.repository}/actions/runs/{args.recovery_run}'], text=True)
            validate_recovery_run(json.loads(run), args.repository, manifest)
        if args.github_output:
            with open(os.environ['GITHUB_OUTPUT'], 'a', encoding='utf-8') as output:
                output.write(f"version={manifest['version']}\ncommit={manifest['commit']}\nrelease=true\n")
    elif args.command == 'preview':
        key = os.environ.get(args.api_key_env)
        if not key:
            raise ValueError('GitHub Packages token is missing')
        publish_candidate(args.manifest, PreviewBackend(args.repository, key, args.user, args.source))
        return
    else:
        key = os.environ.get(args.api_key_env)
        if not key:
            raise ValueError('NuGet API key is missing')
        server, run_id = os.environ.get('GITHUB_SERVER_URL'), os.environ.get('GITHUB_RUN_ID')
        run_url = f'{server}/{args.repository}/actions/runs/{run_id}' if server and run_id else None
        publish_candidate(args.manifest, Backend(args.repository, key, args.nuget_user, run_url))
        return
    print(f"Verified candidate {manifest['version']} at {manifest['commit']}")


if __name__ == '__main__':
    try:
        main()
    except (ValueError, OSError, zipfile.BadZipFile, subprocess.CalledProcessError) as error:
        raise SystemExit(str(error)) from error
