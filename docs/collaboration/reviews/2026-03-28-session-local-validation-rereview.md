# Session-Local Validation Re-Review

**Reviewer:** Claude (external review agent)
**Date:** 2026-03-28
**Branch:** `codex/runtime-io-microphone`
**Scope:** re-review of fixes for SLV-001 through SLV-005 from `2026-03-28-session-local-validation-review.md`

---

## Verification of Original Findings

### SLV-001 (P2): HuaTeng Connect button not gated on draft validation

**Status: Fixed.**

`CanToggleConnection` now reads:

```csharp
public bool CanToggleConnection => !_isConnecting &&
                                   (IsConnected || TryBuildDraftCaptureSettings(out _).IsValid);
```

The `IsConnected ||` guard is correct: when connected, the button acts as Disconnect and should not depend on draft validity. All four draft property setters (`_draftSelectedColorTone`, `_draftSelectedTriggerMode`, `_draftExposureInput`, `_draftFrameRateInput`) now raise `OnPropertyChanged(nameof(CanToggleConnection))`. No remaining issue.

### SLV-002 (P2): HuaTeng BuildCaptureSettings uses applied fields, not drafts

**Status: Partially fixed. One remaining issue (see SLV-R01 below).**

The `ApplySettingsCoreAsync` path was correctly converted to use `TryBuildDraftCaptureSettings`. However, `BuildCaptureSettings()` itself was not rewritten to delegate to `TryBuildDraftCaptureSettings` as recommended. It still reads applied fields (`_selectedPixelFormat`, `_exposureInput`, etc.) at line 892. Two call sites still use it:
- `ConnectAsync` (line 685): `await session.ConnectAsync(BuildCaptureSettings())`
- Detailed-settings apply (line 603): `await _session.ApplySettingsAsync(BuildCaptureSettings())`

The integrated camera and microphone panels both rewrote `BuildCaptureSettings` to delegate to `TryBuildDraftCaptureSettings` + `ThrowIfInvalid`. HuaTeng did not.

### SLV-003 (P3): Pt104Session.ValidateConfiguration not static

**Status: Fixed.**

Changed to `public static SessionValidationResult ValidateConfiguration(Pt104ChannelConfiguration)`. Runtime test exercises the static path directly. No remaining issue.

### SLV-004 (P3): ControlCenterSession.ValidateCommand not static

**Status: Fixed.**

Changed to `public static SessionValidationResult ValidateCommand(ControlCenterCommand)`. Panel now calls the static method unconditionally in `TryBuildCommand`, removing the `_session is not null` workaround. No remaining issue.

### SLV-005 (P3): Missing panel test coverage for HuaTeng and integrated camera draft validation

**Status: Partially fixed. One remaining issue (see SLV-R02 below).**

Runtime-layer validation tests were added for all five sessions. Panel-layer tests were added for ControlCenter (`CanApplyCommand_IsFalse_WhenDraftStepCountIsNegative`) and Microphone (`CanApplySettings_IsFalse_WhenDraftWindowIsInvalid`). Panel-layer tests are still missing for HuaTeng and IntegratedCamera -- the two panels the original finding specifically called out.

---

## New / Remaining Findings

### SLV-R01

- `ID`: `SLV-R01`
- `Severity`: `P2`
- `Area`: `HuaTeng BuildCaptureSettings still bypasses draft validation`
- `File`: `platform/src/ExperimentalControlPlatform.App/DevicePanels/HuaTeng/HuaTengPanelViewModel.cs:892-908`

#### Evidence

```csharp
private HuaTengCaptureSettings BuildCaptureSettings()
{
    var camera = SelectedCamera ?? throw new InvalidOperationException("No HuaTeng camera selected.");
    // ...
    return new HuaTengCaptureSettings(
        deviceId, camera.Index, camera.DisplayName,
        _selectedPixelFormat,           // applied, not draft
        _selectedTriggerMode,           // applied, not draft
        _selectedColorTone,             // applied, not draft
        ParseExposure(_exposureInput),  // applied, not draft
        ParseFrameRate(),               // applied
        ToCaptureRegion(_appliedRoiPixels ?? GetAppliedRoiPixels()));
}
```

Compare with the integrated camera panel (line 871):

```csharp
private IntegratedCameraCaptureSettings BuildCaptureSettings()
{
    var validation = TryBuildDraftCaptureSettings(out var settings);
    validation.ThrowIfInvalid();
    return settings!;
}
```

