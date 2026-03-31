## Review Findings

### No blocking bugs found.

The new types (`CrossSessionValidationIssue`, `CrossSessionValidationResult`, `ExperimentMonitorInitializationResult`) are well-constructed with defensive copies, null guards, and clear factory methods. The validation logic in `ResolvedExperimentDefinition` is correct, and the coordinator enforcement + UI surfacing paths are consistent.

### Minor observations (non-blocking):

**1. Double validation on the happy path**
`MainViewModel.InitializeRuntimeAsync` calls `ValidateStart()` at initialize time. Then `RuntimeCoordinator.StartCore()` validates again at start time. This is intentional defense-in-depth and costs nothing meaningful, but worth noting as a design choice.

**2. Missing happy-path test for `ValidateCrossSession`**
`ResolvedExperimentDefinitionTests` tests the two failure cases but has no test asserting that a clean experiment returns `IsValid == true`. A one-liner `Assert.True(CreateResolvedExperiment().ValidateCrossSession().IsValid)` would close this.

**3. Missing direct `Start(ResolvedExperimentDefinition)` rejection test**
`RuntimeCoordinatorTests.Start_WithRunContext_Rejects_CrossSession_Invalid_Experiment` exercises the `Start(RunContextDefinition)` overload. The `Start(ResolvedExperimentDefinition)` overload hits the same `StartCore` path so it's not a gap in enforcement, but it is an untested public API entry point.

**4. `Math.Max` alarm/warning count merge is safe but fragile**
In `ExperimentMonitorPanelViewModel.ApplyPendingSnapshotForDisplay` (line ~237), `Math.Max(alarmCount, _displaySnapshot.AlarmCount)` works because when idle with initialization items, snapshot counts are zero. If a future snapshot could carry nonzero counts while idle, the merge would undercount rather than sum. Current behavior is correct.

**5. `"Ready to initialize."` string comparison (pre-existing)**
The `LiveSummary` logic at line ~240 depends on `_displaySnapshot.StateSummary == "Ready to initialize."` ¡ª a magic string coupling that predates this slice but now gates whether blocked-initialization status is visible. Not introduced here, just load-bearing.

### Residual risks

- The `ValidateStart` method on the coordinator is a pass-through today. If cross-session validation ever needs coordinator state (e.g., checking which sessions are already active), the current placement on `ResolvedExperimentDefinition` alone won't be sufficient ¡ª the coordinator method is the right seam for that future work.
- No integration-level test validates the full `Initialize ¡ú blocked ¡ú UI stays disabled` flow through `MainViewModel` with a real coordinator. The unit tests cover each layer independently, which is adequate for V1.
