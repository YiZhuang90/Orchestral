# Thread Context Management Contract

## Purpose

This document defines how context should move across threads in Orchestral.

The goal is simple:

- threads should do work,
- files should hold memory,
- and the next thread should be able to resume from written truth instead of fragile chat history.

Plain-language analogy:

- a thread is a worker,
- the waitlist is the job board,
- the handoff packet is the baton,
- and this contract defines what must be written on that baton before passing it on.

This document sits **above** the single-slice workflow in [MODULE_DEVELOPMENT_WORKFLOW.md](./MODULE_DEVELOPMENT_WORKFLOW.md).

---

## Core Principle

Treat files as memory, not threads.

That means:

- planning decisions belong in planning docs,
- slice intent belongs in plans and handoff packets,
- queue state belongs in the waitlist,
- review truth belongs in review artifacts,
- branch and landing truth belongs in written closure notes,
- and chat history should never be the only place where an important decision lives.

---

## Thread Types

Every session type has a universal lifecycle defined in [CROSS_SESSION_WORKFLOW.md](./CROSS_SESSION_WORKFLOW.md) and a session-specific module workflow.

### 1. Planning

Module workflow: [MODULE_PLANNING_WORKFLOW.md](./MODULE_PLANNING_WORKFLOW.md)

Use for:

- roadmap resets,
- system layering,
- execution-track decomposition,
- queue shaping,
- and strategic decisions.

Planning threads may change priorities and define what becomes ready for coding.

### 2. Coding

Module workflow: [MODULE_DEVELOPMENT_WORKFLOW.md](./MODULE_DEVELOPMENT_WORKFLOW.md)

Use for:

- one mergeable implementation slice,
- one bounded branch/worktree path,
- and one clear landing decision.

Coding threads do not own the whole roadmap.
They take one ready slice and carry it forward.

### 3. Debug

Module workflow: [MODULE_DEBUG_WORKFLOW.md](./MODULE_DEBUG_WORKFLOW.md)

Use when:

- the root cause is unclear,
- the failure scope is wider than one local edit,
- or the coding thread can no longer truthfully say what is broken.

Debug threads investigate first.
They do not quietly become general feature work.

### 4. Landing

Module workflow: [MODULE_LANDING_WORKFLOW.md](./MODULE_LANDING_WORKFLOW.md)

Use for:

- merge and branch decisions,
- final verification on the effective merge result,
- review/PR closure,
- and cleanup.

Landing is integration and closure work, not more feature development.

### 5. Optimization

Module workflow: not yet written (deferred until optimization work is on the horizon).

Use when:

- an existing feature already works,
- there is a bounded target to improve,
- and success is measured by a metric, benchmark, or explicit quality bar.

Examples:

- improve runtime latency,
- reduce false positives in a detector,
- tune a UI flow against a measured outcome.

Optimization threads should start from:

- a current baseline,
- a target metric,
- a bounded scope,
- and a keep/discard discipline for attempted changes.

---

## Source-Of-Truth Precedence

Different questions have different truth sources.

### 1. What exists right now?

Use:

- current code,
- current branch/worktree state,
- current tests,
- and current artifacts.

If a doc says something exists but the code does not, the code wins for "what exists".

### 2. What should be built next?

Use:

- the current planning output,
- the current waitlist,
- and the latest non-stale roadmap/architecture docs.

If an old roadmap conflicts with a newer architecture or planning doc, the newer doc wins.

### 3. What is this thread responsible for?

Use:

- the current waitlist item,
- the current handoff packet,
- and the current slice plan.

If the thread starts doing work not described there, either:

- the slice is drifting and must be rewritten,
- or it is actually a new slice and needs a new thread.

### 4. What still needs explanation to the user?

Use:

- the current handoff packet,
- verification state,
- review state,
- and branch/landing state.

Do not assume the next thread can reconstruct these from commit history alone.

---

## Required Bootstrap Context By Thread Type

### Planning Thread Must Load

