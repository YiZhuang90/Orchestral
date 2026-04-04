# Orchestral Strategic Roadmap V2

## 1. Vision

Read [PROJECT_VISION.md](./PROJECT_VISION.md) for why Orchestral exists and what makes it fundamentally different from LabVIEW and traditional lab automation.

This roadmap is how we get there.

---

## 2. System Architecture — Two-Half Model

Orchestral has two halves that work together:

```
ORCHESTRAL = PLATFORM + AGENT

Platform Stack (what runs experiments):
  L4  Experiment Canvas         (design-time authoring scaffold)
  L3  Experiment Logic          (experiment-specific scientific meaning)
  L2  Universal Runtime         (sessions, coordination, streams, monitoring)
  L1  Hardware / Adapters       (SDK wrappers, protocol clients)

Agent Stack (what helps build, operate, and learn):
  A3  Generation                (experiment drafts, reports, analysis outputs)
  A2  Guidance                  (device integration, experiment design, troubleshooting)
  A1  Observation & Knowledge   (decision capture, metadata, lab knowledge, calibration)

Cross-cutting Foundations:
  Data & Artifact Foundation    (experiment skeleton, storage formats, AI data contract)
  Knowledge & Reporting Foundation (decision logs, reports, lab knowledge hub, audit trail)
```

### Platform Ownership

| Layer | Owns | Does NOT Own |
|-------|------|-------------|
| L1 Hardware / Adapters | SDK wrappers, serial/protocol clients, OS capture backends | Runtime state, experiment meaning, UI |
| L2 Universal Runtime | Device sessions, registry, coordinator, stop authority, stream/snapshot ports, recorder, controller infrastructure, monitor infrastructure, validation, replay seams | Experiment-specific scientific logic |
| L3 Experiment Logic | Derived scientific state, transforms, detectors, composite role meaning, experiment-specific control/monitor policies | Raw device APIs, generic session lifecycle |
| L4 Experiment Canvas | High-level authoring: Function -> Role -> Implementation. Readiness indicators. Design-time binding. | Runtime execution. Canvas is NOT a runtime partition. |

### Agent Ownership

| Layer | Owns | Does NOT Own |
|-------|------|-------------|
| A1 Observation & Knowledge | Decision logging, metadata collection, calibration tracking, lab knowledge capture, protocol library | Direct hardware control, runtime execution |
| A2 Guidance | Device integration guidance, experiment design help, troubleshooting assistance, setup workflows | Unilateral runtime control. AI proposes, human approves. |
| A3 Generation | Experiment definition drafts, daily/weekly reports, analysis outputs, audit trail generation | Final approval. All generated artifacts require human review gate. |

### How The Two Halves Connect

- The agent OBSERVES the platform's data foundation (runtime outputs, manifests, metadata).
- The agent WRITES TO the knowledge foundation (decisions, protocols, reports).
- The platform CONSUMES agent-generated artifacts (experiment definitions, configurations).
- The agent never directly controls the platform runtime without human approval.

### Boundary Rules

1. Experiment logic consumes runtime outputs, never raw device APIs.
2. The canvas is a design-time scaffold, not a runtime execution container.
3. Turbulence is a reference case and validation target, not the architecture.
4. The agent proposes, the human approves — always.
5. Data structures and knowledge capture must be designed early, not bolted on after.
6. Agent execution tracks come after the platform execution frontier (L1-L3 stable first).

---

## 3. What's Done

| Layer | Status | Key Landed Work |
|-------|--------|----------------|
| L1 Hardware / Adapters | Sufficient for first reference experiment | HuaTeng camera SDK, PT-104 driver, control-center serial client, integrated mic/camera OS backends |
| L2 Universal Runtime | First generation substantially complete | 5 device sessions, registry, coordinator + stop authority, validation (session-local, cross-session, definition linting), experiment definition + role binding, stream buffering, run context + metadata, recorder + artifact writer, controller-unit session, monitor + alarm surface, control-center capability decomposition, PT-104 replay/synthetic proof |
| L3 Experiment Logic | Documented, not yet real in code | Architecture doc exists. FlowReynoldsDerivedStateSession is first bridge unit but has no explicit code-level home |
| L4 Experiment Canvas | Fully documented, no code | Canvas architecture, block contract, typed block kinds, system language spec |
| A1-A3 Agent Stack | Not formally started | The agent IS already active as Claude Code + Superpowers skills, but no Orchestral-specific agent architecture exists yet |
| Data Foundation | Partial | First-gen recorder + artifact writer, run manifest schema. No standard experiment skeleton or storage format decisions |
| Knowledge Foundation | Not started | Vision defined. No implementation |

