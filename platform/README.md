# Orchestral Platform Workspace

This directory contains the new platform-first implementation workspace for Orchestral.

## Current Slice

The first implemented slice is in place:

- `ExperimentalControlPlatform.Core` defines the core artifact models, including `ExperimentDefinition`, `DeviceDefinition`, `ProtocolDefinition`, `CapabilityDefinition`, `StreamDefinition`, `TransformDefinition`, `MonitorDefinition`, `StopConditionDefinition`, `OutputDefinition`, and `RunManifestDefinition`.
- `ExperimentalControlPlatform.Protocols` defines the protocol primitives `ProtocolKind`, `TransportKind`, and `ProtocolSessionBehavior`.
- `ExperimentalControlPlatform.Devices` defines the capability primitives `CapabilityKind` and `DeviceCapabilityContract`.
- `ExperimentalControlPlatform.Runtime` defines the stop-aware runtime skeleton with `RunState`, `RuntimeRunContext`, `StopReason`, and `RuntimeCoordinator`.
- `ExperimentalControlPlatform.App` is a minimal runtime-aware WPF shell with start/stop controls bound to the runtime coordinator.
- `ExperimentalControlPlatform.AI` is still placeholder-only and not part of the implemented slice yet.

## Current Project Layout

- `src/ExperimentalControlPlatform.Core`: implemented core domain and artifact models.
- `src/ExperimentalControlPlatform.Runtime`: implemented runtime lifecycle and stop skeleton.
- `src/ExperimentalControlPlatform.Protocols`: implemented protocol enums and session-behavior primitives.
- `src/ExperimentalControlPlatform.Devices`: implemented device capability primitives and contracts.
- `src/ExperimentalControlPlatform.App`: implemented minimal WPF app shell wired to the runtime coordinator.
- `src/ExperimentalControlPlatform.AI`: placeholder project reserved for a later AI sidecar integration layer.
- `tests/`: implemented unit test projects for the current Core and Runtime slices.

## Tooling Status

The workspace is organized as a .NET 8 solution, with the first core slice implemented and the remaining platform layers still intentionally thin.
