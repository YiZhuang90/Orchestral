# Orchestral System Language Spec

## 1. Purpose

This document defines the core language of Orchestral V1. It is the semantic layer used by experiment definitions, device bindings, runtime generation, and run manifests.

The goal is not to define a visual editor format or a generic plugin framework. The goal is to define stable nouns and their relationships so the platform can validate, generate, and execute experiments consistently.

## 2. Design Rules

- The language is artifact-first and schema-backed.
- Experiment definitions describe intent and structure, not driver internals.
- Device roles describe needs; devices satisfy those needs.
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
- the device roles required,
- experiment-level parameters,
- stream inputs and derived streams,
- transforms,
- monitors,
- stop conditions,
- outputs.

An experiment does not bind to specific hardware directly. It names what the system needs and what should happen when the experiment runs.

### 3.2 Device Role

A device role is a functional requirement inside an experiment.

A role says what kind of device is needed, such as:

- upstream camera,
- downstream camera,
- actuator,
- temperature sensor.

A role is abstract. It does not identify a vendor, model, or serial number. A role can require one or more capabilities and may constrain protocol family or output shape.

### 3.3 Device

A device is a concrete hardware or simulated instance that can be bound to a role.

A device contains:

- identity,
- protocol binding,
- capability set,
- configurable parameters,
- health status.

A device may satisfy one role in one experiment run and a different role in another run, provided its capabilities and protocol semantics are compatible.

### 3.4 Role Binding

Role binding is the explicit association of a device role with a concrete device for a specific run.

Role binding is the bridge between the abstract experiment and the concrete hardware. A binding resolves:

- which device instance fills the role,
- which protocol instance or protocol configuration is used,
- which role-required capabilities are satisfied by the device,
- which device and role parameters are active for the run.

A binding is specific to one run context. It is part of the resolved runtime state and part of the run manifest.

### 3.5 Protocol

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

### 3.6 Capability

A capability is an operation or data service that a device can provide.

Capability identifiers are canonical machine names used in experiment definitions, bindings, and validation. They should use stable lowercase `snake_case` names such as `frame_stream`, `scalar_sample_stream`, and `pulse_actuation`.

Human-facing prose names may be longer or more descriptive, but they should map directly to a canonical capability identifier.

Examples:

- frame stream production,
- scalar temperature sampling,
- setpoint control,
- pulse actuation,
- status reporting,
- exposure control.

Capabilities are the main compatibility boundary between an experiment role and a concrete device. The experiment asks for capabilities; the device advertises capabilities.

### 3.7 Parameter

A parameter is a named value that configures an experiment, a device binding, a transform, a monitor, or a stop condition.

Parameters have explicit metadata such as:

- type,
- unit,
- range or allowed set,
- default value,
- validation rule,
- scope.

Parameters are not free-form runtime variables. They are declared in the experiment language so they can be validated, surfaced in UI, and recorded in the run manifest.

### 3.8 Stream

A stream is a time-varying sequence of values or events produced by a device, transform, or monitor.

Streams may represent:

- image frames,
- scalar samples,
- event markers,
- derived metrics,
- status updates.

A stream has a producer, a schema, and a time basis. It may be consumed by transforms, monitors, logging, or UI components.

### 3.9 Transform

A transform converts one or more input streams into one or more output streams.

Transforms are used for:

- filtering,
- normalization,
- feature extraction,
- aggregation,
- conversion from raw device output to experiment-level signals.

Transforms are part of the experiment runtime model. They are not the same as device capabilities because they operate on data already inside the experiment graph.

### 3.10 Monitor

A monitor is a named runtime observation or summary of an experiment condition or derived value.

Monitors are used to:

- display live state,
- summarize system health,
- show derived metrics,
- support operator judgment,
- feed stop conditions when needed.

A monitor may observe raw streams or transformed streams. In V1, a monitor is not assumed to be a stream by default.

Monitors have three permitted semantic shapes:

