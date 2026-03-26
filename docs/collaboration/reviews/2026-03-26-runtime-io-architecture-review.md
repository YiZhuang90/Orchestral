# Review: Device Runtime IO Architecture Doc Set

- **Files reviewed**:
  - `docs/architecture/DEVICE_RUNTIME_IO_ARCHITECTURE.md`
  - `docs/architecture/INTEGRATION_PANEL_IO_CONTRACT.md`
  - `docs/architecture/DEVICE_PANEL_CONTRACT.md`
  - `docs/architecture/DEVICE_ARCHETYPE_MAPPING.md`
  - `docs/architecture/AI_GUIDED_HARDWARE_INTEGRATION_SPEC.md`
- **Cross-referenced against**:
  - `platform/src/ExperimentalControlPlatform.App/DevicePanels/Contracts/IIntegrationPanelViewModel.cs`
  - `IntegrationPanelDataOutput.cs`, `IntegrationPanelStatusOutput.cs`, `IntegrationPanelDiagnosticsOutput.cs`, `IntegrationPanelAppliedSettingsOutput.cs`, `IntegrationPanelSessionEndOutput.cs`
  - `platform/src/ExperimentalControlPlatform.Runtime/RuntimeCoordinator.cs`
  - `platform/src/ExperimentalControlPlatform.Devices/Audio/WasapiMicrophoneService.cs`
  - `docs/legacy-knowledge/DEVICE_INVENTORY.md`
- **Reviewer**: Claude Code
- **Date**: 2026-03-26

---

## Executive Summary

The doc set is architecturally coherent at the concept level. The three-layer separation (data plane / command plane / session ownership), the device / endpoint / session scope model, and the Acquisition / Controlled / Hybrid classification are all well-reasoned and internally consistent across the five documents.

**However, three specification gaps would produce ambiguous or incompatible implementations if coding starts now:**

1. `IDeviceOutputPort<T>` — the core subscription mechanism — is named but not typed. Whether it delivers via events, `IObservable<T>`, `Channel<T>`, or callbacks determines every threading, marshaling, and consumer-coupling decision downstream. This must be decided before any session interface is written.

2. `DiagnosticsOutput` is defined as a log-event struct in the architecture doc (with `Timestamp`, `Severity`, `Category`, `Message`) but implemented as a last-known-state snapshot (with `LastCommand`, `LastHardwareResponse`). These are structurally different models. The architecture doc will mislead anyone implementing the runtime session.

3. Session lifecycle ownership — who creates, owns, and disposes `IDeviceSession` instances — is described in principle but not resolved. The doc does not say whether the `RuntimeCoordinator`, `App.xaml.cs`, or the panel itself owns session lifetime. Without this, the first session implementation will make an assumption that constrains every future device.

These are not blocking in the sense that the doc set cannot be used — but they are mandatory resolution points before the first line of `IDeviceSession` is written.

One additional concern: the "first controlled runtime pilot" points to an Arduino Uno R4 device that does not exist in the hardware inventory. The turbulence system's existing serial controller is a better-anchored controlled pilot.

---

## Findings

---

### Blocking Issues

---

#### RI-001

- `ID`: `RI-001`
- `Severity`: `P1`
- `Area`: `DiagnosticsOutput model mismatch between architecture doc and implementation`
- `Files`: `DEVICE_RUNTIME_IO_ARCHITECTURE.md §"Diagnostics output"`, `IntegrationPanelDiagnosticsOutput.cs`

**Issue**

DEVICE_RUNTIME_IO_ARCHITECTURE defines DiagnosticsOutput as a log-event-like struct:

```
Timestamp, Severity, Category, Message,
LastCommand, LastHardwareResponse, LastStateTransition, ErrorCode
```

The implementation (`IntegrationPanelDiagnosticsOutput`) is a state snapshot:

```csharp
public string? LastCommand { get; init; }
public string? LastHardwareResponse { get; init; }
public string? LastError { get; init; }
public string? LastStateTransition { get; init; }
public string? LastValidationResult { get; init; }
```

