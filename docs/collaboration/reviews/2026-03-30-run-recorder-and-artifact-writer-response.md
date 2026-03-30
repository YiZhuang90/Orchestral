# Run Recorder And Artifact Writer Response

## Response

- `Finding ID`: `RRW-001`
- `Decision`: `Accepted`

## Reasoning

The review correctly identified that application shutdown previously bypassed the recorder path. That would have left the runtime stopped but the run artifacts unwritten, which is a real data-loss hole for this slice.

## Fix Summary

- kept runtime shutdown in `App.xaml.cs` on the coordinator path
- added `MainViewModel.FinalizeRunRecordingForLatestStoppedRun()`
- changed app shutdown to:
  - stop the runtime through `RuntimeCoordinator.EnsureStoppedAsync(...)`
  - then finalize the recorder from the latest stopped snapshot on the UI thread
- guarded duplicate completion so the recorder only finalizes a given run once

## Changed Files

- [App.xaml.cs](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/platform/src/ExperimentalControlPlatform.App/App.xaml.cs)
- [MainViewModel.cs](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/platform/src/ExperimentalControlPlatform.App/MainViewModel.cs)
- [MainViewModelTests.cs](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/platform/tests/ExperimentalControlPlatform.App.Tests/MainViewModelTests.cs)

## Retest

- `Status`: `Passed`
- `Evidence`:
  - `dotnet run --project .\\temp\\run-recorder-smoke\\run-recorder-smoke.csproj`
  - verified `MANIFEST_EXISTS True`
  - verified the run directory contained `manifest.yaml` after a real start/stop cycle
  - `dotnet test .\\platform\\tests\\ExperimentalControlPlatform.App.Tests\\ExperimentalControlPlatform.App.Tests.csproj -nodeReuse:false`
  - verified `FinalizeRunRecordingForLatestStoppedRun_Completes_Recording_After_External_Stop`

## Notes

- this fixes shutdown-path artifact loss for the current shell integration

---

## Response

- `Finding ID`: `RRW-002`
- `Decision`: `Accepted`

## Reasoning

The review is correct that a null recorder should behave like a real null-object, not a trap. Throwing during stop makes the default wiring unsafe in any shell that omits recorder configuration.

## Fix Summary

- changed `IRunRecorder.CompleteRun(...)` to return nullable `RunRecordingResult?`
- made `NullRunRecorder.CompleteRun(...)` return `null`
- kept `LastRunRecording` nullable so shells without recording stay safe and truthful

## Changed Files

- [IRunRecorder.cs](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/platform/src/ExperimentalControlPlatform.App/IRunRecorder.cs)
- [MainViewModel.cs](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/platform/src/ExperimentalControlPlatform.App/MainViewModel.cs)

## Retest

- `Status`: `Passed`
- `Evidence`:
  - `dotnet test .\\platform\\tests\\ExperimentalControlPlatform.App.Tests\\ExperimentalControlPlatform.App.Tests.csproj -nodeReuse:false`
  - verified `MainViewModelTests` still passes with recorder integration

## Notes

- the null-object behavior is now safe for future lightweight shells

---

## Response

- `Finding ID`: `RRW-003`
- `Decision`: `Accepted`

## Reasoning

Keeping both `artifact-index.yaml` and the same mapping inside `manifest.yaml` was unnecessary duplication. For this slice, the manifest should be the entry-point source of truth.

## Fix Summary

- removed `artifact-index.yaml`
- removed the extra output artifact id for the artifact index
- kept the artifact path mapping directly in the manifest

## Changed Files

- [RunArtifactWriter.cs](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/platform/src/ExperimentalControlPlatform.App/RunArtifactWriter.cs)
- [RunRecorderTests.cs](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/platform/tests/ExperimentalControlPlatform.App.Tests/RunRecorderTests.cs)

## Retest

- `Status`: `Passed`
- `Evidence`:
  - `dotnet test .\\platform\\tests\\ExperimentalControlPlatform.App.Tests\\ExperimentalControlPlatform.App.Tests.csproj -nodeReuse:false`
  - `dotnet run --project .\\temp\\run-recorder-smoke\\run-recorder-smoke.csproj`
  - verified `manifest.yaml` contains `artifacts:` and there is no `artifact-index.yaml`

