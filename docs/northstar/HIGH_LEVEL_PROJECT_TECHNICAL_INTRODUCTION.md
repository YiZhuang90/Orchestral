# High-Level Project Technical Introduction

## 1. Project Vision

This project aims to become an AI-native experimental operating system.

The long-term goal is not simply to automate one turbulence-transition experiment, but to build a general system in which:

- hardware can be added as reusable modules,
- experiment logic can be changed without rewriting the whole system,
- the user interface grows from actual experiment needs,
- AI can help users design, configure, test, document, and operate experiments through natural language.

The current turbulence-transition control workflow is treated as the first special case and reference implementation. It provides domain knowledge, device knowledge, data-flow patterns, and operational lessons, but it is not the desired long-term software architecture.

## 2. Core Product Idea

The target system is an **AI-assisted experimental operating system** built around structured experiment definitions, reusable hardware interfaces, protocol-aware device integration, runtime orchestration, schema-driven UI generation, and continuous documentation.

Instead of manually building every new experiment from scripts and ad hoc glue code, the user should be able to describe an experiment conversationally, and the system should help construct:

- the experiment structure,
- the hardware bindings,
- the data-processing pipeline,
- the monitoring and control interface,
- the associated documentation and run data schema.

## 3. Guiding Principles

### 3.1 Workflow-first, not code-first

The platform should be organized around the real workflow of experimental work, not around a specific script layout or one-off control program.

### 3.2 Schema-first, not prompt-first

AI should operate on structured experiment, device, protocol, runtime, and UI definitions. It should not initially be allowed to directly invent arbitrary low-level runtime behavior without validation.

### 3.3 Protocol-aware hardware abstraction

For any device, the communication protocol is a first-class concern and part of the device identity. A device is not defined only by what it does, but also by how it communicates, how timing behaves, what errors it produces, and how it is validated and recovered.

### 3.4 UI should emerge from experiment needs

The UI should not be designed as a fixed generic dashboard first. Instead, it should be generated from:

- experiment structure,
- device capabilities,
- runtime state,
- monitoring requirements.

Custom views can be added later where necessary, but the initial UI should be schema-driven.

### 3.5 Documentation is part of the system

Documentation should be generated continuously during all stages of experiment creation and operation. The platform is expected to build code, UI, and documentation together.

### 3.6 Safety and reproducibility are fundamental

Because this is a real experimental control system, the platform must treat safety, validation, runtime state management, parameter constraints, logging, and reproducibility as core concerns.

## 4. High-Level System Scope

The platform is intended to support:

- multiple device types,
- changing scientific topics,
- changing experimental logic,
- AI-guided experiment construction,
- AI-assisted hardware integration,
- runtime monitoring and control,
- structured documentation and compatible data output.

It is not intended to remain a notebook-centered, hardware-specific, manually wired control system.

## 5. Core Architectural Concepts

The long-term architecture is built around the following primary concepts.

### 5.1 Device

A device is any physical or virtual component that the system can interact with.

Examples include:

- cameras,
- temperature sensors,
- DAQs,
- pumps,
- actuators,
- lasers,
- baths,
- balances,
- simulated devices.

A device should expose:

- metadata,
- capabilities,
- configurable parameters,
- commands,
- output streams,
- health/status,
- safety constraints.

### 5.2 Protocol

Protocol is a key part of device integration and must not be hidden inside implementation details.

Protocol definitions should capture:

- transport type,
- message format,
- encoding,
- polling vs event-driven behavior,
- latency and timing expectations,
- error semantics,
- reconnection behavior,
- device-specific constraints.

Examples of protocol families include:

- SDK-based control,
- serial communication,
- USB APIs,
- TCP/IP protocols,
- Modbus,
- DAQ-specific APIs,
- file- or stream-based interfaces,
- custom binary protocols.

In this system, a device is conceptually:

**hardware identity + protocol + capability contract + runtime driver**

### 5.3 Experiment

An experiment is a structured orchestration of:

- required device roles,
- parameters,
- data flows,
- processing steps,
- control logic,
- trigger logic,
- monitored variables,
- stop conditions,
- outputs.

An experiment should be defined declaratively enough to inspect, validate, serialize, document, and modify through AI assistance.

### 5.4 Runtime

The runtime executes the experiment.

It is responsible for:

- binding device instances to experiment roles,
- managing protocol sessions,
- starting and stopping streams,
- executing processing pipelines,
- running control logic,
- maintaining experiment state,
- handling logging and persistence,
- enforcing lifecycle and safety rules.

### 5.5 Schema-driven UI

The UI should be derived from structured system definitions rather than manually built from scratch for every experiment.

It should reflect:

- experiment parameters,
- device settings,
- runtime state,
- key monitors,
- live data streams,
- control actions such as start and stop.

### 5.6 AI Orchestration Layer

The AI layer sits above the schema and runtime, not inside the low-level control loop.

Its responsibilities should include:

- translating user intent into experiment structure,
- proposing device bindings,
- helping gather and structure protocol information,
- generating testing code and device-specific test UIs,
- generating runtime and UI definitions,
- updating structured configurations,
- generating and updating documentation,
- explaining changes and validating them before execution.

Initially, the AI layer should not be trusted to freely generate unsafe runtime behavior without structured validation.

## 6. Platform Development Workflow

The platform should be built around a typical experimental workflow.

### Stage 1. High-level experiment design

The user defines:

- the scientific purpose,
- the overall experiment logic,
- the components needed,
- the data flow between components,
- the main control objectives,
- the variables to monitor,
- the expected outputs.

At this stage, implementation details such as communication protocols stay in the background.

The output of this stage should include:

- a high-level system schematic,
- a structured experiment definition,
- an initial file/project structure,
- a high-level experiment introduction document.

### Stage 2. Device preparation

The user selects one component from the schematic, such as a camera, and then provides technical information or asks AI to help search for it.

The user also defines:

- what kind of control is needed,
- what parameters matter,
- how the device should be tested,
- what the device output should be.

For each device, AI should help generate:

- protocol-aware device definitions,
- testing code,
- a device-specific testing UI,
- configuration and calibration support,
- a detailed device document including protocol details,
- a validated output contract for downstream integration.

This stage assumes a single central computer to which all devices connect.

### Stage 3. Central orchestration and control build

Once devices are prepared, the user describes:

- how data should flow through the experiment,
- what processing should happen,
- what variables matter most,
- what should be monitored continuously,
- what system-level parameters should be displayed,
- what start and stop should do.

From this, the system should generate:

- runtime control structure,
- processing and collection pipelines,
- monitoring definitions,
- central control UI,
- system-level controls and indicators,
- logging and persistence logic.

The main UI should include, at minimum:

- start control,
- stop control,
- key monitors,
- major system parameters,
- relevant device and experiment state.

### Stage 4. Operation and adjustment

The user should be able to:

- manually adjust parameters,
- ask AI to adjust parameters,
- run the experiment,
- monitor the live system,
- iterate safely.

### Stage 5. Documentation and data system generation

At every stage, the platform should generate proper documentation and maintain a compatible data system.

Examples include:

- high-level experiment introduction from Stage 1,
- per-device documentation with protocol information from Stage 2,
- orchestration and system-level documentation from Stage 3,
- data and run compatibility documentation for operation and analysis.

## 7. Proposed Core Artifacts

The system should revolve around stable, structured artifacts rather than scattered procedural code.

A suitable initial artifact model includes:

- `experiment.yaml`
  High-level experiment logic, roles, data flows, outputs, and monitoring definitions.

- `devices/<role>/device.yaml`
  Concrete hardware binding, capabilities, protocol description, driver backend, and parameter constraints.

- `runtime.yaml`
  Generated runtime specification that describes execution and orchestration.

- `ui.yaml`
  Generated UI definition with optional hand-tuned overrides.

