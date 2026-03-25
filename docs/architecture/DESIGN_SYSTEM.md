# Orchestral Design System

## Purpose

This document defines the live visual implementation contract for Orchestral device panels on the current v2 migration branch.

It supersedes the earlier softer first-generation interpretation by describing the tokens, shell, and template behavior that are actually implemented in the shared WPF theme and widget layer.

Read with:

- [WIDGET_CATALOG.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/architecture/WIDGET_CATALOG.md)
- [DEVICE_PANEL_CONTRACT.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/architecture/DEVICE_PANEL_CONTRACT.md)
- [ORCHESTRAL_DESIGN_MIGRATION_DELTA.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/architecture/ORCHESTRAL_DESIGN_MIGRATION_DELTA.md)

## Design Intent

Orchestral should feel:

- precise
- calm
- technical
- editorial
- trustworthy
- screen-first

It should not feel:

- like a default Windows form
- noisy or overly decorative
- like a generic SCADA dashboard
- soft and bubbly
- dark cyber UI

## Core Visual Principles

### 1. Warm Technical Surfaces

The system uses warm neutrals, layered surfaces, and restrained borders. Separation should come primarily from tone, spacing, and hierarchy instead of thick outlines or shadows.

### 2. Strong Hierarchy, Minimal Chrome

Priority is expressed through typography, spacing, and panel composition. Labels are small and disciplined. The single most important live value should stand out clearly without forcing the rest of the UI to shout.

### 3. Operational Readability First

Style must preserve fast comprehension of:

- current state
- live data
- controls
- failure conditions

### 4. Reusable Device Shell

The shell is not PT-104-specific. Scalar, camera, and audio families all inherit the same shared shell, token, and control system.

## Typography

Typography is implemented in:

- [Typography.xaml](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/platform/src/ExperimentalControlPlatform.App/Theme/Typography.xaml)

Primary font pair:

- `Manrope`
  - use for product identity, device titles, major values, section-value emphasis, and subcontext tabs
- `Inter`
  - use for controls, labels, helper text, telemetry, metadata, diagnostics text, and dense operational copy

### Type Roles

- `ShellTitle = 15 px`
- `DisplayValue = 22 px`
- `SectionHeader = 15 px`
- `Body = 12 px`
- `Micro = 10 px`

Weight guidance:

- `Manrope`
  - use `SemiBold` selectively for title-level emphasis
  - keep most operational surfaces at normal weight
- `Inter`
  - `400` for most labels and controls
  - avoid unnecessary bolding in dense telemetry areas

Micro-label guidance:

- use `FontMicro` for section labels, stat-card labels, axis labels, footer metadata, and header context labels
- use `FontBody` for editable controls and denser operational copy
- reserve `FontDisplayValue` for one primary live value per view or card

## Text Case Rules

- use sentence case for headings, controls, buttons, stat-card titles, helper text, and footer segments
- avoid broad `ALL CAPS`
- avoid title-case drift between panels

## Radius System

Use the current sharper radius family:

- `RadiusControl = 4 px`
  - buttons
  - dropdown containers
  - text entry containers
  - small inline controls
- `RadiusPanel = 10 px`
  - cards
  - plotting surfaces
  - image surfaces
  - side panels
  - grouped containers
- `RadiusWindowOuter = 12 px`
- `RadiusWindowInner = 11 px`

Practical rule:

- if the element is a direct control the user edits or clicks, use `RadiusControl`
- if the element groups content or frames a work surface, use `RadiusPanel`

## Color Roles

The live token set is implemented in:

- [Colors.xaml](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/platform/src/ExperimentalControlPlatform.App/Theme/Colors.xaml)
- [Radii.xaml](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/platform/src/ExperimentalControlPlatform.App/Theme/Radii.xaml)
- [Controls.xaml](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/platform/src/ExperimentalControlPlatform.App/Theme/Controls.xaml)

### Neutrals

- `WindowBackgroundBrush = #F6F4EE`
- `WindowBorderBrush = #C9C0B3`
- `ShellDividerBrush = #E2DBCF`
- `FooterDividerBrush = #DDD6CB`
- `RailBackgroundBrush = #EEEBE3`
- `SurfaceBrush = #FBF8F2`
- `MutedSurfaceBrush = #F1EEE6`
- `PlotSurfaceBrush = #F8F5EF`
- `SoftBorderBrush = #DED7CB`
- `PlotBorderBrush = #E7E0D5`
- `TextPrimaryBrush = #302F2C`
- `TextSecondaryBrush = #666158`
- `TextMutedBrush = #6B655D`

### Accent

- `AccentBrush = #9A462A`
- `AccentHoverBrush = #A44D2D`
- `AccentPressedBrush = #7B341F`
- `AccentForegroundBrush = #FBF5F2`
- `PlotFillBrush = #1C9A462A`

