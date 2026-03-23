# Device Widget Expansion

## Purpose

The current Orchestral widget set is strong for scalar time-series acquisition and output devices. This document expands the widget roadmap so future device panels can stay in the same design language without inventing new visual systems ad hoc.

## Current Strength

The current panel system already fits:

- temperature sensors
- pressure sensors
- flow meters
- scalar DAQ channels
- simple serial output devices

These are all well served by the current `serial data panel` pattern.

## Expansion Principle

New widgets should:

- reuse the same typography
- reuse the same color roles
- reuse the same radii
- reuse the same shell and footer behavior
- compose with current widgets where possible

They should not create a second visual language.

## Future Device Families

### 1. Imaging devices

Examples:

- cameras
- IR cameras
- high-speed cameras
- microscope cameras

#### New widgets needed

- `Live image window`
  - a real-time image or video surface
- `ROI editor`
  - draw, move, resize, delete regions of interest
- `ROI list`
  - shows named ROIs and active selection
- `Image analysis strip`
  - mean, max, min, centroid, threshold status
- `Histogram panel`
  - grayscale or channel histogram
- `Frame metadata strip`
  - frame number, timestamp, exposure, gain, dropped frames

#### Preparation to do now

- reserve a main-content variant for image surfaces
- define how ROI overlays inherit Orchestral colors and line weights
- define a card style for image-analysis summaries

### 2. Waveform devices

Examples:

- oscilloscopes
- microphone/acoustic streams
- high-rate DAQ channels
- vibration sensors

#### New widgets needed

- `Waveform panel`
  - time-domain waveform
- `Spectrum panel`
  - FFT or PSD
- `Waveform statistics strip`
  - RMS, peak-to-peak, dominant frequency, bandwidth
- `Trigger control panel`
  - trigger mode, threshold, holdoff

#### Preparation to do now

- define a general plot family with variants:
  - scalar trend
  - waveform
  - spectrum

### 3. Actuator and motion devices

Examples:

- valves
- pumps
- stages
- motors
- shutters
- relays

#### New widgets needed

- `Actuator control panel`
  - setpoint, enable, disable, jog, pulse
- `State timeline`
  - command and state history
- `Command history panel`
  - recent commands and acknowledgements
- `Position/target card strip`
  - target, actual, error, velocity

#### Preparation to do now

- formalize a non-plot main-content variant
- define a standard binary control pattern for enable/disable and open/close

### 4. Protocol and integration tools

Examples:

- serial bridges
- SDK wrappers
- network-connected controllers
- vendor APIs

#### New widgets needed

- `Protocol monitor panel`
  - raw sent/received messages
- `Connection trace panel`
  - timestamps, retries, failures
- `Command tester`
  - send one command and inspect response
- `Capability panel`
  - supported commands, limits, modes

#### Preparation to do now

- define a log/timeline surface pattern
- define a compact monospace data block style for raw protocol content

### 5. Calibration and setup devices

Examples:

- calibration rigs
- sensor-offset tools
- reference standards

#### New widgets needed

- `Calibration panel`
  - offset, gain, reference, apply, revert
- `Reference comparison card`
  - current value vs reference value vs error
- `Calibration history panel`
  - recent calibration events

#### Preparation to do now

- define a form-heavy content variant
- define a safe “apply / revert / save” button hierarchy

### 6. Recording and storage devices

Examples:

- data recorders
- file writers
- capture services

#### New widgets needed

- `Recording panel`
  - start recording, stop recording, output path
- `Recording status strip`
  - duration, file size, throughput, dropped frames
- `Storage health card`
  - free space, path validity, write speed

#### Preparation to do now

- define a standard storage/status summary block

### 7. Alarm and interlock systems

Examples:

- safety relays
- fault monitors
- interlock controllers

#### New widgets needed

- `Alarm panel`
  - active alarms, latched alarms, cleared alarms
- `Interlock state panel`
  - ready, blocked, tripped, bypassed
- `Fault timeline`
  - event history with timestamps

#### Preparation to do now

- define a status severity palette
- define a reusable event-list widget

## Recommended Next Widget Modules

If the goal is to prepare the UI system in advance, the next widgets worth formalizing are:

1. `Live image window`
2. `ROI editor`
3. `Waveform panel`
4. `Actuator control panel`
5. `Protocol monitor panel`
6. `Alarm/event timeline`

These six modules would expand Orchestral beyond scalar sensors while keeping a coherent family.

## Suggested New Panel Archetypes

The current `serial data panel` should be joined by:

- `image device panel`
  - live image window
  - ROI editor
  - image analysis strip
- `waveform device panel`
  - waveform panel
  - spectrum or stats strip
- `actuator panel`
  - actuator control panel
  - command/status history
- `protocol integration panel`
  - protocol monitor
  - diagnostics and capability summary
- `alarm/interlock panel`
  - alarm list
  - state panel
  - event history

## Implementation Preparation

Before implementing these new device families, Orchestral should:

1. extract current shared widgets from PT-104 composition into reusable `UserControl`s
2. define a `widget contract` for inputs, outputs, and visual slots
3. define data models for:
   - scalar stream
   - image stream
   - waveform stream
   - actuator state
   - event timeline
4. keep all new widgets inside the current token system rather than restyling per device

## Success Criterion

This expansion is successful when a future camera panel or actuator panel can be designed by selecting from the widget catalog and archetypes in these docs, instead of inventing a new layout and style from scratch.
