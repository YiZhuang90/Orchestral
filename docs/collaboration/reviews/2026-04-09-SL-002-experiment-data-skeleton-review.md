# SL-002: Experiment Data Skeleton -- Review

- **Reviewer**: Claude Opus 4.6
- **Date**: 2026-04-09
- **Slice Plan**: `docs/development_v2/2026-04-04-SL-002-experiment-data-skeleton-slice-plan.md`
- **Worktree**: `Orchestral-sl-002`

---

## Summary

SL-002 defines the standard file/folder structure for Orchestral experiment data and updates `RunArtifactWriter` to produce output following the new structure. The delivery has two parts: a specification document (`EXPERIMENT_DATA_SKELETON.md`) and a code update to `RunArtifactWriter.cs` with 5 new tests.

The implementation is clean and well-aligned with the slice plan. All five success criteria are met. The spec is internally consistent, clearly separates guaranteed from conditional content, and defines a usable consumer contract. The code changes are minimal and surgical -- path rearrangement plus one new artifact (`applied-parameters.yaml`). Build passes with 0 warnings, 0 errors. All 187 tests pass (52 Core + 79 Runtime + 14 Devices + 11 ExperimentLogic + 31 App).

Three findings are raised below: one P3 regarding missing content validation of the new `applied-parameters.yaml` artifact in tests, one P3 regarding a minor spec/code inconsistency in the `events/warnings-or-faults.yaml` presence table, and one P3 regarding the dropped `artifacts/` subdirectory from the slice plan proposal. None are blocking.

---

## Plan Alignment

### Achieved

1. **Specification document** (`docs/architecture/EXPERIMENT_DATA_SKELETON.md`) exists and covers all required topics: experiment project hierarchy, run directory hierarchy, file naming conventions, storage format mapping, guaranteed vs. optional content, and AI data access contract (consumer contract).
2. **Path reorganization** in `RunArtifactWriter` matches the spec exactly:
   - `runtime-events.yaml` -> `events/runtime-events.yaml`
   - `warnings-or-faults.yaml` -> `events/warnings-or-faults.yaml`
   - `panels/` -> `snapshots/panels/`
   - `monitor-snapshot.yaml` -> `snapshots/monitor-snapshot.yaml`
   - `flow-reynolds-snapshot.yaml` -> `data/derived/flow-reynolds-snapshot.yaml`
   - `flow-reynolds-record.yaml` -> `data/derived/flow-reynolds-record.yaml`
3. **New artifact**: `metadata/applied-parameters.yaml` is written with experiment parameters and role-binding parameter values.
4. **Manifest stays at root** and references all artifacts via organized relative paths with forward slashes.
5. **Directory creation rules** match the spec: `events/`, `snapshots/panels/`, `metadata/` are always created; `data/` and `data/derived/` are only created when data artifacts exist.
6. **5 new tests** verify the organized structure, manifest paths, derived data placement, monitor placement, and conditional data directory creation.
7. **All 7 existing tests pass** unchanged -- no regressions.
8. **RunRecorder** was not modified, which is correct because it delegates all path logic to `RunArtifactWriter`.
9. **Spec includes placeholders** for calibration, decisions, and reports (Section 9).
10. **Consumer contract** (Section 8) defines the discovery protocol, rules for consumers, and includes worked examples.

### Deviated

1. The slice plan proposed an `artifacts/` subdirectory in the run hierarchy. The final spec omits this, instead using the `artifacts` dictionary in `manifest.yaml` as the discovery mechanism. This is a justified improvement -- an `artifacts/` physical directory would duplicate the manifest's role and create ambiguity about where to look.

---

## Testing Evidence

### What was run

```
dotnet build platform/ExperimentalControlPlatform.sln
dotnet test platform/ExperimentalControlPlatform.sln --no-build
```

### What passed

- Build: 0 warnings, 0 errors.
- Tests: 187 total. 52 Core + 79 Runtime + 14 Devices + 11 ExperimentLogic + 31 App. All passed.

### Structural verification

