# Control Center Runtime Review

**Date**: 2026-03-27
**Reviewer**: Claude Code (Opus 4.6)
**Scope**: working-tree delta after `07ce629` on `codex/runtime-io-microphone`

## Review Summary

- Architecture alignment:
  - app host owns registry explicitly
  - control-center session is a controlled-device runtime implementation
  - control-center panel is a session client
- Initial judgment:
  - not yet merge-ready before the findings below were resolved

## Blocking Issues

### CC-001

- **Severity**: `P2`
- **Area**: shutdown lifecycle
- **File**: `platform/src/ExperimentalControlPlatform.App/App.xaml.cs` and control-center panel disposal path
- **Evidence**: app shutdown called registry `StopAllAsync()` and panel disposal still fire-and-forget disposed the same session path.
- **Expected**: one clear ownership path for stop/dispose on shutdown.
- **Actual**: double-stop risk, redundant `SessionEnd` snapshots, and abandoned fire-and-forget work during process exit.
- **Why it matters**: shutdown truth and session-end semantics become unreliable.
- **Recommended fix**: make registry stop clear registry ownership and make panel disposal conditional so it only disposes sessions it successfully removes from the registry.

### CC-002

- **Severity**: `P2`
- **Area**: semantic truthfulness
- **File**: `platform/src/ExperimentalControlPlatform.Runtime/ControlCenterSessionState.cs`
- **Evidence**: `PuffEnabled` and `LaserEnabled` were named as if they were confirmed hardware state.
- **Expected**: names should reflect that the control-center path only knows the last command written to transport unless hardware acknowledgement exists.
- **Actual**: state names overstated transport-write success as confirmed hardware state.
- **Why it matters**: violates the IO truthfulness rules.
- **Recommended fix**: rename to `LastCommandedPuffEnabled` and `LastCommandedLaserEnabled`, and align UI labels accordingly.

## Non-Blocking Issues

### CC-003

- **Severity**: `P3`
- **Area**: IO contract
- **File**: `platform/src/ExperimentalControlPlatform.App/DevicePanels/ControlCenter/ControlCenterPanelViewModel.cs`
- **Evidence**: `IntegrationPanelDataOutput.EndpointId` used the device display name for pulse data.
- **Expected**: endpoint should identify the pulse-readback surface explicitly.
- **Actual**: endpoint identity was too vague.
- **Why it matters**: downstream routing is less precise than it should be.
- **Recommended fix**: use `pulse-counter` as the endpoint ID.

### CC-004

- **Severity**: `P3`
- **Area**: panel dispose
- **File**: `platform/src/ExperimentalControlPlatform.App/DevicePanels/ControlCenter/ControlCenterPanelViewModel.cs`
- **Evidence**: fire-and-forget disposal swallowed exceptions silently.
- **Expected**: leave a development trace.
- **Actual**: failures disappeared silently.
- **Why it matters**: lifecycle bugs become harder to diagnose.
- **Recommended fix**: log disposal failures.

### CC-005

- **Severity**: `P3`
- **Area**: test coverage
- **File**: `platform/tests/ExperimentalControlPlatform.Runtime.Tests/*`
- **Evidence**: missing tests for double-disconnect idempotency, disconnected command rejection, and registry integration.
- **Expected**: the controlled pilot should cover these lifecycle edges.
- **Actual**: coverage stopped short of those cases.
- **Why it matters**: lifecycle regressions would be easy to miss.
- **Recommended fix**: add the missing tests.

### CC-006

- **Severity**: `P3`
- **Area**: diagnostics consistency
- **File**: `platform/src/ExperimentalControlPlatform.Devices/ControlCenter/SerialControlCenterService.cs` and `platform/src/ExperimentalControlPlatform.Runtime/ControlCenterSession.cs`
- **Evidence**: `9600` baud was hardcoded in more than one place.
- **Expected**: a single source of truth for the control-center baud rate.
- **Actual**: duplicated literal.
- **Why it matters**: avoid drift between device transport and diagnostics text.
- **Recommended fix**: centralize the default baud rate.
