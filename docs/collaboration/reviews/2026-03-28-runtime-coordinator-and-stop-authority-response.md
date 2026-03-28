# Runtime Coordinator And Stop Authority Response

## Finding Responses

### F-01

- Status: `Accepted`
- Fix summary:
  - app shutdown now routes through `IRuntimeCoordinator.EnsureStoppedAsync(...)` when the runtime is `Running` or `Stopping`
  - this keeps coordinator state truthful during shutdown instead of bypassing it with a direct registry stop
- Changed files:
  - `platform/src/ExperimentalControlPlatform.Runtime/IRuntimeCoordinator.cs`
  - `platform/src/ExperimentalControlPlatform.Runtime/RuntimeCoordinator.cs`
  - `platform/src/ExperimentalControlPlatform.App/App.xaml.cs`
- Retest status:
  - covered by build, runtime tests, and app smoke launch

### F-02

- Status: `Accepted`
- Fix summary:
  - `MainViewModel.StopRuntime()` now dispatches to `StopRuntimeAsync()` instead of blocking the UI thread with `GetResult()`
  - the runtime status is updated to the intermediate `Stopping` snapshot before awaiting completion
- Changed files:
  - `platform/src/ExperimentalControlPlatform.App/MainViewModel.cs`
  - `platform/src/ExperimentalControlPlatform.App/RuntimeStatusViewModel.cs`
- Retest status:
  - covered by build and app smoke launch

### F-03

- Status: `Accepted`
- Fix summary:
  - added `IRuntimeCoordinator.EnsureStoppedAsync(...)` as the host-level idempotent stop path
  - when shutdown arrives during an in-flight stop, the coordinator now returns the existing stop path instead of double-stopping the registry
- Changed files:
  - `platform/src/ExperimentalControlPlatform.Runtime/IRuntimeCoordinator.cs`
  - `platform/src/ExperimentalControlPlatform.Runtime/RuntimeCoordinator.cs`
  - `platform/src/ExperimentalControlPlatform.App/App.xaml.cs`
- Retest status:
  - covered by `EnsureStoppedAsync_ReusesInFlightStopPathWhileStopping`

### F-04

- Status: `Accepted`
- Fix summary:
  - added recovery coverage for:
    - restart after failed stop
    - cancellation during stop
- Changed files:
  - `platform/tests/ExperimentalControlPlatform.Runtime.Tests/RuntimeCoordinatorTests.cs`
- Retest status:
  - covered by full runtime test pass

## Verification

- `dotnet build .\\platform\\ExperimentalControlPlatform.sln -nodeReuse:false -v:minimal`
- `dotnet test .\\platform\\ExperimentalControlPlatform.sln --no-build -nodeReuse:false -v:minimal`
- app smoke launch:
  - `ALIVE=True`
