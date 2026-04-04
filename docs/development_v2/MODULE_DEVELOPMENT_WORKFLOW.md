# Module Development Workflow

## Purpose

This document defines how one Orchestral module or slice should be developed from idea to landing.

Examples of a slice:

- runtime IO for one device family,
- one device-session migration,
- one shared panel/template migration,
- one experiment-package implementation slice.

The goal is to make development repeatable, reviewable, and compatible with sub-agent work.

## Position In The Larger Multi-Thread System

This document governs the **inside** of one coding slice.

In the multi-thread development model, it sits below:

- [THREAD_CONTEXT_MANAGEMENT_CONTRACT.md](./THREAD_CONTEXT_MANAGEMENT_CONTRACT.md)
- [CROSS_THREAD_DEV_WAITLIST.md](./CROSS_THREAD_DEV_WAITLIST.md)
- [CROSS_THREAD_CODING_WORKFLOW.md](./CROSS_THREAD_CODING_WORKFLOW.md)

Those docs answer:

- which item should be worked on,
- what context must be loaded,
- how context is saved for the next thread,
- and how the queue stays truthful.

This document then answers:

- how to deliver the selected slice cleanly.

## Default Rhythm

The default development rhythm for Orchestral is:

1. one planning thread for strategic or architecture-level decisions
2. one coding thread for one mergeable slice
3. one debug thread when root cause is unclear
4. one short landing thread when the slice is complete

This workflow document governs the **coding thread** and its landing path.
It should not be used as the main structure for broad system re-baselining.

## 1. Clarify The Slice

Before coding, the slice must be written down clearly.

Required:

- goal,
- in-scope work,
- out-of-scope work,
- success criteria,
- dependencies,
- verification target,
- landing target.

### 1.1 Thread Fit

Before implementation starts, confirm that the requested work fits one coding thread.

In the cross-thread workflow, a coding thread should also have:

- a live waitlist item,
- a truthful status,
- and a handoff packet or an explicit instruction to create one now.

Default rule:

- **one coding thread = one mergeable slice**

That usually also means:

- one coding thread = one branch
- one coding thread = one worktree
- one coding thread = one landing decision

If the requested work would produce multiple independently mergeable units, split it before coding.
Do not keep stacking finished slices on one long-lived implementation thread by reflex.

### 1.2 Required Foundation Docs

Every slice must list the docs that govern it before implementation starts.

For each slice, classify docs as:

- `required foundation`
- `supporting reference`
- `conflict winner`

If two docs disagree, the `conflict winner` must be named explicitly.

Example: runtime IO slice

Required foundation:

- [DEVICE_RUNTIME_IO_ARCHITECTURE.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/architecture/DEVICE_RUNTIME_IO_ARCHITECTURE.md)
- [INTEGRATION_PANEL_IO_CONTRACT.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/architecture/INTEGRATION_PANEL_IO_CONTRACT.md)
- [DEVICE_PANEL_CONTRACT.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/architecture/DEVICE_PANEL_CONTRACT.md)
- [TIMING_AND_SYNCHRONIZATION_STRATEGY.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/architecture/TIMING_AND_SYNCHRONIZATION_STRATEGY.md)

Conflict winner:

- [DEVICE_RUNTIME_IO_ARCHITECTURE.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/architecture/DEVICE_RUNTIME_IO_ARCHITECTURE.md)

If the required foundation docs do not exist yet, write them first.

## 2. Inspect Current Reality

Before implementation, inspect the code that already exists.

Required outputs:

- current ownership model,
- current runtime/data/control path,
- files that must change,
- files that must not change,
- risks from existing behavior,
- missing test coverage.

This phase is about actual code truth, not intended design.

## 3. Write Or Select An Execution Plan

Every nontrivial slice should have an execution plan.

The plan should include:

- ordered tasks,
- owned files,
- verification gates,
- review gates,
- landing path.

If the work still needs broad roadmap or architecture restructuring, stop and move it back to a planning thread instead of forcing it through a coding-thread plan.

Execution plans should be slice-specific.
Do not use the roadmap as an execution plan.

## 4. Implement With Verification

### Superpowers integration

Implementation should follow:

- `$test-driven-development`: write a failing test first, then implement, then refactor. RED -> GREEN -> REFACTOR. No code before test.
- `$subagent-driven-development`: each task dispatched to a fresh subagent with TDD and a 2-stage review gate (spec compliance + code quality).
- `$verification-before-completion`: run verification fresh before claiming any task is done. No "should work" or "probably fixed."

Implementation rules:

- use bounded edits,
- write tests first (enforced by `$test-driven-development`),
- verify before declaring success (enforced by `$verification-before-completion`),
- keep architectural ownership clear.

Minimum verification should state:

- what was built,
- what was tested,
- what passed,
- what was not verified.

### 4.1 Device-Integration Closure

For device-integration slices, distinguish between:

- `functionally usable`
  - the real device path works through the intended service/session boundary
- `fully closed`
  - a virtual or simulation path also exists through that same Orchestral boundary

The preferred final step of a device-integration slice is to add the virtual closure path.

This may be:

- a virtual device service,
- a replay path,
- a synthetic source,
- or a narrower mock path when that is the best truthful seam.

This does not mean the virtual path must be the first blocker.
It does mean the integration should be designed so that the virtual path can be added without re-architecting the runtime session later.

