# Module Landing Workflow

## 1. Purpose

Merge a completed and reviewed slice into the shared codebase. This session type handles verification, sync, merge, push, and cleanup.

Landing is integration and closure work. It is not more feature development.

---

## 2. When To Use

Use a landing session when:

- a waitlist item is in Ready To Land,
- implementation, verification, and review are all done,
- the remaining work is the merge decision and execution.

Do not use a landing session to add features, fix newly discovered bugs, or do architectural work. Those belong to coding, debug, or planning sessions.

---

## 3. Context Loading

Before starting, load:

- [THREAD_CONTEXT_MANAGEMENT_CONTRACT.md](./THREAD_CONTEXT_MANAGEMENT_CONTRACT.md)
- [CROSS_SESSION_WORKFLOW.md](./CROSS_SESSION_WORKFLOW.md)
- The waitlist item being landed
- The item's handoff packet
- Review and verification artifacts
- Current branch and worktree state

---

## 4. Superpowers Skill Chain

- `$finishing-a-development-branch` — the primary skill for landing decisions.
- `$verification-before-completion` — always run fresh verification before claiming landing is done. Do not trust cached test results.

---

## 5. Steps

### Step 1: Fresh verification

Run tests on the slice branch. Do not trust cached results from a previous session. Run them again now.

If tests fail, stop. This is a debug situation, not a landing situation. Record the failure and hand off to a debug session.

### Step 2: Determine the base branch

Usually `main`. Confirm with the waitlist item or handoff packet.

### Step 3: Sync check

Is your local copy of the base branch up to date with the shared version on GitHub?

If not, sync it first. In plain language: make sure your "official book" has all the latest chapters before you try to add a new one.

### Step 4: Merge

Apply the slice branch onto the base branch.

If the merge is clean (no conflicts), proceed.

If there are conflicts (two changes to the same part of a file), resolve them. See the Git glossary below for what this means.

### Step 5: Re-verify

If the merge changed anything beyond a clean stack, run tests again on the merged result. The isolated slice may have passed, but the combination might not.

### Step 6: Choose landing path

- **Merge locally and push** — simplest for small slices where review is already done.
- **Push and create a PR** — better when you want a review checkpoint visible on GitHub.
- **Keep as-is** — if landing should wait for some reason.
- **Discard** — rare. Only if the work is no longer needed.

### Step 7: Execute the chosen path

Follow through on the decision from Step 6.

### Step 8: Clean up

- Delete the slice branch if it is no longer needed.
- Delete the worktree if it was used for this slice.
- Reducing clutter makes future sessions cleaner.

### Step 9: Update waitlist

Move the item to Landed. Record the merge commit and date.

---

## 6. Cross-Thread Rules

- Landing sessions should NOT add new features or fix new bugs discovered during landing.
- If a merge conflict reveals a new issue, record it as a new waitlist item for a future coding or debug session.
- Landing is closure, not continuation.
- One landing session = one waitlist item landed.

---

## 7. Context Output

Before closing a landing session, write down:

1. **What changed** — what was merged, which commit, which base branch
2. **What was verified** — test results on the merged result (not just the isolated slice)
3. **What is still open** — any follow-up items discovered during landing
4. **Handoff packet** — updated with merge commit hash, cleanup status
5. **Next session type** — usually the next coding session, or planning if the queue is empty
6. **Waitlist update** — item moved to Landed, any new items added

---

## 8. Plain-Language Git Notes

### Glossary

- **Syncing** — downloading the latest shared version of the project before you try to add your work to it. Like checking that you have the latest edition of a shared book before taping in a new chapter.

- **Merge** — combining your work with the shared version. If nobody else changed the same files, this is automatic. If they did, you need to decide which changes to keep.

- **Merge conflict** — two people changed the same part of a file differently. The system cannot decide automatically which version is correct, so you need to choose. This is normal and not an error.

- **Fast-forward** — the simplest kind of merge. Your work stacks cleanly on top of the latest shared version because nobody else changed anything in between. No conflict possible.

- **Pull request (PR)** — a request on GitHub asking whether your draft chapter should be added to the official book. Other people or AI reviewers can check it before it joins. Useful when you want a visible checkpoint.

- **Branch cleanup** — deleting the branch after its work has been merged. Like throwing away the sticky note that said "draft chapter in progress" after the chapter is published.

- **Worktree cleanup** — deleting the second desk (worktree) after the work has been merged back into the main copy. Reduces clutter on your machine.

---

## 9. Failure Modes

- **Landing untested work.** Always run fresh verification before merging. Never trust "it passed last time."
- **Landing without syncing.** If your local copy is behind the shared version, the merge may silently diverge or fail on push.
- **Mixing new features into landing.** If you discover something new during landing, record it as a waitlist item. Do not fix it in the landing session.
- **Leaving branches and worktrees around.** After landing, clean up. Accumulated clutter makes future sessions confusing.
- **Forgetting to update the waitlist.** The queue becomes stale if landed items are not moved.
- **Not re-verifying after a non-trivial merge.** The merged result may differ from the isolated slice. Test it.
