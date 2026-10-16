import importlib.util
import io
import json
from pathlib import Path
import tempfile
import unittest
import zipfile
from types import SimpleNamespace

SCRIPT = Path(__file__).resolve().parents[1] / 'release_publish.py'
COMMIT_A = 'a' * 40
COMMIT_B = 'b' * 40
VERSION = '1.0.0-preview.1'


def load():
    if not SCRIPT.exists():
        raise AssertionError('immutable candidate publisher is not implemented')
    spec = importlib.util.spec_from_file_location('release_publish', SCRIPT)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def package(payload, signature=None):
    output = io.BytesIO()
    with zipfile.ZipFile(output, 'w') as archive:
        archive.writestr('lib/net10.0/MessageTransit.dll', payload)
        if signature is not None:
            archive.writestr('.signature.p7s', signature)
    return output.getvalue()


class FakeBackend:
    def __init__(self):
        self.reservation = None
        self.feed = {}
        self.fail_on = None
        self.indexing = {}
        self.events = []

    def reserve(self, manifest):
        self.events.append('reserve')
        if self.reservation is not None and self.reservation != manifest:
            raise ValueError('version is reserved for a different candidate')
        self.reservation = manifest

    def fetch_package(self, entry, version):
        return self.feed.get(entry['file'])

    def push_package(self, path):
        self.events.append(path.name)
        if path.name == self.fail_on:
            raise RuntimeError('interrupted push')
        if path.name in self.indexing:
            return False
        self.feed[path.name] = path.read_bytes()
        return True

    def wait_for_package(self, entry, version):
        self.events.append('wait')
        return self.indexing[entry['file']]

    def finalize(self, manifest, manifest_path):
        self.events.append('complete')


class PublicationTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        (self.root / 'packages').mkdir()
        self.names = [f'{name}.{VERSION}.nupkg' for name in ('MessageTransit', 'MessageTransit.Abstractions')]
        for name in self.names:
            (self.root / 'packages' / name).write_bytes(package(b'commit A'))
        (self.root / 'approved.txt').write_text('\n'.join(self.names) + '\n')
        (self.root / 'report.md').write_text('Inspection passed')

    def prepare(self, commit=COMMIT_A):
        mod = load()
        path = self.root / 'candidate.json'
        mod.prepare_candidate(VERSION, commit, self.root / 'packages', self.root / 'approved.txt', path, self.root / 'report.md')
        return mod, path

    def test_partial_retry_uses_same_artifact_and_reserves_before_push(self):
        mod, path = self.prepare()
        backend = FakeBackend()
        backend.fail_on = self.names[1]
        with self.assertRaises(RuntimeError):
            mod.publish_candidate(path, backend)
        self.assertEqual(backend.events[0], 'reserve')
        backend.fail_on = None
        mod.publish_candidate(path, backend)
        self.assertEqual(backend.events.count(self.names[0]), 1)
        self.assertEqual(backend.events[-1], 'complete')

    def test_new_commit_cannot_complete_a_partial_reserved_version(self):
        mod, path = self.prepare()
        backend = FakeBackend()
        backend.fail_on = self.names[1]
        with self.assertRaises(RuntimeError):
            mod.publish_candidate(path, backend)
        for name in self.names:
            (self.root / 'packages' / name).write_bytes(package(b'commit B'))
        self.prepare(COMMIT_B)
        backend.fail_on = None
        with self.assertRaisesRegex(ValueError, 'different candidate'):
            mod.publish_candidate(path, backend)
        self.assertNotIn(self.names[1], backend.feed)

    def test_repository_signature_is_ignored_but_payload_conflict_is_rejected(self):
        mod, path = self.prepare()
        backend = FakeBackend()
        backend.feed[self.names[0]] = package(b'commit A', b'NuGet signature')
        mod.publish_candidate(path, backend)
        self.assertNotIn(self.names[0], backend.events)
        backend.feed[self.names[0]] = package(b'commit B', b'NuGet signature')
        with self.assertRaisesRegex(ValueError, 'payload'):
            mod.publish_candidate(path, backend)

    def test_conflict_for_an_unindexed_upload_waits_and_verifies_the_payload(self):
        mod, path = self.prepare()
        backend = FakeBackend()
        backend.indexing[self.names[0]] = package(b'commit A', b'NuGet signature')
        mod.publish_candidate(path, backend)
        self.assertEqual(backend.events[1:3], [self.names[0], 'wait'])
        backend = FakeBackend()
        backend.indexing[self.names[0]] = package(b'commit B')
        with self.assertRaisesRegex(ValueError, 'payload'):
            mod.publish_candidate(path, backend)

    def test_release_notes_record_package_urls_account_and_run(self):
        mod, path = self.prepare()
        notes = mod.release_notes(mod.verify_candidate(path), 'Inspection passed', 'messagetransit', 'https://github.com/o/r/actions/runs/1')
        self.assertTrue(notes.startswith('Inspection passed\n'))
        self.assertIn('account `messagetransit` from [this workflow run](https://github.com/o/r/actions/runs/1)', notes)
        self.assertIn(f'https://www.nuget.org/packages/MessageTransit.Abstractions/{VERSION}', notes)

    def test_local_artifact_tampering_fails_before_reservation(self):
        mod, path = self.prepare()
        (self.root / 'packages' / self.names[0]).write_bytes(package(b'tampered'))
        backend = FakeBackend()
        with self.assertRaises(ValueError):
            mod.publish_candidate(path, backend)
        self.assertEqual(backend.events, [])

    def test_manifest_validation_rejects_traversal_invalid_identity_and_empty_package_set(self):
        mod, path = self.prepare()
        good = json.loads(path.read_text())
        for field, value in [('commit', 'master'), ('version', '1.0.0;bad'), ('packages', []), ('packages', [{**good['packages'][0], 'file': '../escape.nupkg'}])]:
            with self.subTest(field=field):
                path.write_text(json.dumps({**good, field: value}))
                with self.assertRaises(ValueError):
                    mod.verify_candidate(path)

    def test_report_tampering_fails(self):
        mod, path = self.prepare()
        (self.root / 'report.md').write_text('other report')
        with self.assertRaises(ValueError):
            mod.verify_candidate(path)


