# Control-Center Capability Decomposition Plan

## Goal

Make the control-center runtime session expose explicit capability boundaries for:

- laser control
- puff actuation
- flow telemetry

while keeping the underlying serial protocol intact, so monitor, recorder, and experiment-plane consumers can bind to clear capability surfaces instead of one mixed control-center lump.

## In Scope

- add explicit runtime capability state models for laser control, puff actuation, and flow telemetry
- expose capability-specific snapshot and stream surfaces from `ControlCenterSession`
- add capability-oriented command helpers while preserving the existing transport command path
- migrate downstream runtime consumers to the explicit flow-telemetry surface
- update the control-center panel so its outputs and applied-setting records reflect the decomposed capability semantics
- add tests that prove capability state, command semantics, and downstream flow-telemetry compatibility
- sync architecture and execution docs

## Out Of Scope

- changing the wire protocol format
- redesigning the control-center panel layout
- adding experiment-specific puff logic
- adding new hardware acknowledgement semantics that the protocol does not provide
- broad cross-session validation beyond the control-center slice

## Governing Docs

Required foundation:

- [MODULE_DEVELOPMENT_WORKFLOW.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/development/MODULE_DEVELOPMENT_WORKFLOW.md)
- [REVIEW_PROTOCOL.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/collaboration/REVIEW_PROTOCOL.md)
- [SUBAGENT_DEVELOPMENT_CONTRACT.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/collaboration/SUBAGENT_DEVELOPMENT_CONTRACT.md)
- [DEVICE_RUNTIME_IO_ARCHITECTURE.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/architecture/DEVICE_RUNTIME_IO_ARCHITECTURE.md)
- [INTEGRATION_PANEL_IO_CONTRACT.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/architecture/INTEGRATION_PANEL_IO_CONTRACT.md)
- [DEVICE_PANEL_CONTRACT.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/architecture/DEVICE_PANEL_CONTRACT.md)
- [TIMING_AND_SYNCHRONIZATION_STRATEGY.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/architecture/TIMING_AND_SYNCHRONIZATION_STRATEGY.md)
- [SYSTEM_LANGUAGE_SPEC.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/architecture/SYSTEM_LANGUAGE_SPEC.md)
- [DEVICE_INVENTORY.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/legacy-knowledge/DEVICE_INVENTORY.md)
- [PROTOCOL_INVENTORY.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/legacy-knowledge/PROTOCOL_INVENTORY.md)

Supporting reference:

- [2026-03-31-universal-completion-and-experiment-logic-proof-plan.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/development/2026-03-31-universal-completion-and-experiment-logic-proof-plan.md)
- [FlowReynoldsDerivedStateSession.cs](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/platform/src/ExperimentalControlPlatform.Runtime/FlowReynoldsDerivedStateSession.cs)
- [ControlCenterSession.cs](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/platform/src/ExperimentalControlPlatform.Runtime/ControlCenterSession.cs)
- [ControlCenterPanelViewModel.cs](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/platform/src/ExperimentalControlPlatform.App/DevicePanels/ControlCenter/ControlCenterPanelViewModel.cs)

Conflict winner:

- `DEVICE_RUNTIME_IO_ARCHITECTURE.md` defines the runtime ownership boundary
- legacy device/protocol docs define the preserved protocol truth

## Current Reality

- the device protocol already mixes laser state, puff state, and step count in one serial command line
- `ControlCenterSession` still exposes one flat command/state surface even though the legacy/protocol docs already describe three logical capabilities
- `FlowReynoldsDerivedStateSession` correctly consumes pulse telemetry, but it still binds through a control-center-specific pulse surface rather than an explicit flow-telemetry capability
- the control-center panel still records its applied settings as one flat endpoint setting block
- current tests prove the combined command/session behavior, but not explicit capability semantics

## Current Ownership Model

- devices layer owns transport truth:
  - `ControlCenterCommand`
  - `ControlCenterProtocol`
  - `IControlCenterService`
- runtime layer owns session truth:
  - `ControlCenterSession`
  - `ControlCenterSessionState`
  - `ControlCenterAppliedState`
- app layer owns the panel client and run-artifact snapshots:
  - `ControlCenterPanelViewModel`
  - `RunArtifactWriter`
- experiment plane consumes flow telemetry through runtime:
  - `FlowReynoldsDerivedStateSession`

## Current Runtime / Data / Control Path

1. panel drafts a combined control-center command
2. `ControlCenterSession.ApplyCommandAsync(...)` writes one serial transport command
3. session state stores combined command fields in one flat state record
4. pulse telemetry is read through `ReadPulseCountAsync()` and published as a control-center pulse sample
5. Reynolds derivation consumes those pulse samples downstream

## Files That Must Change

Primary:

- `platform/src/ExperimentalControlPlatform.Runtime/ControlCenterSession.cs`
- `platform/src/ExperimentalControlPlatform.Runtime/ControlCenterSessionState.cs`
- `platform/src/ExperimentalControlPlatform.Runtime/ControlCenterAppliedState.cs`
- `platform/src/ExperimentalControlPlatform.Runtime/FlowReynoldsDerivedStateSession.cs`
- `platform/src/ExperimentalControlPlatform.App/DevicePanels/ControlCenter/ControlCenterPanelViewModel.cs`
- `platform/tests/ExperimentalControlPlatform.Runtime.Tests/ControlCenterSessionTests.cs`
- `platform/tests/ExperimentalControlPlatform.Runtime.Tests/FlowReynoldsDerivedStateSessionTests.cs`
- `platform/tests/ExperimentalControlPlatform.App.Tests/ControlCenterPanelViewModelTests.cs`

Likely secondary:

