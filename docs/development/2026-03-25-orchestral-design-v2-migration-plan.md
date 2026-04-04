# Orchestral Design V2 Migration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Safely migrate the existing device panels to the `Orchestral_Design.md` visual direction without breaking working panel behavior or creating mixed visual generations.

**Architecture:** The migration is system-first. Shared theme tokens and shared UI composites move first, then panel templates, then concrete panels. Every phase has a verification gate with both build checks and real panel interaction checks before the next phase starts.

**Tech Stack:** WPF, shared XAML theme resources, shared composite widgets, device panel templates, UI Automation/manual visual QA, .NET build and test pipeline.

---

## Required Starting Point

This migration must start from the latest branch or worktree that already contains the full current panel set intended for migration.

Current expected panel baseline:

- PT-104
- HuaTeng camera
- Integrated camera
- Integrated microphone
- HyperCam

Rules:

- do not start this migration from `main` if `main` does not yet contain that full panel set
- if the newest panel set exists only as uncommitted work in a feature worktree, create a child migration branch from that worktree state rather than from repository `HEAD`
- if the source branch later changes materially, stop and re-baseline before continuing the visual migration

## File Map

### Existing design and migration documents

- Reference: `docs/architecture/DESIGN_SYSTEM.md`
- Reference: `docs/architecture/ORCHESTRAL_DESIGN_MIGRATION_DELTA.md`
- Reference: `docs/architecture/DEVICE_PANEL_CONTRACT.md`
- Reference: `docs/architecture/DEVICE_WINDOW_IMPLEMENTATION_GUIDE.md`
- Reference: `docs/architecture/TEST_BEFORE_HANDOVER.md`

### Shared theme targets

- Modify: `platform/src/ExperimentalControlPlatform.App/Theme/Colors.xaml`
- Modify: `platform/src/ExperimentalControlPlatform.App/Theme/Radii.xaml`
- Modify: `platform/src/ExperimentalControlPlatform.App/Theme/Typography.xaml`
- Modify: `platform/src/ExperimentalControlPlatform.App/Theme/Controls.xaml`

### Shared shell and composite targets

- Modify: `platform/src/ExperimentalControlPlatform.App/Shell/DevicePanelShell.xaml`
- Modify: `platform/src/ExperimentalControlPlatform.App/Widgets/HeaderDeviceSelector.xaml`
- Modify: `platform/src/ExperimentalControlPlatform.App/Widgets/DeviceControlPanel.xaml`
- Modify: `platform/src/ExperimentalControlPlatform.App/Widgets/FooterStatusBar.xaml`
- Modify: `platform/src/ExperimentalControlPlatform.App/Widgets/StatisticsPanel.xaml`
- Modify: `platform/src/ExperimentalControlPlatform.App/Widgets/ValueCard.xaml`
- Modify: `platform/src/ExperimentalControlPlatform.App/Widgets/RealTimeValueHeader.xaml`
- Modify: `platform/src/ExperimentalControlPlatform.App/Widgets/CameraDataPanel.xaml`
- Modify: `platform/src/ExperimentalControlPlatform.App/Widgets/CameraImageWindow.xaml`
- Modify: `platform/src/ExperimentalControlPlatform.App/Widgets/FrameMetadataStrip.xaml`
- Modify: `platform/src/ExperimentalControlPlatform.App/Widgets/DataPanel.xaml`
- Modify: `platform/src/ExperimentalControlPlatform.App/Widgets/SerialDataPanel.xaml`
- Modify: `platform/src/ExperimentalControlPlatform.App/Widgets/PlottingWindow.xaml`

### Template targets

- Modify: `platform/src/ExperimentalControlPlatform.App/DevicePanels/Scalar/ScalarSensorPanelTemplate.xaml`
- Modify: `platform/src/ExperimentalControlPlatform.App/DevicePanels/Camera/CameraPanelTemplate.xaml`
- Modify: `platform/src/ExperimentalControlPlatform.App/DevicePanels/Audio/AudioInputPanelTemplate.xaml`

### Concrete panel verification set