class BackendTests(unittest.TestCase):
    setUp = PublicationTests.setUp
    prepare = PublicationTests.prepare

    def backend(self, manifest, remote=False, target=None, recorded=None, latest=None, checkout=None):
        mod = load()
        backend = mod.Backend('JacobChwastek/MessageTransit', 'secret-not-for-output')
        calls = []
        def run(*args, check=True):
            calls.append(args)
            if args[:3] == ('git', 'ls-remote', '--tags'):
                return SimpleNamespace(stdout='tag' if remote else '', returncode=0)
            if args[:3] == ('git', 'rev-parse', 'HEAD'):
                return SimpleNamespace(stdout=checkout or manifest['commit'], returncode=0)
            if args[:2] == ('git', 'for-each-ref'):
                return SimpleNamespace(stdout=recorded or mod.canonical(manifest), returncode=0)
            if args[:2] == ('git', 'rev-parse'):
                return SimpleNamespace(stdout=(latest or manifest['commit']) if args[2] == 'FETCH_HEAD' else (target or manifest['commit']), returncode=0)
            return SimpleNamespace(stdout='', returncode=0)
        backend.run = run
        return backend, calls

    def test_remote_reservation_is_checked_against_full_manifest(self):
        mod, path = self.prepare()
        manifest = mod.verify_candidate(path)
        backend, calls = self.backend(manifest, remote=True, recorded='{}')
        with self.assertRaisesRegex(ValueError, 'different candidate'):
            backend.reserve(manifest)
        self.assertFalse(any(call[:2] == ('git', 'push') for call in calls))

    def test_original_reserved_commit_can_resume_after_master_advances(self):
        mod, path = self.prepare()
        manifest = mod.verify_candidate(path)
        backend, calls = self.backend(manifest, remote=True, latest=COMMIT_B)
        backend.reserve(manifest)
        self.assertFalse(any(call[:3] == ('git', 'fetch', 'origin') for call in calls))

    def test_new_reservation_requires_current_master(self):
        mod, path = self.prepare()
        backend, calls = self.backend(mod.verify_candidate(path), latest=COMMIT_B)
        with self.assertRaisesRegex(ValueError, 'latest master'):
            backend.reserve(mod.verify_candidate(path))
        self.assertFalse(any(call[:2] == ('git', 'push') for call in calls))

    def test_publisher_checkout_must_match_manifest_commit(self):
        mod, path = self.prepare()
        backend, calls = self.backend(mod.verify_candidate(path), checkout=COMMIT_B)
        with self.assertRaisesRegex(ValueError, 'checkout'):
            backend.reserve(mod.verify_candidate(path))
        self.assertEqual(len(calls), 1)

    def test_new_remote_tag_contains_identity_before_push(self):
        mod, path = self.prepare()
        manifest = mod.verify_candidate(path)
        backend, calls = self.backend(manifest)
        original = backend.run
        recorded = []
        def run(*args, **kwargs):
            if 'tag' in args and '-a' in args:
                recorded.append(Path(args[-1]).read_text().strip())
            return original(*args, **kwargs)
        backend.run = run
        backend.reserve(manifest)
        self.assertEqual(recorded, [mod.canonical(manifest)])
        tag_index = next(i for i, call in enumerate(calls) if 'tag' in call and '-a' in call)
        push_index = next(i for i, call in enumerate(calls) if call[:2] == ('git', 'push'))
        self.assertLess(tag_index, push_index)

    def test_upload_error_does_not_expose_api_key_or_skip_conflicts(self):
        mod = load()
        backend = mod.Backend('JacobChwastek/MessageTransit', 'secret-not-for-output')
        backend.run = lambda *args, **kwargs: SimpleNamespace(returncode=1, stdout='Response status code 403 (Forbidden)', stderr='')
        with self.assertRaises(RuntimeError) as caught:
            backend.push_package(Path('test.nupkg'))
        self.assertNotIn('secret-not-for-output', str(caught.exception))
        backend.run = lambda *args, **kwargs: SimpleNamespace(returncode=1, stdout='', stderr='Response status code 409 (Conflict)')
        self.assertFalse(backend.push_package(Path('test.nupkg')))

    def test_waiting_for_an_unindexed_package_gives_up_with_resume_guidance(self):
        mod = load()
        backend = mod.Backend('JacobChwastek/MessageTransit', 'key', wait_seconds=0, poll_seconds=0)
        backend.fetch_package = lambda entry, version: None
        with self.assertRaisesRegex(RuntimeError, 'resume'):
            backend.wait_for_package({'file': 'MessageTransit.1.0.0.nupkg'}, '1.0.0')

    def test_recovery_provenance_requires_original_release_run_and_commit(self):
        mod, path = self.prepare()
        valid = {'path': '.github/workflows/release.yml', 'head_sha': COMMIT_A, 'repository': {'full_name': 'JacobChwastek/MessageTransit'}}
        mod.validate_recovery_run(valid, 'JacobChwastek/MessageTransit', mod.verify_candidate(path))
        for bad in ({**valid, 'path': '.github/workflows/build.yml'}, {**valid, 'head_sha': COMMIT_B}, {**valid, 'repository': {'full_name': 'someone/else'}}):
            with self.assertRaises(ValueError):
                mod.validate_recovery_run(bad, 'JacobChwastek/MessageTransit', mod.verify_candidate(path))



