# Cross-Thread Development Waitlist

## Purpose

This file is the live queue for multi-thread Orchestral development.

It answers:

- what still needs planning,
- what is actually ready for a coding thread,
- what is currently in progress,
- what is blocked,
- what is waiting to land,
- and what is already landed.

This file should be updated whenever a planning, coding, debug, landing, or optimization thread closes.

If the same item appears in more than one section, this file is wrong and must be cleaned up.

---

## Queue Discipline

Default rules:

- planning threads shape the queue,
- coding threads pick the first truthful item from `Ready For Coding`,
- landing threads close `Ready To Land` items,
- and optimization work belongs in `Optimization Candidates` until it is scoped and approved.

If `Ready For Coding` is empty, the next coding thread should **not** start by improvising work.
It should report that planning or waitlist-maintenance is needed.

---

## Item Template

Use this structure for each item:

- `id`
- `title`
- `thread type`
- `priority`
- `layer`
- `depends on`
- `governing docs`
- `handoff / plan`
- `branch / worktree`
- `current truth`
- `next action`

Keep each item short.
Link to the handoff packet or plan for the detailed context.

---

## Needs Planning

- `PL-001`
  - title: `Strategic roadmap re-baseline and ROADMAP_V2`
  - thread type: `planning`
  - priority: `P0`
  - layer: `project strategy`
  - depends on: `none`
  - governing docs:
    - [2026-04-03-planning-thread-context.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/development/2026-04-03-planning-thread-context.md)
    - [THREAD_CONTEXT_MANAGEMENT_CONTRACT.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/development/THREAD_CONTEXT_MANAGEMENT_CONTRACT.md)
  - handoff / plan: `none yet`
  - branch / worktree: `planning thread should decide`
  - current truth: `older roadmap docs are likely stale; no replacement roadmap has been written yet`
  - next action: `open a planning-only thread and produce the new strategic roadmap baseline`

- `PL-002`
  - title: `Landing audit and queue normalization`
  - thread type: `planning`
  - priority: `P0 — completed`
  - layer: `project operations`
  - depends on: `none`
  - governing docs:
    - [THREAD_CONTEXT_MANAGEMENT_CONTRACT.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/development/THREAD_CONTEXT_MANAGEMENT_CONTRACT.md)
    - [CROSS_THREAD_DEV_WAITLIST.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/development/CROSS_THREAD_DEV_WAITLIST.md)
  - handoff / plan: [2026-04-03-landing-audit.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/development/2026-04-03-landing-audit.md)
  - branch / worktree: `n/a — planning-only task`
  - current truth: `audit completed 2026-04-03; all slices classified against Git evidence; waitlist normalized`
  - next action: `none — completed`

---

## Planned But Not Ready

- `SL-001`
  - title: `Minimum experiment-logic proof using Flow/Reynolds`
  - thread type: `coding`
  - priority: `P1`
  - layer: `experiment logic`
  - depends on: `PL-001`
  - governing docs:
    - [2026-04-03-planning-thread-context.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/development/2026-04-03-planning-thread-context.md)
  - handoff / plan: `none yet`
  - branch / worktree: `not started`
  - current truth: `this is the likely next build frontier, but the strategic re-baseline should confirm it first`
  - next action: `planning thread confirms or reshapes the next execution frontier`

---

## Ready For Coding

No items currently ready.

That is intentional.
The next coding thread should not guess the next slice until planning has refreshed the queue.

---

## In Progress

No items currently in progress.

---

## Blocked

No blocked items currently recorded here.

---

## In Review

No items currently in review.

---

## Ready To Land