Verified via code review of `RunArtifactWriter.cs` diff:
- Path constants in the code match the spec's directory hierarchy exactly.
- `WriteYaml` creates parent directories lazily via `Directory.CreateDirectory(Path.GetDirectoryName(path)!)`, so `events/` and `metadata/` are created by writing to them, not speculatively.
- `snapshots/panels/` is eagerly created (line 55), consistent with the spec saying panel snapshots are "always" present.
- `data/derived/` is only created when `derivedStateSnapshot` or `derivedStateSamples` are provided, consistent with the spec's conditional rule.

### What was not verified

- Runtime UI behavior (WPF app not launched).
- End-to-end run recording through `MainViewModel.StopRun` to disk.
- Content of `applied-parameters.yaml` YAML output (only file existence and manifest path are tested; see finding CC-SL002-001).

---

## Findings

---

### Finding CC-SL002-001

- `ID`: `CC-SL002-001`
- `Severity`: `P3`
- `Area`: `Test coverage`
- `File`: `platform/tests/ExperimentalControlPlatform.App.Tests/RunRecorderTests.cs`

#### Evidence

The new `applied-parameters.yaml` artifact is verified in two ways:
1. File existence: `Assert.True(File.Exists(Path.Combine(result.RunDirectoryPath, "metadata", "applied-parameters.yaml")))` (line 378).
2. Manifest reference: `Assert.Contains("metadata/applied-parameters.yaml", manifestYaml)` (line 438).

However, no test reads the YAML content of `applied-parameters.yaml` to verify its structure. Every other artifact type has at least one test that reads the file content and asserts on specific YAML keys (e.g., `reynoldsNumber: 2310.5` for derived state, `payloadValue: 640x480` for panel snapshots, `controller.re_primary` for warnings).

#### Expected

At least one test should read `applied-parameters.yaml` and assert on its YAML content (e.g., `kind: applied_parameters`, `experimentId`, `parameterValues`).

#### Actual

The file's existence and manifest path are tested, but the actual serialized content is not validated.

#### Why It Matters

If the serialization structure of `applied-parameters.yaml` regresses (e.g., a key is renamed or omitted), the existing tests would not catch it. This is the only artifact type in the writer that lacks a content assertion.

#### Recommended Fix

Add one content assertion in an existing or new test, for example:

```csharp
var appliedParamsPath = Path.Combine(result.RunDirectoryPath, "metadata", "applied-parameters.yaml");
var appliedParamsYaml = File.ReadAllText(appliedParamsPath);
Assert.Contains("kind: applied_parameters", appliedParamsYaml);
Assert.Contains("experimentId:", appliedParamsYaml);
```

---

### Finding CC-SL002-002

- `ID`: `CC-SL002-002`
- `Severity`: `P3`
- `Area`: `Spec consistency`
- `File`: `docs/architecture/EXPERIMENT_DATA_SKELETON.md:119`

#### Evidence

Section 5 (Guaranteed vs. Optional Content) states:

> `events/warnings-or-faults.yaml` | **Always** | Every run collects warnings (may be empty list)

This is accurate for the code -- the writer always creates this file, even when the warnings list is empty. However, the parenthetical "(may be empty list)" could be read ambiguously: does the file always exist (yes), or is the list always non-empty (no). The current phrasing is technically correct but could be clearer.

Similarly, `snapshots/panels/` is listed as "Always" with the note "directory may contain zero files if no panels." The conditions under which zero panel files occur are worth noting: this happens when the `panels` parameter contains no `IIntegrationPanelViewModel` implementations (which filters out non-integration panels).

#### Expected

Unambiguous language confirming the file always exists and the content may be an empty list.

#### Actual

The language is technically correct but could be more explicit about the empty-content case.

#### Why It Matters

An AI agent consumer reading this spec needs to know with certainty that it can always open `events/warnings-or-faults.yaml` without checking for file existence. The current wording is close but relies on parsing the parenthetical correctly.

#### Recommended Fix

Minor wording improvement (optional):

> `events/warnings-or-faults.yaml` | **Always** | File always exists; the `items` list may be empty when no warnings were collected.

---

### Finding CC-SL002-003

- `ID`: `CC-SL002-003`
- `Severity`: `P3`
- `Area`: `Plan deviation (beneficial)`
- `File`: `docs/development_v2/2026-04-04-SL-002-experiment-data-skeleton-slice-plan.md:59`

#### Evidence

The slice plan's proposed folder structure includes an `artifacts/` subdirectory:

```
<run_id>/
  ...
  artifacts/               # any additional run artifacts
```

