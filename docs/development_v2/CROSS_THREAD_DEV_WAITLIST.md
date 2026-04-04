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

No items currently need planning.

PL-001 and PL-002 are completed and moved to Landed.

---

## Planned But Not Ready

- `SL-001`
  - title: `Establish the experiment-logic code boundary (EF-03)`
  - thread type: `coding`
  - priority: `P1`
  - layer: `experiment logic (L3)`
  - depends on: `LD-001 landed`
  - governing docs:
    - [ROADMAP_V2.md](./ROADMAP_V2.md)
    - [EXPERIMENT_LOGIC_LAYER.md](../architecture/EXPERIMENT_LOGIC_LAYER.md)
  - handoff / plan: `none yet — needs a planning follow-up to produce a slice plan`
  - branch / worktree: `not started`
  - current truth: `confirmed as EF-03 by ROADMAP_V2. Depends on EF-01 (LD-001) landing first.`
  - next action: `land LD-001, then open a planning follow-up to reduce this to a ready coding slice`

- `SL-002`
  - title: `Define the experiment data skeleton (EF-02)`
  - thread type: `coding`
  - priority: `P1`
  - layer: `data and artifact foundation (cross-cutting)`
  - depends on: `LD-001 landed`
  - governing docs:
    - [ROADMAP_V2.md](./ROADMAP_V2.md)
    - [TECH_STACK_DECISIONS.md](./TECH_STACK_DECISIONS.md)
  - handoff / plan: `none yet — needs a planning follow-up to produce a slice plan`
  - branch / worktree: `not started`
  - current truth: `confirmed as EF-02 by ROADMAP_V2. Independent of SL-001. Both depend on LD-001.`
  - next action: `land LD-001, then open a planning follow-up to produce a data-skeleton spec`

---

## Ready For Coding

No items currently ready.

That is intentional and truthful. ROADMAP_V2 has landed, but LD-001 (PT-104 harness) must be landed first before SL-001 or SL-002 can become ready. After LD-001 lands, a short planning follow-up should move one or both of SL-001/SL-002 to Ready For Coding.

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

- `LD-002`
  - title: `Sync local main, commit and push PL-001 docs`
  - thread type: `landing`
  - priority: `P0`
  - layer: `project operations`
  - depends on: `none`
  - governing docs:
    - [ROADMAP_V2.md](./ROADMAP_V2.md)
    - [2026-04-03-landing-audit.md](./2026-04-03-landing-audit.md)
  - handoff / plan: `none — self-contained landing task`
  - branch / worktree: `local main checkout`
  - current truth: |
    Local main is at `af36d25` (8 commits), origin/main is at `be0529f` (29 commits). They diverged.
    Local commit `af36d25` duplicates content already on origin/main as `01e4162`.
    All PL-001 docs (development_v2/, superseded headers, etc.) are uncommitted in the local checkout.
    Two stashes also exist from earlier work.
    Steps needed: stash uncommitted work, reconcile the diverged local commit, pull origin/main, restore work, commit, push.
  - next action: `open a landing thread to sync local main and push PL-001 docs`

- `LD-001`
  - title: `PT-104 runtime-source harness`
  - thread type: `landing`
  - priority: `P1`
  - layer: `universal runtime foundation`
  - depends on: `none`
  - governing docs:
    - [MODULE_DEVELOPMENT_WORKFLOW.md](./MODULE_DEVELOPMENT_WORKFLOW.md)
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

- `PL-001`
  - title: `Strategic roadmap re-baseline and ROADMAP_V2`
  - thread type: `landed`
  - priority: `P0 — completed`
  - layer: `project strategy`
  - depends on: `none`
  - governing docs:
    - [ROADMAP_V2.md](./ROADMAP_V2.md)
    - [PROJECT_VISION.md](./PROJECT_VISION.md)
    - [AGENT_ARCHITECTURE_SKETCH.md](./AGENT_ARCHITECTURE_SKETCH.md)
  - handoff / plan: `docs/development_v2/ — all deliverables written`
  - branch / worktree: `n/a — planning-only, docs written to main checkout`
  - current truth: `completed 2026-04-04. ROADMAP_V2 written, vision doc written, agent sketch written, waitlist updated, old docs marked superseded, active files migrated to development_v2/`
  - next action: `none — completed`

- `PL-002`
  - title: `Landing audit and queue normalization`
  - thread type: `landed`
  - priority: `P0 — completed`
  - layer: `project operations`
  - depends on: `none`
  - governing docs:
    - [THREAD_CONTEXT_MANAGEMENT_CONTRACT.md](./THREAD_CONTEXT_MANAGEMENT_CONTRACT.md)
  - handoff / plan: [2026-04-03-landing-audit.md](./2026-04-03-landing-audit.md)
  - branch / worktree: `n/a — planning-only task`
  - current truth: `audit completed 2026-04-03; all slices classified against Git evidence; waitlist normalized`
  - next action: `none — completed`

- `LD-000`
  - title: `V2 design + runtime foundation + architecture docs baseline`
  - thread type: `landed`
  - priority: `historical`
  - layer: `shared foundation`
  - depends on: `none`
  - governing docs:
    - [V1_EXECUTION_TRACK.md](./V1_EXECUTION_TRACK.md)
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

Audit reference: [2026-04-03-landing-audit.md](./2026-04-03-landing-audit.md)

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
