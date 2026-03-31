# Response: Experiment Canvas And Virtual Twin Architecture

## Review Context

Review artifact:

- [2026-03-31-experiment-canvas-and-virtual-twin-architecture-review-claude-opus-4-6.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/collaboration/reviews/2026-03-31-experiment-canvas-and-virtual-twin-architecture-review-claude-opus-4-6.md)

## Response To Findings

### B1. Phantom document references

Resolved by review-input correction.

The two referenced docs do exist:

- [EXPERIMENT_CANVAS_ARCHITECTURE.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/architecture/EXPERIMENT_CANVAS_ARCHITECTURE.md)
- [VIRTUAL_TWIN_AND_SIMULATION_STRATEGY.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/architecture/VIRTUAL_TWIN_AND_SIMULATION_STRATEGY.md)

They were untracked during the first review run, so `git diff` omitted them from the prompt. The rereview is run against the staged diff so those files are included explicitly.

### B2. Device vs concrete implementation overlap

Resolved in [SYSTEM_LANGUAGE_SPEC.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/architecture/SYSTEM_LANGUAGE_SPEC.md).

The spec now makes this distinction explicit:

- `Concrete Implementation` = the run-bound realization of an experiment role
- `Device` = the identifiable hardware endpoint or device-shaped twin behind a concrete implementation
- replay and synthetic sources are concrete implementations, but not devices

Role binding language was also updated to bind an experiment role to a concrete implementation rather than directly to a device.

### B3. Experiment role vs device role terminology drift

Resolved in [SYSTEM_LANGUAGE_SPEC.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/architecture/SYSTEM_LANGUAGE_SPEC.md).

`Experiment Role` is now the canonical noun in the touched architecture docs. The graph and canvas-order guidance were updated to use that term consistently.

## Response To Non-Blocking Notes

### N2. Source-mode ownership phrasing

Clarified in:

- [SYSTEM_LANGUAGE_SPEC.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/architecture/SYSTEM_LANGUAGE_SPEC.md)
- [DEVICE_RUNTIME_IO_ARCHITECTURE.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/architecture/DEVICE_RUNTIME_IO_ARCHITECTURE.md)
- [VIRTUAL_TWIN_AND_SIMULATION_STRATEGY.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/architecture/VIRTUAL_TWIN_AND_SIMULATION_STRATEGY.md)

Selection belongs to experiment-building and binding surfaces. Once selected, source mode becomes part of the resolved concrete implementation for the run.

### N3. Experiment-logic canvas diagram direction

Clarified in [EXPERIMENT_LOGIC_LAYER.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/architecture/EXPERIMENT_LOGIC_LAYER.md).

The diagram now shows:

- experiment function decomposing into experiment roles and experiment-logic units,
- roles binding to concrete implementations,
- concrete implementations feeding universal runtime,
- universal runtime feeding experiment-logic units.

### N4. V1 scope fence for virtual twin strategy

Clarified in:

- [VIRTUAL_TWIN_AND_SIMULATION_STRATEGY.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/architecture/VIRTUAL_TWIN_AND_SIMULATION_STRATEGY.md)
- [AI_GUIDED_HARDWARE_INTEGRATION_SPEC.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/architecture/AI_GUIDED_HARDWARE_INTEGRATION_SPEC.md)

The docs now state that:

- virtual closure is a preferred final integration target,
- not a first-pass blocker for V1,
- and the first universal harness remains limited to single-session replay plus scalar synthetic sources.

## Rereview Target

The rereview should check:

1. whether the staged diff now includes the two new architecture docs,
2. whether `Experiment Role`, `Concrete Implementation`, and `Device` are now disambiguated enough,
3. whether the source-mode ownership and V1 scope fence are sufficiently explicit.

## Post-Rereview Cleanup

After the rereview marked the slice ready to land, one non-blocking cleanup item was still worth taking:

- normalize touched docs away from worktree-specific `.config/superpowers/worktrees/...` links

That cleanup was applied in the touched files called out by review:

- [DEVICE_RUNTIME_IO_ARCHITECTURE.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/architecture/DEVICE_RUNTIME_IO_ARCHITECTURE.md)
- [V1_EXECUTION_TRACK.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/development/V1_EXECUTION_TRACK.md)
- [2026-03-31-experiment-canvas-and-virtual-twin-architecture-plan.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/development/2026-03-31-experiment-canvas-and-virtual-twin-architecture-plan.md)

The cleanup is link-format only. It does not change the architectural claims or scope decisions reviewed above.