- Verify: `platform/src/ExperimentalControlPlatform.App/DevicePanels/Pt104/Pt104PanelViewModel.cs`
- Verify: `platform/src/ExperimentalControlPlatform.App/DevicePanels/HuaTeng/HuaTengPanelViewModel.cs`
- Verify: `platform/src/ExperimentalControlPlatform.App/DevicePanels/Integrated/IntegratedCameraPanelViewModel.cs`
- Verify: `platform/src/ExperimentalControlPlatform.App/DevicePanels/Microphone/IntegratedMicrophonePanelViewModel.cs`
- Verify: `platform/src/ExperimentalControlPlatform.App/DevicePanels/HyperCam/HyperCamPanelViewModel.cs`

## Migration Rules

- Never restyle a concrete panel before its shared widget/template layer has moved.
- Never replace the live implementation contract in `DESIGN_SYSTEM.md` before the code actually matches it.
- Do not change behavior semantics during the visual migration unless a visual issue exposes a real logic bug.
- After every phase, run build/test and perform panel interaction checks before proceeding.
- If a shared style change breaks more than one panel family, stop and correct the shared layer before continuing.

## Verification Matrix

Run this matrix after each phase.

### Panels

- PT-104
- HuaTeng camera
- Integrated camera
- Integrated microphone
- HyperCam mock panel

### States

- window opens
- idle
- connected
- apply available/unavailable state
- read once or snap frame
- live start
- live stop
- footer/status text readable
- header selector readable
- left rail controls readable

### Commands

- [ ] `dotnet build .\ExperimentalControlPlatform.sln`
- [ ] `dotnet test .\ExperimentalControlPlatform.sln --no-build`

### Visual evidence

- [ ] capture before/after screenshots for each panel state touched in the phase
- [ ] compare radius, border, spacing, and accent usage against the migration target

## Task 1: Lock the migration baseline

**Files:**
- Modify: `docs/architecture/DESIGN_SYSTEM.md`
- Modify: `docs/architecture/ORCHESTRAL_DESIGN_MIGRATION_DELTA.md`
- Test: visual/document review only

- [ ] **Step 1: Re-read the current design system and migration delta**

Confirm the implementation contract and migration rules are aligned before code changes begin.

- [ ] **Step 2: Add explicit "phase gate" language to the migration docs if missing**

The docs must require:
- theme first
- composites second
- templates third
- concrete panels last

- [ ] **Step 3: Record the baseline panel set**

Write down the exact panels that must stay working throughout the migration:
- PT-104
- HuaTeng
- Integrated camera
- Integrated microphone
- HyperCam

- [ ] **Step 4: Commit the doc-only baseline if any doc edits were needed**

```bash
git add docs/architecture/DESIGN_SYSTEM.md docs/architecture/ORCHESTRAL_DESIGN_MIGRATION_DELTA.md
git commit -m "docs: lock orchestral design v2 migration baseline"
```

## Task 2: Migrate shared theme tokens

**Files:**
- Modify: `platform/src/ExperimentalControlPlatform.App/Theme/Colors.xaml`
- Modify: `platform/src/ExperimentalControlPlatform.App/Theme/Radii.xaml`
- Modify: `platform/src/ExperimentalControlPlatform.App/Theme/Typography.xaml`
- Modify: `platform/src/ExperimentalControlPlatform.App/Theme/Controls.xaml`
- Test: full solution build and panel smoke checks

- [ ] **Step 1: Write down the token deltas to implement**

At minimum:
- sharper control/container radii
- revised surface hierarchy
- stricter accent usage
- reduced default border emphasis
- stronger label hierarchy

- [ ] **Step 2: Update radius tokens**

Move from the current softer system toward the approved sharper system in one shared edit.

- [ ] **Step 3: Update color roles**

Reduce generic accent fill usage and strengthen surface-based separation.

- [ ] **Step 4: Update typography roles**

Strengthen micro-label hierarchy without changing control text into unreadable all-caps noise.

- [ ] **Step 5: Update shared control styles**

Make the button, dropdown, and input treatment reflect the new industrial-editorial tone.

- [ ] **Step 6: Run build**

Run: `dotnet build .\ExperimentalControlPlatform.sln`
Expected: PASS

- [ ] **Step 7: Open the baseline panels and visually check them**

Check:
- no clipping
- no unreadable text
- no invisible borders where controls need clarity
- no broken accent semantics

- [ ] **Step 8: Commit the token migration**

```bash
git add platform/src/ExperimentalControlPlatform.App/Theme/Colors.xaml platform/src/ExperimentalControlPlatform.App/Theme/Radii.xaml platform/src/ExperimentalControlPlatform.App/Theme/Typography.xaml platform/src/ExperimentalControlPlatform.App/Theme/Controls.xaml
git commit -m "feat: migrate orchestral v2 theme tokens"
```

