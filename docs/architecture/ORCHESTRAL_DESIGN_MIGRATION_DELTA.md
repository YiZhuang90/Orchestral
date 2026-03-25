# Orchestral Design Migration Delta

## Purpose

This document translates the proposed `Orchestral_Design.md` visual direction into concrete migration guidance for the current implementation system.

It exists because the current:

- [DESIGN_SYSTEM.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/architecture/DESIGN_SYSTEM.md)

is an implementation-facing contract, while `Orchestral_Design.md` is a stronger visual direction. The new design should therefore be adopted as a controlled migration, not a blind replacement.

## Status

Treat `Orchestral_Design.md` as:

- the `v2 visual direction`

Treat [DESIGN_SYSTEM.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/architecture/DESIGN_SYSTEM.md) as:

- the `current implementation contract`

This delta defines how to move from the current system to the new one without creating code-doc drift.

## Migration Gates

The migration must follow these hard gates in order:

1. shared theme tokens
2. shared shell and composite widgets
3. panel templates
4. concrete panels
5. final document promotion

Rules:

- do not restyle concrete panels before shared widgets and templates are migrated
- do not update the live implementation contract in `DESIGN_SYSTEM.md` until the shipped UI matches it
- stop after each gate and run the required verification before continuing

## Baseline Panel Set

The following panels are the required regression baseline throughout the migration:

- PT-104
- HuaTeng camera
- Integrated camera
- Integrated microphone
- HyperCam

These panels must remain visually coherent and operationally usable after each migration gate.

## What Stays

The following decisions should remain stable.

### 1. Shared device shell

Keep the existing shell model:

1. left header = device selector
2. middle header = subcontext selector
3. right header = window controls
4. left control rail
5. right workspace
6. bottom status bar

The new design changes the visual language of the shell, not its information architecture.

### 2. Light operational surfaces

Keep the light-rooted workspace strategy:

- light base canvas
- low-fatigue long-duration monitoring
- restrained accent usage

The new design is still a light industrial interface, not a dark dashboard.

### 3. Font family direction

Keep the current font pairing:

- `Manrope` for identity and hierarchy
- `Inter` for dense operational text

The change is not the font stack itself. The change is how assertively typography is used.

### 4. Data-first interface philosophy

Keep the current priority order:

- operational readability
- truthful status
- fast control comprehension
- reusable shell and widgets

The new design should not push Orchestral toward decorative UI.

## What Changes

### 1. Radius language becomes sharper

Current system:

- `Radius S = 8 px`
- `Radius L = 18 px`

New direction:

- controls should move toward lower-radius or near-sharp geometry
- containers should feel more technical and less soft

Migration implication:

- do not preserve the current rounded-container language as the permanent default
- update radii in shared theme files first, not panel-by-panel ad hoc

### 2. Borders become secondary

Current system:

- light `1 px` borders are a normal structuring tool

New direction:

- prefer surface shifts over visible borders
- visible lines should be rare and intentional

Migration implication:

- card, shell, and sidebar separation should increasingly come from layered surfaces
- borders should remain only where they serve real operational clarity

### 3. Typography becomes more editorial and technical

Current system:

- mostly sentence-case operational UI
- restrained emphasis

New direction:

- stronger typographic hierarchy
- more micro-label usage
- more label-style treatment inspired by technical manuals

Migration implication:

- preserve sentence case for most interactive controls
- selectively strengthen micro-labels, telemetry labels, and status labels
- do not blindly introduce uppercase everywhere

### 4. Accent usage becomes stricter

Current system:

- rust/orange is already the primary accent

New direction:

- rust/orange should be used more narrowly for:
  - live state
  - active status
  - critical focus

Migration implication:

- reduce decorative or broad accent fill usage
- make accent color feel more signal-like and less theme-like

### 5. Components should feel less like soft app controls

Current system:

- clean, modern, reusable, but still somewhat app-like

New direction:

- more technical
- more instrument-like
- less consumer-product softness

Migration implication:

- buttons
- dropdowns
- cards
- tabs
- stat headers

should all move toward a more industrial editorial tone.

## High-Risk Areas

These are the places where a direct replacement would likely break design coherence.

### 1. Replacing docs before theme files

If `DESIGN_SYSTEM.md` is overwritten before the theme files change, the repo loses its truthful source of implementation guidance.

### 2. Migrating panels before shared widgets

If individual panels are restyled first, Orchestral will drift into mixed design generations.

### 3. Over-applying micro-typography

If the new technical-manual style is pushed too hard into every control label, dense WPF panels may become less readable instead of more readable.

### 4. Removing all borders indiscriminately

The "no-line" rule is a visual preference, not permission to remove all structural clarity. In some data-dense surfaces, a minimal border is still the better operational choice.

## Migration Strategy

Use a phased system-first migration.

### Phase 1. Lock the visual direction

Do this first in docs:

- keep [DESIGN_SYSTEM.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/architecture/DESIGN_SYSTEM.md) as the live implementation contract
- use this delta to define the approved direction of change
- do not claim the migration is complete before the shared theme files move

### Phase 2. Update shared tokens

First code targets:

- [Colors.xaml](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/platform/src/ExperimentalControlPlatform.App/Theme/Colors.xaml)
- [Radii.xaml](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/platform/src/ExperimentalControlPlatform.App/Theme/Radii.xaml)
- [Typography.xaml](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/platform/src/ExperimentalControlPlatform.App/Theme/Typography.xaml)
- [Controls.xaml](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/platform/src/ExperimentalControlPlatform.App/Theme/Controls.xaml)

Expected changes:

- sharper radii
- revised surface hierarchy
- reduced border dependency
- stronger label hierarchy
- stricter accent semantics

### Phase 3. Update shared shell and composites

Next code targets:

- [DevicePanelShell.xaml](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/platform/src/ExperimentalControlPlatform.App/Shell/DevicePanelShell.xaml)
- [DeviceControlPanel.xaml](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/platform/src/ExperimentalControlPlatform.App/Widgets/DeviceControlPanel.xaml)
- [FooterStatusBar.xaml](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/platform/src/ExperimentalControlPlatform.App/Widgets/FooterStatusBar.xaml)
- [DataPanel.xaml](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/platform/src/ExperimentalControlPlatform.App/Widgets/DataPanel.xaml)
- [StatisticsPanel.xaml](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/platform/src/ExperimentalControlPlatform.App/Widgets/StatisticsPanel.xaml)

Expected changes:

- shell/header styling
- sidebar treatment
- card separation by surface rather than lines
- more technical stat-card hierarchy

### Phase 4. Migrate template families

Then migrate:

- camera template family
- scalar sensor template family
- audio/time-series family

Do this template-first before polishing individual concrete panels.

### Phase 5. Panel-specific polish

Only after the shared system has moved should individual devices receive tailored visual polish.

## Recommendation

Do not replace [DESIGN_SYSTEM.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/architecture/DESIGN_SYSTEM.md) immediately.

Instead:

1. use `Orchestral_Design.md` as the approved visual target
2. use this document as the migration bridge
3. migrate shared tokens and shared widgets first
4. update [DESIGN_SYSTEM.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/architecture/DESIGN_SYSTEM.md) only when the new system is implemented enough to be the truthful live contract

That preserves both momentum and architectural honesty.
