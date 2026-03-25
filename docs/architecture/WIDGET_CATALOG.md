# Orchestral Widget Catalog

## Purpose

This document defines the reusable widget set for the current Orchestral device-panel generation after the shared v2 token, shell, and template migration.

It exists to make the UI reusable by composition rather than by copying PT-104 markup. Each widget below is treated as a reusable module with:

- visual rules
- interaction rules
- data/function contract
- composition role

Read with:

- [DESIGN_SYSTEM.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/architecture/DESIGN_SYSTEM.md)
- [DEVICE_PANEL_CONTRACT.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/architecture/DEVICE_PANEL_CONTRACT.md)

## Token Baseline

These widget definitions use the live token system implemented in:

- [Colors.xaml](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/platform/src/ExperimentalControlPlatform.App/Theme/Colors.xaml)
- [Typography.xaml](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/platform/src/ExperimentalControlPlatform.App/Theme/Typography.xaml)
- [Radii.xaml](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/platform/src/ExperimentalControlPlatform.App/Theme/Radii.xaml)
- [Controls.xaml](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/platform/src/ExperimentalControlPlatform.App/Theme/Controls.xaml)

### Current core tokens

- `FontShellTitle = 15 px`
- `FontDisplayValue = 22 px`
- `FontSectionHeader = 15 px`
- `FontBody = 12 px`
- `FontMicro = 10 px`
- `RadiusControl = 4 px`
- `RadiusPanel = 10 px`
- `RadiusWindowOuter = 12 px`
- `RadiusWindowInner = 11 px`

### Current core colors

- `WindowBackgroundBrush = #F6F4EE`
- `RailBackgroundBrush = #EEEBE3`
- `SurfaceBrush = #FBF8F2`
- `MutedSurfaceBrush = #F1EEE6`
- `PlotSurfaceBrush = #F8F5EF`
- `TextPrimaryBrush = #302F2C`
- `TextSecondaryBrush = #666158`
- `TextMutedBrush = #6B655D`
- `AccentBrush = #9A462A`
- `AccentHoverBrush = #A44D2D`
- `AccentPressedBrush = #7B341F`
- `AccentForegroundBrush = #FBF5F2`
- `DarkButtonBrush = #5F5E5E`
- `SoftButtonBrush = #E4DED4`
- `SoftBorderBrush = #DED7CB`
- `PlotBorderBrush = #E7E0D5`

## Widget Overview

The reusable widget set is organized into:

- primitive widgets
- composite widgets
- shell widgets

### Primitive widgets

- button
- dropdown box
- checkbox
- header text
- header device selector
- subcontext tab
- value card
- mini/max/close buttons

### Composite widgets

- control panel
- lifecycle action row
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
- default height: `42 px`
- radius: `RadiusControl = 4 px`
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
  - height: `38 px`
  - minimum width: `124 px`
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
- full-width `Apply` directly below the parameter section in the rail

## 2. Dropdown Box

### Purpose

A reusable selection widget for discrete configuration parameters such as sensor type, wire count, mains filter, trigger mode, gain mode, channel mode, or acquisition profile.

### Visual spec

- font: `Inter`
- font size: `12 px`
- control height: `42 px`
- text vertically centered
- outer container background: `SurfaceBrush`
- border: `1 px` using `SoftBorderBrush`
- radius: `RadiusControl = 4 px`
- no default Windows chrome

### Functionality

- opens list on click anywhere on the control, including the chevron area
- selects one item from a discrete list
- updates a bound property
- supports popup dropdown with the same sharper corner language

## 3. Checkbox

### Purpose

A reusable boolean control for single binary options.

### Visual spec

- font: `Inter`
- font size: `12 px`
- low-contrast visual treatment
- aligned with body text

### Functionality

- toggles a boolean state
- use only for true/false options where a binary button would be visually heavier than necessary

## 4. Header

### Purpose

A reusable textual identity or section-heading widget.

