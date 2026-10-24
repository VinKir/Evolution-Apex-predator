# Workflow

## Before changing code
- Discuss the high-level approach with the user before editing code. During design discussions, give only analysis and recommendations, and wait for explicit approval before editing files.
- Don't use sub-agents unless the user explicitly asks for them.
- Read the code before drawing conclusions. If the answer depends on how a Unity package behaves, read the installed source in `Library/PackageCache/<package>@<version>/` instead of answering from memory. Read only the specific type or method you need.

## Editing
- In files with repeated structures (big MonoBehaviours, `switch` tables such as `OrganismStatsCalculator.AddModifier`), anchor each edit to the exact method or case. After editing, check with `git diff -- <file>` that only the intended block changed.
- Don't add warning suppressions (`#pragma warning disable`, `[SuppressMessage]`) unless the user approves that specific one.
- Keep `try` blocks minimal: wrap only the lines that can throw.
- Prefer full descriptive names over abbreviations in variables, loop variables and lambdas.

## Saving output
- Save research summaries to `.claude/docs/research/` and agreed plans to `.claude/docs/plans/`, named `YYYY-MM-DD-short-summary.md`.
- Keep a plan in line with the current agreed design and implementation state, not as a work log. Record decisions, completed and remaining work, and blockers, so work can resume after compaction. Update sections in place.
- Put temporary probe scripts in `.claude/docs/tmp/`, which is gitignored and cleaned by the user. Don't delete them unless asked.

## Responses
- Keep responses concise. For new files, give a link and a one-line description. For edits, summarize only the meaningful changes. Don't paste the content of saved plans, research or deleted files unless asked.
- Don't put copyable text (comments, commit messages) in `>` blockquotes. Use plain text or fenced code blocks.
- Within one answer, don't restart numbering. Use headers or hierarchical numbering (`1.1`, `2.1`).
- Keep persistent instructions in project files (`CLAUDE.md`, `.claude/rules/`), not in global `~/.claude` memory.
