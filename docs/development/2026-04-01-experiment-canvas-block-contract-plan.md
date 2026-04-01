# Experiment Canvas Block Contract Plan

## Goal

Define the V1 structure of one experiment-canvas block so future canvas work does not guess at block ownership, internal role shape, source-mode handling, or readiness semantics.

## In Scope

- create the block-contract architecture doc
- sync the experiment-canvas architecture doc to that contract
- sync the system language spec to the same V1 rules

## Out Of Scope

- canvas UI layout
- editor interactions
- implementation of the canvas itself
- runtime control-panel behavior

## Governing Docs

Required foundation:

- [EXPERIMENT_CANVAS_ARCHITECTURE.md](../architecture/EXPERIMENT_CANVAS_ARCHITECTURE.md)
- [SYSTEM_LANGUAGE_SPEC.md](../architecture/SYSTEM_LANGUAGE_SPEC.md)
- [EXPERIMENT_LOGIC_LAYER.md](../architecture/EXPERIMENT_LOGIC_LAYER.md)
- [VIRTUAL_TWIN_AND_SIMULATION_STRATEGY.md](../architecture/VIRTUAL_TWIN_AND_SIMULATION_STRATEGY.md)

## Current Decision

The approved V1 rules are:

- one block represents one experiment function
- one block may contain multiple internal roles
- one role has exactly one active implementation
- one implementation has exactly one source mode
- the block shows readiness only, not runtime actions
- the block uses a traffic-light readiness indicator with reasons

## Verification

- docs-only verification with `git diff --check`

## Landing Target

- commit on the current Orchestral docs branch
- push to `origin/codex/runtime-io-microphone`
