# Module Planning Workflow

## 1. Purpose

Produce strategic decisions, architecture updates, roadmap changes, and execution-track decomposition. Shape the waitlist so the next coding or landing session has a truthful starting point.

Planning sessions decide what should be built. They do not build it.

---

## 2. When To Use

Use a planning session when:

- the roadmap needs a reset or re-baseline,
- system layering decisions are needed,
- execution tracks need decomposition into mergeable slices,
- the waitlist needs shaping or reprioritization,
- strategic prioritization is needed before coding can proceed,
- or top-down clarity is required before any implementation starts.

Do not use a planning session to start implementation. If the urge to write code appears during planning, record it as a future waitlist item instead.

---

## 3. Context Loading

Before starting, load:

- [THREAD_CONTEXT_MANAGEMENT_CONTRACT.md](./THREAD_CONTEXT_MANAGEMENT_CONTRACT.md)
- [CROSS_SESSION_WORKFLOW.md](./CROSS_SESSION_WORKFLOW.md)
- [ROADMAP_V2.md](./ROADMAP_V2.md)
- Relevant architecture docs (from `docs/architecture/` or the worktree)
- Current [CROSS_THREAD_DEV_WAITLIST.md](./CROSS_THREAD_DEV_WAITLIST.md)
- Any relevant planning-thread-context doc or handoff packet

If the planning session is a follow-up to a previous session, also load the handoff packet from that session.

---

## 4. Superpowers Skill Chain

- `$brainstorming` for exploring ideas, proposing approaches, and getting user approval on design choices.
- `$writing-plans` if the output includes a future execution plan that will feed into coding sessions.
- `$brainstorming` alone if the output is strategic docs only (roadmap, architecture, vision).

During brainstorming:

- ask clarifying questions one at a time,
- prefer multiple choice when possible,
- propose 2-3 approaches with trade-offs before settling,
- present design section by section for user approval.

---

## 5. Steps

### 5.1 Explore current state

Read code, docs, and recent git history to understand where the project actually is. Do not rely on memory or assumptions from previous sessions.

### 5.2 Ask clarifying questions

One question at a time. Prefer multiple choice. Focus on understanding purpose, constraints, and success criteria.

### 5.3 Propose approaches

Present 2-3 different approaches with trade-offs. Lead with your recommendation and explain why.

### 5.4 Present design

Once the approach is chosen, present the design section by section for user approval. Scale each section to its complexity. Ask after each section whether it looks right.

### 5.5 Write planning artifacts to files

All planning outputs must be written to files. Not just chat.

Common output locations:

- Roadmap and strategy: `docs/development_v2/`
- Architecture: `docs/architecture/`
- Execution plans: `docs/development_v2/`
- Slice plans: `docs/development_v2/` or worktree `docs/development/`

### 5.6 Run spec review if substantive

For substantive planning outputs (roadmap replacements, architecture decisions), dispatch a reviewer subagent to check the spec for completeness and consistency before finalizing.

### 5.7 Update the waitlist

Add, modify, reprioritize, or split waitlist items based on the planning decisions.

---

## 6. Cross-Thread Rules

- Planning sessions may reprioritize, add, split, and merge waitlist items.
- Planning sessions may move items to Ready For Coding.
- Planning sessions should NOT start implementation (no code edits, no branch creation).
- Planning sessions should NOT use coding skills (`$using-git-worktrees`, `$subagent-driven-development`).
- If implementation urges arise during planning, record them as future waitlist items instead.

---

## 7. Context Output

Before closing a planning session, write down:

1. **What changed** — decisions made, docs produced, architecture updated, roadmap revised
2. **What was verified** — spec review results if applicable
3. **What is still open** — deferred decisions, unresolved questions, items that need follow-up
4. **Handoff packet** — updated or created, naming the next session type
5. **What is now Ready For Coding** — explicitly stated (or a clear reason why nothing is)
6. **Waitlist update** — all new, changed, or reprioritized items reflected in the waitlist

If decisions were made that affect other docs, those docs should be updated. Do not leave important decisions as chat-only knowledge.

---

## 8. Plain-Language Git Notes

Planning sessions are usually docs-only. No branch or worktree is needed.

If docs are written directly to the local checkout, they will need a landing session to commit and push them to the shared codebase (GitHub).

Think of it as: planning writes the draft, landing publishes it.

---

## 9. Failure Modes

- **Diving into details before system structure is clear.** If the top-level boundaries still feel blurry, stop and go back up a level before continuing.
- **Treating the reference experiment as the architecture.** Turbulence is a validation case, not the source of truth for platform design.
- **Using chat as the only memory.** If a decision lives only in chat, it will be lost when the session ends. Write it to a file.
- **Designing work packages too large to reduce to mergeable slices.** If a planned item cannot be explained, reviewed, and merged as one coherent unit, it needs to be split further.
- **Starting without loading the current waitlist.** This leads to duplicate or contradictory items.
- **Mixing planning and coding.** "It is all related" is the warning sign. Planning shapes the queue. Coding executes one item from it.
