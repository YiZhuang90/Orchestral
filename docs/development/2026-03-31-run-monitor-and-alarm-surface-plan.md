# Run Monitor And Alarm Surface Plan

## Goal

Deliver the first real experiment monitor slice for Orchestral:

- a background monitor session that consumes runtime/coordinator/controller/device state
- a monitor panel that uses the shared device-panel shell grammar
- live run controls and status presentation in one experiment-facing surface
- first warning/alarm aggregation with truthful runtime ownership

## Required Foundation Docs

- `C:\Users\Yi Zhuang\OneDrive\Codes\Projects\Orchestral\docs\development\MODULE_DEVELOPMENT_WORKFLOW.md`
- `C:\Users\Yi Zhuang\OneDrive\Codes\Projects\Orchestral\docs\collaboration\REVIEW_PROTOCOL.md`
- `C:\Users\Yi Zhuang\OneDrive\Codes\Projects\Orchestral\docs\collaboration\SUBAGENT_DEVELOPMENT_CONTRACT.md`
- `C:\Users\Yi Zhuang\OneDrive\Codes\Projects\Orchestral\docs\development\2026-03-28-functional-unit-roadmap.md`
- `C:\Users\Yi Zhuang\OneDrive\Codes\Projects\Orchestral\docs\architecture\DEVICE_RUNTIME_IO_ARCHITECTURE.md`
- `C:\Users\Yi Zhuang\OneDrive\Codes\Projects\Orchestral\docs\architecture\SYSTEM_LANGUAGE_SPEC.md`

Conflict winner:

- implementation truth follows the branch-local runtime/session architecture and the current functional-unit roadmap for this slice

## Current Reality

- the runtime coordinator owns run lifecycle, but exposes only a latest snapshot and no change stream
- the controller unit exists, but the app does not yet surface it through a monitor panel
- the app shell still hosts a single device panel as the main workspace
- there is no experiment-plane monitor backend, no warning/alarm model, and no run monitor panel
- the run recorder writes run artifacts, but does not yet receive monitor items

## Scope

In scope:

- add a background `ExperimentMonitorSession`
- add a normalized monitor snapshot/item model with warning and alarm severity
- add coordinator change notifications needed by the monitor
- add monitor-device normalization over current runtime sessions and controller state
- add an experiment monitor panel/viewmodel using the shared `DevicePanelShell`
- add run controls in the panel:
  - run index
  - initialize
  - start
  - stop
  - display-rate selection (`5` to `30 fps`, step `5`) for UI only
- show primary control status in `Target +/- Error` form when available
- add a bottom status bar summarizing run/health state
- feed monitor warnings/alarms into run recording artifacts

Out of scope:

- experiment-specific scientific processing pipelines
- closed-loop actuation beyond the existing controller-unit decision model
- broad multi-window shell redesign
- full multi-target schedule editor UI
- system-level automatic stop-condition execution beyond first alarm/status surfacing

## Implementation Shape

### Runtime

- add change notification to `IRuntimeCoordinator` / `RuntimeCoordinator`
- add monitor models:
  - `ExperimentMonitorSeverity`
  - `ExperimentMonitorItem`
  - `ExperimentMonitorDeviceSnapshot`
  - `ExperimentMonitorSnapshot`
- add `ExperimentMonitorSession`
  - subscribes to coordinator changes
  - samples controller state
  - inspects known runtime sessions from the registry
  - emits a normalized snapshot stream/snapshot
  - classifies first warnings/alarms
- define first V1 rules:
  - controller measured value stale -> warning
  - disconnected required controlled device while run active -> alarm
  - session diagnostics with `LastError` -> warning or alarm depending on device role
  - runtime `Stopping` -> informational monitor item

### App

- add `ExperimentMonitorViewModel`
- add `ExperimentMonitorPanelTemplate.xaml`
- make the monitor panel the primary `CurrentDevicePanel` surface in the app shell
- retain device panels as underlying session clients/providers
- wire initialize/start/stop through the app view model and coordinator
- expose display-rate dropdown as UI-only decimation for monitor display updates
- show:
  - live health items
  - device status cards
  - primary control target `Target +/- Error`
  - bottom status bar summary

### Recording

- extend the run recorder/artifact writer to persist monitor warnings/alarms and latest monitor snapshot data into run artifacts

## Test Plan

Runtime tests:

- monitor snapshot updates when coordinator state changes
- controller `Target +/- Error` appears in monitor snapshot
- stale controller measured value becomes a warning
- disconnected control-center session while running becomes an alarm

App tests:

- main view model exposes the monitor panel as current primary panel
- initialize/start/stop commands update the monitor surface state
- display-rate selection only affects presentation timing and not runtime state
- status bar summary reflects monitor severity

## Verification

- `dotnet build .\platform\ExperimentalControlPlatform.sln -nodeReuse:false -v:minimal`
- `dotnet test .\platform\ExperimentalControlPlatform.sln --no-build -nodeReuse:false -v:minimal`
- smoke launch the app and confirm the experiment monitor panel loads as the primary surface

## Review Gate

This slice requires external review because it is:

- cross-cutting
- runtime/lifecycle related
- shared foundation
- hard to verify through tests alone

Review artifacts go under:

- `docs/collaboration/reviews/`

Preferred reviewer:

- `claude-opus-4-6`

## Success Criteria

- there is a real monitor backend session in the runtime layer
- the app shows a real experiment monitor panel as the primary run surface
- monitor items, warnings, and alarms are not UI-invented; they come from runtime-backed state
- the primary control target can be shown as `Target +/- Error`
- run artifacts include monitor warnings/alarms
- tests, verification, and external review are complete
