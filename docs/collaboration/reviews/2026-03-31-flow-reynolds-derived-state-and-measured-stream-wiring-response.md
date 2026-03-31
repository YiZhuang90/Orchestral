# Review Response: Flow/Reynolds Derived-State And Measured-Stream Wiring

## Review Basis

- Review artifact: `2026-03-31-flow-reynolds-derived-state-and-measured-stream-wiring-review-claude-opus-4-6.md`
- Reviewer verdict: ready to ship, no blocking issues

## Response By Finding

### 1. `Slugify` duplicated 3 times

Status: deferred

Reason:
- valid cleanup point, but not introduced by this slice
- does not change runtime truth, lifecycle, or artifact correctness
- keeping the slice focused is better than mixing in a cross-cutting string-utility refactor

### 2. Timing-dependent test in `MainViewModelTests.cs`

Status: addressed

Change:
- replaced the fixed `Task.Delay(1200)` wait with `FakeControlCenterService.WaitForReadCountAsync(2, ...)`
- the integration test now waits for the actual pulse-read condition that produces derived-state output instead of sleeping for an assumed interval

### 3. Unused `_pipeLengthMeters` / `_pipeRoughnessMeters`

Status: addressed

Change:
- added an inline comment in `FlowReynoldsDerivedStateSession.ComputeHydraulicState(...)`
- the comment now states that these validated parameters are intentionally retained for the next pressure-drop / friction-factor slice while V1 currently derives only bulk velocity and Reynolds number

## Reviewer Suggestions

### Add physics model references

Status: deferred

Reason:
- useful improvement, but documentation/comment enrichment rather than correctness work
- can be handled in a later science-model documentation pass without changing behavior

### `TemperatureDeltaStreamId` declared but not yet projected as a dedicated runtime stream

Status: accepted as current limitation

Reason:
- the current slice records temperature delta inside the derived-state snapshot and sample artifacts
- it is not yet exposed as a standalone runtime stream port because the first measured-state implementation is centered on the combined derived-state session output
- architecture docs were updated to describe this as remaining dedicated-stream projection work rather than pretending it is already complete

### Architecture docs not updated

Status: addressed

Change:
- updated `docs/development/V1_EXECUTION_TRACK.md`
- updated `docs/architecture/DEVICE_RUNTIME_IO_ARCHITECTURE.md`
- updated `docs/architecture/SYSTEM_LANGUAGE_SPEC.md`

### Shared test builders duplicated

Status: deferred

Reason:
- good cleanup candidate, but not necessary to land the measured-state slice safely

## Post-Response Verification

- `dotnet build .\platform\ExperimentalControlPlatform.sln -nodeReuse:false -v:minimal`
- `dotnet test .\platform\ExperimentalControlPlatform.sln -nodeReuse:false -v:minimal`

Both passed after the response changes.
