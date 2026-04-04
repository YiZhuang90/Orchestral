# V1 Functional Unit Roadmap

## Superseded

This document has been superseded by [ROADMAP_V2.md](../development_v2/ROADMAP_V2.md). Kept for historical reference only. Most units listed here are now built.

---

## Purpose

This document identifies the functional units Orchestral still needs in order to become a real experiment platform rather than only a device-panel and session-runtime prototype.

It is grounded in:

- [2026-03-28-reviewer-system-comments.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/development/2026-03-28-reviewer-system-comments.md)
- [DEVICE_RUNTIME_IO_ARCHITECTURE.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/architecture/DEVICE_RUNTIME_IO_ARCHITECTURE.md)
- [DEVICE_INVENTORY.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/legacy-knowledge/DEVICE_INVENTORY.md)
- [PROTOCOL_INVENTORY.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/legacy-knowledge/PROTOCOL_INVENTORY.md)
- [DATA_OUTPUT_INVENTORY.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/legacy-knowledge/DATA_OUTPUT_INVENTORY.md)
- [SAFETY_AND_STOP_INVENTORY.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/legacy-knowledge/SAFETY_AND_STOP_INVENTORY.md)
- [TURBULENCE_EXPERIMENT_FLOW.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/legacy-knowledge/TURBULENCE_EXPERIMENT_FLOW.md)

The goal is to answer:

- which units are already built enough,
- which units are still missing,
- which units should be built next,
- and which units should stay deferred until the core experiment path is real.

## Status Labels

- `Built`: present and usable as a real system unit
- `Partial`: significant foundation exists, but the unit is not yet complete enough to rely on as a product-level capability
- `Missing`: not yet built as a coherent unit
- `Deferred`: important later, but should not be built before higher-priority units

---

## 1. Summary View

| Priority | Functional Unit                                      | Status   | Why it matters next                                                                                                                                       |
| -------- | ---------------------------------------------------- | -------- | --------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 1        | Device session foundation                            | Partial  | Sessions, ports, and registry exist, but they still need validation and experiment-level composition above them                                           |
| 2        | Operator runtime controls                            | Partial  | Output settings, apply/exit, and device-level safety controls now exist, but they are still per-panel rather than experiment-level                        |
| 3        | Session-local validation                             | Missing  | Prevents contradictory per-device settings before apply                                                                                                   |
| 4        | Runtime coordinator                                  | Missing  | Needed to manage multiple sessions as one run                                                                                                             |
| 5        | Stop authority and shutdown sequencer                | Missing  | Needed to replace the weak legacy stop model with explicit runtime-owned stop reasons and hardware release order                                          |
| 6        | High-rate stream buffering and delivery policy       | Missing  | Needed because camera and signal streams are too important and too fast for naive panel-local event handling                                              |
| 7        | Experiment definition and role binding               | Missing  | Needed to replace notebook globals with typed experiment setup                                                                                            |
| 8        | Run context and metadata unit                        | Missing  | Needed for run identity, operator intent, and experiment log state                                                                                        |
| 9        | Controller unit session                              | Partial  | First control-target artifacts and runtime session now exist, but broader measured-state and experiment-specific controller projection still sit above it |
| 10       | Run recorder and artifact writer                     | Missing  | Needed to reproduce the real scientific outputs of the legacy system                                                                                      |
| 11       | Run monitor and alarm surface                        | Missing  | Needed so operator and coordinator can see experiment truth at run level through a background monitor and dedicated experiment panel                      |
| 12       | Experiment Logic layer                               | Missing  | Needed so experiment-specific scientific meaning stops being treated as universal runtime work                                                            |
| 13       | Control-center capability decomposition              | Partial  | The control-center session exists, but laser, puff, and flow telemetry still need cleaner endpoint/capability structure                                   |
| 14       | Camera-pair role unit                                | Missing  | This is experiment logic because two cameras together carry experiment-specific meaning                                                                   |
| 15       | Flow/Reynolds derived-state unit                     | Built    | This is experiment logic because it derives a scientific state from experiment-specific telemetry and geometry                                            |
| 16       | Image-to-signal processing pipeline                  | Missing  | This is experiment logic because it turns frames into experiment-specific turbulence signals                                                              |
| 17       | Detection / classification / trigger-conversion unit | Missing  | This is experiment logic because it converts processed signals into puff events and trigger timing                                                        |
| 18       | Bath controller session                              | Deferred | Important only if thermal control is part of the first claimed experiment slice                                                                           |
| 19       | Cross-session validation                             | Deferred | Important later, but should follow session-local validation and coordinator behavior                                                                      |
| 20       | Experiment-definition linting                        | Deferred | Valuable later, but depends on real experiment definitions first                                                                                          |
| 21       | Virtual device and replay/simulation harness         | Deferred | High-value hardening tool and the preferred closure path for device integration, but not the next blocking unit                                           |
| 22       | First reference experiment package                   | Missing  | The real integrative milestone for V1                                                                                                                     |
| 23       | AI brain integration                                 | Deferred | Must remain after the runtime and reference experiment are real                                                                                           |

