# PT-104 Protocol Note

## Purpose

This note records the PT-104 PC-facing protocol shape as verified from the local Pico SDK and the first live hardware test.

## Protocol Family

- family: `Pico PT-104 driver API`
- integration type: `SDK-backed USB/Ethernet data logger`
- direct serial text protocol: `no` for the USB path used in the first test

## Vendor SDK Assets Found Locally

- header: `C:\Program Files\Pico Technology\SDK\inc\usbPT104Api.h`
- DLL: `C:\Program Files\Pico Technology\SDK\lib\usbpt104.dll`
- alternate DLL location: `C:\Program Files\Pico Technology\PicoLog\usbpt104.dll`

## Core API Surface Used

- `UsbPt104Enumerate(details, length, CT_USB)`
- `UsbPt104OpenUnit(&handle, serial)`
- `UsbPt104SetMains(handle, sixty_hertz)`
- `UsbPt104SetChannel(handle, channel, type, noOfWires)`
- `UsbPt104GetValue(handle, channel, &value, filtered)`
- `UsbPt104CloseUnit(handle)`

## Observed Calling Order

1. Enumerate USB devices.
2. Parse the returned `USB:<serial>` entry.
3. Open the device using the serial number.
4. Set mains filtering.
5. Configure the target channel.
6. Poll `UsbPt104GetValue(...)` until a valid sample is available.
7. Close the unit.

## Enumerate Behavior

The official programmer's guide states that USB enumeration returns devices as:

- `USB:Serial Number`

Observed live result:

- `USB:HS337/004`

## Channel and Measurement Configuration

Relevant enums from the local SDK header include:

- channels:
  - `USBPT104_CHANNEL_1` through `USBPT104_CHANNEL_8`
- measurement types:
  - `USBPT104_PT100`
  - `USBPT104_PT1000`
  - resistance modes
  - differential voltage modes
  - single-ended voltage modes

For the verified test, the device was configured as:

- channel: `4`
- type: `PT100`
- wires: `4`

## Mains Filtering

The API uses:

- `0` for `50 Hz`
- `1` for `60 Hz`

The verified test used `0`.

## Read Semantics

According to the official programmer's guide, once the unit is opened and channels are configured, the driver begins continuous readings. `UsbPt104GetValue(...)` returns the most recent reading for the requested channel.

For PT100 temperature mode, the returned integer is scaled as:

- `temperature = value / 1000.0` in `C`

## Observed Read Behavior

The first poll after channel setup did not return a usable sample yet. A later poll succeeded. This means callers should expect an initial warm-up or sample-availability delay after channel configuration.

## Integration Implications For Orchestral

The PT-104 adapter should expose at least:

- transport mode selection, defaulting to USB
- channel selection
- measurement type selection
- wire-count selection
- mains filter setting
- filtered vs unfiltered read mode
- a polling-based read path

This should be represented as an SDK-backed device adapter rather than as a serial-protocol device.