- `docs/`
  Continuously generated project, device, runtime, and operational documentation.

These artifacts allow AI to work on structured definitions instead of blindly editing arbitrary control code.

## 8. Role-Based Hardware Binding

Experiments should define **roles**, not specific models, at the design stage.

Examples:

- `camera_upstream`
- `camera_downstream`
- `temperature_inlet`
- `temperature_outlet`
- `flow_controller`
- `trigger_actuator`

Later, a role is bound to:

- a concrete device,
- a driver/backend,
- a protocol,
- an output contract.

This makes replacement, extension, and reuse possible.

## 9. Capability-Based Design

Experiments should depend on capabilities instead of vendor-specific hardware names.

Examples of capabilities:

- image stream production,
- exposure control,
- ROI control,
- timestamped sampling,
- setpoint control,
- pulse actuation,
- scalar sensor output,
- serial command exchange.

Concrete hardware implements capabilities.
Experiments request capabilities.
This is the basis for long-term generalization.

## 10. Runtime and Data Flow Perspective

The runtime should not be treated as one monolithic script. Conceptually, it should behave like a graph of connected roles and operations.

Example categories in the graph:

- device streams,
- preprocessing blocks,
- feature extraction blocks,
- event detectors,
- controllers,
- actuators,
- loggers,
- visualizers.

This graph-based view is especially useful because both AI and the generated UI can reason about it naturally.

## 11. Documentation Strategy

Documentation should be continuously generated and updated, not deferred until the end.

### Stage 1 documentation

- experiment overview,
- high-level schematic,
- objectives,
- required roles,
- expected outputs.

### Stage 2 documentation

- per-device introduction,
- protocol details,
- test procedures,
- parameter descriptions,
- output contract,
- calibration notes,
- limitations.

### Stage 3 documentation

- orchestration document,
- runtime data-flow description,
- monitoring definition,
- control-loop explanation,
- startup and shutdown behavior,
- UI structure.

### Stage 4 and later documentation

- run metadata schema,
- output file format,
- dataset definition,
- compatibility/version information,
- troubleshooting notes.

## 12. Data System Requirements

The data system must be compatible, structured, and reproducible from the beginning.

Each run should record:

- experiment definition version,
- device binding version,
- protocol version,
- runtime version,
- parameter snapshot,
- metadata,
- time-stamped outputs,
- logs and events,
- derived metrics where applicable.

This is required for reproducibility, traceability, and future AI-assisted analysis.

## 13. What This Means for the Current Project

The current turbulence-transition control system should be treated as:

- the first domain case,
- the first experiment package,
- the first validation target for the abstractions.

It should not define the long-term architecture.

What should be preserved from the current project:

- domain knowledge,
- hardware knowledge,
- timing and synchronization lessons,
- signal-processing knowledge,
- control and monitoring needs,
- expected outputs and analysis patterns.

What should not be treated as the long-term architecture:

- notebook-centered control entrypoints,
- heavy global state,
- ad hoc thread orchestration embedded in experiment code,
- direct coupling between one experiment and one implementation structure.

## 14. Initial Scope Recommendation

The first implementation phase should focus on one narrow but well-designed system:

- one runtime core,
- one device/protocol model,
- one experiment schema,
- one generated UI path,
- one AI-assisted build workflow,
- one canonical turbulence-transition experiment end-to-end.

This keeps the first version grounded in a real scientific use case while preserving the ability to expand later into a universal platform.

## 15. Summary

This project is intended to evolve into an AI-native, workflow-driven, protocol-aware experimental operating system.

Its architecture should be based on:

- structured experiment definitions,
- device and protocol models,
- role-based hardware binding,
- capability-based integration,
- runtime orchestration,
- schema-driven UI,
- AI-assisted generation and modification,
- continuous documentation,
- compatible and reproducible data output.

The current turbulence experiment remains important as the first special case, but the platform should be designed from the perspective of the long-term goal: a reusable system that can grow with new hardware, new experiment types, and new scientific workflows.
