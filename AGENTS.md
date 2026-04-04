# AGENTS.md

This file provides guidance to Codex when working with code in this repository.

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

**Always build the full solution and run all tests after making changes.**

## Architecture

**Orchestral** is an AI-native experimental control platform replacing LabVIEW for custom research experiments with mixed hardware. Target framework: .NET 8 (`net8.0-windows` for App, `net8.0` for libraries). WPF desktop app.

### Layered Project Structure

```
platform/src/
├── ExperimentalControlPlatform.Core        # Artifact models (sealed record classes with validation)
├── ExperimentalControlPlatform.Protocols   # Protocol enums & communication primitives
├── ExperimentalControlPlatform.Runtime     # RuntimeCoordinator, run state machine, stop logic
├── ExperimentalControlPlatform.Devices     # Device service abstractions + implementations
├── ExperimentalControlPlatform.App         # WPF shell, ViewModels, device panels, widgets, theme
└── ExperimentalControlPlatform.AI          # AI sidecar (placeholder)
```

**Dependency flow: `App → Runtime → Core ← Devices; Devices → Core; App → Devices`.** Never introduce circular dependencies or skip layers.

### Repository Layout Beyond platform/

```
devices/          # Hardware integration specs with Python probe scripts (pt104/, huateng-camera/)
legacy/           # Reference LabVIEW code — read-only knowledge, NOT the target architecture
experiments/      # Reserved for experiment packages
schemas/          # Reserved for artifact and system-language schemas
temp/             # Standalone hardware test harness projects (for isolated device testing)
docs/             # Architecture, development strategy, legacy knowledge extraction
```

## Patterns You Must Follow

### Artifact Models

All domain concepts in `Core/Artifacts/` are **sealed record classes** with validation logic. When adding a new artifact type:
- Create a sealed record class in `Core/Artifacts/`
- Add field validation in the constructor or a `Validate()` method
- Add corresponding test class in `Core.Tests/Artifacts/`

### Device Integration

Every device follows: **Interface → Service → Client → PanelViewModel**.

1. Define an interface in `Devices/` (e.g., `IUvcCameraService`)
2. Implement the service in `Devices/` (e.g., `OpenCvUvcCameraService`)
3. Create a client wrapper in `App/DevicePanels/{DeviceName}/` if needed
4. Create a PanelViewModel in `App/DevicePanels/{DeviceName}/`
5. Create a PanelView (XAML) in `App/DevicePanels/{DeviceName}/`
6. Register the DataTemplate mapping in `App.xaml`
7. Wire DI in `App.xaml.cs`

Existing integrations: UVC cameras (OpenCvSharp4), HuaTeng cameras (P/Invoke), Pico PT-104 temperature logger (serial).

Device panels implement `IDeviceTestPanelViewModel` (or derived interfaces: `ICameraPanelViewModel`, `IScalarSensorPanelViewModel`).

### WPF / MVVM

- **ObservableObject** (`App/ObservableObject.cs`): Base class with `SetProperty<T>()` for all ViewModels. Always inherit from this.
- **Manual DI**: No IoC container. All composition happens in `App.xaml.cs`. Wire new services and ViewModels there.
- **DataTemplate mapping**: `App.xaml` maps ViewModel types to Views. **Every new panel requires a DataTemplate entry.**
- **DevicePanelShell** (`Shell/DevicePanelShell.xaml`): Composite container with pluggable regions — `TitleContent`, `LeftRailContent`, `WorkspaceContent`, `FooterContent`, `HeaderContent`, and tab support. All device panels compose inside this shell.
- **Async patterns**: Use `async Task` methods with `CancellationToken` for graceful shutdown, not `ICommand`.
- **UI thread marshaling**: Always use `Dispatcher.InvokeAsync` when updating bound properties from background threads. Never update UI-bound properties directly from a non-UI thread.
- **Modal windows**: Extend `OrchestralModalWindow` (`App/Modals/`) for dialog windows.

### Theme System

Located in `App/Theme/`. Use existing design tokens — do not hardcode colors, fonts, or radii.

- `Colors.xaml` — color palette (use `StaticResource` references)
- `Typography.xaml` — font definitions: **Manrope** for shell identity, **Inter** for controls/labels
- `Radii.xaml` — border radius tokens
- `Controls.xaml` — control templates and styles

Icons: MahApps.Metro.IconPacks (Material Design).

### Widget Reuse

Check `App/Widgets/` before creating new UI components. Existing reusable widgets:
- `CameraImageWindow` — camera frame display with overlay support
- `CameraDataPanel` — camera metadata display
- `DeviceControlPanel` — device control buttons/actions
- `FooterStatusBar` — status bar with connection/run state
- `HeaderDeviceSelector` — device selection dropdown
- `FrameMetadataStrip` — frame info strip
- `PlottingWindow` — real-time data plotting
- `StatisticsPanel` — live statistics display
- `ValueCard` — single-value display card

## C# Conventions

From `Directory.Build.props`:
- **Nullable reference types**: enabled — use `?` for nullable types, handle null properly
- **Implicit usings**: enabled — no need for `using System;` etc.
- **LangVersion**: latest — use modern C# features (records, pattern matching, file-scoped namespaces)

## Testing

Tests use **xUnit 2.9.2**. Test projects mirror source projects in `platform/tests/`.

- `Core.Tests` — artifact validation (boundary tests, reference integrity)
- `Runtime.Tests` — state machine behavior (start/stop/restart, duplicate prevention)
- `Devices.Tests` — device service integration tests

When adding new functionality, add corresponding tests in the matching test project.

## Domain Context

The primary use case is **turbulence transition experiments** in pipe flow: cameras capture flow imagery, temperature sensors monitor conditions, serial devices control Reynolds number and laser triggers. The system orchestrates multi-device acquisition, real-time image-to-signal processing, and automatic stop conditions.

The `devices/` directory at repo root contains hardware integration specs and Python probe scripts for device discovery. The `docs/legacy-knowledge/` directory has extracted knowledge from the legacy LabVIEW system (device inventory, protocols, experiment flow, safety conditions).

## Collaboration Protocol

Follow the structured review protocol at `docs/collaboration/REVIEW_PROTOCOL.md` when receiving reviews from Claude Code.

- **Respond to every finding** using the template at `docs/collaboration/REVIEW_RESPONSE_TEMPLATE.md`. Each finding requires: Decision (Accepted/Rejected/Partially accepted), Reasoning, Fix summary, Changed files, Retest status.
- **Write response files** to `docs/collaboration/reviews/YYYY-MM-DD-<topic>-response.md`.
- **Never mark a finding fixed without retest evidence** — include the command run, interaction path, and internal signal checked.
- **Keep architecture decisions in `docs/architecture/`**, not in review responses.
- **Severity levels**: P0 = data loss/safety/crash; P1 = major functional bug; P2 = moderate bug/contract mismatch; P3 = cleanup/non-blocking. P0/P1 findings are blocking.

## Key Documentation

- `docs/architecture/V1_ARCHITECTURE_BLUEPRINT.md` — full system architecture
- `docs/architecture/DESIGN_SYSTEM.md` — visual design language and standards
- `docs/architecture/DEVICE_PANEL_CONTRACT.md` — how device panels integrate with the shell
- `docs/architecture/WIDGET_CATALOG.md` — widget reference and usage
- `docs/development/TECH_STACK_DECISIONS.md` — rationale for C#, WPF, SQLite/Parquet/Zarr
- `docs/development/ROADMAP_V1.md` — implementation stages
- `docs/legacy-knowledge/` — extracted LabVIEW system knowledge
- `docs/collaboration/` — review protocol, finding template, response template
