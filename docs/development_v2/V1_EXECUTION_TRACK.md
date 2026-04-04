# V1 Execution Track

## Purpose

This document is the operational index for V1 execution inside the current Orchestral branch.

Use:

- [ROADMAP_V2.md](./ROADMAP_V2.md) for strategic direction and system layering,
- this file for active execution sequencing,
- and slice-specific execution plans for concrete implementation work.

Note: This file previously referenced ROADMAP_V1.md, which has been superseded by ROADMAP_V2.

## Execution Model

V1 should be executed through:

1. one high-level roadmap,
2. one execution track,
3. multiple slice-specific execution plans.

This prevents two failure modes:

- one giant execution doc that becomes stale,
- many isolated plan files with no central status view.

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

### 1. Runtime and Safety Foundation

Status:

- architecture defined
- runtime-I/O section plan written
- next explicit slice is operator-facing runtime controls before coordinator/orchestration

Required foundations:

- [DEVICE_RUNTIME_IO_ARCHITECTURE.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/architecture/DEVICE_RUNTIME_IO_ARCHITECTURE.md)
- [TIMING_AND_SYNCHRONIZATION_STRATEGY.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/architecture/TIMING_AND_SYNCHRONIZATION_STRATEGY.md)
- [INTEGRATION_PANEL_IO_CONTRACT.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/architecture/INTEGRATION_PANEL_IO_CONTRACT.md)
- [DEVICE_PANEL_CONTRACT.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/architecture/DEVICE_PANEL_CONTRACT.md)

Next slice plans:

- [2026-03-27-runtime-io-execution-plan.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/development/2026-03-27-runtime-io-execution-plan.md)
- [2026-03-27-runtime-operator-controls-plan.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/development/2026-03-27-runtime-operator-controls-plan.md)

### 2. Device Session Migration

Status:

- first acquisition pilot exists
- first controlled-device pilot exists
- PT-104 and camera-family migrations are planned/under branch execution

Planned order:

1. integrated microphone
2. first controlled-device session
3. PT-104
4. camera family

Immediate next step before coordinator/orchestration:

- runtime operator-controls slice
  - output settings
  - controlled-device emergency stop
  - apply-and-exit session behavior

### 3. First Reference Experiment

Status:

- not yet execution-ready

Blocked on:

- runtime session layer landing,
- operator-controls slice,
- experiment-package structure becoming concrete.

### 4. AI Brain Integration

Status:

- architecture and candidate evaluation exist
- execution intentionally deferred until late V1

Required foundations:

- stable runtime session layer,
- structured run artifacts,
- first reference experiment working through the runtime,
- collaboration/review workflow stable.

## Rules

- Do not start a new execution plan without listing it here.
- Do not treat architecture docs as execution plans.
- Do not move AI brain integration ahead of the runtime and reference-experiment foundation.
- If a slice changes the shared architecture, update this track after the slice lands.

## Related Docs

- [DEVELOPMENT_STRATEGY.md](./DEVELOPMENT_STRATEGY.md)
- [MODULE_DEVELOPMENT_WORKFLOW.md](./MODULE_DEVELOPMENT_WORKFLOW.md)
- [2026-03-28-functional-unit-roadmap.md](../development/2026-03-28-functional-unit-roadmap.md) (archived — superseded by ROADMAP_V2)
- [SUBAGENT_DEVELOPMENT_CONTRACT.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/collaboration/SUBAGENT_DEVELOPMENT_CONTRACT.md)
