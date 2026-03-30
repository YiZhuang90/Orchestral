# Run Context And Metadata Plan

## Purpose

Implement the next universal experiment-plane slice above experiment definition and coordinator stop control:

- a first-class run-context artifact,
- coordinator support for starting with structured run metadata,
- and runtime status surfaces that can carry run identity and operator intent without pretending the recorder already exists.

This slice is intentionally narrow. It should not implement artifact writing, run recording, monitor dashboards, or experiment-specific processing.

## 1. Clarify The Slice

### Goal

Add the first real run-context unit so Orchestral can represent:

- which concrete run is active,
- which experiment package it is tied to,
- what operator note and metadata were attached to the run,
- which applied-settings artifacts or placeholders belong to the run,
- and which decision-log or artifact references should travel with the run until recorder behavior exists.

### In Scope

- add a core artifact for run context and metadata
- validate:
  - required run-context identity,
  - optional operator note normalization,
  - metadata key/value copying,
  - applied-settings reference copying,
  - decision-log reference copying,
  - artifact-reference copying
- update runtime coordinator contracts and implementation to start with a run-context artifact
- update `RuntimeRunContext` so run-level snapshots preserve structured run context through:
  - start,
  - stopping,
  - and final idle-after-stop snapshots
- add minimal status-surface support for showing run-context identity and note
- add tests for artifact validation and runtime/coordinator behavior

### Out Of Scope

- run recorder or artifact writer
- persistent storage
- experiment-definition linting
- cross-session validation
- monitor/alarm dashboard work
- experiment-specific controller sessions
- artifact export or file materialization

### Success Criteria

- Core has an explicit `RunContextDefinition` artifact instead of only bare runtime snapshot fields
- runtime coordinator can start with a structured run context in addition to the existing bare or experiment-only start paths
- `RuntimeRunContext` preserves run metadata references across start and stop transitions
- app/runtime status can surface run identity or operator note without inventing recorder behavior
- tests cover both artifact-level validation and runtime integration

### Landing Target

- branch `codex/runtime-io-microphone`

## 1.1 Required Foundation Docs

### Required Foundation

- [DEVICE_RUNTIME_IO_ARCHITECTURE.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/architecture/DEVICE_RUNTIME_IO_ARCHITECTURE.md)
- [TIMING_AND_SYNCHRONIZATION_STRATEGY.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/architecture/TIMING_AND_SYNCHRONIZATION_STRATEGY.md)
- [2026-03-28-functional-unit-roadmap.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/development/2026-03-28-functional-unit-roadmap.md)
- [MODULE_DEVELOPMENT_WORKFLOW.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/development/MODULE_DEVELOPMENT_WORKFLOW.md)

### Supporting Reference

- [SYSTEM_LANGUAGE_SPEC.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/architecture/SYSTEM_LANGUAGE_SPEC.md)
- [RunManifestDefinition.cs](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/platform/src/ExperimentalControlPlatform.Core/Artifacts/RunManifestDefinition.cs)
- [RuntimeRunContext.cs](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/platform/src/ExperimentalControlPlatform.Runtime/RuntimeRunContext.cs)
- [RuntimeCoordinator.cs](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/platform/src/ExperimentalControlPlatform.Runtime/RuntimeCoordinator.cs)

### Conflict Winner

- [DEVICE_RUNTIME_IO_ARCHITECTURE.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/architecture/DEVICE_RUNTIME_IO_ARCHITECTURE.md)

## 2. Current Reality

- `RuntimeRunContext` currently stores only:
  - `RunId`
  - `RunState`
  - start/stop timestamps
  - stop reason
  - optional resolved experiment package
- `RuntimeCoordinator` can start:
  - bare
  - or with a `ResolvedExperimentDefinition`
- `RunManifestDefinition` already models stopped-run output shape, but no live run-context artifact currently bridges operator intent and run state before recorder behavior exists
- `RuntimeStatusViewModel` only knows how to summarize state and optional experiment name
- no current artifact owns:
  - operator note
  - metadata key/value pairs
  - applied-settings artifact references
  - decision-log references
  - reserved artifact references

## 3. Ordered Tasks

1. Add failing core/runtime/app tests for:
   - run-context artifact capture and normalization
   - coordinator start with run context
   - run-context preservation through stop transitions
   - runtime status summary including run-context identity/note
2. Implement the core artifact:
   - `RunContextDefinition`
3. Update runtime contracts and implementation:
   - `RuntimeRunContext`
   - `IRuntimeCoordinator`
   - `RuntimeCoordinator`
4. Update app status surfacing:
   - `RuntimeStatusViewModel`
5. Run full build/test verification
6. Run external review
7. Address findings, reverify, and sync docs

## 4. Owned Files

Primary:

- `platform/src/ExperimentalControlPlatform.Core/Artifacts/RunContextDefinition.cs`
- `platform/tests/ExperimentalControlPlatform.Core.Tests/Artifacts/RunContextDefinitionTests.cs`
- `platform/src/ExperimentalControlPlatform.Runtime/RuntimeRunContext.cs`
- `platform/src/ExperimentalControlPlatform.Runtime/IRuntimeCoordinator.cs`
- `platform/src/ExperimentalControlPlatform.Runtime/RuntimeCoordinator.cs`
- `platform/tests/ExperimentalControlPlatform.Runtime.Tests/RuntimeCoordinatorTests.cs`
- `platform/src/ExperimentalControlPlatform.App/RuntimeStatusViewModel.cs`
- `platform/tests/ExperimentalControlPlatform.App.Tests/RuntimeStatusViewModelTests.cs`

Likely secondary:

- `docs/architecture/DEVICE_RUNTIME_IO_ARCHITECTURE.md`
- `docs/development/V1_EXECUTION_TRACK.md`

## 5. Review Gate

External review is required because this slice is:

- shared foundation,
- runtime/lifecycle related,
- and a new experiment-plane boundary that later recorder and monitor slices will depend on.

Preferred reviewer channel:

- Claude Code CLI with `claude-opus-4-6`

Fallback reviewer channel:

- Gemini CLI `gemini-3-flash-preview`

## 6. Verification Gate

Minimum:

- `dotnet build .\\platform\\ExperimentalControlPlatform.sln -nodeReuse:false`
- `dotnet test .\\platform\\ExperimentalControlPlatform.sln --no-build -nodeReuse:false`

If app summary or runtime shell wiring changes materially:

- one local app-shell smoke launch

## 7. Result

Completed in branch code with:

- `RunContextDefinition` as the first-class run-context artifact,
- coordinator support for `Start(RunContextDefinition)`,
- `RuntimeRunContext` preservation of run context through stop transitions,
- and status-surface support for run display name and operator note.

Verification:

- `dotnet build .\\platform\\ExperimentalControlPlatform.sln -nodeReuse:false`
- `dotnet test .\\platform\\ExperimentalControlPlatform.sln --no-build -nodeReuse:false`

Review artifacts:

- [2026-03-30-run-context-and-metadata-review-gemini-3-flash-preview.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/collaboration/reviews/2026-03-30-run-context-and-metadata-review-gemini-3-flash-preview.md)
- [2026-03-30-run-context-and-metadata-response.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/collaboration/reviews/2026-03-30-run-context-and-metadata-response.md)
- [2026-03-30-run-context-and-metadata-rereview-gemini-3-flash-preview.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/collaboration/reviews/2026-03-30-run-context-and-metadata-rereview-gemini-3-flash-preview.md)
