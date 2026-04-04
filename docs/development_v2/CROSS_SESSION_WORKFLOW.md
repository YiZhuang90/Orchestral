# Cross-Session Workflow

## Purpose

This document defines the universal lifecycle for any Orchestral development session, regardless of type.

It answers:

- how does a session start?
- how does it find its work?
- how does it route to the right module workflow?
- how does it close so the next session can resume from written truth?

This is the generalized successor to the coding-specific cross-thread workflow. It covers all session types.

---

## Position In The Workflow Stack

```
THREAD_CONTEXT_MANAGEMENT_CONTRACT.md      (meta-contract — the rules)
  |
  v
This document: CROSS_SESSION_WORKFLOW.md   (universal session lifecycle)
  |
  v  Route to session-specific module:
  |--- MODULE_DEVELOPMENT_WORKFLOW.md      (coding)
  |--- MODULE_PLANNING_WORKFLOW.md         (planning)
  |--- MODULE_LANDING_WORKFLOW.md          (landing)
  |--- MODULE_DEBUG_WORKFLOW.md            (debug)
```

The meta-contract defines the rules and principles.
This workflow defines the universal startup, routing, and closing.
The module workflows define the session-specific execution steps.

---

## Session Types

| Session Type | Purpose | Module Workflow | Primary Superpowers Skills |
|---|---|---|---|
| Planning | Strategic decisions, roadmap, architecture, waitlist shaping | MODULE_PLANNING_WORKFLOW.md | `$brainstorming` -> `$writing-plans` |
| Coding | One mergeable implementation slice | MODULE_DEVELOPMENT_WORKFLOW.md | `$using-git-worktrees` -> `$subagent-driven-development` -> `$requesting-code-review` -> `$finishing-a-development-branch` |
| Landing | Merge, verify, push, cleanup | MODULE_LANDING_WORKFLOW.md | `$finishing-a-development-branch` |
| Debug | Investigate unclear failures | MODULE_DEBUG_WORKFLOW.md | `$systematic-debugging` -> `$dispatching-parallel-agents` |

---

## Universal Startup

Every session starts the same way, regardless of type.

### 1. Load the workflow stack

Load these in order:

1. [THREAD_CONTEXT_MANAGEMENT_CONTRACT.md](./THREAD_CONTEXT_MANAGEMENT_CONTRACT.md)
2. [CROSS_THREAD_DEV_WAITLIST.md](./CROSS_THREAD_DEV_WAITLIST.md)
3. This document

### 2. Identify the session type

Determine whether this session is:

- planning,
- coding,
- landing,
- or debug.

If unclear, ask. Do not guess.

### 3. Find the relevant waitlist item

- For coding sessions: find the first truthful item in `Ready For Coding`
- For landing sessions: find the item in `Ready To Land`
- For debug sessions: find the item associated with the failure, or create one
- For planning sessions: find the item in `Needs Planning`, or work from the current roadmap frontier

If `Ready For Coding` is empty and this is a coding session, stop. Report that a planning or landing session is needed first.

### 4. Load the item's context

- Load the handoff packet if one exists
- Load the item's governing docs
- Load the existing slice plan if one exists

### 5. Route to the module workflow

Load the appropriate module workflow for this session type and follow it.

---

## Universal Work Item Pickup

When claiming a work item:

- Move it to the appropriate waitlist state (usually `In Progress`)
- Record:
  - session type
  - branch or worktree (if applicable)
  - handoff packet path (create one if it does not exist)

If the work item does not have a handoff packet, create one before proceeding with the module workflow. Minimum handoff fields are defined in [THREAD_CONTEXT_MANAGEMENT_CONTRACT.md](./THREAD_CONTEXT_MANAGEMENT_CONTRACT.md).

---

## Universal Context Output

Before closing ANY session, regardless of type, the following must be written down.

This is the most important section of this document. If the context output is incomplete, the next session cannot resume cleanly.

### Required outputs

1. **What changed** — decisions made, code written, docs produced, architecture updated
2. **What was verified** — tests run, reviews completed, verification evidence
3. **What is still open** — blockers, unresolved questions, deferred work
4. **Handoff packet** — updated or created, pointing to all artifacts from this session
5. **Next session type** — explicitly named (planning / coding / landing / debug / none)
6. **Waitlist update** — the work item moved to its correct state

### Where to write it

- Handoff packet: `docs/development_v2/handoffs/YYYY-MM-DD-<item-name>-handoff.md`
- Waitlist: [CROSS_THREAD_DEV_WAITLIST.md](./CROSS_THREAD_DEV_WAITLIST.md)
- Planning artifacts: `docs/development_v2/` or `docs/architecture/`
- Review artifacts: `docs/collaboration/reviews/`
- Slice plans: `docs/development_v2/` or the worktree's `docs/development/`

### What must NOT be the only record

- Chat history
- Verbal agreements
- Uncommitted mental models

If an important decision exists only in chat, it must be written to a file before the session closes.

---

## Universal Closing Rule

A session is not closed until the context output is complete.

If the context output is missing, the session is not really over. It is abandoned.

Checklist before closing:

- [ ] Handoff packet updated
- [ ] Waitlist item in correct state
- [ ] All produced artifacts written to files
- [ ] Next session type named
- [ ] No important decisions exist only in chat

---

## When To Split Into A New Session

During any session, if you discover:

- the work belongs to a different session type (e.g., a coding session discovers an architectural issue that needs planning),
- the scope has grown beyond one mergeable slice,
- root cause is unclear and investigation is needed,
- or the session has already finished its work but a new piece of work appeared:

Then:

1. Finish or deliberately stop the current session
2. Complete the context output for the current session
3. Record the new work as a new waitlist item
4. Name the new session type in the handoff

Do not silently continue into a different kind of work because "it is all related."

---

## Relationship To The Meta-Contract

This workflow implements the operational side of [THREAD_CONTEXT_MANAGEMENT_CONTRACT.md](./THREAD_CONTEXT_MANAGEMENT_CONTRACT.md).

The contract defines:

- what thread types exist,
- what source-of-truth precedence applies,
- what handoff packets must contain,
- what ownership rules apply to the waitlist.

This workflow defines:

- how a session starts,
- how it finds and claims work,
- how it routes to the right module,
- how it closes with complete context.

---

## Anti-Patterns

- Starting a session without loading the waitlist
- Guessing the session type instead of checking the work item
- Skipping the handoff packet
- Leaving context output incomplete ("I'll finish later")
- Mixing two session types in one session
- Using chat as the only record of decisions
