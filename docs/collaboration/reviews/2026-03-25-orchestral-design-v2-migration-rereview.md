# Re-Review: Orchestral Design v2 Migration — Fix Commit 5afaa16

- **Branch**: `codex/orchestral-design-v2-migration-full`
- **Worktree**: `C:\Users\Yi Zhuang\.config\superpowers\worktrees\Orchestral\panel-contract-enforcement`
- **Commit reviewed**: `5afaa16` fix: resolve v2 migration review findings
- **Prior review**: `docs/collaboration/reviews/2026-03-25-orchestral-design-v2-migration-review.md`
- **Reviewer**: Claude Code
- **Date**: 2026-03-25

---

## Executive Summary

All six findings from the prior review (VM-001 through VM-006) are fully resolved. The fix commit addressed each issue with the correct approach: new named tokens in `Colors.xaml` and `Radii.xaml`, token references in `Controls.xaml` and `DevicePanelShell.xaml`, structural fix in `DeviceControlPanel.xaml`, deletion of all three dead XAML view files, and documentation completeness update in `DESIGN_SYSTEM.md`.

One new P3 issue was introduced: `ButtonPrimaryForegroundBrush` was added to the Accent section of `DESIGN_SYSTEM.md` when it semantically belongs in State/Action. This is non-blocking.

**The migration branch is merge-ready.**

---

## Finding Resolution Status

### VM-001 — Hardcoded `#FAF7F6` in active templates and Controls.xaml

**Status: Resolved**

Two new named tokens were added to `Colors.xaml`:

```xml
<SolidColorBrush x:Key="ButtonPrimaryForegroundBrush" Color="#FAF7F6" />
<SolidColorBrush x:Key="SoftButtonForegroundBrush" Color="#5A5A5A" />
```

`Controls.xaml` was updated to replace all four literal occurrences:
- `PrimaryActionButtonStyle` line 270: `Foreground="#FAF7F6"` → `{StaticResource ButtonPrimaryForegroundBrush}`
- `SoftActionButtonStyle` line 306: `Foreground="#5A5A5A"` → `{StaticResource SoftButtonForegroundBrush}`
- `ConnectionToggleButtonStyle` line 325: `Foreground="#5A5A5A"` → `{StaticResource SoftButtonForegroundBrush}`
- `LiveToggleButtonStyle` line 334: `Foreground="#FAF7F6"` → `{StaticResource ButtonPrimaryForegroundBrush}`

Both active template occurrences were also fixed:
- `ScalarSensorPanelTemplate.xaml`: Export CSV button now uses `{StaticResource ButtonPrimaryForegroundBrush}`
- `AudioInputPanelTemplate.xaml`: Export button now uses `{StaticResource ButtonPrimaryForegroundBrush}`

No hardcoded color literals remain in active templates outside of `Colors.xaml`.

---

### VM-002 — Dead per-device XAML view files

**Status: Resolved**

All six dead files were deleted:
- `DevicePanels/HuaTeng/HuaTengPanelView.xaml`
- `DevicePanels/HuaTeng/HuaTengPanelView.xaml.cs`
- `DevicePanels/Integrated/IntegratedCameraPanelView.xaml`
- `DevicePanels/Integrated/IntegratedCameraPanelView.xaml.cs`
- `DevicePanels/Pt104/Pt104PanelView.xaml`
- `DevicePanels/Pt104/Pt104PanelView.xaml.cs`

The code-behind files were correctly deleted alongside their XAML counterparts. The active `App.xaml` DataTemplate routes remain untouched and continue routing all five ViewModels through shared templates.

---

### VM-003 — DeviceControlPanel orphaned empty row definition

**Status: Resolved**

The outer Grid in `DeviceControlPanel.xaml` was restructured:

Before: three-row outer grid with Row 0 empty, Row 1 main content, Row 2 diagnostics.

After: two-row outer grid:
- Row 0 (`Height="*"`): main configuration/lifecycle/actions grid
- Row 1 (`Height="Auto"`): `DiagnosticsContent` ContentPresenter

Within the inner grid, the layout was also tightened:
- Row 0: `ConfigurationContent`
- Row 1 (`Height="Auto"`): `LifecycleContent` with visibility collapse trigger
- Row 2 (`Height="Auto"`): `ActionsContent`

The inner Row 2 was changed from `Height="*"` to `Height="Auto"`, which is the correct behavior for a stacked action group that should not consume remaining space.

The structural defect is fully resolved. Every row definition is now occupied.

---

### VM-004 — Hardcoded CornerRadius in DevicePanelShell title bar

**Status: Resolved**

`Radii.xaml` now defines:

```xml
<CornerRadius x:Key="RadiusTitleBar">11,11,0,0</CornerRadius>
```

`DevicePanelShell.xaml` was updated:

```xml
CornerRadius="{StaticResource RadiusTitleBar}"
```

The title bar now tracks `RadiusTitleBar` from the token system. If the window inner radius changes, a single token update propagates correctly.

---

