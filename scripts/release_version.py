#!/usr/bin/env python3
"""Select fork release versions; preview is the default and stable promotion is explicit."""

import argparse
import os
from pathlib import Path
import re
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[1]
TAG_PREFIX = 'messagetransit-v'
VERSION = re.compile(r'^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(?:-preview\.([1-9]\d*))?$')
RELEASE_AS = re.compile(r'^Release-As:[ \t]*(\S+)[ \t]*$', re.MULTILINE)


def parse_version(text):
    match = VERSION.fullmatch(text)
    if not match:
        raise ValueError(f'Invalid version {text!r}; use X.Y.Z or X.Y.Z-preview.N')
    major, minor, patch, preview = match.groups()
    return int(major), int(minor), int(patch), int(preview) if preview else None


def version_key(text):
    major, minor, patch, preview = parse_version(text)
    return major, minor, patch, preview is None, preview or 0


def latest_version(versions):
    valid = [value for value in versions if VERSION.fullmatch(value)]
    return max(valid, key=version_key, default=None)


def latest_stable(versions):
    return latest_version([value for value in versions if VERSION.fullmatch(value) and parse_version(value)[3] is None])


def bump_core(core, bump):
    major, minor, patch = core
    return {'major': (major + 1, 0, 0), 'minor': (major, minor + 1, 0), 'patch': (major, minor, patch + 1)}[bump]


def bump_kind(messages):
    if any(re.match(r'^\w+(\([^)]*\))?!:', message) or re.search(r'^BREAKING[ -]CHANGE:', message, re.MULTILINE) for message in messages):
        return 'major'
    if any(re.match(r'^feat(\([^)]*\))?:', message) for message in messages):
        return 'minor'
    return 'patch'


def format_version(core, channel, preview=1):
    text = '.'.join(map(str, core))
    return text if channel == 'stable' else f'{text}-preview.{preview}'


def release_as(override, previous, channel):
    """Adapt a Release-As footer to the selected channel; a stable footer on the preview channel starts its preview series."""
    major, minor, patch, preview = parse_version(override)
    core = (major, minor, patch)
    if channel == 'stable':
        return format_version(core, 'stable')
    if preview is not None:
        return override
    if previous and parse_version(previous)[:3] == core and parse_version(previous)[3] is not None:
        return format_version(core, 'preview', parse_version(previous)[3] + 1)
    return format_version(core, 'preview')


def choose_version(versions, messages, channel='preview', bump='auto', explicit=None, stable_messages=None):
    """messages are commits since the latest release tag; stable_messages are commits since the latest stable tag."""
    if channel not in ('preview', 'stable') or bump not in ('auto', 'patch', 'minor', 'major'):
        raise ValueError('Invalid channel or bump')
    previous = latest_version(versions)
    if explicit:
        if (parse_version(explicit)[3] is None) != (channel == 'stable'):
            raise ValueError('Explicit version must match the selected preview/stable channel')
        if previous and version_key(explicit) <= version_key(previous):
            raise ValueError('Explicit version must be higher than the reserved release version')
        return explicit, True
    overrides = [match.group(1) for message in messages for match in RELEASE_AS.finditer(message)]
    if overrides:
        selected = release_as(overrides[0], previous, channel)
        if not previous or version_key(selected) > version_key(previous):
            return selected, True
        print(f'Ignoring Release-As: {overrides[0]}; {selected} is not higher than {previous}', file=sys.stderr)
    if not previous:
        return format_version((1, 0, 0) if bump == 'auto' else bump_core((1, 0, 0), bump), channel), True
    major, minor, patch, preview = parse_version(previous)
    core = (major, minor, patch)
    if bump != 'auto':
        return format_version(bump_core(core, bump), channel), True
    if preview is None:
        if not messages:
            return previous, False
        return format_version(bump_core(core, bump_kind(messages)), channel), True
    # a preview series must cover every change since the latest stable release, or semver would understate a promotion
    stable = latest_stable(versions)
    required = bump_core(parse_version(stable)[:3], bump_kind(messages if stable_messages is None else stable_messages)) if stable else (1, 0, 0)
    if required > core:
        return format_version(required, channel), True
    if channel == 'stable':
        return format_version(core, 'stable'), True
    if not messages:
        return previous, False
    return format_version(core, 'preview', preview + 1), True


def git(*arguments):
    return subprocess.check_output(['git', *arguments], cwd=ROOT, text=True).strip()


def messages_since(version):
    revision_range = f'{TAG_PREFIX}{version}..HEAD' if version else 'HEAD'
    return [message.strip() for message in git('log', '--format=%B%x00', revision_range).split('\0') if message.strip()]


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--channel', choices=('preview', 'stable'), default='preview')
    parser.add_argument('--bump', choices=('auto', 'patch', 'minor', 'major'), default='auto')
    parser.add_argument('--version', help='Explicit version matching the selected channel')
    args = parser.parse_args()
    tags = git('tag', '--merged', 'HEAD', '--list', f'{TAG_PREFIX}*').split()
    versions = [tag.removeprefix(TAG_PREFIX) for tag in tags]
    stable = latest_stable(versions)
    version, release = choose_version(versions, messages_since(latest_version(versions)), args.channel, args.bump, args.version,
                                      messages_since(stable) if stable else None)
    commit = git('rev-parse', 'HEAD')
    print(f'{version} ({"candidate" if release else "nothing to release"})')
    if 'GITHUB_OUTPUT' in os.environ:
        with open(os.environ['GITHUB_OUTPUT'], 'a', encoding='utf-8') as output:
            output.write(f'version={version}\nrelease={str(release).lower()}\ncommit={commit}\n')


if __name__ == '__main__':
    try:
        main()
    except ValueError as error:
        raise SystemExit(str(error)) from error
