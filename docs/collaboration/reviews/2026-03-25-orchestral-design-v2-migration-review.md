# Review: Orchestral Design v2 Migration

- **Branch**: `codex/orchestral-design-v2-migration-full`
- **Worktree**: `C:\Users\Yi Zhuang\.config\superpowers\worktrees\Orchestral\panel-contract-enforcement`
- **Commits reviewed**:
  - `e0d3eb9` docs: lock orchestral design v2 migration baseline
  - `05f49da` feat: migrate orchestral v2 theme tokens
  - `2f3fefb` feat: migrate orchestral v2 shared shell and widgets
  - `79c3649` feat: migrate orchestral v2 panel templates
  - `9279ab3` docs: promote orchestral design v2 contract
- **Reviewer**: Claude Code
- **Date**: 2026-03-25

---

## Executive Summary

The five-commit migration slice follows the gate ordering defined in `ORCHESTRAL_DESIGN_MIGRATION_DELTA.md`: tokens → shell/widgets → templates → docs. The shared layer is internally consistent. The treatment of Phase 5 (panel-specific polish) as a no-op is **justified** — all concrete panel ViewModels are now routed through shared templates in App.xaml, so per-device XAML files are dead code. No blocking regressions were found.

Two P2 issues need attention before merge: hardcoded color literals in active templates that already have a named token for the value, and orphaned dead XAML panel files that create misleading codebase noise. One structural issue in `DeviceControlPanel.xaml` also warrants a fix.

---

## Findings

### VM-001

- `ID`: `VM-001`
- `Severity`: `P2`
- `Area`: `Theme token usage — hardcoded color literal in active templates`
- `Files`:
  - `App/DevicePanels/Scalar/ScalarSensorPanelTemplate.xaml:182`
  - `App/DevicePanels/Audio/AudioInputPanelTemplate.xaml:166`
  - `App/Theme/Controls.xaml:270` (and 306, 325, 334)

#### Evidence

`#FAF7F6` appears as a hardcoded `Foreground` value on Export/confirm buttons in two active templates:

```xml
<!-- ScalarSensorPanelTemplate.xaml:182 -->
<Button ... Foreground="#FAF7F6" ...>Export CSV</Button>

<!-- AudioInputPanelTemplate.xaml:166 -->
<Button ... Foreground="#FAF7F6" ...>Export CSV</Button>
```

`Controls.xaml` also hardcodes `#FAF7F6` as button foreground in `PrimaryActionButtonStyle` (line 270) and `LiveToggleButtonStyle` (line 334), and `#5A5A5A` in `SoftActionButtonStyle` (line 306) and `ConnectionToggleButtonStyle` (line 325).

`Colors.xaml` defines `AccentForegroundBrush = #FBF5F2`. The values `#FAF7F6` and `#FBF5F2` are visually close but not identical — this is either an intentional two-value split that lacks a named token for the button case, or a copy-paste drift.

#### Expected

All color values used in active shared templates reference named tokens from `Colors.xaml`. If `#FAF7F6` is intentionally distinct from `#AccentForegroundBrush`, it should be a named token such as `ButtonForegroundBrush`.

#### Actual

`#FAF7F6` appears in `Controls.xaml` (the theme layer) and leaks into two active templates directly. The token system is not complete at the leaf level.

#### Why It Matters

The DESIGN_SYSTEM.md promotes this branch as the "live implementation contract." Having named token claims while shipping literal hex values in active templates undermines that contract. If the palette shifts, template-level hardcodes will not update automatically.

#### Recommended Fix

Option A — add a named token and sweep the uses:
```xml
<!-- Colors.xaml -->
<SolidColorBrush x:Key="ButtonPrimaryForegroundBrush" Color="#FAF7F6" />
<SolidColorBrush x:Key="SoftButtonForegroundBrush" Color="#5A5A5A" />
```
Then replace all literal occurrences in `Controls.xaml`, `ScalarSensorPanelTemplate.xaml`, and `AudioInputPanelTemplate.xaml`.

Option B — unify `#FAF7F6` into `AccentForegroundBrush` if the values should be the same. Requires verifying the intent of the two-value split.

---

### VM-002

