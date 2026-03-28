# Runtime Coordinator And Stop Authority Plan

## Purpose

Implement the next experiment-plane slice above the existing device sessions:

- a real runtime coordinator that owns run-level state,
- a truthful stop path that reaches active sessions through the registry,
- and explicit shutdown-state semantics instead of the current instant `Running -> Idle` jump.

This slice is intentionally narrow. It should not introduce experiment definition, buffering, or recorder behavior yet.

## 1. Clarify The Slice

### Goal

Upgrade the runtime coordinator from a placeholder run-state holder into a host-level orchestrator that:

- starts a run,
- tracks `Idle`, `Running`, and `Stopping`,
- requests a stop with an explicit `StopReason`,
- drives `IDeviceSessionRegistry.StopAllAsync(...)`,
- and publishes truthful run-level snapshots before and after shutdown completes.

### In Scope

- `IRuntimeCoordinator` contract update
- `RuntimeCoordinator` implementation update
- `RunState` and `RuntimeRunContext` updates if needed
- runtime tests for start/stop transitions and registry-backed stop
- app-shell updates required by the new stop contract
- runtime-status text updates required by the new state model

### Out Of Scope

- experiment definition and role binding
- cross-session validation
- stream buffering policy
- recorder or artifact writer
- new device sessions
- system-level alarm surface beyond stop state exposure

### Success Criteria

- coordinator stop reaches all active sessions through the registry
- run state exposes a real `Stopping` phase
- final run snapshot preserves stop reason and stop time
- duplicate start while active is rejected
- duplicate stop while idle or already stopping is rejected
- app shell still builds and reports runtime state truthfully

### Dependencies

- existing `IDeviceSessionRegistry.StopAllAsync(...)`
- existing per-session `StopAsync(...)`
- current app shell runtime status surface

### Verification Target

- runtime coordinator unit tests
- app/runtime build
- full solution test pass

### Landing Target

- branch `codex/runtime-io-microphone`

## 1.1 Required Foundation Docs

### Required Foundation

- [DEVICE_RUNTIME_IO_ARCHITECTURE.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/architecture/DEVICE_RUNTIME_IO_ARCHITECTURE.md)
- [MODULE_DEVELOPMENT_WORKFLOW.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/development/MODULE_DEVELOPMENT_WORKFLOW.md)
- [2026-03-28-functional-unit-roadmap.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/development/2026-03-28-functional-unit-roadmap.md)

### Supporting Reference

- [V1_ARCHITECTURE_BLUEPRINT.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/architecture/V1_ARCHITECTURE_BLUEPRINT.md)
- [2026-03-26-runtime-io-architecture-review.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/collaboration/reviews/2026-03-26-runtime-io-architecture-review.md)

### Conflict Winner

- [DEVICE_RUNTIME_IO_ARCHITECTURE.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/architecture/DEVICE_RUNTIME_IO_ARCHITECTURE.md)

## 2. Current Reality

- `RuntimeCoordinator` currently stores only a local run snapshot.
- `RequestStop(...)` is synchronous and does not reach the registry.
- `DeviceSessionRegistry.StopAllAsync(...)` already exists and clears active sessions after stopping them.
- `RunState` currently exposes only `Idle` and `Running`.
- app shutdown calls the registry directly, so the coordinator is not yet the truthful stop hub.

## 3. Ordered Tasks

1. Add tests that define the target behavior:
   - start transitions to `Running`
   - stop request transitions through `Stopping`
   - registry stop is invoked
   - final snapshot captures stop reason and stop time
   - restart is allowed only after stop completes
2. Upgrade runtime contracts:
   - add `RunState.Stopping`
   - change coordinator stop API to async
3. Implement coordinator stop orchestration over `IDeviceSessionRegistry`
4. Update app-shell callers and runtime-status text for the new contract
5. Run full verification
6. Run external Claude review
7. Address findings and reverify

## 4. Owned Files

Primary:

- `platform/src/ExperimentalControlPlatform.Runtime/IRuntimeCoordinator.cs`
- `platform/src/ExperimentalControlPlatform.Runtime/RuntimeCoordinator.cs`
- `platform/src/ExperimentalControlPlatform.Runtime/RunState.cs`
- `platform/src/ExperimentalControlPlatform.Runtime/RuntimeRunContext.cs`
- `platform/tests/ExperimentalControlPlatform.Runtime.Tests/RuntimeCoordinatorTests.cs`

Likely secondary:

- `platform/src/ExperimentalControlPlatform.App/MainViewModel.cs`
- `platform/src/ExperimentalControlPlatform.App/RuntimeStatusViewModel.cs`
- `platform/src/ExperimentalControlPlatform.App/App.xaml.cs`

## 5. Review Gate

External review is required because this slice is:

- runtime/lifecycle related,
- cross-cutting,
- shared foundation,
- and directly tied to stop semantics.

Preferred reviewer channel:

- Claude Code CLI with `claude-opus-4-6`

Expected artifacts:

- review
- response
- re-review if needed

## 6. Verification Gate

Minimum:

- `dotnet build .\\platform\\ExperimentalControlPlatform.sln -nodeReuse:false`
- `dotnet test .\\platform\\ExperimentalControlPlatform.sln --no-build -nodeReuse:false`

If app-shell behavior changes materially:

- one local runtime shell smoke launch
