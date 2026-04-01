# Orchestral System Language Spec

## 1. Purpose

This document defines the core language of Orchestral V1. It is the semantic layer used by experiment definitions, device bindings, runtime generation, and run manifests.

The goal is not to define a visual editor format or a generic plugin framework. The goal is to define stable nouns and their relationships so the platform can validate, generate, and execute experiments consistently.

The experiment canvas may later present these nouns visually, but this document remains the semantic source of truth rather than a UI-layout specification.

## 2. Design Rules

- The language is artifact-first and schema-backed.
- Experiment definitions describe intent and structure, not driver internals.
- Experiment roles describe needs; concrete implementations satisfy those needs.
- Protocols describe communication semantics.
- Capabilities describe what a device can provide or accept.
- Capability names are canonical identifiers, not prose labels.
- Parameters are explicit and validated.
- Streams carry time-varying values or events.
- Transforms and monitors are runtime logic attached to the experiment, not to ad hoc code paths.
- Stop conditions are first-class safety and termination rules.
- Outputs are declared, not implied.
- A run manifest records what actually happened in one execution.

## 3. Core Terms

### 3.1 Experiment

An experiment is the top-level scientific workflow definition.

An experiment defines:

- the purpose of the run,
- the experiment roles required,
- experiment-level parameters,
- control targets,
- stream inputs and derived streams,
- transforms,
- monitors,
- stop conditions,
- outputs.

An experiment does not bind to specific hardware directly. It names what the system needs and what should happen when the experiment runs.

### 3.2 Experiment Function

An experiment function is a high-level scientific or system job inside an experiment.

Examples:

- downstream puff detection,
- upstream verification,
- Reynolds regulation,
- temperature monitoring,
- run recording.

An experiment function is broader than one experiment role.
It may include acquisition, processing, detection, control, and output responsibilities together.

In V1 canvas terms, one experiment function block may contain multiple internal experiment roles.

In the future experiment canvas, top-level blocks should map to experiment functions rather than to vendor devices.

### 3.2.1 Experiment Function Class

Experiment function class is the top-level classification of an experiment function block in the canvas.

The first canonical function classes should be:

- `condition_control`
- `core_experiment_function`
- `order_parameter`
- `data_recording`

In V1, this should be treated as a closed set of canonical top-level function classes.

These classes mean:

- `condition_control`
  - controls the environment or operating condition of the experiment
- `core_experiment_function`
  - performs the main active scientific function of the experiment
- `order_parameter`
  - measures or derives the main scientific outcome of the experiment
- `data_recording`
  - preserves raw and derived evidence for the run

Not every experiment must contain all four classes.

These classes are block-level classifications.
They are not experiment roles, device types, or source modes.
They are primarily a design/build organization for the canvas rather than a required runtime execution partition.

Monitor surfaces and runtime lifecycle concerns should not be classified as top-level experiment-function classes.

Calibration or reference logic should usually be nested under one of these top-level classes rather than promoted to top level by default.
Promotion should happen only when that calibration/reference work has its own independent schedule, outputs, or operator-visible identity in the experiment design.

### 3.2.2 Typed Canvas Block Kind

Typed canvas block kind is the lower-level composition classification used inside or below an experiment function block.

The first canonical lower-level kinds should be:

- `FunctionBlock`
- `PipelineBlock`
- `ComputeBlock`
- `ControlBlock`
- `BindingBlock`

These kinds mean:

- `FunctionBlock`
  - a meaningful subsystem with a clear responsibility, inputs, and outputs
- `PipelineBlock`
  - an ordered processing path where stage order is semantically important
- `ComputeBlock`
  - a derivation or calculation block that transforms inputs into outputs without directly owning hardware actuation
- `ControlBlock`
  - a decision or command block that turns targets, measurements, rules, or events into commands or gated decisions
- `BindingBlock`
  - a leaf block that connects an abstract role, port, or edge to a concrete implementation, often in `Real`, `Virtual`, `Replay`, or `Synthetic` mode

These lower-level kinds are composition vocabulary for design-time structure.
They should not be read as a promise that the runtime will instantiate one hard execution container per canvas block.

Top-level experiment-function blocks should be treated as `FunctionBlock` instances with an added `Experiment Function Class`.

### 3.3 Experiment Role

An experiment role is a functional requirement inside an experiment.

A role says what kind of device is needed, such as:

- upstream camera,
- downstream camera,
- actuator,
- temperature sensor.

A role is abstract. It does not identify a vendor, model, or serial number. A role can require one or more capabilities and may constrain protocol family or output shape.

