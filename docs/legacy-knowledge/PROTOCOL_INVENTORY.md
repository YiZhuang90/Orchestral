# Protocol Inventory

## Purpose

This document captures how the preserved pipe-flow system communicates with its devices. In Orchestral terms, this is protocol knowledge, not runtime architecture.

## Reference Sources

- [exp_control_functions.py](../../legacy/pipe-flow-reference/python/exp_control_functions.py)
- [mvsdk.py](../../legacy/pipe-flow-reference/vendor/mvsdk.py)

## Protocol Families

### 1. MindVision camera SDK

- Device class: industrial cameras.
- Transport surface in Python:
  - SDK wrapper through `mvsdk.py`,
  - dynamic library loading on Windows (`MVCAMSDK` or `MVCAMSDK_X64`),
  - function-level camera control rather than serial text commands.
- Main operations used:
  - enumerate devices,
  - initialize camera,
  - set analog gain,
  - set trigger mode,
  - set exposure,
  - start streaming,
  - fetch frame buffer,
  - release frame buffer,
  - uninitialize camera.
- Runtime implications:
  - this is an SDK-backed image-source protocol, not a generic serial device,
  - it needs a device-specific adapter in Orchestral rather than a text-protocol abstraction.

### 2. Pico PT-104 SDK protocol

- Device class: RTD temperature logger.
- Transport surface in Python:
  - `picosdk.usbPT104.usbPt104`,
  - C-style API through ctypes.
- Main operations used:
  - open unit,
  - set mains filter,
  - set active channels,
  - read temperature values,
  - close unit.
- Runtime implications:
  - this is another SDK-backed device family,
  - channel selection and wiring mode are part of the protocol setup, not just experiment config.

### 3. Control center serial protocol

- Device class: combined laser + puff actuator + flowrate source.
- Serial settings seen in code:
  - baud rate `9600`,
  - timeout `2 s`.
- Command style:
  - write ASCII line with comma-separated fields and trailing newline:
    - `"{int(puff_state)},{int(laser_state)},{step_number}\n"`.
- Readback style:
  - read ASCII line,
  - parse `"time_stamp,pulse_count"`,
  - convert timestamp from milliseconds to seconds.
- Protocol meaning in the preserved workflow:
  - output fields are shared across multiple physical effects,
  - the same session carries both control commands and flowrate telemetry.
- Runtime implications:
  - this is a stateful serial protocol with mixed control and telemetry responsibilities,
  - it should likely be split logically in Orchestral into:
    - laser capability,
    - pulse actuator capability,
    - flowrate telemetry stream.

### 4. End-height controller serial protocol

- Device class: linear actuator / end-height controller.
- Serial settings seen in code:
  - baud rate `9600`,
  - timeout `2 s`.
- Command style:
  - write ASCII representation of `step_number` with no explicit newline in `change_end_height(ser)`.
- Protocol meaning:
  - the control loop computes `step_number`,
  - the serial device receives only the step count command.
- Runtime implications:
  - the current abstraction is thin and assumes the device firmware understands plain numeric commands,
  - this protocol needs an explicit request/acknowledgement model in the new system if reliability matters.

### 5. Bath serial protocol

- Device class: temperature-control bath.
- Serial settings seen in code:
  - baud rate `4800`,
  - timeout `2 s`,
  - 8-bit data.
- Read command:
  - `b"S\r"` to query set temperature,
  - response parsed by slicing characters `response[5:10]`.
- Write command:
  - `f"W S0 {set_temp:.2f}\r"`.
- Acknowledgement:
  - expected reply is `$`,
  - any other reply is treated as a failed set command.
- Runtime implications:
  - this is a text command/response serial device with protocol-specific parsing assumptions,
  - the fixed response slicing is brittle and should be made explicit in a future adapter.

## Timing and Runtime Assumptions Embedded in the Legacy Code

- Camera timing is treated as high-frequency and partly hardware-stamped via `FrameHead.uiTimeStamp`.
- Computed signal generation runs at a lower logical rate such as `100 Hz`.
- Flowrate telemetry is interpreted from pulse counts and previous timestamps.
- Several loops run continuously until `exit_flag` is set.
- Some threads use `threading.Timer` as if it were a long-running worker thread entrypoint.

## Migration Risks

- The preserved looping notebook references helper functions such as `send_global_variables_looping`, `Looping_verification`, and `data_log_at_end_looping_version`, but the current preserved helper module no longer contains matching active definitions for all of them.
- That means protocol behavior for some legacy paths is partly preserved in the notebook’s intent and partly preserved in the helper module’s surviving code.
- Orchestral should therefore treat this protocol inventory as:
  - partly direct evidence from code,
  - partly evidence requiring reconciliation during migration.
