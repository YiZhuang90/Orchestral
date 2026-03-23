# HuaTeng Camera Integration

## 1. Integration Target

This device package targets a HuaTeng USB industrial camera connected directly to the PC.

The allowed vendor-facing technical reference for this integration is:

- [mvsdk.py](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/legacy/pipe-flow-reference/vendor/mvsdk.py)

The active runtime path no longer depends on Python for normal app operation. The current Orchestral implementation uses a native C# binding against the vendor SDK DLL.

## 2. Current Architecture

The current HuaTeng path is built around:

- native C# SDK interop
- a long-lived in-process camera session
- in-memory frame transport
- a camera-specific device implementation window built from reusable widgets

Relevant implementation areas:

- native device layer:
  - [HuaTengNativeCameraService.cs](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/platform/src/ExperimentalControlPlatform.Devices/HuaTeng/HuaTengNativeCameraService.cs)
  - [NativeHuaTengSdk.cs](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/platform/src/ExperimentalControlPlatform.Devices/HuaTeng/NativeHuaTengSdk.cs)
- app-side camera panel:
  - [HuaTengPanelViewModel.cs](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/platform/src/ExperimentalControlPlatform.App/DevicePanels/HuaTeng/HuaTengPanelViewModel.cs)
  - [HuaTengPanelView.xaml](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/platform/src/ExperimentalControlPlatform.App/DevicePanels/HuaTeng/HuaTengPanelView.xaml)

## 3. Acquisition Modes

### 3.1 Snap frame

`Snap frame` is the bounded single-frame action.

Current behavior:

- open session
- configure camera
- call `CameraSoftTrigger()`
- grab one frame
- convert to in-memory pixels
- update the UI once

This mode is intended for:

- connectivity validation
- parameter sanity checks
- one-shot capture debugging

### 3.2 Triggered live

`Triggered live` is the deterministic software-trigger loop.

Current behavior:

- open one long-lived session
- configure trigger mode as `Triggered`
- call `CameraSoftTrigger()` once per loop iteration
- acquire one frame
- compute capture-rate metrics
- update the UI preview separately

This mode is intended for:

- explicit host-paced acquisition
- trigger-driven experiments
- rate-control experiments where host timing matters

### 3.3 Continuous live

`Continuous live` is the free-run acquisition path.

Current behavior:

- open one long-lived session
- configure trigger mode as `Continuous`
- do not send `CameraSoftTrigger()`
- drain frames directly from the camera/SDK stream

This mode is intended for:

- higher-throughput preview
- freerun capture validation
- comparison against triggered-mode performance

## 4. Rate Semantics

The camera UI must distinguish three different rates:

- `Target frame rate`
  - the requested acquisition pacing target from the control panel
- `Capture rate`
  - the measured delivered acquisition rate from the camera path
- `Display rate`
  - the UI preview refresh rate

The display rate is intentionally capped separately and is not the same thing as capture rate.

The operating rule is:

- the control panel sets `Target frame rate`
- the value card reports `Capture rate`
- the UI should not present display rate unless needed for diagnostics

## 5. Default Camera Parameter Surface

The default camera control panel for Orchestral should include:

- exposure time
- target frame rate
- color

Advanced settings should live behind a `More settings` modal rather than crowding the main control rail.

The first HuaTeng implementation already follows this direction.

## 6. ROI Behavior

The HuaTeng camera path uses ROI as a hardware-applied acquisition setting, not just a visual crop.

Current intended behavior:

- `Set ROI` enters ROI edit mode
- the last full frame remains visible as frozen background
- the ROI rectangle can be moved and resized
- when ROI editing ends, the ROI is applied directly to the camera
- `Start live` should then stream the real ROI acquisition

### 6.1 ROI constraints

Current enforced rule:

- ROI width must be a multiple of `12`
- ROI height must be a multiple of `12`

This rule must be applied in both:

- the UI-side ROI draft normalization
- the native SDK-side ROI normalization

This avoids drift between the visible ROI and the hardware-applied ROI.

### 6.2 ROI visualization rule

When ROI is active:

- the last full frame stays as the visual background
- only the ROI area should update live
- the region outside the ROI should remain dimmed

If the ROI image appears distorted, the first thing to verify is whether the displayed ROI rectangle still matches the hardware-normalized ROI dimensions.

## 7. Performance Interpretation

Triggered mode and continuous mode should not be expected to perform the same way.

General rule:

- `Triggered` gives more explicit host control
- `Continuous` can usually reach higher throughput

If a higher requested target frame rate does not increase the measured capture rate, the acquisition path is saturating at a lower real limit.

That limit may come from:

- exposure time
- readout/transfer time
- SDK buffering behavior
- host-side acquisition overhead

## 8. Validation Checklist

The first validation sequence for this camera should be:

1. enumerate the USB camera
2. connect successfully
3. snap one frame
4. start triggered live
5. start continuous live
6. compare capture-rate behavior between both modes
7. set and clear ROI
8. verify ROI dimensions are hardware-valid
9. verify ROI live updates the ROI region only
10. verify advanced settings can be applied without corrupting acquisition state

## 9. Troubleshooting Notes

Common failure classes:

- camera occupied by vendor viewer or another process
- ROI dimensions not matching hardware constraints
- triggered-mode saturation at higher requested rates
- confusion between target frame rate and measured capture rate
- UI ROI rectangle drifting from hardware-applied ROI

The first troubleshooting rule for odd imaging behavior is:

- verify what the camera actually accepted,
- not only what the UI requested.