These are two architecturally different patterns:

- A **log-event struct** is append-only. Each new diagnostic event is a new object in a stream. Consumers see a time-ordered sequence. This is what `Timestamp`, `Severity`, `Category`, and `Message` imply.
- A **state snapshot** is replace-on-update. There is one current record reflecting the latest known state. This is what `LastCommand`, `LastError` implies.

INTEGRATION_PANEL_IO_CONTRACT aligns with the snapshot model (no `Severity`, no `Message`, no `Timestamp`). The implementation also uses the snapshot model. The architecture doc diverges by adding event-log fields.

**Why it matters**

When Codex implements `IDeviceOutputPort<DiagnosticsOutput>`, the choice of model determines the port semantics:
- If it is a snapshot port, it publishes a single current record that consumers read on demand.
- If it is a log-event stream, it pushes a sequence of entries that consumers buffer.

Implementing both in parallel without a decision will produce incompatible ports. Correcting later means refactoring all consumers.

**Recommended fix**

Decide: DiagnosticsOutput is a **state snapshot** (aligned with the implementation and INTEGRATION_PANEL_IO_CONTRACT). Remove `Timestamp`, `Severity`, `Category`, `Message`, and `ErrorCode` from the architecture doc's DiagnosticsOutput suggestion, and instead document a separate `DiagnosticsEvent` log for the event-stream case. This keeps the snapshot model for status display and separates the append log for audit/history — two different needs, two different output ports.

---

#### RI-002

- `ID`: `RI-002`
- `Severity`: `P1`
- `Area`: `IDeviceOutputPort<T> subscription mechanism undefined`
- `File`: `DEVICE_RUNTIME_IO_ARCHITECTURE.md §"Core runtime abstractions"`, §"How panels should relate to the runtime"`

**Issue**

The architecture doc names `IDeviceOutputPort<T>` and states that panels should "subscribe to structured outputs." No interface body or delivery mechanism is specified.

The choice of delivery mechanism is not an implementation detail — it is the most architecturally consequential decision in the entire runtime I/O layer:

| Mechanism | Implication |
|---|---|
| C# event (`event Action<T>`) | Thread-unsafe; consumers must marshal to UI thread; no backpressure |
| `IObservable<T>` (Rx.NET) | Strong composition; requires System.Reactive dependency; scheduler-aware threading |
| `Channel<T>` / `ChannelReader<T>` | Async-native; push/pull model; backpressure possible; consumer must drain |
| Polling callback | Simplest; requires consumer to drive; no real-time push |

If two implementations choose different mechanisms (e.g., camera uses events, microphone uses `Channel<T>`), consumers cannot be written generically. The panel subscription code diverges per device.

**Why it matters**

All five output port types (`DataOutput`, `StatusOutput`, `DiagnosticsOutput`, `AppliedSettingsOutput`, `SessionEndOutput`) share `IDeviceOutputPort<T>`. The mechanism chosen for this single interface propagates to every consumer written against it, including future recording, analysis, and comparison modules. Changing it after the first consumer is written requires refactoring all consumers.

**Recommended fix**

Add a §"Output port delivery contract" section to DEVICE_RUNTIME_IO_ARCHITECTURE that specifies:
1. The chosen delivery mechanism (recommendation: `event Action<T>` for the snapshot ports — Status, Diagnostics, AppliedSettings, SessionEnd — and `event Action<T>` or `Channel<T>` for the DataOutput stream, with `Dispatcher.InvokeAsync` marshaling handled in the session, not the consumer).
2. Threading contract: is the callback raised on the producer's thread, or on the UI thread?
3. Subscription lifecycle: when must a consumer subscribe / unsubscribe to avoid memory leaks?

---

#### RI-003

- `ID`: `RI-003`
- `Severity`: `P1`
- `Area`: `DeviceId missing from DataOutput in implementation; misaligned across docs`
- `Files`: `DEVICE_RUNTIME_IO_ARCHITECTURE.md §"Data output"`, `IntegrationPanelDataOutput.cs`

**Issue**

DEVICE_RUNTIME_IO_ARCHITECTURE lists `DeviceId` as a suggested field in DataOutput. The implementation (`IntegrationPanelDataOutput`) has no `DeviceId`. INTEGRATION_PANEL_IO_CONTRACT §3.1 also omits it.

When a downstream consumer — a recording module, a comparison module, a computation pipeline — receives DataOutput from the session layer, it must know which device produced it to route, tag, or correlate the data. Without `DeviceId` in the output record, the consumer must track source identity out-of-band (e.g., by knowing which port it subscribed to), which breaks the goal of "downstream modules can consume device outputs directly without device-specific knowledge."

Additionally, `MeasuredRate` (architecture doc) vs `CaptureRate` (INTEGRATION_PANEL_IO_CONTRACT and implementation) is a naming divergence for the same field. A developer writing a consumer from the architecture doc would reference `.MeasuredRate` which does not exist.

**Why it matters**

The session-level output is the published surface for all downstream consumers. If `DeviceId` is absent, recorded data loses provenance when multiple devices are running simultaneously. This is a first-class concern for the turbulence experiment which runs cameras and temperature sensors in parallel.

**Recommended fix**

1. Add `DeviceId` (typed as `DeviceSessionId`) to `IntegrationPanelDataOutput` and document it consistently across both docs.
2. Align `MeasuredRate` / `CaptureRate` — pick one name and use it everywhere. `CaptureRate` (the implementation name) is preferred since it matches the actual code.

---

### Semantic Mismatches

---

#### RI-004

- `ID`: `RI-004`
- `Severity`: `P2`
- `Area`: `"First controlled runtime pilot" points to a device not in the hardware inventory`
- `File`: `DEVICE_RUNTIME_IO_ARCHITECTURE.md §6 "Example B: Arduino Uno R4 WiFi with LED matrix"`, §8 "Adoption order"`

