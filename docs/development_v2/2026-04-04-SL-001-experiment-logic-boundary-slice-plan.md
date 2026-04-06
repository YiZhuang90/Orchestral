# SL-001: Experiment-Logic Code Boundary — Slice Plan

## Goal

Give experiment logic an explicit code-level home by creating a new project, relocating the first experiment-logic unit (FlowReynoldsDerivedStateSession), and proving the L2/L3 boundary works in code.

## In Scope

1. **New project**: Create `ExperimentalControlPlatform.ExperimentLogic`
   - New .csproj in `platform/src/ExperimentalControlPlatform.ExperimentLogic/`
   - References: `ExperimentalControlPlatform.Core`, `ExperimentalControlPlatform.Runtime`
   - Does NOT reference `ExperimentalControlPlatform.Devices` directly (experiment logic consumes runtime, not raw device APIs)

2. **Relocate FlowReynoldsDerivedStateSession**:
   - Move `FlowReynoldsDerivedStateSession.cs` from Runtime to ExperimentLogic
   - Move `FlowReynoldsDerivedStateSnapshot.cs` from Runtime to ExperimentLogic
   - Move `FlowReynoldsDerivedStateSample.cs` from Runtime to ExperimentLogic
   - Keep the runtime-facing interfaces stable (no breaking changes to how Runtime or App consume the session)
   - Update all references in Runtime, App, and tests

3. **Update solution**: Add the new project to `ExperimentalControlPlatform.sln`

4. **Update App references**: `ExperimentalControlPlatform.App` must reference the new project for DI wiring

5. **Integration proof test**:
   - Write a test that verifies experiment-logic code (FlowReynoldsDerivedStateSession) consumes runtime session outputs correctly
   - Verify derived state reaches controller and monitor
   - Verify run artifacts still capture the result

6. **New test project**: Create `ExperimentalControlPlatform.ExperimentLogic.Tests`

## Out Of Scope

- Relocating ExperimentMonitorSession (it's runtime infrastructure, not experiment logic)
- Relocating ControllerUnitSession (it's runtime infrastructure)
- Adding new experiment-logic units (camera-pair, image-to-signal, etc.)
- Canvas code
- Broader refactoring of Runtime

## Governing Docs

- Required foundation:
  - [ROADMAP_V2.md](./ROADMAP_V2.md) — Section 4, EF-03
  - [EXPERIMENT_LOGIC_LAYER.md](../architecture/EXPERIMENT_LOGIC_LAYER.md) — Defines what belongs here
  - [DEVICE_RUNTIME_IO_ARCHITECTURE.md](../architecture/DEVICE_RUNTIME_IO_ARCHITECTURE.md) — The "experiment logic should relate to runtime" section
- Conflict winner:
  - EXPERIMENT_LOGIC_LAYER.md — if Runtime docs still classify FlowReynolds as a runtime unit

## Current File Locations

Files to relocate from `platform/src/ExperimentalControlPlatform.Runtime/`:
- `FlowReynoldsDerivedStateSession.cs`
- `FlowReynoldsDerivedStateSnapshot.cs`
- `FlowReynoldsDerivedStateSample.cs`

Files that reference FlowReynolds (must be updated):
- `ExperimentMonitorSession.cs` (in Runtime — consumes derived state)
- `RunRecorder.cs` / `RunArtifactWriter.cs` (in App — records derived state)
- `MainViewModel.cs` (in App — DI wiring)
- Test files in `ExperimentalControlPlatform.Runtime.Tests`

## Dependency Flow After Change

```
App → ExperimentLogic → Runtime → Core ← Devices
App → Runtime → Core
App → Devices
```

The key rule: ExperimentLogic references Runtime (to consume sessions and streams), but Runtime does NOT reference ExperimentLogic. The connection flows downward only.

If Runtime currently has direct references to FlowReynolds types in its own code (e.g., ExperimentMonitorSession consuming FlowReynolds snapshots), those may need to be abstracted through an interface that Runtime defines and ExperimentLogic implements.

## Success Criteria

1. `ExperimentalControlPlatform.ExperimentLogic` project exists and compiles
2. FlowReynoldsDerivedState* classes live in the new project
3. `dotnet build platform/ExperimentalControlPlatform.sln` passes
4. `dotnet test platform/ExperimentalControlPlatform.sln` passes
5. At least one integration test proves the L2→L3 consumption path works
6. Runtime does NOT reference ExperimentLogic (dependency flows downward only)

## Estimated Complexity

Medium. The relocation is mechanical. The main design challenge is ensuring Runtime can still consume FlowReynolds-derived data without a reverse dependency — this may require extracting an interface.

## Dependencies

- LD-001 landed (provides FlowReynoldsDerivedStateSession in the shared codebase) ✓

## Superpowers Skill Chain

`$using-git-worktrees` → `$writing-plans` (for task breakdown) → `$subagent-driven-development` → `$requesting-code-review` → `$finishing-a-development-branch`
