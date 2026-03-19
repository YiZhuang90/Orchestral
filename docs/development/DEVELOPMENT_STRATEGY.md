# Development Strategy

## 1. Purpose

This document defines the development strategy for V1 of the AI-native experimental operating system.

It translates the north-star vision in [HIGH_LEVEL_PROJECT_TECHNICAL_INTRODUCTION.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/northstar/HIGH_LEVEL_PROJECT_TECHNICAL_INTRODUCTION.md) and the system structure in [V1_ARCHITECTURE_BLUEPRINT.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/architecture/V1_ARCHITECTURE_BLUEPRINT.md) into a practical build direction.

The goal is not to refine the current turbulence codebase into a platform. The goal is to build a new platform from scratch, while using the existing pipe-flow experiment as working legacy knowledge.

## 2. Strategic Position

V1 should be developed as:

- a clean-slate platform,
- validated on one real turbulence-transition experiment,
- architected for future hardware growth,
- architected for future experiment-domain changes,
- AI-assisted from the beginning,
- safety-aware from the runtime foundation.

The existing pipe-flow control code should not be treated as the architecture base. It should be treated as:

- a domain knowledge source,
- a protocol knowledge source,
- an algorithm reference,
- an operations reference,
- a first reference experiment.

## 3. Core Development Direction

### 3.1 Build around the workflow

Development should follow the real experiment lifecycle:

1. define experiment logic,
2. bind and characterize devices,
3. define data and control flow,
4. run and monitor experiments,
5. document and persist outputs.

The system architecture should be shaped by this workflow, not by a script layout or one notebook.

### 3.2 Build around structured artifacts

The platform should be built around explicit machine-readable artifacts, not informal glue code.

Key artifact families for V1:

- experiment definition,
- device definition,
- protocol definition,
- runtime definition,
- run manifest,
- documentation outputs.

### 3.3 Build around protocol-aware devices

Protocol must be treated as a first-class system concern.

In V1, every real device integration should be represented as:

**hardware identity + protocol model + capability contract + runtime driver**

### 3.4 Build around runtime-owned safety

Safety should not be delegated to UI habits or AI-generated code.

The runtime must own:

- startup validation,
- shutdown order,
- automatic stop logic,
- device-health supervision,
- stale-data detection,
- disconnect handling,
- experiment-defined stop conditions.

### 3.5 Build around AI as a controlled sidecar

AI should begin as a light background assistant, not as the controller of live hardware.

In early V1, AI should help:

- define and edit artifacts,
- summarize legacy knowledge,
- generate scaffolding,
- generate docs,
- generate test code,
- propose configuration changes,
- explain diffs and implications.

AI should not initially be trusted to directly invent unsafe low-level control behavior during live runs.

## 4. Development Priorities

The order of priorities should be:

1. language and runtime foundation,
2. artifact and schema foundation,
3. protocol and device model,
4. runtime supervision and stop logic,
5. AI sidecar integration,
6. first full turbulence experiment package,
7. later UI refinement and broader generalization.

This means the project should not spend early effort on polished generic UI templates before the runtime and device model are stable.

## 5. V1 Product Shape

V1 should prove that the platform can do the following on one machine:

- define one real experiment in structured form,
- bind several real devices through protocol-aware integrations,
- supervise them in one runtime,
- stop automatically when necessary,
- log runs in a structured way,
- expose a usable main control interface,
- allow AI to assist with setup, edits, and documentation,
- reuse legacy turbulence knowledge without inheriting legacy architecture.

## 6. Technical Position on Languages

The platform should not be all-Python.

The recommended split is:

- **C#/.NET as the main system language**
- **Python as the scientific, legacy, and tooling side language**

This split exists because V1 needs:

- stronger concurrency and supervision than Python provides comfortably,
- stronger typing for a growing platform,
- a better foundation for long-running runtime services,
- a practical desktop application path,
- while still preserving easy reuse of existing scientific logic.

## 7. What V1 Should Not Try to Solve

V1 should not attempt:

- a universal no-code experiment builder,
- a fully autonomous AI scientist,
- full multi-machine orchestration,
- complete support for every hardware family,
- a perfect reusable device test UI framework,
- complete removal of all Python from the system.

V1 should instead prove the architecture on one demanding but known case.

## 8. Development Principles

- Prefer explicit models over hidden assumptions.
- Prefer capability-based interfaces over device-specific branching.
- Prefer runtime validation over optimistic execution.
- Prefer generated documentation over undocumented growth.
- Prefer one clean reference implementation over premature generalization.
- Prefer a stronger system core over the convenience of keeping everything in Python.
- Prefer AI-assisted structured editing over AI-generated ad hoc runtime code.

## 9. Expected Outcome of This Strategy

If this strategy is followed, the project should evolve into:

- a platform rather than a script collection,
- a system that can absorb new hardware cleanly,
- a system that can host new experiment logic,
- a system whose UI grows from structure,
- and a system where AI contributes safely and productively from an early stage.