### 3.4 Concrete Implementation

A concrete implementation is the real or virtual implementation bound to an experiment role.

Examples:

- a real `HuaTengCamera`,
- a real `PT104`,
- a virtual control-center service,
- a replay-backed stream source,
- a synthetic scalar source.

Concrete implementation sits below experiment-role meaning and above the transport or runtime-service details that make execution possible.

A concrete implementation is the run-bound realization of a role.
It may wrap a device, but replay and synthetic implementations are not themselves devices.

### 3.5 Device

A device is the identifiable hardware endpoint or device-shaped twin that exposes protocol and capability semantics to Orchestral.

A device contains:

- identity,
- protocol binding,
- capability set,
- configurable parameters,
- health status.

A device may back one concrete implementation in one run and a different one in another run, provided its capabilities and protocol semantics are compatible.

Replay and synthetic sources are concrete implementations, but they are not devices.

### 3.6 Source Mode

Source mode describes how a concrete implementation is currently realized.

The first canonical modes should be:

- `Real`
- `Virtual`
- `Replay`
- `Synthetic`

These modes are important because one experiment function or role may stay stable while the bound concrete implementation changes source mode.

Source mode is selected by experiment-building or binding surfaces and then becomes a property of the resolved concrete implementation for that run.

### 3.7 Role Binding

Role binding is the explicit association of an experiment role with a concrete implementation for a specific run.

Role binding is the bridge between the abstract experiment and the concrete execution path. A binding resolves:

- which concrete implementation fills the role,
- which device, replay source, or synthetic source stands behind that implementation,
- which protocol instance or protocol configuration is used when relevant,
- which role-required capabilities are satisfied,
- which source mode is active,
- and which implementation and role parameters are active for the run.

A binding is specific to one run context. It is part of the resolved runtime state and part of the run manifest.

In V1 canvas authoring, one experiment role should bind to exactly one active concrete implementation at a time.

At design time, that intended binding may be authored through a canvas block or other experiment-building surface.
At run time, the resolved run manifest is the authoritative record of the actual binding used for execution.

### 3.8 Protocol

A protocol is the communication contract used to interact with a device.

A protocol describes:

- transport family,
- message style,
- connection lifecycle,
- timing expectations,
- failure behavior,
- reconnection or retry expectations,
- command and response semantics.

Protocol answers "how do we talk to this device?" Capability answers "what can we ask it to do?"

### 3.9 Capability

A capability is an operation or data service that a device can provide.

Capability identifiers are canonical machine names used in experiment definitions, bindings, and validation. They should use stable lowercase `snake_case` names such as `frame_stream`, `scalar_sample_stream`, `pulse_actuation`, `laser_control`, and `flow_telemetry`.

Human-facing prose names may be longer or more descriptive, but they should map directly to a canonical capability identifier.

Examples:

- frame stream production,
- scalar temperature sampling,
- setpoint control,
- pulse actuation,
- status reporting,
- exposure control.

Capabilities are the main compatibility boundary between an experiment role and a concrete device. The experiment asks for capabilities; the device advertises capabilities.

### 3.10 Parameter

A parameter is a named value that configures an experiment, a device binding, a transform, a monitor, or a stop condition.

Parameters have explicit metadata such as:

- type,
- unit,
- range or allowed set,
- default value,
- validation rule,
- scope.

Parameters are not free-form runtime variables. They are declared in the experiment language so they can be validated, surfaced in UI, and recorded in the run manifest.

### 3.11 Stream

A stream is a time-varying sequence of values or events produced by a device, transform, or monitor.

Streams may represent:

- image frames,
- scalar samples,
- event markers,
- derived metrics,
- status updates.

A stream has a producer, a schema, and a time basis. It may be consumed by transforms, monitors, logging, or UI components.

### 3.12 Transform

A transform converts one or more input streams into one or more output streams.

Transforms are used for:

- filtering,
- normalization,
- feature extraction,
- aggregation,
- conversion from raw device output to experiment-level signals.

Transforms are part of the experiment runtime model. They are not the same as device capabilities because they operate on data already inside the experiment graph.

### 3.13 Monitor

A monitor is a named runtime observation or summary of an experiment condition or derived value.

Monitors are used to:

- display live state,
- summarize system health,
- show derived metrics,
- show control-tracking state,
- support operator judgment,
- feed stop conditions when needed.

A monitor may observe raw streams or transformed streams. In V1, a monitor is not assumed to be a stream by default.

Monitors have three permitted semantic shapes:

- display-only: presents live values or state without producing a new stream,
- derived-state: maintains a computed summary object that can be queried by the UI or runtime,
- signal-producing: explicitly publishes a named output stream, which must be declared separately.