**Issue**

The doc says:

> Use this as the first controlled runtime pilot.

Arduino Uno R4 WiFi is not listed in `DEVICE_INVENTORY.md` — the turbulence system's hardware set contains cameras, PT-104, a flow-rate controller, and a serial trigger device. The Arduino is illustrative but not a real integration target in the current scope.

Pointing to a non-existent device as the "first pilot" carries two risks:

1. **Scope inflation**: Codex may begin an Arduino integration to exercise the controlled runtime path, adding a net-new hardware dependency instead of validating the architecture on existing hardware.
2. **Disconnected from real safety requirements**: The turbulence system's existing serial controller (which sets the pump/flow rate) is a real controlled device with real stop conditions. Using it as the controlled pilot would simultaneously validate the architecture AND produce a deliverable that the reference experiment needs.

**Recommended fix**

Replace the Arduino recommendation with:

> Use the turbulence system's serial flow-rate / pump controller as the first controlled runtime pilot. It is already in the hardware inventory, has a defined command protocol, and produces acknowledgement feedback — making it an appropriate first case for the controlled device runtime path.

Arduino remains a valid illustration. Mark it as an illustrative example rather than the implementation target.

---

#### RI-005

- `ID`: `RI-005`
- `Severity`: `P2`
- `Area`: `Session lifecycle ownership undefined — creates first-implementation coupling risk`
- `File`: `DEVICE_RUNTIME_IO_ARCHITECTURE.md §"How routing should work"`

**Issue**

The doc describes `IDeviceSessionRegistry` but does not answer:

- Who calls `Create(deviceIdentity)` on the registry?
- Who calls `Dispose` / `Close` on the session?
- Is session creation triggered by the panel's Connect command, or by the RuntimeCoordinator starting a run?
- Can multiple sessions for the same device identity exist simultaneously?
- Does `IDeviceSessionRegistry` live inside `RuntimeCoordinator`, or is it a peer?