## Task 3: Migrate shared shell and composite widgets

**Files:**
- Modify: `platform/src/ExperimentalControlPlatform.App/Shell/DevicePanelShell.xaml`
- Modify: `platform/src/ExperimentalControlPlatform.App/Widgets/HeaderDeviceSelector.xaml`
- Modify: `platform/src/ExperimentalControlPlatform.App/Widgets/DeviceControlPanel.xaml`
- Modify: `platform/src/ExperimentalControlPlatform.App/Widgets/FooterStatusBar.xaml`
- Modify: `platform/src/ExperimentalControlPlatform.App/Widgets/StatisticsPanel.xaml`
- Modify: `platform/src/ExperimentalControlPlatform.App/Widgets/ValueCard.xaml`
- Modify: `platform/src/ExperimentalControlPlatform.App/Widgets/RealTimeValueHeader.xaml`
- Modify: `platform/src/ExperimentalControlPlatform.App/Widgets/DataPanel.xaml`
- Modify: `platform/src/ExperimentalControlPlatform.App/Widgets/SerialDataPanel.xaml`
- Modify: `platform/src/ExperimentalControlPlatform.App/Widgets/PlottingWindow.xaml`
- Modify: `platform/src/ExperimentalControlPlatform.App/Widgets/CameraDataPanel.xaml`
- Modify: `platform/src/ExperimentalControlPlatform.App/Widgets/CameraImageWindow.xaml`
- Modify: `platform/src/ExperimentalControlPlatform.App/Widgets/FrameMetadataStrip.xaml`
- Test: build, tests, panel smoke checks

- [ ] **Step 1: Restyle the shell frame and header**

Update the shell surface language without changing:
- device selector placement
- subcontext selector placement
- window control behavior

- [ ] **Step 2: Restyle the left control rail**

Ensure the parameter section, apply button, action stack, and diagnostics section still read clearly after border/radius changes.

- [ ] **Step 3: Restyle cards and metric composites**

Apply the new hierarchy to:
- stat cards
- real-time header
- metadata strip
- footer

- [ ] **Step 4: Restyle plotting and camera workspace containers**

Make sure the chart/image region reads as the main content area without relying on old border assumptions.

- [ ] **Step 5: Run build and tests**

Run:
- `dotnet build .\ExperimentalControlPlatform.sln`
- `dotnet test .\ExperimentalControlPlatform.sln --no-build`

Expected: PASS

- [ ] **Step 6: Run the full baseline verification matrix**

Open each baseline panel and check all matrix states.

- [ ] **Step 7: Commit the shared widget migration**

```bash
git add platform/src/ExperimentalControlPlatform.App/Shell/DevicePanelShell.xaml platform/src/ExperimentalControlPlatform.App/Widgets
git commit -m "feat: migrate orchestral v2 shared shell and widgets"
```

## Task 4: Migrate panel templates

**Files:**
- Modify: `platform/src/ExperimentalControlPlatform.App/DevicePanels/Scalar/ScalarSensorPanelTemplate.xaml`
- Modify: `platform/src/ExperimentalControlPlatform.App/DevicePanels/Camera/CameraPanelTemplate.xaml`
- Modify: `platform/src/ExperimentalControlPlatform.App/DevicePanels/Audio/AudioInputPanelTemplate.xaml`
- Test: build, tests, per-family panel checks

- [ ] **Step 1: Update the scalar template**

Make PT-104 inherit the new system through the template, not through view-model hacks or one-off widget overrides.

- [ ] **Step 2: Update the camera template**

Make HuaTeng, Integrated camera, and HyperCam inherit the same revised shell and workspace composition.

- [ ] **Step 3: Update the audio template**

Make the microphone panel inherit the new design through the same system-level decisions.

- [ ] **Step 4: Run build and tests**

Run:
- `dotnet build .\ExperimentalControlPlatform.sln`
- `dotnet test .\ExperimentalControlPlatform.sln --no-build`

Expected: PASS

- [ ] **Step 5: Verify one representative panel from each family**

Verify:
- PT-104
- HuaTeng or HyperCam
- Integrated microphone

- [ ] **Step 6: Commit the template migration**