- `LD-001`
  - title: `PT-104 runtime-source harness`
  - thread type: `landing`
  - priority: `P1`
  - layer: `universal runtime foundation`
  - depends on: `none`
  - governing docs:
    - [MODULE_DEVELOPMENT_WORKFLOW.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/development/MODULE_DEVELOPMENT_WORKFLOW.md)
  - handoff / plan:
    - `plan: C:\Users\Yi Zhuang\.config\superpowers\worktrees\Orchestral\runtime-io-microphone\docs\development\2026-04-02-pt104-runtime-source-harness-plan.md`
    - `review: C:\Users\Yi Zhuang\.config\superpowers\worktrees\Orchestral\runtime-io-microphone\docs\collaboration\reviews\2026-04-02-pt104-runtime-source-harness-review-claude-opus-4-6.md`
    - `response: C:\Users\Yi Zhuang\.config\superpowers\worktrees\Orchestral\runtime-io-microphone\docs\collaboration\reviews\2026-04-02-pt104-runtime-source-harness-response.md`
  - branch / worktree:
    - branch: `codex/pt104-runtime-source-harness`
    - worktree: `C:\Users\Yi Zhuang\.config\superpowers\worktrees\Orchestral\runtime-io-microphone`
    - commit: `7465325`
  - current truth: `slice is implemented, verified, reviewed, and pushed; it still needs an explicit landing decision`
  - next action: `run the landing step when ready`

---

## Landed

- `LD-000`
  - title: `V2 design + runtime foundation + architecture docs baseline`
  - thread type: `landed`
  - priority: `historical`
  - layer: `shared foundation`
  - depends on: `none`
  - governing docs:
    - [V1_EXECUTION_TRACK.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/development/V1_EXECUTION_TRACK.md)
  - handoff / plan: `recorded in prior slice artifacts`
  - branch / worktree: `origin/main @ be0529f (29 commits)`
  - current truth: |
    Verified 2026-04-03 via landing audit. Three landed groups:
    1. V2 design migration — theme tokens, shared shell, widgets, panel templates (PR #1, commit `caf3262`)
    2. Runtime foundation chain — microphone session, control-center pilot, runtime IO migrations, operator controls, safe panel close, session-local validation, coordinator + stop authority, experiment definition + role binding, high-rate stream buffering, run context, run recorder, controller unit session, run monitor + alarm surface, flow/Reynolds derived-state, control-center capability decomposition, cross-session validation, experiment-definition linting
    3. Architecture docs — experiment logic layer, canvas architecture, canvas block contract, typed block kinds, virtual twin strategy, universal completion planning
  - next action: `none`

---

## Historical

Old branches whose work is already incorporated into `origin/main` or that carry no unique work. These are recorded for reference, not tracked as active queue items.

- `codex/orchestral-design-v2-landing` — landed via PR #1 squash merge
- `codex/orchestral-design-v2-migration` — earlier draft, superseded by PR #1
- `codex/orchestral-design-v2-migration-full` — fuller draft, superseded by PR #1
- `codex/runtime-io-doc-push` — fully merged to origin/main
- `codex/runtime-io-microphone` — fully merged to origin/main
- `claude/cool-almeida` — no unique work
- `codex/panel-contract-enforcement` — no unique work

Corresponding worktrees for several of these branches still exist on disk. They can be cleaned up whenever convenient but are not blocking anything.

Audit reference: [2026-04-03-landing-audit.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/development/2026-04-03-landing-audit.md)

---

## Operational Notes

### Stale local main checkout

As of 2026-04-03, the local `main` checkout is at `af36d25` (8 commits) while `origin/main` is at `be0529f` (29 commits). Local main is missing ~21 commits of runtime work that was pushed through worktrees. It also has 1 commit (`af36d25`) that duplicates content already on `origin/main` as `01e4162`, and 2 stashes.

This needs syncing before any new coding work in the main checkout folder. The worktrees are more current.

### Worktree inventory

6 worktrees exist. Only the `runtime-io-microphone` worktree (on branch `codex/pt104-runtime-source-harness`) is actively useful. The other 4 non-main worktrees are historical and can be cleaned up later.

---

## Optimization Candidates

No optimization candidates recorded yet.

Use this section only for bounded work that:

- starts from an existing working feature,
- has a metric or explicit quality target,
- and is not just ordinary new feature development in disguise.