- [2026-04-03-planning-thread-context.md](./2026-04-03-planning-thread-context.md)
- this contract
- the relevant current architecture docs
- the live waitlist when decomposition or prioritization matters

Planning threads should also inspect older roadmap docs only as historical inputs, not as default truth.

### Coding Thread Must Load

- this contract
- [CROSS_THREAD_DEV_WAITLIST.md](./CROSS_THREAD_DEV_WAITLIST.md)
- [CROSS_THREAD_CODING_WORKFLOW.md](./CROSS_THREAD_CODING_WORKFLOW.md)
- [MODULE_DEVELOPMENT_WORKFLOW.md](./MODULE_DEVELOPMENT_WORKFLOW.md)
- the selected item's handoff packet
- the selected slice plan if it already exists

### Debug Thread Must Load

- this contract
- the current waitlist item
- the current handoff packet
- the failure evidence
- the relevant governing docs

### Landing Thread Must Load

- this contract
- the current waitlist item
- the current handoff packet
- verification evidence
- review artifacts
- current branch/worktree and merge-target truth

### Optimization Thread Must Load

- this contract
- the current waitlist item
- the current handoff packet
- the optimization target
- the current baseline
- the success metric
- the bounded scope

---

## Handoff Packet Contract

Every important thread should leave or update a handoff packet.

Recommended location:

- `docs/development_v2/handoffs/YYYY-MM-DD-<item-name>-handoff.md`

If another location is used, the waitlist must point to it explicitly.

### Minimum Required Fields

- item id and title
- thread type
- purpose of the work
- current status
- governing docs
- current branch
- current worktree
- current commit or diff state
- verification state
- review state
- open risks or blockers
- next recommended thread type
- next explicit action

### Good Example

- item: `SL-003 Minimum experiment-logic proof`
- status: `Blocked`
- blocker: `planning thread has not yet re-baselined the roadmap`
- next thread type: `Planning`
- next action: `decide whether Reynolds proof stays the next experiment-logic slice`

This is much better than:

- "we should probably continue later"

---

## Waitlist Contract

The waitlist is the live queue.

It should track:

- what still needs planning,
- what is ready for coding,
- what is already active,
- what is blocked,
- what is waiting to land,
- what has landed,
- and what may later become optimization work.

### Ownership Rules

Planning threads may:

- reprioritize,
- add new items,
- split items,
- merge items,
- and move items into `Ready For Coding`.

Coding threads may:

- claim one ready item,
- move that item through execution states,
- update its handoff packet,
- and propose new follow-up items.

Coding threads should **not** casually rewrite the whole queue.

Landing threads may:

- move completed work into `Landed`,
- or return it to `Blocked` / `Ready For Coding` if landing fails.

---

## Open And Close Rules

### Before Opening A Thread

Make sure:

- the thread type is named,
- the required bootstrap docs are loaded,
- the relevant waitlist item exists,
- and the thread has one clear success criterion.

### Before Closing A Thread

Make sure:

- the waitlist item status is updated,
- the handoff packet is updated,
- the branch/worktree state is recorded,
- verification and review truth are written down,
- and the next thread type is named explicitly.

If that is missing, the thread is not really closed.

---

## Git And GitHub Explanation Rule

Because the user has limited Git/GitHub experience, every thread that touches branch or landing state must explain those things plainly.

Preferred model:

- `main` = the shared book
- `branch` = a draft chapter
- `worktree` = a second desk with that draft open
- `commit` = a checkpoint photo
- `pull request` = asking whether the draft can join the book

Do not rely on unexplained jargon when simple language would work.

---

## Anti-Patterns

Do not:

- use chat as the only memory,
- start a coding thread without a waitlist item,
- keep one coding thread alive after the slice is already done,
- silently reprioritize the queue from inside a coding thread,
- mix planning and coding because "it is all related",
- or leave branch/worktree truth unwritten for the next thread.

If any of these happen, stop and repair the written state first.
