# Integration Panel IO Contract

## Purpose

This document defines what an Orchestral integration panel consumes, what it emits, and what should happen when the operator applies settings or exits the panel.

The goal is to make every integration panel usable not only as a UI surface, but also as a trustworthy source of structured state for:

- data display modules,
- internal computation,
- downstream data processing,
- logging,
- recording,
- and future orchestration.

The runtime architecture that should carry those inputs and outputs at system level is defined in:

- [DEVICE_RUNTIME_IO_ARCHITECTURE.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/architecture/DEVICE_RUNTIME_IO_ARCHITECTURE.md)

## Core Principle

An integration panel is not only a renderer.

It is a bounded operational node with:

- input,
- runtime state,
- output,
- and termination behavior.

## 1. Input Contract

**Status**: architecture intent, not yet implemented as a general system contract.

The input contract depends on:

- a device identity registry,
- a capability-model layer,
- and a settings persistence layer.

Those are out of scope for the first runtime IO implementation pass.
Current panels still rely heavily on hardcoded defaults and local initialization paths.

Every integration panel should consume:

- `DeviceIdentity`
  - hardware identity, model, serial, transport, instance id
- `CapabilityModel`
  - supported modes, settings, limits, alignment rules, advanced-setting surface
- `SavedSettings`
  - persisted settings available before the panel opens
- `DefaultSettings`
  - fallback settings when no saved settings exist
- `TemplateContext`
  - which class template or general shell is consuming the panel

## 2. Runtime State Contract

Every integration panel should maintain runtime state across three scopes:

### Device scope

- connection status
- shared diagnostics
- shared hardware health
- shared capabilities currently enabled

### Endpoint scope

- active endpoint selection
- per-endpoint settings
- per-endpoint live values or frames
- per-endpoint plot, stats, or history

### Session scope

- idle
- read once or snap
- triggered live
- continuous live
- export in progress
- shutting down

## 3. Output Contract

Every integration panel should emit structured outputs.

### 3.1 Data output

The panel should expose the current device payload in a structured form.

Examples:

- scalar sample stream
- waveform frame
- image frame
- actuator feedback sample
- protocol message stream

Suggested fields:

- `Timestamp`
- `DeviceId`
- `EndpointId`
- `PayloadType`
- `PayloadValue`
- `Units`
- `SequenceNumber`
- `CaptureRate`
- `SourceMode`

### 3.2 Applied-settings output

The panel should expose what is actually applied, not only what is typed into visible controls.

Suggested fields:

- `AppliedAt`
- `DeviceSettings`
- `EndpointSettings`
- `SessionSettings`
- `NormalizationNotes`

Examples of normalization notes:

- ROI width normalized to multiple of 12
- trigger mode coerced by hardware
- mains setting is global to the shared box

### 3.3 Status output

The panel should expose structured operational status.

Suggested fields:

- `Connected`
- `ReadyState`
- `FaultState`
- `LiveState`
- `SelectedEndpoint`
- `BackgroundActiveEndpoints`

### 3.4 Diagnostics output

The panel should expose enough internal evidence to support debugging and handover verification.

Suggested fields:

- `LastCommand`
- `LastHardwareResponse`
- `LastError`
- `LastStateTransition`
- `LastValidationResult`

### 3.5 Session-end output

When the panel closes or terminates a session, it should emit a final record.

Suggested fields:

- `EndedAt`
- `ExitReason`
- `ConnectionClosed`
- `LiveStopped`
- `AppliedSettingsSnapshot`
- `FinalStatus`
- `OpenIssues`

## 4. Lifecycle Actions

Every panel should define the operational meaning of:

- `Apply`
- `Apply and exit`
- `Close without apply`
- `Disconnect and close` when relevant

## 4.1 Apply

`Apply` should:

- validate visible settings,
- apply them to the correct state scope,
- update the applied-settings output,
- preserve the panel session unless the hardware requires a restart.

## 4.2 Apply and exit

`Apply and exit` should:

- perform the same validation and apply step,
- log the final applied settings,
- terminate the live session and hardware connection cleanly,
- emit the session-end output,
- close the panel.

This should be the default safe termination path for integration work.

## 4.3 Close without apply

`Close without apply` should:

- discard unapplied edits,
- preserve or terminate hardware state according to explicit device policy,
- and record that settings were not applied.

## 4.4 Disconnect and close

If a panel exposes `Disconnect and close`, it should:

- stop live acquisition,
- disconnect the hardware,
- emit final status and diagnostics outputs,
- then close the panel.

## 5. Truthfulness Rules

The panel output should never confuse:

- requested value,
- applied value,
- measured value,
- displayed value.

Examples:

- `Target frame rate` is not `Capture rate`
- requested ROI is not applied ROI when hardware normalization occurs
- displayed refresh rate is not the same as capture or acquisition rate

## 6. Consumers of Panel Output

The output contract exists so other modules can consume integration state without scraping UI text.

Likely consumers:

- data-display module
- compare/overlay module
- recording/export pipeline
- computational pipeline
- diagnostics log
- automated handover verification

## 7. Minimum Success Criterion

This contract is successful when a new module can consume panel outputs directly for analysis, comparison, or logging without needing device-specific UI knowledge.