For detailed landing evidence, see [2026-04-03-landing-audit.md](./2026-04-03-landing-audit.md).

---

## 4. Execution Frontier — Next Tracks

### Track EF-01: Land the PT-104 Runtime-Source Harness

- Domain: Platform L2
- What: Merge the existing reviewed branch (`codex/pt104-runtime-source-harness`) into the shared codebase.
- Depends on: Nothing.
- Git note: The branch is like a "reviewed draft chapter" sitting in a separate workspace. Landing = adding it to the "official book" (main branch). This requires a separate landing thread.
- Result: Replay/synthetic proof available in shared codebase.

### Track EF-02: Define the Experiment Data Skeleton

- Domain: Data & Artifact Foundation
- What: Standard file/folder structure for experiment data. Storage format decisions. AI data access contract.
- Depends on: EF-01 landed.
- Result: Written standard the recorder follows and future AI can consume.

### Track EF-03: Establish the Experiment-Logic Code Boundary

- Domain: Platform L3
- What: Explicit code-level home for experiment logic. Relocate FlowReynoldsDerivedStateSession. Integration proof.
- Depends on: EF-01 landed.
- Note: EF-02 and EF-03 are independent — they can be parallel or in either order after EF-01.
- Result: The L2/L3 boundary is real in code, not just in docs.

### Track EF-04: First Reference Experiment Package (Minimum)

- Domain: Platform L3 + Data Foundation
- What: Minimum experiment package using turbulence reference case. Uses L1-L3, follows data skeleton, produces real run manifest.
- Depends on: EF-02 and EF-03.
- Result: One real experiment can be authored, validated, run, and recorded with proper data structure.

### Track EF-05: Agent Architecture Detailed Design

- Domain: Agent Stack
- What: Detailed agent architecture beyond the sketch. Decide: build custom agent, extend existing tooling, or hybrid. Define agent capability contracts, knowledge storage, observation model.
- Depends on: EF-04 (agent needs a working platform to observe and assist).
- Starting point: [AGENT_ARCHITECTURE_SKETCH.md](./AGENT_ARCHITECTURE_SKETCH.md).
- Result: Detailed agent design ready for implementation.

---

## 5. Later Horizons

These should only be detailed when the execution frontier advances to them.

| Horizon | Domain | Examples |
|---------|--------|----------|
| A: Experiment-Logic Expansion | Platform L3 | Camera-pair role, image-to-signal pipeline, detection/classification |
| B: Canvas Code Implementation | Platform L4 | Canvas model in code, function block authoring, role binding UI, readiness indicators |
| C: Agent Foundation Build | Agent A1-A3 | First agent implementation, observation pipeline, knowledge storage |
| D: Data & Reporting Automation | Cross-cutting | Auto metadata logging, daily dream process, auto reports, calibration tracking |
| E: Knowledge Hub Foundation | Cross-cutting | Protocol library, device experience capture, lab-wide deployment model |
| F: Hardening & Generalization | Platform L2-L3 | Broader virtual twins, failure tests, multi-experiment, plugin boundaries |

---

## 6. Boundary Rules and Anti-Drift Guardrails

These rules exist to prevent the drift patterns identified from V1 experience:

