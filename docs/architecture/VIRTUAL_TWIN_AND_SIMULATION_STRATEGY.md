# Virtual Twin And Simulation Strategy

## Purpose

This document defines how Orchestral should represent and use:

- virtual device twins,
- replay sources,
- and synthetic sources.

The goal is to make device integrations testable without requiring physical hardware every time.

## Core Rule

Real and virtual implementations should share the same Orchestral service/session contract.

They should not be coupled primarily through the raw vendor SDK.

That means:

- the runtime session should sit above an Orchestral device service boundary,
- the real path uses a real service implementation,
- the virtual path uses a virtual service implementation,
- both feed the same runtime session and downstream runtime consumers.

## Source Modes

Orchestral should support four source modes for concrete implementations:

- `Real`
- `Virtual`
- `Replay`
- `Synthetic`

Source-mode selection belongs to experiment-building and binding surfaces.
Once selected, it should be recorded as part of the resolved concrete implementation for the run.

### Real

Uses the real device, real transport, and real service implementation.

### Virtual

Uses a virtual twin that implements the same Orchestral contract as the real device service.

### Replay

Uses recorded data from a previous run and plays it back through the runtime-facing consumer path.

### Synthetic

Uses generated data rather than recorded data.

Examples:

- mean plus random noise
- ramp plus noise
- periodic oscillation
- step change
- stale or dropout injection

## Relationship To Device Integration

Virtual twin work should be the preferred final closure step of a device-integration slice.

The intended distinction is:

- `functionally usable`
  - the real device path works
- `fully closed`
  - the virtual or simulation path also exists

This does not mean every first-pass integration must ship with a perfect full-fidelity twin.
It does mean the integration should be designed so that the virtual path can be added without re-architecting the runtime session later.

For V1 platform hardening, this stage is a preferred closure target rather than a first-pass blocker.
The first universal harness should stay narrow even when the long-term strategy is broader.

## Default Preference Order

The preferred order for virtual testing support is:

1. service-level fake
2. replay source
3. synthetic source
4. transport-level mock when specifically useful

## Canvas Relationship

The experiment canvas should expose virtualization at the concrete-implementation layer, not at the device-panel layer.

That means:

- top-level function blocks stay high-level,
- roles stay abstract,
- concrete implementations may switch source mode.

So a function block may remain stable while its bound implementation changes from:

- `Real`
- to `Virtual`
- to `Replay`
- to `Synthetic`

## First V1 Harness Shape

The first universal harness should support:

- single-session replay
- scalar synthetic generation
- runtime-facing consumer playback
- automated-test use

V1 does not require full virtual device services for every device family.
Those remain desirable closure paths when the service/session boundary is ready for them.

The first concrete proof target should stay narrow:

- PT-104 scalar temperature data as the first replay/synthetic source,
- a runtime-source broadcaster rather than a full virtual PT-104 device,
- internal/test-only use before any app-shell source-mode selection,
- and downstream experiment-plane consumers such as derived-state logic as the first proof path.

This is enough to validate:

- controller logic
- monitor logic
- recorder behavior
- experiment-logic units consuming scalar streams
