# Hyper-Waterfall (NovaModbus Customized)

Source: <https://github.com/edwardkim/rhwp> (`mydocs/manual/hyper_waterfall.md`,
`hyper_waterfall_docs_guide.md`, `codex/docs_and_git_workflow.md`).
Adapted for a single-developer .NET 10 + WPF project. The rhwp-specific
machinery (multi-platform CI, visual sweep, WASM, upstream/devel,
external-contributor routing, parallel-session locks, mandatory PR
integration) is intentionally dropped.

## 1. Definition

**Macro waterfall discipline, micro agile speed — AI makes both possible
in one cycle of hours, not weeks.**

```
Macro (project level) — waterfall discipline:
  plan ──> design ──> implement ──> verify ──> integrate
    │          │           │            │            │
    ▼          ▼           ▼            ▼            ▼
  docs       docs        docs         docs         docs

Micro (task level) — agile speed:
  implement -> test -> feedback -> fix -> test -> ... (fast loop)
      │          │         │          │        │
     AI      automation  human       AI   automation
```

Plan-mode agent writes the plan, human approves, Build-mode agent
implements step by step. Every step is verified before the next begins.

## 2. Three Principles

Violate these and the work degrades into vibe coding.

### P1. Keep the goal in the AI context

The AI is consistent only inside the current conversation. Vision,
architecture rules, and quality bars must live in files, not memory.

- `AGENTS.md` + `rules/` carry project rules, build commands, workflow.
- Every task states goal and scope at registration.
- Long context: re-inject a summary of key decisions; never rely on
  "the AI remembers".

### P2. The director keeps control

The human decides what to build, in what order, and when to stop.

- AI proposes plans; the human approves or requests changes.
- AI never declares a task finished — the director does.
- Architecture changes always pass human judgment.
- Never wave through with "looks good" — verify by execution
  (`dotnet build`, `dotnet test`, spec vectors).

### P3. Periodically verify AI context retention

- When the AI starts forgetting earlier decisions, that is the warning
  signal: stop, summarize, re-inject.
- Externalize key decisions to `docs/` files, not chat memory.
- New session starts from `docs/orders/<today>.md` + the latest step
  report, not from zero.

> "The human never stops thinking."

## 3. Roles

Single developer wears two hats: **director** (always) and occasionally
**direct editor** (quick fixes by hand). The AI is the pair programmer.

### Director (human)

Thinks: direction ("what next?"), priority, quality judgment
("is this good enough?"), architecture decisions, domain knowledge
(Modbus spec, serial timing, WPF threading), feedback ("this is wrong
because...").

Direct hand-edits are allowed anytime. The AI treats them as authoritative:
never revert or overwrite them without asking.

### AI pair programmer

Executes: codebase exploration, plan writing, code + test authoring,
reports and docs, log analysis, fix proposals, iteration on feedback.

Reads every line it produces before presenting it. Never asks the
director to rubber-stamp unverified output.

## 4. Task Cycle

```
1. Register .... Director: GitHub Issue (scope + goal), duplicate search first
2. Plan ........ AI: plan doc, 3-6 steps (rules/docs-and-git-workflow.md)
                 Director: review -> approve or request changes
3. Implement ... AI: code + tests per step, step report per step
                 Director: verify -> approve or feedback
4. Feedback .... Director: correction notes (docs/feedback/, human-only)
                 AI: apply fixes
5. Report ...... AI: final report (plan vs result, deltas explained)
                 Director: approve -> merge to main (direct, no PR required)
                 -> close issue
```

One step = one commit (code + its docs together). No step starts
without the previous step's approval. Step approval covers commit +
push of the task branch; only merge-to-main, PR creation (optional),
and issue close need a separate go-ahead.

## 5. Quality Gates

Every task passes all of these (details in `rules/docs-and-git-workflow.md`):

1. **Plan approval** — direction checked before code exists.
2. **Build clean** — `dotnet build -c Release`, warnings = errors.
3. **Tests pass** — `dotnet test`, full suite, no golden weakening.
4. **Spec grounding** — protocol claims checked against Modbus vectors
   (e.g. CRC `01 03 00 00 00 0A -> C5 CD`), not just self-consistent tests.
5. **No regression** — pre-existing tests green; `[TestCategory("Hardware")]`
   stays excluded without real COM ports.

## 6. When NOT to Use This

- "Build fast and throw away" prototypes.
- Directing without domain knowledge (Modbus/C#/WPF) to judge AI output.
- Skipping verification because the cycle feels slow — that is exactly
  the vibe-coding trap this methodology exists to prevent.

## 7. Single-Developer Simplifications

Ceremony kept vs. dropped for one person:

| Kept | Dropped |
|------|---------|
| Plan doc + approval before code | Issue assignee-as-lock (no competing sessions) |
| Step reports + verification per step | Mandatory PR integration (direct merge to main) |
| `docs/` evidence trail | Monthly archive ritual (archive when root gets noisy) |
| Quality gates (build/test/spec/regression) | Draft→Ready PR staging, reviewer routing |
| Approval for merge / close | Per-push approval (covered by step approval) |