- display-only: presents live values or state without producing a new stream,
- derived-state: maintains a computed summary object that can be queried by the UI or runtime,
- signal-producing: explicitly publishes a named output stream, which must be declared separately.

If a monitor produces time-varying data, that output must be named as a stream rather than implied by the monitor name alone.

### 3.11 Stop Condition

A stop condition is a rule that ends or pauses a run when a condition is met.

Stop conditions may be:

- time-based,
- threshold-based,
- state-based,
- safety-based,
- operator-request-based.

Stop conditions are system-level rules. They are checked by the runtime, and when satisfied they produce a stop reason that becomes part of the run manifest.

### 3.12 Output

An output is a declared artifact or data product produced by a run.

Outputs may include:

- run manifest,
- raw streams,
- processed streams,
- derived summaries,
- logs,
- exported figures or tables.

Outputs are declared in the experiment so the runtime knows what to persist and the user knows what to expect.

### 3.13 Run Manifest

A run manifest is the authoritative record of one execution of an experiment.

It records:

- experiment identity and version,
- role-to-device bindings,
- protocol versions or protocol identifiers,
- parameter values used for the run,
- start and stop timestamps,
- activated stop condition and stop reason,
- output references,
- key runtime events,
- warnings or faults.

The run manifest is not a full data lake. It is the compact, structured summary that ties the experiment definition to the actual execution.

## 4. Semantics and Relationships

The language should be read as a graph of constrained relationships:

- an experiment requires one or more device roles,
- a role requires one or more capabilities,
- a device provides capabilities through a protocol,
- a device binding connects a device to a role,
- parameters configure experiment behavior and device behavior,
- streams carry data through the runtime,
- transforms derive new streams from existing streams,
- monitors report important runtime values,
- stop conditions observe monitors and streams,
- outputs capture the resulting artifacts,
- the run manifest records the resolved configuration and outcome.

The platform should validate these relationships before a run starts.

## 5. Minimal Role Binding Semantics

Role binding should be strict enough to be safe, but not so strict that the language becomes brittle.

At minimum, a binding should verify:

- the device advertises the capabilities the role requires,
- the protocol is compatible with the role's expected communication style,
- required parameters are present and valid,
- the device can produce or accept the stream types the experiment expects.

The language should not require the experiment definition to know vendor names, driver classes, or low-level implementation details.

A binding should be represented explicitly in the resolved run state and in the run manifest, so a run can be replayed or audited without reconstructing the mapping from logs.

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

### 6.8 Transforms

- `upstream_roi_crop` consumes `camera_upstream.frames` and produces `upstream_roi.frames`.
- `downstream_roi_crop` consumes `camera_downstream.frames` and produces `downstream_roi.frames`.
- `temperature_smoother` consumes `temperature_inlet.samples` and produces `temperature_inlet.smoothed`.
- `turbulence_indicator` consumes both ROI streams and produces `puff_indicator`.

### 6.9 Monitors

- `live_reynolds_number` shows the current target and resolved operating condition.
- `temperature_monitor` shows the current inlet temperature and trend.
- `puff_monitor` shows the derived turbulence indicator.
- `actuator_status_monitor` shows whether the actuator is armed, active, or idle.

### 6.10 Stop condition

System-level stop condition: stop the run if `temperature_inlet.samples` exceeds `temperature_limit_c` for longer than the configured debounce window.

This is a system-level stop condition because it protects the entire run, not just one device.

### 6.11 Outputs

- `run_manifest`
- `raw_camera_streams`
- `temperature_timeseries`
- `derived_turbulence_indicator`
- `runtime_logs`

### 6.12 Example binding sketch

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
  camera_exposure_ms:
    type: float
    unit: ms
  actuator_pulse_ms:
    type: float
    unit: ms
  temperature_limit_c:
    type: float
    unit: C

monitors:
  - id: temperature_monitor
    source: temperature_inlet.samples
  - id: puff_monitor
    source: puff_indicator

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
