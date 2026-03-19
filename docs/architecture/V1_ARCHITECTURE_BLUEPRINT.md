# V1 Architecture Blueprint

## 1. Purpose of This Document

This document defines the first concrete architecture for the project described in [HIGH_LEVEL_PROJECT_TECHNICAL_INTRODUCTION.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/northstar/HIGH_LEVEL_PROJECT_TECHNICAL_INTRODUCTION.md).

It is intended to serve as the first implementation blueprint for:

- an AI-native experimental operating system,
- using the current turbulence-transition control problem as the first reference experiment,
- while keeping the architecture general enough to support future hardware and future experiment domains.

This document is not a refactor plan for the current codebase. It is a clean-slate architecture plan.

## 2. V1 Objective

The goal of V1 is to prove the platform architecture on one real experiment.

V1 should support:

- one central computer,
- multiple connected devices,
- protocol-aware device integration,
- one canonical experiment definition,
- one execution runtime,
- one generated control UI,
- one AI-assisted experiment-building workflow,
- structured documentation generation,
- structured run logging and data output.

V1 should not attempt to solve every future use case immediately.

## 3. Architectural Position

This system should be built as a layered platform:

```text
+---------------------------------------------------------------+
|                        User / Operator                        |
+---------------------------------------------------------------+
|                    AI Builder / Assistant                     |
+---------------------------------------------------------------+
|                 Experiment Design + UI Schema                 |
+---------------------------------------------------------------+
|                      Runtime Orchestrator                     |
+---------------------------------------------------------------+
|      Devices      |    Processing    |   Logging / Data       |
+---------------------------------------------------------------+
|                    Protocol / Driver Layer                    |
+---------------------------------------------------------------+
|                     Physical Hardware Layer                   |
+---------------------------------------------------------------+
```

The current turbulence experiment belongs at the experiment package level, not at the system-core level.

## 4. Core Design Principles

### 4.1 Workflow-first

The platform follows the lifecycle of experimental work:

1. define the experiment,
2. prepare each device,
3. build orchestration,
4. operate the experiment,
5. document and store outputs.

### 4.2 Artifact-first

Every meaningful stage should produce explicit structured artifacts.

Examples:

- experiment definition,
- device definition,
- protocol definition,
- runtime specification,
- UI definition,
- run manifest,
- documentation outputs.

### 4.3 Protocol-aware hardware integration

Protocol is a first-class architectural concept. It is not an implementation detail.

### 4.4 Capability-driven binding

Experiments should depend on capabilities and device roles, not directly on concrete hardware models.

### 4.5 Schema-driven UI

The UI should emerge from experiment needs and structured definitions rather than being fully hand-coded first.

### 4.6 AI as a structured co-builder

AI should generate and modify artifacts, docs, UI, and scaffolds in a controlled way. It should not initially have unconstrained authority over unsafe low-level runtime logic.

## 5. V1 System Scope

### In scope

- single-machine architecture,
- local hardware connections,
- plugin-based device support,
- protocol modeling,
- experiment graph definition,
- runtime execution engine,
- generated device testing UI,
- generated experiment control UI,
- documentation generation,
- structured run logging,
- turbulence-transition experiment as first full case.

### Out of scope for V1

- distributed multi-computer orchestration,
- autonomous AI-written runtime code with no human review,
- full visual no-code editor,
- cloud-native orchestration,
- multi-lab deployment infrastructure,
- universal support for every hardware family.

## 6. Primary Architectural Objects

The platform should revolve around seven primary object types.

### 6.1 DeviceRole

Represents what the experiment needs, not which concrete hardware is used.

Examples:

- `camera_upstream`
- `camera_downstream`
- `temperature_inlet`
- `temperature_outlet`
- `flow_measurement`
- `trigger_actuator`
- `system_indicator`

### 6.2 Device

Represents a concrete hardware instance bound to a role.

Fields:

- id
- vendor
- model
- serial number or unique identity
- protocol reference
- driver reference
- capability set
- parameter schema
- health schema

### 6.3 Protocol

Represents how a device communicates.

Fields:

- transport type
- session rules
- command format
- response format
- polling or event-driven mode
- timing expectations
- reconnection strategy
- failure semantics
- safety constraints