### Visual spec

- shell/device identity: `Manrope`, `15 px`
- primary live value: `Manrope`, `22 px`
- section value/card value: `Manrope`, `15 px`
- small labels and metadata: `Inter`, `10 px`
- sentence case by default

### Functionality

- presents structure, identity, or current primary value
- is not interactive by default

## 4A. Header Device Selector

### Purpose

A reusable header widget that shows the current device identity and becomes a dropdown selector when multiple same-class devices are available.

### Visual spec

- same visual role as shell title text
- no boxed input chrome
- same font and alignment as the device name label
- optional subtle chevron only when multiple choices exist
- default max width around `280 px` with ellipsis

### Functionality

- shows the selected device
- opens a device list on click when multiple device instances exist
- should not be mistaken for a form control in the rail

## 4B. Subcontext Switcher

### Purpose

A reusable middle-banner widget for switching endpoint or internal context within the already selected device.

### Visual spec

- static label such as `Channel:` or `Mode:`
- followed by compact tabs
- occupies the middle banner only when needed

### Functionality

- switches channel, mode, view, or comparable device-internal context
- does not switch between different hardware instances

### Current uses

- `Channel: 2 4` on PT-104

## 5. Subcontext Tab

### Purpose

A reusable selector for device-internal contexts such as channels, modes, or views.

### Visual spec

- font: `Manrope`
- font size: `12 px`
- padding: `14,6`
- underline height: `3 px`
- selected text color: `AccentBrush`
- idle text color: `TextMutedBrush`
- hover background: `MutedSurfaceBrush`

### Functionality

- switches current channel, mode, or comparable device-internal context
- updates the bound selected item
- should not be used as a device catalog; device switching belongs to the left header selector

### Current uses

- `Channel: 2 4` on PT-104

## 6. Mini / Max / Close Buttons

### Purpose

Reusable custom window-control buttons for shell windows and Orchestral modals.

### Visual spec

- size: `30 x 24`
- radius: `RadiusControl = 4 px`
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

## 7. Top Banner

### Purpose

A reusable shell widget that combines:

- device selector
- subcontext selector
- mini/max/close buttons

### Visual spec

- height: `52 px`
- background: `SurfaceBrush`
- bottom divider: `1 px` using `ShellDividerBrush`
- top corner radius: `11,11,0,0`
- left/right content margin: `16 px` left, `12 px` right

### Functionality

- shows device identity
- switches device instance from the left region
- switches device-internal context from the middle region
- supports title-bar dragging
- hosts native-like window controls

## 8. Control Panel

### Purpose

A reusable left-rail module that groups configuration widgets and action widgets for one device.

### Composition

- dropdown box
- checkbox
- lifecycle action row
- binary button
- standard action button
- diagnostics button

### Visual spec

- background is provided by the left rail
- vertical spacing follows the shared compact spacing family
- configuration labels use `Inter` `10 px`
- buttons follow the shared button family

### Functionality

- edits device configuration
- issues safe bring-up actions
- exposes diagnostics
- makes shared vs endpoint settings legible
- exposes default parameters only and pushes advanced settings into secondary surfaces

## 8A. Lifecycle Action Row

### Purpose

A reusable action cluster for panel lifecycle and operational commands.

### Typical actions

- `Apply`
- `Apply and exit`
- `Connect`
- `Start live`
- `Stop live`
- `Diagnostics`

### Visual spec

- `Apply` sits directly below the parameter block
- `Apply` uses full available rail width
- lifecycle actions should read as one ordered stack, not as loose floating buttons

### Functionality

- groups actions by lifecycle meaning rather than arbitrary placement
- makes the difference between apply, start, stop, and exit explicit
- emits structured lifecycle events, not only visual changes

## 9. Value Card

### Purpose

A reusable card for presenting one summary metric.

### Visual spec

