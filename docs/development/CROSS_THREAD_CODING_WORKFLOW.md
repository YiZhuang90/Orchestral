# Cross-Thread Coding Workflow

## Purpose

This document explains how one coding thread should start, run, and close inside the larger multi-thread Orchestral process.

It sits:

- below [THREAD_CONTEXT_MANAGEMENT_CONTRACT.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/development/THREAD_CONTEXT_MANAGEMENT_CONTRACT.md),
- beside [CROSS_THREAD_DEV_WAITLIST.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/development/CROSS_THREAD_DEV_WAITLIST.md),
- and above [MODULE_DEVELOPMENT_WORKFLOW.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/development/MODULE_DEVELOPMENT_WORKFLOW.md).

Plain-language summary:

- the contract defines the baton,
- the waitlist says which baton is available,
- this workflow says how to pick it up and pass it on,
- and the module workflow says how to execute the actual slice.

---

## Default Rule

The default rule is:

- **one coding thread = one ready waitlist item**

Not:

- one coding thread = whatever seems important,
- or one coding thread = several related slices,
- or one coding thread = everything on the same branch.

---

## Startup Checklist

Before starting coding work, load:

- [THREAD_CONTEXT_MANAGEMENT_CONTRACT.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/development/THREAD_CONTEXT_MANAGEMENT_CONTRACT.md)
- [CROSS_THREAD_DEV_WAITLIST.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/development/CROSS_THREAD_DEV_WAITLIST.md)
- this workflow
- [MODULE_DEVELOPMENT_WORKFLOW.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/development/MODULE_DEVELOPMENT_WORKFLOW.md)

Then do this:

1. find the first truthful item in `Ready For Coding`
2. load its handoff packet and existing plan if they exist
3. load its governing docs
4. confirm the item is still one mergeable slice

If `Ready For Coding` is empty, stop.
Report that a planning or waitlist-maintenance thread is needed.

---

## Pickup Procedure

### 1. Claim The Item

Move the chosen item from:

- `Ready For Coding`

to:

- `In Progress`

Record:

- thread type: `coding`
- branch or planned branch
- worktree or planned worktree
- handoff packet path

### 2. Create Or Refresh The Handoff Packet

If no handoff packet exists, create one before implementation starts.

Minimum contents:

- item id and title
- slice goal
- governing docs
- current status
- branch/worktree plan
- open questions
- next success criterion

### 3. Route Into The Single-Slice Workflow

Once the item is claimed, use:

- [MODULE_DEVELOPMENT_WORKFLOW.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/development/MODULE_DEVELOPMENT_WORKFLOW.md)
- and the [`orch-module-dev`](C:/Users/Yi%20Zhuang/.codex/skills/orch-module-dev/SKILL.md) skill

for the actual slice execution.

This workflow does **not** replace the slice workflow.
It only selects and frames the slice.

---

## During Execution

While the coding thread is active:

- keep the slice bounded,
- keep the waitlist truthful,
- update the handoff packet when the real situation changes,
- and do not silently absorb unrelated work because it is nearby.

If a second independent slice appears:

1. finish or deliberately stop the current slice
2. record the follow-up as a new waitlist item
3. open a new coding thread later

If root cause becomes unclear, split into a debug thread.
If merge/landing becomes its own job, split into a landing thread.

---

## Closing Procedure

Before a coding thread ends, it must do two things:

### 1. Update The Handoff Packet

Record:

- what changed
- what was verified
- what review happened
- what branch/worktree/commit state exists now
- what is still open
- what the next thread type should be

### 2. Move The Waitlist Item To The Correct State

Common outcomes:

- `Blocked`
  - work cannot continue without outside help or a different thread type
- `In Review`
  - review is still the active next step
- `Ready To Land`
  - implementation, verification, and review are done; landing remains
- `Landed`
  - the slice is merged and closed
- `Planned But Not Ready`
  - the slice was re-scoped and needs planning before coding resumes

Do not leave the item in `In Progress` if the thread is actually over.

---

## What This Workflow Does Not Do

This workflow does **not**:

- reprioritize the entire project
- replace planning threads
- replace debug methodology
- replace landing methodology
- replace [`orch-module-dev`](C:/Users/Yi%20Zhuang/.codex/skills/orch-module-dev/SKILL.md)
- or decide optimization strategy

It is specifically the pickup/resume/close wrapper for one coding thread.

---

## Example

Good sequence:

1. planning thread updates the waitlist and marks one slice `Ready For Coding`
2. coding thread claims that item and moves it to `In Progress`
3. coding thread writes or refreshes the handoff packet
4. coding thread executes the slice under the module workflow
5. coding thread finishes with `Ready To Land`
6. landing thread merges it and moves it to `Landed`

Bad sequence:

1. no ready item exists
2. coding thread starts anyway
3. thread invents scope while implementing
4. branch now mixes planning, coding, and landing
5. nobody knows what should happen next

This workflow exists to prevent the bad sequence.
