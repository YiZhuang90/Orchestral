# Run Recorder And Artifact Writer Plan

## Purpose

Implement the next universal experiment-plane slice above run context and buffering:

- a first real run recorder,
- a concrete artifact writer that materializes run outputs on disk,
- and manifest generation that ties the live runtime back to the artifact-first system language.

This slice is intentionally narrow. It should make run recording real now without pretending that the full scientific trace/export pipeline is already built.

## 1. Clarify The Slice

### Goal

Add a first-generation recorder/writer unit so Orchestral can:

- create a run output directory,
- materialize a structured `manifest.yaml`,
- record panel-normalized outputs as concrete run artifacts,
- preserve runtime events and warnings/faults as file-backed outputs,
- and do so even when the shell is running an ad hoc runtime session rather than a fully authored experiment package.

### In Scope

- add the first real recorder service for run start/stop lifecycle
- add a concrete artifact writer that materializes YAML artifacts on disk
- write:
  - `manifest.yaml`
  - runtime-events artifact
  - warnings/faults artifact
  - per-panel snapshot artifacts for the normalized panel contract where data exists
- generate output artifact identifiers and connect them to `RunManifestDefinition.OutputIds`
- support both:
  - explicit experiment/run-context input from the coordinator when present
  - ad hoc experiment fallback derived from active integration panels when not present
- wire the recorder into the app shell start/stop path
- add tests for:
  - ad hoc manifest generation
  - artifact writing
  - recorder integration across start/stop

### Out Of Scope

- full raw-stream capture or binary frame dumps
- processed scientific traces or puff/event tables
- database persistence
- report generation
- experiment monitor/alarm dashboards
- experiment-definition linting
- cross-session validation

### Success Criteria

- stopping a run produces a real run directory with file-backed artifacts
- `manifest.yaml` is written in YAML and references the generated output artifact ids
- normalized panel outputs are materialized as first-generation run artifacts rather than staying UI-only
- runtime events and warnings/faults are preserved as artifacts instead of transient status text only
- the slice works with both authored run context and ad hoc shell runs
- build/tests pass and a local shell smoke run produces artifacts

### Landing Target

- branch `codex/runtime-io-microphone`

## 1.1 Required Foundation Docs

### Required Foundation

- [DEVICE_RUNTIME_IO_ARCHITECTURE.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/architecture/DEVICE_RUNTIME_IO_ARCHITECTURE.md)
- [SYSTEM_LANGUAGE_SPEC.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/architecture/SYSTEM_LANGUAGE_SPEC.md)
- [TECH_STACK_DECISIONS.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/development/TECH_STACK_DECISIONS.md)
- [DATA_OUTPUT_INVENTORY.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/legacy-knowledge/DATA_OUTPUT_INVENTORY.md)
- [MODULE_DEVELOPMENT_WORKFLOW.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/development/MODULE_DEVELOPMENT_WORKFLOW.md)

### Supporting Reference

- [RunManifestDefinition.cs](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/platform/src/ExperimentalControlPlatform.Core/Artifacts/RunManifestDefinition.cs)
- [RunContextDefinition.cs](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/platform/src/ExperimentalControlPlatform.Core/Artifacts/RunContextDefinition.cs)
- [IIntegrationPanelViewModel.cs](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/platform/src/ExperimentalControlPlatform.App/DevicePanels/Contracts/IIntegrationPanelViewModel.cs)
- [IntegrationPanelDataOutput.cs](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/platform/src/ExperimentalControlPlatform.App/DevicePanels/Contracts/IntegrationPanelDataOutput.cs)
- [RuntimeCoordinator.cs](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/platform/src/ExperimentalControlPlatform.Runtime/RuntimeCoordinator.cs)

### Conflict Winner

- [SYSTEM_LANGUAGE_SPEC.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/architecture/SYSTEM_LANGUAGE_SPEC.md)

## 2. Current Reality

- `RunManifestDefinition` already exists, but nothing writes it
- `RunContextDefinition` now exists, but the current app shell still starts bare runtime runs
- the most normalized output surface currently available is the integration-panel contract in the app layer:
  - `DataOutput`
  - `AppliedSettingsOutput`
  - `StatusOutput`
  - `DiagnosticsOutput`
  - `SessionEndOutput`
- runtime events and stop reasons exist in runtime state, but they are not yet persisted as artifacts
- artifact format guidance currently says run artifacts should be human-readable YAML backed by typed models