---

## 2. Immediate Units To Build Next

These are the units that should come next because they unlock the experiment plane above the already-built session layer.

### 2.1 Session-Local Validation

Status:

- `Missing`

Why it is next:

- the reviewer comments are right that settings can contradict each other
- the current runtime can apply settings, but it cannot yet systematically reject invalid combinations before apply
- this is the cheapest safe next abstraction because it stays local to one device session

Expected capability:

- `ValidateSettings(settings) -> ValidationResult`
- per-device rules such as:
  - invalid frequency for periodic output
  - incompatible device mode combinations
  - out-of-range session settings

Examples:

- microphone periodic output frequency higher than meaningful update rate
- camera frame-rate request inconsistent with trigger mode
- PT-104 channel configuration that does not make sense for the active measurement mode

### 2.2 Runtime Coordinator

Status:

- `Missing`

Why it is next:

- the current runtime proves one-device truth
- the turbulence experiment is a multi-device orchestration problem
- the system needs one unit that manages active sessions as one run

Expected capability:

- enumerate active sessions
- start/stop groups of sessions
- implement `StopAll`
- own run-level state
- provide one integration point for later automation and AI

Examples:

- stop both cameras, PT-104, and controller sessions together
- mark the run as `Armed`, `Running`, `Stopping`, `Stopped`
- publish one run-level snapshot rather than forcing the UI to infer state from several panels

### 2.3 Stop Authority and Shutdown Sequencer

Status:

- `Missing`

Why it is next:

- [SAFETY_AND_STOP_INVENTORY.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/legacy-knowledge/SAFETY_AND_STOP_INVENTORY.md) shows the old system had cleanup habits but weak stop authority
- device-level disconnect exists, but the system still lacks structured run-level stop reasons and hardware-release ordering

Expected capability:

- explicit stop reasons
- manual stop
- stale-data stop
- device-failure stop
- protocol-parse-failure stop
- ordered shutdown sequencing
- shutdown-failure reporting

Examples:

- camera stream goes stale -> run stop reason becomes `CameraStale`
- control-center serial parse fails repeatedly -> coordinator stops the run
- stop order shuts down actuation before passive sensors

### 2.4 High-Rate Stream Buffering and Delivery Policy

Status:

- `Missing`

Why it is next:

- the legacy system uses high-rate imaging and derived signal paths
- naive direct event delivery is not enough for display, recording, and processing all at once
- this is the hidden unit most likely to hurt correctness later if ignored now

Expected capability:

- buffering policy
- decimation policy
- latest-only vs full-stream handling
- drop policy under load
- consumer-specific rate handling

Examples:

- panel display consumes 20 Hz while recorder writes all frames
- computation pipeline consumes a decimated but time-ordered signal stream
- overload policy preserves scientific recording while reducing UI rate

### 2.5 Experiment Definition and Role Binding

Status:

- `Missing`

Why it is next:

- the legacy notebook used grouped parameter bundles and implicit globals
- Orchestral needs a typed replacement before the reference experiment can be real

Expected capability:

- device-role assignment
- experiment-level parameters
- geometry and offsets
- thresholds and processing settings
- run-level toggles

Examples:

- assign one camera as `upstream verification`
- assign one camera as `downstream detection`
- bind PT-104 channels `[2,4]` as inlet/outlet temperature
- bind the experiment-specific control actuator when the chosen experiment requires one

### 2.6 Run Context and Metadata Unit

Status:

- `Missing`

Why it is next:

- run identity and operator intent should not live only in filenames or ad hoc notes
- the future recorder, report generation, and AI layer all need a stable run context

Expected capability:

- run ID
- experiment ID
- operator/session metadata
- applied device settings references
- start/end timestamps
- decision-log references

Examples:

- run `2026-03-28-001`
- operator note: `looping mode, RR=0.36`
- links to the exact applied settings snapshots for both cameras and PT-104

---

## 3. Experiment-Logic Units

These are the units that belong above the universal runtime foundation and carry experiment-specific scientific meaning.

They are still required for the first reference experiment, but they should not be planned as if they were generic cross-project runtime primitives.

### 3.1 Experiment Logic Layer

Status:

- `Missing`

Why it matters:

- the current runtime foundation is now strong enough that the next missing work is no longer "one more universal runtime unit"
- units such as camera-pair semantics, Reynolds derivation, and puff detection depend on experiment-specific meaning
- without an explicit layer boundary, the runtime architecture will keep absorbing experiment-specific assumptions

Expected capability:

- formal boundary between universal runtime and experiment-specific scientific logic
- planning guidance for future experiment-specific slices
- architectural rule that experiment logic consumes runtime outputs rather than raw hardware APIs

### 3.2 Control-Center Capability Decomposition

Status:

- `Partial`

Why it matters:

- the control-center device is currently modeled as one session
- the legacy system uses it as:
  - laser controller
  - puff actuator
  - flow telemetry source
- those are related, but not the same capability

Expected capability:

- clearer endpoint/capability structure inside the session
- cleaner outputs and command semantics

### 3.3 Experiment-Specific Controller Sessions

Status:

- `Deferred`

Why it matters:

- some experiments will require extra controller sessions beyond the universal runtime foundation
- those sessions should be introduced by experiment-specific plans, not treated as universal V1 units
- the legacy turbulence setup includes an end-height controller, but that does not make an end-height session a universal next slice for Orchestral

### 3.4 Bath Controller Session

Status:

- `Deferred`

Why it matters:

- useful for thermal-control paths
- not mandatory for the smallest first reference experiment if thermal control is intentionally excluded

### 3.5 Camera-Pair Role Unit

Status:

- `Missing`

Why it matters:

- the legacy experiment does not just use two cameras independently
- it uses them as coordinated streamwise roles
- this should therefore be treated as experiment logic, not universal runtime

Expected capability:

- upstream/downstream semantics
- point-1/point-2 semantics
- paired trigger/timing interpretation

### 3.6 Flow/Reynolds Derived-State Unit

Status:

- `Built`

Why it matters:

- Reynolds number is not raw hardware data
- it is derived from pulse-derived flowrate, temperature, geometry, and fluid properties
- this derived state is central to control and analysis
- this is experiment logic even if it executes on top of the runtime layer

Current note:

- the current branch already includes a first real `FlowReynoldsDerivedStateSession`
- what remains is broader experiment-logic composition around that derived state, not the absence of the unit itself

### 3.7 Image-to-Signal Processing Pipeline

Status:

- `Missing`

Why it matters:

- the experiment’s scientific value comes from turning frames into reduced turbulence signals
- session-layer imaging alone is not enough

Expected capability:

- ROI crop
- laminar reference estimation
- normalization/subtraction
- smoothing
- scalar turbulence signal extraction

### 3.8 Detection / Classification / Trigger Conversion

Status:

- `Missing`

Why it matters:

- the old system converts thresholded turbulence signals into puff-related events and trigger times
- this is experiment logic, not generic device logic

Expected capability:

- threshold crossing
- event segmentation
- puff classification
- trigger-time generation

### 3.9 Controller Unit Session

Status:

- `Partial`

Why it matters:

- controlled-device sessions exist, but they are still transport/device units rather than experiment-level controller logic
- a constant target can still require closed-loop regulation, so control semantics cannot be reduced to "locked vs changing"
- the platform needs one runtime-owned unit that consumes measured experiment state and drives command-capable sessions

