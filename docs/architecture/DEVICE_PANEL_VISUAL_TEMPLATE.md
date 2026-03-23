# Device Panel Visual Template

## Purpose

This document turns the PT-104 device test window into a reusable visual template for future Orchestral device panels.

It is the visual companion to:

- [DESIGN_SYSTEM.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/architecture/DESIGN_SYSTEM.md)
- [DEVICE_PANEL_CONTRACT.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/architecture/DEVICE_PANEL_CONTRACT.md)

## Template Anatomy

```text
+--------------------------------------------------------------------+
| Top Shell: Device title | Tabs | Window controls                   |
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

## Top Shell

### Left

Show the device identity for the current panel.

Examples:

- `PT-104`
- `Flow Meter`
- `Valve Controller`

### Center

Use tabs for:

- multiple channels of the same device
- multiple instances of the same device family

Do not use them as a general-purpose device catalog.

### Right

Custom window controls:

- minimize
- maximize/restore
- close

## Left Rail

The left rail should be narrow, dense, and operational.

### Section order

1. identity and subtitle
2. configuration
3. actions
4. flexible spacer
5. diagnostics

### Why this order

- configuration is what the operator sets first
- actions come next
- diagnostics stays visible but secondary

## Main Workspace

### Top metric line

Use a strong `Manrope` metric line for the primary reading or state.

Example:

- `Precision Temperature: 21.547 C`

### Secondary line

Only add a second line if it carries live operational meaning. Do not keep dead helper text by default.

### Main data card

This is the largest surface in the panel.

For sensor-like devices it should contain:

- live data plot
- frame preview
- waveform

For actuator-like devices it may contain:

- state timeline
- recent commands
- pulse/status trace

### Stat strip

Use a four-card strip when the device naturally produces summary metrics.

If the device does not, replace the strip with the equivalent compact summary layout.

## Footer

The footer is part of the shell, not the device's custom layout.

### Visual behavior

- fixed height
- always visible
- no vertical growth on resize

### Resize policy

When the window height changes:

- footer height stays fixed
- left-rail gap above diagnostics absorbs change
- main data card height absorbs change

## Text and Typography

Follow [FONT_DESIGN_GUIDE.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/architecture/FONT_DESIGN_GUIDE.md).

### Summary

- `Manrope` for titles, section hierarchy, metrics
- `Inter` for controls, labels, helper text, footer text
- `sentence case` for operational headings, controls, helper and descriptive text

## Control Rules

### Buttons

- shared button family
- `Radius S`
- consistent height family
- centered text/icon layout

### Dropdowns

- same `Radius S` as buttons
- no default Windows chrome
- centered content vertically
- clickable chevron zone

### Cards

- `Radius L`
- light border
- no heavy shadow

## PT-104 Mapping

The current PT-104 window should be read as:

- `PT-104` = top-shell identity
- `Channel 1..4` = shell tabs
- left rail = configuration and actions
- main metric = precision temperature
- main data card = plot + chart actions
- stat strip = peak high / peak low / RMS variance / samples
- footer = connection + config summary + system state

## Reuse Rule

Future device panels should reuse:

- the shell
- the left-rail structure
- the main-card structure
- the footer structure

This is now implemented as:

- [DevicePanelShell.xaml](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/platform/src/ExperimentalControlPlatform.App/Shell/DevicePanelShell.xaml)
- [Pt104PanelView.xaml](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/platform/src/ExperimentalControlPlatform.App/DevicePanels/Pt104/Pt104PanelView.xaml)

They should only change:

- device-specific settings
- device-specific actions
- device-specific live content
- device-specific stat summaries

## Success Criterion

This template is successful when a new device panel can be sketched from this document before any code is copied, and the resulting panel still clearly belongs to the Orchestral family.
