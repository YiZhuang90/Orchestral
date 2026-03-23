# Device Panel Contract

## Purpose

This document defines the contract for a reusable Orchestral device panel.

The goal is to prevent future device UIs from being built by copying PT-104 markup and editing it ad hoc. A device panel should plug into a common shell, follow a common visual language, and expose a predictable set of data and actions.

The reusable widget vocabulary for those panels is defined in:

- [WIDGET_CATALOG.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/architecture/WIDGET_CATALOG.md)
- [INTEGRATION_PANEL_IO_CONTRACT.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/architecture/INTEGRATION_PANEL_IO_CONTRACT.md)

## Scope

This contract is for the first-generation device-testing and hardware-integration panels, not the final multi-device experiment runtime console.

## Contract Layers

Each device panel should be split into three layers:

1. `design system`
   - tokens and control styles
2. `device shell`
   - window frame and panel layout skeleton
3. `device-specific content`
   - the actual settings, actions, live data, and diagnostics for one device

## State Ownership Model

Every integration panel should explicitly separate three scopes of state:

1. `Device scope`
   - one physical or logical hardware instance
   - shared identity
   - shared connection lifecycle
   - shared diagnostics
   - shared capability surface
2. `Endpoint scope`
   - one channel, camera, axis, port, or similar sub-device context
   - endpoint settings
   - endpoint live data
   - endpoint history, plot, frame, or summary state
3. `Session scope`
   - idle, snap, read once, triggered live, continuous live, export, shutdown
   - background tasks
   - pending apply state
   - close and termination state

This separation is mandatory for mixed hardware structures.

Example:

- PT-104 box = `device scope`
- PT100 channel 2 or 4 = `endpoint scope`
- active live-read loop = `session scope`

The UI should never blur these scopes together.

## Shell Responsibilities

The shared shell should own:

- custom window chrome
- left title/device-selector area
- middle subcontext area
- native-like window controls
- rounded outer frame
- fixed footer/status bar
- left rail container
- right workspace container
- stat-card strip container

Device implementations should not each reinvent these.

The first extracted implementation is:

- [DevicePanelShell.xaml](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/platform/src/ExperimentalControlPlatform.App/Shell/DevicePanelShell.xaml)

## Header Contract

The top banner should follow one universal structure:

- left: `device selector`
- middle: `subcontext selector`
- right: `window controls`

### Left region

The left region should show the current device identity.

Rules:

- when only one same-class device is available, it may behave like static title text
- when multiple same-class devices are available, it should become a dropdown trigger without changing its visual role
- it should look like a title label first, not like a boxed form control

Long identity strings should be shortened for the banner surface.

Examples:

- show `Integrated camera` instead of a raw record dump
- show `PT-104 (HS337/004)` instead of a long transport-prefixed identity string when space is limited

### Middle region

The middle region should only represent subcontext within the selected device.

Examples:

- `Channel: 1 2 3 4`
- `Axis: X Y Z`
- `View: Preview Analysis`

It should not be used as a general device selector.

## Required Device Metadata

Every device panel should provide:

- `Device title`
  - example: `PT-104`
- `Identity string`
  - example: `USB:HS337/004`
- `Device selector items`
- `Selected device`
- `Subcontext set`
  - example: `Channel: 1..4`
- `Selected subcontext`
- `Capability summary`

## Left Rail Contract

The left rail should contain:

- editable configuration controls
- primary actions
- diagnostics access

The preferred left-rail order is:

1. configuration
2. actions
3. flexible spacer
4. diagnostics

### Required sections

- `Configuration`
- `Actions`
- `Diagnostics`

### Configuration responsibilities

A device panel must expose the operator-visible configuration needed for safe bring-up.

Examples:

- sensor type
- wire count
- mains filter
- sample mode
- gain
- exposure
- channel selection if not in the top shell

### Action responsibilities

A device panel must expose the minimum safe actions required to prove the device works.

Examples:

- connect
- disconnect
- read once
- start live
- stop

Settings should also be tagged by ownership:

- shared device setting
- endpoint-specific setting
- session-only setting

The panel should make that ownership legible, either by grouping or by placement.

Panels should also keep the default parameter surface small.

High-value parameters belong here.
Advanced, unstable, or rarely changed settings should move into:

- `More settings`
- diagnostics
- secondary dialogs

## Main Workspace Contract

The right workspace should communicate the current live behavior of the device.

### Required content

- main metric or primary state
- main content card

The main metric line should be strong and concise.

Only add a second line if it carries live operational meaning. Dead helper text should not remain in the visible panel by default.

### Optional content depending on device type