- `platform/src/ExperimentalControlPlatform.Runtime/ControlCenterLaserCapabilityState.cs`
- `platform/src/ExperimentalControlPlatform.Runtime/ControlCenterPuffCapabilityState.cs`
- `platform/src/ExperimentalControlPlatform.Runtime/ControlCenterFlowTelemetryState.cs`
- `docs/architecture/DEVICE_RUNTIME_IO_ARCHITECTURE.md`
- `docs/architecture/SYSTEM_LANGUAGE_SPEC.md`
- `docs/development/V1_EXECUTION_TRACK.md`

## Files That Must Not Change

- transport protocol shape in `ControlCenterProtocol.cs`
- unrelated controller or monitor semantics
- experiment-logic units beyond the flow-telemetry consumer seam

## Risks

- capability helpers may accidentally misrepresent transport truth if they imply hardware acknowledgement that does not exist
- downstream Reynolds wiring could break if the flow-telemetry seam is not migrated carefully
- panel outputs could become inconsistent with the new capability state if the applied-settings snapshot is not updated alongside the runtime change

## Missing Tests

- capability-specific runtime state publication
- capability helper methods preserving the current combined transport write semantics
- flow-telemetry consumer compatibility after the seam rename/decomposition
- panel applied-settings snapshots reflecting decomposed capability keys/notes

## Ordered Tasks

1. Add failing runtime tests for:
   - laser capability state publication
   - puff capability state publication
   - flow-telemetry state publication
   - capability helper methods preserving the combined transport write
2. Add a failing Reynolds-derived-state test proving it consumes the explicit flow-telemetry surface.
3. Add a failing panel test for decomposed applied-settings semantics.
4. Implement capability state records and capability output ports on `ControlCenterSession`.
5. Implement capability-oriented helpers on `ControlCenterSession` while keeping the combined transport command as the underlying write path.
6. Migrate `FlowReynoldsDerivedStateSession` to the explicit flow-telemetry surface.
7. Update the control-center panel outputs and applied/session-end snapshots to reflect the decomposed capability semantics.
8. Sync architecture/execution docs.
9. Run build/tests, external review, address findings, reverify, commit, and push.

## Success Criteria

- `ControlCenterSession` exposes explicit laser, puff, and flow-telemetry capability surfaces
- downstream consumers no longer need to infer flow telemetry from a generic control-center lump
- the control-center panel and run artifacts represent decomposed capability semantics clearly
- transport-level protocol truth is preserved
- build and full test suite pass
- external review closes without blocking findings

## Review Gate

External review is required because this slice is:

- runtime and lifecycle related
- shared foundation hardening
- hardware-truthfulness related
- and a dependency for later cross-session validation

Preferred reviewer channel:

- Claude Code CLI `claude-opus-4-6`

Fallback reviewer channel:

- Gemini CLI `gemini-3-flash-preview`

## Verification Gate

Focused red/green:

- `dotnet test .\\platform\\tests\\ExperimentalControlPlatform.Runtime.Tests\\ExperimentalControlPlatform.Runtime.Tests.csproj -nodeReuse:false --filter ControlCenterSession`
- `dotnet test .\\platform\\tests\\ExperimentalControlPlatform.Runtime.Tests\\ExperimentalControlPlatform.Runtime.Tests.csproj -nodeReuse:false --filter FlowReynoldsDerivedStateSession`
- `dotnet test .\\platform\\tests\\ExperimentalControlPlatform.App.Tests\\ExperimentalControlPlatform.App.Tests.csproj -nodeReuse:false --filter ControlCenterPanelViewModel`

Minimum completion verification:

- `dotnet build .\\platform\\ExperimentalControlPlatform.sln -nodeReuse:false -v:minimal`
- `dotnet test .\\platform\\ExperimentalControlPlatform.sln --no-build -nodeReuse:false -v:minimal`

## Outcome

Implemented in this branch:

- explicit `LaserControl`, `PuffActuation`, and `FlowTelemetry` runtime capability surfaces on `ControlCenterSession`
- capability-oriented helper methods:
  - `ApplyLaserControlAsync(...)`
  - `ApplyPuffActuationAsync(...)`
  - `ReadFlowTelemetryAsync(...)`
- compatibility aliases marked obsolete for the old pulse-oriented runtime names
- `FlowReynoldsDerivedStateSession` wired to the explicit `FlowTelemetryReads` surface
- control-center panel state and applied-settings snapshots updated to capability-scoped semantics
- architecture and execution docs synced so this universal hardening unit is now marked complete

Verification:

- `dotnet test .\\platform\\tests\\ExperimentalControlPlatform.Runtime.Tests\\ExperimentalControlPlatform.Runtime.Tests.csproj -nodeReuse:false --filter ControlCenterSession`
- `dotnet test .\\platform\\tests\\ExperimentalControlPlatform.Runtime.Tests\\ExperimentalControlPlatform.Runtime.Tests.csproj -nodeReuse:false --filter FlowReynoldsDerivedStateSession`
- `dotnet test .\\platform\\tests\\ExperimentalControlPlatform.App.Tests\\ExperimentalControlPlatform.App.Tests.csproj -nodeReuse:false --filter ControlCenterPanelViewModel`
- `dotnet build .\\platform\\ExperimentalControlPlatform.sln -nodeReuse:false -v:minimal`
- `dotnet test .\\platform\\ExperimentalControlPlatform.sln --no-build -nodeReuse:false -v:minimal`

Review trail:

- `2026-03-31-control-center-capability-decomposition-review-claude-opus-4-6.md`
- `2026-03-31-control-center-capability-decomposition-response.md`
- `2026-03-31-control-center-capability-decomposition-rereview-claude-opus-4-6.md`