### 6.4 Capability

Represents what a device can do or provide.

Examples:

- frame stream
- scalar sample stream
- exposure setting
- ROI setting
- trigger control
- setpoint control
- pulse actuation
- state reporting

### 6.5 ExperimentDefinition

Represents the scientific workflow and system-level data/control structure.

Fields:

- purpose
- roles
- experiment parameters
- data flow graph
- monitors
- control loops
- triggers
- stop conditions
- output definitions

### 6.6 RuntimeSpec

Represents the executable form of an experiment.

Fields:

- bound roles and devices
- instantiated nodes
- pipeline topology
- task topology
- event channels
- logging plan
- UI state channels

### 6.7 RunRecord

Represents one executed experiment run.

Fields:

- run id
- timestamps
- experiment version
- device bindings
- protocol versions
- runtime version
- parameter snapshot
- event log
- output file references
- derived metrics

## 7. Suggested Repository Structure

The first clean repository layout should look like this:

```text
project-root/
|
+-- docs/
|   +-- northstar/
|   |   +-- HIGH_LEVEL_PROJECT_TECHNICAL_INTRODUCTION.md
|   |   +-- V1_ARCHITECTURE_BLUEPRINT.md
|   +-- experiments/
|   +-- devices/
|   +-- runtime/
|   +-- protocols/
|
+-- schemas/
|   +-- experiment.schema.yaml
|   +-- device.schema.yaml
|   +-- protocol.schema.yaml
|   +-- runtime.schema.yaml
|   +-- ui.schema.yaml
|   +-- run.schema.yaml
|
+-- core/
|   +-- models/
|   +-- validation/
|   +-- registry/
|   +-- artifacts/
|
+-- protocols/
|   +-- serial/
|   +-- sdk/
|   +-- tcp/
|   +-- modbus/
|   +-- custom/
|
+-- devices/
|   +-- mindvision_camera/
|   +-- pico_temperature/
|   +-- serial_flow_controller/
|   +-- trigger_actuator/
|   +-- bath_controller/
|   +-- ...
|
+-- runtime/
|   +-- orchestrator/
|   +-- execution/
|   +-- state/
|   +-- event_bus/
|   +-- logging/
|   +-- safety/
|
+-- ui/
|   +-- generated/
|   +-- custom/
|   +-- widgets/
|   +-- device_panels/
|   +-- run_console/
|
+-- ai/
|   +-- planners/
|   +-- builders/
|   +-- prompts/
|   +-- artifact_editors/
|   +-- validators/
|
+-- experiments/
|   +-- turbulence_transition/
|   |   +-- experiment.yaml
|   |   +-- ui.yaml
|   |   +-- docs/
|   |   +-- tests/
|   |   +-- templates/
|   +-- ...
|
+-- runs/
|   +-- <experiment-slug>/
|       +-- <run-id>/
|           +-- manifest.yaml
|           +-- outputs/
|           +-- logs/
|           +-- derived/
|
+-- tests/
|   +-- unit/
|   +-- integration/
|   +-- hardware_sim/
|   +-- workflow/
|
+-- tools/
|   +-- generators/
|   +-- docs/
|   +-- migration/
|   +-- simulation/
|
+-- app/
|   +-- main.py
|   +-- bootstrap.py
```

This is a conceptual layout. Exact naming can change, but the separation should remain.

## 8. Artifact Model

V1 should define stable artifact types.

## 8.1 `experiment.yaml`

Represents the high-level experiment.

Example shape:

```yaml
id: turbulence_transition_v1
title: Turbulence Transition Puff Tracking
purpose: Measure and control transitional puff behavior using imaging and flow control

roles:
  - id: camera_upstream
    requires: [frame_stream, exposure_control, roi_control]
  - id: camera_downstream
    requires: [frame_stream, exposure_control, roi_control]
  - id: temperature_inlet
    requires: [scalar_sample_stream]
  - id: temperature_outlet
    requires: [scalar_sample_stream]
  - id: flow_measurement
    requires: [scalar_sample_stream]
  - id: trigger_actuator
    requires: [pulse_actuation]

parameters:
  re_target:
    type: float
    unit: dimensionless
  threshold:
    type: float
  exposure_ms_p1:
    type: float
    unit: ms
  exposure_ms_p2:
    type: float
    unit: ms

graph:
  nodes: []
  edges: []

monitors:
  - re_live
  - temperature_delta
  - p1_signal
  - p2_signal
  - puff_classification

outputs:
  - run_manifest
  - signal_timeseries
  - device_logs
  - derived_metrics
```

