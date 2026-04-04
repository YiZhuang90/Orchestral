# Safety And Stop Inventory

## Purpose

This document captures how the preserved pipe-flow system stops, what failures it appears to recognize, and what stop conditions are still only implicit.

## Reference Sources

- [Looping_exp_main.ipynb](../../legacy/pipe-flow-reference/notebooks/Looping_exp_main.ipynb)
- [exp_control_functions.py](../../legacy/pipe-flow-reference/python/exp_control_functions.py)

## Explicit Stop Paths Present in the Legacy System

### 1. Manual keyboard interruption

- The main notebook control path waits in a `while True` loop.
- A `KeyboardInterrupt` sets `exit_flag = 1`.
- `send_exit_flag(exit_flag)` propagates the stop flag into worker loops.

This is the clearest operator-driven stop path in the preserved system.

### 2. Loop guards tied to `exit_flag`

Many long-running workers use:

- `while not exit_flag`

as their only stop boundary. This includes:

- laminar background computation,
- temperature acquisition,
- flowrate acquisition,
- Reynolds-number control,
- bath control,
- display loops,
- detection loops.

### 3. Bounded worker loops

Some image-processing loops also stop on finite acquisition limits such as:

- `idx < max_frame`.

This stops those worker loops, but in the preserved looping notebook it does not by itself terminate the main thread and trigger full experiment cleanup.

### 4. Display-driven stop signal

The display path contains an explicit branch that sets `exit_flag = True` and breaks out. This is a worker-side stop signal, but in the preserved looping notebook the main thread still waits in an unconditional loop until manual interruption.

## Explicit Cleanup Actions Present in the Legacy System

The preserved shutdown sequence includes:

- joining worker threads,
- camera uninitialization,
- PT-104 shutdown,
- turning off the laser,
- releasing the control center,
- releasing the end-height controller,
- releasing the bath if it was enabled,
- writing final run outputs.

This is important migration knowledge because stop is not only about halting loops. It also has a required hardware-release order.

## Implicit Safety Logic Present In Control Loops

### 1. Temperature balancing

`temperature_control_center(...)` attempts to reduce inlet/outlet temperature mismatch:

- if `abs(delta_t) > 0.01`, it adjusts bath setpoint by `0.03`.

This is control logic, not a hard stop, but it reflects an operational stability concern.

### 2. Reynolds-number regulation

`Re_control_center(...)` continuously adjusts the end-height controller based on Reynolds-number error.

Again, this is corrective control rather than a stop condition, but it is part of the safety/stability picture.

### 3. Laminar-background recomputation guardrails

The detection path contains logic that rejects problematic recomputed laminar backgrounds based on thresholded differences. This is a local data-quality safeguard, though not a run-level stop.

## Missing Or Weak Stop Conditions

The preserved code shows several areas where Orchestral should introduce stronger runtime stop authority.

### 1. Device disconnect / serial failure stop

The old code assumes:

- serial devices remain available,
- SDK calls keep working,
- the control center continues streaming parseable data.

There is little evidence of hard stop behavior on:

- serial timeout,
- malformed control-center line,
- camera acquisition failure,
- PT-104 read failure.

### 2. Stale data stop

The runtime depends on live updates from:

- cameras,
- flowrate telemetry,
- temperature telemetry.

The preserved code does not appear to define a formal stale-data timeout that stops the experiment automatically.

### 3. Unsafe-control-output stop

The old system computes:

- actuator step commands,
- bath setpoint changes,
- trigger times,

but it does not show a centralized safety layer checking those outputs against global experiment constraints before acting.

### 4. Exception handling is coarse

Several control regions use broad `except:` handlers that print messages such as:

- `error in threading`
- `Couldn't stop all threads.`

This is a warning sign. Failure is noticed, but not turned into a structured stop reason.

## Migration Requirements For Orchestral

The new runtime should make the following stop conditions first-class:

- manual stop,
- run-length stop,
- device disconnect,
- protocol parse failure,
- stale telemetry,
- camera acquisition failure,
- invalid control output,
- experiment-specific scientific stop conditions,
- shutdown failure reporting.

## Important Legacy Conclusion

The old system has a usable shutdown habit, but not a strong runtime stop model. Orchestral should preserve the hardware-release sequencing while replacing the stop model with explicit runtime-owned stop conditions and reasons.