### VM-005 — Hardcoded ComboBox chevron color

**Status: Resolved**

`Colors.xaml` now defines:

```xml
<SolidColorBrush x:Key="ComboBoxChevronBrush" Color="#6A7170" />
```

`Controls.xaml` was updated:

```xml
<Path ... Stroke="{StaticResource ComboBoxChevronBrush}" ... />
```

The chevron is now fully tokenized. The value `#6A7170` is distinct from `TextMutedBrush = #6B655D` and is correctly preserved as a separate named token rather than unified.

---

### VM-006 — Two undocumented tokens in DESIGN_SYSTEM.md

**Status: Resolved**

`DESIGN_SYSTEM.md` was updated to document all new and previously undocumented tokens under the State/Action section:

```
- `SoftButtonForegroundBrush = #5A5A5A`
- `ComboBoxChevronBrush = #6A7170`
- `GridLineBrush = #E7E0D5`  (plot grid lines)
- `GridEdgeBrush = #DDD6CA`  (plot border / edge lines)
```

`GridLineBrush` and `GridEdgeBrush` are now documented. The Color Roles section is complete.

---

## New Finding

### VMR-001

- `ID`: `VMR-001`
- `Severity`: `P3`
- `Area`: `Docs — ButtonPrimaryForegroundBrush placed in wrong section of DESIGN_SYSTEM.md`
- `File`: `docs/architecture/DESIGN_SYSTEM.md` (Color Roles → Accent section)

#### Evidence

`ButtonPrimaryForegroundBrush = #FAF7F6` was added to the Accent subsection of Color Roles:

```
### Accent
...
- `AccentForegroundBrush = #FBF5F2`
- `ButtonPrimaryForegroundBrush = #FAF7F6`
```

`ButtonPrimaryForegroundBrush` is not an accent color. It is the foreground for `DarkButtonBrush`-family buttons (primary, live-toggle). It belongs under State/Action alongside `DarkButtonBrush`, `SoftButtonForegroundBrush`, and `ComboBoxChevronBrush`.

#### Expected

`ButtonPrimaryForegroundBrush` appears under the State/Action section, co-located with the button background tokens it pairs with.

#### Actual

It appears under Accent, adjacent to `AccentForegroundBrush`, implying it is an accent-derived foreground when it is not.

#### Why It Matters

Low risk, but a developer scanning the Accent section will infer incorrect pairing intent. The section structure is the semantic guide for token use.

#### Recommended Fix

Move `ButtonPrimaryForegroundBrush` from the Accent block to the State/Action block in `DESIGN_SYSTEM.md`, adjacent to `DarkButtonBrush`.

---

## Regression Check

No regressions were introduced by the fix commit. Verified:

- Active templates (`ScalarSensorPanelTemplate.xaml`, `CameraPanelTemplate.xaml`, `AudioInputPanelTemplate.xaml`) were not structurally changed beyond the `ButtonPrimaryForegroundBrush` substitution.
- `App.xaml` DataTemplate routes are unchanged — all five ViewModels still route to shared templates.
- `DevicePanelShell.xaml` changes were limited to the `RadiusTitleBar` substitution.
- `DeviceControlPanel.xaml` restructure is purely layout — no bindings, styles, or content presenters were removed.
- Deleted files were confirmed dead code at the time of prior review. Deletion does not affect any active rendering path.

---

## Merge Readiness Assessment

**Merge-ready.**

| Finding | Prior Severity | Status |
|---------|---------------|--------|
| VM-001: Hardcoded `#FAF7F6` in active templates + Controls.xaml | P2 | Resolved |
| VM-002: Dead XAML panel files | P2 | Resolved |
| VM-003: DeviceControlPanel empty row | P2 | Resolved |
| VM-004: Hardcoded CornerRadius in shell | P3 | Resolved |
| VM-005: Hardcoded ComboBox chevron color | P3 | Resolved |
| VM-006: Two undocumented tokens in doc | P3 | Resolved |
| VMR-001: ButtonPrimaryForegroundBrush in wrong doc section | P3 (new) | Open — non-blocking |

All P2 findings are closed. The one new P3 (VMR-001) is a documentation placement issue and does not block merge.

---

## Answers to the Three Questions

**1. Which prior findings are fully resolved?**

All six: VM-001 through VM-006. Every finding was addressed with the correct mechanism — named token additions, StaticResource substitutions, structural layout fix, dead file deletion, and doc completeness update.

**2. Did the fix introduce any new regression or cleanup issue?**

No regressions. One new P3 doc placement issue (VMR-001): `ButtonPrimaryForegroundBrush` was added to the Accent section of `DESIGN_SYSTEM.md` instead of State/Action. This does not affect runtime behavior or token correctness.

**3. Is the migration branch now merge-ready as a coherent slice?**

Yes. The five original migration commits plus this fix commit represent a complete, internally consistent migration: tokens are named and documented, the shell and widget layer uses them, the active templates use them, dead code has been removed, and the architecture docs reflect the live state. The branch can merge.
