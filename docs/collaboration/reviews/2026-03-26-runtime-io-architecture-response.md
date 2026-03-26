# Response: Runtime IO Architecture Review

- **Review file**:
  - [2026-03-26-runtime-io-architecture-review.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/collaboration/reviews/2026-03-26-runtime-io-architecture-review.md)
- **Responder**: Codex
- **Date**: 2026-03-26

## Disposition

- `RI-001` Accepted and fixed
- `RI-002` Accepted and fixed
- `RI-003` Accepted and fixed
- `RI-004` Accepted and fixed
- `RI-005` Accepted and fixed
- `RI-006` Accepted and fixed
- `RI-007` Accepted and fixed
- `RI-008` Accepted and fixed
- `RI-009` Accepted and fixed
- `RI-010` Accepted and fixed

## What changed

### Runtime architecture doc

Updated [DEVICE_RUNTIME_IO_ARCHITECTURE.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/architecture/DEVICE_RUNTIME_IO_ARCHITECTURE.md) to:

- define session ownership and lifecycle explicitly
- replace the vague `IDeviceOutputPort<T>` concept with explicit snapshot-port and stream-port roles
- define the first-generation delivery contract using .NET events
- define the threading and subscription contract
- align `DiagnosticsOutput` to the existing snapshot model
- align `MeasuredRate` to `CaptureRate`
- split lifecycle commands from operational commands
- replace the first controlled pilot recommendation with the inventory-backed turbulence control-center / flow-rate controller path
- add the hybrid-versus-two-session decision rule
- correct the microphone example to `PCM-only format constraints`

### Panel IO contract doc

Updated [INTEGRATION_PANEL_IO_CONTRACT.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/architecture/INTEGRATION_PANEL_IO_CONTRACT.md) to:

- mark the input contract as architecture intent, not yet implemented
- add `DeviceId` to the suggested data-output fields

### Archetype docs

Updated [DEVICE_ARCHETYPE_MAPPING.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/architecture/DEVICE_ARCHETYPE_MAPPING.md) to:

- add the hybrid-versus-two-session rule
- point the required output artifact at a concrete template and location

Added:

- [DEVICE_ARCHETYPE_RECORD_TEMPLATE.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/architecture/DEVICE_ARCHETYPE_RECORD_TEMPLATE.md)

Updated [AI_GUIDED_HARDWARE_INTEGRATION_SPEC.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/architecture/AI_GUIDED_HARDWARE_INTEGRATION_SPEC.md) to:

- require a concrete archetype record for each integrated device

### Existing contract type alignment

Aligned the existing branch-local output record by adding `DeviceId` to:

- [IntegrationPanelDataOutput.cs](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/panel-contract-enforcement/platform/src/ExperimentalControlPlatform.App/DevicePanels/Contracts/IntegrationPanelDataOutput.cs)

## Verification

- `dotnet build .\platform\ExperimentalControlPlatform.sln`
  - passed in the panel-contract-enforcement worktree after the `DeviceId` addition

## Remaining note

These fixes make the doc set strong enough to start the first runtime-session implementation.
The next implementation target should still be:

- `IntegratedMicrophoneSession`
