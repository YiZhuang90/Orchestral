# Device Window Implementation Guide

## Purpose

This guide explains how a new device implementation window should be built in Orchestral.

It is the practical bridge between:

- [DESIGN_SYSTEM.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/architecture/DESIGN_SYSTEM.md)
- [DEVICE_PANEL_CONTRACT.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/architecture/DEVICE_PANEL_CONTRACT.md)
- [WIDGET_CATALOG.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/architecture/WIDGET_CATALOG.md)

The goal is to make new device windows reusable by composition, not by cloning PT-104 markup.

On the current v2 migration branch, shared theme tokens, the shell, and the first family templates have already been migrated to the sharper `Orchestral_Design.md` direction. New work should inherit those layers instead of restyling concrete panels locally.

## Layered Structure

Every device implementation window should be organized in five layers.

### 1. Host window

The top-level window is a real application window, not a normal widget.

Current host:

- [DeviceTestWindow.xaml](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/platform/src/ExperimentalControlPlatform.App/DeviceTestWindow.xaml)

Responsibilities:

- OS window behavior
- drag / resize / maximize
- sharper outer frame
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

Its current visual language should follow the sharper shared system:

- warmer layered surfaces
- reduced border dependence
- sharper radii
- stronger micro-label hierarchy
- subcontext in the middle header, not hard-coded channel tabs

### 3. Composite widget layer

Composite widgets are the reusable mid-level modules that assemble multiple atomic controls into a meaningful panel.

Current examples:

- [DeviceControlPanel.xaml](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/platform/src/ExperimentalControlPlatform.App/Widgets/DeviceControlPanel.xaml)
- [DataPanel.xaml](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/platform/src/ExperimentalControlPlatform.App/Widgets/DataPanel.xaml)
- [SerialDataPanel.xaml](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/platform/src/ExperimentalControlPlatform.App/Widgets/SerialDataPanel.xaml)
- [StatisticsPanel.xaml](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/platform/src/ExperimentalControlPlatform.App/Widgets/StatisticsPanel.xaml)
- [FooterStatusBar.xaml](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/platform/src/ExperimentalControlPlatform.App/Widgets/FooterStatusBar.xaml)

These are the main reuse boundary for future device families. When visual changes are needed, update this layer before touching concrete device panels.

### 4. Atomic widget layer

Atomic widgets are the smallest reusable UI elements.

Current examples:

- buttons
- dropdown boxes
- subcontext tabs
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

Preferred composition:

```text
DeviceTestWindow
  -> DevicePanelShell
    -> Left rail
      -> DeviceControlPanel
        -> config fields
        -> lifecycle row
        -> action buttons
        -> diagnostics button
    -> Workspace
      -> Scalar / Camera / Audio family template
        -> RealTimeValueHeader
        -> PlottingWindow or image surface or waveform panel
        -> chart/data actions
        -> StatisticsPanel or metadata strip
    -> Footer
      -> FooterStatusBar
```

Visual order:

1. top shell
2. left rail
3. main workspace
4. footer

Within the left rail:

1. configuration
2. apply / lifecycle
3. actions
4. spacer
5. diagnostics

Within the main workspace:

1. primary live value or primary state
2. dominant content surface
3. compact summary strip when applicable

## Build Sequence For A New Device Window

### Step 1. Classify the device archetype

Decide which family the device belongs to:

- scalar sensor
- actuator / output device
- imaging device
- audio input device
- protocol / diagnostics device
- hybrid device

This determines which family template or workspace composite should be used.

### Step 2. Reuse the shell

Use [DevicePanelShell.xaml](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/platform/src/ExperimentalControlPlatform.App/Shell/DevicePanelShell.xaml) as the structural host.

Only fill these slots:

- `LeftRailContent`
- `WorkspaceContent`
- `FooterContent`

Do not change shell geometry unless the design system itself is being updated.

### Step 3. Choose the family template or workspace composite

For current families:

- scalar sensor: `ScalarSensorPanelTemplate`
- camera device: `CameraPanelTemplate`
- audio input device: `AudioInputPanelTemplate`
- simpler live device: `DataPanel`
- future actuator device: actuator control composite

If no family template fits, create a new reusable family/template layer before building a large one-off device window.

### Step 4. Bind device metadata into the shell

Every device implementation should provide:

- panel title
- device selector items
- selected device
- subcontext label and items
- selected subcontext
- footer status values

Subcontext examples:

- PT-104: `Channel: 2 4`
- a microphone with truthful split modes: `Mode: ...`
- a simple camera: empty middle header

These are shell inputs, not ad hoc text blocks.

### Step 5. Bind the left rail

Use `DeviceControlPanel` for:

- configuration content
- lifecycle content
- actions content
- diagnostics content

If the device needs more than this pattern can support, extend the composite carefully rather than inlining an unrelated layout.

### Step 6. Bind the workspace

For scalar sensor-like devices, the workspace should provide:

- real-time value header
- plotting window
- chart action buttons
- statistics panel

For camera or audio devices, keep the same shell but swap in the correct family template before inventing a one-off workspace composite.

### Step 7. Bind the footer

Use `FooterStatusBar` for:

- connection summary
- selected mode or format summary
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
- do not reintroduce the older softer 8/18 radius language in new work

## Data-Binding Rules

The device view model should expose structured fields, not only display text.

Typical inputs:

- configuration option collections
- selected values
- enabled / disabled states
- current value
- plot points or preview source
- statistic or metadata items
- footer text segments

The shell and widgets should be able to render from those bindings without PT-104-specific knowledge.

## Resize Behavior

Device implementation windows should keep the current shell behavior:

- top banner fixed
- footer fixed
- left rail mostly fixed
- diagnostics stays at the bottom of the left rail
- workspace absorbs most resize pressure
- plotting, preview, or waveform area takes most vertical resize change

Do not let every device redefine resize behavior independently.

## Recommended Archetypes

### Scalar sensor window

Use:

- `DevicePanelShell`
- `DeviceControlPanel`
- `ScalarSensorPanelTemplate`
- `FooterStatusBar`

Examples:

- temperature
- pressure
- flow rate

### Camera or imaging window

Use:

- `DevicePanelShell`
- `DeviceControlPanel`
- `CameraPanelTemplate`
- `FooterStatusBar`

Likely content:

- live image
- acquisition controls
- frame metadata
- ROI or mode controls when truthfully supported

### Audio input window

Use:

- `DevicePanelShell`
- `DeviceControlPanel`
- `AudioInputPanelTemplate`
- `FooterStatusBar`

Likely content:

- waveform
- RMS/peak metrics
- update-rate controls
- diagnostics

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

## Current Reference Implementations

Current family references:

- scalar: [Pt104PanelView.xaml](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/platform/src/ExperimentalControlPlatform.App/DevicePanels/Pt104/Pt104PanelView.xaml)
- camera: shared camera template consumers
- audio: shared audio template consumers

Use them as proof that the stack works, not as file-by-file cloning targets.

## Success Criterion

This guide is successful when a new device implementation window can be built by:

1. choosing the device archetype,
2. reusing the shell,
3. reusing an existing family template or composite widget,
4. creating only the missing device-specific bindings or surfaces,

instead of copying an existing panel and editing it by hand.
