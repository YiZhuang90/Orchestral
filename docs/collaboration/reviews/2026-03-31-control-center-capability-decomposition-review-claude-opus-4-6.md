Looking at the skill list, I should check if there's a review skill available.

Using the `/review` skill for this task.

---

## Review Findings

### F-01: `WriteCommandAsync` does not publish `FlowTelemetry` capability state on command success

- **Severity**: Medium — silent capability surface gap
- **Area**: Runtime contract completeness
- **File**: `ControlCenterSession.cs`, `WriteCommandAsync` method (around the success path after `_service.SendCommand`)
- **Evidence**: `WriteCommandAsync` publishes `LaserControl` and `PuffActuation` capability states after a successful command write, but does **not** publish a `FlowTelemetry` state update. Meanwhile, `ReadFlowTelemetryAsync` does publish all three implicitly (flow telemetry explicitly, and laser/puff are untouched).
- **Expected**: All three capability surfaces should be consistently updated after every command write, or at minimum the omission should be intentional and documented. A consumer subscribing only to `FlowTelemetry.Changed` would never learn that a command was sent (which may reset hardware counters or affect flow state).
- **Actual**: `FlowTelemetry` is only published from `ReadFlowTelemetryAsync`, `ConnectAsync`, and `DisconnectAsync` — never from `WriteCommandAsync`.
- **Why it matters**: If a command write resets hardware pulse counters or changes flow-affecting state, the `FlowTelemetry` snapshot becomes stale without any signal to downstream consumers. The asymmetry between laser/puff (always published on command) and flow (only published on read) is not self-evident.
- **Recommended fix**: Either publish a `FlowTelemetry` state in `WriteCommandAsync` (even if unchanged, to signal "command written, telemetry may be stale"), or add a code comment documenting the intentional asymmetry (flow telemetry is read-only, not affected by commands).

---

### F-02: `PulseReads` property still exposed alongside `FlowTelemetryReads` — same backing field, two public API names

- **Severity**: Low — API confusion / misleading contract
- **Area**: Public API surface
- **File**: `ControlCenterSession.cs`, lines exposing `PulseReads` and `FlowTelemetryReads`
- **Evidence**: Both properties exist:
  ```csharp
  public IStreamOutputPort<ControlCenterPulseReadback> PulseReads => _flowTelemetryReads;
  public IStreamOutputPort<ControlCenterPulseReadback> FlowTelemetryReads => _flowTelemetryReads;
  ```
- **Expected**: If the intent is to deprecate `PulseReads` in favor of `FlowTelemetryReads`, the old name should be removed or marked obsolete. Keeping both with identical backing creates ambiguity about which is canonical.
- **Actual**: Two properties expose the same stream. The panel VM subscribes to `FlowTelemetryReads`, the derived-state session subscribes to `FlowTelemetryReads`, but `PulseReads` remains callable.
- **Why it matters**: Future consumers may subscribe to `PulseReads` thinking it's a different stream, or may subscribe to both and double-handle events.
- **Recommended fix**: Mark `PulseReads` with `[Obsolete]` pointing to `FlowTelemetryReads`, or remove it if no external consumers depend on it. Similarly, `ReadPulseCountAsync` delegates to `ReadFlowTelemetryAsync` — same treatment applies.

---

### F-03: `ApplyLaserControlAsync` and `ApplyPuffActuationAsync` read last-commanded state from `State.Current` which may be stale or null

- **Severity**: Medium — potential runtime bug
- **Area**: Command construction correctness
- **File**: `ControlCenterSession.cs`, `ApplyLaserControlAsync` and `ApplyPuffActuationAsync`
- **Evidence**:
  ```csharp
  public Task ApplyLaserControlAsync(bool enabled, ...)
  {
      var currentState = State.Current!;
      var command = new ControlCenterCommand(
          PuffEnabled: currentState.LastCommandedPuffEnabled ?? false,
          LaserEnabled: enabled,
          StepCount: currentState.LastStepCount ?? 0);
  ```
  `LastCommandedPuffEnabled` and `LastCommandedLaserEnabled` are nullable on the session state and default to `null` until the first command is applied.
- **Expected**: If `ApplyLaserControlAsync(true)` is called before any prior command, the "other" fields should be populated from a known-safe default, and the fact that no prior puff state exists should be handled explicitly.
- **Actual**: The `?? false` / `?? 0` fallbacks silently construct a command with `PuffEnabled: false, StepCount: 0`. This is probably correct for a fresh session, but if the hardware was in a different state before session start (e.g., from a prior session or manual operation), the capability-scoped method will silently override the other capability's state without the caller knowing.
- **Why it matters**: The capability-scoped API promises "change only this capability" but actually writes all three fields. If the session state is out of sync with hardware reality, the "preserved" fields are wrong. This is an inherent limitation of the shared serial protocol, but the API name `ApplyLaserControlAsync` implies isolation that doesn't exist.
- **Recommended fix**: Add a doc comment or status message noting that capability-scoped commands reconstruct the full serial command from last-known state. Consider logging a warning if `LastCommandedPuffEnabled` is null when `ApplyLaserControlAsync` is called (indicating no prior full command established baseline state).

