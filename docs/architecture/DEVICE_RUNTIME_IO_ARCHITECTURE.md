# Device Runtime IO Architecture

## Purpose

This document defines the runtime IO layer that sits between:

- hardware backends,
- integration panels,
- and future data-processing, comparison, automation, and orchestration modules.

The panel contract defines how a device panel should look and behave.
The IO contract defines what a panel should emit.
This document defines the missing runtime layer that makes those outputs and inputs first-class system behavior instead of view-model-local behavior.

Read this together with:

- [DEVICE_PANEL_CONTRACT.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/architecture/DEVICE_PANEL_CONTRACT.md)
- [INTEGRATION_PANEL_IO_CONTRACT.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/architecture/INTEGRATION_PANEL_IO_CONTRACT.md)
- [DEVICE_ARCHETYPE_MAPPING.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/architecture/DEVICE_ARCHETYPE_MAPPING.md)
- [TIMING_AND_SYNCHRONIZATION_STRATEGY.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/architecture/TIMING_AND_SYNCHRONIZATION_STRATEGY.md)

## Why

### Why this layer is needed

The integration panel should not become the system bus.

Without a runtime IO layer, each device panel tends to own too much:

- connection lifecycle,
- acquisition or control loops,
- internal state,
- output formatting,
- command handling,
- and downstream integration.

That creates three problems:

1. downstream modules end up depending on panel-specific view-models,
2. acquisition and control logic become duplicated across panels,
3. a device can only be "used through its panel" instead of being a reusable runtime node.

Orchestral needs a runtime layer so that:

- acquisition devices can publish structured data,
- controlled devices can accept structured commands,
- hybrid devices can do both,
- and other modules can interact with devices without scraping UI text or knowing panel internals.

### Why acquisition and control should be separated

Device IO is not one generic blob.

There are two different planes:

1. `data plane`
   - samples, frames, waveforms, state feedback, messages
2. `command plane`
   - connect, disconnect, apply, start, stop, setpoint, trigger, export

These planes interact, but they are not the same thing.

Trying to model both with one vague "IO" abstraction usually leads to unclear ownership and weak semantics.

### Why one live runtime object per active hardware instance

Each active hardware instance should have one in-memory runtime owner while Orchestral is using it.

This runtime owner is called a `device session`.

It is:

- not a UI control,
- not a thread,
- not a global singleton.

It is the ownership boundary for:

- connection handles,
- active settings,
- endpoint state,
- live state,
- output streams,
- accepted commands,
- diagnostics,
- and termination records.

The implementation may internally use:

- async tasks,
- callbacks,
- background threads,
- timers,
- or none of those.

The session object is the architectural concept.
The threading model is an implementation detail.

## Current implementation status

The first runtime-IO implementation pass is now present in code, not only in architecture.

Implemented runtime sessions currently include:

- `IntegratedMicrophoneSession`
- `ControlCenterSession`
- `Pt104Session`
- `IntegratedCameraSession`
- `HuaTengCameraSession`

The current branch also already reflects these architectural consequences:

- panels for those devices are moving to session-client behavior instead of owning hardware loops directly,
- the application host owns session-registry behavior,
- session-local validation now exists as a shared runtime unit with structured `SessionValidationResult` output,
- controlled-device sessions can define device-level `EmergencyStop`,
- acquisition-style panels can expose a small operator-facing output-settings surface,
- and `ApplyAndExit` is treated as a session-lifecycle action rather than only a UI close action.

The current coordinator status is still only foundational:

- `RuntimeCoordinator` now owns truthful run-level `Idle -> Running -> Stopping -> Idle` stop transitions,
- it delegates run-level stop to `IDeviceSessionRegistry.StopAllAsync(...)`,
- and the application host can join an in-flight stop through `EnsureStoppedAsync(...)`,
- but it is not yet the full experiment-level orchestration layer for multiple active sessions.

## How

### How the system should be layered

The runtime stack should be:

1. `hardware adapter`
   - SDK wrapper, serial client, protocol client, OS capture backend
2. `device session`
   - runtime owner for one active device instance
3. `output ports`
   - structured publish surfaces
4. `command ports`
   - structured command-entry surfaces
5. `consumers`
   - panel UI, data display, compare, recording, computation, automation

```mermaid
flowchart LR
    A["Hardware Adapter"] --> B["Device Session"]
    C["Command Port"] --> B
    B --> D["Data Output Port"]
    B --> E["Status Output Port"]
    B --> F["Diagnostics Output Port"]
    B --> G["Applied Settings Output Port"]
    B --> H["Session-End Output Port"]
    D --> I["Integration Panel"]
    D --> J["Data Display / Compare"]
    D --> K["Computation / Recording"]
    C <-- L["Operator / Automation / Runtime Logic"]
```

### How state ownership maps into runtime IO

The three panel state scopes remain valid here:

1. `device scope`
   - shared identity
   - connection state
   - shared diagnostics
   - shared health and interlocks