1. **No coding without a roadmap position.** Every coding slice must map to an execution track. If it doesn't map, it needs a planning thread first.
2. **No experiment-specific logic in universal runtime.** If a unit depends on one experiment's scientific meaning, it belongs in L3, not L2.
3. **No runtime controls in the canvas.** Canvas = authoring. Start/stop/monitor belong to the runtime and operator surfaces.
4. **Agent proposes, human approves.** No unilateral agent actions on runtime or data.
5. **Turbulence is a reference case, not the architecture.** Decisions should be judged by generality first.
6. **Data structure early, not late.** The experiment data skeleton (EF-02) is a prerequisite for the first reference experiment, not a cleanup task after.
7. **One coding thread = one mergeable slice.** No stacking multiple slices on one long-lived thread.
8. **Planning and coding don't mix in one thread.** Planning threads shape the queue. Coding threads execute one item.
9. **New capabilities grow by addition.** Adding a data-processing cell, a new device type, or a new report format should be possible by adding a new block/session/unit — not by restructuring the existing system. If a new feature requires restructuring, that's a design smell.
10. **Agent design comes after platform stability.** Don't build the agent on shaky foundations.

---

## 7. Development Workflow — Superpowers Skill Integration

Orchestral uses the Superpowers skill set as its standard development workflow.

### Thread-to-Skill Mapping

| Orchestral Thread Type | Superpowers Skill Chain | When |
|---|---|---|
| Planning thread | `$brainstorming` -> `$writing-plans` | Roadmap resets, architecture decisions, track decomposition |
| Coding thread | `$using-git-worktrees` -> `$subagent-driven-development` -> `$requesting-code-review` -> `$finishing-a-development-branch` | One mergeable implementation slice |
| Debug thread | `$systematic-debugging` -> `$dispatching-parallel-agents` (if multi-bug) | Root cause unclear, failure scope wider than one edit |
| Landing thread | `$finishing-a-development-branch` | Merge decision, final verification, cleanup |

### Embedded Disciplines (All Coding Threads)

- **Test-Driven Development** (`$test-driven-development`): Write failing test first, then implement. Every feature, bugfix, and refactor follows RED -> GREEN -> REFACTOR. No code before test.
- **Verification Before Completion** (`$verification-before-completion`): Run verification fresh before claiming any task is done. No "should work" or "probably fixed."
- **Systematic Debugging** (`$systematic-debugging`): When a bug appears, follow the 4-phase process (investigate -> pattern analysis -> hypothesis testing -> implementation). Max 3 fix attempts before questioning assumptions.

### Recommended Execution Model

Subagent-driven development. Each task from the plan is dispatched to a fresh subagent. Each subagent implements using TDD. Each task goes through a 2-stage review gate (spec compliance + code quality). The main thread coordinates and preserves context.

### Worktree Discipline

Every coding slice should use a git worktree for isolation. In plain language: a worktree is like having a second copy of the project open on a separate desk, so your experiments on one desk don't mess up the clean copy on the other. When the work is done, it gets merged back.

### Connection to Cross-Thread Workflow

- A planning thread uses `$brainstorming` to explore and `$writing-plans` to produce execution plans. Those plans feed directly into waitlist items.
- A coding thread picks one `Ready For Coding` item from the [waitlist](./CROSS_THREAD_DEV_WAITLIST.md), creates a worktree, and runs through the subagent-driven chain.
- Review artifacts from `$requesting-code-review` map to the [review protocol](../collaboration/REVIEW_PROTOCOL.md).
- `$finishing-a-development-branch` maps to the landing step in [MODULE_DEVELOPMENT_WORKFLOW.md](./MODULE_DEVELOPMENT_WORKFLOW.md).

**Anti-pattern:** Never skip skills because a task "seems simple." The skills enforce quality gates that prevent the drift and premature-detail problems identified in V1 experience.

---

## 8. What Happened to Old Docs

- **ROADMAP_V1.md** (`docs/development/`): Superseded by this document. Kept for historical reference.
- **2026-03-28-functional-unit-roadmap.md** (`docs/development/`): Superseded by this document. Its unit inventory was useful historically but is now mostly obsolete.
- **V1_EXECUTION_TRACK.md**: Remains valid as the operational execution index. Now references this document instead of ROADMAP_V1.
- **docs/development/**: Archived. Active development docs live in `docs/development_v2/`.
