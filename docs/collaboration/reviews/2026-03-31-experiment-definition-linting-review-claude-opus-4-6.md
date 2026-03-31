## Review Findings

### No blocking bugs found.

The linting slice is structurally sound. Lint runs before cross-session validation in `ValidateRunContextForInitialization` (MainViewModel.cs:248-254). Both the `InitializeRuntimeAsync` and `StartRuntimeCoreAsync` paths are gated. The `_preparedRunContext` is correctly nulled on lint failure. All five planned V1 lint rule families are implemented and the core types are clean.

---

### Minor observations (non-blocking)

**1. Transform reference test only asserts input, not output (P3)**
- **File:** `ExperimentDefinitionLintingTests.cs:11-36`
- **Evidence:** The test creates a transform with *both* `stream.missing_input` (input) and `stream.signal` (output) referencing non-existent streams, but only asserts `unknown_transform_input_stream`. The `unknown_transform_output_stream` issue is also generated but never verified.
- **Why it matters:** If the output-stream reference check were accidentally removed, this test would still pass.
- **Recommended fix:** Add `Assert.Contains(lint.Issues, issue => issue.Code == "unknown_transform_output_stream");`

**2. No test coverage for duplicate detection in collections other than streams (P3)**
- **File:** `ExperimentDefinitionLintingTests.cs`
- **Evidence:** `Lint_Returns_Issue_For_Duplicate_Stream_Id` covers duplicate streams. The `ValidateDuplicateDefinitions` method is called for all eight collections (roles, parameters, streams, transforms, monitors, stop conditions, outputs, control targets), but only the stream case is tested.
- **Why it matters:** The generic helper is likely correct, but one additional case (e.g. duplicate roles) would confirm the wiring for another collection.
- **Recommended fix:** Add one test for a second collection, such as duplicate role ids.

**3. No test for `ScheduleParameterId` reference validation (P3)**
- **File:** `ExperimentDefinition.cs:175-181`, `ExperimentDefinitionLintingTests.cs`
- **Evidence:** `ValidateControlTargetParameterReference` is called for both `TargetParameterId` and `ScheduleParameterId`, but the control-target test only supplies a `targetParameterId`. The schedule path exercises only the null early-return.
- **Why it matters:** A broken schedule-parameter reference check would go undetected.
- **Recommended fix:** Add a test with a control target that includes a non-existent `scheduleParameterId`.

**4. `RequireText` / `ToDisplayName` duplicated across types (P3)**
- **Files:** `ExperimentDefinition.cs:260-271`, `ExperimentDefinitionLintIssue.cs:42-52`
- **Evidence:** Both types contain identical private `RequireText` and `ToDisplayName` helpers.
- **Why it matters:** Minor duplication only. Not worth extracting unless it appears in a third type.

---

### Residual risks

- **Warning severity is unused.** All current lint rules emit `Error`. `Warning = 0` exists in the enum and `IsValid` / `BuildInitializationLintItems` correctly handle warnings (pass-through / filter-out respectively), but no rule currently exercises that path. First rule that emits `Warning` should add a test confirming it does not block initialization.
- **Ad-hoc factory output is not linted in tests.** The `AdHocRunDefinitionFactory` synthesizes experiment packages that should always pass lint (no dangling references by construction). A regression in the factory that introduces a broken reference would only surface at runtime. A single round-trip test (`factory.Create(...).Experiment.Lint().IsValid` == true) would close that gap cheaply.
