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
- controller-unit session semantics and first implementation now exist for experiment-level control targets,
- run monitor and alarm surface now exists as:
  - a background `ExperimentMonitorSession`,
  - an experiment monitor panel/client surface using the shared panel shell,
  - first warning/alarm aggregation,
  - runtime-backed `Target +/- Error` presentation,
  - and a display-only live-view path with explicit decimation choices,
- flow/Reynolds derived-state and measured-stream wiring now exists as:
  - a runtime-owned `FlowReynoldsDerivedStateSession`,
  - derived Reynolds/flow/temperature state from control-center pulse telemetry and PT-104 samples,
  - measured-value wiring into `ControllerUnitSession`,
  - monitor-side derived-state presentation and stale-state warnings,
  - and run-artifact persistence for derived-state snapshot/record outputs,
- control-center capability decomposition now exists as:
  - explicit `LaserControl`, `PuffActuation`, and `FlowTelemetry` runtime capability surfaces above the mixed serial protocol,
  - capability-oriented command helpers that still preserve one truthful transport write path,
  - and panel/applied-settings semantics that no longer record the control-center as one vague command lump,
- cross-session validation now exists as:
  - a structured `CrossSessionValidationResult` above the resolved experiment package,
  - first reusable rules for ambiguous shared-device capability ownership and conflicting control-target command-role ownership,
  - runtime-coordinator enforcement before run start,
  - and initialize-time surfacing in the experiment monitor panel so invalid runs stay blocked before hardware work begins,
- experiment-definition linting now exists as:
  - a structured `ExperimentDefinitionLintResult` on the authored experiment package,
  - first reusable static rules for duplicate definitions and broken stream, role, and parameter references,
  - initialize-time enforcement before cross-session validation or hardware start,
  - and experiment monitor surfacing so authored-package blockers are visible as pre-flight alarms,
- universal runtime is now strong enough that the next missing work should not be framed as new first-generation runtime creation,
- next shared guidance slice is formalizing the `Experiment Logic` layer that sits above runtime and below later AI orchestration.

Required foundations:

- [DEVICE_RUNTIME_IO_ARCHITECTURE.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/architecture/DEVICE_RUNTIME_IO_ARCHITECTURE.md)
- [TIMING_AND_SYNCHRONIZATION_STRATEGY.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/architecture/TIMING_AND_SYNCHRONIZATION_STRATEGY.md)
- [SYSTEM_LANGUAGE_SPEC.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/architecture/SYSTEM_LANGUAGE_SPEC.md)
- [EXPERIMENT_LOGIC_LAYER.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/architecture/EXPERIMENT_LOGIC_LAYER.md)

Active slice plans:

- [2026-03-28-session-local-validation-plan.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/development/2026-03-28-session-local-validation-plan.md)
- [2026-03-28-runtime-coordinator-and-stop-authority-plan.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/development/2026-03-28-runtime-coordinator-and-stop-authority-plan.md)
- [2026-03-30-experiment-definition-and-role-binding-plan.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/development/2026-03-30-experiment-definition-and-role-binding-plan.md)
- [2026-03-30-high-rate-stream-buffering-and-delivery-policy-plan.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/development/2026-03-30-high-rate-stream-buffering-and-delivery-policy-plan.md)
- [2026-03-30-run-context-and-metadata-plan.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/development/2026-03-30-run-context-and-metadata-plan.md)
- [2026-03-30-run-recorder-and-artifact-writer-plan.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/development/2026-03-30-run-recorder-and-artifact-writer-plan.md)
- [2026-03-30-controller-unit-session-plan.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/development/2026-03-30-controller-unit-session-plan.md)
- [2026-03-31-run-monitor-and-alarm-surface-plan.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/development/2026-03-31-run-monitor-and-alarm-surface-plan.md)
- [2026-03-31-flow-reynolds-derived-state-and-measured-stream-wiring-plan.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/development/2026-03-31-flow-reynolds-derived-state-and-measured-stream-wiring-plan.md)
- [2026-03-31-experiment-logic-layer-plan.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/development/2026-03-31-experiment-logic-layer-plan.md)
- [2026-03-31-cross-session-validation-plan.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/development/2026-03-31-cross-session-validation-plan.md)
- [2026-03-31-experiment-definition-linting-plan.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/development/2026-03-31-experiment-definition-linting-plan.md)

### 2. Experiment Logic Layer

Status:

- now formalized as the layer above universal runtime and below later AI orchestration,
- intended for experiment-specific units such as:
  - camera-pair semantics,
  - Reynolds derivation,
  - image-to-signal transforms,
  - detection/classification,
  - experiment-specific control and monitor logic,
- next implementation slices for the first reference experiment should route through this layer instead of being misclassified as universal runtime work.

Required foundations:

- [DEVICE_RUNTIME_IO_ARCHITECTURE.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/architecture/DEVICE_RUNTIME_IO_ARCHITECTURE.md)
- [SYSTEM_LANGUAGE_SPEC.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/architecture/SYSTEM_LANGUAGE_SPEC.md)
- [EXPERIMENT_LOGIC_LAYER.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/architecture/EXPERIMENT_LOGIC_LAYER.md)

### 3. Universal Hardening Before Experiment Logic Proof

Status:

- the first-generation universal runtime foundation is built enough to stop adding new generic core slices by reflex,
- the remaining universal work is now:
  - `Replay / simulator harness`,
- `Control-center capability decomposition`, `Cross-session validation`, and `Experiment-definition linting` are now complete,
- after those are complete, the next step should be a minimum code-level proof of the experiment-logic layer rather than another broad runtime rewrite.

Guiding plan:

- [2026-03-31-universal-completion-and-experiment-logic-proof-plan.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/development/2026-03-31-universal-completion-and-experiment-logic-proof-plan.md)
- [2026-03-31-control-center-capability-decomposition-plan.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/development/2026-03-31-control-center-capability-decomposition-plan.md)

As each hardening unit lands, update both:

- this section's sequencing note,
- and section `1. Runtime And Safety Foundation` if the shared runtime status bullets changed.

### 4. First Reference Experiment

Status:

- partially unblocked,
- still waiting on:
  - experiment-logic units such as camera-pair semantics and experiment-specific processing,
  - experiment-specific controller sessions as required by the chosen reference experiment.

### 5. AI Brain Integration

Status:

- intentionally deferred until the runtime and first reference experiment are real.

## Rules

- Do not start a new execution plan without listing it here.
- Do not treat architecture docs as execution plans.
- Do not move AI brain integration ahead of runtime and first-reference-experiment foundation.
- If a slice changes the shared architecture, update this track after the slice lands.
