# Device Window Implementation Guide

## Purpose

This guide explains how a new device implementation window should be built in Orchestral.

It is the practical bridge between:

- [DESIGN_SYSTEM.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/architecture/DESIGN_SYSTEM.md)
- [DEVICE_PANEL_CONTRACT.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/architecture/DEVICE_PANEL_CONTRACT.md)
- [WIDGET_CATALOG.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/architecture/WIDGET_CATALOG.md)

The goal is to make new device windows reusable by composition, not by cloning PT-104 markup.

## Layered Structure

Every device implementation window should be organized in five layers.

### 1. Host window

The top-level window is a real application window, not a normal widget.

Current host:

- [DeviceTestWindow.xaml](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/platform/src/ExperimentalControlPlatform.App/DeviceTestWindow.xaml)

Responsibilities:

- OS window behavior
- drag / resize / maximize
- rounded outer frame
- app-level content host

Do not put device-specific UI logic here.

### 2. Shell layer

The shell defines the structural layout shared by device windows.

Current shell:

- [DevicePanelShell.xaml](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/platform/src/ExperimentalControlPlatform.App/Shell/DevicePanelShell.xaml)

Responsibilities:

- top banner
- device selector
- subcontext selector
- window controls
- left rail slot
- workspace slot
- footer slot

The shell owns structure, not device content.

### 3. Composite widget layer

Composite widgets are the reusable mid-level modules that assemble multiple atomic controls into a meaningful panel.

Current examples:

- [DeviceControlPanel.xaml](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/platform/src/ExperimentalControlPlatform.App/Widgets/DeviceControlPanel.xaml)
- [DataPanel.xaml](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/platform/src/ExperimentalControlPlatform.App/Widgets/DataPanel.xaml)
- [SerialDataPanel.xaml](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/platform/src/ExperimentalControlPlatform.App/Widgets/SerialDataPanel.xaml)
- [StatisticsPanel.xaml](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/platform/src/ExperimentalControlPlatform.App/Widgets/StatisticsPanel.xaml)
- [FooterStatusBar.xaml](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/platform/src/ExperimentalControlPlatform.App/Widgets/FooterStatusBar.xaml)

These are the main reuse boundary for future device families.

### 4. Atomic widget layer

Atomic widgets are the smallest reusable UI elements.

Current examples:

- buttons
- dropdown boxes
- channel tabs
- window control buttons
- value cards
- plotting window
- real-time value header

These should follow the shared design tokens and shared styles.

### 5. Device-specific layer

This is the device implementation itself.

Current example:

- [Pt104PanelView.xaml](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/platform/src/ExperimentalControlPlatform.App/DevicePanels/Pt104/Pt104PanelView.xaml)
- [Pt104PanelViewModel.cs](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/platform/src/ExperimentalControlPlatform.App/DevicePanels/Pt104/Pt104PanelViewModel.cs)

This layer should only provide:

- device settings
- device actions
- device data bindings
- device status data
- device-specific diagnostics

It should not redefine shell layout or general styling.

## Composition Hierarchy

The preferred composition hierarchy is:

```text
DeviceTestWindow
  -> DevicePanelShell
    -> Left rail
      -> DeviceControlPanel
        -> dropdowns / checkboxes / binary buttons / diagnostics button
    -> Workspace
      -> DataPanel or SerialDataPanel or future device-specific workspace composite
        -> RealTimeValueHeader
        -> PlottingWindow or image canvas or waveform panel
        -> chart/action buttons
        -> StatisticsPanel
          -> ValueCard x N
    -> Footer
      -> FooterStatusBar
```

This is the default pattern for scalar sensor devices.

The visual order should stay:

1. top shell
2. left rail
3. main workspace
4. footer

Within the left rail:

1. configuration
2. actions
3. spacer
4. diagnostics

Within the main workspace:

1. main metric or primary state
2. main content card
3. compact summary strip when applicable

## Build Sequence For A New Device Window

Build new device windows in this order.

### Step 1. Classify the device archetype

Decide which family the device belongs to:

- scalar sensor
- actuator / output device
- imaging device
- protocol / diagnostics device
- hybrid device

This determines which composite workspace widget should be used.

### Step 2. Reuse the shell

Use [DevicePanelShell.xaml](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/platform/src/ExperimentalControlPlatform.App/Shell/DevicePanelShell.xaml) as the structural host.

Only fill these slots:

- `LeftRailContent`
- `WorkspaceContent`
- `FooterContent`

Do not change shell geometry unless the design system itself is being updated.

### Step 3. Choose the workspace composite

For current device families:

- scalar sensor: `SerialDataPanel`
- simpler live device: `DataPanel`
- future camera device: camera panel composite
- future actuator device: actuator control composite

If no composite fits, create a new composite widget before building a large one-off device window.

