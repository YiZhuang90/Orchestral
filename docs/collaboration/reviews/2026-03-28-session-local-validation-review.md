# Session-Local Validation Review

**Reviewer:** Claude (external review agent)
**Date:** 2026-03-28
**Branch:** `codex/runtime-io-microphone`
**Scope:** session-local validation slice as defined in `docs/development/2026-03-28-session-local-validation-plan.md`

---

## Finding SLV-001

- `ID`: `SLV-001`
- `Severity`: `P2`
- `Area`: `HuaTeng panel draft gating`
- `File`: `platform/src/ExperimentalControlPlatform.App/DevicePanels/HuaTeng/HuaTengPanelViewModel.cs:418`

### Evidence

`CanToggleConnection` is defined as:

```csharp
public bool CanToggleConnection => !_isConnecting;
```

Compare with the microphone panel (line 331):

```csharp
public bool CanToggleConnection => SelectedDevice is not null &&
                                   !_isBusy &&
                                   !IsLiveReading &&
                                   TryBuildDraftCaptureSettings(out _).IsValid;
```

And the integrated camera panel (line 356):

```csharp
public bool CanToggleConnection => SelectedCamera is not null &&
                                   !_isBusy &&
                                   !IsLivePreviewing &&
                                   TryBuildDraftCaptureSettings(out _).IsValid;
```

### Expected

`CanToggleConnection` on HuaTeng should gate on draft validation like the other two panels, so the Connect button is disabled when the operator has invalid draft fields (e.g. frame rate = 0, exposure = -1).

### Actual

HuaTeng allows the Connect button to be enabled with any draft state. The `ConnectAsync` method (line 680) calls `BuildCaptureSettings()` which builds from the *applied* fields (`_selectedPixelFormat`, `_exposureInput`, etc.), not the *draft* fields. This means:

1. If the operator edits draft fields to invalid values, Connect remains enabled.
2. `BuildCaptureSettings` uses the last-applied field values, not the draft, so the session connects with stale settings even though the operator just changed them.

### Why It Matters

This is exactly the "silent fallback to stale values" pattern the plan was designed to eliminate. An operator who changes exposure to an invalid value and clicks Connect gets the old exposure applied without feedback. The microphone and integrated camera panels both had this fixed; HuaTeng was missed.

### Recommended Fix

