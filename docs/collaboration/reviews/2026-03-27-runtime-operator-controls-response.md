# Runtime Operator Controls Response

## Scope

Response to:

- [2026-03-27-runtime-operator-controls-review.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/collaboration/reviews/2026-03-27-runtime-operator-controls-review.md)

## Finding Responses

### F-01

- `Accepted`
- fix summary:
  - removed the manual re-assignment of staged operator settings after `EmergencyStopAsync`
  - let the runtime-applied safe command remain the authoritative `AppliedSettingsOutput`
  - preserved staged operator intent as an appended normalization note instead of pretending it was the applied hardware state
- changed files:
  - [ControlCenterPanelViewModel.cs](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/platform/src/ExperimentalControlPlatform.App/DevicePanels/ControlCenter/ControlCenterPanelViewModel.cs)
  - [ControlCenterPanelViewModelTests.cs](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/platform/tests/ExperimentalControlPlatform.App.Tests/ControlCenterPanelViewModelTests.cs)
- retest status:
  - verified by updated app test and full solution test pass

### F-02

- `Accepted`
- fix summary:
  - prevented `DisposeAsync` from re-running `DisconnectAsync` when the session was already disconnected
  - preserved the caller-provided session-end reason from the first disconnect path
- changed files:
  - [ControlCenterSession.cs](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/platform/src/ExperimentalControlPlatform.Runtime/ControlCenterSession.cs)
  - [ControlCenterPanelViewModelTests.cs](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/platform/tests/ExperimentalControlPlatform.App.Tests/ControlCenterPanelViewModelTests.cs)
- retest status:
  - verified by updated app test and full solution test pass

### F-03

- `Accepted`
- fix summary:
  - moved post-disconnect `AppliedSettingsOutput` and `SessionEndOutput` mutation back onto the UI dispatcher via `RunOnUi(...)`
- changed files:
  - [ControlCenterPanelViewModel.cs](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/platform/src/ExperimentalControlPlatform.App/DevicePanels/ControlCenter/ControlCenterPanelViewModel.cs)
- retest status:
  - verified by full build/test pass; static thread-affinity issue removed from code path

### F-04

- `Accepted`
- fix summary:
  - made `IntegrationPanelOutputPublisher` state access synchronized with a private lock around `ShouldPublish` and `Reset`
- changed files:
  - [IntegrationPanelOutputPublisher.cs](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/platform/src/ExperimentalControlPlatform.App/DevicePanels/Contracts/IntegrationPanelOutputPublisher.cs)
- retest status:
  - verified by full build/test pass

### F-05

- `Accepted`
- fix summary:
  - strengthened the app test to assert the preserved session-end reason and the final applied-settings snapshot content
- changed files:
  - [ControlCenterPanelViewModelTests.cs](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/platform/tests/ExperimentalControlPlatform.App.Tests/ControlCenterPanelViewModelTests.cs)
- retest status:
  - verified by full solution test pass

### F-06

- `Accepted`
- fix summary:
  - documented the first-generation exception for acquisition-only panels that do not stage meaningful command state
- changed files:
  - [INTEGRATION_PANEL_IO_CONTRACT.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/architecture/INTEGRATION_PANEL_IO_CONTRACT.md)
  - [DEVICE_PANEL_CONTRACT.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/architecture/DEVICE_PANEL_CONTRACT.md)
- retest status:
  - docs-only

### F-07

- `Accepted`
- fix summary:
  - changed `EmergencyStopAsync` to degrade gracefully when already disconnected
  - it now publishes a diagnostic note and status message instead of throwing
- changed files:
  - [ControlCenterSession.cs](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/platform/src/ExperimentalControlPlatform.Runtime/ControlCenterSession.cs)
  - [ControlCenterSessionTests.cs](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/platform/tests/ExperimentalControlPlatform.Runtime.Tests/ControlCenterSessionTests.cs)
- retest status:
  - verified by new runtime test and full solution test pass

## Verification

- `dotnet build .\\platform\\ExperimentalControlPlatform.sln`
- `dotnet test .\\platform\\ExperimentalControlPlatform.sln --no-build`