If a monitor produces time-varying data, that output must be named as a stream rather than implied by the monitor name alone.

For a primary controlled variable, a monitor may present live tracking state in `Target +/- Error` form, such as `Re 1600 +/- 12`.

Display-rate decimation is a UI concern only. A monitor surface may render at a lower display rate without changing the underlying stream, recorder fidelity, or alarm evaluation.

### 3.14 Control Target

A control target is a named experiment-level declaration that says:

- which variable is being controlled,
- which measured stream provides the observed value,
- which command-capable role or runtime unit receives control output,
- how the target evolves over run time,
- and how regulation should be performed.

Control targets belong to the experiment definition and resolved runtime state.
They should be chosen during experiment building, not improvised during initialize-time operator interaction.

At minimum, a control target should declare two independent axes:

- `setpoint profile`
  - `constant`
  - `scheduled`
- `regulation mode`
  - `open_loop`
  - `closed_loop`

This distinction matters because a constant target may still require closed-loop regulation, and a scheduled target may still be executed either open-loop or closed-loop.

The system language should allow multiple control targets in one experiment, even if the first-generation monitor or panel emphasizes one primary target.

If a derived quantity is used as the measured input for control, it must be surfaced as a named stream before a control target references it. Monitor identities are not valid control-target measured sources.

### 3.15 Stop Condition

A stop condition is a rule that ends or pauses a run when a condition is met.

Stop conditions may be:

- time-based,
- threshold-based,
- state-based,
- safety-based,
- operator-request-based.

Stop conditions are system-level rules. They are checked by the runtime, and when satisfied they produce a stop reason that becomes part of the run manifest.

### 3.16 Output

An output is a declared artifact or data product produced by a run.

Outputs may include:

- run manifest,
- raw streams,
- processed streams,
- derived summaries,
- logs,
- exported figures or tables.

Outputs are declared in the experiment so the runtime knows what to persist and the user knows what to expect.

### 3.17 Run Manifest

A run manifest is the authoritative record of one execution of an experiment.

It records:

- experiment identity and version,
- role-to-device bindings,
- protocol versions or protocol identifiers,
- parameter values used for the run,
- start and stop timestamps,
- activated stop condition and stop reason,
- output references,
- file-backed artifact-path references when the run is materialized as a local artifact directory,
- key runtime events,
- warnings or faults.

The run manifest is not a full data lake. It is the compact, structured summary that ties the experiment definition to the actual execution.

### 3.18 Experiment Logic

Experiment logic is the experiment-specific runtime meaning layered on top of the universal runtime foundation.

It is where the platform expresses things such as:

- composite role meaning,
- derived scientific state,
- experiment-specific transforms,
- experiment-specific detectors,
- experiment-specific control policies,
- experiment-specific monitor rules.

Examples:

- an upstream/downstream camera pair,
- Reynolds-number derivation from pulse and temperature telemetry,
- turbulence-signal extraction from image streams,
- puff-event classification and trigger conversion.

Experiment logic is not the same thing as raw runtime infrastructure.

The runtime foundation answers questions like:

- how are sessions owned?
- how are streams published?
- how is a run started, stopped, and recorded?

Experiment logic answers questions like:

- what do these device streams mean for this experiment?
- which devices together form one experiment role composite?
- what derived state matters scientifically?
- what experiment-specific monitor or control behavior should exist?

If a unit depends on one experiment's scientific meaning rather than remaining reusable across many experiments, it should be classified as experiment logic rather than universal runtime.

### 3.19 Function Readiness

Function readiness is the coarse canvas-level readiness state of one experiment function block.

It is not the same thing as live runtime health.

The first canonical readiness states should be:

- `Ready`
- `Caution`
- `Blocked`

The intended visual encoding is:

- `Green` for `Ready`
- `Yellow` for `Caution`
- `Red` for `Blocked`

Examples:

- `Ready`
  - all required roles are bound and valid for intended execution
- `Caution`
  - runnable only through virtual, replay, or synthetic source modes
- `Blocked`
  - required role or implementation missing, or blocking validation still present

The intended source mode should be recorded by the experiment-building surface so this comparison is explicit rather than guessed at run time.

## 4. Semantics and Relationships

The language should be read as a graph of constrained relationships:

- an experiment may be described in terms of one or more experiment functions,
- each experiment function may be classified as a top-level function class for canvas organization,
- an experiment function expands into one or more experiment roles and logic responsibilities,
- an experiment requires one or more experiment roles,
- a role requires one or more capabilities,
- a concrete implementation may run in real, virtual, replay, or synthetic source mode,
- a device provides capabilities through a protocol,
- a role binding connects an experiment role to a concrete implementation,
- parameters configure experiment behavior and device behavior,
- control targets define how measured experiment state should drive commands,
- streams carry data through the runtime,
- transforms derive new streams from existing streams,
- experiment logic composes streams, transforms, monitors, and control targets into experiment-specific meaning,
- monitors report important runtime values,
- stop conditions observe monitors and streams,
- outputs capture the resulting artifacts,
- the run manifest records the resolved configuration and outcome.

The platform should validate these relationships before a run starts.

In V1, this validation now has at least three distinct layers:

- experiment-definition linting on the authored package for static structural mistakes,
- binding validation on the resolved package for concrete role, protocol, capability, and parameter mismatches,
- cross-session validation for reusable contradictions across bound roles and control targets.

In V1, reusable cross-session validation should at least catch:

- ambiguous capability ownership when one concrete device is bound to multiple roles and the same capability is claimed by more than one role binding,
- conflicting control-target ownership when more than one control target tries to command the same role.

## 5. Minimal Role Binding Semantics

Role binding should be strict enough to be safe, but not so strict that the language becomes brittle.

At minimum, a binding should verify:

- the device advertises the capabilities the role requires,
- the protocol is compatible with the role's expected communication style,
- required parameters are present and valid,
- the device can produce or accept the stream types the experiment expects.

The language should not require the experiment definition to know vendor names, driver classes, or low-level implementation details.

A binding should be represented explicitly in the resolved run state and in the run manifest, so a run can be replayed or audited without reconstructing the mapping from logs.

When a canvas is present, it should use these nouns in this order:

1. experiment function
2. experiment role
3. concrete implementation

That preserves high-level user intent while still allowing the runtime to bind down to concrete devices or virtual sources.

## 6. Turbulence Example

The example below is a compact end-to-end sketch of the binding layer and key runtime declarations for a turbulence-control slice. It is not a full file format.

### 6.1 Experiment summary

Experiment: `turbulence_transition_v1`

Purpose: observe and control a turbulence-transition setup using two cameras, one actuator, and one temperature sensor, while enforcing a system-level stop condition.

### 6.2 Roles

- `camera_upstream`: captures the upstream flow region.
- `camera_downstream`: captures the downstream flow region.
- `flow_actuator`: issues control pulses to the actuator hardware.
- `temperature_inlet`: reports inlet temperature as a scalar stream.

### 6.3 Required capabilities

- `camera_upstream` requires `frame_stream`, `exposure_control`, `roi_control`.
- `camera_downstream` requires `frame_stream`, `exposure_control`, `roi_control`.
- `flow_actuator` requires `pulse_actuation` and `status_report`.
- `temperature_inlet` requires `scalar_sample_stream` and `health_check`.

Combined devices may expose multiple runtime capability surfaces even when the transport protocol is shared.
For example, the control-center serial device should be modeled in runtime as separate `laser_control`, `pulse_actuation`, and `flow_telemetry` capability surfaces above one mixed serial command/readback protocol.

### 6.4 Devices and protocols

- `camera_01` is a concrete device bound through `sdk_camera_v1`.
- `camera_02` is a concrete device bound through `sdk_camera_v1`.
- `actuator_01` is a concrete device bound through `serial_ascii_v1`.
- `temp_sensor_01` is a concrete device bound through `serial_ascii_v1`.

### 6.5 Role bindings

- `camera_upstream` binds to `camera_01`.
- `camera_downstream` binds to `camera_02`.
- `flow_actuator` binds to `actuator_01`.
- `temperature_inlet` binds to `temp_sensor_01`.

### 6.6 Parameters

- `re_target`: target Reynolds number for the run.
- `camera_exposure_ms`: shared exposure setting for both cameras.
- `actuator_pulse_ms`: pulse duration for the actuator.
- `temperature_limit_c`: safety ceiling for inlet temperature.

### 6.7 Streams

- `camera_upstream.frames`: raw image stream from the upstream camera.
- `camera_downstream.frames`: raw image stream from the downstream camera.
- `temperature_inlet.samples`: scalar temperature stream.
- `flow_actuator.state`: actuator status stream.
- `flowrate_lpm`: derived flow-rate stream from pulse telemetry.
- `reynolds_number`: derived experiment-control stream used by the primary Reynolds target.
- `temperature_mean_c`: derived mean-temperature stream for flow-property correction.
- `temperature_delta_c`: derived temperature-spread stream for monitor and artifact context.

