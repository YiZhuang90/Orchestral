# Experiment Canvas Block Contract

## Purpose

This document defines the V1 contract for one experiment-canvas block in Orchestral.

The goal is to keep the canvas high-level, legible, and scientifically trustworthy.

This document does not define exact UI visuals.
It defines what one block means and what one block is allowed to contain.

Read this together with:

- [EXPERIMENT_CANVAS_ARCHITECTURE.md](./EXPERIMENT_CANVAS_ARCHITECTURE.md)
- [EXPERIMENT_CANVAS_TYPED_BLOCK_KINDS.md](./EXPERIMENT_CANVAS_TYPED_BLOCK_KINDS.md)
- [SYSTEM_LANGUAGE_SPEC.md](./SYSTEM_LANGUAGE_SPEC.md)

## Core Rule

One V1 canvas block represents one `Experiment Function`.

Examples:

- `Downstream Puff Detection`
- `Reynolds Regulation`
- `Temperature Monitoring`
- `Run Recording`

A block is therefore broader than one device and broader than one experiment role.

## V1 Block Structure

Each block should contain these conceptual parts:

1. function identity
2. function contract
3. internal experiment roles
4. bound concrete implementations
5. readiness state

### 1. Function Identity

Each block should have:

- function id
- function class
- function name
- short purpose summary

For V1, `function class` should be one of:

- `condition_control`
- `core_experiment_function`
- `order_parameter`
- `data_recording`

In V1, this should be treated as a closed set of canonical top-level function classes.

The summary should answer:

- what does this function do for the experiment?

`Function class` is a classification of the experiment function block itself.
It is not a runtime role, not a device class, and not a source mode.
Its canonical meanings are defined in [SYSTEM_LANGUAGE_SPEC.md](./SYSTEM_LANGUAGE_SPEC.md).
It is primarily an authoring-time organizational cue, not a required runtime execution partition.

### 2. Function Contract

Each block should declare:

- intended inputs
- intended outputs
- high-level responsibility

This lets the user understand what the function consumes and what it produces before hardware is attached.

Examples:

- `Downstream Puff Detection`
  - inputs:
    - downstream observation stream
    - optional timing reference
  - outputs:
    - puff-detected binary signal
    - detection event stream
    - monitor summary

### 3. Internal Experiment Roles

One function block may contain multiple internal experiment roles in V1.

Example:

- `Downstream Puff Detection`
  - observation source
  - preprocessing path
  - detector path
  - event-output role

This is required because many scientist-facing functions are wider than one role.

The canvas should therefore stay high-level at the block boundary while still allowing internal structure below that boundary.
That lower-level internal structure may be described with the typed block kinds defined in [EXPERIMENT_CANVAS_TYPED_BLOCK_KINDS.md](./EXPERIMENT_CANVAS_TYPED_BLOCK_KINDS.md).

Support concerns such as calibration or reference generation should usually remain nested under one of the top-level function classes rather than becoming top-level blocks by default.
Promotion should happen only when the calibration/reference work has its own independent schedule, outputs, or operator-visible identity in the experiment design.

### 4. Bound Concrete Implementations

Each internal role may have exactly one concrete implementation in V1.

In design-time canvas state, the block is the authoritative source of truth for which implementation is currently bound to each role.

At run time, the resolved run manifest becomes the authoritative record of what binding and source mode were actually executed.

So the ownership split is:

- canvas block = design-time intended binding
- resolved run manifest = run-time actual binding

That implementation may run in exactly one source mode:

- `Real`
- `Virtual`
- `Replay`
- `Synthetic`

V1 should not allow multiple prepared implementations on one role at the same time.

This rule exists to protect scientific trust and operator clarity:

- the user must always be able to see exactly what is active,
- the system must not quietly drift from `Real` to `Replay`,
- and the block should never look ready because of a hidden inactive fallback.

If the user wants to switch implementation or source mode, that should be an explicit rebind action in the design/build flow.

## What A Block Must Not Contain

The canvas is not the runtime control surface.

A block must not own:

- initialize button
- start button
- stop button
- emergency stop button
- live monitor charts
- real-time run controls

Those belong to the system control and monitor panel.

## V1 Readiness Indicator

Each block should expose one traffic-light readiness indicator.

This indicator represents canonical `function readiness`, not live runtime health.

The canonical readiness names are:

- `Ready`
- `Caution`
- `Blocked`

The traffic-light colors are only the visual encoding:

- `Green` = `Ready`
- `Yellow` = `Caution`
- `Red` = `Blocked`

### Green / Ready

`Green` means:

- the function is fully bound,
- required roles are satisfied,
- required implementations exist,
- the current source modes are valid,
- no blocking validation issues exist,
- and the function is structurally complete and ready to be submitted to a run with intended fidelity.

### Yellow / Caution

`Yellow` means:

- the function is usable but degraded, provisional, or cautionary.

Examples:

- a virtual implementation exists but the intended real implementation is missing,
- replay or synthetic mode exists for testing but not for the intended scientific run,
- an optional role is missing,
- or warning-level validation issues exist.

The intended source mode should be recorded in the design-time block binding, so the canvas can truthfully compare intended mode against currently bound mode.

### Red / Blocked

`Red` means:

- the function is blocked.

Examples:

- a required role is unbound,
- no valid implementation exists,
- a required input or output contract is broken,
- or blocking validation/lint issues exist.

## Readiness Reasons

The traffic light alone is not enough.

Each block should also expose concise readiness reasons such as:

- `Observation source missing`
- `Bound to replay source only`
- `Required detector output not declared`
- `Ready with real hardware`

This is necessary so the canvas remains interpretable without opening a separate diagnostics surface.

## Relationship To Runtime

The block contract must stay consistent with the runtime architecture:

- experiment function at the top,
- experiment roles inside the function,
- one active concrete implementation per role,
- runtime sessions and services below that binding.

The block is therefore:

- an authoring/building object,
- not the runtime session itself,
- and not the live monitor surface.

The runtime does not need one hard execution container for every top-level canvas block.
Coordinator start/stop flow, monitor subscriptions, and recorder inputs may still connect directly to runtime-owned sessions, streams, and summaries.

It should also not be used as the top-level home for generic lifecycle concerns such as device initialization or termination.

## V1 Summary

The V1 rules are:

1. one block = one experiment function
2. one block has one `function_class`
3. one block may contain multiple experiment roles
4. one role = exactly one active concrete implementation (`V1` constraint)
5. one implementation = exactly one source mode
6. block shows readiness, not runtime controls
7. traffic light must be paired with concise readiness reasons
