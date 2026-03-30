# Controller Unit Session Review Response

## Scope

- review: [2026-03-30-controller-unit-session-review-claude-opus-4-6.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/collaboration/reviews/2026-03-30-controller-unit-session-review-claude-opus-4-6.md)
- slice: first-generation experiment-plane controller unit semantics and runtime implementation

## Finding Responses

### 1. `ControlOutputValue` equals raw error in closed-loop mode

Status:

- `accepted`

Response:

- kept `ControlOutputValue` for downstream numeric consumption, but added `ControlOutputInterpretation` to both [ControllerDecision.cs](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/platform/src/ExperimentalControlPlatform.Runtime/ControllerDecision.cs) and [ControllerUnitState.cs](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/platform/src/ExperimentalControlPlatform.Runtime/ControllerUnitState.cs)
- `ControllerUnitSession` now publishes:
  - `setpoint_passthrough` for open-loop targets
  - `proportional_error` for closed-loop targets
- runtime tests now assert those values explicitly in [ControllerUnitSessionTests.cs](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/platform/tests/ExperimentalControlPlatform.Runtime.Tests/ControllerUnitSessionTests.cs)

### 2. `MeasuredValue` fallback to previous state on null input

Status:

- `accepted`

Response:

- added `MeasuredValueObservedAtUtc` and `MeasuredValueIsStale` to both controller decision/state artifacts
- `ControllerUnitSession` now preserves the original observation timestamp when a measurement is carried forward and marks the carried value as stale instead of pretending it is fresh
- status text now reflects this with `Target +/- Error (stale measured value)` when appropriate
- added explicit stale-measurement coverage in [ControllerUnitSessionTests.cs](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/platform/tests/ExperimentalControlPlatform.Runtime.Tests/ControllerUnitSessionTests.cs)

### 3. `MeasuredSourceId` refers to streams or monitors

Status:

- `accepted`

Response:

- narrowed resolved-experiment validation to stream IDs only in [ResolvedExperimentDefinition.cs](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/platform/src/ExperimentalControlPlatform.Core/Artifacts/ResolvedExperimentDefinition.cs)
- updated the language/architecture docs so derived values must be surfaced as named streams before a control target can reference them:
  - [SYSTEM_LANGUAGE_SPEC.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/architecture/SYSTEM_LANGUAGE_SPEC.md)
  - [DEVICE_RUNTIME_IO_ARCHITECTURE.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/architecture/DEVICE_RUNTIME_IO_ARCHITECTURE.md)
- added a validation test proving a monitor ID is rejected as a measured source in [ResolvedExperimentControlTargetTests.cs](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/platform/tests/ExperimentalControlPlatform.Core.Tests/Artifacts/ResolvedExperimentControlTargetTests.cs)

### 4. Schedule format is only parsed at runtime construction

Status:

- `accepted`

Response:

- extracted shared parsing into [ControlTargetScheduleParser.cs](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/platform/src/ExperimentalControlPlatform.Core/Artifacts/ControlTargetScheduleParser.cs)
- `ResolvedExperimentDefinition.ValidateControlTargets(...)` now validates scheduled-parameter format at resolve time and emits `invalid_control_target_schedule_format` on malformed input
- added validation coverage in [ResolvedExperimentControlTargetTests.cs](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/platform/tests/ExperimentalControlPlatform.Core.Tests/Artifacts/ResolvedExperimentControlTargetTests.cs)

### 5. No common session interface for `ControllerUnitSession`

Status:

- `accepted with defer`

Response:

- intentionally left `ControllerUnitSession` outside `IDeviceSession` in this slice
- current reasoning:
  - it is an experiment-plane unit above device sessions rather than a device-session peer
  - introducing a shared experiment-plane interface now would be premature without the monitor surface and multi-unit coordinator semantics
- deferred follow-up:
  - resolve whether experiment-plane units need `IExperimentPlaneUnit` or separate coordinator ownership in the upcoming monitor/alarm slice

### 6. `StopAsync` uses internal `UtcNow`

Status:

- `accepted`

Response:

- `ControllerUnitSession.StopAsync(...)` now accepts an optional caller-supplied stop timestamp and only falls back to `UtcNow` when none is provided
- added explicit stop-timestamp coverage in [ControllerUnitSessionTests.cs](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/platform/tests/ExperimentalControlPlatform.Runtime.Tests/ControllerUnitSessionTests.cs)

### 7. Thread-safety gap around async apply callback

Status:

- `accepted`

Response:

- the implementation now uses a `SemaphoreSlim` gate across the full sample -> publish -> apply cycle in [ControllerUnitSession.cs](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/platform/src/ExperimentalControlPlatform.Runtime/ControllerUnitSession.cs)
- this keeps overlapping `SampleAsync(...)` calls from interleaving state publication and command-apply callbacks
- added serialized-overlap coverage in [ControllerUnitSessionTests.cs](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/platform/tests/ExperimentalControlPlatform.Runtime.Tests/ControllerUnitSessionTests.cs)

## Verification

- `dotnet test .\\platform\\tests\\ExperimentalControlPlatform.Core.Tests\\ExperimentalControlPlatform.Core.Tests.csproj -nodeReuse:false --filter ControlTarget`
- `dotnet test .\\platform\\tests\\ExperimentalControlPlatform.Runtime.Tests\\ExperimentalControlPlatform.Runtime.Tests.csproj -nodeReuse:false --filter ControllerUnitSession`
- `dotnet build .\\platform\\ExperimentalControlPlatform.sln -nodeReuse:false -v:minimal`
- `dotnet test .\\platform\\ExperimentalControlPlatform.sln --no-build -nodeReuse:false -v:minimal`

Result:

- focused core tests passed `10/10`
- focused runtime tests passed `7/7`
- full build passed with `0 warnings, 0 errors`
- full test suite passed:
  - Core `42/42`
  - Devices `14/14`
  - App `14/14`
  - Runtime `67/67`
