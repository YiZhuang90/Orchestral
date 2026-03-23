# Test Before Handover

## 1. Purpose

This document defines the minimum verification standard before Orchestral hands a device implementation window to the user.

This standard applies:

- after the first implementation handover,
- and after every troubleshooting pass that claims the panel is fixed.

The goal is to prevent false handovers where:

- the code builds,
- unit tests pass,
- a backend harness works,
- but the actual implementation window still behaves incorrectly.

## 2. Core Rule

Before handover, Orchestral must verify both:

- visible UI behavior,
- and the internal data flow behind that behavior.

Neither alone is sufficient.

## 3. What Does Not Count As Enough

The following are useful, but not sufficient by themselves:

- `dotnet build` passed
- automated tests passed
- vendor SDK probe worked
- backend harness produced correct values
- a screenshot looked plausible

Handover requires real interaction with the actual implementation window.

## 4. Verification Layers

Every handover check should distinguish:

- UI state change
- internal state transition
- hardware response
- displayed value update

If any layer disagrees with the others, the issue is unresolved.

## 5. Minimum Interaction Coverage

Orchestral should exercise all visible user-facing controls that can affect behavior.

At minimum, this includes:

- every primary button
- every secondary action button
- every parameter input
- every dropdown or selector
- every toggle
- every diagnostics or settings popup
- every live preview, plot, or display surface
- every footer or status label that claims hardware state

## 6. Internal Data-Flow Checks

For each exercised interaction, Orchestral should also verify the corresponding internal effect.

Examples:

- `Connect`
  - confirm that a real device/session open occurs
- `Disconnect`
  - confirm that the session closes cleanly
- `Snap frame`
  - confirm that a new frame is acquired, not a stale cached frame
- `Start live`
  - confirm that the acquisition loop actually starts and new timestamps/counters advance
- `Stop live`
  - confirm that the loop actually stops and resources are released
- `Apply`
  - confirm that the parameter write or restart path actually succeeds
- `Set ROI`
  - confirm that the ROI is normalized if needed, sent to hardware, and reflected truthfully in the UI

## 7. Device-Family Verification Matrix

### 7.1 Scalar sensor

Verify:

- connect
- single read
- live read start
- live read stop
- parameter apply
- displayed value and plotted/live value consistency

### 7.2 Camera or imaging device

Verify:

- connect
- snap frame
- start live
- stop live
- parameter apply
- diagnostics popup
- metadata updates
- preview surface renders actual frames
- ROI edit/apply if ROI exists

For camera panels, verify that:

- the preview really changes with new frames
- the reported capture or preview rate matches the intended semantic meaning
- the displayed frame or ROI metadata matches the actual acquisition mode

### 7.3 Actuator

Verify:

- connect
- enable
- disable
- setpoint apply
- actuation command
- state feedback
- alarm or interlock status if present

### 7.4 Protocol or debug device

Verify:

- connect
- send command
- receive response
- diagnostics/log view
- parsing or status indicators

## 8. Evidence Required

The handover check should leave behind a small verification record.

That record should state:

- which UI actions were exercised
- which internal signals were observed
- which hardware responses were confirmed
- which displayed values were cross-checked
- and what remains unverified, if anything

The record can be lightweight, but it should be explicit.

## 9. Handover Gate

Orchestral should only hand over the panel when:

- the implementation window has been exercised directly
- the corresponding internal data paths were checked
- the observed behavior matches the intended semantics
- and any remaining gaps are clearly stated

If a troubleshooting pass changes the panel, this gate must be run again.