1. Change `CanToggleConnection` to include `TryBuildDraftCaptureSettings(out _).IsValid` (matching the other panels' pattern).
2. Change `ConnectAsync` to build settings via the draft path.

---

## Finding SLV-002

- `ID`: `SLV-002`
- `Severity`: `P2`
- `Area`: `HuaTeng panel BuildCaptureSettings uses applied fields, not drafts`
- `File`: `platform/src/ExperimentalControlPlatform.App/DevicePanels/HuaTeng/HuaTengPanelViewModel.cs:887-903`

### Evidence

```csharp
private HuaTengCaptureSettings BuildCaptureSettings()
{
    // ...
    return new HuaTengCaptureSettings(
        deviceId, camera.Index, camera.DisplayName,
        _selectedPixelFormat,             // applied, not draft
        _selectedTriggerMode,             // applied, not draft
        _selectedColorTone,               // applied, not draft
        ParseExposure(_exposureInput),    // applied, not draft
        ParseFrameRate(),                 // applied
        ...);
}
```

Compare: `TryBuildDraftCaptureSettings` (line 905) correctly reads `_draftSelectedPixelFormat`, `_draftExposureInput`, `_draftFrameRateInput`, etc.

The microphone panel's `BuildCaptureSettings` (line 1215) delegates to `TryBuildDraftCaptureSettings`:

```csharp
private MicrophoneCaptureSettings BuildCaptureSettings()
{
    var validation = TryBuildDraftCaptureSettings(out var settings);
    validation.ThrowIfInvalid();
    return settings!;
}
```

The integrated camera panel's `BuildCaptureSettings` (line 871) uses the same pattern.

### Expected

`BuildCaptureSettings` should delegate to `TryBuildDraftCaptureSettings` + `ThrowIfInvalid`, matching the pattern in the other two panels.

### Actual

`BuildCaptureSettings` is a separate code path that bypasses draft validation entirely and reads applied (pre-edit) values.

### Why It Matters

This is the root cause of SLV-001. `ConnectAsync` calls `BuildCaptureSettings` to get settings for the session, and that method ignores the operator's current draft. The validation gate and the settings source are both wrong, but they are separate fixes.

### Recommended Fix

Rewrite `BuildCaptureSettings` to delegate to `TryBuildDraftCaptureSettings` + `ThrowIfInvalid`, matching the other panels. Delete the duplicate applied-field construction.

---

## Finding SLV-003

- `ID`: `SLV-003`
- `Severity`: `P3`
- `Area`: `Pt104Session.ValidateConfiguration is not static`
- `File`: `platform/src/ExperimentalControlPlatform.Runtime/Pt104Session.cs:595`

### Evidence

All other sessions expose validation as both an instance method and a `static` method so that panel-layer code can validate before session creation:

- `IntegratedMicrophoneSession.ValidateSettings(string, MicrophoneCaptureSettings)` -- static
- `IntegratedCameraSession.ValidateSettings(string, int, settings)` -- static
- `HuaTengCameraSession.ValidateSettings(string, int, settings)` -- static

`Pt104Session.ValidateConfiguration` is instance-only. The method body does not reference instance state.

### Expected

A static overload for consistency with the other sessions and to allow panel-side pre-validation without a session instance.

### Actual

Instance-only. Not currently blocking because the PT-104 panel was not in scope for this slice.

### Why It Matters

When the PT-104 panel is converted to use draft validation (same pattern as the other three panels), a static overload will be needed.

### Recommended Fix

Add a `public static` overload and have the instance method delegate to it. Can be deferred to the PT-104 panel slice.

---

## Finding SLV-004

- `ID`: `SLV-004`
- `Severity`: `P3`
- `Area`: `ControlCenterSession.ValidateCommand is not static`
- `File`: `platform/src/ExperimentalControlPlatform.Runtime/ControlCenterSession.cs:382`

### Evidence

Same pattern as SLV-003. `ValidateCommand` is instance-only but does not use instance state. The control center panel works around this by null-checking `_session` before calling it (line 654 of ControlCenterPanelViewModel.cs):

```csharp
if (_session is not null && !_session.ValidateCommand(command).IsValid)
{
    command = new ControlCenterCommand(false, false, 0);
    return false;
}
```

### Expected

A static overload so the panel can validate even when `_session` is null.

### Actual

When `_session` is null (pre-connect), `TryBuildCommand` skips session-level validation. In practice, the only session rule (`StepCount >= 0`) is already covered by the local parse check (`stepCount < 0` on line 643), so no real gap exists today.

### Why It Matters

If a new session-level validation rule is added, the panel will not reflect it until connected. Low risk currently.

### Recommended Fix

Make `ValidateCommand` static. No urgency.

---

## Finding SLV-005

- `ID`: `SLV-005`
- `Severity`: `P3`
- `Area`: `Missing panel test coverage for HuaTeng and integrated camera draft validation`
- `File`: `platform/tests/ExperimentalControlPlatform.App.Tests/`

### Evidence

The microphone panel has a test: `CanApplySettings_IsFalse_WhenDraftWindowIsInvalid`

The control center panel has: `CanApplyCommand_IsFalse_WhenDraftStepCountIsNegative`

No equivalent tests exist for:
- `IntegratedCameraPanelViewModel` (no draft-validation test in App.Tests)
- `HuaTengPanelViewModel` (no test file in App.Tests at all)

### Expected

At least one panel-layer test per device type asserting that `CanApplySettings` (or `CanToggleConnection`) becomes false when a draft field is set to an invalid value.

### Actual

Two of four panels lack draft-validation panel tests.

### Why It Matters

If a future change breaks the `TryBuildDraftCaptureSettings` wiring, no test will catch it for the camera panels. Particularly important for HuaTeng given SLV-001/SLV-002.

### Recommended Fix

Add at least one `CanApplySettings_IsFalse_WhenDraftIsInvalid` test for each untested panel. Can be combined with the SLV-001/SLV-002 fix.

---

## Summary

| ID | Severity | Status | Description |
|----|----------|--------|-------------|
| SLV-001 | P2 | Open | HuaTeng Connect button not gated on draft validation |
| SLV-002 | P2 | Open | HuaTeng ConnectAsync/BuildCaptureSettings uses applied fields, not drafts |
| SLV-003 | P3 | Open (deferrable) | Pt104Session.ValidateConfiguration not static |
| SLV-004 | P3 | Open (deferrable) | ControlCenterSession.ValidateCommand not static |
| SLV-005 | P3 | Open (deferrable) | Missing panel tests for HuaTeng and integrated camera draft validation |

**Overall assessment:** The shared `SessionValidationResult` contract is clean, minimal, and coherent with the runtime session architecture. The microphone, integrated camera, and control center panels correctly use draft validation to gate both `CanApplySettings` and `CanToggleConnection`. The HuaTeng panel is the outlier -- it was not fully converted and still has the silent-fallback-to-applied-values pattern that the plan targeted. SLV-001 and SLV-002 should be fixed before this slice is marked complete. The P3 findings are deferrable.

**Residual risk:** The runtime validation itself is solid across all five sessions. The risk is concentrated in the HuaTeng panel layer, which is exactly one `BuildCaptureSettings` rewrite and one `CanToggleConnection` fix away from parity with the other panels.
