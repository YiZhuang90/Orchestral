# Reviewer System Comments - 2026-03-28

Author: Claude Code (external reviewer)

These comments consolidate architectural guidance from the 2026-03-28 review session. They are useful as system-level input for future Orchestral development, but they are not all equally urgent. This file reflects the current engineering judgment after comparing the review comments against the actual repo state and review-response trail.

---

## 1. Keep Product Workflow and Developer Workflow Separate

Do not merge:

- `AI_GUIDED_HARDWARE_INTEGRATION_SPEC.md`
- `SUBAGENT_DEVELOPMENT_CONTRACT.md`

They serve different audiences and should remain separate.

- `AI_GUIDED_HARDWARE_INTEGRATION_SPEC.md` is a product workflow spec.
  It describes how Orchestral, as a running system, should guide device onboarding and integration work.
- `SUBAGENT_DEVELOPMENT_CONTRACT.md` is a developer process spec.
  It describes how implementation work is delegated and reviewed during development.

The connection between them already exists through `MODULE_DEVELOPMENT_WORKFLOW.md`, which references the sub-agent contract as part of the development workflow.

Verdict:

- keep separate
- no merge needed

---

## 2. MODULE_DEVELOPMENT_WORKFLOW.md Is Already General Enough

The six-step workflow:

1. Clarify
2. Inspect
3. Plan
4. Implement
5. Review
6. Land

is already general enough for:

- runtime-session slices
- UI slices
- computation or analysis slices
- experiment-package slices
- AI-layer slices

One minor improvement is still reasonable:

- add one non-runtime example to section `1.1 Required Foundation Docs`

That would make the generality more obvious, but no structural rewrite is needed.

Verdict:

- keep the document structure
- optional improvement only

---

## 3. Cross-Parameter Validation Is The Right Direction, But Do Not Overbuild It Yet

The core problem is real:

- device settings can have hidden dependencies
- output settings can contradict actual capture behavior
- future multi-device experiment setups will introduce cross-session constraints

The proposed direction is sound:

### Level 1: Session-local validation

This should be the next real step.

Each runtime session should be able to validate its own settings before apply.

Recommended shape:

```text
ValidateSettings(settings) -> ValidationResult
```

Examples:

- periodic output frequency must be positive
- periodic output frequency must not exceed actual meaningful capture/update rate
- `OnChange` should warn that output frequency is irrelevant
- device-specific parameter contradictions should be caught before apply

This belongs in the session layer first.

### Level 2: Cross-session validation

This is a later coordinator/orchestration concern.

Examples:

- trigger rate mismatch between controller and camera
- cross-device safety or timing preconditions
- shared-trigger or shared-clock incompatibilities

These rules should not be hardcoded prematurely into the registry without real experiment definitions.

### Level 3: Experiment-definition linting

This is a later experiment-package concern.

A static linter for experiment definitions is a good long-term direction, but it should come after:

- real session validation exists
- coordinator behavior exists
- experiment-definition artifacts are real enough to lint

### Current engineering judgment

The important takeaway is:

- yes, add validation
- no, do not jump straight to a full shared `SettingsConstraint` engine

The safe sequence is:

1. add session-local validation seam
2. implement it in a few real sessions
3. compare the real cases
4. then decide whether a shared evaluator/constraint-data model is justified

Verdict:

- adopt the validation direction
- defer the full generic constraint engine until more real cases exist

---

## 4. Functional Units Before System-Level Design Is Still Correct

The current execution order remains sound:

1. Runtime and Safety Foundation
2. Device Session Migration
3. First Reference Experiment
4. AI Brain Integration

Why this order works:

- the architecture docs are now strong enough to constrain implementation
- real sessions expose interface truth that documents alone cannot predict
- the coordinator/orchestration layer should be informed by actual per-session behavior
- the AI layer should come after the core runtime and experiment workflow are real

The specific unit-first logic is correct:

- build session contracts
- build real device sessions
- validate operator controls
- validate stop behavior
- then move upward to coordinator/global behavior

Verdict:

- keep the execution order
- do not jump to coordinator/global parameter design too early

---

## 5. Review Status Note

The original reviewer note claimed some earlier findings were still open. That is no longer true as written.

Current status:

- the panel-contract baseline/audio review already has a response artifact in the worktree:
  - `C:/Users/Yi Zhuang/.config/superpowers/worktrees/Orchestral/panel-contract-enforcement/docs/collaboration/reviews/2026-03-25-panel-contract-baseline-audio-response.md`
- the runtime-IO architecture review already has a formal response artifact in the main repo:
  - `C:/Users/Yi Zhuang/OneDrive/Codes/Projects/Orchestral/docs/collaboration/reviews/2026-03-26-runtime-io-architecture-response.md`

So this file should not be treated as the authoritative source for which historical findings are still open.

If open-review status is needed in the future, it should be maintained from:

- the actual review artifact
- the actual response artifact
- the actual re-review artifact

not from a narrative summary file like this one.

Verdict:

- treat prior "open findings" language as stale
- rely on review/response/re-review artifacts directly

---

## Summary

- Keep `AI_GUIDED_HARDWARE_INTEGRATION_SPEC.md` and `SUBAGENT_DEVELOPMENT_CONTRACT.md` separate.
- `MODULE_DEVELOPMENT_WORKFLOW.md` is already general enough.
- Add validation to the session layer, but do not overbuild a generic constraint engine yet.
- Keep the current functional-unit-first execution order.
- Do not use this file as the source of truth for historical open review findings; use the actual review artifacts instead.
