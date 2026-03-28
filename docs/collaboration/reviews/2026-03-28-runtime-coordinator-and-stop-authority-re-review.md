# Runtime Coordinator And Stop Authority Re-Review

Re-review of the runtime coordinator / stop-authority slice after fixes documented in
`2026-03-28-runtime-coordinator-and-stop-authority-response.md`.

## Verification Summary

| ID | Original Sev | Status | Evidence |
|----|-------------|--------|----------|
| **F-01** | **P1** | **Resolved** | `App.xaml.cs:39-43` routes shutdown through `IRuntimeCoordinator.EnsureStoppedAsync` when coordinator state is `Running` or `Stopping`. Coordinator state is now truthful during shutdown. |
| F-02 | P2 | **Resolved** | `MainViewModel.StopRuntime()` dispatches to `StopRuntimeAsync()` (fire-and-forget with internal error handling). The intermediate `Stopping` snapshot is pushed to `RuntimeStatus` at line 56 before awaiting completion. UI thread is never blocked. |
| F-03 | P2 | **Resolved** | `EnsureStoppedAsync` reuses `_activeStopTask` when state is already `Stopping` (lines 84-101), preventing double-stop. Verified by `EnsureStoppedAsync_ReusesInFlightStopPathWhileStopping` which asserts `StopAllCallCount == 1`. |
| F-04 | P3 | **Resolved** | Two new tests added: `Start_AfterFailedStop_BeginsNewRunAndClearsPreviousStopRequest` (restart-after-failed-stop) and `RequestStopAsync_CancellationStillFinalizesStoppedSnapshot` (cancellation-during-stop recovery). |

## Code Review Notes

### Coordinator (`RuntimeCoordinator.cs`)

- `EnsureStoppedAsync` correctly handles all three states: returns current snapshot if `Idle`, joins in-flight task if `Stopping`, initiates new stop if `Running`.
- `_activeStopTask` is assigned inside the same lock that transitions to `Stopping`, so the null-check at line 87-89 is a valid defensive assertion (cannot be null under the lock if state is `Stopping`).
- `StopCoreAsync` clears `_activeStopTask` in `finally`, but callers that captured the `Task` reference before this point still await it correctly.
- `BeginStop` is factored cleanly and shared between `RequestStopAsync` and `EnsureStoppedAsync`.

### App Shell (`App.xaml.cs`)

- The else branch (line 46) still calls `_sessionRegistry?.StopAllAsync` directly when the coordinator is idle or null. This is a correct safety net for shutdown -- it catches any sessions that exist outside the coordinator lifecycle without conflicting with coordinator state.

### ViewModel (`MainViewModel.cs`, `RuntimeStatusViewModel.cs`)

- Fire-and-forget in `StopRuntime()` is safe because `StopRuntimeAsync` has a full `try/catch` that routes errors to `RuntimeStatus.ShowOperationError`.
- The `Stopping` state is rendered because `RuntimeStatus.Update(_runtimeCoordinator.LatestSnapshot)` runs synchronously before awaiting the stop task.

### Tests (`RuntimeCoordinatorTests.cs`)

- 13 coordinator tests cover: idle baseline, start, stop lifecycle, duplicate-start/stop rejection, `EnsureStoppedAsync` join, registry delegation, restart-after-stop, snapshot retention, failed-stop recovery, restart-after-failed-stop, and cancellation-during-stop.
- `FakeRegistry` with `blockStopUntilReleased` and `stopFailure` modes provides good control over async and error paths.

## Build and Test

- `dotnet build` -- 0 warnings, 0 errors.
- `dotnet test` -- 96 passed, 0 failed, 0 skipped.

## New Findings

None.

## Verdict

**This slice is merge-ready.** All four original findings (F-01 through F-04) are resolved. No new blocking or non-blocking issues found.
