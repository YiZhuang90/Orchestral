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

- `PL-004`
  - title: `Agent P0 Skeleton slice planning`
  - thread type: `planning`
  - priority: `P2`
  - layer: `agent stack (A1-A3)`
  - depends on: `none (P0 has no platform dependency)`
  - governing docs:
    - [AGENT_SIDECAR_BLUEPRINT.md](./AGENT_SIDECAR_BLUEPRINT.md) — Phase 0 section
  - handoff / plan: `none yet`
  - branch / worktree: `not started`
  - current truth: `blueprint defines P0 scope: ExperimentAssistant + AIChatPanel + basic system prompt. No plugins, no knowledge. Just prove the chat loop works inside the WPF app.`
  - next action: `open a planning session to produce a detailed slice plan for P0`

- `PL-005`
  - title: `Agent P1 Researcher slice planning`
  - thread type: `planning`
  - priority: `P2`
  - layer: `agent stack (A2R)`
  - depends on: `PL-004 (P0 must be planned first)`
  - governing docs:
    - [AGENT_SIDECAR_BLUEPRINT.md](./AGENT_SIDECAR_BLUEPRINT.md) — Phase 1 section
  - handoff / plan: `none yet`
  - branch / worktree: `not started`
  - current truth: `blueprint defines P1 scope: ResearchPlugin with search_web, fetch_page, search_vendor_docs. Agent can find hardware docs, SDKs, literature.`
  - next action: `plan after P0 is planned`

Note: Agent phases P2-P6 exist in the blueprint but are not added to the waitlist yet. They should be added as the frontier advances.

---

## Planned But Not Ready

No items currently in this state.

---

## Ready For Coding

- `SL-003`
  - title: `Define the standard Orchestral project skeleton`
  - thread type: `coding`
  - priority: `P1`
  - layer: `data and artifact foundation (cross-cutting)`
  - depends on: `none`
  - governing docs:
    - [ROADMAP_V2.md](./ROADMAP_V2.md)
    - [PROJECT_VISION.md](./PROJECT_VISION.md) — Principle 3: structured data from initial skeleton
    - [AGENT_SIDECAR_BLUEPRINT.md](./AGENT_SIDECAR_BLUEPRINT.md) — knowledge wiki structure (K1-K10)
  - handoff / plan: [2026-04-09-SL-003-project-skeleton-slice-plan.md](./2026-04-09-SL-003-project-skeleton-slice-plan.md)
  - branch / worktree: `not started`
  - current truth: `defines the standard folder/file structure for a new Orchestral project from creation. Covers: project manifest, experiment definitions, device library, knowledge wiki layout, reports, calibration, config. Parallel with SL-002.`
  - next action: `open a coding session with $orch-session-coding`

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

- `SL-002`
  - title: `Define the experiment data skeleton (EF-02)`
  - thread type: `landing`
  - priority: `P1`
  - layer: `data and artifact foundation (cross-cutting)`
  - depends on: `LD-001 landed` *(satisfied 2026-04-04)*
  - governing docs:
    - [ROADMAP_V2.md](./ROADMAP_V2.md) — EF-02
    - [TECH_STACK_DECISIONS.md](./TECH_STACK_DECISIONS.md) — Section 5
    - [PROJECT_VISION.md](./PROJECT_VISION.md) — Principles 3, 4, 8
    - New spec: `docs/architecture/EXPERIMENT_DATA_SKELETON.md`
  - handoff / plan:
    - slice plan: [2026-04-04-SL-002-experiment-data-skeleton-slice-plan.md](./2026-04-04-SL-002-experiment-data-skeleton-slice-plan.md)
    - handoff: [handoffs/2026-04-09-SL-002-handoff.md](./handoffs/2026-04-09-SL-002-handoff.md)
    - review: [2026-04-09-SL-002-experiment-data-skeleton-review.md](../collaboration/reviews/2026-04-09-SL-002-experiment-data-skeleton-review.md) *(on sl-002 branch)*
  - branch / worktree: `sl-002/experiment-data-skeleton @ baa2b56 (worktree: Orchestral-sl-002)`
  - current truth: |
    Coding complete 2026-04-09. Branch `sl-002/experiment-data-skeleton` committed at `baa2b56` (4 files, +791/-6).
    - New spec: `docs/architecture/EXPERIMENT_DATA_SKELETON.md` (245 lines, 9 sections, includes consumer contract)
    - `RunArtifactWriter` reorganized into `events/`, `snapshots/`, `metadata/`, `data/derived/` subdirectories
    - New `metadata/applied-parameters.yaml` written for every run
    - 5 new structure-verification tests added
    - Build clean (0 warnings, 0 errors), 187 tests pass (52 Core + 79 Runtime + 14 Devices + 11 ExperimentLogic + 31 App)
    - Review: pass with 3 non-blocking P3 findings (CC-SL002-001 addressed in same commit; CC-SL002-002 and CC-SL002-003 deferred as non-blocking cleanup)
    - Branch not yet merged into main, not yet pushed
  - next action: `open a landing session to merge sl-002/experiment-data-skeleton into main, verify, push, and cleanup the worktree`

