# Orchestral Widget Catalog

## Purpose

This document defines the reusable widget set for first-generation Orchestral device panels.

It exists to make the UI reusable by composition rather than by copying PT-104 markup. Each widget below is treated as a reusable module with:

- visual rules
- interaction rules
- data/function contract
- composition role

This catalog should be read together with:

- [DESIGN_SYSTEM.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/architecture/DESIGN_SYSTEM.md)
- [FONT_DESIGN_GUIDE.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/architecture/FONT_DESIGN_GUIDE.md)
- [DEVICE_PANEL_CONTRACT.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/architecture/DEVICE_PANEL_CONTRACT.md)
- [DEVICE_PANEL_VISUAL_TEMPLATE.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/architecture/DEVICE_PANEL_VISUAL_TEMPLATE.md)

## Token Baseline

These widget definitions use the current Orchestral token system implemented in:

- [Colors.xaml](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/platform/src/ExperimentalControlPlatform.App/Theme/Colors.xaml)
- [Typography.xaml](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/platform/src/ExperimentalControlPlatform.App/Theme/Typography.xaml)
- [Radii.xaml](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/platform/src/ExperimentalControlPlatform.App/Theme/Radii.xaml)
- [Controls.xaml](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/platform/src/ExperimentalControlPlatform.App/Theme/Controls.xaml)

### Current core tokens

- `FontShellTitle = 16 px`
- `FontDisplayValue = 20 px`
- `FontSectionHeader = 16 px`
- `FontBody = 12 px`
- `FontMicro = 12 px`
- `RadiusControl = 8 px`
- `RadiusPanel = 18 px`
- `RadiusWindowOuter = 16 px`
- `RadiusWindowInner = 15 px`

### Current core colors

- `WindowBackgroundBrush = #F9F9F6`
- `RailBackgroundBrush = #F3F4F0`
- `SurfaceBrush = #FFFFFF`
- `MutedSurfaceBrush = #F3F4F0`
- `PlotSurfaceBrush = #FCFCF9`
- `TextPrimaryBrush = #2E3430`
- `TextSecondaryBrush = #5A615C`
- `TextMutedBrush = #5F5E5E`
- `AccentBrush = #9A462A`
- `AccentHoverBrush = #AE532D`
- `AccentPressedBrush = #7E351F`
- `DarkButtonBrush = #6B6A69`
- `SoftButtonBrush = #E9E7E7`
- `SoftBorderBrush = #E8EBE5`
- `PlotBorderBrush = #ECECE5`

## Widget Overview

The current reusable widget set is organized into:

- primitive widgets
- composite widgets
- shell widgets

### Primitive widgets

- button
- dropdown box
- checkbox
- header text
- header device selector
- subcontext switcher
- value card
- channel tab
- mini/max/close buttons

### Composite widgets

- control panel
- action row
- real-time value header
- plotting window
- data panel
- statistics panel
- serial data panel
- bottom status bar

### Shell widgets

- top banner

## 1. Button

### Purpose

A reusable action surface for commands such as connect, read once, start live, clear data, export, diagnostics, confirm, and close.

### Visual spec

- font: `Inter`
- font size: `12 px`
- default font weight: regular
- default height: `40 px`
- radius: `RadiusControl = 8 px`
- icon size: `14 x 14` for content buttons
- content alignment: centered horizontally and vertically

### Button families

- `PrimaryActionButtonStyle`
  - background: `DarkButtonBrush`
  - foreground: light text
- `SoftActionButtonStyle`
  - background: `SoftButtonBrush`
  - foreground: muted gray text
- `OutlineActionButtonStyle`
  - background: `SurfaceBrush`
  - border: `2 px`
  - border color: `DarkButtonBrush`
- `AccentActionButtonStyle`
  - background: `AccentHoverBrush`
  - foreground: `AccentForegroundBrush`
- `ChartActionButtonStyle`
  - height: `40 px`
  - minimum width: `132 px`
  - used for chart-toolbar actions

### Functionality

- triggers one discrete action on click
- may be enabled or disabled from device state
- may show an embedded icon and text
- may be used as a binary toggle when the label and icon depend on state

### Current uses

- connect/disconnect binary button
- read once
- start live/stop live binary button
- diagnostics
- clear data
- export CSV

## 2. Dropdown Box

### Purpose

A reusable selection widget for discrete configuration parameters such as sensor type, wire count, mains filter, trigger mode, gain mode, or acquisition profile.

### Visual spec

- font: `Inter`
- font size: `12 px`
- control height: `40 px`
- text vertically centered
- outer container background: `SurfaceBrush`
- border: `1 px` using `SoftBorderBrush`
- radius: `RadiusControl = 8 px`
- chevron stroke: `#6A7170`
- no default Windows chrome

### Functionality

- opens list on click anywhere on the control, including chevron area
- selects one item from a discrete list
- updates a bound property
- supports popup dropdown with same corner language