```bash
git add platform/src/ExperimentalControlPlatform.App/DevicePanels/Scalar/ScalarSensorPanelTemplate.xaml platform/src/ExperimentalControlPlatform.App/DevicePanels/Camera/CameraPanelTemplate.xaml platform/src/ExperimentalControlPlatform.App/DevicePanels/Audio/AudioInputPanelTemplate.xaml
git commit -m "feat: migrate orchestral v2 panel templates"
```

## Task 5: Panel-specific cleanup and edge fixes

**Files:**
- Modify only panels that still have local visual mismatches after the template migration
- Test: targeted panel verification

- [ ] **Step 1: Identify true local mismatches**

Only fix issues that remain after the shared system migration, such as:
- layout overflow
- panel-specific spacing imbalance
- local unreadable metadata density
- image/plot container framing mismatches

- [ ] **Step 2: Fix PT-104-specific polish if needed**

Examples:
- channel strip spacing
- value card density
- footer/status fit

- [ ] **Step 3: Fix camera-specific polish if needed**

Examples:
- camera metadata density
- preview framing
- ROI-related framing if applicable

- [ ] **Step 4: Fix microphone-specific polish if needed**

Examples:
- waveform container hierarchy
- RMS card emphasis
- left rail density

- [ ] **Step 5: Re-run targeted verification on every changed panel**

Use the matrix again for only the concrete panels touched in this task.

- [ ] **Step 6: Commit the cleanup pass**

```bash
git add platform/src/ExperimentalControlPlatform.App/DevicePanels
git commit -m "fix: polish panel-specific v2 design migration edges"
```

## Task 6: Promote the new design to the live contract

**Files:**
- Modify: `docs/architecture/DESIGN_SYSTEM.md`
- Modify: `docs/architecture/DEVICE_WINDOW_IMPLEMENTATION_GUIDE.md`
- Modify: `docs/architecture/WIDGET_CATALOG.md`
- Modify: `docs/architecture/ORCHESTRAL_DESIGN_MIGRATION_DELTA.md`
- Test: doc review against shipped UI

- [ ] **Step 1: Update DESIGN_SYSTEM.md to match actual shipped v2 styling**

Only do this after the theme, shell, and templates are really migrated.

- [ ] **Step 2: Update implementation and widget docs**

Make sure the docs describe the current widgets and style decisions truthfully.

- [ ] **Step 3: Downgrade the migration delta from "active bridge" to "record of migration decisions"**

Leave it as historical rationale, not the main source of truth.

- [ ] **Step 4: Do a final build and verification pass**

Run:
- `dotnet build .\ExperimentalControlPlatform.sln`
- `dotnet test .\ExperimentalControlPlatform.sln --no-build`
- full panel verification matrix

- [ ] **Step 5: Commit the doc promotion**

```bash
git add docs/architecture/DESIGN_SYSTEM.md docs/architecture/DEVICE_WINDOW_IMPLEMENTATION_GUIDE.md docs/architecture/WIDGET_CATALOG.md docs/architecture/ORCHESTRAL_DESIGN_MIGRATION_DELTA.md
git commit -m "docs: promote orchestral design v2 to live contract"
```

## Manual Work Required From The User

The migration is mostly implementable without manual coding from your side, but a few manual actions are still valuable.

### Required or strongly recommended

- [ ] final visual approval of the new design direction after each major phase
- [ ] hardware availability when verifying PT-104, HuaTeng, integrated camera, and microphone states
- [ ] taste decisions if a close call appears between:
  - readability and minimal borders
  - sharper geometry and touch comfort
  - micro-label style and dense operational clarity

### Not required from your side

- no manual code edits are required
- no manual theme-file editing is required
- no manual template wiring is required

## Exit Criteria

The migration is complete only when all of the following are true.

- [ ] shared theme files encode the new visual language
- [ ] shared shell and widgets encode the new visual language
- [ ] scalar, camera, and audio templates inherit that language cleanly
- [ ] PT-104, HuaTeng, Integrated camera, Integrated microphone, and HyperCam all render coherently
- [ ] no concrete panel depends on one-off styling to look correct
- [ ] docs match shipped reality

## Recommended execution mode

Use `subagent-driven-development`.

Reason:

- the migration breaks cleanly into phase-sized tasks
- each phase can be reviewed before the next
- visual regressions need strict containment
- shared-theme work and template work benefit from separate review loops
