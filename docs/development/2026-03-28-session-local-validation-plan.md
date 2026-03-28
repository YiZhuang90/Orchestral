# Session-Local Validation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** add a real session-local validation unit so each runtime session can validate its own settings or commands before apply/connect, and panels stop silently falling back to stale values when drafts are invalid.

**Architecture:** introduce a shared runtime validation result contract, convert the existing per-session private validation guards into public validation methods that return structured results, and reuse those validators in the panel layer before connect/apply. Keep the first slice local to individual sessions; do not mix in cross-session or coordinator rules yet.

**Tech Stack:** .NET 8, C#, xUnit, WPF MVVM

---

## Slice Definition

**In scope**

- shared runtime `SessionValidationResult` contract
- structured validation for:
  - integrated microphone settings
  - integrated camera settings
  - HuaTeng camera settings
  - PT-104 channel configuration
  - control-center commands
- panel-side use of validation so invalid drafts do not apply via fallback values
- test coverage for runtime validation and key panel behavior

**Out of scope**

- cross-session validation
- coordinator/run-level validation
- stream buffering policy
- new coordinator behavior

**Required foundation**

- [MODULE_DEVELOPMENT_WORKFLOW.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/development/MODULE_DEVELOPMENT_WORKFLOW.md)
- [DEVICE_RUNTIME_IO_ARCHITECTURE.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/architecture/DEVICE_RUNTIME_IO_ARCHITECTURE.md)
- [INTEGRATION_PANEL_IO_CONTRACT.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/architecture/INTEGRATION_PANEL_IO_CONTRACT.md)
- [DEVICE_PANEL_CONTRACT.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/architecture/DEVICE_PANEL_CONTRACT.md)
- [2026-03-28-functional-unit-roadmap.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/development/2026-03-28-functional-unit-roadmap.md)

**Conflict winner**

- [DEVICE_RUNTIME_IO_ARCHITECTURE.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/architecture/DEVICE_RUNTIME_IO_ARCHITECTURE.md)

**Success criteria**

- sessions expose structured validation instead of only private exception guards
- invalid drafts no longer silently reuse previous numeric values in the panel layer
- `CanApply...` reflects real validation, not just “dirty” state
- tests prove both runtime and panel behavior

---

## Task 1: Add Shared Runtime Validation Contract

**Files:**
- Create: `platform/src/ExperimentalControlPlatform.Runtime/SessionValidationIssue.cs`
- Create: `platform/src/ExperimentalControlPlatform.Runtime/SessionValidationResult.cs`
- Test: `platform/tests/ExperimentalControlPlatform.Runtime.Tests/SessionValidationResultTests.cs`

- [x] **Step 1: Write the failing tests for the result contract**
- [x] **Step 2: Run the runtime test project to verify the new tests fail**
- [x] **Step 3: Add the minimal runtime validation result types**
- [x] **Step 4: Run the runtime test project to verify the tests pass**

## Task 2: Convert Sessions To Structured Validation

**Files:**
- Modify: `platform/src/ExperimentalControlPlatform.Runtime/IntegratedMicrophoneSession.cs`
- Modify: `platform/src/ExperimentalControlPlatform.Runtime/IntegratedCameraSession.cs`
- Modify: `platform/src/ExperimentalControlPlatform.Runtime/HuaTengCameraSession.cs`
- Modify: `platform/src/ExperimentalControlPlatform.Runtime/Pt104Session.cs`
- Modify: `platform/src/ExperimentalControlPlatform.Runtime/ControlCenterSession.cs`
- Test: `platform/tests/ExperimentalControlPlatform.Runtime.Tests/IntegratedMicrophoneSessionTests.cs`
- Test: `platform/tests/ExperimentalControlPlatform.Runtime.Tests/IntegratedCameraSessionTests.cs`
- Test: `platform/tests/ExperimentalControlPlatform.Runtime.Tests/HuaTengCameraSessionTests.cs`
- Test: `platform/tests/ExperimentalControlPlatform.Runtime.Tests/Pt104SessionTests.cs`
- Test: `platform/tests/ExperimentalControlPlatform.Runtime.Tests/ControlCenterSessionTests.cs`

- [x] **Step 1: Add failing runtime tests that assert invalid settings/commands return a structured invalid result**
- [x] **Step 2: Run the targeted runtime tests and verify they fail**
- [x] **Step 3: Replace private throw-only validators with public structured validators and internal throw-on-invalid calls**
- [x] **Step 4: Keep diagnostics text aligned with the new validation result summaries**
- [x] **Step 5: Run the targeted runtime tests and verify they pass**

## Task 3: Stop Panel Draft Fallback On Invalid Input

**Files:**
- Modify: `platform/src/ExperimentalControlPlatform.App/DevicePanels/Microphone/IntegratedMicrophonePanelViewModel.cs`
- Modify: `platform/src/ExperimentalControlPlatform.App/DevicePanels/Integrated/IntegratedCameraPanelViewModel.cs`
- Modify: `platform/src/ExperimentalControlPlatform.App/DevicePanels/HuaTeng/HuaTengPanelViewModel.cs`
- Modify: `platform/src/ExperimentalControlPlatform.App/DevicePanels/ControlCenter/ControlCenterPanelViewModel.cs`
- Test: `platform/tests/ExperimentalControlPlatform.App.Tests/IntegratedMicrophonePanelViewModelTests.cs`
- Test: `platform/tests/ExperimentalControlPlatform.App.Tests/ControlCenterPanelViewModelTests.cs`

- [x] **Step 1: Add failing app tests for invalid draft behavior**
- [x] **Step 2: Run the targeted app tests and verify they fail**
- [x] **Step 3: Replace fallback draft-building with validation-aware draft creation**
- [x] **Step 4: Make `CanApply...` depend on both dirty state and valid draft state**
- [x] **Step 5: Run the targeted app tests and verify they pass**

## Task 4: Verify And Sync Slice Status

**Files:**
- Modify: `docs/development/V1_EXECUTION_TRACK.md`
- Modify: `docs/development/2026-03-28-functional-unit-roadmap.md`

- [x] **Step 1: Run the full solution build**
- [x] **Step 2: Run the full test suite**
- [x] **Step 3: Update execution/status docs to mark session-local validation as partially or fully built based on the actual result**
- [x] **Step 4: Summarize the next coordinator slice based on what remains after this validation work**

## Result

- shared runtime validation result contract added
- runtime sessions now expose structured validation instead of only private throw-only guards
- microphone, integrated camera, and HuaTeng panels now gate invalid drafts before apply/connect where that path is session-owned
- control-center and PT-104 now expose static validation paths for pre-session use
- local verification passed:
  - `dotnet build .\\platform\\ExperimentalControlPlatform.sln -nodeReuse:false`
  - `dotnet test .\\platform\\ExperimentalControlPlatform.sln --no-build -nodeReuse:false`
- review artifacts:
  - [2026-03-28-session-local-validation-review.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/collaboration/reviews/2026-03-28-session-local-validation-review.md)
  - [2026-03-28-session-local-validation-response.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/collaboration/reviews/2026-03-28-session-local-validation-response.md)
  - [2026-03-28-session-local-validation-rereview.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/collaboration/reviews/2026-03-28-session-local-validation-rereview.md)
