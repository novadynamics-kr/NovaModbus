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
   `gh issue edit <n> --add-assignee '@me'` (PowerShell: quoted) marks the
   issue in-progress, which is what a resumed session reads to know the
   state. If the token scope rejects assignee edit, skip it and record
   in-progress state in `docs/orders/` instead.
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

- Base is latest `main`. One task = one **local-only** branch
  (`task/<issue>-<slug>`). Task branches are never pushed: no remote
  branches, no PRs. Trivial hotfixes may commit directly on `main`.
- One step = one local commit: code + its step report + order update.
  Steps are not pushed individually.
- Task end (after approval): merge the task branch into local `main`,
  run the pre-merge gate, then publish with `git push nova main` directly.
  Delete the local task branch after a successful push.
- `git status` check before every merge/push. Never revert or delete
  changes the director made by hand without asking.
- Before closing an issue, confirm the fix commits are actually in `main`.
- Pre-merge gate (full local regression, no shortcuts):
  `dotnet build NovaModbus.slnx -c Release` then
  `dotnet test NovaModbus.slnx -c Release`.

## 8. Remote Sync (git over gh auth + gh)

File transfer is git's job; API work (issues/PRs) is gh's job. Both
authenticate through the single `gh auth login` (git side uses the
credential helper it configures — verify once with
`git config credential.helper`; if empty, run `gh auth setup-git`).
The canonical remote name in this repo is **`nova`**, not `origin`.

```powershell
git fetch nova     # remote state refresh (read-only)
git pull nova main # update local main (task start)
git push nova main # publish (task end only — main only, never a task branch)
```

Cycle placement:

1. Task start: `git fetch nova` → `main` 최신 확인 후 로컬 task 브랜치 생성.
   Task 브랜치는 절대 push하지 않는다.
2. Step approval: 로컬 commit only (push 없음).
3. Task end (after merge approval): `main`으로 전환 → merge →
   pre-merge gate → `git push nova main` → 로컬 task 브랜치 삭제
   (`git branch -d`).
4. `gh repo sync` may be used as a shortcut for step 1 refresh, but
   always confirm with `git status --short --branch` afterwards.

Never name a task branch in a push (that creates a remote branch).
If one slips through, remove it with
`git push nova --delete <branch>` and unset the local upstream
(`git branch --unset-upstream`).

`gh` never pushes files and git never touches issues — mixing them up
(e.g. expecting `gh` to upload commits) is the common mistake this
section exists to prevent.

## 9. Task Start Checklist (new session)

```text
1. docs/orders/<today>.md — what is on the plate, what state?
2. Latest docs/working/task_* report — where did we stop?
3. docs/plans/task_*_impl.md — what was agreed?
4. docs/feedback/ + docs/tech/ — why this approach, which traps?
5. gh issue view — state (open? self-assigned? close confirmed in main?).
```

Director context-handoff cost should be near zero when these exist.
