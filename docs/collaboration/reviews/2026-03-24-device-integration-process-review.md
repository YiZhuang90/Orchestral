# Review: Device Integration Process

- **Subject**: Codex-proposed new device integration workflow
- **Reviewer**: Claude Code
- **Date**: 2026-03-24
- **Inputs**: Codex process description (8-step workflow), branch `codex/panel-contract-enforcement` implementation evidence (PT-104, IntegratedCamera, HuaTeng panels), existing spec docs

---

## Summary

Codex proposed an 8-step device integration process: official docs, archetype classification, execution architecture choice, state ownership definition, truthful semantics, backend proof, panel binding, and test-before-handover. The sequence is fundamentally sound. This review evaluates it against evidence from the first three integrations and identifies five process gaps that would improve first-shot success rate.

---

## Findings

### DP-001

- `ID`: `DP-001`
- `Severity`: `P1`
- `Area`: `Process gap — no contract boilerplate extraction step`
- `File`: `Pt104PanelViewModel.cs`, `IntegratedCameraPanelViewModel.cs`, `HuaTengPanelViewModel.cs`

#### Evidence

All three panel ViewModels contain ~80 lines of identical code for:
- Five `IntegrationPanel*Output` backing fields and properties
- Five diagnostic tracking fields (`_lastCommand`, `_lastHardwareResponse`, `_lastError`, `_lastStateTransition`, `_lastValidationResult`)
- Six identical helper methods: `RefreshDiagnosticsOutput()`, `SetLastCommand()`, `SetLastHardwareResponse()`, `SetLastError()`, `ClearLastError()`, `SetLastStateTransition()`, `SetLastValidationResult()`
- Identical `HandleLifecycleCommandException()`

This was also flagged as CC-005 (P2) in the contract enforcement review, but in the context of a repeatable process it escalates: every future device integration will copy this boilerplate.

#### Expected

The process includes a step that directs the implementer to inherit shared contract machinery rather than duplicate it.

#### Actual

Step 7 ("Bind it into the panel template") says to expose the five output types but does not mention where the implementation of that machinery comes from. The implicit path is copy-paste from the previous panel.

#### Why It Matters

The fourth device integration will copy the boilerplate a fourth time. Divergence between copies is inevitable — a bug fix to diagnostics tracking in one panel will be missed in others. This directly reduces first-shot success because the implementer must manually verify behavioral consistency across all panels after every change.

#### Recommended Fix

Add step **6.5: "Inherit contract base class"** to the process. Extract `IntegrationPanelViewModelBase : ObservableObject` that owns:
- All five output properties with `SetProperty` backing
- All diagnostic tracking fields and helpers
- `HandleLifecycleCommandException` default behavior
- `RefreshDiagnosticsOutput()`, `RefreshStatusOutput()` (abstract or virtual for device-specific status)

The concrete ViewModel then inherits from this base instead of `ObservableObject`. Step 7 becomes: "inherit `IntegrationPanelViewModelBase`, implement device-specific `BuildStatusOutput()` and `ExecuteLifecycleActionAsync()`."

This must happen before the next device integration.

---

### DP-002

- `ID`: `DP-002`
- `Severity`: `P2`
- `Area`: `Process gap — no error/fault path verification in step 8`
- `File`: `docs/architecture/TEST_BEFORE_HANDOVER.md` (conceptual gap)

#### Evidence

Step 8 lists: click every button, change every input, start/stop live paths, verify internal data flow, verify displayed values match hardware reality. These are all happy-path verifications.

From the contract enforcement review:
- CC-003 found that `AsyncRelayCommand` had no atomic re-entry guard (now fixed with `Interlocked.CompareExchange`)
- CC-004 found that exceptions were silently swallowed when no handler was provided (now fixed with `Debug.WriteLine` fallback)

Both of these would have been caught earlier if the process included explicit fault-path testing.

#### Expected

Step 8 includes a fault-path verification section: hardware disconnect mid-operation, invalid settings submission, button double-click during async work, and exception visibility.

#### Actual

Step 8 only covers normal operation paths. Fault behavior is discovered during code review, not during the integration testing phase.

#### Why It Matters

