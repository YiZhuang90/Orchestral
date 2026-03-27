# AI-Guided Hardware Integration Spec

## 1. Purpose

This document defines the `AI-guided hardware integration` workflow for Orchestral.

The goal of this workflow is to let a user attach a device to a PC, provide only basic identifying information, and have Orchestral attempt to discover how to integrate that device with minimal manual effort.

The workflow is intentionally called `AI-guided`, not `AI-automatic`, because Orchestral must be able to:

- search and infer,
- generate and test,
- retry intelligently,
- adapt the UI integration path,
- and then stop honestly and guide the user when the required information is still missing.

## 2. Core Product Intention

This workflow exists to reduce the burden of:

- finding the right manual,
- identifying the communication path,
- determining initialization and read/write behavior,
- building the first test harness,
- linking the device to an appropriate implementation window,
- and documenting the device integration process.

It does not assume that AI will always succeed. It assumes that AI should do as much as it can before asking the user for more.

## 3. Inputs

The workflow should begin from minimal user input, such as:

- device name,
- model name,
- rough device category,
- how the device is physically connected,
- and any known hints such as channel number or sensor type.

Example:

- `PT100 RTD`
- `PT-104 data logger`
- `channel 4`
- `connected to the PC through the PT-104`

## 4. Expected Outputs

If successful, the workflow should produce:

- a likely device identity,
- a likely protocol/driver path,
- a likely execution architecture,
- an initialization strategy,
- a read/write strategy if relevant,
- test code,
- a minimal test harness,
- a device definition draft,
- a protocol definition draft,
- a suitable implementation-window mapping,
- a runtime-session mapping,
- a verification plan,
- a handover verification record,
- and device integration documentation.

If unsuccessful after bounded retries, it should produce:

- a failure summary,
- a list of missing facts,
- a guide for what the user should look for next,
- and a record of what was already tried.

## 5. Search Policy

Orchestral should search in the following order:

### 5.1 First priority: official or vendor sources

Examples:

- vendor manuals,
- official SDK documentation,
- official API references,
- official driver downloads,
- official user guides.

This is the default first pass because it gives the best chance of correct protocol and initialization information.

If no official documentation is already provided by the user, Orchestral should actively try to find official documentation before relying on community or broader-web material.

This means it should attempt to locate at least one of the following:

- official manual,
- official SDK guide,
- official API reference,
- official driver page,
- official user guide,
- or official support knowledge base entry.

Only when that search fails, or when the discovered official material does not cover the relevant problem, should the workflow lean on lower-priority sources.

### 5.2 Second priority: GitHub and Stack Overflow

Examples:

- open-source integrations,
- wrapper libraries,
- example code,
- troubleshooting threads,
- common usage patterns.

This is the second pass because it often reveals how people actually get the device working, especially when vendor docs are incomplete or difficult to use.

### 5.3 Third priority: broader web

Examples:

- blog posts,
- forum discussions,
- lab notes,
- mirrored manuals,
- archived troubleshooting pages.

This should only be used when the first two source classes are insufficient.

## 5.4 Problem-solving source discipline

Once Orchestral has found an official manual, SDK guide, API reference, or vendor documentation for the device, that documentation becomes the first place to return when problems appear.

This rule applies to:

- initialization failures,
- timing or throughput mismatches,
- trigger-mode behavior,
- ROI or alignment constraints,
- state-model confusion,
- parameter semantics,
- and any unexpected device-side behavior.

The operating order should be:

1. re-check the official documentation for the relevant behavior,
2. revise the implementation to match the documented model,
3. verify the result on the real device,
4. only then use community or external methods when the official documentation is silent, ambiguous, incomplete, or contradicted by real hardware evidence.

External methods should not become the default first reaction once official documentation is available.

## 6. Trust Model

All externally discovered facts should be treated as one of:

- `verified by official source`,
- `supported by community evidence`,
- `inferred but unverified`.

Orchestral should explicitly track that distinction.

Protocol or driver assumptions should not be treated as trusted until they are:

- supported by strong documentation,
- or verified by a successful device test.

## 7. Workflow

## 7.1 Stage A: identity and connection inference

Orchestral should try to determine:

- what the device most likely is,
- whether the PC talks to it directly or through another box,
- whether it is likely SDK-based, serial, USB, TCP, Modbus, DAQ, or something else,
- and whether the named sensor is actually the device to integrate, or only a sensor behind another device.

Example:

- a `PT100 RTD` is not itself the PC-connected device,
- the `PT-104` is the integration target,
- the PT100 is part of the measurement chain and capability contract.

## 7.2 Stage B: protocol and driver hypothesis

Orchestral should then infer:

- likely communication protocol,
- likely driver or SDK requirement,
- likely execution architecture,
- likely initialization sequence,
- likely read path,
- likely channel configuration requirements,
- and likely validation signals.

The execution-architecture choice should be explicit, not implicit.

The detailed selection rules are defined in:

- [EXECUTION_ARCHITECTURE_SELECTION.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/architecture/EXECUTION_ARCHITECTURE_SELECTION.md)

At the workflow level, the key rule is:

- start with the fastest safe validation path,
- but escalate to a native or in-process path when the device class is performance-sensitive or the first path is not truthful enough.

When official documentation has already been found, Stage B should be anchored to that documentation first.

This means Orchestral should prefer:

- vendor-defined mode semantics over guessed semantics,
- vendor-defined timing limits over inferred timing expectations,
- vendor-defined parameter ranges and alignment rules over UI-side approximations,
- and vendor-defined initialization order over trial-and-error reordering.

## 7.3 Stage C: implementation-window archetype inference

Before generating the final onboarding artifacts, Orchestral should also infer the most suitable UI archetype for the device.

The decision should be based on:

- device category,
- whether the device produces scalar time-series data,
- whether it is mainly an actuator,
- whether it is an imaging device,
- whether it is mainly a protocol/debug tool,
- and whether it mixes multiple roles.

The detailed archetype and widget mapping rules are defined in:

- [DEVICE_ARCHETYPE_MAPPING.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/architecture/DEVICE_ARCHETYPE_MAPPING.md)
- [DEVICE_RUNTIME_IO_ARCHITECTURE.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/architecture/DEVICE_RUNTIME_IO_ARCHITECTURE.md)
- [TIMING_AND_SYNCHRONIZATION_STRATEGY.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/architecture/TIMING_AND_SYNCHRONIZATION_STRATEGY.md)

This stage should not create a one-off UI by default. It should first try to map the device onto the existing widget system and window archetypes, then infer the default parameter surface for the chosen archetype.

This stage should also explicitly decide between:

- consuming an existing device-class template,
- consuming the high-level general device shell when no class template exists yet,
- or creating a new class template when the device introduces a reusable new interaction family.

The default order should be:

1. existing device-class template,
2. high-level general device template and shell,
3. new class template,
4. one-off panel only as a last resort.

For each integrated device, this workflow should also produce a concrete archetype record using:

- [DEVICE_ARCHETYPE_RECORD_TEMPLATE.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/architecture/DEVICE_ARCHETYPE_RECORD_TEMPLATE.md)

## 7.4 Stage D: generated integration attempt

Orchestral should generate:

- a small test program,
- a minimal test harness,
- any required setup notes,
- a draft device/protocol record,
- and a draft UI-mapping record.

This generated code should aim to answer a narrow question first:

- can the PC initialize the device and read one valid sample?

For performance-sensitive devices, the generated code should also answer a second narrow question as early as possible:

- is the chosen execution architecture good enough for truthful operation and UI integration?

## 7.5 Stage E: permission-gated test execution

Before running anything that touches the actual device, Orchestral should ask permission.

The first live test should be narrow and safe:

- initialize device,
- read status or channel metadata if possible,
- acquire one sample or a very short stream,
- stop cleanly.

## 7.6 Stage F: UI auto-adaptation and implementation-window linking

Once the protocol path and first live behavior are verified, Orchestral should adapt the UI integration path.

This means:

- choose the most suitable implementation-window archetype,
- choose the matching widget stack,
- decide whether an existing device window can be reused with new bindings,
- decide whether a new composite widget is required,
- and generate or update the device-to-window mapping artifacts.

The default rule should be:

- reuse an existing shell and composite widgets whenever possible,
- create a new widget only when the device introduces a genuinely new interaction surface.

The stronger template rule should be:

- if a suitable integration template already exists for the device class, consume it,
- if no suitable class template exists, consume the high-level general template and shared shell,
- only after that should Orchestral introduce a new class template or a new widget family.

For example:

- a temperature logger should map to the scalar sensor window pattern,
- a camera should map to a camera/imaging window pattern,
- a valve controller should map to an actuator window pattern.

This stage links hardware integration to UI integration instead of treating them as separate later tasks.

The linked implementation window should also expose structured outputs for downstream consumers such as data display, processing, diagnostics, and recording.

The detailed panel IO rules are defined in:

- [INTEGRATION_PANEL_IO_CONTRACT.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/architecture/INTEGRATION_PANEL_IO_CONTRACT.md)

UI adaptation must also preserve semantic honesty.

Orchestral should distinguish:

- requested setpoint or target rate,
- measured delivered rate,
- display or render refresh rate,
- and hardware capability limits.

The UI should not collapse these into one ambiguous value.

When troubleshooting a UI or hardware-behavior mismatch during this stage, Orchestral should go back to the official documentation first before trying ad hoc fixes.

## 7.7 Stage G: failure analysis and retry

If the test fails, Orchestral should:

- inspect the failure,
- revisit the official documentation first if one exists,
- classify the likely reason,
- search again using the new evidence,
- revise the integration attempt,
- and try again.

This loop should run at most `3` rounds.

Performance failure should count as a meaningful failure class, even if the device technically “works.”

Examples:

- camera preview works but capture rate is far below expected because the bridge path is too slow
- triggered acquisition works but timing semantics are mislabeled in the UI
- ROI works visually but the hardware-normalized ROI differs from the requested ROI

## 7.8 Stage H: handover-grade verification

Before handing a newly integrated device panel to the user, Orchestral should run a final verification pass on the actual implementation window.

This rule should apply:

- after the first implementation handover,
- and after every troubleshooting pass that claims the panel or integration is fixed.

The detailed operational standard is defined in:

- [TEST_BEFORE_HANDOVER.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/architecture/TEST_BEFORE_HANDOVER.md)
- [REVIEW_PROTOCOL.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/collaboration/REVIEW_PROTOCOL.md) for file-backed reviewer and implementer communication when external review or testing is involved

At the workflow level, the key requirement is:

- verify real UI behavior,
- verify the corresponding internal data flow,
- and record what was truly confirmed before handover.

## 7.9 Stage I: guided escalation

If Orchestral still cannot produce a working initialization/read path after three rounds, it should stop the automatic attempt and guide the user.

That guidance should include:

- what was attempted,
- what failed,
- what is still unknown,
- what manual, screenshot, SDK file, or setting page the user should find,
- and exactly why that missing information matters.

## 8. Retry Model

The retry policy should be explicit:

- `Round 1`: official/vendor-first search and first generated test
- `Round 2`: refined search using failure evidence, including GitHub/Stack Overflow if needed
- `Round 3`: broader web search and last bounded integration attempt
- `After Round 3`: stop, summarize, and guide the user

This keeps the workflow ambitious but bounded.

If the chosen architecture works functionally but fails the performance or truthfulness checks, Orchestral should revise the architecture, not just the test script.

If official documentation exists, each retry should record whether the new hypothesis is:

- directly documented,
- a documented interpretation,
- or an external fallback because the documentation did not cover the issue.

## 9. Device-Test Principles

The first generated test should be:

- minimal,
- observable,
- reversible,
- and low-risk.

Examples:

- read one sensor value,
- query one channel configuration,
- verify device presence,
- fetch one metadata record.

The test should avoid high-energy or unsafe actions unless the device category explicitly allows a harmless live command.

For high-rate or imaging devices, the first generated tests should also separate:

- one-shot validation,
- persistent acquisition validation,
- and UI preview validation.

These are not the same question and should not be conflated.

For handover-quality validation, Orchestral should follow the test-and-evidence rules in:

- [TEST_BEFORE_HANDOVER.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/architecture/TEST_BEFORE_HANDOVER.md)

This means the workflow should answer not only:

- `can the device work?`

but also:

- `does the implementation window truthfully represent what the device is doing?`

## 10. User Guidance Model

When Orchestral cannot proceed, it should not ask vague questions like:

- `Can you provide more details?`

Instead, it should ask for targeted missing evidence such as:

- model number label,
- screenshot of device manager entry,
- vendor manual PDF,
- SDK install folder,
- serial port name,
- baud rate page,
- channel wiring page,
- example output format.

This is a core part of the workflow. The AI must know how to guide the user toward the missing technical facts.

## 11. Generated Artifacts

The workflow should ultimately be able to generate or update:

- device integration notes,
- protocol notes,
- execution-architecture notes,
- a device definition draft,
- a protocol definition draft,
- a capability contract draft,
- a test harness,
- a troubleshooting log,
- a handover verification report,
- an implementation-window mapping,
- and a widget/archetype selection record.

The implementation-window mapping should explicitly record:

- which template was consumed,
- whether that template was device-class-specific or only the high-level general template,
- what parts were reused unchanged,
- and what new reusable modules, if any, had to be introduced.

The integration artifacts should also explicitly record:

- which runtime session owns the live device behavior,
- whether the panel is acting as a session client,
- what output-settings surface exists, if any,
- whether the device requires a device-level emergency stop path,
- and what `Apply and exit` means for that device's idle or waiting-for-call state.

These artifacts should become part of project memory.

The handover verification report should capture:

- tested UI actions,
- monitored internal signals,
- hardware responses,
- truthfulness checks for displayed values,
- and any known gaps that still require manual confirmation.

For runtime-backed devices, the handover record should also capture:

- whether the panel is subscribing to structured session outputs instead of owning hardware loops directly,
- whether output settings publish truthful structured fields,
- whether device-level emergency stop behavior was verified when relevant,
- and whether `Apply and exit` leaves the device in the expected idle or waiting-for-call state.

For devices with hardware constraints, the artifacts should also record:

- alignment rules,
- ROI/grid restrictions,
- timing semantics,
- mode semantics such as snap, triggered live, and continuous live,
- and any UI normalization rules required to keep the interface truthful.

## 12. Position In Orchestral

This workflow should sit between:

- user intent,
- online/manual search,
- protocol inference,
- code generation,
- device testing,
- UI adaptation,
- handover-grade verification,
- and artifact generation.

It is not itself the runtime. It is a build-and-onboarding workflow that supports the runtime and the device implementation window system.

For high-performance devices, it must also decide when the onboarding path should stop being “good enough for first contact” and become a production-grade in-process integration.

## 13. Safety Boundary

The operating rule should remain:

- AI can infer,
- AI can generate,
- AI can propose,
- humans approve live tests,
- runtime and safety rules govern actual execution.

The AI-guided hardware integration workflow should never silently become a direct unsafe control path.

## 14. First Validation Case

The first validation case for this workflow should be a real hardware path from the turbulence system, starting from only minimal human-provided identification.

A good example is:

- `PT100 RTD`
- `PT-104`
- `channel 4`

This is a strong first case because it tests whether Orchestral can correctly infer that:

- the PT100 is the sensing element,
- the PT-104 is the PC-facing integration target,
- channel configuration matters,
- a scalar sensor implementation window is the correct UI target,
- and a successful initial result means a verified temperature read rather than only device detection.

## 15. Success Criteria

This workflow is successful when Orchestral can:

- turn minimal device descriptions into a credible integration attempt,
- search in a disciplined source order,
- generate useful first-pass test code,
- learn from failures for up to three rounds,
- return to official documentation first when problems arise and only use external methods when the documentation does not solve or cover the issue,
- choose an execution architecture that is not only functional but truthful enough for the device class,
- map the device onto a suitable implementation window,
- consume an existing class template when possible, or otherwise consume the high-level general template instead of jumping straight to one-off UI,
- reuse or adapt the existing widget stack instead of defaulting to one-off UI,
- verify the actual panel through both user-facing interaction and internal data-flow checks before handover,
- repeat that verification after troubleshooting rather than only after the first implementation,
- and, when it still fails, guide the user toward exactly the missing information instead of stalling vaguely.
