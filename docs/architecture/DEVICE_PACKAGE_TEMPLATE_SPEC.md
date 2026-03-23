# Device Package Template Spec

## 1. Purpose

This document defines the first standard device package template for Orchestral.

The goal of a device package is to make hardware integration reusable, reviewable, and shareable across:

- labs,
- future experiments,
- future Orchestral users,
- and eventually the public GitHub ecosystem around the project.

A device package should not be just a driver. It should be a complete integration asset.

## 2. Design Goal

A standard device package should capture all of the following in one coherent unit:

- what the device is,
- how the PC talks to it,
- what parameters matter,
- how to initialize it,
- how to read from it,
- how to write to it if applicable,
- how to test it safely,
- how to monitor it in the UI,
- and how to document verified behavior.

This makes a device package both a software component and a knowledge component.

## 3. Guiding Principles

### 3.1 Reusable

A device package should be useful beyond one experiment.

### 3.2 Verifiable

A device package should contain a narrow test path that proves the device can actually be reached and used.

### 3.3 Shareable

A device package should be publishable in the repo as a reusable integration asset.

### 3.4 Bounded

A device package should not try to solve every possible workflow for the device in V1.

### 3.5 Honest

A package should distinguish:

- documented facts,
- verified facts,
- inferred assumptions,
- and still-unknown behavior.

## 4. Language Policy

The package should allow both `C#` and `Python`, but with strict ownership boundaries.

### 4.1 C# responsibilities

C# is the primary language of the device package.

It should own:

- the real Orchestral device integration surface,
- the runtime-facing adapter,
- the parameter model,
- the app-facing test panel,
- the long-term reusable package structure.

### 4.2 Python responsibilities

Python is optional and secondary.

It may be used for:

- quick probes,
- vendor-SDK experiments,
- one-off validation helpers,
- migration support,
- rapid hardware bring-up.

### 4.3 Non-duplication rule

The same runtime behavior should not be owned in parallel by both languages.

The long-term authoritative integration should live in C#.

## 5. Standard Package Responsibilities

Every device package should aim to provide:

- device identity,
- protocol identity,
- parameter schema,
- capability summary,
- minimum working integration path,
- test harness,
- test panel,
- troubleshooting notes,
- validation notes.

## 6. Standard Package Structure

A first V0 package should use a structure like this:

```text
<device-package>/
©À©¤ docs/
©¦  ©À©¤ INTEGRATION.md
©¦  ©À©¤ PROTOCOL.md
©¦  ©À©¤ VALIDATION.md
©¦  ©¸©¤ TROUBLESHOOTING.md
©À©¤ definition/
©¦  ©À©¤ device.yaml
©¦  ©À©¤ protocol.yaml
©¦  ©¸©¤ parameters.yaml
©À©¤ probes/
©¦  ©¸©¤ optional quick probes (Python or C#)
©À©¤ src/
©¦  ©À©¤ adapter code
©¦  ©À©¤ parameter model
©¦  ©¸©¤ UI test panel code
©¸©¤ tests/
   ©À©¤ unit tests
   ©¸©¤ integration or hardware-present tests
```

This is a logical structure. In the current repo, the early PT-104 files may be distributed between docs and tools until the package system is fully implemented.

## 7. Required Documentation Files

### 7.1 INTEGRATION.md

This file should answer:

- what the PC-facing device is,
- where the SDK or driver comes from,
- what protocol family it uses,
- what the minimum lifecycle is,
- what the key parameters are,
- and what the first verified working path is.

### 7.2 PROTOCOL.md

This file should capture:

- communication family,
- transport mode,
- command/API surface,
- setup order,
- scaling rules,
- response handling,
- and protocol-specific quirks.

### 7.3 VALIDATION.md

This file should capture:

- what information the user initially provided,
- what Orchestral inferred,
- what source order was used,
- what test artifact was generated,
- what live test result occurred,
- and what remains unknown.

### 7.4 TROUBLESHOOTING.md

This file should capture:

- common failure modes,
- missing dependency symptoms,
- permission or driver issues,
- parameter mismatch issues,
- and what to check next.

## 8. Required Definition Files

### 8.1 device.yaml

This should capture:

- package name,
- vendor,
- model,
- device class,
- connection form,
- capability summary,
- dependency notes,
- status of validation.

### 8.2 protocol.yaml

This should capture:

- protocol family,
- transport type,
- SDK or library requirement,
- initialization path,
- read path,
- write path if applicable,
- trust status,
- source references.

### 8.3 parameters.yaml

This should capture:

- parameter name,
- type,
- unit,
- allowed values or range,
- default,
- required/optional state,
- operator visibility,
- and a short description.

## 9. Standard Parameter Model

Each device package should explicitly classify parameters.

### 9.1 Identity parameters

Examples:

- serial number,
- USB selection,
- IP address,
- channel index.

### 9.2 Configuration parameters

Examples:

- sensor type,
- gain,
- exposure,
- wire count,
- mains filter,
- filtered read flag.

### 9.3 Acquisition parameters

Examples:

- sample interval,
- read timeout,
- polling mode,
- buffer length.

### 9.4 Safety or operating parameters

Examples:

- writable command limits,
- maximum drive values,
- safe defaults,
- stop-on-error behavior.

## 10. Standard Test Harness Contract

Each device package should include a narrow test harness that can answer the minimum bring-up question:

- can this device be identified, initialized, and used for one safe operation?

The harness should ideally support:

- enumerate or discover,
- connect,
- configure,
- read once or perform one safe output,
- stop and release,
- print a structured result.

## 11. Standard UI Test Panel Contract

Each package should support a minimal test panel in the Orchestral app.

The V0 panel should include:

- device identity area,
- connection state,
- editable core parameters,
- `Connect` action,
- `Read Once` or equivalent safe test action,
- `Start` short live test,
- `Stop`,
- current value/status display,
- error/status message area.

This panel should be functional first, not visually polished.

## 12. Validation Levels

Every package should record a validation level.

Suggested early levels:

- `documented`: docs exist, but no live verification yet
- `probed`: minimum live probe succeeded
- `panel-validated`: app test panel succeeded on real hardware
- `runtime-validated`: used successfully in a real experiment runtime

## 13. PT-104 As The First Reference Package

The PT-104 package should serve as the first reference implementation of this template.

It is a good first case because it already exercises:

- vendor SDK discovery,
- protocol inference,
- key parameter documentation,
- minimum live probe,
- reusable integration notes.

The PT-104 package should therefore be used to validate and refine this template, not treated as a one-off exception.

## 14. What Not To Do In V0

The first device package template should not yet require:

- a fully generic plugin marketplace,
- a rich visual design system,
- every possible device lifecycle hook,
- automatic code generation for every file,
- full cross-device abstraction purity.

The goal is a practical standard that can grow, not a perfect framework guessed too early.

## 15. Success Criteria

This template is successful when a new device package can be built with it and yields:

- a clear reusable knowledge record,
- a narrow working probe,
- a minimal app test panel,
- explicit parameter handling,
- and a path toward runtime integration.