### 6.8 Control Targets

- `re_control` uses the derived Reynolds-number stream as its measured value.
- its `setpoint_profile` may be `constant` or `scheduled`.
- its `regulation_mode` is `closed_loop` when the platform actively holds Reynolds number near target.

### 6.9 Transforms

- `upstream_roi_crop` consumes `camera_upstream.frames` and produces `upstream_roi.frames`.
- `downstream_roi_crop` consumes `camera_downstream.frames` and produces `downstream_roi.frames`.
- `temperature_smoother` consumes `temperature_inlet.samples` and produces `temperature_inlet.smoothed`.
- `flow_reynolds_derivation` consumes pulse telemetry plus available PT-104 temperature samples and produces:
  - `flowrate_lpm`
  - `reynolds_number`
  - `temperature_mean_c`
  - `temperature_delta_c`
- `turbulence_indicator` consumes both ROI streams and produces `puff_indicator`.

### 6.10 Monitors

- `live_reynolds_number` shows the current target and tracking error, for example `Target Re +/- error`.
- `derived_flow_state` shows the latest derived Reynolds/flow/temperature summary and stale-state status.
- `temperature_monitor` shows the current inlet temperature and trend.
- `puff_monitor` shows the derived turbulence indicator.
- `actuator_status_monitor` shows whether the actuator is armed, active, or idle.

### 6.11 Stop condition

System-level stop condition: stop the run if `temperature_inlet.samples` exceeds `temperature_limit_c` for longer than the configured debounce window.

This is a system-level stop condition because it protects the entire run, not just one device.

### 6.12 Outputs

- `run_manifest`
- `raw_camera_streams`
- `temperature_timeseries`
- `derived_turbulence_indicator`
- `runtime_logs`

### 6.13 Example binding sketch

```yaml
experiment:
  id: turbulence_transition_v1
  purpose: Observe and control turbulence transition with live safety monitoring

roles:
  - id: camera_upstream
    requires: [frame_stream, exposure_control, roi_control]
  - id: camera_downstream
    requires: [frame_stream, exposure_control, roi_control]
  - id: flow_actuator
    requires: [pulse_actuation, status_report]
  - id: temperature_inlet
    requires: [scalar_sample_stream, health_check]

devices:
  - id: camera_01
    protocol: sdk_camera_v1
    capabilities: [frame_stream, exposure_control, roi_control]
  - id: camera_02
    protocol: sdk_camera_v1
    capabilities: [frame_stream, exposure_control, roi_control]
  - id: actuator_01
    protocol: serial_ascii_v1
    capabilities: [pulse_actuation, status_report]
  - id: temp_sensor_01
    protocol: serial_ascii_v1
    capabilities: [scalar_sample_stream, health_check]

bindings:
  - role: camera_upstream
    device: camera_01
  - role: camera_downstream
    device: camera_02
  - role: flow_actuator
    device: actuator_01
  - role: temperature_inlet
    device: temp_sensor_01

parameters:
  re_target:
    type: float
    unit: dimensionless
  re_schedule:
    type: time_series
    unit: dimensionless
  camera_exposure_ms:
    type: float
    unit: ms
  actuator_pulse_ms:
    type: float
    unit: ms
  temperature_limit_c:
    type: float
    unit: C

control_targets:
  - id: re_control
    measured_source: reynolds_number
    command_role: flow_actuator
    setpoint_profile: constant
    regulation_mode: closed_loop
    target_parameter: re_target

monitors:
  - id: temperature_monitor
    source: temperature_inlet.samples
  - id: puff_monitor
    source: puff_indicator
  - id: live_reynolds_number
    source: reynolds_number
    display: target_plus_minus_error

stop_conditions:
  - id: stop_on_high_temperature
    type: threshold
    source: temperature_inlet.samples
    operator: gt
    threshold: temperature_limit_c
    debounce_ms: 500

outputs:
  - run_manifest
  - raw_camera_streams
  - temperature_timeseries
  - derived_turbulence_indicator
  - runtime_logs
```

## 7. Non-Goals for This Spec

This spec intentionally does not define:

- visual editor syntax,
- distributed orchestration concepts,
- fully generic plugin packaging rules,
- implementation details for specific device drivers,
- storage engine internals,
- UI layout grammar.

Those topics belong in later implementation docs.

## 8. Practical Reading Rule

If a future artifact, runtime component, or UI view uses one of the nouns above, it should preserve the meaning defined here.

If a proposed concept does not fit one of these nouns, it should be introduced only if there is a clear need and a clear semantic boundary.
