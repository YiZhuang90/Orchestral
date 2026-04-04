# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Role: External Reviewer & Tester

Codex is the primary developer for this project. Claude Code's role is to **review Codex output and run tests**.

### Review Checklist

When reviewing code changes, check for:

- **Dependency flow violations** — `App → Runtime → Core ← Devices` must be respected, no circular or layer-skipping references
- **Pattern adherence** — device integrations follow `Interface → Service → Client → PanelViewModel`; artifacts are sealed record classes with validation
- **DataTemplate registration** — new device panels must have a DataTemplate entry in `App.xaml` and DI wiring in `App.xaml.cs`
- **Nullable correctness** — nullable reference types are enabled; no unguarded null dereferences
- **Async/cancellation** — async methods accept `CancellationToken`; no fire-and-forget tasks without error handling
- **UI thread safety** — UI-bound property updates from background threads use `Dispatcher.InvokeAsync`
- **Theme token usage** — colors, fonts, and radii reference `App/Theme/` resources, not hardcoded values
- **Widget reuse** — check `App/Widgets/` before approving new UI components that duplicate existing functionality
- **Test coverage** — new artifacts, runtime behavior, and device services have corresponding test classes

### Testing Workflow

```bash
# Full verification
dotnet build platform/ExperimentalControlPlatform.sln
dotnet test platform/ExperimentalControlPlatform.sln

# Targeted test runs
dotnet test platform/tests/ExperimentalControlPlatform.Core.Tests
dotnet test platform/tests/ExperimentalControlPlatform.Runtime.Tests
dotnet test platform/tests/ExperimentalControlPlatform.Devices.Tests
```

### Collaboration Protocol

Follow the structured review protocol at `docs/collaboration/REVIEW_PROTOCOL.md`. Key rules:

- **Write findings as files**, not chat summaries. Output to `docs/collaboration/reviews/YYYY-MM-DD-<topic>-review.md`.
- **Use the finding template** at `docs/collaboration/REVIEW_FINDING_TEMPLATE.md` for each issue. Every finding needs: ID, Severity (P0–P3), Area, File, Evidence, Expected, Actual, Why it matters, Recommended fix.
- **Severity classification**: P0 = data loss/safety/crash; P1 = major functional bug; P2 = moderate bug/contract mismatch; P3 = cleanup/non-blocking.
- **Separate blocking from non-blocking**. P0/P1 findings block; P2/P3 do not.
- **Report testing evidence**: what was run, what passed, what failed, what was not verified.
- **A finding is not closed** until Codex responds with a fix and retest evidence via `docs/collaboration/REVIEW_RESPONSE_TEMPLATE.md`.

---

## Build & Run Commands

```bash
# Build entire solution
dotnet build platform/ExperimentalControlPlatform.sln

# Build specific project
dotnet build platform/src/ExperimentalControlPlatform.App/ExperimentalControlPlatform.App.csproj

# Run the WPF application
dotnet run --project platform/src/ExperimentalControlPlatform.App

# Run all tests
dotnet test platform/ExperimentalControlPlatform.sln

# Run a specific test project
dotnet test platform/tests/ExperimentalControlPlatform.Core.Tests
dotnet test platform/tests/ExperimentalControlPlatform.Runtime.Tests
dotnet test platform/tests/ExperimentalControlPlatform.Devices.Tests

# Run a single test by name
dotnet test platform/ExperimentalControlPlatform.sln --filter "FullyQualifiedName~TestMethodName"
```

## Architecture

**Orchestral** is an AI-native experimental control platform replacing LabVIEW for custom research experiments with mixed hardware. Target framework: .NET 8 (net8.0-windows), WPF desktop app.

### Layered Project Structure

```
platform/src/
├── ExperimentalControlPlatform.Core        # Artifact models (ExperimentDefinition, DeviceDefinition, etc.)
├── ExperimentalControlPlatform.Protocols   # Protocol enums & communication primitives
├── ExperimentalControlPlatform.Runtime     # RuntimeCoordinator, run state machine, stop logic
├── ExperimentalControlPlatform.Devices     # Device service abstractions + implementations
├── ExperimentalControlPlatform.App         # WPF shell, ViewModels, device panels, widgets, theme
└── ExperimentalControlPlatform.AI          # AI sidecar (placeholder)
```

