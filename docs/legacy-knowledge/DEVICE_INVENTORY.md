# Device Inventory

## Purpose

This document captures the concrete hardware roles used by the preserved pipe-flow turbulence system. It is a knowledge artifact for Orchestral migration, not a claim that the old software architecture should be reused.

## Reference Sources

- [Looping_exp_main.ipynb](../../legacy/pipe-flow-reference/notebooks/Looping_exp_main.ipynb)
- [exp_control_functions.py](../../legacy/pipe-flow-reference/python/exp_control_functions.py)
- [mvsdk.py](../../legacy/pipe-flow-reference/vendor/mvsdk.py)

## Device Roles

### 1. Camera pair

- Role: two monochrome imaging sources used at two streamwise locations.
- Runtime use:
  - one camera is used for upstream verification,
  - one camera is used for downstream detection in looping mode,
  - in the two-point tracking path they act as point 1 and point 2 for puff survival/decay/splitting logic.
- Software boundary:
  - camera enumeration and setup live in `initialize_all_cameras()` and `setup_camera()`,
  - frame acquisition uses `grab_an_image()`,
  - shutdown uses `shut_down_camera()`.
- Important settings surfaced by the old system:
  - exposure time,
  - gain,
  - software trigger on/off,
  - ROI,
  - expected frame frequency,
  - base-frame count for laminar background estimation.
- Concrete values seen in the preserved looping control cell:
  - exposure: `[0.35, 0.4]` ms,
  - gain: `[2, 2]`,
  - expected frequency: `890 Hz`,
  - ROI 1: `[85, 85, 310, 80]`,
  - ROI 2: `[0, 90, 200, 80]`.
- Vendor boundary:
  - `mvsdk.py` is a vendor wrapper over the MindVision camera SDK,
  - the wrapper dynamically loads `MVCAMSDK` / `MVCAMSDK_X64` on Windows.

### 2. Pico PT-104 temperature logger

- Role: multi-channel RTD acquisition for inlet/outlet temperature monitoring.
- Software boundary:
  - initialization: `initialize_pico_channel_multisensor(channel_idxes)`,
  - acquisition loop: `acquire_temperature_multisensor(...)`,
  - shutdown: `terminate_pico_logger(...)`.
- Important settings:
  - channel indices,
  - temperature offsets,
  - filtered/unfiltered read mode,
  - acquisition frequency via `Re_frequency`.
- Concrete values seen in the looping control cell:
  - channels: `[2, 4]`,
  - temperature shifts: `[0.117710, 0.127578]`.
- Data produced per sample in the acquisition loop:
  - channel 1 temperature,
  - channel 2 temperature,
  - mean temperature,
  - delta temperature,
  - elapsed time.

### 3. Control center serial device

- Role: combined control hub for laser state, puff actuator state, and flowrate pulse readback.
- Software boundary:
  - initialization: `initialize_control_center(port_info)`,
  - command send path: `send_control_command(ser)`,
  - laser toggling: `turn_on_laser(ser)` / `turn_off_laser(ser)`,
  - puff actuation: `activate_puff_actuator(ser)`,
  - flowrate readback: `read_pulse_count(ser)` and `acquire_flowrate_control_center_version(ser)`,
  - shutdown: `release_control_center(ser)`.
- Concrete port seen in the looping control cell:
  - `COM5`.
- Important observation:
  - this is not only an actuator device; it is also the flowrate telemetry source in the preserved looping workflow.

### 4. End-height linear actuator controller

- Role: serial-controlled linear actuator used for exit height / flow control, especially for Reynolds-number control.
- Software boundary:
  - initialization: `initialize_end_height_control(port_info)`,
  - command send: `change_end_height(ser)`,
  - shutdown: `release_end_height_control(ser)`.
- Concrete port seen in the looping control cell:
  - `COM8`.
- Important observation:
  - `Re_control_center(...)` computes a step count and writes that step count as a plain string to this serial device.

### 5. Temperature-control bath

- Role: optional heat-bath controller for maintaining thermal conditions.
- Software boundary:
  - initialization: `initialize_bath(bath_port)`,
  - read setpoint: `check_bath_set_temp(ser)`,
  - write setpoint: `change_bath_temp(ser, set_temp)`,
  - control loop: `temperature_control_center(ser, iteration_time)`,
  - shutdown: `release_bath(ser)`.
- Concrete port seen in the looping control cell:
  - `COM12`.
- Important observation:
  - the bath is optional in the preserved workflow via `temperature_control_switch`.

## Secondary / Historical Device Paths

The helper module still contains older or alternate hardware paths that are not central to the preserved looping control cell but remain valuable as legacy knowledge:

- Arduino flowrate path via `initialize_arduino()` and `initialize_actuator_set_interval(...)`,
- Kern scale path via `initialize_kern_scale(...)` and `read_mass_flowrate(...)`.

These should be treated as historical device adapters, not part of the currently trusted looping workflow.

## Migration Notes

- The old system is already close to a role-based device model:
  - imaging source,
  - temperature sensor,
  - combined actuator/telemetry controller,
  - linear actuator,
  - thermal controller.
- The new platform should preserve those roles while separating:
  - device identity,
  - protocol,
  - capabilities,
  - experiment logic.