### Step 4. Bind device metadata into the shell

Every device implementation should provide:

- panel title
- device selector items
- selected device
- subcontext label and items
- selected subcontext
- footer status values

These are shell inputs, not ad hoc text blocks.

### Step 5. Bind the left rail

Use `DeviceControlPanel` for:

- configuration content
- actions content
- diagnostics content

If the device needs more than this pattern can support, extend the composite carefully rather than inlining an unrelated layout.

### Step 6. Bind the workspace

For scalar sensor-like devices, the workspace should provide:

- real-time value header
- plotting window
- chart action buttons
- statistics panel

For actuator or camera devices, keep the same shell but swap in a better-fitting workspace composite.

### Step 7. Bind the footer

Use `FooterStatusBar` for:

- connection summary
- selected mode summary
- system state summary

The footer should summarize operational state, not repeat descriptive labels already visible elsewhere.

### Step 8. Bind the panel outputs

Every implementation should expose structured panel outputs, not only display bindings.

Use:

- [INTEGRATION_PANEL_IO_CONTRACT.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/architecture/INTEGRATION_PANEL_IO_CONTRACT.md)

At minimum bind:

- data output
- applied-settings output
- status output
- diagnostics output
- session-end output

## Widget Reuse Rules

### Reuse existing widgets when

- the visual role already exists
- only content or bindings differ
- the interaction pattern is the same

Examples:

- new dropdown with different options
- new metric header with a different label
- new stat cards with different values

### Create a new composite widget when

- the device introduces a new interaction surface
- the new surface is likely to be used by more than one device family
- the layout is more than a small local variation

Examples:

- camera live view panel
- ROI editor
- waveform acquisition panel
- actuator command panel
- protocol monitor panel

### Do not create a new widget when

- only one label changes
- only one bound property changes
- the difference is cosmetic but within the same visual pattern

## Styling Rules

All widgets in a new device implementation window must use the shared design system.

Use:

- [Colors.xaml](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/platform/src/ExperimentalControlPlatform.App/Theme/Colors.xaml)
- [Typography.xaml](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/platform/src/ExperimentalControlPlatform.App/Theme/Typography.xaml)
- [Radii.xaml](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/platform/src/ExperimentalControlPlatform.App/Theme/Radii.xaml)
- [Controls.xaml](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/platform/src/ExperimentalControlPlatform.App/Theme/Controls.xaml)

Follow these rules:

- use sentence case by default
- use only the shared font sizes
- use `RadiusControl` for controls
- use `RadiusPanel` for grouped surfaces
- do not invent one-off colors
- do not reintroduce default Windows control chrome

## Data-Binding Rules

The device view model should expose structured fields, not only display text.

Typical inputs:

- configuration option collections
- selected values
- enabled / disabled states
- current value
- plot points
- statistic card items
- footer text segments

The shell and widgets should be able to render from those bindings without PT-104-specific knowledge.

## Resize Behavior

Device implementation windows should keep the current shell behavior:

- top banner fixed
- footer fixed
- left rail mostly fixed
- diagnostics stays at the bottom of the left rail
- workspace absorbs most resize pressure
- plotting or preview area takes most vertical resize change

Do not let every device redefine resize behavior independently.

## Recommended Archetypes

### Scalar sensor window

Use:

- `DevicePanelShell`
- `DeviceControlPanel`
- `SerialDataPanel`
- `FooterStatusBar`

Examples:

- temperature
- pressure
- flow rate

### Actuator window

Use:

- `DevicePanelShell`
- `DeviceControlPanel`
- future actuator workspace composite
- `FooterStatusBar`

Likely content:

- setpoint
- enable / disable
- pulse or jog action
- recent commands
- state confirmation

### Camera or imaging window

Use:

- `DevicePanelShell`
- `DeviceControlPanel`
- future camera workspace composite
- `FooterStatusBar`

Likely content:

- live image
- zoom/pan
- ROI editor
- acquisition controls
- frame metadata

### Protocol or debug window

Use:

- `DevicePanelShell`
- `DeviceControlPanel`
- future protocol monitor composite
- `FooterStatusBar`

Likely content:

- sent / received messages
- timestamps
- parse status
- connection diagnostics

## Current Reference Implementation

The current reference implementation is:

- [Pt104PanelView.xaml](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/platform/src/ExperimentalControlPlatform.App/DevicePanels/Pt104/Pt104PanelView.xaml)

This should be used as proof that the widget stack works, not as a template to clone file-by-file.

## Success Criterion

This guide is successful when a new device implementation window can be built by:

1. choosing the device archetype,
2. reusing the shell,
3. reusing existing composite widgets,
4. creating only the missing device-specific content,

instead of copying the PT-104 panel and editing it by hand.