---

## Landed

- `SL-001`
  - title: `Establish the experiment-logic code boundary (EF-03)`
  - thread type: `landed`
  - priority: `P1 — completed`
  - layer: `experiment logic (L3)`
  - depends on: `LD-001 landed` *(satisfied 2026-04-04)*
  - governing docs:
    - [ROADMAP_V2.md](./ROADMAP_V2.md)
    - [EXPERIMENT_LOGIC_LAYER.md](../architecture/EXPERIMENT_LOGIC_LAYER.md)
  - handoff / plan: [2026-04-04-SL-001-experiment-logic-boundary-slice-plan.md](./2026-04-04-SL-001-experiment-logic-boundary-slice-plan.md)
  - branch / worktree: `branch deleted, worktree pending cleanup (Windows file lock)`
  - current truth: |
    Landed 2026-04-06. Merged to main as e852d95 (merge commit). 19 files changed, +376/-36.
    Build clean, 182 tests pass (52 Core, 79 Runtime, 14 Devices, 11 ExperimentLogic, 26 App).
    Key changes:
    - New project: ExperimentalControlPlatform.ExperimentLogic (L3)
    - New test project: ExperimentalControlPlatform.ExperimentLogic.Tests
    - New interfaces in Runtime: IDerivedStateSession, IDerivedStateSnapshot
    - FlowReynoldsDerivedState* relocated from Runtime to ExperimentLogic
    - ExperimentMonitorSession uses interfaces (Runtime has zero ExperimentLogic refs)
    - Review: pass with 2 non-blocking P2 findings (Devices dependency, interface generality)
  - next action: `none — completed`

- `LD-003`
  - title: `Commit and push planning session docs (Deliverables 11-17, workflows, blueprint updates)`
  - thread type: `landed`
  - priority: `P0 — completed`
  - layer: `project operations`
  - depends on: `none`
  - governing docs:
    - [ROADMAP_V2.md](./ROADMAP_V2.md)
    - [MODULE_LANDING_WORKFLOW.md](./MODULE_LANDING_WORKFLOW.md)
  - handoff / plan: `none — self-contained landing task`
  - branch / worktree: `local main checkout`
  - current truth: `landed 2026-04-06. Committed as c15602d (15 files, +1241/-77), pushed. Build clean, 177 tests pass.`
  - next action: `none — completed`

- `PL-003`
  - title: `Agent sidecar architecture design (EF-05)`
  - thread type: `landed`
  - priority: `P1 — completed`
  - layer: `agent stack (A1-A3)`
  - depends on: `none`
  - governing docs:
    - [ROADMAP_V2.md](./ROADMAP_V2.md)
    - [AGENT_ARCHITECTURE_SKETCH.md](./AGENT_ARCHITECTURE_SKETCH.md)
    - [AI_INTEGRATION_PLAN.md](./AI_INTEGRATION_PLAN.md)
  - handoff / plan: [AGENT_SIDECAR_BLUEPRINT.md](./AGENT_SIDECAR_BLUEPRINT.md)
  - branch / worktree: `n/a — planning-only, design doc`
  - current truth: |
    Completed 2026-04-05. Full agent sidecar blueprint written. Key decisions:
    - Framework: Microsoft Semantic Kernel (C#/.NET)
    - 4 agent layers: A1 (Observation), A2 (Guidance), A2R (Research), A3 (Generation)
    - Knowledge: Karpathy LLM Wiki pattern, 3-layer (Raw/Wiki/Schema), 10 categories (K1-K10)
    - Anti-database for failures and lessons learned
    - 6 build-use-refine phases (P0-P6)
    - P0-P1 can start immediately (no platform dependency)
    - Turbulence experiment as end-to-end acceptance test
  - next action: `none — design complete. Implementation phases (P0-P6) are future coding slices.`

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

As of 2026-04-06, local main is synced with origin/main at `c15602d`. History was rewritten via git-filter-repo on 2026-04-04 to purge legacy/ from all commits. Two historical stashes remain.

### Worktree inventory

5 worktrees remain (main + 4 historical). The `runtime-io-microphone` worktree was removed during LD-001 landing. The 4 historical worktrees (design-v2-landing, orchestral-design-v2-migration, panel-contract-enforcement, runtime-io-doc-push) carry no unique work and can be cleaned up. Git worktree metadata pruning is blocked by Windows file locks; retry when no other process holds the `.git/worktrees/` directory.

---

## Optimization Candidates

No optimization candidates recorded yet.

Use this section only for bounded work that:

- starts from an existing working feature,
- has a metric or explicit quality target,
- and is not just ordinary new feature development in disguise.
