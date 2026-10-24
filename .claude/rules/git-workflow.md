---
paths:
  - ".git/**"
  - "**/*.cs"
---

# Git workflow

Treat git as the source of truth when a task refers to an existing commit, a previous implementation, or similar past behavior.

## Commits
- Don't commit unless the user asks.
- Only the developer appears in history. Don't add `Co-Authored-By` or other AI attribution.
- Message style is Conventional Commits in lowercase: `feat: ...`, `fix: ...`, `refactor: ...`.

## Commit-specific work
- If a task names a commit hash, inspect it before changing code:
  - `git show --name-only <hash>` to list the changed files
  - `git show <hash> -- <file>` to see the exact diff for one file
- Don't guess the changed files from memory or file names.

## Pattern hunting
- `git log --oneline --grep="<pattern>"` finds candidate commits.
- `git log -p -- <file>` shows how a file evolved.
- `git blame <file>` answers line-specific questions.

## Review discipline
- If history shows an implementation was intentionally removed, don't bring it back indirectly under a new helper or flag.