Currently, `RuntimeCoordinator` is a simple run-state machine (Start / RequestStop). It has no concept of device sessions. If the first session implementation assumes that `RuntimeCoordinator` creates sessions, the RuntimeCoordinator must be extended. If the panel creates sessions, sessions exist outside the runtime coordinator's knowledge — which breaks centralized stop logic.

**Why it matters**

The "automatic stop" requirement (Stage 3 of the roadmap) depends on the RuntimeCoordinator being able to stop all active device sessions on a stop signal. If session creation is panel-triggered and the registry is app-local, the coordinator cannot reach sessions without a global registry reference. This coupling decision must be made before any session is written.

**Recommended fix**

Add a §"Session ownership and lifecycle" section specifying:
1. Sessions are created by the `IDeviceSessionRegistry`, which is injected into panels.
2. The registry is held by the application host (App.xaml.cs), not by RuntimeCoordinator.
3. On `RuntimeCoordinator.RequestStop()`, the coordinator calls `registry.StopAll()` or iterates active sessions and calls `StopLive()`.
4. Session identity persists across Connect/Disconnect within a panel session; a new session is created only when the panel opens.

---

#### RI-006

- `ID`: `RI-006`
- `Severity`: `P2`
- `Area`: `Input contract in INTEGRATION_PANEL_IO_CONTRACT §1 presented at same maturity level as output contract`
- `File`: `INTEGRATION_PANEL_IO_CONTRACT.md §1 "Input Contract"`

**Issue**

§1 defines five inputs:

```
DeviceIdentity, CapabilityModel, SavedSettings, DefaultSettings, TemplateContext
```

None of these are implemented. The current ViewModels have hardcoded settings, no external `DeviceIdentity` injection, and no `SavedSettings` loader. This is appropriate for the current stage — but the doc presents the input contract with the same format, section level, and prescriptive tone as the output contract, which is substantially implemented.

A developer reading the doc in order would reasonably try to define `IDeviceIdentityProvider`, `ICapabilityModel`, and a settings persistence layer before they implement the runtime session — all of which are out of scope for the first runtime I/O pass.

**Why it matters**

