# Roadmap V1

## Superseded

This document has been superseded by [ROADMAP_V2.md](../development_v2/ROADMAP_V2.md). Kept for historical reference only.

---

## 1. Purpose

This document defines the high-level development roadmap for V1.

V1 is intended to prove the platform on the turbulence-transition experiment while building the architecture as a general experimental control foundation.

## 2. Roadmap Philosophy

The roadmap is organized around architectural dependency, not cosmetic completeness.

That means:

- extract old knowledge first,
- define structure before implementation volume,
- build safety before convenience,
- integrate AI early but lightly,
- validate one full experiment before broad generalization.

## 3. Stage 0: Legacy Knowledge Extraction

### Goal

Mine the current pipe-flow system for knowledge without inheriting its architecture.

### Inputs

- [exp_control_functions.py](../../legacy/pipe-flow-reference/python/exp_control_functions.py)
- [Looping_exp_main.ipynb](../../legacy/pipe-flow-reference/notebooks/Looping_exp_main.ipynb)
- [PipeFlowComputer.py](../../legacy/pipe-flow-reference/python/PipeFlowComputer.py)
- [pufffuncs.py](../../legacy/pipe-flow-reference/python/pufffuncs.py)
- [Statistics.py](../../legacy/pipe-flow-reference/python/Statistics.py)

### Outputs

- device inventory,
- protocol inventory,
- control and monitoring inventory,
- signal-processing inventory,
- startup/shutdown sequence notes,
- data-output inventory,
- known operational assumptions,
- known failure and stop conditions.

### Result

The old system becomes a structured knowledge source for the new platform.

## 4. Stage 1: Core Artifact and Schema Foundation

### Goal

Define the system language in structured form.

Status: partially complete. The core artifact models now exist in `platform/src/ExperimentalControlPlatform.Core`.

### Outputs

- experiment schema,
- device schema,
- protocol schema,
- runtime schema,
- run manifest schema,
- initial versioning rules,
- validation rules for units, ranges, and required bindings.

### Result

The project gets a stable artifact foundation that both humans and AI can work with.

## 5. Stage 2: Device and Protocol Core

### Goal

Build the first real device/protocol abstraction layer.

Status: started. The protocol and capability primitives now exist in `platform/src/ExperimentalControlPlatform.Protocols` and `platform/src/ExperimentalControlPlatform.Devices`.

### Outputs

- protocol model,
- capability model,
- device contract,
- initial driver/plugin boundaries,
- first real device integrations,
- initial device-level validation and health model.

### Stage-End Result

At the end of this stage, the platform should be able to represent and supervise concrete hardware in a reusable way.

## 6. Stage 3: Runtime and Safety Core

### Goal

Build the central executable runtime.

Status: started. The runtime stop skeleton and minimal runtime-aware app shell now exist in `platform/src/ExperimentalControlPlatform.Runtime` and `platform/src/ExperimentalControlPlatform.App`.

### Outputs

- runtime orchestrator,
- experiment execution graph or equivalent runtime plan,
- state model,
- ordered startup/shutdown lifecycle,
- event and alarm propagation,
- automatic stop logic,
- run logging foundation.

### Stage-End Result

At the end of this stage, the project should have a real system core rather than script-based control.

## 7. Stage 4: AI Sidecar Integration

### Goal

Add useful AI assistance without giving AI direct unsafe control authority.

### Outputs

- background AI session model,
- artifact-aware assistant workflow,
- doc generation workflow,
- diff-and-approval workflow,
- log-aware support workflow,
- prompt patterns for design, device integration, and experiment setup.

### Result

AI becomes a productive co-builder early, without owning the runtime.

## 8. Stage 5: First Reference Experiment

### Goal

Implement the turbulence-transition experiment cleanly inside the new platform.

### Outputs

- turbulence experiment definition,
- concrete device bindings,
- migrated key processing logic,
- system-level monitors,
- experiment-specific stop conditions,
- structured outputs and manifests,
- first operator workflow through the new runtime.

### Result

The architecture is validated on a real, nontrivial experiment.

## 9. Stage 6: Hardening and Generalization

### Goal

Make the first architecture stable and ready for broader reuse.

### Outputs

- simulator or replay harness,
- runtime failure-path tests,
- improved docs,
- cleaner plugin boundaries,
- better packaging,
- support for adding another experiment or device family.

### Result

The platform becomes a credible foundation rather than a one-off demo.

## 10. Early Milestone Order

The first concrete milestones should be:

1. formalize the old pipe-flow knowledge set,
2. lock the language split and stack direction,
3. define artifact schemas,
4. define the protocol and capability model,
5. define the automatic stop model,
6. build the runtime skeleton,
7. add AI sidecar workflows,
8. migrate the turbulence experiment into the new structure.

## 11. What Not to Prioritize Early

V1 should not spend early effort on:

- polished generic device-testing UI templates,
- broad visual customization,
- broad multi-domain marketing claims,
- replacing every old Python function immediately,
- full autonomy for AI changes.

The goal is a sound core, not early polish.
