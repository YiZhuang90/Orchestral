# Controller Unit Session Plan

## Purpose

Implement the next experiment-plane runtime unit above controlled-device sessions:

- first-generation control-target artifacts in the system language,
- one real `ControllerUnitSession` runtime unit for a single control target,
- and validation/tests that prove constant vs scheduled setpoint profiles and open-loop vs closed-loop regulation semantics.

This slice is intentionally bounded.
It should make the controller unit real without also building the monitor panel, multi-target UI, or experiment-specific actuator math.

## 1. Clarify The Slice

### Goal

Add a first-generation controller unit so Orchestral can:

- represent control targets in experiment definitions,
- resolve them into runtime-safe experiment packages,
- evaluate a live target value from constant or scheduled setpoint profile,
- track measured value, target value, and error,
- and emit normalized controller state/decisions above device sessions.

### In Scope

- add control-target artifacts to the core system language
- extend resolved experiment artifacts to carry control targets
- add validation for:
  - known command role
  - known target parameter
  - allowed profile/mode values
- implement a first `ControllerUnitSession`
- support:
  - `Constant` setpoint profile
  - `Scheduled` setpoint profile with relative run-time steps
  - `OpenLoop`
  - `ClosedLoop`
- emit controller snapshot and control-decision outputs
- provide one simple control-output projection based on numeric setpoint/error
- add runtime and core tests

### Out Of Scope

- experiment monitor panel
- live monitor display-rate UI
- multi-target experiment control UI
- hardware-specific actuator adapters beyond a generic output callback
- PID tuning or advanced controller algorithms
- experiment-specific Reynolds-number derivation
- recorder/reporter integration beyond existing experiment artifacts

### Success Criteria

- an experiment definition can declare one or more control targets
- resolved experiment validation rejects invalid target references
- `ControllerUnitSession` computes target value from constant and scheduled profiles
- `ControllerUnitSession` reports measured value, target value, and signed error
- `OpenLoop` and `ClosedLoop` produce observably different control decisions
- tests pass and the slice is externally reviewed

### Landing Target

- branch `codex/runtime-io-microphone`

## 1.1 Required Foundation Docs

### Required Foundation

- [DEVICE_RUNTIME_IO_ARCHITECTURE.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/architecture/DEVICE_RUNTIME_IO_ARCHITECTURE.md)
- [SYSTEM_LANGUAGE_SPEC.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/architecture/SYSTEM_LANGUAGE_SPEC.md)
- [MODULE_DEVELOPMENT_WORKFLOW.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/development/MODULE_DEVELOPMENT_WORKFLOW.md)

### Supporting Reference

- [ExperimentDefinition.cs](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/platform/src/ExperimentalControlPlatform.Core/Artifacts/ExperimentDefinition.cs)
- [ResolvedExperimentDefinition.cs](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/platform/src/ExperimentalControlPlatform.Core/Artifacts/ResolvedExperimentDefinition.cs)
- [RuntimeCoordinator.cs](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/platform/src/ExperimentalControlPlatform.Runtime/RuntimeCoordinator.cs)
- [ControlCenterSession.cs](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/platform/src/ExperimentalControlPlatform.Runtime/ControlCenterSession.cs)

### Conflict Winner

- [SYSTEM_LANGUAGE_SPEC.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/architecture/SYSTEM_LANGUAGE_SPEC.md)

## 2. Current Reality

- device-level controlled sessions already exist, but they only transport commands and expose device state
- experiment definitions do not yet carry control-target artifacts in code
- resolved experiment validation does not yet check control-target references
- there is no runtime-owned controller unit above `ControlCenterSession`
- the monitor design now assumes controller-produced `Target +/- Error` truth, but no such producer exists yet

## 3. Ordered Tasks

1. Add failing core tests for control-target artifacts and resolved-experiment validation
2. Add failing runtime tests for:
   - constant target evaluation
   - scheduled target evaluation
   - open-loop vs closed-loop decision semantics
3. Implement core control-target artifact models
4. Extend experiment and resolved-experiment artifacts
5. Implement `ControllerUnitSession`
6. Sync architecture/execution docs
7. Run full build/test verification
8. Run external review
9. Address findings, reverify, and land

## 4. Owned Files

Primary:

