# Control-Center Capability Decomposition Review Response

## Finding Responses

### F-01

- Status: Accepted
- Fix summary:
  - `WriteCommandAsync(...)` now republishes the `FlowTelemetry` capability snapshot after each successful command write with an explicit "awaiting refresh" status.
  - the runtime tests now assert that command writes update the flow-telemetry capability surface as part of the capability-decomposition contract.
- Changed files:
  - `platform/src/ExperimentalControlPlatform.Runtime/ControlCenterSession.cs`
  - `platform/tests/ExperimentalControlPlatform.Runtime.Tests/ControlCenterSessionTests.cs`
- Retest status:
  - covered by `ControlCenterSessionTests.ApplyLaserControlAsync_PublishesLaserCapability_AndPreservesPuffActuationState`
  - full runtime and solution tests pass

### F-02

- Status: Accepted
- Fix summary:
  - retained the old `PulseReads` and `ReadPulseCountAsync(...)` surfaces only as compatibility aliases
  - marked both as `[Obsolete]` with messages pointing consumers to `FlowTelemetryReads` and `ReadFlowTelemetryAsync(...)`
- Changed files:
  - `platform/src/ExperimentalControlPlatform.Runtime/ControlCenterSession.cs`
- Retest status:
  - solution build and tests pass with the canonical flow-telemetry surface used by new runtime and panel code

### F-03

- Status: Accepted
- Fix summary:
  - added explicit validation-summary messaging for capability-scoped helpers
  - when no prior session command exists, the runtime now states that unspecified fields defaulted to safe off/0 values
  - when a prior command exists, the runtime now states that unspecified fields were reconstructed from the last session command rather than hardware acknowledgement
- Changed files:
  - `platform/src/ExperimentalControlPlatform.Runtime/ControlCenterSession.cs`
- Retest status:
  - capability-scoped helper tests pass
  - diagnostics behavior remains covered by the runtime session suite

### F-04

- Status: Accepted
- Fix summary:
  - `WriteCommandAsync(...)` now accepts the already-acquired connection
  - `EmergencyStopAsync(...)` performs one graceful disconnected check, then passes the live connection through, closing the old TOCTOU gap
- Changed files:
  - `platform/src/ExperimentalControlPlatform.Runtime/ControlCenterSession.cs`
- Retest status:
  - `EmergencyStopAsync_WhenDisconnected_DoesNotThrow_AndPublishesDiagnosticNote`
  - `EmergencyStopAsync_SendsSafeCommand_AndLeavesSessionConnected`
  - full solution tests pass

### F-05

- Status: Accepted
- Fix summary:
  - renamed the panel stream handler from `HandlePulseProduced` to `HandleFlowTelemetryProduced` for consistency with the new capability naming
- Changed files:
  - `platform/src/ExperimentalControlPlatform.App/DevicePanels/ControlCenter/ControlCenterPanelViewModel.cs`
- Retest status:
  - control-center panel tests pass
  - full solution tests pass

### F-06

- Status: Accepted
- Fix summary:
  - added the symmetric runtime test for puff-actuation capability commands preserving laser-control state
- Changed files:
  - `platform/tests/ExperimentalControlPlatform.Runtime.Tests/ControlCenterSessionTests.cs`
- Retest status:
  - `ApplyPuffActuationAsync_PublishesPuffCapability_AndPreservesLaserControlState` passes
  - full solution tests pass

### F-07

- Status: Accepted
- Fix summary:
  - the rereview package will include the new capability-state files so the record contracts are visible to the reviewer
- Changed files:
  - `platform/src/ExperimentalControlPlatform.Runtime/ControlCenterLaserCapabilityState.cs`
  - `platform/src/ExperimentalControlPlatform.Runtime/ControlCenterPuffActuationCapabilityState.cs`
  - `platform/src/ExperimentalControlPlatform.Runtime/ControlCenterFlowTelemetryState.cs`
- Retest status:
  - full solution build passes, which already verifies the contracts compile cleanly

## Verification

- `dotnet test .\\platform\\tests\\ExperimentalControlPlatform.Runtime.Tests\\ExperimentalControlPlatform.Runtime.Tests.csproj -nodeReuse:false --filter ControlCenterSession`
- `dotnet test .\\platform\\tests\\ExperimentalControlPlatform.App.Tests\\ExperimentalControlPlatform.App.Tests.csproj -nodeReuse:false --filter ControlCenterPanelViewModel`
- `dotnet build .\\platform\\ExperimentalControlPlatform.sln -nodeReuse:false -v:minimal`
- `dotnet test .\\platform\\ExperimentalControlPlatform.sln --no-build -nodeReuse:false -v:minimal`