---

### F-04: `EmergencyStopAsync` calls `RequireConnection()` inside `WriteCommandAsync` but also has its own early-exit connection check — double-checked but asymmetric

- **Severity**: Low — dead code path / confusing control flow
- **Area**: Control flow clarity
- **File**: `ControlCenterSession.cs`, `EmergencyStopAsync`
- **Evidence**: `EmergencyStopAsync` does:
  1. `lock (_syncRoot)` → checks `_connection` → returns early if null
  2. Then calls `WriteCommandAsync` → which calls `RequireConnection()` → which also checks `_connection` under lock

  If step 1 passes (connection exists), step 2 will also pass. But there's a TOCTOU gap: the connection could become null between step 1 and step 2 if `DisconnectAsync` races on another thread.
- **Expected**: Either trust `WriteCommandAsync` to handle the disconnected case (and remove the early check), or pass the connection into `WriteCommandAsync` to avoid the TOCTOU gap.
- **Actual**: Double-checking with a potential race window between checks.
- **Why it matters**: In the emergency-stop path specifically, a race could cause an `InvalidOperationException` from `RequireConnection()` instead of the graceful "ignored because disconnected" message, which is worse UX during an emergency.
- **Recommended fix**: Pass the already-acquired connection reference into `WriteCommandAsync` to close the TOCTOU gap, or move the disconnected-check entirely into `WriteCommandAsync` with a graceful path.

---

### F-05: Panel VM `HandlePulseProduced` still uses old `EndpointId` naming convention internally — but the semantic rename is incomplete

- **Severity**: Low — artifact correctness
- **Area**: Run artifact output
- **File**: `ControlCenterPanelViewModel.cs`, `HandlePulseProduced` handler (around line 605 in the diff)
- **Evidence**: The handler method is still named `HandlePulseProduced` (old naming) but is now wired to `FlowTelemetryReads.Produced`. The endpoint ID was updated to `"flow_telemetry.pulse_count"` and payload type to `"ControlCenterFlowTelemetryPulseCount"`, which is correct.
- **Expected**: Method name should match the new semantic (`HandleFlowTelemetryProduced` or similar), consistent with the rename done in `FlowReynoldsDerivedStateSession`.
- **Actual**: Method name is still `HandlePulseProduced` while the equivalent in `FlowReynoldsDerivedStateSession` was renamed to `HandleFlowTelemetryProduced`.
- **Why it matters**: Inconsistent naming between the two consumers of the same event. Minor, but the review scope emphasizes misleading semantics.
- **Recommended fix**: Rename to `HandleFlowTelemetryPulseProduced` or `HandleFlowTelemetryProduced` for consistency.

---

### F-06: No test coverage for `ApplyPuffActuationAsync` preserving laser state

- **Severity**: Low — test gap
- **Area**: Test coverage
- **File**: `ControlCenterSessionTests.cs`
- **Evidence**: Test `ApplyLaserControlAsync_PublishesLaserCapability_AndPreservesPuffActuationState` exists and verifies that laser-scoped commands preserve puff state. But there is no symmetric test for `ApplyPuffActuationAsync` preserving laser state.
- **Expected**: Symmetric test coverage for both capability-scoped command methods.
- **Actual**: Only the laser→preserves-puff direction is tested.
- **Why it matters**: The puff→preserves-laser path uses the same `State.Current!.LastCommandedLaserEnabled ?? false` pattern and could independently regress.
- **Recommended fix**: Add a test `ApplyPuffActuationAsync_PublishesCapability_AndPreservesLaserState`.

---

### F-07: New capability state types not included in the diff — cannot verify record contracts

- **Severity**: Info — review limitation
- **Area**: Contract verification
- **File**: `ControlCenterLaserCapabilityState.cs`, `ControlCenterPuffActuationCapabilityState.cs`, `ControlCenterFlowTelemetryState.cs`
- **Evidence**: These files are listed as untracked (`??`) in git status but their content is not in the diff.
- **Expected**: The review should verify that these record types have the properties referenced in the session and VM code (`LastCommandedEnabled`, `LastStepCount`, `LastCommandedAtUtc`, `CommandSequence`, `StatusMessage`, `LastPulseCount`, `LastControllerTimestampSeconds`, `LastObservedAtUtc`, `PulseSequence`).
- **Actual**: Cannot verify from the diff alone.
- **Why it matters**: If any property is missing or misnamed, the `with` expressions and constructors will fail at compile time — but the review can't confirm this without seeing the types.
- **Recommended fix**: Include these files in the review diff, or verify they compile cleanly.

---

## Residual Risks

- **Thread safety of capability state reads during command writes**: The capability-scoped commands read `State.Current` without holding `_syncRoot`. If two capability commands race, the "preserve other capability" logic could produce an inconsistent command. This is pre-existing (the old `ApplyCommandAsync` had the same unsynchronized reads) but the new capability-scoped API makes concurrent use more likely.
- **Downstream Reynolds session still uses the old `PulseReads` in its test setup**: The production code was updated to `FlowTelemetryReads`, but if any test helper or mock still exposes only `PulseReads`, it would silently work (same backing field) but mask a real wiring issue.
