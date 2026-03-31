# Experiment Logic Layer Plan

## Goal

Define `Experiment Logic` as a first-class Orchestral layer so experiment-specific composite roles, derived-state units, transforms, detectors, and control policies stop being treated as universal runtime infrastructure.

## In Scope

- add a new architecture doc for the experiment-logic layer
- define the boundary between:
  - universal runtime foundation
  - experiment logic
  - later AI orchestration
- reclassify camera-pair, Reynolds derivation, image-to-signal, and trigger logic as experiment-logic units rather than universal units
- update execution and roadmap docs so future slices are routed through the new layer

## Out Of Scope

- implementing a specific experiment-logic unit
- rewriting the existing runtime/session architecture
- adding new runtime code
- changing AI integration order

## Governing Docs

Required foundation:

- [MODULE_DEVELOPMENT_WORKFLOW.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/development/MODULE_DEVELOPMENT_WORKFLOW.md)
- [REVIEW_PROTOCOL.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/collaboration/REVIEW_PROTOCOL.md)
- [SUBAGENT_DEVELOPMENT_CONTRACT.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/collaboration/SUBAGENT_DEVELOPMENT_CONTRACT.md)
- [DEVICE_RUNTIME_IO_ARCHITECTURE.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/architecture/DEVICE_RUNTIME_IO_ARCHITECTURE.md)
- [SYSTEM_LANGUAGE_SPEC.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/architecture/SYSTEM_LANGUAGE_SPEC.md)
- [2026-03-28-functional-unit-roadmap.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/development/2026-03-28-functional-unit-roadmap.md)

Conflict winner:

- the current branch runtime architecture defines the universal/runtime boundary
- the new experiment-logic doc defines where experiment-specific units start

## Current Reality

- universal runtime foundations are now substantial:
  - session layer
  - coordinator
  - monitor
  - recorder
  - controller unit
- the next suggested unit was `camera-pair role unit`
- that unit is not actually universal; it carries experiment-specific scientific meaning
- `FlowReynoldsDerivedStateSession` already proves the same issue: it is executed on the runtime, but its logic is experiment-specific rather than universal

## Execution Steps

1. Write `EXPERIMENT_LOGIC_LAYER.md`.
2. Update runtime architecture docs to reference the new layer explicitly.
3. Update the system-language doc to define `Experiment Logic` as a first-class semantic boundary.
4. Update execution/roadmap docs so experiment-specific units move under the new layer.
5. Run docs verification and external architecture review.
6. Commit and push the docs slice.

## Success Criteria

- Orchestral now has an explicit `Experiment Logic` layer in the docs
- future experiment-specific units no longer appear as universal runtime units
- the execution track no longer claims `camera-pair role unit` is the next universal slice
- the roadmap and architecture docs point future work through the new layer

## Outcome

Implemented in this branch:

- new architecture doc:
  - `EXPERIMENT_LOGIC_LAYER.md`
- runtime and language docs now distinguish:
  - universal runtime foundation
  - experiment logic
  - later AI orchestration
- execution tracking no longer treats `camera-pair role unit` as the next universal runtime slice

Review trail:

- `2026-03-31-experiment-logic-layer-review-claude-opus-4-6.md`
- `2026-03-31-experiment-logic-layer-response.md`
- `2026-03-31-experiment-logic-layer-rereview-claude-opus-4-6.md`

Verification:

- `git diff --check`

No code build or test step was required because this slice is docs-only.