Hardware devices fail in unpredictable ways — cables disconnect, serial ports hang, SDK calls return error codes. If fault paths aren't tested during integration, the panel may freeze, swallow errors, or leave the user with no feedback. This is especially critical for the PT-104 where the hardware was not available for the branch verification session.

#### Recommended Fix

Add a **"Fault path verification"** subsection to step 8 (or to TEST_BEFORE_HANDOVER.md) with these mandatory checks:

| Test | What to verify |
|------|----------------|
| Disconnect hardware mid-acquisition | Panel transitions to error state, live loop stops, status message updates |
| Send invalid/out-of-range settings | Error surfaces to user via StatusMessage, does not crash |
| Double-click action button during async | Re-entry guard prevents duplicate execution |
| Close panel while live acquisition running | CancellationToken fires, background task terminates cleanly |
| Hardware returns unexpected data format | Exception is caught, diagnostics output records the error |

---

### DP-003

- `ID`: `DP-003`
- `Severity`: `P2`
- `Area`: `Process gap — no state machine boundary test step`
- `File`: Process design (between steps 5 and 6)

#### Evidence

The PT-104 panel's lifecycle semantics are well-designed: `Apply` is only available when `IsConnected && !AnyChannelLiveReading`. This was verified by static code review but could not be verified at runtime because the hardware was unavailable during the branch check.

The camera panels follow a similar pattern: `Apply` requires `IsConnected && !IsLivePreviewing && !IsBusy`.

These state transition rules are encoded implicitly in `CanExecuteLifecycleAction()` methods and various property setters, but there is no explicit state transition table and no unit tests that verify the transitions without hardware.

#### Expected

Each device integration includes a state transition table and corresponding unit tests that verify lifecycle action availability across all states — without requiring hardware.

#### Actual

State transitions are verified by manual UI testing (step 8) which requires hardware. When hardware is unavailable, verification is limited to static code review.

#### Why It Matters

State machine bugs are the most common source of "button does nothing" or "button shouldn't be available" issues. A unit test like `Assert.False(vm.CanExecuteLifecycleAction(Apply))` when `IsConnected = false` costs nothing to run and catches regressions immediately. The PT-104 hardware unavailability during branch verification is exactly the scenario where these tests would have provided confidence.

#### Recommended Fix

Add step **5.5: "Define and test state transitions"** between truthful semantics (step 5) and backend proof (step 6):

1. Write a state transition table for the device:

| State | Connect | Disconnect | Apply | StartLive | StopLive | ReadOnce |
|-------|---------|------------|-------|-----------|----------|----------|
| Disconnected | Yes | No | No | No | No | No |
| Connected+Idle | No | Yes | Yes | Yes | No | Yes |
| Connected+Live | No | No | No | No | Yes | No |
| Connected+Busy | No | No | No | No | No | No |

2. Write unit tests for each cell using a mock/stub driver
3. These tests run without hardware and catch lifecycle regressions in CI

---

### DP-004

- `ID`: `DP-004`
- `Severity`: `P2`
- `Area`: `Process gap — archetype template check is advisory, not enforced`
- `File`: Process design (step 2)

#### Evidence

Step 2 says: "map it to an existing panel/template first" and "if no device-class template exists, fall back to the high-level shell/template instead of inventing a one-off panel."

Currently two shared templates exist:
- `ScalarSensorPanelTemplate` (for PT-104-like devices)
- `CameraPanelTemplate` (for camera devices)

The process does not define what happens if someone creates a third template or a one-off panel. There is no artifact or gate that records the classification decision.

#### Expected

Template selection is a recorded decision with justification. Creating a new template requires explicit rationale.

#### Actual

Template selection is implicit — the implementer picks a template and proceeds. If they create a new template unnecessarily, there's no review checkpoint that catches it.

#### Why It Matters

Template proliferation defeats the purpose of the shared shell architecture. Each new template adds maintenance surface and diverges from the design system. With AI-assisted development (Codex), the implementer may default to generating a new template rather than adapting an existing one, because generation is easier than adaptation.

#### Recommended Fix

Add to step 2: "Record the archetype classification in the device's `devices/{device-name}/docs/INTEGRATION.md` with the selected template and rationale. If no existing template fits, document why and get approval before creating a new one."

The review checklist (CLAUDE.md) should include: "New templates must have documented justification in the device's INTEGRATION.md."

