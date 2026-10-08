# Repository instructions

## Source edits

- Use file-scoped namespaces in new or edited C# files where the file structure permits it.
- Keep base classes and implemented interfaces on the type declaration line when the declaration fits within 160 characters; wrap longer lists.
- Do not add per-file fork or modification comments, including `Modified for MessageTransit` or `MessageTransit change` markers, when editing source, project files, build configuration, or documentation.
- Describe meaningful changes in commit messages and pull request descriptions. Add public migration guidance when a change affects consumers.
- Preserve existing copyright notices, license text, and third-party attribution.

## Public documentation

- Write public documentation for its public audience. Keep it self-contained.
- Do not reference internal decisions, `.workflow`, agent reports, private task boards, issue identifiers, personal application inventories, or private conversation confirmations in public content.
- Preserve necessary upstream and third-party attribution.

## Git naming

- Do not prefix pull request titles, branch names, or commit messages with `[codex]` unless explicitly requested.
- Do not use `codex/` as a branch prefix unless explicitly requested.
- Use plain, repository-appropriate pull request titles, Conventional Commit messages, and short issue branches such as `feature/<issue-id>`.

## Workflow

- Inspect the current branch and working tree before making changes. Preserve unrelated user changes.
- Do not stage, commit, push, rewrite history, publish packages, or change external systems without explicit authorization for that action.
- Match verification to the changes and distinguish passed checks from skipped or unavailable checks.
