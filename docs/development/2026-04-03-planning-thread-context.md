# Planning Thread Context

## Purpose

This file exists to initialize a **new planning-only thread** for Orchestral.

The next thread should focus on:

- strategic re-baselining,
- architecture/layering clarity,
- roadmap restructuring,
- and execution-track decomposition.

It should **not** be used to start implementation directly.

---

## Recommended Thread Intent

Use the new thread to answer:

1. what the canonical top-to-bottom system structure is now,
2. which old roadmap assumptions are stale,
3. what the replacement strategic roadmap should be,
4. how the next execution tracks should be decomposed.

Recommended skill sequence for that new thread:

1. `$brainstorming`
2. `$writing-plans`
3. `$plan-eng-review`
4. optionally `$autoplan`

Do **not** start with `$orch-module-dev` in that thread unless the work has already been reduced to one bounded implementation slice.
Do **not** start with `cross-thread-coding` in that thread either.
That skill is for picking up an already-ready coding item from the queue, not for reshaping the queue itself.

---

## Strategic Goal Of The Project

Orchestral should be planned as a **general experimental control platform**.

Important clarification:

- the goal is **not** to rebuild the turbulence experiment literally,
- the turbulence experiment is a **reference case / inspiration / validation target**,
- architecture decisions should therefore be judged by generality first, not by turbulence-specific convenience.

---

## Current Repo Reality

There are currently **two important local truths**:

### 1. Dirty local main checkout

Path:

- `C:\Users\Yi Zhuang\OneDrive\Codes\Projects\Orchestral`

Important note:

- this checkout is dirty and diverged from `origin/main`
- treat it carefully
- do not assume its files are the freshest architectural truth

### 2. Fresher development worktree

Path:

- `C:\Users\Yi Zhuang\.config\superpowers\worktrees\Orchestral\runtime-io-microphone`

This worktree contains the latest architectural truth used in recent slices.

Relevant git points:

- `origin/main` currently points to commit `be0529f`
- latest additional branch proof slice:
  - branch `codex/pt104-runtime-source-harness`
  - commit `7465325`

Meaning:

- `origin/main` already includes the large runtime/canvas/experiment-logic documentation baseline
- the PT-104 runtime-source harness proof is newer and lives on its own small branch

---

## Canonical Layering To Start From

The current best system layering is:

1. `Hardware / Adapters`
2. `Universal Runtime Foundation`
3. `Experiment Logic`
4. `Experiment Canvas`
5. `AI Orchestration`

Interpretation:

- `Hardware / Adapters`
  - SDK wrappers
  - serial/protocol clients
  - OS/device backends
- `Universal Runtime Foundation`
  - reusable sessions
  - registry
  - coordinator
  - stop authority
  - stream/snapshot ports
  - recorder
  - controller infrastructure
  - monitor infrastructure
  - validation and linting
  - replay/simulation proof seams
- `Experiment Logic`
  - experiment-specific derived state
  - transforms
  - detectors
  - control policies
  - experiment-specific monitor logic
- `Experiment Canvas`
  - high-level authoring/design surface
  - `Experiment Function -> Experiment Role -> Concrete Implementation`
  - design-time scaffold, **not** a runtime execution partition
- `AI Orchestration`
  - guidance, planning, setup help, future automation
  - intentionally deferred until the lower layers are real and stable

Two critical rules already established:

1. the canvas is a **design-time/system-thinking scaffold**, not a hard runtime container model
2. turbulence is an **example mapping**, not the architectural source of truth

---

## What Is Already Built Enough

As of this planning handoff, the universal runtime foundation is no longer the main missing piece.

Recent built/shared units include:

- device session foundation
- session registry
- runtime coordinator and stop authority
- session-local validation
- cross-session validation
- experiment-definition linting
- experiment definition and role binding
- high-rate stream buffering and delivery policy
- run context and metadata
- run recorder and artifact writer
- controller-unit session
- run monitor and alarm surface
- flow/Reynolds derived-state wiring
- control-center capability decomposition
- first replay/simulation proof:
  - PT-104 runtime-source seam
  - replay broadcaster
  - synthetic broadcaster
  - hardware-free experiment-plane scalar proof

This means:

- the runtime layer is now **substantially real**
- the strategic frontier has shifted upward

---

## What Is Now The Active Frontier

The next strategic focus should not be another vague round of “more runtime.”

The active frontier is:

1. **re-baseline the roadmap**
2. **give Experiment Logic a real code-level roadmap position**
3. **connect Experiment Canvas thinking to future execution/binding**
4. **define the next execution tracks after runtime foundation**

The most likely next execution frontier after roadmap cleanup is:

- the **minimum experiment-logic proof**
- using the already-built Reynolds derived-state unit as the first real boundary test

---

## Newer Docs That Represent Current Truth

Use these as the primary architectural inputs for the new planning thread.

From the fresher worktree:

- `C:\Users\Yi Zhuang\.config\superpowers\worktrees\Orchestral\runtime-io-microphone\docs\development\V1_EXECUTION_TRACK.md`
- `C:\Users\Yi Zhuang\.config\superpowers\worktrees\Orchestral\runtime-io-microphone\docs\architecture\DEVICE_RUNTIME_IO_ARCHITECTURE.md`
- `C:\Users\Yi Zhuang\.config\superpowers\worktrees\Orchestral\runtime-io-microphone\docs\architecture\EXPERIMENT_LOGIC_LAYER.md`
- `C:\Users\Yi Zhuang\.config\superpowers\worktrees\Orchestral\runtime-io-microphone\docs\architecture\EXPERIMENT_CANVAS_ARCHITECTURE.md`
- `C:\Users\Yi Zhuang\.config\superpowers\worktrees\Orchestral\runtime-io-microphone\docs\architecture\EXPERIMENT_CANVAS_BLOCK_CONTRACT.md`
- `C:\Users\Yi Zhuang\.config\superpowers\worktrees\Orchestral\runtime-io-microphone\docs\architecture\EXPERIMENT_CANVAS_TYPED_BLOCK_KINDS.md`
- `C:\Users\Yi Zhuang\.config\superpowers\worktrees\Orchestral\runtime-io-microphone\docs\architecture\SYSTEM_LANGUAGE_SPEC.md`
- `C:\Users\Yi Zhuang\.config\superpowers\worktrees\Orchestral\runtime-io-microphone\docs\architecture\VIRTUAL_TWIN_AND_SIMULATION_STRATEGY.md`
- `C:\Users\Yi Zhuang\.config\superpowers\worktrees\Orchestral\runtime-io-microphone\docs\development\2026-03-31-universal-completion-and-experiment-logic-proof-plan.md`

Also relevant from the main checkout:

- `C:\Users\Yi Zhuang\OneDrive\Codes\Projects\Orchestral\docs\development\MODULE_DEVELOPMENT_WORKFLOW.md`
- `C:\Users\Yi Zhuang\OneDrive\Codes\Projects\Orchestral\docs\development\THREAD_CONTEXT_MANAGEMENT_CONTRACT.md`
- `C:\Users\Yi Zhuang\OneDrive\Codes\Projects\Orchestral\docs\development\CROSS_THREAD_DEV_WAITLIST.md`
- `C:\Users\Yi Zhuang\OneDrive\Codes\Projects\Orchestral\docs\development\CROSS_THREAD_CODING_WORKFLOW.md`

---

## Older Docs Suspected To Be Stale

These should be treated as **inputs to review**, not as current truth:

- `C:\Users\Yi Zhuang\OneDrive\Codes\Projects\Orchestral\docs\development\2026-03-28-functional-unit-roadmap.md`
- `C:\Users\Yi Zhuang\OneDrive\Codes\Projects\Orchestral\docs\development\ROADMAP_V1.md`

Why they are suspected stale:

- they predate much of the landed runtime foundation
- they predate the current Experiment Logic / Experiment Canvas framing
- they still over-center turbulence as the V1 organizing lens
- they do not fully reflect the newer source-mode and typed-block model

---

## What The New Planning Thread Should Produce

The desired outputs are:

1. a **clear restatement of the canonical layering**
2. a **strategic roadmap replacement**, likely `ROADMAP_V2.md`
3. a recommendation for what to do with:
   - `2026-03-28-functional-unit-roadmap.md`
   - `ROADMAP_V1.md`
4. a decomposition of the next major execution tracks
5. a planning-ready statement of what the next non-doc implementation frontier should be
6. a refreshed waitlist with at least one truthful `Ready For Coding` item or an explicit reason why none should exist yet
7. a one-time landing audit of recent slices so landed vs ready-to-land truth is written down instead of assumed

Recommended shape:

- one strategic roadmap doc
- one architecture-level "system stack / layering" clarification if needed
- then execution decomposition

The planning thread should also update:

- [CROSS_THREAD_DEV_WAITLIST.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/development/CROSS_THREAD_DEV_WAITLIST.md)

so that the next coding thread has a truthful `Ready For Coding` item instead of guessing.

The planning thread should also normalize landing truth.
That means:

- inventory the recent slices,
- classify each one as `Landed`, `Ready To Land`, `Blocked`, `In Progress`, or `Historical`,
- and write that classification back into the waitlist.

Do **not** jump directly into a full implementation plan for the whole system.
That would likely become stale immediately.

---

## Constraints For The New Thread

Please preserve these constraints:

- planning thread only
- no implementation work
- no opportunistic code edits
- optimize for top-to-bottom clarity and stable boundaries
- prioritize generality over turbulence-specific convenience
- treat canvas as authoring/design, not runtime partitioning
- treat AI as deferred until the lower layers are clearer

## User Support Context

The user has very limited confidence and experience with:

- version control
- Git
- GitHub
- branch and merge decisions

So when planning touches those topics:

- explain them plainly
- prefer concrete examples over abstract Git vocabulary
- use light analogies when helpful
- recommend safe defaults instead of assuming the user can improvise Git strategy

Do not turn the planning thread into a Git tutorial.
Do explain Git and GitHub when they matter to the plan.

## Failure Modes To Guard Against

The planning thread should actively guard against these patterns that have already appeared:

1. starting from details before the top-level system structure is clear
2. treating the turbulence experiment as the platform architecture instead of as a reference case
3. letting universal runtime absorb experiment-specific logic
4. mixing planning and implementation in one thread
5. using chat history as the only memory instead of docs, plans, and review artifacts
6. designing very large work packages that are not reducible to mergeable slices

If any of these appear, the planning thread should explicitly call them out and correct course.

---

## Suggested Opening Prompt For The New Thread

Use `$brainstorming` to re-baseline Orchestral at the system level.

Goal:

- produce a crystal-clear top-to-bottom architecture and strategic roadmap for the **general platform**, not for turbulence specifically

Use the following as current truth:

- `C:\Users\Yi Zhuang\OneDrive\Codes\Projects\Orchestral\docs\development\2026-04-03-planning-thread-context.md`
- the newer runtime/canvas/experiment-logic docs referenced inside it

Tasks:

1. determine whether the old roadmap docs are now stale enough to supersede
2. define the canonical system layering
3. propose the correct roadmap replacement structure
4. define the next execution tracks

Deliverables:

1. a strategic roadmap replacement
2. a top-to-bottom system layering statement
3. a decomposition of the next planning/execution frontier
4. clear guidance on how future work should be reduced into mergeable slices

Planning only. Do not start implementation.