And the microphone panel (line 1215):

```csharp
private MicrophoneCaptureSettings BuildCaptureSettings()
{
    var validation = TryBuildDraftCaptureSettings(out var settings);
    validation.ThrowIfInvalid();
    return settings!;
}
```

#### Expected

`BuildCaptureSettings` should delegate to `TryBuildDraftCaptureSettings` + `ThrowIfInvalid`, matching the other two panels.

#### Actual

`BuildCaptureSettings` is a separate code path that bypasses draft validation and reads applied (pre-edit) field values. `ConnectAsync` (line 685) and the detailed-settings apply path (line 603) both call it, so both connect/apply with stale values.

#### Why It Matters

This is the same silent-fallback-to-stale-values pattern from SLV-002. The `CanToggleConnection` gate (SLV-001) was fixed, so invalid drafts now disable Connect. But if the operator enters a *valid-but-different* draft value and clicks Connect, the session still receives the old applied values, not the draft. The gate passes but the payload is wrong.

#### Recommended Fix

Rewrite `BuildCaptureSettings` to delegate to `TryBuildDraftCaptureSettings` + `ThrowIfInvalid`, identical to the pattern in the other two panels. This fixes both remaining call sites at once.

---

### SLV-R02

- `ID`: `SLV-R02`
- `Severity`: `P3`
- `Area`: `Missing panel-layer draft-validation tests for HuaTeng and IntegratedCamera`
- `File`: `platform/tests/ExperimentalControlPlatform.App.Tests/`

#### Evidence

Panel-layer tests exist for:
- `ControlCenterPanelViewModelTests.CanApplyCommand_IsFalse_WhenDraftStepCountIsNegative`
- `IntegratedMicrophonePanelViewModelTests.CanApplySettings_IsFalse_WhenDraftWindowIsInvalid`

No equivalent panel-layer tests exist for:
- `HuaTengPanelViewModel` (no test file in App.Tests)
- `IntegratedCameraPanelViewModel` (no draft-validation test in App.Tests)

Runtime-layer tests were added for all sessions, which is good. But the panel-layer wiring (property -> `TryBuildDraftCaptureSettings` -> `CanApplySettings`/`CanToggleConnection`) is untested for two of four panels.

#### Expected

At least one panel-layer test per device type asserting that `CanApplySettings` (or `CanToggleConnection`) becomes false when a draft field is set to an invalid value.

#### Actual

Two of four panels lack panel-layer draft-validation tests. This was the original SLV-005 finding; the fix addressed the control center and microphone but not the two panels the finding highlighted.

#### Why It Matters

The HuaTeng panel in particular still has the `BuildCaptureSettings` divergence (SLV-R01). A panel-layer test would catch if the `TryBuildDraftCaptureSettings` wiring breaks or was never correctly applied to a code path.

#### Recommended Fix

Add at least one `CanApplySettings_IsFalse_WhenDraftIsInvalid` test for each untested panel. Can be combined with the SLV-R01 fix.

---

## Summary

| ID | Severity | Status | Description |
|----|----------|--------|-------------|
| SLV-001 | P2 | **Closed** | HuaTeng Connect button not gated on draft validation |
| SLV-002 | P2 | **Partially fixed** | HuaTeng BuildCaptureSettings still reads applied fields (see SLV-R01) |
| SLV-003 | P3 | **Closed** | Pt104Session.ValidateConfiguration not static |
| SLV-004 | P3 | **Closed** | ControlCenterSession.ValidateCommand not static |
| SLV-005 | P3 | **Partially fixed** | Panel tests added for 2/4 panels (see SLV-R02) |

| ID | Severity | Status | Description |
|----|----------|--------|-------------|
| SLV-R01 | P2 | **Open** | HuaTeng BuildCaptureSettings still bypasses draft path |
| SLV-R02 | P3 | **Open (deferrable)** | Missing panel-layer tests for HuaTeng and IntegratedCamera |

## Merge Readiness

**Not yet merge-ready.** SLV-R01 is a P2 that preserves the silent-fallback-to-stale-values pattern in two HuaTeng code paths (`ConnectAsync`, detailed-settings apply). The fix is mechanical: rewrite `BuildCaptureSettings` to delegate to `TryBuildDraftCaptureSettings` + `ThrowIfInvalid`, matching the integrated camera and microphone panels. SLV-R02 is deferrable but recommended to land alongside SLV-R01.

Once SLV-R01 is addressed, the slice is merge-ready.