- `ID`: `VM-002`
- `Severity`: `P2`
- `Area`: `Dead code — orphaned per-device XAML views`
- `Files`:
  - `App/DevicePanels/HuaTeng/HuaTengPanelView.xaml`
  - `App/DevicePanels/Integrated/IntegratedCameraPanelView.xaml`
  - `App/DevicePanels/Pt104/Pt104PanelView.xaml`

#### Evidence

`App.xaml` registers DataTemplates that route all three ViewModels to shared templates:

```xml
<DataTemplate DataType="{x:Type pt104:Pt104PanelViewModel}">
    <scalar:ScalarSensorPanelTemplate />
</DataTemplate>
<DataTemplate DataType="{x:Type huateng:HuaTengPanelViewModel}">
    <camera:CameraPanelTemplate />
</DataTemplate>
<DataTemplate DataType="{x:Type integrated:IntegratedCameraPanelViewModel}">
    <camera:CameraPanelTemplate />
</DataTemplate>
```

None of the per-device XAML files (`HuaTengPanelView.xaml`, `IntegratedCameraPanelView.xaml`, `Pt104PanelView.xaml`) are referenced in `App.xaml`. They are unreachable at runtime.

These files still use pre-migration tokens: `FontBody` on config section labels, `Height="30"` on TextBoxes, explicit `HeightSize` overrides, and `shells:DevicePanelShell.HeaderContent` in `HuaTengPanelView` instead of `TitleContent`.

#### Expected

Dead XAML views are either removed or explicitly marked as legacy/reference material.

#### Actual

Three XAML files with old typography and layout conventions remain in active source directories, indistinguishable from working code. Any developer reading the codebase may assume these files represent the active panel implementation.

#### Why It Matters

Misleading. Future contributors may try to "fix" inconsistencies in these files, apply migration work to them, or assume they affect runtime behavior when they do not. The codebase should be truthful about what runs.

#### Recommended Fix

