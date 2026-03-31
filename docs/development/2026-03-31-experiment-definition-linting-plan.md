# Experiment-Definition Linting Plan

## Goal

Implement a reusable static linting unit for authored experiment packages so Orchestral can reject structurally invalid experiment definitions before initialization and before any hardware start path is attempted.

## In Scope

- add a reusable authored-package lint result in Core
- add first V1 static lint rules for experiment-definition structure
- surface lint blockers during initialize in the experiment monitor path
- keep start blocked when lint errors exist
- add tests for lint rules and initialize-time surfacing
- sync execution and architecture docs if shared truth changes

## Out Of Scope

- replacing cross-session validation
- adding experiment-specific scientific lint rules
- changing device-session validation
- replay or simulator work

## Governing Docs

Required foundation:

- [MODULE_DEVELOPMENT_WORKFLOW.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/development/MODULE_DEVELOPMENT_WORKFLOW.md)
- [REVIEW_PROTOCOL.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/collaboration/REVIEW_PROTOCOL.md)
- [SUBAGENT_DEVELOPMENT_CONTRACT.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/collaboration/SUBAGENT_DEVELOPMENT_CONTRACT.md)
- [V1_EXECUTION_TRACK.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/development/V1_EXECUTION_TRACK.md)
- [SYSTEM_LANGUAGE_SPEC.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/architecture/SYSTEM_LANGUAGE_SPEC.md)
- [DEVICE_RUNTIME_IO_ARCHITECTURE.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/architecture/DEVICE_RUNTIME_IO_ARCHITECTURE.md)

Supporting reference:

- [2026-03-31-universal-completion-and-experiment-logic-proof-plan.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/development/2026-03-31-universal-completion-and-experiment-logic-proof-plan.md)

Conflict winner:

- branch-local architecture and execution docs

## Current Reality

- initialize currently builds a `RunContextDefinition`
- initialize then runs cross-session validation on the resolved experiment package
- there is no authored-package lint result yet
- structural mistakes in transforms, monitors, stop conditions, or declared references are not checked through one reusable pre-flight unit

## Implementation Shape

1. Add authored-package lint types in Core
   - `ExperimentDefinitionLintIssue`
   - `ExperimentDefinitionLintResult`

2. Add `ExperimentDefinition.Lint()`
   - static structural checks only in V1
   - no runtime or hardware checks

3. First V1 lint rules
   - duplicate ids inside each authored collection
   - transform input/output stream references must exist
   - monitor observed-stream references must exist
   - control-target measured source must exist
   - control-target role and parameter references must exist

4. Initialize-time gate
   - run lint before cross-session validation
   - block initialize and keep start disabled when lint errors exist
   - surface lint findings as experiment monitor alarm items

## Verification

- targeted Core tests for linting rules
- App tests for initialize-time blocking and monitor surfacing
- full solution build
- full solution test run

## Review Gate

This slice is shared foundation and requires external review with file-backed artifacts under:

- `docs/collaboration/reviews/`

Preferred reviewer:

- `claude-opus-4-6`

## Landing Target

- commit on `codex/runtime-io-microphone`
- push to `origin/codex/runtime-io-microphone`
- update execution track after landing