- background: `SurfaceBrush`
- radius: `RadiusPanel = 10 px`
- padding: `12,10`
- border: `1 px` `SoftBorderBrush`
- label font: `Inter` `10 px`
- value font: `Manrope` `15 px`

### Functionality

- presents one bound summary value
- may update live during acquisition
- should not expose direct controls

## 10. Statistics Panel

### Purpose

A reusable summary strip composed of multiple value cards.

### Composition

- 2 to 6 value cards
- currently a simple equal-width strip

### Visual spec

- sits below the main data surface
- card gutters use the standard spacing scale
- cards use `RadiusPanel`

### Functionality

- presents live or recent summary metrics
- updates as the data stream updates

## 11. Real-Time Value Header

### Purpose

A reusable header that presents the primary current device value or device state.

### Visual spec

- label font: `Inter` `12 px`
- value font: `Manrope` `22 px`
- label color: `TextSecondaryBrush`
- live value color: `AccentBrush`
- aligned with data-panel toolbar buttons

### Functionality

- shows one live value or one live state
- updates continuously as readings change
- avoids redundant helper text

## 12. Plotting Window

### Purpose

A reusable time-series plotting surface for scalar data streams.

### Visual spec

- surface background: `PlotSurfaceBrush`
- border: `1 px` using `PlotBorderBrush`
- radius: `RadiusPanel = 10 px`
- fill color: `PlotFillBrush`
- line color: `AccentBrush`
- line thickness: `3 px`
- grid lines: soft dashed neutral lines
- axis labels use `FontMicro`

### Functionality

- shows streaming scalar data over time
- exposes x-axis and y-axis labels
- clips plot content to the rounded shape
- resizes with the main workspace

## 13. Data Panel

### Purpose

A reusable composite for:

- real-time value header
- chart-toolbar buttons
- plotting or equivalent main surface

### Visual spec

- background: `SurfaceBrush`
- outer radius: `RadiusPanel`
- main header and toolbar aligned in one row
- dominant content surface fills most of panel height

### Functionality

- shows the current primary value
- allows data-surface actions such as clear/export
- hosts the main live surface

## 14. Serial Data Panel

### Purpose

A reusable composite for time-series acquisition devices that combines:

- data panel
- statistics panel

### Functionality

- presents the current live value
- shows a live plot
- shows accumulated summary metrics
- supports stream actions such as clear/export

### Scope

Suitable for:

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

- fixed height family around `36 px`
- background: `SurfaceBrush`
- top divider: `1 px` using `FooterDividerBrush`
- bottom corner radius: `0,0,11,11`
- body font: `Inter` `12 px`
- accent state text where needed

### Composition

- left summary cluster
- right system-state cluster

### Functionality

- shows connection state
- shows active context or mode
- shows current operating configuration
- shows high-level state such as `System ready`, `Streaming`, `Idle`, `Fault`
- should be driven by structured status output, not handwritten display strings alone

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
- visual state should reflect the current mode, not only the next action

## 17. Apply And Exit Button

### Purpose

A reusable lifecycle action for safe panel termination after configuration.

### Visual spec

- same family as `Apply`
- should read as a lifecycle action, not a destructive close button

### Functionality

- validates settings
- applies them to hardware or runtime state
- records the applied configuration
- terminates live work cleanly
- closes the panel

## Current Coverage Summary

### Represented in code and style

- button
- dropdown box
- checkbox
- header
- header device selector
- subcontext tab
- mini/max/close buttons
- top banner
- control panel
- lifecycle action row
- value card
- statistics panel
- real-time value header
- plotting window
- data panel
- serial data panel
- bottom status bar
- binary button

## Future Widget Roadmap

Likely next reusable widgets:

- ROI editor
- histogram panel
- waveform panel
- spectrum panel
- actuator control panel
- state timeline
- command history panel
- protocol monitor panel
- connection trace panel
- calibration panel
- recording panel
- event timeline

These should stay inside the current token system and should not create a second visual language.
