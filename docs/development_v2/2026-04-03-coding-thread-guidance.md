# Coding Thread Guidance

## Superseded

This document's content has been merged into [MODULE_DEVELOPMENT_WORKFLOW.md](./MODULE_DEVELOPMENT_WORKFLOW.md) (failure modes, slice sizing) and [CROSS_SESSION_WORKFLOW.md](./CROSS_SESSION_WORKFLOW.md) (universal session lifecycle). Kept for historical reference.

---

## Purpose

This document explains how coding threads should be used for Orchestral.

The goal is to keep development:

- clear,
- bounded,
- reviewable,
- and easy to land in small, coherent slices.

This document is intentionally separate from planning guidance.

The planning thread decides what should be built.
The coding thread delivers one bounded slice of that decision.

For the live multi-thread baton-passing rules, use:

- [THREAD_CONTEXT_MANAGEMENT_CONTRACT.md](./THREAD_CONTEXT_MANAGEMENT_CONTRACT.md)
- [CROSS_THREAD_DEV_WAITLIST.md](./CROSS_THREAD_DEV_WAITLIST.md)
- [CROSS_THREAD_CODING_WORKFLOW.md](./CROSS_THREAD_CODING_WORKFLOW.md)

---

## Core Rule

The default rule is:

- **one coding thread = one mergeable slice**

That usually also means:

- one coding thread = one branch
- one coding thread = one worktree
- one coding thread = one landing decision

This does **not** mean one thread per file or one thread per tiny edit.
It means one thread should produce one coherent unit that can be:

- explained,
- reviewed,
- verified,
- merged,
- and reverted on its own.

---

## Why This Rule Exists

Large projects become messy when one thread tries to do too many jobs at once.

Typical failure pattern:

1. start with one idea
2. drift into related details
3. fix two or three more things "while already here"
4. finish with a branch that no longer has one clear purpose

That makes review, verification, and landing much harder.

Small slice threads are better because they:

- reduce confusion,
- reduce merge risk,
- make verification easier,
- keep commit history understandable,
- and prevent strategic work from being mixed with tactical work.

---

## Thread Types

Use different thread types for different kinds of work.

### 1. Planning Thread

Use for:

- system layering
- roadmap restructuring
- architectural re-baselining
- execution-track decomposition

Output:

- planning docs
- roadmap docs
- architecture docs

Do not use this thread to start implementation.

### 2. Coding Thread

Use for:

- one bounded implementation slice
- one mergeable unit of work

Output:

- slice plan
- code changes
- tests
- review artifacts
- pushed branch

### 3. Debug Thread

Use for:

- root-cause investigation
- failures
- regressions
- unclear behavior

Only move from debug into implementation once the problem is understood.

### 4. Landing Thread

Use for:

- final review of a completed slice
- merge decision
- cleanup

This should be short and operational.

### 5. Optimization Thread

Use for:

- bounded improvement of an already-working feature
- work with a metric, benchmark, or explicit quality target
- iterative keep/discard exploration

Do not use this thread type as a vague excuse for feature creep.

---

## When To Open A New Coding Thread

Open a new coding thread when any one of these changes:

- the success criterion changes
- the branch should be a separate merge unit
- the work needs a different governing doc set
- the task becomes strategic instead of tactical
- the current thread starts mixing implementation with debugging or planning

Good signs that a new thread is needed:

- "this is actually a different slice"
- "this should land separately"
- "this needs a different review"
- "this is no longer just implementation"

---

## Good Slice Size

A good coding-thread slice is:

- bounded,
- independently reviewable,
- independently testable,
- and understandable in one short explanation.

Good examples:

- add one runtime-source seam and one proof test path
- move one experiment-logic unit to its new layer
- add one validation rule family with tests
- land one recorder artifact improvement

Bad examples:

- "finish the whole monitor system"
- "build the entire canvas"
- "refactor runtime, recorder, and experiment logic together"

