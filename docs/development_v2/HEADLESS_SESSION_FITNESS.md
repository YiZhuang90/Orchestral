# Headless Session Fitness

## Purpose

Before dispatching any session (coding, landing, debug) as a headless (non-interactive) run, this document defines the fitness check that determines whether the session can complete without human input.

If all criteria pass, the session can run headless (remotely, unattended). If any criterion fails, the session must be interactive.

---

## Universal Fitness Criteria

A session is headless-safe when ALL of these are true:

| Criterion | Why it matters |
|-----------|---------------|
| **Slice plan exists and is complete** | A headless session cannot ask clarifying questions. Every decision must already be in the plan. |
| **No open design questions** | If the plan says "TBD" or "decide during implementation," the session will stall waiting for input that never arrives. |
| **Success criteria are machine-verifiable** | The session must be able to tell if it succeeded by running tests or build checks. Human judgment cannot be applied headlessly. |
| **Git state is clean** | No diverged branches, no unresolved stashes, no pending conflicts. If Git needs human judgment, headless fails. |
| **No hardware interaction required** | Headless sessions cannot physically connect devices or observe hardware behavior. |
| **Governing docs are consistent** | If two docs disagree, the session needs human judgment to pick a winner. That conflict must be resolved before dispatch. |
| **Precedent exists** | First-time patterns (first project creation, first landing, first new workflow) should be interactive so the user sees how it works. Repeat patterns are safe for headless. |

---

## Per-Session-Type Fitness

### Coding Sessions

**Headless-safe when:**

- Slice plan has ordered tasks with expected outcomes
- Tests exist or the plan specifies which tests to write
- No new architectural patterns being established for the first time
- Worktree can be created from a clean base branch
- Build passes on the current base branch

**NOT headless-safe when:**

- The slice involves a new project or namespace being created for the first time (user should see the structure)
- The spec has open questions or multiple valid approaches not yet resolved
- The slice touches shared contracts where the wrong choice would affect other layers
- The slice is the first use of a new workflow or skill

### Landing Sessions

**Headless-safe when:**

- The slice is reviewed and verified (Ready To Land)
- Local main is synced with origin/main (no divergence)
- The merge is expected to be a clean fast-forward
- No stashes or uncommitted work in the target checkout
- This is not the first time landing through this workflow

**NOT headless-safe when:**

- Local main is diverged from origin/main (conflict resolution may be needed)
- Stashes exist that might interact with the merge
- The slice was not yet reviewed
- First time landing through this workflow (user should see the process)

### Debug Sessions

**Headless-safe when:**

- The failure is reproducible with a specific test command
- The scope is bounded to one module or file area
- The debugging strategy is clear (e.g., "run test X, trace why Y fails")

**NOT headless-safe when:**

- Root cause is completely unknown
- Multiple independent failures may be interacting
- The failure may be environmental (hardware, OS, timing)
- Human judgment is needed to decide whether to fix inline or defer to a new slice

### Planning Sessions

Almost never headless-safe. Planning requires human judgment, clarifying questions, and design approval. The only exception would be a purely mechanical planning task like "update the waitlist to reflect that LD-001 has landed."

---

## How To Use This Check

### Before dispatching any session:

1. Read the slice plan for the target work item
2. Walk through each criterion in the universal fitness table above
3. Check the session-type-specific criteria
4. If ALL criteria pass, headless dispatch is safe
5. If ANY criterion fails, the session must be interactive
6. Record the fitness decision in the handoff packet so future sessions can see the precedent

### In the orch-session-* skills:

Each session skill should evaluate headless fitness as the first step after context loading. If the check fails, the skill should announce which criteria failed and recommend interactive mode.

---

## Fitness Record Template

When recording a fitness decision in the handoff packet:

```
Headless fitness: PASS / FAIL
Checked: [date]
Failed criteria: [list, or "none"]
Recommendation: headless / interactive
Reason: [one sentence]
```

---

## Anti-Patterns

- Dispatching headless without checking fitness ("it's probably fine")
- Ignoring the "precedent exists" criterion for novel patterns
- Assuming a previously-headless task is still headless-safe after the codebase changed
- Running debug sessions headless when root cause is unknown