class PreviewBackendTests(unittest.TestCase):
    setUp = PublicationTests.setUp

    def preview(self, version='1.0.0-preview.7'):
        mod = load()
        names = [f'{name}.{version}.nupkg' for name in ('MessageTransit', 'MessageTransit.Abstractions')]
        for name in names:
            (self.root / 'packages' / name).write_bytes(package(b'preview build'))
        for stale in self.names:
            (self.root / 'packages' / stale).unlink()
        (self.root / 'approved.txt').write_text('\n'.join(names) + '\n')
        path = self.root / 'candidate.json'
        mod.prepare_candidate(version, COMMIT_A, self.root / 'packages', self.root / 'approved.txt', path, self.root / 'report.md')
        backend = mod.PreviewBackend('JacobChwastek/MessageTransit', 'token-not-for-output', 'github-actions', 'github-previews',
                                     wait_seconds=0, poll_seconds=0)
        feed, pushes, requests = {}, [], []
        backend.fail_on = None
        base = 'https://nuget.pkg.github.com/JacobChwastek/download/'

        def get(url):
            requests.append(url)
            if url == backend.index_url:
                return json.dumps({'resources': [{'@id': base, '@type': 'PackageBaseAddress/3.0.0'}]}).encode()
            if url not in feed:
                raise mod.urllib.error.HTTPError(url, 404, 'Not Found', None, None)
            return feed[url]

        def run(*args, check=True):
            pushed = Path(args[3])
            pushes.append(pushed.name)
            self.assertEqual(args[args.index('--source') + 1], 'github-previews')
            if pushed.name == backend.fail_on:
                return SimpleNamespace(returncode=1, stdout='Response status code 500', stderr='')
            package_id = pushed.name.removesuffix(f'.{version}.nupkg').lower()
            feed[f'{base}{package_id}/{version}/{package_id}.{version}.nupkg'] = pushed.read_bytes()
            return SimpleNamespace(returncode=0, stdout='', stderr='')

        backend.get, backend.run = get, run
        return mod, path, names, backend, feed, pushes

    def test_retry_after_partial_preview_upload_skips_verified_packages_and_pushes_the_rest(self):
        mod, path, names, backend, feed, pushes = self.preview()
        backend.fail_on = names[1]
        with self.assertRaises(RuntimeError) as caught:
            mod.publish_candidate(path, backend)
        self.assertNotIn('token-not-for-output', str(caught.exception))
        self.assertEqual(pushes, [names[0], names[1]])
        self.assertEqual(len(feed), 1)
        backend.fail_on = None
        pushes.clear()
        mod.publish_candidate(path, backend)
        self.assertEqual(pushes, [names[1]])
        self.assertEqual(len(feed), 2)

    def test_preview_with_different_contents_on_the_feed_is_rejected(self):
        mod, path, names, backend, feed, pushes = self.preview()
        mod.publish_candidate(path, backend)
        for url in feed:
            feed[url] = package(b'another build')
        pushes.clear()
        with self.assertRaisesRegex(ValueError, 'conflicts'):
            mod.publish_candidate(path, backend)
        self.assertEqual(pushes, [])

    def test_preview_backend_authenticates_and_rejects_stable_versions(self):
        mod = load()
        backend = mod.PreviewBackend('JacobChwastek/MessageTransit', 'token', 'github-actions', 'github-previews')
        sent = []

        class Response(io.BytesIO):
            def __enter__(self):
                return self

            def __exit__(self, *args):
                return False

        def urlopen(request, timeout):
            sent.append(request.get_header('Authorization'))
            return Response(b'{}')

        original = mod.urllib.request.urlopen
        mod.urllib.request.urlopen = urlopen
        try:
            backend.get(backend.index_url)
        finally:
            mod.urllib.request.urlopen = original
        self.assertEqual(sent, ['Basic ' + mod.base64.b64encode(b'github-actions:token').decode()])
        with self.assertRaisesRegex(ValueError, 'preview'):
            backend.reserve({'version': '1.0.0'})


if __name__ == '__main__':
    unittest.main()