### Current uses

- sensor type
- wire count
- mains filter

## 3. Checkbox

### Purpose

A reusable boolean control for single binary options.

### Visual spec

- font: `Inter`
- font size: `12 px`
- low-contrast visual treatment
- aligned with body text, not oversized

### Functionality

- toggles a boolean state
- should be used only for true/false options where a binary button would be visually heavier than necessary

### Current uses

- filtered read

## 4. Header

### Purpose

A reusable textual identity or section-heading widget.

### Visual spec

- shell/device identity: `Manrope`, `16 px`
- major real-time value header: `Manrope`, `20 px`
- section header: `Manrope`, `16 px`
- sentence case by default

### Functionality

- presents structure, identity, or current primary value
- not interactive by default

### Current uses

- device title in the top banner
- real-time value header in the data panel
- section labels such as `Sensor type`, `Wire count`, `Mains filter`, `Actions`

## 4A. Header Device Selector

### Purpose

A reusable header widget that shows the current device identity and becomes a dropdown selector when multiple same-class devices are available.

### Visual spec

- same visual role as shell title text
- no boxed input chrome
- same font and alignment as the device name label
- optional subtle chevron only when multiple choices exist

### Functionality

- shows the selected device
- opens a device list on click when multiple device instances exist
- should not be mistaken for a form control in the rail

### Current uses

- PT-104 header
- integrated camera header

## 4B. Subcontext Switcher

### Purpose

A reusable middle-banner widget for switching endpoint or internal context within the already selected device.

### Visual spec

- static label such as `Channel:`
- followed by compact tabs or context items
- should occupy the middle banner region only when needed

### Functionality

- switches channel, axis, view, or comparable device-internal context
- does not switch between different hardware instances

### Current uses

- `Channel: 2 4` on PT-104

## 5. Channel Tab

### Purpose

A reusable selector for multiple channels of the same device or multiple instances of the same device family.

### Visual spec

- font: `Manrope`
- font size: `12 px`
- padding: `16,7`
- underline height: `2 px`
- selected text color: `AccentBrush`
- idle text color: `TextMutedBrush`
- hover background: `MutedSurfaceBrush`

### Functionality

- switches current channel or current device instance
- updates the bound selected item
- should not be used as a general device catalog

### Current uses

- `Channel 1..4` on PT-104

## 6. Mini / Max / Close Buttons

### Purpose

Reusable custom window-control buttons for shell windows and Orchestral modals.

### Visual spec

- size: `32 x 26`
- radius: `RadiusControl = 8 px`
- icon size: `12 x 12`
- foreground: `TextMutedBrush`
- neutral hover: `NeutralHoverBrush`
- neutral pressed: `NeutralPressedBrush`
- close hover: `AccentHoverBrush`
- close pressed: `AccentPressedBrush`
- close hover foreground: `AccentForegroundBrush`

### Functionality

- minimize window
- maximize/restore window
- close window

### Current uses

- top banner in `DevicePanelShell`
- close button in `OrchestralModalWindow`

## 7. Top Banner

### Purpose

A reusable shell widget that combines:

- device selector
- subcontext selector
- mini/max/close buttons

### Visual spec

- height: `48 px`
- background: `WindowBackgroundBrush`
- bottom divider: `1 px` using `ShellDividerBrush`
- top corner radius: `15,15,0,0` inside shell
- left and right content margin: `14 px`

### Functionality

- shows device identity
- switches device instance from the left region
- switches internal context from the middle region
- supports title-bar dragging
- hosts native-like window controls

### Current uses

- [DevicePanelShell.xaml](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/platform/src/ExperimentalControlPlatform.App/Shell/DevicePanelShell.xaml)

## 8. Control Panel

### Purpose

A reusable left-rail module that groups configuration widgets and action widgets for one device.

### Composition

- dropdown box
- checkbox
- binary button
- standard action button
- diagnostics button

### Visual spec

- background is provided by left rail
- vertical spacing follows 6 to 16 px family
- configuration labels use `Inter` `12 px`
- buttons follow shared button family

### Functionality

- edits device configuration
- issues safe bring-up actions
- exposes diagnostics
- should make shared vs endpoint settings legible
- should expose default parameters only and push advanced settings into secondary surfaces

## 8A. Action Row

### Purpose

A reusable action cluster for panel lifecycle and operational commands.

### Typical actions

- `Apply`
- `Apply and exit`
- `Connect`
- `Start live`
- `Stop live`
- `Diagnostics`

### Functionality

- groups actions by lifecycle meaning rather than arbitrary placement
- should make the difference between apply, start, stop, and exit explicit
- should emit structured lifecycle events, not only visual changes

### Current uses

- PT-104 left rail in [Pt104PanelView.xaml](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/platform/src/ExperimentalControlPlatform.App/DevicePanels/Pt104/Pt104PanelView.xaml)

## 9. Value Card

### Purpose

