# Experiment Definition And Role Binding Plan

## Purpose

Implement the next experiment-plane slice above the runtime coordinator:

- a typed experiment package artifact,
- explicit role-to-device bindings with validation,
- and a coordinator start path that can carry a real experiment package instead of only a bare run id.

This slice is intentionally narrow. It should not implement buffering, recorder behavior, or the full turbulence processing pipeline yet.

## 1. Clarify The Slice

### Goal

Add the first concrete, validated experiment package model so Orchestral can represent:

- what experiment is being run,
- which abstract roles the experiment requires,
- which concrete devices satisfy those roles for a run,
- and whether those bindings are valid before runtime start.

### In Scope

- extend the `ExperimentalControlPlatform.Core.Artifacts` layer for typed role bindings
- add a resolved experiment package artifact that combines:
  - experiment definition,
  - concrete device definitions,
  - role bindings,
  - active parameter values
- validate:
  - missing required roles,
  - unknown role ids,
  - unknown device ids,
  - duplicate role bindings,
  - capability mismatch,
  - protocol mismatch
- add runtime/coordinator support for starting with an experiment package
- update runtime run context to carry experiment identity
- add unit tests for artifact validation and coordinator start behavior

### Out Of Scope

- cross-session validation against live session registry
- high-rate stream buffering
- run recorder / artifact writer
- experiment-specific controller sessions
- camera-pair processing logic
- experiment-definition linting beyond package-local validation

### Success Criteria

- Core has an explicit role-binding model instead of only manifest dictionaries
- a resolved experiment package can validate itself before runtime start
- runtime coordinator can start a run against a concrete experiment package
- runtime run context exposes experiment identity in addition to run state
- tests cover both artifact validation and coordinator integration

### Landing Target

- branch `codex/runtime-io-microphone`

## 1.1 Required Foundation Docs

### Required Foundation

- [SYSTEM_LANGUAGE_SPEC.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/architecture/SYSTEM_LANGUAGE_SPEC.md)
- [DEVICE_RUNTIME_IO_ARCHITECTURE.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/architecture/DEVICE_RUNTIME_IO_ARCHITECTURE.md)
- [MODULE_DEVELOPMENT_WORKFLOW.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/development/MODULE_DEVELOPMENT_WORKFLOW.md)
- [2026-03-28-functional-unit-roadmap.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/development/2026-03-28-functional-unit-roadmap.md)

### Supporting Reference

- [TURBULENCE_EXPERIMENT_FLOW.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/legacy-knowledge/TURBULENCE_EXPERIMENT_FLOW.md)
- [DEVICE_INVENTORY.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/legacy-knowledge/DEVICE_INVENTORY.md)
- [V1_ARCHITECTURE_BLUEPRINT.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/architecture/V1_ARCHITECTURE_BLUEPRINT.md)

### Conflict Winner

- [SYSTEM_LANGUAGE_SPEC.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/architecture/SYSTEM_LANGUAGE_SPEC.md)

## 2. Current Reality

- `ExperimentalControlPlatform.Core.Artifacts` already contains:
  - `ExperimentDefinition`
  - `DeviceRoleDefinition`
  - `DeviceDefinition`
  - `RunManifestDefinition`
- but there is no explicit typed role-binding artifact yet
- runtime does not currently reference experiment packages at start time
- `RuntimeCoordinator.Start()` only creates a new `RunId` and run-state snapshot
- `RunManifestDefinition` already has role/protocol binding dictionaries, so the missing piece is the validated pre-run package that feeds runtime and later manifest generation

## 3. Ordered Tasks

1. Add failing core tests for:
   - explicit role binding
   - resolved experiment package validation
   - capability and protocol mismatch cases
2. Implement the artifact layer:
   - `RoleBindingDefinition`
   - `ResolvedExperimentDefinition`
   - validation result / issue types if needed
3. Add coordinator tests for starting with an experiment package and preserving experiment identity in the run snapshot
4. Update runtime contracts and implementation for experiment-aware start
5. Run full build/test verification
6. Run external Claude review
7. Address findings, reverify, and sync status docs

## 4. Owned Files

Primary:

- `platform/src/ExperimentalControlPlatform.Core/Artifacts/ExperimentDefinition.cs`
- `platform/src/ExperimentalControlPlatform.Core/Artifacts/DeviceRoleDefinition.cs`
- `platform/src/ExperimentalControlPlatform.Core/Artifacts/RunManifestDefinition.cs`
- `platform/src/ExperimentalControlPlatform.Core/Artifacts/RoleBindingDefinition.cs`
- `platform/src/ExperimentalControlPlatform.Core/Artifacts/ResolvedExperimentDefinition.cs`
- `platform/tests/ExperimentalControlPlatform.Core.Tests/Artifacts/ExperimentDefinitionTests.cs`
- `platform/tests/ExperimentalControlPlatform.Core.Tests/Artifacts/RoleBindingDefinitionTests.cs`
- `platform/tests/ExperimentalControlPlatform.Core.Tests/Artifacts/ResolvedExperimentDefinitionTests.cs`
- `platform/src/ExperimentalControlPlatform.Runtime/IRuntimeCoordinator.cs`
- `platform/src/ExperimentalControlPlatform.Runtime/RuntimeCoordinator.cs`
- `platform/src/ExperimentalControlPlatform.Runtime/RuntimeRunContext.cs`
- `platform/tests/ExperimentalControlPlatform.Runtime.Tests/RuntimeCoordinatorTests.cs`

Likely secondary:

- `platform/src/ExperimentalControlPlatform.App/MainViewModel.cs`
- `platform/src/ExperimentalControlPlatform.App/RuntimeStatusViewModel.cs`

## 5. Review Gate

External review is required because this slice is:

- shared foundation,
- architecture-affecting,
- and the first explicit bridge between experiment artifacts and runtime execution.

Preferred reviewer channel:

- Claude Code CLI with `claude-opus-4-6`

## 6. Verification Gate

Minimum:

- `dotnet build .\\platform\\ExperimentalControlPlatform.sln -nodeReuse:false`
- `dotnet test .\\platform\\ExperimentalControlPlatform.sln --no-build -nodeReuse:false`

If runtime start behavior changes materially:

- one local app-shell smoke launch