The final spec omits this directory entirely. The spec instead uses the `artifacts` dictionary in `manifest.yaml` (Section 8) as the canonical mechanism for locating any artifact by ID.

#### Expected

Acknowledge the deviation was intentional.

#### Actual

The deviation is not explicitly documented in the spec.

#### Why It Matters

This is a beneficial deviation. A physical `artifacts/` directory would create confusion: are artifacts located by navigating directories or by reading the manifest? The manifest-only approach is cleaner and consistent with the consumer contract rule "Never hardcode paths beyond `manifest.yaml`." Noting this explicitly removes any ambiguity for future readers of the slice plan vs. the spec.

#### Recommended Fix

No code change needed. Optionally, add a brief note in the spec or the slice plan acknowledging that the `artifacts/` directory was dropped in favor of the manifest-based discovery approach.

---

## Review Checklist Results

| Check | Result |
|-------|--------|
| Dependency flow violations | None. `RunArtifactWriter` remains in App (L4), references ExperimentLogic (L3), Runtime (L2), Core (L1). No new dependency edges. |
| Pattern adherence | Yes. Follows the existing `WriteYaml` pattern. New `applied-parameters.yaml` artifact follows the same serialization approach as all other artifacts. |
| Nullable correctness | Yes. The `Path.GetDirectoryName(path)!` null-forgiving operator is correct because the paths are always constructed from `Path.Combine` with non-null components. |
| Test coverage | Good. 5 new tests cover directory structure, manifest paths, derived data placement, monitor placement, and conditional directory creation. One gap: `applied-parameters.yaml` content not validated (CC-SL002-001, P3). |
| L2/L3 boundary | No violations. The `applied-parameters.yaml` artifact reads from `resolvedExperiment.ParameterValues` and `resolvedExperiment.RoleBindings`, which are resolved experiment data already available at the App layer. No new L2/L3 coupling. |
| Spec quality | High. Internally consistent. Clear two-level model. Good separation of guaranteed vs. conditional. Storage format mapping is forward-looking without over-specifying. |
| Consumer contract completeness | Good. Discovery protocol is clear. Examples are helpful. The "Never hardcode paths" and "Check before assuming" rules are well-stated. |

---

## What Was Done Well

1. **Spec is well-structured and self-contained.** The two-level data model (experiment vs. run) is clearly explained. The guaranteed vs. optional table is immediately useful. The consumer contract with worked examples is exactly what an AI agent would need to navigate run data.

2. **Minimal, surgical code changes.** The diff is clean: 6 path changes and 1 new artifact. No behavioral changes to existing logic. The `WriteYaml` helper's `Directory.CreateDirectory` call handles all subdirectory creation without additional plumbing.

3. **Directory creation rules are correct.** `snapshots/panels/` is eagerly created (always present). `events/` and `metadata/` are created via `WriteYaml`'s `Directory.CreateDirectory` call when files are written (always present because events and metadata are always written). `data/derived/` is only created when data artifacts exist (conditional). This matches the spec exactly.

4. **Forward-slash normalization.** The existing `NormalizeRelativePath` method ensures manifest paths use forward slashes for cross-platform compatibility, which the spec explicitly requires.

5. **Regression protection.** The new tests explicitly assert that old flat paths do NOT exist (`Assert.False(File.Exists(...))`), providing a regression guard against accidentally reverting to the pre-SL-002 structure.

6. **Placeholder sections are well-scoped.** The calibration, decisions, and reports placeholders define location and purpose without over-specifying implementation, which keeps them useful without creating premature commitments.

---

## Verdict

**Pass with non-blocking findings.**

- CC-SL002-001 (P3): Add content validation for `applied-parameters.yaml` in tests.
- CC-SL002-002 (P3): Minor spec wording improvement for empty-content cases.
- CC-SL002-003 (P3): Acknowledge beneficial `artifacts/` directory omission from slice plan.

All five success criteria from the slice plan are met:
1. `EXPERIMENT_DATA_SKELETON.md` exists and defines the standard.
2. `RunArtifactWriter` produces output following the standard.
3. Tests verify the new folder structure and file locations.
4. The spec includes placeholders for calibration, decisions, and reports.
5. The spec includes an AI data access contract section.

The implementation is ready to land.