A reusable card for presenting one summary metric.

### Visual spec

- background: `MutedSurfaceBrush`
- radius: `RadiusPanel = 18 px`
- padding: `10 px`
- border: none in current PT-104 direction
- label font: `Inter` `12 px`
- value font: `Manrope` `16 px`

### Functionality

- presents one bound summary value
- may update live during acquisition
- should not expose direct controls

### Current uses

- `Peak high`
- `Peak low`
- `RMS variance`
- `Samples`

## 10. Statistics Panel

### Purpose

A reusable summary strip composed of multiple value cards.

### Composition

- 2 to 6 value cards
- currently `UniformGrid`

### Visual spec

- sits below the main data panel
- card gutters use the standard spacing scale
- cards use `RadiusPanel`

### Functionality

- presents live or recent summary metrics
- updates as the data stream updates

### Current uses

- PT-104 four-card summary strip

## 11. Real-Time Value Header

### Purpose

A reusable header that presents the primary current device value or device state.

### Visual spec

- font family: `Manrope`
- font size: `20 px`
- primary label color: `TextPrimaryBrush`
- live value color: `AccentBrush`
- vertically aligned with data-panel toolbar buttons

### Functionality

- shows one live value or one live state
- updates continuously as readings change
- should avoid redundant helper text unless strictly necessary

### Current uses

- `Precision temperature: ...`

## 12. Plotting Window

### Purpose

A reusable time-series plotting surface for scalar data streams.

### Visual spec

- surface background: `PlotSurfaceBrush`
- border: `1 px` using `PlotBorderBrush`
- radius: `RadiusPanel = 18 px`
- fill color: `PlotFillBrush`
- line color: `AccentBrush`
- line thickness: `3 px`
- grid lines: soft dashed neutral lines

### Functionality

- shows streaming scalar data over time
- exposes x-axis and y-axis labels
- clips plot content to rounded shape
- resizes with main workspace

### Current uses

- PT-104 temperature plot

## 13. Data Panel

### Purpose

A reusable composite for:

- real-time value header
- chart-toolbar buttons
- plotting window

### Visual spec

- background: `SurfaceBrush`
- outer radius: `RadiusPanel`
- main header and toolbar aligned in one row
- plot area fills most of panel height

### Functionality

- shows current primary value
- allows data-surface actions such as clear/export
- hosts the main live plot

### Current uses

- PT-104 main data surface

## 14. Serial Data Panel

### Purpose

A reusable composite for time-series acquisition and output devices that combines:

- data panel
- statistics panel

### Functionality

- presents current live value
- shows live plot
- shows accumulated summary metrics
- supports stream actions such as clear/export

### Current uses

- PT-104 panel is the first implementation

### Scope

This pattern is suitable for:

- temperature
- pressure
- flow rate
- voltage
- current
- scalar DAQ channels

## 15. Bottom Status Bar

### Purpose

A reusable shell widget for current configuration and system state.

### Visual spec

- fixed height: `36 px`
- background: `WindowBackgroundBrush`
- top divider: `1 px` using `FooterDividerBrush`
- bottom corner radius: `0,0,15,15`
- body font: `Inter` `12 px`
- accent state text on both sides where needed

### Composition

- left summary cluster
- right system-state cluster

### Functionality

- shows connection state
- shows active channel or instance
- shows current operating configuration
- shows high-level state such as `System ready`, `Streaming`, `Idle`, `Fault`
- should be driven by structured status output, not handwritten display strings alone

### Current uses

- PT-104 footer summary

## 16. Binary Button

### Purpose

A reusable button pattern for two-state actions where separate buttons would add unnecessary visual clutter.

### Current examples

- connect/disconnect
- start live/stop live

### Visual spec

- uses the standard button height and radius
- label and icon swap with state
- background may switch family by state

### Functionality

- one click performs the state transition
- visual state should reflect current mode, not only next action

## 17. Apply And Exit Button

### Purpose

A reusable lifecycle action for safe panel termination after configuration.

### Visual spec

- same family as `Apply`
- should read as a primary lifecycle action, not a destructive close button

### Functionality

- validates settings
- applies them to hardware or runtime state
- records the applied configuration
- terminates live work cleanly
- closes the panel

### Current uses

- planned universal action-row behavior

## Current Coverage Summary

### Fully represented in code and style

- button
- dropdown box
- checkbox
- header
- header device selector
- subcontext switcher
- channel tab
- mini/max/close buttons
- top banner
- value card
- statistics panel
- real-time value header
- plotting window
- data panel
- serial data panel
- bottom status bar
- binary button

### Present in composition but not yet extracted as independent `UserControl`s

- control panel
- value card
- statistics panel
- real-time value header
- plotting window
- data panel
- serial data panel
- bottom status bar

This means the visual language and composition rules now exist, but the code still needs another pass of extraction if the goal is a true widget library rather than a shared shell plus one concrete device view.
