## Re-Review: "Run Monitor And Alarm Surface"

### 1. Blocking Issues

**None.** All four previously identified blocking findings remain fixed in the current uncommitted code:

- **B1 (ReplaceSources subscription/leak race):** `ReplaceSources` (`ExperimentMonitorSession.cs:128-150`) holds `_syncRoot` while unsubscribing old sources and subscribing new ones atomically. Old sources are disposed outside the lock. `RecomputeSnapshot` reads `_sources` under the same lock (`line 161-165`). No race.
- **B2 (synchronous-over-async disposal deadlock):** `MainViewModel.Dispose` (`line 156-169`) delegates async disposal through `RunAsyncCleanup` (`line 271-274`) which uses `Task.Run(...).GetAwaiter().GetResult()`, offloading to the thread pool and avoiding the UI sync-context deadlock.
- **B3 (stale `_preparedRunContext` carried across runs):** `_preparedRunContext` is nulled on the success path (`MainViewModel.cs:135`), the failure/catch path (`line 140`), and consumed-then-nulled before start (`line 112`). Both stop-failure and stop-success clear it.
- **B4 (StopCoreAsync returning re-read snapshot):** `StopCoreAsync` (`RuntimeCoordinator.cs:160-192`) captures `stoppedSnapshot` from `_latestSnapshot` inside the lock at `line 185`, then returns that captured value at `line 191`. No re-read.

**Post-last-review changes verified:**

- Stop-failure path clears `_preparedRunContext` at `MainViewModel.cs:140`.
- `ExperimentMonitorPanelViewModel.StopAsync` resets `_isInitialized` and `_initializationStatus` in a `finally` block (`lines 173-179`), so the monitor panel resets even when the stop delegate throws.

### 2. Residual Risks

- **`_disposed` flag not under lock in `DisposeAsync`:** `ExperimentMonitorSession.DisposeAsync` (`line 76-100`) reads/writes `_disposed` without holding `_syncRoot`. A concurrent `RecomputeSnapshot` could race past the `_disposed` check at `line 154`. Low risk: disposal is typically single-threaded and the worst outcome is one extra snapshot publish, not a leak or crash.
- **`RunAsyncCleanup` cleanup chain can short-circuit on exception:** `Task.Run(cleanup).GetAwaiter().GetResult()` in `MainViewModel.cs:273` will propagate exceptions, and `Dispose` does not catch around the two cleanup calls (`lines 159, 163`). If the first cleanup throws, the second cleanup and `panel.Dispose()` calls are skipped. Low risk in practice because these `DisposeAsync` paths are unlikely to throw.
- **Fire-and-forget in `StartRuntime`/`StopRuntime`:** `MainViewModel` uses `_ = ...Async()` at `lines 63` and `76`, so exceptions on those wrapper entry points are not observed directly. Acceptable for now if the real UI path goes through the async methods, but worth revisiting if command error surfacing becomes important.

### 3. Summary

All four previously accepted blocking findings remain fixed. The additional cleanup changes to clear `_preparedRunContext` on the stop-failure path and to reset monitor-panel initialization state in the `StopAsync` `finally` block are present and correct. No new blocking issues were found. The remaining risks are low severity and do not block landing this slice.
