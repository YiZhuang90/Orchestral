# Panel Close And Apply-Exit Response

## Reviewed Slice

- [2026-03-28-panel-close-and-apply-exit-review.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/collaboration/reviews/2026-03-28-panel-close-and-apply-exit-review.md)

## Finding Responses

### F-001

- Status: `Accepted`
- Summary:
  - `CloseRequested` could be raised off the UI thread after `ConfigureAwait(false)` in apply-and-exit paths.
  - The fix keeps the close-request continuation on the UI thread for all six panels that expose `Apply and Exit`.
- Changed files:
  - [IntegratedMicrophonePanelViewModel.cs](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/platform/src/ExperimentalControlPlatform.App/DevicePanels/Microphone/IntegratedMicrophonePanelViewModel.cs)
  - [IntegratedCameraPanelViewModel.cs](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/platform/src/ExperimentalControlPlatform.App/DevicePanels/Integrated/IntegratedCameraPanelViewModel.cs)
  - [HuaTengPanelViewModel.cs](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/platform/src/ExperimentalControlPlatform.App/DevicePanels/HuaTeng/HuaTengPanelViewModel.cs)
  - [Pt104PanelViewModel.cs](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/platform/src/ExperimentalControlPlatform.App/DevicePanels/Pt104/Pt104PanelViewModel.cs)
  - [ControlCenterPanelViewModel.cs](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/platform/src/ExperimentalControlPlatform.App/DevicePanels/ControlCenter/ControlCenterPanelViewModel.cs)
  - [HyperCamPanelViewModel.cs](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/platform/src/ExperimentalControlPlatform.App/DevicePanels/HyperCam/HyperCamPanelViewModel.cs)
- Retest:
  - `dotnet build .\\platform\\ExperimentalControlPlatform.sln -nodeReuse:false`
  - `dotnet test .\\platform\\ExperimentalControlPlatform.sln`
- Result:
  - passed