## 3. Ordered Tasks

1. Add failing tests for:
   - ad hoc experiment fallback from active integration panels
   - manifest creation with output artifact ids
   - YAML artifact writing for a stopped run
   - shell integration of recorder start/stop flow
2. Add the recorder/artifact models and writer seam
3. Implement the ad hoc experiment/run fallback for shell-driven runs
4. Implement YAML artifact writing for:
   - manifest
   - runtime events
   - warnings/faults
   - panel snapshots
5. Wire the recorder into app-shell runtime start/stop
6. Run full build/test verification
7. Run external review
8. Address findings, reverify, and sync docs

## 4. Owned Files

Primary:

- `platform/src/ExperimentalControlPlatform.App/MainViewModel.cs`
- `platform/src/ExperimentalControlPlatform.App/RuntimeStatusViewModel.cs`
- `platform/src/ExperimentalControlPlatform.App/DevicePanels/Contracts/IIntegrationPanelViewModel.cs`
- `platform/src/ExperimentalControlPlatform.App/RunRecorder.cs`
- `platform/src/ExperimentalControlPlatform.App/RunArtifactWriter.cs`
- `platform/src/ExperimentalControlPlatform.App/RunRecordingResult.cs`
- `platform/src/ExperimentalControlPlatform.App/AdHocRunDefinitionFactory.cs`
- `platform/tests/ExperimentalControlPlatform.App.Tests/RunRecorderTests.cs`
- `platform/tests/ExperimentalControlPlatform.App.Tests/MainViewModelTests.cs`

Likely secondary:

- `platform/src/ExperimentalControlPlatform.Core/Artifacts/RunManifestDefinition.cs`
- `platform/tests/ExperimentalControlPlatform.Core.Tests/Artifacts/RunManifestDefinitionTests.cs`
- `platform/src/ExperimentalControlPlatform.App/ExperimentalControlPlatform.App.csproj`
- `docs/architecture/DEVICE_RUNTIME_IO_ARCHITECTURE.md`
- `docs/development/V1_EXECUTION_TRACK.md`

## 5. Review Gate

External review is required because this slice is:

- shared experiment-plane foundation,
- a new artifact-writing boundary,
- and the first authoritative persistence unit above the runtime.

Preferred reviewer channel:

- Gemini CLI `gemini-3-flash-preview`

Fallback reviewer channel:

- Claude Code CLI when quota allows

## 6. Verification Gate

Minimum:

- `dotnet build .\\platform\\ExperimentalControlPlatform.sln -nodeReuse:false`
- `dotnet test .\\platform\\ExperimentalControlPlatform.sln --no-build -nodeReuse:false`

Required smoke:

- one local app-shell launch
- one run start/stop cycle that produces a real artifact directory

## 7. Result

Completed in branch code with:

- `RunRecorder` as the first real run-recorder lifecycle unit
- `RunArtifactWriter` materializing:
  - `manifest.yaml`
  - `runtime-events.yaml`
  - `warnings-or-faults.yaml`
  - per-panel snapshot artifacts
- ad hoc experiment fallback from active integration panels when no authored run context is attached to the shell
- `RunManifestDefinition.Artifacts` as a typed artifact-path mapping
- app-shell stop integration that records artifacts both on normal stop and on shutdown finalization

Verification:

- `dotnet build .\\platform\\ExperimentalControlPlatform.sln -nodeReuse:false -v:minimal`
- `dotnet test .\\platform\\ExperimentalControlPlatform.sln --no-build -nodeReuse:false -v:minimal`
- `dotnet run --project .\\temp\\run-recorder-smoke\\run-recorder-smoke.csproj`

Review artifacts:

- [2026-03-30-run-recorder-and-artifact-writer-review-gemini-3-flash-preview.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/collaboration/reviews/2026-03-30-run-recorder-and-artifact-writer-review-gemini-3-flash-preview.md)
- [2026-03-30-run-recorder-and-artifact-writer-response.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/collaboration/reviews/2026-03-30-run-recorder-and-artifact-writer-response.md)
- [2026-03-30-run-recorder-and-artifact-writer-rereview-gemini-3-flash-preview.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/collaboration/reviews/2026-03-30-run-recorder-and-artifact-writer-rereview-gemini-3-flash-preview.md)
