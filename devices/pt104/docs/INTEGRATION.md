# PT-104 Integration Dossier

## Purpose

This document records the first verified Orchestral hardware integration dossier for the Pico Technology PT-104 path.

It captures:

- what the PC-facing device is,
- where the SDK comes from,
- what protocol family it uses,
- the minimum working initialization/read/terminate path,
- the key parameters,
- parameter handling notes,
- and the result of a live hardware probe.

## Device Chain

- sensing element: `PT100 RTD`
- PC-facing device: `PT-104`
- active measurement channel in the first test: `channel 4`

Important distinction:

- the `PT100` is not the device to integrate at the PC boundary,
- the `PT-104` is the real integration target,
- the PT100 belongs in the capability and channel configuration model.

## Integration Classification

- integration mode: `SDK-backed USB device`
- protocol family: `Pico PT-104 driver API`
- transport used in the first verified test: `USB`
- direct serial protocol: `no`

## Official Source Locations Used

- local SDK header:
  - `C:\Program Files\Pico Technology\SDK\inc\usbPT104Api.h`
- local driver DLLs found:
  - `C:\Program Files\Pico Technology\SDK\lib\usbpt104.dll`
  - `C:\Program Files\Pico Technology\PicoLog\usbpt104.dll`
- official programmer's guide used for API semantics:
  - `USB PT-104 USB/Ethernet RTD Data Logger Programmer's Guide`

## Minimum Working Lifecycle

The verified lifecycle for the first successful test was:

1. enumerate attached PT-104 units over USB
2. open the first returned unit by serial number
3. set mains filtering
4. configure the active channel
5. poll until a valid sample is available
6. scale the returned integer into engineering units
7. close the unit cleanly

## Minimum Working API Path

### Initialize

- `UsbPt104Enumerate(details, length, CT_USB)`
- `UsbPt104OpenUnit(&handle, serial)`
- `UsbPt104SetMains(handle, 0)` for `50 Hz`
- `UsbPt104SetChannel(handle, channel, type, noOfWires)`

### Read

- `UsbPt104GetValue(handle, channel, &value, filtered)`
- for `PT100`, convert raw integer with:
  - `temperature_c = value / 1000.0`

### Terminate

- `UsbPt104CloseUnit(handle)`

## Key Parameters

### Channel

- meaning: which PT-104 input channel is active
- tested value: `4`
- handling note:
  - the correct physical channel must be known or discovered from wiring

### Measurement type

- meaning: what the channel should interpret the sensor as
- relevant options from the SDK include:
  - `PT100`
  - `PT1000`
  - resistance ranges
  - voltage ranges
- tested value: `PT100`
- handling note:
  - this is critical; a wrong type changes the meaning of the returned value

### Wire count

- meaning: the RTD wiring mode used by the sensor connection
- valid range from the SDK:
  - `2`
  - `3`
  - `4`
- tested value: `4`
- handling note:
  - this must match the actual probe wiring, not just the sensor family

### Mains filter

- meaning: local line-frequency filter mode
- API meaning:
  - `0` = `50 Hz`
  - `1` = `60 Hz`
- tested value: `0`
- handling note:
  - this should match the local mains environment for best noise rejection

### Filtered read flag

- meaning: whether `UsbPt104GetValue(...)` should return a filtered result
- tested value: `1`
- handling note:
  - filtered values are useful for stable monitoring,
  - unfiltered mode may be preferable for debugging or timing-sensitive diagnosis

## Parameter Handling Notes

- The PT-104 begins continuous readings after the unit is opened and channels are configured.
- A valid sample may not be available immediately after channel setup.
- The first read can fail transiently before a fresh sample is ready.
- For PT100 temperature mode, the returned integer is scaled as `1/1000 C`.

## First Verified Probe Result

The first live probe against the connected hardware produced:

- enumerate status: success
- detected device: `USB:HS337/004`
- open status: success
- mains set status: success
- channel 4 set as PT100, 4-wire: success
- first read: no usable sample yet
- second read: success
- measured temperature on channel 4: `21.501 C`
- close status: success

## Generated Probe Script

The current minimum probe script is:

- [pt104_probe.py](../probes/pt104_probe.py)

This script should be migrated into the real Orchestral workspace as a proper device test harness in a later implementation step.

## Integration Guidance For Reuse

When reusing this integration in Orchestral, the first safe test should always aim to answer:

- can the driver enumerate the device?
- can the unit be opened?
- can the target channel be configured?
- can one valid sample be read and scaled correctly?

Only after that should richer UI or longer acquisition behavior be introduced.