The input contract is a Stage 2+ concern (it requires a settings persistence system and a device identity registry that don't exist yet). Treating it as equal to the output contract inflates scope for the first implementation sprint.

**Recommended fix**

Add an explicit status marker to §1:

> **Status**: Architecture intent. Not yet implemented. The input contract requires a settings persistence layer and device identity registry that are out of scope for the first runtime I/O implementation. Panels currently use hardcoded defaults. Input contract implementation is a follow-on concern.

---

#### RI-007

- `ID`: `RI-007`
- `Severity`: `P2`
- `Area`: `DEVICE_ARCHETYPE_MAPPING.md has no decision rule for Hybrid vs two separate Acquisition + Controlled sessions`
- `File`: `DEVICE_ARCHETYPE_MAPPING.md §7 "Hybrid Device"`

**Issue**

The Hybrid archetype section says "use when the device clearly spans multiple roles" and "start from the dominant archetype." But for the turbulence domain's realistic cases (e.g., a pump controller that both sets flow rate AND reads back measured flow), the choice between:

- One Hybrid session owning both planes, vs.
- Two sessions — one Controlled, one Acquisition — for the same physical device

is not addressed. Both are defensible architecturally. Choosing the wrong one leads to either an overly complex single session or an artificial device identity split.

The Arduino example in §7 of DEVICE_RUNTIME_IO_ARCHITECTURE has the same gap: "one shared device at device scope, with multiple endpoints behind it" — but which endpoint belongs to which plane?

**Recommended fix**

Add a decision rule: "A device should be modeled as a single Hybrid session when its data output and command acceptance share the same connection lifecycle and protocol. It should be modeled as two sessions only when the data acquisition path and the command path use physically different communication channels or have independent connection state."

---

### Non-blocking Improvements

---

#### RI-008

- `ID`: `RI-008`
- `Severity`: `P3`
- `Area`: `Microphone example diagnostics overstatement`
- `File`: `DEVICE_RUNTIME_IO_ARCHITECTURE.md §5`

**Issue**

"diagnostics such as unsupported raw mode" is listed as a microphone diagnostic. `WasapiMicrophoneService` doesn't detect or report unsupported raw modes — it throws `NotSupportedException` on non-PCM-16 formats. The diagnostic breadcrumb doesn't exist.

**Recommended fix**

Replace with "unsupported format exception" or "PCM-only constraint" to match the actual implementation.

---

#### RI-009

- `ID`: `RI-009`
- `Severity`: `P3`
- `Area`: `DEVICE_ARCHETYPE_MAPPING.md §10 "Output Artifact" is a rule with no template and no home`
- `File`: `DEVICE_ARCHETYPE_MAPPING.md §10`

**Issue**

§10 says: "For each integrated device, Orchestral should record: chosen archetype, chosen widget stack, default parameter surface, ownership model, panel output model..." There is no template for this artifact and no location specified.

Without a concrete template and a defined location (e.g., `docs/devices/<device-name>/archetype-record.md`), this rule will not be followed consistently, and the information will exist only in commit messages.

**Recommended fix**

Create a `docs/architecture/DEVICE_ARCHETYPE_RECORD_TEMPLATE.md` with the exact fields listed in §10 as headings. Codex fills one in per device as part of Stage E/F in the AI-guided integration workflow.

---

#### RI-010

- `ID`: `RI-010`
- `Severity`: `P3`
- `Area`: `DEVICE_RUNTIME_IO_ARCHITECTURE §3 command list mixes lifecycle commands with data commands`
- `File`: `DEVICE_RUNTIME_IO_ARCHITECTURE.md §3 "Command families"`

**Issue**

The command list includes:

```
Connect, Disconnect, ApplySettings, ApplyAndExit, CloseWithoutApply,
ReadOnce, SnapFrame, StartLive, StopLive, SetSetpoint, SendActuatorCommand, EmergencyStop
```

This mixes two command classes:
- **Lifecycle commands**: `Connect`, `Disconnect`, `ApplySettings`, `ApplyAndExit`, `CloseWithoutApply` — these manage the session lifecycle and map to `SupportedLifecycleActions` in `IIntegrationPanelViewModel`.
- **Data/operational commands**: `ReadOnce`, `SnapFrame`, `StartLive`, `StopLive`, `SetSetpoint`, `SendActuatorCommand`, `EmergencyStop` — these drive runtime behavior.

These have different semantics, different callers (lifecycle commands are user-initiated; operational commands may be automation-initiated), and potentially different authorization rules. Mixing them in one flat command family will complicate the `IDeviceCommandPort` interface.

**Recommended fix**

Split into two groups in §3: `Lifecycle commands` (panel-facing) and `Operational commands` (runtime-facing). The distinction is also useful for the AI-guided workflow when deciding which commands are safe to exercise in Stage E (first live test).

---

## Architectural Coherence Assessment

The five-document set is coherent at the conceptual level. Reading them together, the ownership model is consistent:

- `DEVICE_PANEL_CONTRACT` → defines the shell structure and state scope model
- `INTEGRATION_PANEL_IO_CONTRACT` → defines what the panel emits and the lifecycle actions
- `DEVICE_RUNTIME_IO_ARCHITECTURE` → defines the session layer that the panel should become a client of
- `DEVICE_ARCHETYPE_MAPPING` → maps devices onto the right pattern
- `AI_GUIDED_HARDWARE_INTEGRATION_SPEC` → defines how new devices enter the system

The doc references are tight: each doc cross-links to the others correctly, and the same three-scope model (device / endpoint / session) is used consistently throughout all five.

The gap is between the architecture doc's future-tense session model and the current implementation, which still has all state inside the ViewModel. The docs do not acknowledge this delta explicitly — a developer reading them cannot tell which parts are implemented and which are planned.

---

## Answers to the Three Questions

### 1. Is this doc set strong enough to serve as the source of truth for implementing the first runtime I/O layer?

**Conditionally yes, with three mandatory pre-work items:**

a. **Resolve RI-001** (DiagnosticsOutput model) — decide: snapshot or event stream. Clarify the architecture doc.

b. **Resolve RI-002** (IDeviceOutputPort<T> delivery mechanism) — add a concrete delivery contract section. Without it, the first two session implementations will diverge.

c. **Resolve RI-005** (session lifecycle ownership) — answer who creates sessions and how RuntimeCoordinator interacts with the registry before any session code is written.

Once those three questions are answered in writing and added to DEVICE_RUNTIME_IO_ARCHITECTURE, the doc set is a credible source of truth.

### 2. What is the biggest architectural risk if implementation starts from these docs now?

**The subscription mechanism (RI-002).**

If the first session implementation uses raw C# events for `IDeviceOutputPort<T>`, the threading contract becomes: "the consumer marshals to the UI thread." Every subsequent consumer — recording module, comparison module, computational pipeline — must implement that same marshaling. If a second developer uses `IObservable<T>` for a different device, consumers cannot be written generically, and the output port interface fractures.

This risk is uniquely high because:
1. It is invisible to the compiler — both approaches satisfy the `IDeviceOutputPort<T>` interface.
2. Fixing it requires touching every consumer already written.
3. It compounds with the microphone's background thread (WASAPI raises `DataAvailable` on a capture thread), making the wrong choice immediately dangerous if a consumer updates UI state directly on the callback.

### 3. What should be the first pilot implementation after approval?

**`IntegratedMicrophoneSession : IDeviceSession`**, backed by the existing `WasapiMicrophoneService`.

Reasons:

- The hardware adapter is fully implemented and tested (`WasapiMicrophoneService`, 12 passing unit tests on `MicrophoneAnalysis`).
- The command set is small and well-defined: Connect, Disconnect, StartLive, StopLive, ApplySettings — exactly the five commands needed to exercise both the lifecycle and operational command paths.
- The data output is well-typed (`MicrophoneFrame` with RMS, peak, clipping, channel data) — ideal for verifying that downstream consumers can consume structured data without knowing panel internals.
- The `IntegratedMicrophonePanelViewModel` exists but is not yet wired into the runtime session. Migrating it from "VM owns all state" to "VM subscribes to session" is the exact architecture migration the docs describe, and it exercises the migration path on a bounded, hardware-available case.
- The microphone has no safety implications — it can be started and stopped freely during development.

The outcome of this pilot should be:
1. A working `IDeviceSession` + `IDeviceSessionRegistry` implementation.
2. `IntegratedMicrophonePanelViewModel` rewritten as a session client (the "panel is a consumer" migration).
3. One downstream consumer — a `MicrophoneDataLogger` or the `DiagnosticsDialogViewModel` — consuming session output without panel coupling.

That combination validates all three planes: data, command, and session ownership.

---

## Summary Table

| ID | Severity | Area | Merge-blocking? |
|----|----------|------|-----------------|
| RI-001 | P1 | DiagnosticsOutput model mismatch (log-event vs snapshot) | **Yes — must resolve before IDeviceSession is written** |
| RI-002 | P1 | IDeviceOutputPort<T> delivery mechanism unspecified | **Yes — must resolve before IDeviceSession is written** |
| RI-003 | P1 | DeviceId missing from DataOutput; MeasuredRate vs CaptureRate divergence | **Yes — must align before implementing output records** |
| RI-004 | P2 | Arduino as first controlled pilot conflicts with hardware inventory | No — but recommendation should be corrected |
| RI-005 | P2 | Session lifecycle ownership undefined | No — but must resolve before coding RuntimeCoordinator integration |
| RI-006 | P2 | Input contract overstates implementation maturity | No — add status marker |
| RI-007 | P2 | No Hybrid vs two-sessions decision rule | No |
| RI-008 | P3 | Microphone example diagnostics overstatement | No |
| RI-009 | P3 | Output Artifact rule has no template or home | No |
| RI-010 | P3 | Command families mix lifecycle and operational commands | No |
