import importlib.util
import io
from pathlib import Path
import unittest
from unittest.mock import patch

SCRIPT = Path(__file__).resolve().parents[1] / 'release_version.py'


def load():
    if not SCRIPT.exists():
        raise AssertionError('prerelease version selector is not implemented')
    spec = importlib.util.spec_from_file_location('release_version', SCRIPT)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


class VersionTests(unittest.TestCase):
    def test_first_release_is_preview(self):
        self.assertEqual(load().choose_version([], ['fix: initial']), ('1.0.0-preview.1', True))

    def test_first_explicit_version_and_bump_are_honored(self):
        mod = load()
        self.assertEqual(mod.choose_version([], ['chore: initial\n\nRelease-As: 2.0.0-preview.1']), ('2.0.0-preview.1', True))
        self.assertEqual(mod.choose_version([], [], bump='major'), ('2.0.0-preview.1', True))

    def test_preview_increments_and_stable_promotion_is_explicit(self):
        mod = load()
        self.assertEqual(mod.choose_version(['1.0.0-preview.1'], ['fix: regression']), ('1.0.0-preview.2', True))
        self.assertEqual(mod.choose_version(['1.0.0-preview.2'], [], channel='stable'), ('1.0.0', True))
        self.assertEqual(mod.choose_version(['1.0.0-preview.2'], []), ('1.0.0-preview.2', False))

    def test_stable_baseline_uses_conventional_commits(self):
        mod = load()
        self.assertEqual(mod.choose_version(['1.2.3'], ['feat: new']), ('1.3.0-preview.1', True))
        self.assertEqual(mod.choose_version(['1.2.3'], ['fix!: incompatible']), ('2.0.0-preview.1', True))
        self.assertEqual(mod.choose_version(['1.2.3'], ['fix: repair'], channel='stable'), ('1.2.4', True))

    def test_channel_mismatch_and_non_increasing_override_fail(self):
        mod = load()
        for kwargs in ({'explicit': '1.0.0'}, {'explicit': '1.0.0-preview.1', 'channel': 'stable'}, {'explicit': '1.0.0-preview.1'}):
            with self.subTest(kwargs=kwargs), self.assertRaises(ValueError):
                mod.choose_version(['1.0.0-preview.1'], ['fix: repair'], **kwargs)

    def test_preview_series_after_stable_covers_every_change_since_stable(self):
        mod = load()
        since_stable = ['feat!: drop api', 'fix: repair']
        self.assertEqual(mod.choose_version(['1.0.0', '1.0.1-preview.1'], ['feat!: drop api'], stable_messages=since_stable),
                         ('2.0.0-preview.1', True))
        self.assertEqual(mod.choose_version(['1.0.0', '1.0.1-preview.2'], [], channel='stable', stable_messages=['fix: a', 'feat: b']),
                         ('1.1.0', True))
        self.assertEqual(mod.choose_version(['1.0.0', '1.1.0-preview.1'], ['fix: c'], stable_messages=['feat: b', 'fix: c']),
                         ('1.1.0-preview.2', True))

    def test_release_as_footer_adapts_to_the_channel(self):
        mod = load()
        self.assertEqual(mod.choose_version(['1.0.0-preview.3'], ['fix: y\n\nRelease-As: 2.0.0']), ('2.0.0-preview.1', True))
        self.assertEqual(mod.choose_version(['2.0.0-preview.1'], ['fix: y\n\nRelease-As: 2.0.0']), ('2.0.0-preview.2', True))
        self.assertEqual(mod.choose_version(['1.0.0-preview.3'], ['Release-As: 2.0.0-preview.1'], channel='stable'), ('2.0.0', True))

    def test_stale_release_as_footer_falls_back_to_commits(self):
        mod = load()
        with patch('sys.stderr', io.StringIO()) as errors:
            self.assertEqual(mod.choose_version(['2.0.0'], ['fix: y\n\nRelease-As: 1.5.0']), ('2.0.1-preview.1', True))
        self.assertIn('Ignoring Release-As: 1.5.0', errors.getvalue())

    def test_numeric_preview_order_and_upstream_tags(self):
        mod = load()
        self.assertEqual(mod.choose_version(['v8.5.10', '1.0.0-preview.9', '1.0.0-preview.10'], ['fix: repair']), ('1.0.0-preview.11', True))

    def test_pull_request_previews_the_stable_version_of_its_merge(self):
        mod = load()
        stable, release = mod.choose_version([], ['fix: initial'], channel='stable')
        self.assertEqual((stable, release), ('1.0.0', True))
        self.assertEqual(mod.pull_request_preview(stable, 42), '1.0.0-preview.42')
        stable, _ = mod.choose_version(['1.0.0'], ['feat: new'], channel='stable')
        self.assertEqual(mod.pull_request_preview(stable, 43), '1.1.0-preview.43')
        for version, number in (('1.0.0-preview.1', 1), ('1.0.0', 0)):
            with self.subTest(version=version, number=number), self.assertRaises(ValueError):
                mod.pull_request_preview(version, number)


if __name__ == '__main__':
    unittest.main()
