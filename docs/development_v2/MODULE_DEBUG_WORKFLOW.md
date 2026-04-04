# Module Debug Workflow

## 1. Purpose

Investigate and diagnose issues when the root cause is unclear. Debug sessions investigate first. They do not quietly become feature work.

The goal is to understand what is broken and why, then either fix it (if simple and bounded) or hand off to a coding session (if the fix requires a new slice).

---

## 2. When To Use

Use a debug session when:

- the root cause of a failure is unclear,
- the failure scope is wider than one local edit,
- the coding session can no longer truthfully say what is broken,
- tests fail for reasons that are not immediately obvious,
- or behavior differs from what architecture docs describe.

---

## 3. Context Loading

Before starting, load:

- [THREAD_CONTEXT_MANAGEMENT_CONTRACT.md](./THREAD_CONTEXT_MANAGEMENT_CONTRACT.md)
- [CROSS_SESSION_WORKFLOW.md](./CROSS_SESSION_WORKFLOW.md)
- The failure evidence (error messages, test output, logs)
- The waitlist item if one exists
- The handoff packet from the session that discovered the issue
- Relevant governing docs (architecture docs, device contracts, etc.)

---

## 4. Superpowers Skill Chain

- `$systematic-debugging` — the primary skill. Enforces the 4-phase investigation process described below. Prevents guessing.
- `$dispatching-parallel-agents` — use when 3 or more independent failures exist and can be investigated in parallel.
- `$verification-before-completion` — verify the fix actually works before claiming it is done.

---

## 5. Steps

The debug process follows 4 phases. Do not skip ahead.

### Phase 1: Root Cause Investigation

- Read the actual error messages. Do not guess.
- Reproduce the failure. If it cannot be reproduced, document the exact conditions.
- Trace the data flow from input to failure point.
- Check: does the code match what the architecture docs say it should do?

The goal of this phase is understanding, not fixing.

### Phase 2: Pattern Analysis

- Find a working example of similar behavior in the codebase.
- Compare the working path with the broken path.
- Identify what is different.

This narrows the search space before any fix is attempted.

### Phase 3: Hypothesis Testing

- Form a single clear hypothesis about the root cause.
- Test it with the smallest possible change.
- If the hypothesis is wrong, revise it. Do not stack more guesses on top.
- **Maximum 3 fix attempts.** If you have tried 3 times and the issue persists, step back and question your assumptions about what is broken. The problem may be in a different place than you think.

### Phase 4: Implementation

- Write a failing test that captures the root cause.
- Fix the root cause (not a symptom).
- Run the test and verify it passes.
- Run the broader test suite to check for regressions.

---

## 6. Cross-Thread Rules

- Debug sessions investigate first, then decide whether to fix or hand off.
- If the fix is simple and bounded (a few lines, same file, clear cause), it can be applied in the debug session.
- If the fix requires a new slice (new feature, architecture change, multi-file refactor), record it as a new waitlist item and close the debug session.
- Debug sessions should NOT quietly become general feature branches.
- If the issue turns out to be architectural (the design is wrong, not the code), hand off to a planning session.

---

## 7. Context Output

Before closing a debug session, write down:

1. **Root cause** — documented clearly, even if the fix has not been applied yet. This is the most valuable output of a debug session.
2. **What was tried** — each hypothesis and its result. This prevents the next session from re-discovering the same dead ends.
3. **Whether the fix was applied** — in this session, or deferred to a new coding slice.
4. **Waitlist update** — new items if the fix was deferred, current item updated if the fix was applied.
5. **Next session type** — explicitly named:
   - Coding session, if the fix needs a new slice
   - Planning session, if the issue is architectural
   - Landing session, if the fix was applied and is ready to merge
   - No follow-up, if the issue was fully resolved in this session

---

## 8. Plain-Language Git Notes

Debug sessions may or may not create branches.

- If the debug session only investigates without making code changes, no branch is needed.
- If the fix is applied, it should follow the same branch and commit discipline as a coding session: work on a branch, commit the fix with a clear message, push when ready.

---

## 9. Failure Modes

- **Guessing before reading the error.** The first step is always reading the actual error message, not forming a theory.
- **Retrying the same failed approach.** If something did not work, understand why before trying again.
- **Scope creep.** A debug session that quietly becomes a feature branch has lost its purpose. If you find new work, record it as a waitlist item and close the debug session.
- **Not recording what was tried.** The next session wastes time rediscovering dead ends if this is not written down.
- **Exceeding 3 fix attempts without stepping back.** After 3 failed fixes, the assumptions about the problem are likely wrong. Step back and re-examine.
- **Fixing a symptom instead of the root cause.** Symptom fixes come back. Root cause fixes stay fixed.
- **Not writing a regression test.** Without a test, the same bug can return later.
