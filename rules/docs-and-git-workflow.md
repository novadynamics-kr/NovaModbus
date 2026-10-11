# Docs & Git Workflow (Hyper-Waterfall for NovaModbus)

Operational companion to `rules/hyper-waterfall.md`.
Distilled from rhwp `codex/docs_and_git_workflow.md` and its `AGENTS.md`
"GitHub CLI Usage" rules; rhwp-only machinery (upstream/devel split,
external-contributor roles, visual-sweep gates) is omitted.

## 1. Agent Bootstrap (read order)

1. `AGENTS.md`
2. `rules/hyper-waterfall.md` (this methodology)
3. `rules/docs-and-git-workflow.md` (this file — task session)
4. Task-specific: `docs/orders/<today>.md`, latest `docs/working/*` report,
   `docs/plans/*` plan, related `docs/feedback/*` + `docs/tech/*`

A more specific document wins over a summary when they differ.

## 2. Document Language

All project docs in Korean. Code and code comments in English.

## 3. Folder Roles (`docs/`)

```text
docs/
├── orders/ ............ daily task list (today's work + status)
├── plans/ ............. plan doc + implementation plan per task
├── working/ ............ step completion reports
├── report/ ............. final result reports
├── feedback/ ........... director corrections, code-review notes (human-only,
│                         AI never fabricates these)
├── tech/ ............... discovered technical facts (spec findings, timing math)
├── troubleshootings/ ... hard problems + resolutions ("don't fall in again")
└── archives/ ........... prior-month records, per-folder archives/ when large
```

- Direct children of `orders/plans/working/report` = **current working
  set** (fast lookup). When the root gets noisy, move older records to
  `archives/` (no fixed monthly ritual at this volume).
  Archiving never implies issue closure.
- Root `docs/` holds task evidence. No second doc root (no `mydocs/`, no `doc/`).
- Session state ("what am I doing now") is never stored in rule files —
  it lives in `orders/` + step reports.

## 4. Naming

File stem carries the **GitHub issue number** (issues and PRs share one
number space — state `Issue: #N` in the report body, never guess a PR number
for a filename). Phase work without an issue uses `phase{N}`.

```text
docs/orders/YYYYMMDD.md ............ e.g. 20261011.md
docs/plans/task_{issue}.md ......... goal, scope, expected steps (plan doc)
docs/plans/task_{issue}_impl.md .... file-by-file change plan (impl plan)
docs/working/task_{issue}_stage{N}.md  step N completion report
docs/report/task_{issue}_report.md ..... final report (plan vs result + deltas)
```

## 5. Plan / Step-Report Templates

Plan doc (`docs/plans/task_{issue}.md`):

```markdown
# Task {issue}: <title>
Issue: #N
Goal: ...
Non-goals: ...
Steps (3-6): 1. ... 2. ... 3. ...
Quality gates: build / test / spec-vector / regression
Approval: [ ] director approved (date)
```

Step report (`docs/working/task_{issue}_stage{N}.md`):

```markdown
# Task {issue} Stage {N}: <title>
Changes: <files + one-line why each>
Verification: build log / test count / spec vectors checked
Deviations from plan: <none | ... + reason>
Approval: [ ] director approved (date)
```

Final report (`docs/report/task_{issue}_report.md`): plan-vs-result table,
unresolved items, follow-ups.

## 6. Issue Workflow (gh CLI)

GitHub mutations go through local authenticated `gh`
(`gh auth login` required once). No direct API-token scripts.

```powershell
gh issue list -R novadynamics-kr/NovaModbus --state open
gh search issues -R novadynamics-kr/NovaModbus --state open "<symptom keywords>"
gh issue view <n> -R novadynamics-kr/NovaModbus
gh issue edit <n> -R novadynamics-kr/NovaModbus --add-assignee @me
gh issue comment <n> -R novadynamics-kr/NovaModbus --body "..."
```

Rules:

1. **Duplicate search first** — before opening an issue, search symptoms
   once. If an open issue covers it, attach analysis there instead.
2. **Self-assign at start** — single developer, so no lock protocol.
   `gh issue edit <n> --add-assignee @me` marks the issue in-progress,
   which is what a resumed session reads to know the state.
3. **Approval scope** — step approval covers commit + push of the task
   branch. Merge-to-main, `gh pr create` (optional), and issue close each
   need a separate explicit go-ahead. The director writes issue comments
   directly; the AI posts a comment only when asked.
4. **gh actions are recorded** — every `gh` mutation is noted in the
   related order/plan/report doc.
5. **Korean multi-line bodies on Windows PowerShell** — never pipe a
   here-string into `--body-file -`. Write a UTF-8 **without BOM** temp
   `.md` file, pass it via `--body-file`, then verify Korean/BOM via the API.
6. `gh` never replaces the cycle: issue check, branch, docs, approval gates
   stay mandatory.

## 7. Branch / Commit / Merge Rules

- Base is latest `main`. One task = one branch (`task/<issue>-<slug>`).
  Trivial hotfixes may commit directly on `main`; everything else uses a
  task branch. Integrate by direct fast-forward merge after approval —
  PR creation is optional (milestone review only), never required.
- One step = one commit: code + its step report + order update together.
- `git status` check before every merge/push. Never revert or delete
  changes the director made by hand without asking.
- Before closing an issue, confirm the fix commits are actually in `main`.
- Pre-merge gate (full local regression, no shortcuts):
  `dotnet build NovaModbus.slnx -c Release` then
  `dotnet test NovaModbus.slnx -c Release`.

## 8. Task Start Checklist (new session)

```text
1. docs/orders/<today>.md — what is on the plate, what state?
2. Latest docs/working/task_* report — where did we stop?
3. docs/plans/task_*_impl.md — what was agreed?
4. docs/feedback/ + docs/tech/ — why this approach, which traps?
5. gh issue view — state (open? self-assigned? close confirmed in main?).
```

Director context-handoff cost should be near zero when these exist.