Accent usage rule:

- use accent on active live values, selected subcontext tabs, and truly primary actions
- do not use accent as a general decorative fill across the panel

### State / Action

- `DarkButtonBrush = #5F5E5E`
- `SoftButtonBrush = #E4DED4`
- `NeutralHoverBrush = #ECE6DC`
- `NeutralPressedBrush = #DDD6CA`

## Spacing System

Use a restrained repeatable spacing family:

- `4 px`
- `6 px`
- `8 px`
- `10 px`
- `12 px`
- `14 px`
- `16 px`

Use these for:

- internal control padding
- section spacing
- card padding
- title-to-value spacing
- plot and card gutters

Avoid ad hoc spacing except to resolve a real alignment issue.

## Border and Shadow Policy

### Borders

- prefer layered surface separation over visible borders
- keep borders light and sparse where they still help clarity
- prefer `1 px` borders
- do not stack multiple visible borders unless clearly necessary

Current implementation note:

- cards and plotting/image surfaces do use light `1 px` borders in the current v2 branch
- controls keep their chrome minimal and let container shape do most of the work

### Shadow

The current device panels avoid visible card shadows. The window shell relies on a frame and surface hierarchy instead of a strong outer shadow band.

## Control Styles

### Buttons

All action buttons derive from a shared family:

- primary
- soft
- outline
- accent
- chart-action

Button families share:

- `RadiusControl`
- centered content
- `FontBody`
- a tight height family

Current heights:

- standard rail and lifecycle buttons: `42 px`
- chart/data-toolbar buttons: `38 px`
- window controls: `30 x 24`

### Dropdowns

Dropdowns should:

- use `RadiusControl`
- sit inside a light raised container
- avoid default Windows chrome
- keep the chevron area clickable
- align vertically with the shared 42 px control height

### Checkboxes

Checkboxes should be visually quiet and aligned with `Inter` body text.

## Device Panel Shell Anatomy

Every device panel should fit this shell anatomy:

1. top shell bar
   - device selector on the left
   - subcontext selector in the center
   - window controls on the right
2. left control rail
   - configuration
   - apply and lifecycle
   - actions
   - diagnostics
3. right main workspace
   - primary live metric
   - dominant content surface
   - summary strip when appropriate
4. fixed bottom status bar

The reusable shell is implemented in:

- [DevicePanelShell.xaml](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/platform/src/ExperimentalControlPlatform.App/Shell/DevicePanelShell.xaml)
- [DevicePanelShell.xaml.cs](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/platform/src/ExperimentalControlPlatform.App/Shell/DevicePanelShell.xaml.cs)

Preferred anatomy:

```text
+--------------------------------------------------------------------+
| Top Shell: device selector | subcontext | window controls          |
+-----------------+--------------------------------------------------+
| Left Rail       | Main Workspace                                   |
|                 |                                                  |
| Configuration   | Primary live value                               |
| Apply/Lifecycle | Main content surface                             |
| Actions         | Summary strip                                    |
+-----------------+--------------------------------------------------+
| Fixed Footer: connection/config summary | system state             |
+--------------------------------------------------------------------+
```

Header semantics:

- left = device selector disguised as title text
- middle = subcontext selector
- right = window controls

The middle region is not channel-specific. It can represent:

- `Channel: 2 4`
- `Mode: ...`
- `View: ...`
- another truthful device-internal context switch

The left rail should follow this default order:

1. configuration
2. apply / lifecycle
3. actions
4. flexible spacer
5. diagnostics

## Resize Behavior

### Fixed

- top shell height
- bottom status bar height
- left rail structural sections

### Elastic

- lower gap above diagnostics
- primary workspace height
- plotting/preview/waveform area

The interface should preserve footer visibility on a typical 1080p laptop display.

## Family Patterns

### Scalar Sensor

Panels should include:

- live numeric metric
- time-series plot
- current configuration
- stat cards
- footer summary

### Camera

Panels should include:

- live image surface
- frame metadata strip
- current acquisition controls
- capture/display semantics surfaced truthfully

### Audio Input

Panels should include:

- waveform surface
- RMS or similar primary value
- update-window controls
- footer state summary

### Actuator

Panels may replace the main plot with:

- state timeline
- recent command log
- pulse/test controls
- state confirmation

The shell still stays the same.

## What Should Stay Tokenized In Code

The following belong in reusable WPF resources:

- font families
- font sizes
- colors
- radius values
- button styles
- dropdown styles
- card styles
- shell frame style
- footer style

## Success Criterion

The design system is working when a new panel family can be built without copying PT-104 markup or manually restyling it. A new panel should feel like Orchestral immediately because the shell, tokens, shared controls, and family templates are already aligned.
