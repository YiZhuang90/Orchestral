# V1 Execution Track

## Purpose

This document is the operational index for V1 execution inside the current Orchestral branch.

Use:

- [ROADMAP_V1.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/development/ROADMAP_V1.md) for strategic stage order,
- this file for active execution sequencing,
- and slice-specific execution plans for concrete implementation work.

## Current Execution Order

The intended V1 order is:

1. legacy knowledge extraction and structured inventories,
2. artifact/schema foundation,
3. protocol and device model foundation,
4. runtime and safety foundation,
5. first reference experiment through the new runtime,
6. hardening and broader device-family generalization,
7. AI brain integration after the core runtime is real.

## Active Execution Tracks

### 1. Runtime And Safety Foundation

Status:

- shared runtime session layer exists,
- operator controls exist,
- session-local validation exists,
- runtime coordinator and stop authority exist,
- experiment definition and role binding now exist as a validated artifact/runtime boundary,
- high-rate stream buffering and delivery policy now exists for runtime stream ports and the highest-pressure UI consumers,
- run context and metadata now exist as a first-class experiment-plane unit,
- run recorder and artifact writer now exist as a first-generation manifest/panel-artifact persistence layer,
- next universal slice is run monitor and alarm surface.

Required foundations:

- [DEVICE_RUNTIME_IO_ARCHITECTURE.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/architecture/DEVICE_RUNTIME_IO_ARCHITECTURE.md)
- [TIMING_AND_SYNCHRONIZATION_STRATEGY.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/architecture/TIMING_AND_SYNCHRONIZATION_STRATEGY.md)
- [SYSTEM_LANGUAGE_SPEC.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/architecture/SYSTEM_LANGUAGE_SPEC.md)

Active slice plans:

- [2026-03-28-session-local-validation-plan.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/development/2026-03-28-session-local-validation-plan.md)
- [2026-03-28-runtime-coordinator-and-stop-authority-plan.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/development/2026-03-28-runtime-coordinator-and-stop-authority-plan.md)
- [2026-03-30-experiment-definition-and-role-binding-plan.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/development/2026-03-30-experiment-definition-and-role-binding-plan.md)
- [2026-03-30-high-rate-stream-buffering-and-delivery-policy-plan.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/development/2026-03-30-high-rate-stream-buffering-and-delivery-policy-plan.md)
- [2026-03-30-run-context-and-metadata-plan.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/development/2026-03-30-run-context-and-metadata-plan.md)
- [2026-03-30-run-recorder-and-artifact-writer-plan.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/development/2026-03-30-run-recorder-and-artifact-writer-plan.md)

### 2. First Reference Experiment

Status:

- partially unblocked,
- still waiting on:
  - run monitor/alarm surface,
  - experiment-specific controller sessions as required by the chosen reference experiment.

### 3. AI Brain Integration

Status:

- intentionally deferred until the runtime and first reference experiment are real.

## Rules

- Do not start a new execution plan without listing it here.
- Do not treat architecture docs as execution plans.
- Do not move AI brain integration ahead of runtime and first-reference-experiment foundation.
- If a slice changes the shared architecture, update this track after the slice lands.
