# Turbulence Experiment Flow

## Purpose

This document captures the high-level experiment flow encoded in the legacy pipe-flow system, with emphasis on the looping control path preserved in the first notebook cell.

## Reference Sources

- [Looping_exp_main.ipynb](../../legacy/pipe-flow-reference/notebooks/Looping_exp_main.ipynb)
- [exp_control_functions.py](../../legacy/pipe-flow-reference/python/exp_control_functions.py)
- [PipeFlowComputer.py](../../legacy/pipe-flow-reference/python/PipeFlowComputer.py)

## High-Level Goal

The preserved system controls and monitors a pipe-flow turbulence experiment. In the looping mode, it detects a turbulent structure from camera data and converts that structure into one or more puff-trigger commands downstream or later in the same loop. In the two-point tracking path, it uses two streamwise observation points to classify puff behavior.

## Main Runtime Flow in the Preserved Looping Control Cell

### 1. Parameter assembly

The notebook first assembles grouped parameter bundles for:

- switches,
- camera settings,
- display settings,
- detection settings,
- temperature settings,
- looping settings,
- experiment settings.

This is effectively a primitive artifact system, but it is carried by notebook globals rather than typed schemas.

### 2. Device initialization

The control path then initializes:

- PT-104 temperature acquisition,
- control center serial device,
- laser state via the control center,
- end-height controller,
- optional heat bath,
- all cameras.

### 3. Camera setup

Each camera is configured with:

- exposure time,
- gain,
- trigger mode,
- image resolution,
- ROI metadata for later processing.

The system allocates one queue per camera for display or downstream processing.

### 4. Worker topology

The preserved looping cell starts multiple long-running workers:

- puff trigger control,
- flowrate acquisition,
- temperature acquisition,
- Reynolds-number control,
- optional bath control,
- display,
- turbulence-fraction computation,
- upstream verification path,
- downstream detection path,
- laminar-background computation for each path.

This is the key architectural lesson from the old system: the experiment is already a multi-stream orchestration problem, not a single linear script.

### 5. Image-to-signal path

Within the detection and verification logic, the preserved code follows this broad data flow:

1. grab a frame,
2. crop to ROI,
3. build or refresh a laminar reference field,
4. subtract or normalize against the laminar background,
5. compute a scalar turbulence signal from the ROI,
6. smooth it,
7. evaluate threshold crossings,
8. classify or convert turbulent segments into puff-related events.

In the looping detection path, thresholded signal segments are passed into `trigger_converter(...)`, which produces trigger times.

### 6. Trigger logic

The looping mode supports at least two trigger styles conceptually:

- constant triggering,
- signal-derived triggering.

The preserved `Looping_detection(...)` implementation explicitly handles `trigger_mode == 'constant'`, while also maintaining a waiting list of trigger times for detected structures.

### 7. Flow and Reynolds-number control

The preserved control logic computes flow-derived Reynolds number from:

- measured pulse-derived volume flowrate,
- measured or fallback temperature,
- pipe geometry,
- fluid-property functions in `PipeFlowComputer_FR(...)` and `PipeFlowComputer_Re(...)`.

The `Re_control_center(...)` loop then computes an error against the target Reynolds number and converts that into end-height step commands.

### 8. Temperature control

If enabled, the preserved bath loop tries to reduce inlet/outlet temperature difference by nudging the bath setpoint.

### 9. Run termination

In the preserved looping notebook, the main control thread itself only transitions into cleanup on manual `KeyboardInterrupt`.

Worker-side stop signals do exist:

- some worker loops stop when `exit_flag` is set,
- some acquisition paths are bounded by `max_frame`,
- the display path can set `exit_flag = True`.

But the notebook's main `while True` loop does not check `exit_flag`, so those worker-side stop paths do not automatically drive the whole run into cleanup by themselves.

Once the main thread does enter cleanup, it:

- sets `exit_flag`,
- joins worker threads,
- shuts down cameras,
- terminates the PT-104 logger,
- turns off the laser,
- closes serial devices,
- writes end-of-run outputs.

## Scientific Meaning of the Main Computation

The preserved codebase is trying to measure and control transitional structures in pipe flow by turning high-speed camera image data into reduced turbulence signals. The important scientific pipeline is:

- spatial imaging,
- laminar background estimation,
- turbulence signal extraction,
- threshold-based structure detection,
- timing conversion into actuation,
- comparison against flow and temperature state.

That scientific meaning is more important than the exact thread layout of the old code.

## Migration-Relevant Observations

### What should be preserved

- the experiment roles,
- the image-to-signal pipeline,
- the derived Reynolds-number logic,
- the monitored variables,
- the operator-visible state,
- the final outputs.

### What should not be preserved structurally

- notebook-first orchestration,
- global parameter broadcasting,
- implicit shared mutable state,
- `threading.Timer` as a generic worker model.

### Important trust caveat

The first notebook cell is still the clearest statement of the intended looping experiment flow, but some referenced helper functions no longer match the preserved helper module exactly. Migration should therefore treat this flow as scientifically authoritative but not mechanically trustworthy without reconciliation.
