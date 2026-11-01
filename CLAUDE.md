# Workflow

All changes go through Spec Kit skills in `.claude/skills/`. Run each step, show the result, and wait for my review before the next one.

- **New feature or improvement to an existing one:** `/speckit-specify` → `/speckit-clarify` (if anything is ambiguous) → `/speckit-plan` → `/speckit-tasks` → `/speckit-analyze` → `/speckit-implement` → `/speckit-converge`, repeating implement/converge until it reports `Converged`.
- **Bug fix:** `/speckit-bug-assess "<symptom>" slug=<slug>` → `/speckit-bug-fix slug=<slug>` → `/speckit-bug-test slug=<slug>`. Only a `verified` verdict counts as fixed.
- If `.specify/memory/constitution.md` is still the template, run `/speckit-constitution` first.
- Don't edit code outside these flows unless I explicitly say to skip Spec Kit.
