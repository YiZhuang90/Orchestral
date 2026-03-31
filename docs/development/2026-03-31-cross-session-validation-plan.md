# Cross-Session Validation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** add a reusable runtime-owned cross-session validation pass that checks relationships across more than one role binding or control target before a run starts, blocks invalid starts, and surfaces the result during initialize-time operator flow.

**Architecture:** keep session-local validation where it is, and add a second validation seam above the resolved experiment package. The first cross-session rules should stay universal: reject ambiguous shared-device capability ownership across multiple role bindings, and reject multiple control targets that try to command the same role in V1. Enforce the validation in the runtime coordinator, but surface it through the experiment monitor initialize flow before start.

**Tech Stack:** .NET 8, C#, xUnit, WPF MVVM

---

## Slice Definition

**In scope**

- structured cross-session validation result contract
- validation rules over interacting role bindings and control targets
- runtime-coordinator validation enforcement before start
- initialize-time surfacing in the experiment monitor panel flow
- test coverage for core/runtime/app behavior

**Out of scope**

- experiment-specific scientific logic
- session-registry live-hardware availability checks
- experiment-definition linting beyond the first cross-session rules
- virtual device and replay/simulation infrastructure

**Required foundation**

- [MODULE_DEVELOPMENT_WORKFLOW.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/development/MODULE_DEVELOPMENT_WORKFLOW.md)
- [DEVICE_RUNTIME_IO_ARCHITECTURE.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/architecture/DEVICE_RUNTIME_IO_ARCHITECTURE.md)
- [SYSTEM_LANGUAGE_SPEC.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/architecture/SYSTEM_LANGUAGE_SPEC.md)
- [V1_EXECUTION_TRACK.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/development/V1_EXECUTION_TRACK.md)
- [2026-03-31-universal-completion-and-experiment-logic-proof-plan.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/development/2026-03-31-universal-completion-and-experiment-logic-proof-plan.md)

**Conflict winner**

- [SYSTEM_LANGUAGE_SPEC.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/architecture/SYSTEM_LANGUAGE_SPEC.md) for universal language semantics
- [DEVICE_RUNTIME_IO_ARCHITECTURE.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/architecture/DEVICE_RUNTIME_IO_ARCHITECTURE.md) for runtime ownership and start/stop behavior

**Success criteria**

- the platform can produce a structured cross-session validation result before run start
- the runtime coordinator rejects invalid run contexts instead of starting anyway
- initialize-time UI flow surfaces the invalid result and keeps `Start` blocked
- tests prove at least one real multi-binding contradiction and one conflicting-control-target contradiction

---

## Task 1: Add Structured Cross-Session Validation Contract

**Files:**
- Create: `platform/src/ExperimentalControlPlatform.Core/Artifacts/CrossSessionValidationIssue.cs`
- Create: `platform/src/ExperimentalControlPlatform.Core/Artifacts/CrossSessionValidationResult.cs`
- Modify: `platform/src/ExperimentalControlPlatform.Core/Artifacts/ResolvedExperimentDefinition.cs`
- Test: `platform/tests/ExperimentalControlPlatform.Core.Tests/Artifacts/ResolvedExperimentDefinitionTests.cs`

- [x] **Step 1: Add failing core tests for duplicate shared-device capability claims across role bindings**
- [x] **Step 2: Add failing core tests for conflicting control targets that command the same role**
- [x] **Step 3: Implement the new cross-session validation contract and the first rule set**
- [x] **Step 4: Run the targeted core tests and verify they pass**

## Task 2: Enforce Validation In Runtime Start

**Files:**
- Modify: `platform/src/ExperimentalControlPlatform.Runtime/IRuntimeCoordinator.cs`
- Modify: `platform/src/ExperimentalControlPlatform.Runtime/RuntimeCoordinator.cs`
- Test: `platform/tests/ExperimentalControlPlatform.Runtime.Tests/RuntimeCoordinatorTests.cs`

- [x] **Step 1: Add failing runtime tests proving invalid run contexts are rejected before start**
- [x] **Step 2: Add a validation seam on the coordinator and reuse the core cross-session result**
- [x] **Step 3: Make `Start(...)` reject invalid run contexts with the structured validation summary**
- [x] **Step 4: Run the targeted runtime tests and verify they pass**

## Task 3: Surface Validation During Initialize Flow

**Files:**
- Modify: `platform/src/ExperimentalControlPlatform.App/MainViewModel.cs`
- Modify: `platform/src/ExperimentalControlPlatform.App/ExperimentMonitor/ExperimentMonitorPanelViewModel.cs`
- Modify: `platform/src/ExperimentalControlPlatform.App/ExperimentMonitor/ExperimentMonitorPanelTemplate.xaml`
- Test: `platform/tests/ExperimentalControlPlatform.App.Tests/MainViewModelTests.cs`
- Test: `platform/tests/ExperimentalControlPlatform.App.Tests/ExperimentMonitorPanelViewModelTests.cs`

- [x] **Step 1: Add failing app tests proving invalid initialize results keep `Start` disabled**
- [x] **Step 2: Surface cross-session validation summaries and issue items in the monitor initialize flow**
- [x] **Step 3: Make direct start paths validate too, not only initialize-driven paths**
- [x] **Step 4: Run the targeted app tests and verify they pass**

## Task 4: Verify And Sync Slice Status

**Files:**
- Modify: `docs/development/V1_EXECUTION_TRACK.md`
- Modify: `docs/architecture/DEVICE_RUNTIME_IO_ARCHITECTURE.md`
- Modify: `docs/architecture/SYSTEM_LANGUAGE_SPEC.md`

- [x] **Step 1: Run the full solution build**
- [x] **Step 2: Run the full test suite**
- [x] **Step 3: Update execution and architecture docs to mark cross-session validation as landed**
- [x] **Step 4: Record review artifacts and note the next universal unit**

## Result

- cross-session validation result contract added
- resolved experiment packages now expose reusable cross-session relationship validation
- runtime coordinator enforces that validation before start
- experiment monitor initialize flow surfaces invalid cross-session relationships before hardware start
- local verification passed:
  - `dotnet build .\\platform\\ExperimentalControlPlatform.sln -nodeReuse:false -v:minimal`
  - `dotnet test .\\platform\\ExperimentalControlPlatform.sln --no-build -nodeReuse:false -v:minimal`
- review artifacts:
  - `2026-03-31-cross-session-validation-review-claude-opus-4-6.md`
  - `2026-03-31-cross-session-validation-response.md`
  - `2026-03-31-cross-session-validation-rereview-claude-opus-4-6.md`
- remaining universal sequence after this slice:
  - `Experiment-definition linting`
  - `Virtual device and replay/simulation harness`