- live plot
- image preview
- waveform
- event log
- actuator timeline
- recent commands

The workspace should reflect endpoint state, not only the currently visible UI state.

If background acquisition continues on non-visible endpoints, the contract should still preserve that endpoint state internally.

If the device naturally produces summary metrics, the workspace should also expose a compact stat strip below the main content card.

If it does not, this region may be replaced with a more suitable summary surface.

## Stats Contract

If the device produces a continuous data stream, the panel should expose a compact stat strip below the main content card.

Examples:

- peak high
- peak low
- RMS variance
- sample count
- min/max
- frame rate

If the device does not naturally generate those, this region may be replaced with a more suitable summary strip.

## Footer Contract

The footer is fixed-height and should summarize the current operational state.

### Left side

Configuration and connection state, for example:

- hardware connected/disconnected
- active channel
- measurement type
- wire mode
- mains frequency
- filtered/raw mode

The footer should distinguish device-scope facts from endpoint-scope facts.

### Right side

The current high-level system state, for example:

- system ready
- streaming
- idle
- fault

## Lifecycle Contract

Every panel should define the behavior of:

- `Apply`
- `Apply and exit`
- `Close without apply`
- `Disconnect and close` when relevant

### Apply

`Apply` should:

- validate visible settings,
- write or stage them to the correct ownership scope,
- update the applied-settings record,
- and leave the panel open.

### Apply and exit

`Apply and exit` should:

- validate settings,
- apply them to the correct scope,
- persist or log the final applied configuration,
- terminate the live session or connection cleanly,
- emit a session-end record,
- and close the panel.

### Close without apply

`Close without apply` should:

- leave unapplied edits unapplied,
- terminate or preserve the session according to the panel's explicit policy,
- and never silently pretend settings were applied.

### Disconnect and close

If present, `Disconnect and close` should:

- stop live behavior,
- terminate the device connection,
- emit final status and diagnostics records,
- and close the panel.

## Data Contract

A device panel should expose data in a structured way, not only as UI text.

Suggested fields:

- `CurrentValue`
- `CurrentValueDisplay`
- `StatusMessage`
- `SystemState`
- `ConnectedDeviceId`
- `PlotSeries`
- `StatCards`
- `FooterSegments`

That structured data should be treated as panel output, not only rendering input.

The detailed output model is defined in:

- [INTEGRATION_PANEL_IO_CONTRACT.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/architecture/INTEGRATION_PANEL_IO_CONTRACT.md)

## View-Model Contract

Each device panel should provide a view-model that can answer:

- what settings exist
- which settings are editable
- which actions are enabled
- what the current live state is
- what the footer should show
- what the device selector should show
- what the subcontext selector should show
- what the panel outputs are
- what the apply and exit lifecycle should emit

The shell should not need PT-104-specific knowledge to render these basics.

## Template Consumption Rule

When implementing a new device panel, the default order should be:

1. consume an existing device-class template
2. consume the high-level general device shell if no class template exists yet
3. introduce a new device-class template only when the interaction family is reusable
4. build a one-off panel only as a last resort

The contract exists to make this order practical and enforceable.

## Visual Contract

Each device panel must follow the Orchestral design system:

- typography, layout, and tokens from [DESIGN_SYSTEM.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/architecture/DESIGN_SYSTEM.md)

That includes:

- sentence case for operational headings
- `Radius S` for controls
- `Radius L` for grouped surfaces
- shared button styles
- shared dropdown style
- fixed footer behavior

## Sensor vs Actuator Variation

The contract should allow device-type-specific differences without breaking the shell.

### Sensor-like device

- main metric
- live plot
- stat cards

### Actuator-like device

- state readout
- command controls
- recent command history
- optional state-confirmation view

### Hybrid device

- whichever of the two is primary
- secondary content can appear in the main card or stat strip

## PT-104 as Reference

PT-104 is the first scalar-sensor implementation of this contract.

It should be treated as the validation reference for:

- shell behavior
- left-rail structure
- live-stream card
- stat-card strip
- footer status layout

But PT-104 should not be treated as the universal template for all device classes.

That role belongs to:

- the high-level shell contract,
- the IO contract,
- and the device-class templates built on top of them.

PT-104 remains the visual and behavioral reference for:

- scalar-sensor shell composition
- left-rail order
- main metric plus data-card pattern
- stat-strip pattern
- footer summary pattern

The next device panel should reuse this contract instead of copying PT-104 screen code directly.

## Success Criterion

This contract is successful when the second device can be built by implementing device content and bindings against the shared shell, rather than by cloning PT-104 XAML and changing labels.