- `platform/src/ExperimentalControlPlatform.Core/Artifacts/ExperimentDefinition.cs`
- `platform/src/ExperimentalControlPlatform.Core/Artifacts/ResolvedExperimentDefinition.cs`
- `platform/src/ExperimentalControlPlatform.Core/Artifacts/ControlTargetDefinition.cs`
- `platform/src/ExperimentalControlPlatform.Core/Artifacts/ControlTargetSchedulePoint.cs`
- `platform/src/ExperimentalControlPlatform.Runtime/ControllerUnitSession.cs`
- `platform/src/ExperimentalControlPlatform.Runtime/ControllerUnitState.cs`
- `platform/src/ExperimentalControlPlatform.Runtime/ControllerDecision.cs`
- `platform/tests/ExperimentalControlPlatform.Core.Tests/Artifacts/ResolvedExperimentDefinitionTests.cs`
- `platform/tests/ExperimentalControlPlatform.Runtime.Tests/ControllerUnitSessionTests.cs`

Likely secondary:

- `docs/architecture/DEVICE_RUNTIME_IO_ARCHITECTURE.md`
- `docs/architecture/SYSTEM_LANGUAGE_SPEC.md`
- `docs/development/V1_EXECUTION_TRACK.md`

## 5. Review Gate

External review is required because this slice is:

- shared experiment-plane foundation,
- runtime and lifecycle related,
- and defines the first experiment-level control semantics above device sessions.

Preferred reviewer channel:

- Claude Code CLI `claude-opus-4-6`

Fallback reviewer channel:

- Gemini CLI `gemini-3-flash-preview`

## 6. Verification Gate

Minimum:

- `dotnet build .\\platform\\ExperimentalControlPlatform.sln -nodeReuse:false`
- `dotnet test .\\platform\\ExperimentalControlPlatform.sln --no-build -nodeReuse:false`

Focused red/green cycle:

- `dotnet test .\\platform\\tests\\ExperimentalControlPlatform.Core.Tests\\ExperimentalControlPlatform.Core.Tests.csproj -nodeReuse:false --filter ControlTarget`
- `dotnet test .\\platform\\tests\\ExperimentalControlPlatform.Runtime.Tests\\ExperimentalControlPlatform.Runtime.Tests.csproj -nodeReuse:false --filter ControllerUnitSession`

## 7. Result

- Completed in branch code with:
  - `ControlTargetDefinition` as the first real experiment-language control-target artifact,
  - `ExperimentDefinition` and `ResolvedExperimentDefinition` extended to carry and validate control targets,
  - `ControllerUnitSession` as the first runtime-owned experiment-plane controller unit,
  - constant and scheduled setpoint-profile handling,
  - open-loop and closed-loop decision semantics,
  - and normalized controller state with live `Target +/- Error` values.

Verification:

- `dotnet test .\\platform\\tests\\ExperimentalControlPlatform.Core.Tests\\ExperimentalControlPlatform.Core.Tests.csproj -nodeReuse:false --filter ControlTarget`
- `dotnet test .\\platform\\tests\\ExperimentalControlPlatform.Runtime.Tests\\ExperimentalControlPlatform.Runtime.Tests.csproj -nodeReuse:false --filter ControllerUnitSession`
- `dotnet build .\\platform\\ExperimentalControlPlatform.sln -nodeReuse:false -v:minimal`
- `dotnet test .\\platform\\ExperimentalControlPlatform.sln --no-build -nodeReuse:false -v:minimal`

Review artifacts:

- [2026-03-30-controller-unit-session-review-claude-opus-4-6.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/collaboration/reviews/2026-03-30-controller-unit-session-review-claude-opus-4-6.md)
- [2026-03-30-controller-unit-session-response.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/collaboration/reviews/2026-03-30-controller-unit-session-response.md)
- [2026-03-30-controller-unit-session-rereview-claude-opus-4-6.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/collaboration/reviews/2026-03-30-controller-unit-session-rereview-claude-opus-4-6.md)

Review result:

- original Opus review found:
  - 3 semantic mismatches
  - 4 non-blocking improvements
- rereview verdict:
  - pass
  - all 7 findings resolved

Verification result:

- focused core tests passed `10/10`
- focused runtime tests passed `7/7`
- full build passed with `0 warnings, 0 errors`
- full test suite passed:
  - Core `42/42`
  - Devices `14/14`
  - App `14/14`
  - Runtime `67/67`