## 8.2 `device.yaml`

Represents a concrete device binding.

Example shape:

```yaml
role: camera_upstream
device:
  vendor: MindVision
  model: <model-name>
  identity: <serial-or-uid>

protocol:
  kind: sdk
  implementation: mindvision_sdk

driver:
  plugin: mindvision_camera
  version: 1

capabilities:
  - frame_stream
  - exposure_control
  - gain_control
  - roi_control

parameters:
  exposure_ms:
    type: float
    range: [0.1, 10.0]
  gain:
    type: float
  roi:
    type: rect

expected_output:
  stream: image_frame
  timestamped: true
```

## 8.3 `protocol.yaml`

Represents communication semantics.

Example shape:

```yaml
id: serial_ascii_v1
transport: serial
encoding: ascii
mode: request_response

connection:
  baudrate: 9600
  timeout_ms: 2000

messages:
  command_terminator: "\n"
  response_terminator: "\n"

behavior:
  retry_count: 3
  reconnect_on_failure: true
  pollable: true

timing:
  expected_latency_ms: 20
  max_safe_command_rate_hz: 10
```

## 8.4 `runtime.yaml`

Represents how the experiment is executed.

This should be generated, not primarily hand-authored.

## 8.5 `ui.yaml`

Represents generated UI panels and bindings.

## 8.6 `manifest.yaml`

Represents one run snapshot.

## 9. Capability Model

Capabilities are the universal integration language of the platform.

Suggested V1 capability classes:

- `frame_stream`
- `scalar_sample_stream`
- `vector_sample_stream`
- `exposure_control`
- `gain_control`
- `roi_control`
- `setpoint_control`
- `pulse_actuation`
- `binary_state_control`
- `status_report`
- `health_check`

Each capability should define:

- input parameters,
- output types,
- update model,
- rate behavior,
- validation rules.

## 10. Protocol Model

Protocol must be explicitly modeled because it governs implementation and reliability.

V1 protocol classes:

- `sdk`
- `serial_ascii`
- `serial_binary`
- `tcp_ascii`
- `tcp_binary`
- `modbus`
- `daq_api`
- `usb_api`

For every protocol, the system should document:

- connection lifecycle,
- handshake requirements,
- command/response semantics,
- timing model,
- failure states,
- recovery path.

## 11. Plugin Architecture

V1 should support plugins in four areas:

### 11.1 Device plugins

Each plugin implements:

- device discovery or manual binding,
- capability advertisement,
- protocol session handling,
- parameter translation,
- command execution,
- output stream production,
- health checks.

### 11.2 Protocol adapters

Shared protocol handlers should be reusable across devices.

### 11.3 Processing plugins

Examples:

- frame preprocessing,
- background subtraction,
- feature extraction,
- event detection,
- synchronization correction,
- control-law computation.

### 11.4 UI plugins

Examples:

- image display panel,
- scalar trend panel,
- device test panel,
- system monitor card,
- parameter editor panel.

## 12. Runtime Architecture

The runtime should have the following major subsystems.

## 12.1 Device Manager

Responsibilities:

- instantiate devices,
- open and close connections,
- validate health,
- expose streams and commands,
- maintain per-device state.

## 12.2 Protocol Session Manager

Responsibilities:

- manage low-level communication sessions,
- retry and reconnect,
- normalize transport errors,
- expose timing and health metadata.

## 12.3 Experiment Orchestrator

Responsibilities:

- read experiment and runtime specs,
- bind roles to devices,
- instantiate nodes,
- connect data paths,
- start and stop the system in the right order.

## 12.4 Execution Engine

Responsibilities:

- run stream consumers and producers,
- run processing pipelines,
- run controllers,
- enforce scheduling mode.

V1 execution can use threads or async tasks, but that decision should be internal to the engine, not hard-coded into the experiment definitions.

## 12.5 State Manager

Responsibilities:

- maintain experiment state,
- maintain run state,
- surface current values to UI,
- surface health and fault state.

## 12.6 Event Bus

Responsibilities:

- publish device events,
- publish runtime events,
- publish alarms,
- allow UI and logger subscriptions.

## 12.7 Logging and Persistence Manager

Responsibilities:

- write raw streams,
- write event logs,
- write derived data,
- write run manifest,
- maintain compatibility across runs.

## 12.8 Safety Manager

Responsibilities:

- startup interlocks,
- shutdown ordering,
- hard parameter limits,
- command-rate limits,
- emergency stop handling,
- invalid-state prevention.

## 13. Runtime Lifecycle

The V1 runtime lifecycle should be:

```text
1. Load experiment definition
2. Load device bindings
3. Validate compatibility
4. Generate runtime spec
5. Generate UI spec
6. Initialize devices
7. Initialize protocol sessions
8. Run device self-checks
9. Enter READY state
10. User presses START
11. Start streams
12. Start pipelines
13. Start controllers
14. Start logging
15. Run until stop condition or user stop
16. Safe shutdown
17. Finalize run manifest
18. Generate summary artifacts
```

## 14. State Model

The system should expose explicit top-level states:

- `DESIGNING`
- `DEVICE_PREPARATION`
- `READY`
- `RUNNING`
- `PAUSED`
- `STOPPING`
- `COMPLETED`
- `FAILED`
- `EMERGENCY_STOP`

Each device should also have local states:

- `UNBOUND`
- `BOUND`
- `INITIALIZING`
- `READY`
- `STREAMING`
- `ERROR`
- `DISCONNECTED`

## 15. UI Architecture

V1 UI should be composed from structured definitions.

### 15.1 UI layers

- generated base UI from schema,
- custom panels for domain-specific needs,
- runtime overlays for warnings, alarms, and state.

### 15.2 V1 UI categories

**Experiment Designer UI**
- role editor
- parameter editor
- schematic view
- output definition view

**Device Workbench UI**
- connection panel
- protocol panel
- command test panel
- live output preview
- parameter tuning panel

**Run Console UI**
- start/stop control
- live monitors
- device status cards
- alarms and warnings
- logs and metrics

### 15.3 UI generation rule

Default UI should come from:

- parameter schemas,
- capability schemas,
- monitor definitions,
- runtime state definitions.

This keeps the UI aligned with experiment needs.

## 16. AI Assistant Boundary

The AI layer should have explicit responsibilities and limits.

### AI should be allowed to:

- generate initial experiment definitions,
- propose role and capability structure,
- help gather device and protocol information,
- generate device test code,
- generate device-preparation UI,
- generate runtime and UI specs,
- explain and edit parameters,
- generate documentation,
- generate run summaries.

### AI should not initially be allowed to:

- execute unsafe hardware commands without confirmation,
- bypass validation gates,
- write arbitrary low-level runtime behavior directly into the live system without review,
- modify safety rules implicitly.

### AI interaction model

The user should be able to say things like:

- "I need two timestamped cameras and one trigger actuator."
- "Build a test panel for this serial temperature sensor."
- "The output of this device should be a timestamped scalar temperature stream."
- "Show me the central monitors for this experiment."
- "Add Reynolds number as a system-level parameter."

AI should translate that into artifact updates and generated code/UI/docs.

## 17. Documentation Architecture

Docs should be generated as part of the lifecycle.

### Required generated documents in V1

**Project level**
- experiment introduction
- system overview
- architecture summary

**Device level**
- hardware summary
- protocol specification
- connection instructions
- test instructions
- output contract

**Runtime level**
- orchestration summary
- pipeline description
- control summary
- monitor summary
- startup/shutdown procedure

**Run level**
- run manifest
- parameter snapshot
- data inventory
- run notes

## 18. Data Architecture

The platform should define three levels of data.

### 18.1 Definition-time data

- experiment definitions
- device definitions
- protocol definitions
- UI definitions

### 18.2 Run-time data

- live streams
- system state
- alarms
- events
- command history

### 18.3 Post-run data

- raw outputs
- processed outputs
- derived summaries
- logs
- compatibility metadata

### Suggested run folder structure

