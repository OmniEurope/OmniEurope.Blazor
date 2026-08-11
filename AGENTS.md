# AGENTS.md - OmniEurope.Blazor

Before doing project work:

1. Read and follow `CLAUDE.md`.
2. Treat `CLAUDE.md` as the primary project instruction source.
3. Read only the task-relevant documents referenced by `CLAUDE.md`.
4. Read `.claude/handoff.md` when resuming unfinished work, but verify that its branch and commit still match the current repository state.
5. Treat the code, `OmniEurope.Blazor.slnx`, and every checked-in architecture/convention guard test as the structural ground truth; do not assume an uncreated guard exists.

Do not duplicate project rules here. Update the canonical documentation instead.

Host note: Claude Code may use installed named agents; Codex uses runtime collaboration sub-agents and does not load Claude's `agents/*.md` definitions. In either host, review-only is a behavioral policy, not a filesystem sandbox; delegated prompts must preserve scope, safety, language, and output rules.
