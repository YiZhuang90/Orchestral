# Review Protocol

## Purpose

This protocol defines how Orchestral implementation agents, external reviewers, and external testers communicate.

The goal is:
- evidence over intent
- file-backed review loops instead of chat paraphrases
- clear separation between findings, decisions, fixes, and retests

## Core Rules

1. Review the actual code and actual behavior, not the intended design.
2. Use file-backed artifacts under `docs/collaboration/` or `docs/collaboration/reviews/`.
3. Every finding must be concrete, reproducible, and scoped.
4. Separate blocking issues from non-blocking cleanup.
5. The implementer must explicitly accept, reject, or partially accept each finding.
6. A fixed finding is not closed until it is retested.

## Source Priority

When reviewing or debugging:
1. actual code and actual runtime behavior
2. official vendor or framework documentation
3. repository contracts and architecture docs
4. external/community sources only if the official material is missing or insufficient

## Artifact Types

### Review file

Use for external reviewer or tester output.

Recommended path:
- `docs/collaboration/reviews/YYYY-MM-DD-<topic>-review.md`

### Response file

Use for implementer decisions and fix status.

Recommended path:
- `docs/collaboration/reviews/YYYY-MM-DD-<topic>-response.md`

## Required Finding Format

Each finding must include:
- `ID`
- `Severity`
- `Area`
- `File` or runtime surface
- `Evidence`
- `Expected`
- `Actual`
- `Why it matters`
- `Recommended fix`

Severity levels:
- `P0`: data loss, safety issue, crash, or invalid experiment behavior
- `P1`: major functional or semantic bug
- `P2`: moderate bug, misleading UI, contract mismatch, or regression risk
- `P3`: cleanup or non-blocking quality issue

## Required Testing Format

If the reviewer or tester executed behavior, they must report:
- what was clicked/run
- what internal signal was checked
- what passed
- what failed
- what was not verified

Examples of internal signals:
- command issued
- state transition
- driver/session state
- frame/value arrival
- output object update
- status or diagnostics update

## Required Implementer Response Format

For each finding:
- `Accepted`
- `Rejected`
- `Partially accepted`

If accepted or partially accepted, include:
- fix summary
- changed files
- retest status

If rejected, include:
- technical reason
- why the finding is incorrect, out of scope, or superseded

## Best Division Of Labor

External reviewer/tester:
- adversarial review
- runtime testing
- reproduction
- evidence collection

Implementer:
- architecture decisions
- code changes
- integration
- contract consistency
- retest and close-out

## Anti-Patterns

Do not:
- pass chat summaries back and forth as the primary review record
- report “looks wrong” without evidence
- mark a finding fixed without retest
- mix blocking bugs with minor style notes as if they are equal
- describe staged or draft state as applied or verified state

## Relationship To Other Docs

- architecture rules stay in `docs/architecture/`
- handover verification rules stay in `docs/architecture/TEST_BEFORE_HANDOVER.md`
- this file governs reviewer and implementer communication around those checks