```text
runs/<experiment-id>/<run-id>/
|
+-- manifest.yaml
+-- experiment_snapshot.yaml
+-- device_snapshot.yaml
+-- runtime_snapshot.yaml
+-- ui_snapshot.yaml
+-- raw/
+-- processed/
+-- derived/
+-- logs/
+-- notes/
```

## 19. Validation Gates

The platform should validate at every stage.

### Stage 1 validation

- experiment roles are coherent,
- required outputs are defined,
- graph is structurally valid.

### Stage 2 validation

- protocol definition is complete,
- device can connect,
- device test passes,
- output contract is satisfied.

### Stage 3 validation

- role bindings satisfy experiment requirements,
- runtime graph is executable,
- monitors are bound,
- logging outputs are defined.

### Stage 4 validation

- parameters are in safe range,
- all required devices are healthy,
- startup ordering is safe.

### Stage 5 validation

- run manifest is complete,
- output schema is valid,
- docs are synchronized with artifacts.

## 20. Turbulence Experiment as the First Reference Case

The turbulence-transition experiment should be used to validate the abstractions.

### V1 reference roles

- `camera_upstream`
- `camera_downstream`
- `temperature_inlet`
- `temperature_outlet`
- `flow_measurement`
- `trigger_actuator`
- `flow_control_output`

### V1 reference capabilities

- frame stream acquisition
- ROI parameterization
- exposure and gain control
- timestamped scalar temperature acquisition
- timestamped scalar flow measurement
- trigger pulse actuation
- control-loop setpoint management

### V1 reference processing pipeline

```text
camera frame
  -> ROI crop
  -> laminar/background normalization
  -> scalar signal extraction
  -> filtering
  -> event detection
  -> cross-location comparison
  -> puff classification
  -> runtime monitors
  -> logging
```

### V1 reference system monitors

- Reynolds number
- temperature mean
- temperature delta
- camera stream health
- P1 signal
- P2 signal
- event classification
- actuator activity
- device health summary

### V1 reference outputs

- run manifest
- signal timeseries
- derived event summaries
- device logs
- monitor history

## 21. Suggested Technology Direction for V1

This document stays architecture-focused, but V1 should follow these implementation constraints:

- typed schemas,
- plugin-friendly architecture,
- desktop-first local runtime,
- strong local file-based artifacts,
- generated UI from definitions,
- safe validation layer before execution.

Exact framework choices can be made in a later implementation plan.

## 22. V1 Build Order

This is the recommended implementation order.

### Phase 1. Foundations

- create artifact schemas,
- create core model layer,
- create registry and validation system,
- create project/document generators.

### Phase 2. Protocol and device framework

- implement protocol base interfaces,
- implement device plugin base interfaces,
- build first protocol-aware device workbench,
- support one or two concrete device types first.

### Phase 3. Runtime framework

- implement event bus,
- implement orchestrator,
- implement execution engine,
- implement state manager,
- implement logger and run manifest system.

### Phase 4. Generated UI

- implement schema-driven parameter UI,
- implement device testing UI,
- implement run console UI,
- implement monitor binding.

### Phase 5. AI artifact builder

- implement AI prompt-to-artifact flow,
- implement artifact edit and diff review,
- implement doc generation flow,
- implement safe parameter modification flow.

### Phase 6. Turbulence reference package

- implement turbulence experiment definition,
- bind real devices,
- implement turbulence processing plugins,
- validate end-to-end experiment run.

## 23. What Success Looks Like for V1

V1 is successful if a user can:

1. define the high-level turbulence experiment in structured form,
2. bind and test each device through a generated device UI,
3. have the platform generate a central control UI from experiment needs,
4. start and stop the experiment safely,
5. monitor key system variables live,
6. save structured outputs and documentation automatically,
7. ask AI to make controlled changes to the experiment setup.

That is enough to prove the architecture.

## 24. Final Position

V1 should not be treated as a cleaned-up version of the old notebook workflow.

It should be treated as:

- the first implementation of a new platform,
- using the turbulence experiment as the first full-stack validation case,
- with architecture centered on artifacts, workflows, protocol-aware devices, runtime orchestration, generated UI, and AI-assisted construction.

This is the shortest path toward the long-term goal while still staying grounded in a real experiment.
