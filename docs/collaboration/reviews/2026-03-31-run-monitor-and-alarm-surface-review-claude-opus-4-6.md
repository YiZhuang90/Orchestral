Now I have a comprehensive picture of the changes. Let me produce the review.

## Run Monitor And Alarm Surface Slice Review

### Blocking Issues

**B1. `ReplaceSources` leaks event subscriptions on replacement**
`ExperimentMonitorSession.cs:128-146` ¡ª When `HandleRegistryChanged` fires, `ReplaceSources` is called with newly created sources. Inside the lock, it subscribes `Changed` on all *new* sources, but if `ReplaceSources` is called again before the old sources are unsubscribed (which happens outside the lock), there is a window where events from old sources still fire `HandleSourceChanged` after they have been conceptually replaced. More critically: if the new source list contains a source object that was already in `_sources` (not the case today since `CreateSources` always creates new wrappers, but fragile), it would double-subscribe. The real concern is that unsubscribe+dispose of old sources happens **outside the lock** while new event handlers are already live, so a concurrent `RecomputeSnapshot` from an old source's `Changed` event could read the new `_sources` list with partially-disposed old sources still firing. This is a race between `RecomputeSnapshot` reading `_sources` under lock and old-source disposal outside the lock.

**Severity**: P1 ¡ª Potential `InvalidOperationException` from `CreateSnapshot()` on a disposed source during the race window.

**B2. `Dispose` in `MainViewModel` synchronously blocks on async disposal**
`MainViewModel.cs:148-153` ¡ª `_monitorSession.DisposeAsync().AsTask().GetAwaiter().GetResult()` and the same pattern for `_controllerUnitSession` will deadlock if called on the UI thread and either `DisposeAsync` captures a synchronization context. While the current `ExperimentMonitorSession.DisposeAsync` is synchronous (`ValueTask.CompletedTask`), this is a latent deadlock for any future async work in disposal, and it violates the `IAsyncDisposable` contract expectations.

**Severity**: P2 ¡ª No deadlock today because disposal is synchronous, but the pattern is a known WPF deadlock trap.

**B3. `_preparedRunContext` is not cleared on stop, only on start**
`MainViewModel.cs:113` clears `_preparedRunContext = null` only after a successful start. If `InitializeRuntimeAsync` is called, setting `_preparedRunContext`, but then the user never starts (or start fails), the stale context persists. A subsequent `StartRuntime()` (without re-initializing) would pick up the stale context silently. This could cause a run to start with outdated operator intent.

**Severity**: P2 ¡ª Silent use of stale run context after a failed or skipped start.

**B4. `RuntimeCoordinator.StopCoreAsync` returns `LatestSnapshot` after publishing, not `stoppedSnapshot`**
`RuntimeCoordinator.cs:184` ¡ª After the `finally` block publishes `stoppedSnapshot`, the method returns `LatestSnapshot` (line ~184 in diff context, original line `return LatestSnapshot;`). This reads the property again, which acquires the lock a second time. In normal operation this returns the same value, but if another thread calls `Start()` between the `PublishSnapshotChanged` and the `return`, the caller of `RequestStopAsync` receives the *new running* snapshot instead of the *stopped* snapshot. This is a race condition in the stop¡ústart transition.

**Severity**: P1 ¡ª Caller of `RequestStopAsync` could receive a `Running` snapshot instead of `Idle`, causing `FinalizeRunRecording` to skip recording (it checks `snapshot.State != RunState.Idle`).

### Non-Blocking Improvements

**N1. `ActiveDeviceNames` computed property on `ExperimentMonitorSnapshot` allocates on every access**
`ExperimentMonitorSnapshot.cs:41-42` ¡ª `ActiveDeviceNames` does `.Select(...).ToArray()` every time it is accessed. Since `ExperimentMonitorSnapshot` is a `record` with init-only properties, this could be computed once during construction or cached.

**N2. Duplicate `sessionSourceId` / `controlCenterSourceId` local functions**
`ExperimentMonitorSession.cs:437-441` ¡ª These two local functions have identical implementations. Consolidate into one.

**N3. `ExperimentMonitorPanelViewModel` does not reset `_isInitialized` on stop completion**
`ExperimentMonitorPanelViewModel.cs:98` ¡ª `CanStart` checks `_isInitialized && RunState == Idle`. After a run completes and returns to `Idle`, the previous `_isInitialized = true` persists, so `CanStart` is `true` without re-initialization. This may be intentional for rapid re-start, but the `InitializationStatus` still shows the old run's message, which could mislead the operator.

