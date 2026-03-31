## Re-Review: Experiment-Definition Linting Slice

### No blocking bugs found.

All four accepted fixes from the initial review are correctly applied and verified in the current working tree.

---

### Verification of implemented fixes

**1. Transform output stream assertion ！ Fixed**
- `ExperimentDefinitionLintingTests.cs:36` now asserts both `unknown_transform_input_stream` and `unknown_transform_output_stream`. The test fixture already had both dangling references; only the assertion was missing. Confirmed.

**2. Duplicate role-id test ！ Fixed**
- `ExperimentDefinitionLintingTests.cs:131-158` adds `Lint_Returns_Issue_For_Duplicate_Role_Id`, which wires a second collection through the generic `ValidateDuplicateDefinitions` path. This confirms the generic helper is correctly invoked for a non-stream collection. Confirmed.

**3. Schedule parameter reference test ！ Fixed**
- `ExperimentDefinitionLintingTests.cs:161-197` adds `Lint_Returns_Issue_For_Unknown_Control_Target_Schedule_Parameter`. The fixture uses `scheduleParameterId:` (not `targetParameterId:`), which correctly exercises the second `ValidateControlTargetParameterReference` call at `ExperimentDefinition.cs:175-181`. Confirmed.

**4. Ad hoc factory lint round-trip ！ Fixed**
- `AdHocRunDefinitionFactoryTests.cs:14-35` synthesizes an experiment with `primaryControlTargetValue: 1600`, which exercises the most complex synthesis path (control-center streams, parameters, and a control target). The `Assert.True(lint.IsValid)` / `Assert.Empty(lint.Issues)` pair confirms no dangling references are introduced by construction. Confirmed.

**5. Helper duplication ！ Not adopted (agreed)**
- `RequireText`/`ToDisplayName` remain duplicated across `ExperimentDefinition.cs:260-271` and `ExperimentDefinitionLintIssue.cs:41-52`. Still only two occurrences; extraction not warranted.

---

### Structural re-check

- **Gate ordering** in `ValidateRunContextForInitialization` (`MainViewModel.cs:248-254`): lint runs first, cross-session validation second. Both `InitializeRuntimeAsync` (line 74) and `StartRuntimeCoreAsync` (line 139) funnel through this gate. Confirmed.
- **`_preparedRunContext` nulled on failure**: `InitializeRuntimeAsync` sets it to `null` at line 77 when `!initializationValidation.IsReady`. Confirmed.
- **`BuildInitializationLintItems`** (`MainViewModel.cs:412-413`): correctly filters to `Error` severity only, so warnings won't block initialization. Confirmed.
- **`IsValid`** (`ExperimentDefinitionLintResult.cs:17`): `Issues.All(issue => issue.Severity != Error)` ！ correct; an empty issue list returns `true`. Confirmed.
- **Integration test** (`MainViewModelTests.cs:162-177`): `InvalidLintRunDefinitionFactory` injects a broken monitor reference that fails lint, and the test asserts `IsReady == false` with the expected status message and validation source. Confirmed.
- **Positive path test** (`ExperimentDefinitionLintingTests.cs:199-271`): exercises all five lint rule families (duplicates, transform refs, monitor refs, control-target refs, parameter refs) on a clean definition and asserts zero issues. Confirmed.

---

### Residual observations carried forward (unchanged)

- **Warning severity is still unused.** All current rules emit `Error`. First rule that emits `Warning` should add a test confirming it does not block initialization. (Not a bug ！ just a gap to close when the path is exercised.)
- **Fallback factory path not tested.** The ad hoc factory round-trip test covers the control-center panel path. The zero-panel fallback (`BuildFallbackPanelEntry`) produces a trivially valid experiment (no streams, no transforms, no monitors, no control targets), so it would pass lint by construction. A test is not strictly needed but could be added for completeness if desired.