Expected capability:

- one or more named control targets
- independent `setpoint profile` and `regulation mode`
- measured-state input from streams or derived-state units
- command output to controlled-device sessions
- limits and degraded/fault behavior

Recommended first-generation model:

- `SetpointProfile`
  - `Constant`
  - `Scheduled`
- `RegulationMode`
  - `OpenLoop`
  - `ClosedLoop`

Notes:

- the data model should support multiple control targets
- the first monitor/panel can still emphasize one primary target
- control strategy should be chosen during experiment building, not ad hoc at initialize time
- the current branch now includes:
  - `ControlTargetDefinition`
  - resolved-experiment validation for control targets
  - `ControllerUnitSession`
  - constant/scheduled target evaluation
  - open-loop/closed-loop decision semantics
- remaining work:
  - runtime-bus wiring from measured streams
  - monitor-surface integration
  - experiment-specific actuator projection where needed

### 3.10 Run Monitor and Alarm Surface

Status:

- `Missing`

Why it matters:

- operator and coordinator both need experiment-level truth
- per-panel status is not enough once multiple sessions are active

Expected capability:

- background `ExperimentMonitorSession` that starts once the experiment is initialized
- monitor consumes runtime streams and snapshots rather than reading raw device APIs directly
- experiment monitor panel using the general panel visual style
- left-side run control area for lifecycle actions such as initialize, start, and stop
- live information surface fed by monitor outputs
- bottom status bar
- live health overview
- stale-data warnings
- active alarms and stop reasons
- run-level derived-state visibility

Display guidance:

- the monitor surface may expose display-rate choices such as `5` to `30 fps`
- display decimation must be UI-only and must not affect acquisition, detection, recorder fidelity, or alarm evaluation

Primary controlled-variable guidance:

- the first monitor surface should show the primary target in `Target +/- Error` form
- example:
  - `Re 1600 +/- 12`

---

## 4. Later-System Units

These are important, but should follow the previous units rather than precede them.

### 4.1 Cross-Session Validation

Status:

- `Deferred`

Why later:

- it depends on real coordinator behavior and real experiment definitions
- build session-local validation first

### 4.2 Experiment-Definition Linting

Status:

- `Deferred`

Why later:

- static linting is only valuable after experiment-definition artifacts become real and stable enough to check

### 4.3 Virtual Device And Replay/Simulation Harness

Status:

- `Deferred`

Why later:

- high-value hardening tool
- preferred final closure path for device integration once the real path works
- especially useful before broad generalization
- but not the next blocking unit for the first integrated experiment slice

### 4.4 AI Brain Integration

Status:

- `Deferred`

Why later:

- the AI layer should sit above:
  - stable sessions
  - stable coordinator
  - stable experiment artifacts
  - stable review and approval workflow

---

## 5. Recommended Build Order

The next practical build order should be:

1. session-local validation
2. runtime coordinator
3. stop authority and shutdown sequencer
4. experiment definition and role binding
5. high-rate stream buffering and delivery policy
6. run context and metadata unit
7. controller unit session
8. run recorder and artifact writer
9. run monitor and alarm surface
10. experiment logic layer
11. control-center capability decomposition
12. flow/Reynolds derived-state unit
13. camera-pair role unit
14. image-to-signal processing pipeline
15. detection / classification / trigger conversion
16. optional experiment-specific controller sessions
17. optional bath controller session
18. cross-session validation
19. experiment-definition linting
20. virtual device and replay/simulation harness
21. first reference experiment package hardening
22. AI brain integration

---

## 6. Main Conclusion

Orchestral has already crossed the boundary from panel-only thinking into real device-session thinking.

The next missing layer is not "more device panels." It is first the experiment plane above the sessions, and then the experiment-logic layer above that reusable runtime:

- run coordination,
- stop authority,
- experiment definition,
- run artifact production,
- derived scientific state,
- processing and trigger logic,
- and other experiment-specific composite meanings.

Until those units exist, Orchestral remains a strong device-runtime prototype.
Once the runtime foundation and experiment-logic layer are both real, it becomes a real experiment platform.
