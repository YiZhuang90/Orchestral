## Code Review Results: Flow/Reynolds Derived-State

### Verdict: Ready to ship (no blocking issues)

The reviewer found the implementation **solid and well-aligned with the plan**. All 9 execution steps are addressed. The Reynolds number derivation physics (Tanaka density model, Vogel viscosity equation) are verified correct.

### Important Issues (non-blocking, worth addressing)

| # | Issue | Location |
|---|-------|----------|
| 1 | **`Slugify` duplicated 3 times** ！ identical `private static` method in 3 files. Extract to shared utility. | `AdHocRunDefinitionFactory.cs`, `RunArtifactWriter.cs`, `MainViewModel.cs` |
| 2 | **Timing-dependent test** ！ `Task.Delay(1200)` could be flaky under CI load. Consider deterministic sync. | `MainViewModelTests.cs:139` |
| 3 | **Unused params need comments** ！ `_pipeLengthMeters` / `_pipeRoughnessMeters` are validated then discarded with `_ =`. Intent is unclear without a comment. | `FlowReynoldsDerivedStateSession.cs:410-411` |

### Suggestions

- **Add physics model references** ！ one-line comments citing Tanaka (2001) for density and Vogel for viscosity would help future maintainers.
- **`TemperatureDeltaStreamId` declared but unwired** ！ the stream is in the ad hoc package but no session publishes to it. Either remove for V1 or mark as reserved.
- **Architecture docs not updated** ！ plan step 8 calls for updating `DEVICE_RUNTIME_IO_ARCHITECTURE.md` etc. No architecture doc changes in the diff.
- **Consider extracting shared test builders** ！ `CreateResolvedExperimentDefinition` is duplicated across test files with slight variations.

Would you like me to address any of these findings?