## 5. Review

### Superpowers integration

Use `$requesting-code-review` to dispatch a reviewer subagent with precisely crafted context (not your session history). The reviewer returns strengths, issues (critical/important/minor), and specific recommendations. Fix issues before proceeding to landing.

Use `$receiving-code-review` when receiving feedback: read completely, verify against codebase, respond with technical rigor, implement one item at a time.

Every meaningful slice needs review.

### 5.1 When External Review Is Required

External review is required when the slice is:

- architecture-affecting,
- cross-cutting across multiple modules,
- runtime or lifecycle related,
- safety related,
- hardware-truthfulness related,
- hard to verify through tests alone,
- or likely to become shared foundation.

Examples that should receive external review:

- runtime IO architecture implementation,
- shared session layer,
- stop/safety logic,
- shared panel contract enforcement.

Examples that usually do not require external review:

- isolated wording cleanups,
- small cosmetic fixes,
- tiny local refactors with low system impact.

### 5.2 Review Artifacts

Use:

- [REVIEW_PROTOCOL.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/collaboration/REVIEW_PROTOCOL.md)

Review should produce:

- a review artifact,
- an implementer response artifact,
- and explicit retest status for accepted findings.

If a direct reviewer CLI such as Claude Code is available, prefer non-interactive one-shot review runs that emit file-backed artifacts, rather than keeping one persistent reviewer chat open across multiple unrelated slices.

## 6. Land The Slice

For the detailed landing procedure, see [MODULE_LANDING_WORKFLOW.md](./MODULE_LANDING_WORKFLOW.md).

Use `$finishing-a-development-branch` as the Superpowers skill for landing.

Landing includes:

1. clean branch or landing branch if needed,
2. final verification,
3. commit,
4. push,
5. PR or direct landing path,
6. merge,
7. cleanup of temporary worktrees/branches.

### 6.1 Git And GitHub Control

Treat landing as a real git-control step, not as an afterthought.

Before merge:

- make sure the slice branch is scoped to one coherent slice,
- sync against the latest `main` or chosen base branch,
- resolve conflicts before the final landing decision,
- rerun verification if the effective merge result changed,
- and choose the landing path explicitly:
  - direct fast-forward or local merge when appropriate,
  - or push plus pull request when review, visibility, or delayed landing is needed.

After merge:

- confirm the landed result on the base branch or merged result,
- push the updated base branch if the landing was local,
- delete or close the feature branch when it is no longer needed,
- and clean up any temporary worktree used for the slice.

If the local `main` checkout is dirty, do not force a merge there by reflex.
Use a clean landing branch or clean worktree instead.

### 6.2 Default Merge Size

The default landing unit should be one independently reviewable and verifiable slice.

Prefer:

- smaller merges,
- one coherent slice per branch,
- one coherent slice per PR,
- one coherent slice per coding thread,
- and frequent landing once that slice is actually done.

Avoid stacking multiple already-finished slices on one long-lived branch unless there is a clear reason to keep them together.

Smaller merges are preferred because they:

- reduce review burden,
- reduce merge-conflict risk,
- make rollback easier,
- and keep execution history easier to understand.

This does not mean every merge must be tiny.
It means one merge should normally correspond to one coherent slice that can be explained, reviewed, verified, and reverted on its own.

If a second slice emerges during implementation, the default action is:

1. finish or deliberately stop the current slice
2. land or park it clearly
3. open a new coding thread for the next slice

If the slice changed shared architecture or execution sequencing, update:

- [V1_EXECUTION_TRACK.md](./V1_EXECUTION_TRACK.md)

## 7. Context Output

Before closing a coding session, write down:

1. **What changed** — code written, tests added, docs updated
2. **What was verified** — which tests passed, what review happened
3. **What is still open** — blockers, deferred items, known gaps
4. **Handoff packet** — updated with branch/worktree state, verification evidence, review state
5. **Next session type** — explicitly named (landing if ready to merge, debug if issues found, planning if scope changed)
6. **Waitlist update** — item moved to correct state (In Review, Ready To Land, Blocked, etc.)

If any of these are missing, the session is not properly closed. See [CROSS_SESSION_WORKFLOW.md](./CROSS_SESSION_WORKFLOW.md) for the universal context output rules.

## 8. Failure Modes

Do not:

- start coding before naming required foundation docs,
- treat architecture docs as if they were execution plans,
- let the panel own runtime truth when a runtime layer is intended,
- claim success without verification (`$verification-before-completion` prevents this),
- skip review on shared foundation slices,
- land mixed unrelated work as one slice,
- start randomly without checking which layer the work belongs to (planning, runtime, experiment logic, canvas, or AI),
- treat the reference experiment as the architecture (turbulence is a reference case),
- let one branch accumulate multiple finished slices instead of landing each one,
- fall into detail before the system-level boundary is clear (go back up a level when boundaries feel blurry),
- use chat as the only record of important decisions.

## 9. Relationship To Sub-Agent Work

Sub-agents are the recommended execution model via `$subagent-driven-development`.

Each task from the plan is dispatched to a fresh subagent. Each subagent implements using TDD with a 2-stage review gate. The main session coordinates and preserves context.

For sub-agent contracts, see:

- [SUBAGENT_DEVELOPMENT_CONTRACT.md](../collaboration/SUBAGENT_DEVELOPMENT_CONTRACT.md)