2. `endpoint scope`
   - one channel, camera stream, axis, actuator lane, sensor, or mode-specific endpoint
   - endpoint settings
   - endpoint live data and history
3. `session scope`
   - live acquisition, read-once, snap, actuation command in progress, export, shutdown

The runtime layer must preserve these scopes instead of flattening them.

Examples:

- PT-104 box = device scope, RTD channel = endpoint scope, live loop = session scope
- microphone = device scope, processing mode if supported = endpoint or mode scope, streaming = session scope
- Arduino Uno R4 = device scope, LED matrix or attached sensor = endpoint scope, active command or polling loop = session scope

### How panels should relate to the runtime

The panel should become a client of the runtime session.

The panel should:

- subscribe to structured outputs,
- send structured commands,
- render truthfully,
- and never be the only place where the real runtime state exists.

This lets a future compare/overlay module consume the same data that the panel sees, without depending on the panel itself.

### How routing should work

The system should maintain a runtime registry or router for active device sessions.

Responsibilities:

- create and dispose sessions,
- find a session by device identity,
- expose the available output ports,
- expose the accepted command surface,
- allow downstream modules to bind by device and endpoint identity.

This can start as an in-process registry.
It does not need to be a distributed message bus.

### How session ownership and lifecycle should work

Session ownership should be explicit.

For the first-generation runtime layer:

- the application host owns the `IDeviceSessionRegistry`,
- panels and other consumers receive sessions through that registry,
- runtime sessions are created by the registry, not by panel-local constructors,
- and stop logic should be able to reach active sessions through the registry.

The recommended ownership model is:

1. `App host`
   - creates and owns the registry
2. `Panel or runtime consumer`
   - requests a session for a specific device identity
3. `Registry`
   - returns the existing session for that device identity, or creates one if needed
4. `RuntimeCoordinator`
   - does not own sessions directly
   - triggers `StopAll()` through the registry as the run-level stop hub
   - exposes a host-safe `EnsureStoppedAsync(...)` path so shutdown can join an in-flight stop instead of bypassing coordinator state

Default first-generation rules:

- one live session per device identity
- no duplicate simultaneous sessions for the same physical device unless that device explicitly supports independent multi-session access
- connect and disconnect within one panel lifetime normally reuse the same session object
- a new session is created only when the host intentionally starts a new device-session lifetime
- controlled-device sessions should define an explicit device-level safe-stop path when the hardware can cause unsafe real-world state

## What

### 1. Core runtime abstractions

The first-generation runtime layer should define:

- `DeviceSessionId`
- `IDeviceSession`
- `ISnapshotOutputPort<T>`
- `IStreamOutputPort<T>`
- `IDeviceCommandPort`
- `IDeviceSessionRegistry`

These names are illustrative.
The important part is the ownership model, not the exact interface names.

### 1.1 Output port delivery contract

The delivery contract must be explicit before the first session is written.

The first-generation recommendation is:

- `ISnapshotOutputPort<T>`
  - exposes the latest snapshot
  - raises a `Changed` event when the snapshot changes
- `IStreamOutputPort<T>`
  - raises a `Produced` event for each new payload item

The first-generation delivery mechanism should be standard .NET events.

Recommended shape:

- snapshot port
  - `T? Current`
  - `event Action<T> Changed`
- stream port
  - `event Action<T> Produced`

Threading contract:

- events are raised on the runtime producer thread or callback thread
- ports do not automatically marshal to the UI thread
- UI-facing consumers must marshal to the dispatcher at the panel-adapter boundary
- non-UI consumers remain free to process events without WPF coupling

Subscription lifecycle:

- consumers must unsubscribe when they are disposed or detached
- sessions must stop raising events after disposal
- panels must subscribe when they bind to a session and unsubscribe when they close or change devices

### 2. Output families

Every device session should be able to expose some subset of:

- `DataOutput`
- `StatusOutput`
- `DiagnosticsOutput`
- `AppliedSettingsOutput`
- `SessionEndOutput`

#### Data output

Examples:

- scalar sample
- waveform window
- image frame
- actuator state feedback
- protocol message

Suggested fields:

- `Timestamp`
- `DeviceId`
- `EndpointId`
- `PayloadType`
- `Payload`
- `Units`
- `SequenceNumber`
- `CaptureRate`
- `SourceMode`

The first-generation operator-facing output-settings surface should stay minimal.

Recommended operator-controlled fields:

- `PayloadType`
- `EmissionMode`
- `OutputFrequency`
- `MetadataIncluded`

Output routing should not be a first-generation panel choice.
The runtime bus should be the default path, with recorder or downstream wiring composed later by coordinator or agent-driven logic.

#### Status output

Suggested fields:

- `Connected`
- `ReadyState`
- `LiveState`
- `FaultState`
- `SelectedEndpoint`
- `BackgroundActiveEndpoints`

#### Diagnostics output

Diagnostics means structured operational evidence, not scientific payload data.

Examples:

- last command sent
- last hardware response
- last error
- dropped frame count
- retry count
- timeout count
- startup-probe result
- normalization or coercion event
- unsupported-mode reason