Delete all three files. Their ViewModels are complete; the shared templates are the rendering path. If any of the per-device XAML contains features not yet present in the shared templates (e.g., `HuaTengPanelView`'s `ChannelOptions`/`TabsSource` wiring), note those as follow-up gaps before deleting.

---

### VM-003

- `ID`: `VM-003`
- `Severity`: `P2`
- `Area`: `DeviceControlPanel layout — orphaned empty row definition`
- `File`: `App/Widgets/DeviceControlPanel.xaml:6-10`

#### Evidence

```xml
<Grid>
  <Grid.RowDefinitions>
    <RowDefinition Height="Auto" />   <!-- Row 0: nothing uses this -->
    <RowDefinition Height="*" />      <!-- Row 1: main content grid -->
    <RowDefinition Height="Auto" />   <!-- Row 2: diagnostics -->
  </Grid.RowDefinitions>

  <Grid Grid.Row="1"> ... </Grid>       <!-- ConfigurationContent, LifecycleContent, ActionsContent -->
  <ContentPresenter Grid.Row="2" ...>  <!-- DiagnosticsContent -->
</Grid>
```

Row 0 of the outer Grid has `Height="Auto"` but no element is placed in it. Row 1 holds the main configuration/lifecycle/actions content.

#### Expected

Every row definition is occupied, or is documented as a reserved future slot.

#### Actual

An unreferenced `Height="Auto"` row sits at the top of the panel, adding zero-height dead space.

#### Why It Matters

If `Height="Auto"` is zero the visual impact is nil, but the intent is unclear and it is a structural oddity in what is now the shared widget used by every device panel. If any padding/margin is later applied to Row 0, it would produce unexpected layout behavior.

#### Recommended Fix

If Row 0 was intended as a future `HeaderContent` slot, add the corresponding DependencyProperty and ContentPresenter. Otherwise remove it: reduce to a two-row grid (Row 0 = main content, Row 1 = diagnostics).

---

### VM-004

- `ID`: `VM-004`
- `Severity`: `P3`
- `Area`: `Theme token usage — hardcoded corner radius in DevicePanelShell title bar`
- `File`: `App/Shell/DevicePanelShell.xaml:17`

#### Evidence

```xml
<Border ...
        CornerRadius="11,11,0,0"
        ... >
```

`Radii.xaml` defines `RadiusWindowInner = 11`. The value `11` is correct but hardcoded directly rather than referenced.

#### Expected

```xml
CornerRadius="{Binding Source={StaticResource RadiusWindowInner},
              Converter={...TopCornersConverter}}"
```
or an explicit token such as `RadiusTitleBar = "11,11,0,0"`.

#### Actual

Hardcoded `"11,11,0,0"` in the title bar border. If `RadiusWindowInner` is changed, the title bar does not track.

#### Why It Matters

Low risk now, but adds a known divergence point. The DESIGN_SYSTEM.md states all radii should reference the token system.

#### Recommended Fix

Add `<CornerRadius x:Key="RadiusTitleBar">11,11,0,0</CornerRadius>` to `Radii.xaml` and reference it. Alternatively accept as known deviation and annotate with a comment.

---

### VM-005

- `ID`: `VM-005`
- `Severity`: `P3`
- `Area`: `Controls.xaml — one hardcoded ComboBox chevron color not tokenized`
- `File`: `App/Theme/Controls.xaml:132`

#### Evidence

```xml
<Path ...
      Stroke="#6A7170"
      ... />
```

This is the ComboBox dropdown chevron. The value `#6A7170` is visually close to `TextMutedBrush = #6B655D` but not identical. No named token matches it.

#### Expected

A named token is used for the chevron stroke, or `TextMutedBrush` is used directly if the values should be unified.

#### Actual

Single hardcoded color in the ComboBox template inside `Controls.xaml` itself.

#### Why It Matters

Minor. Controls.xaml is the theme layer so the impact is contained. Noted for completeness of the token sweep.

#### Recommended Fix

Unify with `TextMutedBrush` if the visual difference is unintentional, or add a named token if the two grays must remain distinct.

---

### VM-006

- `ID`: `VM-006`
- `Severity`: `P3`
- `Area`: `Docs — DESIGN_SYSTEM.md does not list all tokens in Colors.xaml`
- `File`: `docs/architecture/DESIGN_SYSTEM.md` (Color Roles section)

#### Evidence

`Colors.xaml` defines two tokens not listed in DESIGN_SYSTEM.md's Color Roles section:
- `GridLineBrush = #E7E0D5`
- `GridEdgeBrush = #DDD6CA`

Both are used in `PlottingWindow.xaml` and related chart widgets.

#### Expected

The Color Roles section of DESIGN_SYSTEM.md is exhaustive — it documents every token in Colors.xaml.

#### Actual

Two plot-surface tokens are silently omitted.

#### Why It Matters

Minor documentation completeness gap. Anyone adding a new chart widget would not find `GridLineBrush` by reading the doc.

#### Recommended Fix

Add to DESIGN_SYSTEM.md Color Roles section:
```
- `GridLineBrush = #E7E0D5`  (plot grid lines)
- `GridEdgeBrush = #DDD6CA`  (plot border/edge lines)
```

---

## Active Panel Set Verified

`App.xaml` registers five DataTemplates. All five routes to shared templates were confirmed:

| ViewModel | Template | Template migrated? |
|-----------|----------|--------------------|
| `Pt104PanelViewModel` | `ScalarSensorPanelTemplate` | Yes — `FontMicro` labels, `RadiusControl`/`RadiusPanel`, no hardcoded colors except VM-001 |
| `HuaTengPanelViewModel` | `CameraPanelTemplate` | Yes — same token usage |
| `IntegratedCameraPanelViewModel` | `CameraPanelTemplate` | Yes |
| `HyperCamPanelViewModel` | `CameraPanelTemplate` | Yes (new — no per-device XAML, by design) |
| `IntegratedMicrophonePanelViewModel` | `AudioInputPanelTemplate` | Yes (new — `FontMicro`, `RadiusControl`, same token system) |

No active template-backed panel has direct `FontBody` label usage or hardcoded structural colors except the `#FAF7F6` issue in VM-001.

---

## Task 5 No-Op Assessment

**Justified.** Phase 5 of the migration delta ("panel-specific polish") was scoped to applying tailored visual refinements after the shared system moved. The three concrete panel views (`HuaTengPanelView`, `Pt104PanelView`, `IntegratedCameraPanelView`) are dead code — their ViewModels are all rendered through shared templates. There is no runtime path through these files, so migrating their `FontBody` labels would be noise. The correct follow-up action is deletion (VM-002), not migration.

---

## Docs Truthfulness Assessment

| Document | Assessment |
|----------|------------|
| `DESIGN_SYSTEM.md` | Mostly accurate. Token values match `Colors.xaml`. Typography roles match `Typography.xaml`. Radius system matches `Radii.xaml`. Two undocumented tokens (VM-006). Does not disclose the remaining hardcoded values in `Controls.xaml` (VM-001) — minor. |
| `WIDGET_CATALOG.md` | Accurate. Widget list matches what is implemented. Composite widget roles are correct. `LifecycleActionRow` documented correctly. |
| `ORCHESTRAL_DESIGN_MIGRATION_DELTA.md` | Accurate. Gate ordering was followed in the commits. Phase 5 rationale is consistent with what the branch actually did. |
| `DEVICE_WINDOW_IMPLEMENTATION_GUIDE.md` | Not audited in detail — outside migration scope. |

---

## Testing Performed

| Action | Result |
|--------|--------|
| `dotnet build` (non-App projects) | Passed — Core, Runtime, Devices: 0 errors, 0 warnings |
| `dotnet build` (full solution) | File-lock error only — app was running (PID 72848 holds DLL). No compilation errors. |
| `dotnet test` | Not run — App DLL locked by running process |
| Reviewed all 5 migration commits by file diff | Confirmed gate order followed |
| Verified all active App.xaml DataTemplate routes | All 5 ViewModels route to shared migrated templates |
| Inspected all active template XAML for token usage | FontMicro on config labels ✓, RadiusControl/RadiusPanel ✓, except VM-001 |
| Cross-checked Colors.xaml token values against DESIGN_SYSTEM.md | Match, plus 2 undocumented tokens (VM-006) |
| Searched for hardcoded hex colors in non-theme XAML | Found #FAF7F6 in 2 active templates (VM-001) |
| Inspected DeviceControlPanel.xaml structure | Found orphaned row definition (VM-003) |

---

## Merge Readiness Assessment

**Not yet — two P2 items need resolution.**

| Finding | Severity | Blocks merge? |
|---------|----------|---------------|
| VM-001: `#FAF7F6` in active templates + Controls.xaml | P2 | Yes — theme contract is incomplete |
| VM-002: Dead XAML panel files | P2 | Yes — misleads codebase readers |
| VM-003: DeviceControlPanel empty row | P2 | Yes — structural defect in shared widget |
| VM-004: Hardcoded CornerRadius in shell | P3 | No |
| VM-005: Hardcoded ComboBox chevron color | P3 | No |
| VM-006: Two undocumented tokens in doc | P3 | No |

**After VM-001 through VM-003 are resolved, the five migration commits represent a coherent, correctly-gated slice that can merge.**

---

## Answers to the Three Questions

**1. Are the five migration commits merge-worthy as a coherent slice?**

Structurally yes — the gate ordering (tokens → shell/widgets → templates → docs) was followed correctly. The internal consistency of the shared layer is high. Three P2 issues (VM-001 through VM-003) must be resolved first. After those fixes, the slice is clean enough to merge.

**2. Did the migration correctly stop at the shared-system layer, or is there still necessary panel-specific cleanup?**

The migration correctly stopped at the shared layer. The concrete panel XAML files that have old typography are dead code — their ViewModels are all routed through shared templates. No panel-specific cleanup is necessary for the migration to be functionally complete. What remains is deletion of the dead files (VM-002), not migration of them.

**3. Are any docs now overstating what is truly implemented?**

Mild overstatement in DESIGN_SYSTEM.md: the Color Roles section omits `GridLineBrush` and `GridEdgeBrush`, and does not acknowledge the remaining hardcoded values in `Controls.xaml`. The doc claims a fully tokenized system but `#FAF7F6`, `#5A5A5A`, and `#6A7170` remain as literal hex in the theme layer. These are contained within `Controls.xaml` itself, not leaked broadly, but the doc should either add named tokens for them or note them as known remaining literals.
