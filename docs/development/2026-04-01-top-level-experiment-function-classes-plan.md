# Top-Level Experiment Function Classes Plan

## Goal

Refine the experiment-canvas architecture so the top level reflects a general system pattern rather than turbulence-specific names.

## In Scope

- define the general top-level experiment-function classes
- sync the canvas block contract to carry a `function_class`
- sync the system language spec so the same classes are canonical nouns
- keep turbulence only as an example mapping

## Out Of Scope

- canvas UI implementation
- recursive lower-level block kinds
- runtime monitor/control-panel behavior
- turbulence-specific algorithm design

## Governing Docs

Required foundation:

- [EXPERIMENT_CANVAS_ARCHITECTURE.md](../architecture/EXPERIMENT_CANVAS_ARCHITECTURE.md)
- [EXPERIMENT_CANVAS_BLOCK_CONTRACT.md](../architecture/EXPERIMENT_CANVAS_BLOCK_CONTRACT.md)
- [SYSTEM_LANGUAGE_SPEC.md](../architecture/SYSTEM_LANGUAGE_SPEC.md)
- [EXPERIMENT_LOGIC_LAYER.md](../architecture/EXPERIMENT_LOGIC_LAYER.md)

## Current Decision

The top-level general pattern should use these experiment-function classes:

- `condition_control`
- `core_experiment_function`
- `order_parameter`
- `data_recording`

The following should stay out of top-level experiment-function classification:

- monitor surfaces
- device lifecycle / init / terminate behavior
- generic runtime orchestration

Calibration and reference logic should normally stay nested under one of the top-level classes rather than being promoted to top level by default.

## Verification

- docs-only verification with `git diff --check`

## Landing Target

- commit on the current Orchestral docs branch
- push to `origin/codex/runtime-io-microphone`
