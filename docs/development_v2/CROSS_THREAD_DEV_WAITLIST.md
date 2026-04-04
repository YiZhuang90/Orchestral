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
  - depends on: `LD-001 landed` *(satisfied 2026-04-04)*
  - governing docs:
    - [ROADMAP_V2.md](./ROADMAP_V2.md)
    - [EXPERIMENT_LOGIC_LAYER.md](../architecture/EXPERIMENT_LOGIC_LAYER.md)
  - handoff / plan: `none yet — needs a planning follow-up to produce a slice plan`
  - branch / worktree: `not started`
  - current truth: `confirmed as EF-03 by ROADMAP_V2. LD-001 dependency satisfied.`
  - next action: `open a planning session to reduce this to a ready coding slice`

- `SL-002`
  - title: `Define the experiment data skeleton (EF-02)`
  - thread type: `coding`
  - priority: `P1`
  - layer: `data and artifact foundation (cross-cutting)`
  - depends on: `LD-001 landed` *(satisfied 2026-04-04)*
  - governing docs:
    - [ROADMAP_V2.md](./ROADMAP_V2.md)
    - [TECH_STACK_DECISIONS.md](./TECH_STACK_DECISIONS.md)
  - handoff / plan: `none yet — needs a planning follow-up to produce a slice plan`
  - branch / worktree: `not started`
  - current truth: `confirmed as EF-02 by ROADMAP_V2. Independent of SL-001. LD-001 dependency satisfied.`
  - next action: `open a planning session to produce a data-skeleton spec`

---

## Ready For Coding

No items currently ready.

LD-001 has landed (2026-04-04). SL-001 and SL-002 are now unblocked but still need a planning follow-up to produce slice plans before they can move here.

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

No items currently ready to land.

---

## Landed

- `LD-001`
  - title: `PT-104 runtime-source harness`
  - thread type: `landed`
  - priority: `P1 — completed`
  - layer: `universal runtime foundation`
  - depends on: `none`
  - governing docs:
    - [MODULE_DEVELOPMENT_WORKFLOW.md](./MODULE_DEVELOPMENT_WORKFLOW.md)
  - handoff / plan:
    - `plan: docs/development/2026-04-02-pt104-runtime-source-harness-plan.md`
    - `review: docs/collaboration/reviews/2026-04-02-pt104-runtime-source-harness-review-claude-opus-4-6.md`
    - `response: docs/collaboration/reviews/2026-04-02-pt104-runtime-source-harness-response.md`
  - branch / worktree: `branch deleted, worktree removed`
  - current truth: `landed 2026-04-04. Merged to main as 75a1df2 (merge commit). 13 files, +418/-18. Build clean, 177 tests pass (85 runtime, 52 core, 26 app, 14 devices). SL-001 and SL-002 are now unblocked.`
  - next action: `none — completed`

- `LD-002`
  - title: `Sync local main, commit and push PL-001 docs`
  - thread type: `landed`
  - priority: `P0 — completed`
  - layer: `project operations`
  - depends on: `none`
  - governing docs:
    - [ROADMAP_V2.md](./ROADMAP_V2.md)
    - [2026-04-03-landing-audit.md](./2026-04-03-landing-audit.md)
  - handoff / plan: `none — self-contained landing task`
  - branch / worktree: `local main checkout`
  - current truth: `landed 2026-04-04. Local main synced to origin/main (be0529f), PL-001 docs committed as 04af9ab (51 files, 12929 insertions), pushed. Build and 175 tests pass.`
  - next action: `none — completed`

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
- `codex/pt104-runtime-source-harness` — landed as LD-001 (merge commit `75a1df2`), branch deleted

Corresponding worktrees for several of these branches still exist on disk. They can be cleaned up whenever convenient but are not blocking anything.

Audit reference: [2026-04-03-landing-audit.md](./2026-04-03-landing-audit.md)

---

## Operational Notes

### Local main checkout

As of 2026-04-04, local main is synced with origin/main at `04af9ab` (30 commits). The prior divergence (local `af36d25` vs origin `be0529f`) was resolved during LD-002 landing. Two historical stashes remain (`codex-temp-before-runtime-io-push` and a WIP on `779fa4d`).

### Worktree inventory

5 worktrees remain (main + 4 historical). The `runtime-io-microphone` worktree was removed during LD-001 landing. The 4 historical worktrees (design-v2-landing, orchestral-design-v2-migration, panel-contract-enforcement, runtime-io-doc-push) carry no unique work and can be cleaned up. Git worktree metadata pruning is blocked by Windows file locks; retry when no other process holds the `.git/worktrees/` directory.

---

## Optimization Candidates

No optimization candidates recorded yet.

Use this section only for bounded work that:

- starts from an existing working feature,
- has a metric or explicit quality target,
- and is not just ordinary new feature development in disguise.
