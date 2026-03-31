# Flow/Reynolds Derived-State And Measured-Stream Wiring Plan

## Goal

Make the experiment plane compute a real measured Reynolds-number state from runtime telemetry and wire that measured state into both:

- `ControllerUnitSession`
- `ExperimentMonitorSession`

so the existing `Target +/- Error` display and controller logic use actual derived experiment values rather than only placeholder sampling.

## In Scope

- add a runtime-owned derived-state unit for flow/Reynolds computation
- derive Reynolds number from:
  - control-center pulse telemetry
  - PT-104 temperature samples when available
  - experiment parameters for geometry and calibration
- wire derived-state samples into the controller unit
- wire derived-state snapshot/state into the monitor surface
- persist first-generation derived-state artifacts in the run recorder path
- extend the ad hoc experiment package so the required measured-state streams and parameters exist with explicit defaults
- update architecture/execution docs to reflect the new runtime truth

## Out Of Scope

- experiment-specific end-height actuator sessions
- multi-target controller orchestration
- camera-pair processing
- puff detection or trigger conversion
- full cross-session validation

## Governing Docs

Required foundation:

- [MODULE_DEVELOPMENT_WORKFLOW.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/development/MODULE_DEVELOPMENT_WORKFLOW.md)
- [REVIEW_PROTOCOL.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/collaboration/REVIEW_PROTOCOL.md)
- [SUBAGENT_DEVELOPMENT_CONTRACT.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/collaboration/SUBAGENT_DEVELOPMENT_CONTRACT.md)
- [DEVICE_RUNTIME_IO_ARCHITECTURE.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/architecture/DEVICE_RUNTIME_IO_ARCHITECTURE.md)
- [SYSTEM_LANGUAGE_SPEC.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/architecture/SYSTEM_LANGUAGE_SPEC.md)
- [TURBULENCE_EXPERIMENT_FLOW.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/legacy-knowledge/TURBULENCE_EXPERIMENT_FLOW.md)
- [DEVICE_INVENTORY.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/legacy-knowledge/DEVICE_INVENTORY.md)
- [DATA_OUTPUT_INVENTORY.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/legacy-knowledge/DATA_OUTPUT_INVENTORY.md)

Conflict winner:

- runtime and language docs in the current branch define architecture shape
- legacy docs define scientific and operational truth for Reynolds-number derivation inputs/defaults

## Current Reality

- `ControllerUnitSession` already supports measured values but only receives a manual initial sample
- `ExperimentMonitorSession` already displays `Target +/- Error`, but it does not own a real measured-state source
- `ControlCenterSession` already exposes pulse telemetry snapshots/streams
- `Pt104Session` already exposes temperature readings
- the ad hoc experiment package already declares `stream.reynolds_number` when a primary target is configured, but it does not yet declare the broader flow/Reynolds measured-state inputs explicitly
- the run recorder currently writes monitor snapshots but not a dedicated Reynolds/flow derived-state artifact

## Assumptions For V1

- default pulse calibration remains `80` pulses per liter from the preserved control-center flow path
- default pipe inner diameter remains `0.00403 m` from the preserved looping notebook
- default pipe length remains `1.0 m`
- default pipe roughness remains `0.0 m`
- default fallback temperature remains `20.95 C`
- first-generation PT-104 temperature aggregation uses the mean of all currently available channel values and computes delta as `max - min` when more than one channel is available

## Execution Steps

1. Add failing tests for:
   - derived Reynolds/flow computation
   - fallback temperature behavior
   - controller measured-state wiring
   - monitor measured-state summary/warnings
   - derived-state artifact writing
2. Add shared artifact IDs/default-parameter helpers for flow/Reynolds measured state.
3. Extend the ad hoc experiment package with:
   - measured-state parameter definitions
   - flow and temperature stream declarations
   - existing control-target reuse against the derived Reynolds stream
4. Implement a runtime-owned derived-state unit that:
   - consumes pulse and temperature telemetry
   - polls control-center pulse telemetry during a live run
   - publishes derived-state snapshot and samples
5. Wire derived-state samples into `ControllerUnitSession`.
6. Wire derived-state snapshot into `ExperimentMonitorSession` and the experiment monitor panel.
7. Persist derived-state snapshot/record artifacts through the run recorder.
8. Sync docs and execution track.
9. Run build/tests/smoke, request external review, address findings, reverify, commit, and push.

## Success Criteria

- a live run can compute Reynolds number from real runtime telemetry
- `ControllerUnitSession` receives measured Reynolds samples automatically during the run
- the monitor surface shows real measured-state information, not only placeholder controller state
- derived-state artifacts are written into the run directory
- build and full test suite pass
- external review closes without blocking findings

## Outcome

Implemented in this branch:

- `FlowReynoldsDerivedStateSession` as the runtime-owned measured-state unit
- ad hoc experiment defaults and artifact ids for flow/Reynolds derivation
- measured-value wiring from derived-state samples into `ControllerUnitSession`
- monitor-side derived-state summary and stale-state warning support
- run-artifact persistence for derived-state snapshot and recorded sample outputs

Verification:

- `dotnet build .\platform\ExperimentalControlPlatform.sln -nodeReuse:false -v:minimal`
- `dotnet test .\platform\ExperimentalControlPlatform.sln -nodeReuse:false -v:minimal`

Review trail:

- `2026-03-31-flow-reynolds-derived-state-and-measured-stream-wiring-review-claude-opus-4-6.md`
- `2026-03-31-flow-reynolds-derived-state-and-measured-stream-wiring-response.md`
- `2026-03-31-flow-reynolds-derived-state-and-measured-stream-wiring-rereview-claude-opus-4-6.md`