If a thread needs multiple independent commit messages to explain what it really does, it is probably too broad.

---

## Relationship Between Planning And Coding

The planning thread should hand off:

- system intent
- boundaries
- the next slice frontier
- the relevant docs

The coding thread should then narrow that into:

- one slice
- one plan
- one landing path

Do not use a coding thread to keep re-deciding the whole system.
Do not use a planning thread to quietly start editing code.

---

## Memory Model

Treat files as memory, not threads.

Each important thread should leave behind one or more of:

- a context doc
- a handoff packet
- a roadmap update
- a slice plan
- review artifacts
- a branch

The next thread should start from those artifacts instead of depending on long conversation history.

---

## Git And GitHub Mental Model

The user should not be expected to already understand version control well.

When Git or GitHub matters, explain it simply.

Useful mental model:

- `main` = the shared book everyone trusts
- `branch` = your private draft chapter
- `worktree` = a second desk where that draft is open
- `commit` = a saved checkpoint photo of the draft
- `pull request` = asking whether the draft chapter is ready to join the book

Simple example:

- planning says "build PT-104 runtime-source harness"
- create branch `codex/pt104-runtime-source-harness`
- work on that branch only
- commit the finished slice
- push it
- then decide whether to merge it

In plain words:

- a branch keeps one slice isolated
- a worktree keeps that slice physically separated from other local work
- a commit saves progress safely
- a PR or merge is the decision to make that slice part of shared truth

When explaining Git steps, prefer:

- examples
- plain language
- and light analogies

Do not assume the user already understands:

- fast-forward
- divergence
- rebasing
- detached HEAD
- or branch tracking

Explain those only when they matter.

---

## Common Failure Modes

These are failure modes already seen in this project.

### 1. Starting Randomly

Pattern:

- jump into whatever feels concrete first
- details start moving before the layer boundary is clear

Why it hurts:

- work gets done in the wrong layer
- later docs and architecture must be rewritten around implementation drift

Correction:

- decide first whether the work is:
  - planning
  - runtime
  - experiment logic
  - canvas
  - or AI

### 2. Treating The Reference Experiment As The Architecture

Pattern:

- turbulence-specific needs start defining the platform too early

Why it hurts:

- generality gets lost
- platform concepts become experiment-shaped

Correction:

- turbulence is a reference case
- architecture must be justified more generally than turbulence convenience

### 3. Mixing Planning And Coding In One Thread

Pattern:

- thread starts with strategy
- quietly turns into implementation
- then turns into merge/landing discussion

Why it hurts:

- decisions blur together
- the thread loses one clear purpose

Correction:

- split threads by intent

### 4. Letting One Branch Accumulate Multiple Finished Slices

Pattern:

- slice A is done
- then slice B gets added
- then slice C gets added because "we are already here"

Why it hurts:

- review gets harder
- rollback gets harder
- merge decisions become all-or-nothing

Correction:

- land slice A
- then open a new branch/thread for slice B

### 5. Using Conversation History As The Only Memory

Pattern:

- important design decisions only live in chat

Why it hurts:

- future work starts from partial memory
- decisions become inconsistent

Correction:

- write the decision into a doc, plan, review artifact, or branch

### 6. Falling Into Detail Too Early

Pattern:

- implementation detail feels safer than system design
- detailed work starts before the top-level structure is stable

Why it hurts:

- local clarity hides global confusion

Correction:

- when the system boundary still feels blurry, stop and go back up a level

---

## Recommended Rhythm For Orchestral Right Now

At the current stage of Orchestral, use this rhythm:

1. one planning thread to keep system structure and roadmap honest
2. one coding thread per mergeable slice
3. one debug thread when the root cause is unclear
4. one short landing thread when a slice is ready

In practical terms:

- first decide the frontier
- then reduce it to one slice
- then deliver that slice cleanly
- then land it
- then open the next slice thread

That rhythm is slower at the very start of a slice, but much faster over the life of a large project.
