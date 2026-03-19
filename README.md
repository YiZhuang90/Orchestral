# Orchestral

**Design, Run, and Evolve Experiments with AI**

This repository is being reorganized from a single-experiment control codebase into a platform-first development workspace for **Orchestral**, an AI-native experimental operating system.

Orchestral is intended to help scientists design experiments at the system level first, then use AI to turn device manuals, protocol details, and experiment intent into a runnable, documented, and reproducible experimental system.

The long-term ambition is to become a layered alternative to legacy experiment-building workflows such as LabVIEW, starting with custom research experiments that mix hardware, change often, and are currently painful to build and maintain.

## Product Direction

- Category: `AI-native experimental operating system`
- Product name: `Orchestral`
- Subtitle: `Design, Run, and Evolve Experiments with AI`
- Initial wedge: custom research experiments with mixed hardware on a single central computer
- Validation path: rebuild the turbulence experiment first, then build the flame experiment on the same core architecture

## Current Status

The first core slice is implemented in `platform/`:

- core artifact models exist for experiments, devices, protocols, capabilities, streams, transforms, monitors, stop conditions, outputs, and run manifests,
- protocol and capability primitives exist as small typed boundaries,
- the runtime now has a stop-aware in-memory skeleton,
- the WPF app shell is runtime-aware and exposes start/stop controls.

## Repository Layout

- [docs/](docs): project direction, architecture, development strategy, and planning documents
- [legacy/pipe-flow-reference/](legacy/pipe-flow-reference): the existing working pipe-flow experiment code preserved as legacy reference knowledge
- [platform/](platform): the new platform workspace and starter C# solution
- [schemas/](schemas): reserved for future artifact and system-language schemas
- [experiments/](experiments): reserved for experiment packages built on the new platform
- [experiments/turbulence-transition/](experiments/turbulence-transition): planned turbulence experiment package, not implemented yet
- [tools/](tools): development and migration helpers

## Key Principle

The code under `legacy/pipe-flow-reference/` is preserved as working old knowledge. It is not the target architecture of the new platform.

## Current Direction

The current direction is documented in:

- [HIGH_LEVEL_PROJECT_TECHNICAL_INTRODUCTION.md](docs/northstar/HIGH_LEVEL_PROJECT_TECHNICAL_INTRODUCTION.md)
- [PRODUCT_THESIS_AND_WEDGE.md](docs/northstar/PRODUCT_THESIS_AND_WEDGE.md)
- [LABVIEW_REPLACEMENT_THESIS.md](docs/northstar/LABVIEW_REPLACEMENT_THESIS.md)
- [PROJECT_NAMING_AND_POSITIONING_NOTE.md](docs/northstar/PROJECT_NAMING_AND_POSITIONING_NOTE.md)
- [V1_ARCHITECTURE_BLUEPRINT.md](docs/architecture/V1_ARCHITECTURE_BLUEPRINT.md)
- [DEVELOPMENT_STRATEGY.md](docs/development/DEVELOPMENT_STRATEGY.md)
- [TECH_STACK_DECISIONS.md](docs/development/TECH_STACK_DECISIONS.md)
- [ROADMAP_V1.md](docs/development/ROADMAP_V1.md)
- [OPEN_CORE_AND_COMMERCIALIZATION_MODEL.md](docs/development/OPEN_CORE_AND_COMMERCIALIZATION_MODEL.md)

## Tooling Note

The platform scaffold is in place as a .NET 8 solution, and the current focus is the first concrete architecture slice rather than broad feature completeness.
