# Orchestral Design System

## Purpose

This document defines the first-generation visual system for Orchestral device panels.

The PT-104 hardware integration window is the first reference implementation. The goal is to turn the decisions made there into reusable design rules, tokens, and layout patterns for future device panels and later full-OS surfaces.

Detailed widget definitions and future expansion planning are captured in:

- [WIDGET_CATALOG.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/architecture/WIDGET_CATALOG.md)

The planned migration path toward the newer `Orchestral_Design.md` visual direction is captured in:

- [ORCHESTRAL_DESIGN_MIGRATION_DELTA.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/architecture/ORCHESTRAL_DESIGN_MIGRATION_DELTA.md)

Until that migration is complete, this document remains the live implementation contract. Shared theme files and shared widgets must move first; concrete panels must not drift ahead of the system.

## Design Intent

Orchestral should feel:

- precise
- calm
- technical
- modern
- trustworthy
- screen-first

It should not feel:

- like a default Windows form
- noisy or overly decorative
- like a generic industrial SCADA panel
- like a dark "cyber" dashboard

## Core Visual Principles

### 1. Calm Technical Surfaces

Most surfaces should be light, neutral, and low-contrast, with emphasis created through typography, spacing, and a restrained accent color.

### 2. Strong Hierarchy, Not Heavy Decoration

The UI should communicate priority through layout and type, not through many borders, gradients, or competing accent colors.

### 3. Operational Readability First

Any stylistic decision should preserve fast comprehension of:

- current state
- live data
- controls
- failure conditions

### 4. Reusable Device Shell

The PT-104 layout is not a one-off screen. It is the first instance of the standard device-panel shell.

## Typography

Typography is implemented in:

- [Typography.xaml](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/platform/src/ExperimentalControlPlatform.App/Theme/Typography.xaml)

Primary font pair:

- `Manrope`
  - use for product identity, device title, major headers, major live values, section hierarchy, and navigation emphasis
- `Inter`
  - use for controls, labels, helper text, telemetry, diagnostics text, footer text, and dense technical copy

### Type Roles

- `ShellTitle = 16 px`
- `DisplayValue = 20 px`
- `SectionHeader = 16 px`
- `Body = 12 px`
- `Micro = 12 px`

Weight guidance:

- `Manrope`
  - regular by default in operational UI
  - stronger weights only when hierarchy is unclear without them
- `Inter`
  - `400` for normal body and control text
  - avoid unnecessary bold emphasis in forms and telemetry

Practical rule:

- if the text is about identity, hierarchy, or emphasis, prefer `Manrope`
- if the text is about operation, data, or repeated reading, prefer `Inter`

## Text Case Rules

- Use `sentence case` for headings, controls, buttons, stat-card titles, and helper text
- Avoid `Title Case` drift across panels unless there is a deliberate brand reason
- Avoid broad `ALL CAPS` in normal operational UI

## Radius System

Use a two-radius system:

- `Radius S = 8 px`
  - buttons
  - dropdown containers
  - small inline controls
- `Radius L = 18 px`
  - cards
  - plot containers
  - side panels
  - stat cards
  - grouped surfaces

Practical rule:

- if the element is a direct control the user clicks or edits, use `Radius S`
- if the element is a container that groups information or content, use `Radius L`

The outer window shell may use the large-radius family, but should not create a second competing language.

## Color Roles

The first PT-104 panel establishes the baseline token set. These are now implemented in:

- [Colors.xaml](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/platform/src/ExperimentalControlPlatform.App/Theme/Colors.xaml)
- [Radii.xaml](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/platform/src/ExperimentalControlPlatform.App/Theme/Radii.xaml)
- [Controls.xaml](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/platform/src/ExperimentalControlPlatform.App/Theme/Controls.xaml)

### Neutrals

- `Surface Root = #F9F9F6`
- `Surface Raised = #FFFFFF`
- `Surface Soft = #F3F4F0`
- `Surface Plot = #FCFCF9`
- `Border Soft = #DEE4DE`
- `Border Frame = #B8B8B8`
- `Text Primary = #2E3430`
- `Text Secondary = #5A615C`
- `Text Tertiary = #5F5E5E`

### Accent

- `Accent Primary = #9A462A`
- `Accent Strong = #AE532D`
- `Accent Fill = #239A462A`
- `Accent On Dark = #FFF7F5`

### State / Action

- `Action Neutral = #6B6A69`
- `Action Hover Neutral = #D7DBD6`
- `Action Soft = #E9E7E7`
- `Action Outline = #6B6A69`
- `Action Danger/Close Hover = #9A462A`