**N4. `DeviceSessionRegistry.SessionsChanged` event fires outside the lock**
`DeviceSessionRegistry.cs:37,63-65` ¡ª This is correct for avoiding deadlocks, but the event is a plain `Action?` with no error handling. If a subscriber throws, the registry method (`GetOrAdd`, `Remove`, `StopAllAsync`) propagates the exception to the caller, potentially corrupting caller state. Consider wrapping the invocation in a try/catch or using a safer event-dispatch pattern.

**N5. `AdHocRunDefinitionFactory` hardcodes `"role.control_center"` artifact ID**
`AdHocRunDefinitionFactory.cs:34` ¡ª The role ID `"role.control_center"` is matched with `==` against `new ArtifactId(...)`. This works because `ArtifactId` presumably has value equality, but the string is a magic constant duplicated across the factory and wherever the role is defined. Consider extracting to a shared constant.

**N6. Monitor snapshot serialization uses hand-rolled YAML dictionaries**
`RunArtifactWriter.cs:95-145` ¡ª The `WriteYaml` approach with nested `Dictionary<string, object?>` is fragile for representing lists of items. If the existing `WriteYaml` helper doesn't handle nested arrays well, this could produce malformed YAML. Not blocking because the existing panel-snapshot path presumably works, but worth verifying the output for the more complex monitor snapshot structure.

### Testing Notes

**Covered by tests:**
- Controller `Target +/- Error` appears in monitor snapshot (pass)
- Stale controller measured value becomes a warning (pass)
- Disconnected critical control-center session while running becomes an alarm (pass)
- Display-rate selection updates ticker interval only (pass)
- Tick applies pending snapshot to display state (pass)
- Ad-hoc factory adds primary control target when value is provided (pass)
- Run artifact writer writes monitor snapshot artifact and includes monitor warnings (pass)
- Main view model exposes the monitor panel as `CurrentDevicePanel` (pass)

**Missing test coverage (per plan):**
- **Initialize/start/stop commands update monitor surface state** ¡ª No test calls `InitializeAsync` ¡ú `StartAsync` ¡ú `StopAsync` on the panel view model and verifies state transitions across the full lifecycle.
- **Status bar summary reflects monitor severity** ¡ª Only indirectly tested through `FooterHealthLabel` in the tick test; no test verifies the `FooterRunLabel` path or severity escalation from `Warning` to `Alarm`.
- **Monitor snapshot updates when coordinator state changes without controller** ¡ª No test verifies that the monitor session recomputes when the coordinator transitions (e.g., `Start` ¡ú `Stop`) without a controller attached.
- **`ExperimentMonitorSession` with real `IDeviceSessionRegistry` constructor** ¡ª The registry-based constructor path (line 37-48) has no dedicated test. The `HandleRegistryChanged` ¡ú `ReplaceSources` ¡ú `CreateSources` path is untested.
- **`RuntimeCoordinator.SnapshotChanged` fires on stop** ¡ª Only tested for start. No test verifies the event fires during `RequestStopAsync` or `EnsureStoppedAsync`.
- **`MainViewModel.InitializeRuntimeAsync` integration** ¡ª No test exercises the initialize ¡ú start ¡ú stop lifecycle through `MainViewModel`, only the existing start ¡ú stop path.

### Summary

The slice delivers a structurally sound first-generation monitor session, panel view model with display decimation, and recording integration. The architecture follows the documented contract: the monitor consumes runtime snapshots rather than raw device APIs, and the UI is a client of the background session.

Two P1 issues need attention: the race in `RuntimeCoordinator.StopCoreAsync` returning `LatestSnapshot` instead of the captured `stoppedSnapshot` (B4), and the source-replacement race in `ExperimentMonitorSession.ReplaceSources` (B1). The stale `_preparedRunContext` issue (B3) and synchronous-over-async disposal pattern (B2) are P2 but should be addressed before this slice lands.

Test coverage matches most of the plan's runtime test cases but is missing the registry-constructor integration path, the full initialize¡ústart¡ústop lifecycle through the view model, and coordinator `SnapshotChanged` verification on stop transitions.