## Notes

- this makes the run directory smaller and removes a second mapping surface

---

## Response

- `Finding ID`: `RRW-004`
- `Decision`: `Accepted`

## Reasoning

The reviewer was right that the manifest YAML should not contain structure that the typed model cannot represent. The artifact map belongs in the typed manifest model if the manifest is meant to be authoritative.

## Fix Summary

- added `Artifacts` to `RunManifestDefinition`
- updated constructors so existing call sites still work with empty artifacts by default
- updated the writer to materialize the manifest from the typed `Artifacts` property
- added core coverage for artifact-path capture

## Changed Files

- [RunManifestDefinition.cs](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/platform/src/ExperimentalControlPlatform.Core/Artifacts/RunManifestDefinition.cs)
- [RunManifestDefinitionTests.cs](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/platform/tests/ExperimentalControlPlatform.Core.Tests/Artifacts/RunManifestDefinitionTests.cs)
- [RunArtifactWriter.cs](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/platform/src/ExperimentalControlPlatform.App/RunArtifactWriter.cs)

## Retest

- `Status`: `Passed`
- `Evidence`:
  - `dotnet test .\\platform\\tests\\ExperimentalControlPlatform.Core.Tests\\ExperimentalControlPlatform.Core.Tests.csproj -nodeReuse:false`
  - verified the new manifest test captures artifact-path mapping

## Notes

- the manifest is now typed and self-contained for downstream consumers

---

## Response

- `Finding ID`: `RRW-005`
- `Decision`: `Rejected`

## Reasoning

The `StopRuntime()` fire-and-forget wrapper is an intentional command-surface convenience. The awaitable path already exists as `StopRuntimeAsync()`, and shutdown now uses `EnsureRuntimeStoppedAsync(...)` for the reliable awaitable coordination path. So the wrapper itself is not the blocker.

## Fix Summary

- no change to the synchronous wrapper
- added the explicit awaited shutdown path instead

## Changed Files

- [MainViewModel.cs](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/platform/src/ExperimentalControlPlatform.App/MainViewModel.cs)

## Retest

- `Status`: `Passed`
- `Evidence`:
  - `dotnet run --project .\\temp\\run-recorder-smoke\\run-recorder-smoke.csproj`
  - shutdown-safe path now runs through `EnsureRuntimeStoppedAsync(...)`

## Notes

- if later UI flow needs stronger stop-task visibility, that should be handled as a separate shell-command refinement

---

## Response

- `Finding ID`: `RRW-006`
- `Decision`: `Rejected`

## Reasoning

The current implementation captures panel snapshots on the same awaited stop path used by the shell and does not offload snapshot reads to a background worker. So the claimed cross-thread issue is not currently present in this slice.

## Fix Summary

- no change

## Changed Files

- [RunArtifactWriter.cs](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/platform/src/ExperimentalControlPlatform.App/RunArtifactWriter.cs)
- [MainViewModel.cs](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/platform/src/ExperimentalControlPlatform.App/MainViewModel.cs)

## Retest

- `Status`: `Passed`
- `Evidence`:
  - reviewed the current call chain:
    - `StopRuntimeAsync()`
    - awaited coordinator stop
    - `CompleteRun(...)`
  - no background dispatch was introduced in this slice

## Notes

- if panel snapshotting is later moved off-thread, this concern should be revisited then

---

## Response

- `Finding ID`: `RRW-007`
- `Decision`: `Deferred`

## Reasoning

Richer runtime event capture is desirable, but the current coordinator does not yet expose a real event stream. This belongs to a later monitoring/reporting hardening slice, not as a hidden expansion of the first recorder slice.

## Fix Summary

- no code change in this slice

## Changed Files

- [RunRecorder.cs](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/platform/src/ExperimentalControlPlatform.App/RunRecorder.cs)

## Retest

- `Status`: `Passed`
- `Evidence`:
  - existing recorder tests and smoke run remain green

## Notes

- this should be revisited when `run monitor and alarm surface` or coordinator event publication is implemented
