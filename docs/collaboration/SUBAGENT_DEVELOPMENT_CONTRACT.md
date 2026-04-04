# Sub-Agent Development Contract

## Purpose

This document defines how sub-agents should be used during Orchestral development.

Sub-agents exist to reduce ambiguity and increase throughput on bounded work.
They do not replace the main development workflow and they do not own final architectural truth.

Read this together with:

- [MODULE_DEVELOPMENT_WORKFLOW.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/development/MODULE_DEVELOPMENT_WORKFLOW.md)
- [REVIEW_PROTOCOL.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/collaboration/REVIEW_PROTOCOL.md)

## 1. Position In The Workflow

Sub-agents are used inside the module workflow, not instead of it.

Typical placement:

1. clarify slice and foundation docs
2. inspect current reality
3. write or select execution plan
4. implement with verification
5. review
6. land

Sub-agents are most useful in steps:

- 1: bounded external fact gathering when local docs are insufficient,
- 2: inspection
- 4: implementation
- 5: review

The main agent remains responsible for:

- defining the slice,
- choosing the architecture,
- integrating results,
- deciding on review findings,
- final landing.

## 2. Sub-Agent Task Types

### Explorer

Use for:

- current-code inspection,
- bounded architecture questions,
- locating ownership and dependencies,
- identifying file targets and risks.

Expected output:

- findings about current truth,
- file list,
- risks,
- no broad implementation.

### Researcher

Use for:

- bounded web fetch or browse tasks,
- vendor manual or SDK discovery,
- protocol or standards lookup,
- confirming facts that are missing, stale, or uncertain in local docs,
- collecting source-backed evidence before implementation.

Default channel:

- Gemini CLI when available as a local research tool.

Expected output:

- source links,
- key claims found,
- dates or version identifiers when relevant,
- unresolved uncertainty,
- implementation impact summary,
- no code edits.

### Worker

Use for:

- bounded implementation with clear file ownership,
- tests for one slice,
- contained refactors.

Expected output:

- code changes in owned files,
- verification results,
- assumptions and unresolved risks.

### Reviewer

Use for:

- adversarial review,
- contract mismatch detection,
- verification of claims,
- runtime or behavior checks.

Expected output:

- concrete findings only,
- no silent architecture replacement.

## 3. Required Delegation Package

Every sub-agent task must include:

- task goal,
- why this task exists,
- required foundation docs,
- exact owned files or modules,
- explicit non-owned files if relevant,
- expected output,
- required verification,
- whether web fetch or browsing is allowed or required,
- artifact path for review notes if applicable,
- whether edits are allowed,
- whether commit is expected.

Bad delegation:

- "work on runtime IO"

Good delegation:

- "Inspect where the microphone panel currently owns live-loop state, list exact files involved, and identify the smallest seam for moving that ownership into a runtime session. Do not edit code."
- "Using Gemini CLI, find the official HuaTeng SDK/manual pages for trigger mode and frame-rate constraints, return links plus the exact limits confirmed, and state what remains uncertain. Do not edit code."

## 4. Ownership Rules

Workers should have disjoint write scopes whenever possible.

Required rules:

- one worker, one bounded responsibility,
- no silent out-of-scope refactors,
- no reverting others' work,
- no edits outside owned files unless explicitly approved,
- if ownership becomes unclear, stop and escalate.

## 5. Worktree And Branch Rules

Use isolated worktrees or branches when:

- the slice is substantial,
- the slice is risky,
- multiple workers may edit in parallel,
- or landing requires review isolation.

Reuse the current branch only when:

- the change is small,
- the write scope is tightly bounded,
- and conflict risk is low.

## 6. Verification Rules

No sub-agent task is complete without explicit verification.

Verification must state:

- what commands were run,
- what passed,
- what failed,
- what was not verified.

If no verification was possible, that must be stated plainly.

For research tasks, verification must also state:

- what search or fetch command was run,
- which sources were actually opened,
- whether the sources were official, vendor-provided, or secondary,
- and what could not be confirmed.

## 7. External Review Handling

If a slice requires external review under the module workflow rules:

- the main agent prepares the review package,
- the configured external reviewer channel is used if available,
- otherwise a human-triggered external review is requested,
- the results are stored under `docs/collaboration/reviews/`.

If Claude Code CLI is available as a reviewer channel, the default mode should be:

- direct CLI invocation
- non-interactive
- one bounded run per review task
- one separate run per re-review task

This is preferred over one long-lived interactive reviewer session for normal review work.

Important:

- do not pretend an external reviewer was called directly if no tool integration exists,
- do not paraphrase findings without creating the review artifact.

## 8. Gemini CLI As A Research Sub-Agent

Gemini CLI may be used as a dedicated research sub-agent for web fetch and browsing work.

Use it only when:

- local governing docs have already been read,
- local code inspection is complete enough to frame the question,
- and important facts are still missing, stale, vendor-specific, or externally defined.

Preferred uses:

- vendor protocol discovery,
- SDK/manual lookup,
- hardware capability confirmation,
- standards lookup,
- source-backed research for device integration.

Preferred mode:

- one bounded non-interactive run per research question,
- one artifact or summary per task,
- source-backed output with links and explicit uncertainty,
- no code editing through the Gemini research task.

Do not use Gemini CLI:

- as a replacement for reading local architecture docs,
- as a default step on every slice,
- as an uncited fact generator,
- or as a general implementation worker.

If Gemini-derived facts materially affect implementation, the main agent must:

- incorporate the relevant sources into the slice notes, plan, or review artifact,
- state the impact on the implementation decision,
- and keep the final architectural judgment local.

## 9. Integration Rules

The main agent must:

- review returned changes,
- integrate or reject them,
- resolve conflicts between worker outputs,
- maintain architecture coherence,
- decide when the slice is ready to land.

Sub-agents contribute slices.
They do not define final system truth.

## 10. Anti-Patterns

Do not:

- delegate before the slice is clarified,
- delegate vague cross-cutting work,
- use sub-agents for tiny local tasks,
- use multiple workers on overlapping files without need,
- treat review comments as architecture decisions automatically,
- treat Gemini browse output as authoritative without source review,
- use web fetch before reading local governing docs,
- claim "parallelization" while creating merge conflict debt.

## 11. Minimal Task Templates

### Explorer Task

- Goal:
- Why:
- Required foundation docs:
- Scope:
- Non-scope:
- Output required:
- Edit permission: no
- Verification required:

### Worker Task

- Goal:
- Why:
- Required foundation docs:
- Owned files:
- Non-owned files:
- Output required:
- Edit permission: yes
- Verification required:
- Commit expected: yes or no

### Reviewer Task

- Goal:
- Why:
- Required foundation docs:
- Scope:
- Output required:
- Edit permission: no unless explicitly requested
- Review artifact path:
- Verification required:

### Researcher Task

- Goal:
- Why:
- Required foundation docs already read:
- Research question:
- Allowed source types:
- Output required:
- Edit permission: no
- Verification required:
