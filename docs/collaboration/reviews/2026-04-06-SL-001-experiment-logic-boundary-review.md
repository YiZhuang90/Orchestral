# SL-001: Experiment-Logic Code Boundary — Review

- **Reviewer**: Claude Opus 4.6
- **Date**: 2026-04-06
- **Slice Plan**: `docs/development_v2/2026-04-04-SL-001-experiment-logic-boundary-slice-plan.md`
- **Worktree**: `Orchestral-SL-001`

---

## Summary

SL-001 establishes the L2/L3 code boundary by creating the `ExperimentalControlPlatform.ExperimentLogic` project, relocating the FlowReynolds derived-state types from Runtime to ExperimentLogic, and introducing two interfaces (`IDerivedStateSession`, `IDerivedStateSnapshot`) in Runtime so that Runtime infrastructure can consume derived state without referencing ExperimentLogic.

The implementation is well-executed. The primary architectural goal -- Runtime must not reference ExperimentLogic -- is achieved cleanly. Build passes with 0 warnings, 0 errors. All 182 tests pass (52 Core + 79 Runtime + 14 Devices + 11 ExperimentLogic + 26 App). The test refactoring in Runtime.Tests, replacing concrete FlowReynolds types with stub implementations of the interfaces, is a strong demonstration that the boundary is real.

Two findings are raised below: one P2 regarding a plan deviation in the Devices dependency, and one P2 regarding the `IDerivedStateSnapshot` interface containing experiment-specific property names. Neither is blocking.

---

## Plan Alignment

### Achieved

1. New project `ExperimentalControlPlatform.ExperimentLogic` exists and compiles.
2. `FlowReynoldsDerivedStateSession.cs`, `FlowReynoldsDerivedStateSnapshot.cs`, and `FlowReynoldsDerivedStateSample.cs` relocated from Runtime to ExperimentLogic.
3. Solution file updated with both source and test projects.
4. App project references ExperimentLogic for DI wiring.
5. 5 integration tests in `FlowReynoldsIntegrationTests` prove the L2/L3 boundary.
6. 6 migrated unit tests in `FlowReynoldsDerivedStateSessionTests` pass in the new project.
7. Runtime has zero references to `ExperimentLogic` (grep-verified).
8. Runtime.Tests has zero references to `ExperimentLogic` (grep-verified); uses stub implementations.
9. `ExperimentMonitorSession` consumes `IDerivedStateSession`/`IDerivedStateSnapshot` via interface, not concrete types.

### Deviated

1. The slice plan states ExperimentLogic "Does NOT reference ExperimentalControlPlatform.Devices directly." The actual implementation references Devices because `FlowReynoldsDerivedStateSession` consumes `ControlCenterPulseReadback` (defined in `Devices.ControlCenter`) and interacts with `ControlCenterSession` (defined in Runtime, but which itself exposes Devices types). See finding CC-SL001-001.

---

## Testing Evidence

### What was run

```
dotnet build platform/ExperimentalControlPlatform.sln
dotnet test platform/ExperimentalControlPlatform.sln
```

### What passed

- Build: 0 warnings, 0 errors.
- Tests: 182 total. 52 Core + 79 Runtime + 14 Devices + 11 ExperimentLogic + 26 App. All passed.

### Boundary verification

```
grep -r "ExperimentLogic" platform/src/ExperimentalControlPlatform.Runtime/  → 0 matches
grep -r "ExperimentLogic" platform/tests/ExperimentalControlPlatform.Runtime.Tests/  → 0 matches
```

### What was not verified

- Runtime UI behavior (WPF app not launched).
- End-to-end run recording with real derived state attached (the existing `RunRecorderTests` and `RunArtifactWriter` tests cover serialization of FlowReynolds types from App.Tests, which now correctly references ExperimentLogic).

---

## Findings

---

### Finding CC-SL001-001

- `ID`: `CC-SL001-001`
- `Severity`: `P2`
- `Area`: `Dependency flow`
- `File`: `platform/src/ExperimentalControlPlatform.ExperimentLogic/ExperimentalControlPlatform.ExperimentLogic.csproj`

#### Evidence

The ExperimentLogic csproj contains:

```xml
<ProjectReference Include="..\ExperimentalControlPlatform.Devices\ExperimentalControlPlatform.Devices.csproj" />
```

The slice plan (Section 1) states: "Does NOT reference ExperimentalControlPlatform.Devices directly (experiment logic consumes runtime, not raw device APIs)."

`FlowReynoldsDerivedStateSession.cs` line 8 uses `using ExperimentalControlPlatform.Devices.ControlCenter;` and directly references `ControlCenterPulseReadback` (a Devices type) and `ControlCenterSession` (a Runtime type that surfaces Devices DTOs).

#### Expected

ExperimentLogic references only Core and Runtime, per the slice plan.

#### Actual

ExperimentLogic also references Devices, because the FlowReynolds session directly consumes device-layer data transfer objects (`ControlCenterPulseReadback`).

#### Why It Matters

This is a justified deviation, not a bug. The existing `FlowReynoldsDerivedStateSession` was written before the L3 layer existed and naturally consumed Devices types. Removing this dependency would require extracting additional runtime-level abstractions for pulse telemetry data, which is out of scope for this slice. However, this deviation should be explicitly acknowledged and tracked, because the architectural intent is that experiment logic should consume runtime abstractions, not raw device types. The actual dependency flow is: `ExperimentLogic -> Runtime -> Core <- Devices` AND `ExperimentLogic -> Devices`, which introduces a diamond dependency.

#### Recommended Fix

1. Accept as a known deviation for SL-001.
2. Track as a future cleanup item: extract a runtime-level pulse telemetry interface/DTO so that `FlowReynoldsDerivedStateSession` can consume pulse data through Runtime without importing Devices directly.
3. Add a comment in the ExperimentLogic csproj noting the deviation and its rationale.

---

### Finding CC-SL001-002

- `ID`: `CC-SL001-002`
- `Severity`: `P2`
- `Area`: `L2/L3 boundary / interface generality`
- `File`: `platform/src/ExperimentalControlPlatform.Runtime/IDerivedStateSnapshot.cs`

#### Evidence

The `IDerivedStateSnapshot` interface defined in Runtime (L2) contains:

```csharp
double? FilteredFlowRateLitersPerMinute { get; }
double? ReynoldsNumber { get; }
double? MeanTemperatureC { get; }
bool UsesFallbackTemperature { get; }
bool PulseTelemetryIsStale { get; }
bool TemperatureIsStale { get; }
```

These property names are specific to the turbulence/flow-Reynolds experiment. A second experiment type (e.g., acoustic, optical) would not have Reynolds numbers or flow rates, yet would need to implement this interface to participate in the monitor infrastructure.

#### Expected

A Runtime-level interface should use general-purpose property names or a more extensible contract (e.g., named key-value pairs, or a minimal interface with only `ObservedAtUtc` and staleness, with experiment-specific values accessed through a typed downcast or dictionary).

#### Actual

The interface hard-codes flow/Reynolds terminology at the L2 layer, coupling Runtime's monitoring vocabulary to one specific experiment.

#### Why It Matters

The CLAUDE.md review checklist says: "Turbulence as reference case -- architecture decisions must be justified by generality, not turbulence-specific convenience." This interface is currently shaped by the turbulence experiment. When a second experiment type arrives, the interface will need to be generalized, which will be a breaking change to all consumers (ExperimentMonitorSession, App-layer recording).

This is explicitly P2, not P1, because: (a) there is currently only one experiment, (b) the interface successfully decouples Runtime from ExperimentLogic at the reference level, and (c) generalizing the interface now without a second experiment would be speculative over-engineering.

#### Recommended Fix

1. Accept as a known limitation for SL-001.
2. When the second experiment type is introduced (SL-002 or later), generalize `IDerivedStateSnapshot` to use experiment-agnostic properties (e.g., `IReadOnlyDictionary<string, double?> DerivedValues`, or split into `IDerivedStateSnapshot` with only `ObservedAtUtc` + staleness flags, and a separate typed accessor pattern).
3. `ExperimentMonitorSession.BuildDerivedStateSummary` and `RecomputeSnapshot` already only access the interface -- so when the interface generalizes, only those call sites need updating.

---

## What Was Done Well

1. **Clean boundary enforcement.** The core architectural goal is fully achieved. Runtime has zero compile-time knowledge of ExperimentLogic. The grep evidence is conclusive.

2. **Interface extraction is minimal and correct.** `IDerivedStateSession` exposes exactly two things: an event and a current snapshot. This is the minimum contract needed for `ExperimentMonitorSession` to consume derived state. The explicit interface implementation in `FlowReynoldsDerivedStateSession` (lines 70-76) keeps the concrete API rich while the interface stays narrow.

3. **Test refactoring demonstrates the boundary.** The `StubDerivedStateSession` and `StubDerivedStateSnapshot` in `ExperimentMonitorSessionTests.cs` prove that Runtime.Tests can exercise derived-state monitoring without any knowledge of FlowReynolds. This is the strongest evidence that the boundary works.

4. **Integration tests are well-structured.** The 5 tests in `FlowReynoldsIntegrationTests` cover: assignability verification, interface-through-concrete field equality, event firing, and snapshot accessibility through the interface. The `Interface_Snapshot_Fields_Match_Concrete_Snapshot` test is particularly valuable as a regression guard.

5. **No functional regressions.** All 182 tests pass. The relocated code is functionally identical -- only the namespace and project changed.

6. **App-layer updates are minimal and correct.** `IRunRecorder`, `RunRecorder`, `RunArtifactWriter`, and `MainViewModel` added `using ExperimentalControlPlatform.ExperimentLogic;` and a project reference. No behavioral changes.

---

## Verdict

**Pass with non-blocking findings.**

- CC-SL001-001 (P2): Devices dependency deviation -- accept and track for future cleanup.
- CC-SL001-002 (P2): Interface generality -- accept as a known limitation until a second experiment type motivates generalization.

All six success criteria from the slice plan are met. The implementation is ready to land.