Suggested fields:

- `LastCommand`
- `LastHardwareResponse`
- `LastError`
- `LastStateTransition`
- `LastValidationResult`

If Orchestral later needs append-only diagnostic history, that should be modeled as a separate `DiagnosticsEvent` stream rather than folded into the diagnostics snapshot output.

#### Applied-settings output

This must record what the runtime actually applied, not only what the operator typed.

Suggested fields:

- `AppliedAt`
- `DeviceSettings`
- `EndpointSettings`
- `SessionSettings`
- `NormalizationNotes`

#### Session-end output

Suggested fields:

- `EndedAt`
- `ExitReason`
- `ConnectionClosed`
- `LiveStopped`
- `AppliedSettingsSnapshot`
- `FinalStatus`
- `OpenIssues`

### 3. Command families

The first-generation runtime layer should support two command groups.

#### Lifecycle commands

These manage session and panel lifecycle:

- `Connect`
- `Disconnect`
- `ApplySettings`
- `ApplyAndExit`
- `CloseWithoutApply`

#### Operational commands

These drive device behavior:

- `ReadOnce`
- `SnapFrame`
- `StartLive`
- `StopLive`
- `SetSetpoint`
- `SendActuatorCommand`
- `EmergencyStop`

Not every device supports every command.
The runtime contract should expose supported commands explicitly.

### 4. Device categories in runtime terms

The runtime should classify devices as:

- `Acquisition`
- `Controlled`
- `Hybrid`

#### Acquisition device

Primary trait:

- produces payload data

Examples:

- PT-104
- integrated microphone
- HuaTeng camera

Runtime emphasis:

- strong data plane
- lighter command plane for acquisition control

#### Controlled device

Primary trait:

- accepts operator or automation commands

Examples:

- stage
- valve
- pump
- serial flow-rate or pump controller

Runtime emphasis:

- strong command plane
- data plane used for acknowledgement, state feedback, and faults

#### Hybrid device

Primary trait:

- both production of meaningful data and meaningful acceptance of commands are first-class

Examples:

- smart motion controller with encoder feedback
- Arduino board hosting both sensor acquisition and actuator control

Runtime emphasis:

- both planes are first-class

Decision rule:

- model the device as one hybrid session when command acceptance and data output share the same connection lifecycle and protocol boundary
- model it as two sessions only when the command path and the acquisition path use physically different communication channels or genuinely independent connection state

### 5. Example A: integrated microphone

Use this as the first acquisition runtime pilot.

#### Why

The microphone is always present and easy to test.

#### How

One microphone device session owns:

- selected microphone identity,
- capture backend,
- current settings,
- current waveform window,
- current RMS output,
- current live state,
- diagnostics such as PCM-only format constraints.

#### What it exposes

- data output:
  - waveform slices
  - RMS dBFS samples
- status output:
  - connected
  - streaming
  - selected mode
- diagnostics output:
  - backend path
  - mode support
  - dropouts
  - callback failures
- command plane:
  - connect
  - disconnect
  - read once
  - start live
  - stop live
  - apply settings

### 6. Example B: control-center / flow-rate controller

Use this as the first real controlled-device runtime pilot.

#### Why

It already exists in the current trusted hardware inventory and proves real command-plane behavior.

#### How

One control-center device session owns:

- device identity and serial transport,
- connection lifecycle,
- current applied command state,
- command acknowledgements,
- fault, safe-stop, and reconnect behavior.

#### What it exposes

- command plane:
  - connect
  - disconnect
  - apply command
  - emergency stop
- data/status plane:
- command result
- applied controller state
- controller health
- transport diagnostics

Device-level `EmergencyStop` should be treated as the base controlled-device safety primitive.
A future system-level `StopAll` should orchestrate over those device-level guarantees rather than replacing them.

### 7. How to treat Arduino with attached sensors or actuators

Arduino should usually be modeled as:

- one shared device at device scope,
- with multiple endpoints behind it.

It is not automatically one archetype by itself.

The correct top-level classification depends on what the active panel or runtime role is:

- sensor-focused usage -> acquisition
- actuator-focused usage -> controlled
- both at once -> hybrid

Tabs or subcontext switching should only be used when the attached endpoints are genuinely channel-like or mode-like.

Do not force every attached function into tabs.

### 8. Adoption order

The safe implementation order is:

1. define the runtime interfaces and registry,
2. pilot one acquisition session using integrated microphone,
3. pilot one controlled session using the turbulence control-center serial device or flow-rate controller path,
4. verify downstream consumers can subscribe without knowing panel internals,
5. then migrate PT-104 and cameras onto the same runtime pattern.

The Arduino example remains useful as a low-cost lab test node and timing/control illustration, but it should not be treated as the primary inventory-backed pilot.

### 9. Success criterion

This architecture is successful when:

- a panel no longer owns the only real device state,
- downstream modules can consume device outputs directly,
- commands can be issued without UI scraping,
- acquisition and control are both modeled truthfully,
- and a new device can plug into the runtime layer using the same conceptual model.
