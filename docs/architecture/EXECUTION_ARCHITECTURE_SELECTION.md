# Execution Architecture Selection

## 1. Purpose

This document defines how Orchestral should choose the execution architecture for a new hardware integration.

The core question is not only:

- `can this device be made to work?`

but also:

- `what integration architecture is truthful, maintainable, and performant enough for this device class?`

## 2. Candidate Architecture Types

Typical candidates are:

- vendor CLI or existing executable wrapper
- Python probe or SDK wrapper
- native C# SDK binding
- network protocol client
- serial driver path

These should be treated as explicit architecture choices, not implementation accidents.

## 3. Default Selection Rule

Start with the fastest safe validation path.

Then escalate if the chosen path is:

- too slow,
- semantically misleading,
- unstable,
- too hard to verify,
- or unsuitable for the device class.

## 4. Architecture Profiles

### 4.1 Vendor CLI or existing executable wrapper

Use when:

- the vendor already provides a stable command tool
- the device is low-rate
- the first goal is proof of contact

Strengths:

- fast to validate
- low implementation cost

Weaknesses:

- limited control
- poor observability
- often poor for live/high-rate behavior

### 4.2 Python probe or SDK wrapper

Use when:

- a vendor SDK is easiest to access from Python
- the goal is onboarding or first-pass experimentation
- the device is not yet proven worth native integration

Strengths:

- fast iteration
- easy access to SDK wrappers
- good for early probing and exploratory integration

Weaknesses:

- extra process and marshaling overhead
- weaker timing guarantees
- more fragile for live preview or high-rate control

### 4.3 Native C# SDK binding

Use when:

- the device is performance-sensitive
- long-lived acquisition/control loops are required
- Orchestral needs truthful in-process behavior
- the device is likely to become a first-class supported path

Strengths:

- best performance
- best memory/control over buffers and timing
- easier truthful integration with WPF and runtime state

Weaknesses:

- highest implementation effort
- more interop and marshaling work

### 4.4 Network protocol client

Use when:

- the device is a networked instrument
- the protocol is documented or standard
- the device behavior is already packet/message-based

Strengths:

- clean direct integration
- strong observability

Weaknesses:

- protocol reverse-engineering may be costly if docs are weak

### 4.5 Serial driver path

Use when:

- the device is fundamentally serial
- the protocol is textual or binary over COM/UART/USB-serial

Strengths:

- simple and inspectable
- often easy to troubleshoot

Weaknesses:

- device-specific framing and timing details can still be tricky

## 5. Escalation Rules

Orchestral should escalate away from the first working architecture when:

- the measured behavior is far below device expectations
- timing semantics become ambiguous or misleading
- UI values cannot be kept truthful
- the bridge path dominates performance
- repeated troubleshooting reveals architecture-level limits rather than implementation bugs

Examples:

- camera preview works through Python, but delivered frame rate is far below expected
- waveform device reads one sample, but persistent acquisition is too slow through a CLI wrapper
- actuator command works, but response timing is too uncertain for reliable runtime use

## 6. Device-Class Recommendations

### 6.1 Scalar sensors

Typical recommendation:

- vendor CLI, Python probe, native C#, serial, or protocol client can all be acceptable

Default priority:

- easiest truthful path first

### 6.2 Actuators

Typical recommendation:

- protocol client, serial path, or native SDK

Important:

- truthfulness and safety matter more than raw throughput

### 6.3 Cameras and imaging devices

Typical recommendation:

- Python probe may be acceptable for onboarding
- native C# SDK binding is usually the right production path

Important:

- distinguish target capture rate, measured capture rate, and display rate
- avoid file-based frame handoff in production paths

### 6.4 Waveform or high-rate DAQ devices

Typical recommendation:

- native or protocol-level streaming path preferred early

Important:

- first-pass success via a slow bridge is not enough if it hides the real limits

## 7. Validation Questions

Before locking an architecture, Orchestral should answer:

- does it initialize the device reliably?
- does it support the required control/read path?
- is it truthful enough for the intended UI?
- is the measured behavior close enough to device expectations?
- can it be debugged and verified cleanly?
- is it maintainable as a first-class Orchestral device path?

## 8. Output Artifact

For each integrated device, Orchestral should record:

- chosen execution architecture
- why it was chosen
- what alternatives were considered
- what performance/truthfulness limits were observed
- and what would justify a future escalation
