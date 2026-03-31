# Experiment Canvas And Virtual Twin Architecture Plan

## Goal

Define the written architecture for:

- a high-level experiment canvas based on experiment-function blocks,
- the three-layer canvas model,
- and the virtual-twin/simulation strategy that closes device integration work.

## In Scope

- create the experiment-canvas architecture doc
- create the virtual-twin and simulation strategy doc
- update shared language, runtime, workflow, integration, and execution docs
- rename the remaining universal hardening unit to reflect virtual twins plus replay/simulation

## Out Of Scope

- canvas UI details
- exact block layout or editor interaction design
- implementing virtual twins or replay/simulation code
- changing panel-level runtime behavior

## Governing Docs

Required foundation:

- [MODULE_DEVELOPMENT_WORKFLOW.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/development/MODULE_DEVELOPMENT_WORKFLOW.md)
- [DEVICE_RUNTIME_IO_ARCHITECTURE.md](../architecture/DEVICE_RUNTIME_IO_ARCHITECTURE.md)
- [SYSTEM_LANGUAGE_SPEC.md](../architecture/SYSTEM_LANGUAGE_SPEC.md)
- [EXPERIMENT_LOGIC_LAYER.md](../architecture/EXPERIMENT_LOGIC_LAYER.md)
- [V1_EXECUTION_TRACK.md](./V1_EXECUTION_TRACK.md)

Supporting reference:

- [AI_GUIDED_HARDWARE_INTEGRATION_SPEC.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/architecture/AI_GUIDED_HARDWARE_INTEGRATION_SPEC.md)
- [2026-03-28-functional-unit-roadmap.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/development/2026-03-28-functional-unit-roadmap.md)

Conflict winner:

- the branch-local architecture docs for current runtime and experiment-layer boundaries

## Current Reality

- experiment logic is now defined as a layer above universal runtime
- the system language has strong nouns for roles, bindings, streams, monitors, and control targets
- the project still lacks written architecture for a high-level experiment canvas
- the project still treats replay/simulation mostly as a generic harness instead of as the closure path for integrated devices

## Verification

- docs-only verification with `git diff --check`

## Landing Target

- commit on `codex/runtime-io-microphone`
- push to `origin/codex/runtime-io-microphone`
