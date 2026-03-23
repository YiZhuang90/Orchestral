# Device Archetype Mapping

## 1. Purpose

This document defines how Orchestral should map a newly integrated device onto an implementation-window archetype and widget stack.

The default rule is:

- reuse an existing shell and widget family whenever possible
- only create a new composite widget or archetype when the device introduces a genuinely new interaction surface

## 2. Core Archetypes

The first-class archetypes are:

- scalar sensor
- actuator
- camera or imaging device
- protocol or debug device
- hybrid device

## 3. Scalar Sensor

Use when:

- the device mainly produces one or more scalar values over time
- the main user task is reading, monitoring, plotting, and basic configuration

Typical widgets:

- top banner
- device selector header
- subcontext switcher
- control panel
- real-time value header
- plotting window
- statistics panel
- bottom status bar

Typical default parameters:

- channel
- filter
- sample/read mode
- sensor-specific settings

Typical ownership model:

- shared device scope for connection and diagnostics
- endpoint scope for channel settings and data
- session scope for read once and live acquisition

Typical outputs:

- scalar sample stream
- applied channel settings
- endpoint stats
- status and diagnostics events

Examples:

- temperature logger
- pressure sensor
- flow-rate device

## 4. Actuator

Use when:

- the device’s main role is applying commands or setpoints
- monitoring exists, but is secondary to actuation

Typical widgets:

- top banner
- device selector header
- subcontext switcher when the actuator has multiple axes or ports
- control panel
- actuator command panel
- status/feedback panel
- bottom status bar

Typical default parameters:

- setpoint
- enable/disable
- action mode
- safety or interlock status

Typical ownership model:

- shared device scope for connection and safety state
- endpoint scope for one axis, valve, or output lane
- session scope for command execution and confirmation

Typical outputs:

- command result stream
- live state feedback
- applied settings record
- interlock and fault state

Examples:

- valve controller
- motorized stage
- pump controller

## 5. Camera or Imaging Device

Use when:

- the device’s primary output is image frames
- preview, acquisition mode, and metadata matter more than scalar plotting

Typical widgets:

- top banner
- device selector header
- optional subcontext switcher
- camera control panel
- real-time value header
- camera image window
- frame metadata strip
- bottom status bar

Optional widgets:

- ROI editor
- histogram panel
- recording panel
- image analysis strip

Typical default parameters:

- exposure
- target frame rate
- color mode
- more settings

Typical ownership model:

- shared device scope for camera identity and connection
- endpoint scope only when there are multiple sensors, streams, or views
- session scope for snap, triggered live, continuous live, ROI editing, and recording

Typical outputs:

- frame stream
- applied acquisition settings
- ROI and normalization record
- capture-rate and diagnostics events

Examples:

- industrial USB camera
- integrated laptop camera
- scientific camera

## 6. Protocol or Debug Device

Use when:

- the main job is protocol visibility, command exchange, or troubleshooting
- the device is not best represented as a scalar sensor or camera

Typical widgets:

- top banner
- device selector header
- optional subcontext switcher for multiple ports or sessions
- connection/config panel
- protocol monitor panel
- diagnostics panel
- status bar

Typical default parameters:

- port or endpoint
- baud or protocol mode
- connect/disconnect
- send command
- response log

Typical ownership model:

- shared device scope for connection and transport identity
- endpoint scope for port/session/channel when relevant
- session scope for monitor, send, and capture modes

Typical outputs:

- message stream
- applied transport settings
- diagnostics log
- error and retry events

Examples:

- protocol bridge
- serial debug instrument
- packet-oriented controller

## 7. Hybrid Device

Use when:

- the device clearly spans multiple roles
- for example, an actuator with strong live sensing, or a camera with substantial protocol/config tooling

Rule:

- start from the dominant archetype
- add one secondary composite widget family only if it is truly needed

Ownership rule:

- keep the dominant archetype's ownership model
- add only one extra endpoint or session layer when the secondary role truly requires it

Examples:

- camera with deep hardware-control workflow
- DAQ with both waveform capture and active output control

## 8. Widget-Selection Rule

When mapping a new device, Orchestral should decide:

- which archetype is primary
- which widgets are mandatory
- which widgets are optional
- which parameters belong in the default control panel
- which settings should be moved into `More settings`
- which scopes own those settings
- which outputs the panel must emit

## 9. Parameter-Surface Rule

Default parameters should be:

- few in number
- high-value
- visible without opening a secondary dialog

More detailed or unstable settings should move into:

- advanced settings popup
- diagnostics
- device-specific secondary panels

## 10. Output Artifact

For each integrated device, Orchestral should record:

- chosen archetype
- chosen widget stack
- default parameter surface
- ownership model
- panel output model
- optional widget families left for later
- and why this mapping was selected