---

### DP-005

- `ID`: `DP-005`
- `Severity`: `P3`
- `Area`: `Process gap — capability registration not connected to panel contract`
- `File`: `platform/src/ExperimentalControlPlatform.Devices/Capabilities/DeviceCapabilityContract.cs`

#### Evidence

The `DeviceCapabilityContract` in `Devices/Capabilities/` defines device capabilities for the runtime layer. The `IIntegrationPanelViewModel` in `App/DevicePanels/Contracts/` defines the panel contract for the UI layer. The integration process (step 7) describes binding to the panel template but does not mention registering the device's capabilities in the capability system.

#### Expected

The process includes a step where the device declares its capabilities so the runtime can match experiments to available hardware.

#### Actual

Capability registration is not mentioned in the 8-step process. The panel contract and the capability contract exist independently.

#### Why It Matters

When the runtime needs to validate that an experiment's required devices are available (step 4 of experiment execution), it queries capabilities. If a new device integration skips capability registration, the device will work in the test panel but not be discoverable by the experiment runtime. This is non-blocking now (runtime experiment matching is not yet implemented) but will become blocking when it is.

#### Recommended Fix

Add step **7.5: "Register device capabilities"** — declare what the device offers (temperature sensing, imaging, actuation, etc.) in the capability system so the runtime can bind experiments to devices. Mark as deferred if the capability matching runtime is not yet built, but include it in the checklist so it's not forgotten.

---

## Strengths

- **Truth-first ordering is correct.** Starting with official docs and truthful semantics before UI prevents building panels around assumptions. The PT-104's careful distinction between staged and applied settings proves this works.
- **Archetype classification prevents one-off panels.** Mapping to existing templates first is the right default. The scalar and camera templates serve well as shared foundations.
- **Execution architecture selection is pragmatic.** "Fastest safe validation path first, escalate if performance-sensitive" avoids premature optimization while keeping the door open.
- **State ownership clarity.** Explicitly defining device-level vs endpoint/channel-level vs session state is critical for multi-channel devices like PT-104. This step alone prevents a class of bugs.
- **Test-before-handover is a real gate.** Having a named checklist (TEST_BEFORE_HANDOVER.md) rather than just "run the tests" is the right structure.

---

## Proposed Process (Revised)

For reference, the full revised process with additions marked:

1. **Identify device truth** — official docs, SDK, protocol manual
2. **Classify the device** — archetype mapping, template selection, **record decision in INTEGRATION.md** *(DP-004)*
3. **Choose execution architecture** — CLI wrapping, Python probe, native SDK, serial client
4. **Define state ownership** — device-level, endpoint-level, session-level
5. **Define truthful semantics** — requested vs applied, capture vs display rate, staged vs applied
5.5. **Define and test state transitions** — state table, unit tests without hardware *(DP-003)*
6. **Implement backend proof** — enumerate, connect, configure, read one sample
6.5. **Inherit contract base class** — `IntegrationPanelViewModelBase`, not copy-paste *(DP-001)*
7. **Bind into panel template** — shell regions, five output types
7.5. **Register device capabilities** — declare capabilities for runtime binding *(DP-005)*
8. **Test before handover** — happy paths **+ fault paths** *(DP-002)*

---

## Assessment

The proposed process is solid and reflects real lessons from the first three integrations. The five gaps identified are all addressable without restructuring the process — they are additions to specific steps, not changes to the sequence.

| Finding | Severity | Blocking? | Effort |
|---------|----------|-----------|--------|
| DP-001: Contract base class extraction | P1 | Yes (before next device) | Medium — extract ~80 lines into base class, update 3 ViewModels |
| DP-002: Fault path verification | P2 | No | Small — add checklist rows to TEST_BEFORE_HANDOVER.md |
| DP-003: State machine boundary tests | P2 | No | Medium — write state tables + unit tests per device |
| DP-004: Template selection enforcement | P2 | No | Small — add recording requirement to step 2 |
| DP-005: Capability registration | P3 | No | Deferred — runtime matching not yet built |

**Recommendation:** Accept the process as-is for the current camera slice (already in progress). Before the next new device integration, implement DP-001 (base class) and DP-002 (fault paths). DP-003 and DP-004 can be adopted incrementally.