## Spacing System

Spacing should be restrained and repeatable rather than custom at every level.

Recommended base spacing family:

- `4 px`
- `6 px`
- `8 px`
- `10 px`
- `12 px`
- `14 px`
- `18 px`

Use these for:

- internal control padding
- section spacing
- card padding
- title-to-subtitle spacing
- plot and card gutters

Avoid one-off spacing values unless a real alignment problem requires them.

## Border and Shadow Policy

### Borders

- use light borders for cards, dropdown containers, and shell edges
- prefer `1 px` borders
- do not stack multiple visible borders unless the element clearly needs them

### Shadow

For the current generation of device panels, avoid visible card shadows. The window shell may use either:

- no shadow with a clear `1 px` frame line, or
- a very subtle outer separation only if it does not create dark edge artifacts

The approved PT-104 direction is currently:

- rounded window shell
- visible `1 px` edge line
- no heavy shadow band

## Control Styles

### Buttons

All action buttons should be derived from a shared family:

- primary
- soft
- outline
- accent
- chart-action

Buttons should share:

- `Radius S`
- consistent height family
- consistent font role
- centered content

### Dropdowns

Dropdowns should:

- use the same `Radius S` as buttons
- sit inside a white rounded container
- avoid default Windows chrome
- use centered vertical alignment for text
- keep the right chevron clickable

### Checkboxes

Checkboxes should be visually quiet and align with `Inter` body text.

## Device Panel Shell Anatomy

Every device panel should fit into this shell anatomy:

1. top shell bar
   - device selector on the left
   - subcontext selector in the center
   - window controls on the right
2. left control rail
   - configuration
   - actions
   - diagnostics
3. right main workspace
   - main live metric
   - main data card
   - stat-card strip
4. fixed bottom status bar

The reusable shell is now implemented in:

- [DevicePanelShell.xaml](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/platform/src/ExperimentalControlPlatform.App/Shell/DevicePanelShell.xaml)
- [DevicePanelShell.xaml.cs](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/platform/src/ExperimentalControlPlatform.App/Shell/DevicePanelShell.xaml.cs)

PT-104 is the first consumer:

- [Pt104PanelView.xaml](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/platform/src/ExperimentalControlPlatform.App/DevicePanels/Pt104/Pt104PanelView.xaml)
- [Pt104PanelView.xaml.cs](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/platform/src/ExperimentalControlPlatform.App/DevicePanels/Pt104/Pt104PanelView.xaml.cs)

The preferred visual anatomy is:

```text
+--------------------------------------------------------------------+
| Top Shell: device selector | subcontext | window controls          |
+-----------------+--------------------------------------------------+
| Left Rail       | Main Workspace                                   |
|                 |                                                  |
| Configuration   | Main metric                                      |
| Actions         | Main data card                                   |
| Diagnostics     | Stat-card strip                                  |
+-----------------+--------------------------------------------------+
| Fixed Footer: connection/config summary | system state             |
+--------------------------------------------------------------------+
```

The left rail should follow this default order:

1. configuration
2. actions
3. flexible spacer
4. diagnostics

The workspace should use:

- one strong main metric line
- no second line unless it carries live operational meaning
- one dominant main data card
- one compact summary strip when the device naturally produces summary metrics

## Resize Behavior

Resize behavior is part of the design system.

### Fixed

- top shell height
- bottom status bar height
- left rail structural sections

### Elastic

- left-rail lower gap above diagnostics
- main live-data panel height

The interface should preserve footer visibility on a typical 1080p laptop display.

## Sensor Panel Pattern

For data-producing devices such as:

- temperature
- pressure
- flow rate
- voltage
- image-derived scalars

the panel should include:

- live numeric metric
- live plot or stream area
- current configuration
- stat cards
- footer state summary

## Actuator Panel Pattern

For output-only devices such as:

- valves
- triggers
- relays
- pumps

the main workspace may replace the live plot with:

- state timeline
- recent command log
- pulse/test controls
- state confirmation

The shell still stays the same.

## What Should Become Tokens In Code

The following should move into reusable WPF resources:

- font families
- font sizes
- colors
- radius values
- button styles
- dropdown styles
- card styles
- shell border style
- status bar style

## Success Criterion

The design system is working when the second device panel can be built without copying PT-104 layout and manually restyling it. The new panel should feel like Orchestral immediately because the shell, tokens, and control styles are shared.
