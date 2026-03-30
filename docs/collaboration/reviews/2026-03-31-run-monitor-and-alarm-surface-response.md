## Run Monitor And Alarm Surface Slice Response

### B1

- `Accepted`
- fix summary:
  - changed `ExperimentMonitorSession.ReplaceSources(...)` so old source handlers are unsubscribed before swapping the active source set
  - old sources are disposed only after they are no longer able to raise `HandleSourceChanged`
- changed files:
  - `platform/src/ExperimentalControlPlatform.Runtime/ExperimentMonitorSession.cs`
- retest status:
  - covered by full solution build and runtime/app test pass

### B2

- `Accepted`
- fix summary:
  - removed direct UI-thread `GetAwaiter().GetResult()` calls on async disposal in `MainViewModel.Dispose()`
  - cleanup now runs through a thread-pool wrapper so future async disposal work does not deadlock the WPF close path
- changed files:
  - `platform/src/ExperimentalControlPlatform.App/MainViewModel.cs`
- retest status:
  - covered by build/test pass and app smoke launch/close

### B3

- `Accepted`
- fix summary:
  - cleared `_preparedRunContext` before start execution and again on successful stop
  - the monitor panel now resets initialization state after stop so the next run requires fresh initialize intent
- changed files:
  - `platform/src/ExperimentalControlPlatform.App/MainViewModel.cs`
  - `platform/src/ExperimentalControlPlatform.App/ExperimentMonitor/ExperimentMonitorPanelViewModel.cs`
- retest status:
  - new panel lifecycle test covers initialize -> start -> stop reset behavior

### B4

- `Accepted`
- fix summary:
  - `RuntimeCoordinator.StopCoreAsync(...)` now returns the captured `stoppedSnapshot` instead of rereading `LatestSnapshot`
  - added test coverage that `SnapshotChanged` fires on stopping and stopped transitions
- changed files:
  - `platform/src/ExperimentalControlPlatform.Runtime/RuntimeCoordinator.cs`
  - `platform/tests/ExperimentalControlPlatform.Runtime.Tests/RuntimeCoordinatorTests.cs`
- retest status:
  - runtime tests passed

### N1

- `Rejected`
- technical reason:
  - `ActiveDeviceNames` allocation is real but low-impact and not on a hot path used by the panel or recorder today
  - keeping the record shape simple is preferable in this slice

### N2

- `Accepted`
- fix summary:
  - consolidated the duplicate source-id helpers into one shared local helper
- changed files:
  - `platform/src/ExperimentalControlPlatform.Runtime/ExperimentMonitorSession.cs`
- retest status:
  - covered by build/test pass

### N3

- `Accepted`
- fix summary:
  - stop now resets monitor-panel initialization state and status text
- changed files:
  - `platform/src/ExperimentalControlPlatform.App/ExperimentMonitor/ExperimentMonitorPanelViewModel.cs`
- retest status:
  - covered by the panel lifecycle test

### N4

- `Rejected`
- technical reason:
  - `SessionsChanged` currently has one internal consumer and exceptions should surface loudly during development
  - swallowing subscriber exceptions at the registry boundary would hide real runtime integration bugs

### N5

- `Rejected`
- technical reason:
  - the `role.control_center` constant is intentionally localized to the ad hoc factory path for now
  - extracting a shared constant is not justified until a second production caller needs it

### N6

- `Partially accepted`
- fix summary:
  - monitor snapshot writing is now covered by a dedicated artifact-writer test that verifies the YAML artifact is emitted and warning content is present
  - no serializer refactor was made in this slice
- changed files:
  - `platform/tests/ExperimentalControlPlatform.App.Tests/RunRecorderTests.cs`
- retest status:
  - app tests passed

## Verification

- `dotnet build .\\platform\\ExperimentalControlPlatform.sln -nodeReuse:false -v:minimal`
- `dotnet test .\\platform\\ExperimentalControlPlatform.sln --no-build -nodeReuse:false -v:minimal`
- smoke launch:
  - app stayed alive for 6 seconds with no startup exception