Dependency flow: App → Runtime → Core ← Devices; Devices → Core; App → Devices.

### Core Design Principles

- **Artifact-first**: Every meaningful concept is a structured YAML artifact (experiment.yaml, device.yaml, protocol.yaml). AI proposes changes to artifacts; humans approve; runtime executes.
- **Capability-driven binding**: Experiments depend on device capabilities/roles, not specific hardware models.
- **Protocol-aware**: Communication protocol (serial, SDK, USB) is a first-class architectural concern in device definitions.
- **Runtime safety**: The RuntimeCoordinator owns startup validation, shutdown ordering, and automatic stop conditions via `IStopConditionEvaluator`.

### WPF / MVVM Patterns

- **ObservableObject** (`App/ObservableObject.cs`): Base class with `SetProperty<T>()` for all ViewModels.
- **Manual DI**: No IoC container. Composition happens in `App.xaml.cs` where services are wired into ViewModels.
- **DataTemplate mapping**: `App.xaml` maps ViewModel types to Views. Adding a new device panel requires a new DataTemplate entry.
- **DevicePanelShell** (`Shell/DevicePanelShell.xaml`): Composite container with pluggable regions — `TitleContent`, `LeftRailContent`, `WorkspaceContent`, `FooterContent`, `HeaderContent`, and tab support. All device panels compose inside this shell.
- **Async commands**: Uses `async Task` methods (e.g., `ConnectAsync()`, `StartLivePreviewAsync()`) with `CancellationToken` for graceful shutdown, not ICommand.
- **UI thread marshaling**: `Dispatcher.InvokeAsync` for updating bound properties from background threads.

### Device Layer

Each device integration follows: **Interface → Service → Client → PanelViewModel**.

- `IUvcCameraService` / `OpenCvUvcCameraService` — generic USB cameras via OpenCvSharp4
- `IHuaTengSdk` / `NativeHuaTengSdk` — proprietary camera via P/Invoke
- `Pt104Driver` — Pico PT-104 temperature logger

Device panels implement `IDeviceTestPanelViewModel` (or derived interfaces like `ICameraPanelViewModel`, `IScalarSensorPanelViewModel`).

### Theme System

Located in `App/Theme/`: `Colors.xaml`, `Typography.xaml`, `Radii.xaml`, `Controls.xaml`. Fonts: Manrope (shell identity), Inter (controls/labels). Uses MahApps.Metro.IconPacks for Material Design icons.

### Widget Catalog

Reusable widgets in `App/Widgets/`: `CameraImageWindow`, `CameraDataPanel`, `DeviceControlPanel`, `FooterStatusBar`, `HeaderDeviceSelector`, `FrameMetadataStrip`, `PlottingWindow`, `StatisticsPanel`.

## Key Documentation

- `docs/architecture/V1_ARCHITECTURE_BLUEPRINT.md` — full system architecture
- `docs/architecture/DESIGN_SYSTEM.md` — visual design language and standards
- `docs/architecture/DEVICE_PANEL_CONTRACT.md` — how device panels integrate with the shell
- `docs/development/TECH_STACK_DECISIONS.md` — rationale for C#, WPF, SQLite/Parquet/Zarr
- `docs/development/ROADMAP_V1.md` — implementation stages
- `docs/legacy-knowledge/` — extracted knowledge from the legacy LabVIEW system (device inventory, protocols, experiment flow, safety conditions)

## Domain Context

The primary use case is **turbulence transition experiments** in pipe flow: cameras capture flow imagery, temperature sensors monitor conditions, serial devices control Reynolds number and laser triggers. The system orchestrates multi-device acquisition, real-time image-to-signal processing, and automatic stop conditions. The `devices/` directory at the repo root contains hardware integration specs for HuaTeng cameras and PT-104 temperature loggers.

## Testing

Tests use **xUnit 2.9.2**. Test projects mirror source projects. Key test areas:
- `ArtifactBoundaryValidationTests` — artifact field validation rules
- `RuntimeCoordinatorTests` — run state machine (start/stop/restart, duplicate prevention)
- Device integration tests for camera and sensor services
