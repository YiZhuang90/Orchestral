# Runtime Operator Controls Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add the missing operator-facing runtime controls before the higher-level coordinator/orchestration slice:

- minimal output-settings sub-panel
- device-level emergency stop for controlled devices
- `Apply and Exit` semantics that leave the device in a known idle/waiting state

**Why now:** The device-session layer is strong enough, but the operator/runtime surface is still missing key controls. Without these, the coordinator layer would be built on top of an incomplete device-control surface.

**Architecture:** Device sessions remain the runtime owners. Panels remain session clients. The new controls should operate through session commands and session-end records, not through panel-local hacks.

**Tech Stack:** C#/.NET 8, WPF, `ExperimentalControlPlatform.Runtime`, `ExperimentalControlPlatform.App`, xUnit, review-backed landing flow.

## Required Foundation Docs

Required foundation:

- [DEVICE_RUNTIME_IO_ARCHITECTURE.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/architecture/DEVICE_RUNTIME_IO_ARCHITECTURE.md)
- [INTEGRATION_PANEL_IO_CONTRACT.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/architecture/INTEGRATION_PANEL_IO_CONTRACT.md)
- [DEVICE_PANEL_CONTRACT.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/architecture/DEVICE_PANEL_CONTRACT.md)
- [MODULE_DEVELOPMENT_WORKFLOW.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/development/MODULE_DEVELOPMENT_WORKFLOW.md)
- [REVIEW_PROTOCOL.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/collaboration/REVIEW_PROTOCOL.md)

Conflict winner:

- [DEVICE_RUNTIME_IO_ARCHITECTURE.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/architecture/DEVICE_RUNTIME_IO_ARCHITECTURE.md)

## Locked Product Decisions

### 1. Minimal output-settings surface

The output-settings sub-panel should stay intentionally small.

The first-generation required controls are:

1. `Payload type`
   - why: downstream consumers need to know what is being emitted
   - examples: scalar, waveform, image, status, diagnostics, command result
2. `Emission mode`
   - why: this is the core behavior switch for whether the runtime emits snapshots, periodic stream output, or event-driven output
   - examples: latest-only, periodic, on-change
3. `Output frequency`
   - why: rate limiting is the minimum practical control for data volume and downstream load
4. `Include metadata`
   - why: device id, endpoint id, timestamps, and applied settings context are critical for later experiment logging and replay

`Destination` is intentionally excluded from the first-generation operator surface.
The runtime bus should be the default underlying path, and higher-level orchestration or the embedded AI agent should decide whether outputs are wired onward to recorder, downstream computation, experiment logging, or report generation.

Everything else remains out of the first-generation surface and can be added later directly or mediated by the embedded AI agent.

### 2. Emergency stop hierarchy

Emergency stop exists at two levels:

1. `Device-level emergency stop`
   - mandatory base primitive
   - required only for controlled devices or any device that can create real damage
   - examples: pump, valve, stage, power output, laser controller
2. `System-level emergency stop`
   - later coordinator feature
   - implemented as a hub that calls device-level emergency stop across active controlled sessions

Acquisition-only devices do not need a dramatic emergency-stop affordance by default. Their ordinary stop/disconnect behavior is sufficient unless a specific hardware risk says otherwise.

### 3. Apply and Exit semantics

`Apply and Exit` means:

1. apply current settings successfully
2. persist/log the applied-settings record
3. emit session-end output
4. stop live activity
5. disconnect hardware if that device uses a connection-oriented session
6. dispose the device-level runtime session
7. leave the device in a known idle/waiting-for-call state

This is not just a window-close button. It is an explicit device-session handoff action.

## Task 1: Output Settings Sub-Panel

- [ ] **Step 1: Define the first-generation output settings model**

Add a small settings record or view-model surface with:

- `PayloadType`
- `EmissionMode`
- `OutputFrequencyHz`
- `IncludeMetadata`

- [ ] **Step 2: Decide the panel placement**

Use one `Output settings` button that opens a sub-panel or modal.
Do not crowd the main parameter rail.

- [ ] **Step 3: Wire the sub-panel to the device panel contract**

The panel should stage and apply output settings through the session/client boundary, not through local-only display flags.

- [ ] **Step 4: Add verification**

Cover:

- opening the sub-panel
- saving staged values
- applying values to the session/client layer
- truthful logging in diagnostics/applied-settings output

## Task 2: Device-Level Emergency Stop

- [ ] **Step 1: Lock the applicability rule**

Emergency stop is required for:

- controlled devices
- any hybrid device whose control side can create real damage

Emergency stop is not required by default for pure acquisition devices.

- [ ] **Step 2: Add the runtime command surface**

Define a device-session-level emergency-stop command/result path for controlled devices.

- [ ] **Step 3: Implement the first controlled-device emergency stop**

Start with the existing controlled-device pilot so the pattern is real, not theoretical.

- [ ] **Step 4: Add the panel affordance**

Requirements:

- clearly visible
- not mixed with ordinary connect/apply actions
- disabled only when truly unavailable

- [ ] **Step 5: Verify safe-state semantics**

The stop action must leave the device in a known safe/idle state and log:

- stop command issued
- stop result
- final device status

## Task 3: Apply and Exit

- [ ] **Step 1: Add explicit `Apply and Exit` action semantics**

The action should be modeled explicitly, not improvised out of existing `Apply` plus `Disconnect`.

- [ ] **Step 2: Define the runtime sequence**

Sequence:

1. validate
2. apply
3. log applied settings
4. emit session-end record
5. stop/disconnect/dispose

- [ ] **Step 3: Decide where the button appears**

It should be available where settings handoff matters, not automatically on every trivial surface.

- [ ] **Step 4: Ensure post-exit state is truthful**

After `Apply and Exit`, the device session must no longer appear active.
The device should be represented as idle/waiting for the next system-level call.

- [ ] **Step 5: Add tests**

Cover:

- apply success path
- apply failure does not falsely exit
- session-end output exists
- connected state becomes false after exit

## Task 4: Documentation and Review

- [ ] **Step 1: Update runtime docs if semantics changed**

At minimum review:

- [DEVICE_RUNTIME_IO_ARCHITECTURE.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/architecture/DEVICE_RUNTIME_IO_ARCHITECTURE.md)
- [INTEGRATION_PANEL_IO_CONTRACT.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/architecture/INTEGRATION_PANEL_IO_CONTRACT.md)
- [DEVICE_PANEL_CONTRACT.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/architecture/DEVICE_PANEL_CONTRACT.md)

- [ ] **Step 2: External review**

External review is required for this slice because it changes:

- operator control semantics
- session-end behavior
- controlled-device safety surface

Use Claude CLI in non-interactive mode with `claude-opus-4-6`.

- [ ] **Step 3: Record review response and re-review if needed**

## Verification Gates

- [ ] `dotnet build .\platform\ExperimentalControlPlatform.sln`
- [ ] `dotnet test .\platform\ExperimentalControlPlatform.sln --no-build`

Required behavioral verification:

- output-settings sub-panel opens and applies
- controlled-device emergency stop leaves the device safe/idle
- `Apply and Exit` produces the expected session-end/logging/disconnect behavior

## Definition Of Done

This slice is complete when:

- minimal output settings exist as a real sub-panel
- the first controlled device has a real device-level emergency stop
- `Apply and Exit` exists with truthful session-end semantics
- tests pass
- review artifacts exist
- the execution track is updated to point to the next slice: coordinator/orchestration
