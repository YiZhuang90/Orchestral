# Review: Panel Contract Baseline and Audio Panels

- **Branch**: `codex/orchestral-design-v2-migration-full`
- **Commit reviewed**: `6624684` feat: add panel contract baseline and audio panels
- **Reviewer**: Claude Code
- **Date**: 2026-03-25

---

## Summary

Commit `6624684` adds the `IIntegrationPanelViewModel` contract and five sealed output records, the `AudioInputPanelTemplate`, the `WasapiMicrophoneService` with full analysis utilities, the `HyperCamPanelViewModel`, and the `IntegratedMicrophonePanelViewModel`. **Build passes (0 errors, 0 warnings). All 38 tests pass (Core 19 / Devices 12 / Runtime 7).**

The microphone device integration is well-structured and largely follows the expected chain. The HyperCam panel is an unanchored stub with no service layer. Two issues require prompt attention before the branch lands on main: the missing Microphone DI wiring (PC-004) and the now-urgent base class extraction (PC-003, escalation of CC-005).

---

## Findings

### PC-001

- `ID`: `PC-001`
- `Severity`: `P2`
- `Area`: `HyperCam — missing service layer in Devices`
- `File`: `platform/src/ExperimentalControlPlatform.App/DevicePanels/HyperCam/HyperCamPanelViewModel.cs`

#### Evidence

`HyperCamPanelViewModel` is instantiated with no arguments:

```csharp
var hyperCamPanel = new HyperCamPanelViewModel();
```

The ViewModel contains mock-only logic:

```csharp
_statusMessage = "Mock camera ready";
CaptureAppliedSettingsSnapshot("Mock HyperCam defaults loaded.");
```

There is no `IHyperCamService` interface in `ExperimentalControlPlatform.Devices` and no corresponding `HyperCamClient` in the App. The Devices project does contain the infrastructure for Phantom cameras (`IPhantomVendorSdk`, `PhantomV10ProbeService`, `ReflectionPhantomVendorSdk`), but `HyperCamPanelViewModel` does not reference any of it.

#### Expected

The integration follows the documented chain: `IHyperCamService → ConcreteService → HyperCamClient → HyperCamPanelViewModel`. The ViewModel takes its service as a constructor parameter.

#### Actual

Only the ViewModel (App side) exists. The Devices half of the chain is absent. The ViewModel is a self-contained mock with hardcoded defaults.

#### Why It Matters

The `HyperCamPanelViewModel` will be rendered in the DeviceTestWindow (it is wired in App.xaml.cs and has a DataTemplate). Users who open the panel will see a camera interface that produces no real data. There is no error, no warning, and no visual distinction between this stub and a real integration. Additionally, when the real Phantom SDK binding is eventually written, it cannot be injected because the ViewModel constructor accepts no dependencies.

#### Recommended Fix

Either:
1. Mark `HyperCamPanelViewModel` as a placeholder stub by adding an `IsStub = true` guard or a visible banner in the UI until the service layer exists, OR
2. Create `IHyperCamService` in `ExperimentalControlPlatform.Devices` and inject it as a constructor parameter — even if the concrete implementation behind it is still a no-op.

This is P2 (not P1) because the build and tests are unaffected, and the stub pattern is an established development approach. However it should be resolved before the next camera integration proceeds.

---

### PC-002

- `ID`: `PC-002`
- `Severity`: `P2`
- `Area`: `WasapiMicrophoneService — blocking call in CaptureSnapshot`
- `File`: `platform/src/ExperimentalControlPlatform.Devices/Audio/WasapiMicrophoneService.cs:48`

#### Evidence

```csharp
public MicrophoneFrame CaptureSnapshot(MicrophoneCaptureSettings settings)
{
    using var timeout = new CancellationTokenSource(SnapshotTimeout);
    using var linked = CancellationTokenSource.CreateLinkedTokenSource(timeout.Token);

    MicrophoneFrame? snapshot = null;
    StreamFramesAsync(
        settings,
        frame =>
        {
            snapshot = frame;
            linked.Cancel();
            return Task.CompletedTask;
        },
        linked.Token).GetAwaiter().GetResult();   // ← blocking call
    ...
}
```

`StreamFramesAsync` uses NAudio's `WaveInEvent`, which internally posts `DataAvailable` on the same thread that called `StartRecording`. On a STA (single-threaded apartment) thread — including the WPF UI thread — this can deadlock if the calling context has a synchronization context that serializes continuations.

#### Expected

`CaptureSnapshot` either (a) uses its own dedicated thread via `Task.Run` with no ambient sync context, or (b) is documented as "must not be called from a UI thread or async context."

#### Actual

`GetAwaiter().GetResult()` is called without `ConfigureAwait(false)` inside `StreamFramesAsync` (the `completion.Task` await does use `ConfigureAwait(false)`, which mitigates the risk). If `CaptureSnapshot` is ever called from the UI thread, the STA thread is blocked for up to 5 seconds while waiting for a WaveIn callback that may also need the STA pump.

#### Why It Matters

`IntegratedMicrophonePanelViewModel` will call `CaptureSnapshot` as part of its live-preview or connect flow. If that call is made from a command handler on the UI thread (which `AsyncRelayCommand` catches via `Execute` → `async void`), deadlock is possible.

#### Recommended Fix

Wrap the call inside `Task.Run` at the ViewModel level, or convert `IMicrophoneService.CaptureSnapshot` to `Task<MicrophoneFrame> CaptureSnapshotAsync(...)` and implement it with `await StreamFramesAsync(...).ConfigureAwait(false)`. The synchronous overload can remain as a convenience wrapper that internally calls `Task.Run(...).GetAwaiter().GetResult()` in a non-UI context.

---

### PC-003

- `ID`: `PC-003`
- `Severity`: `P1` *(escalation of CC-005)*
- `Area`: `ViewModel contract boilerplate — five implementations, no base class`
- `File`: `Pt104PanelViewModel.cs`, `HuaTengPanelViewModel.cs`, `IntegratedCameraPanelViewModel.cs`, `HyperCamPanelViewModel.cs`, `IntegratedMicrophonePanelViewModel.cs`

#### Evidence

`HyperCamPanelViewModel.cs` (659 lines) and `IntegratedMicrophonePanelViewModel.cs` (1073 lines) each reproduce the same pattern seen in the three previously reviewed ViewModels:

```csharp
// present verbatim in all five ViewModels:
private string? _lastCommand;
private string? _lastHardwareResponse;
private string? _lastError;
private string? _lastStateTransition;
private string? _lastValidationResult;
private IntegrationPanelDataOutput? _dataOutput;
private IntegrationPanelAppliedSettingsOutput? _appliedSettingsOutput;
private IntegrationPanelStatusOutput? _statusOutput;
private IntegrationPanelDiagnosticsOutput? _diagnosticsOutput;
private IntegrationPanelSessionEndOutput? _sessionEndOutput;

// and the ~8 helper methods: SetLastCommand, SetLastHardwareResponse,
// SetLastError, ClearLastError, SetLastStateTransition, RefreshDiagnosticsOutput,
// HandleLifecycleCommandException, ...
```

CC-005 identified this pattern at three implementations. Codex's response deferred it as "non-blocking before stabilization." The branch now has five implementations.

#### Expected

A shared `IntegrationPanelViewModelBase : ObservableObject` owns the 10 backing fields and 8 helper methods. All five ViewModels extend it.

#### Actual

Five independent copies. Any behavioral fix to diagnostics tracking or output refresh must be applied to five files and verified five times.

#### Why It Matters

The `IntegratedMicrophonePanelViewModel` at 1073 lines is already harder to audit than the earlier smaller ViewModels partly because the contract boilerplate is inlined. The sixth device panel (should one be added next) will copy the pattern again. The window for extracting the base class before divergence sets in is closing.

#### Recommended Fix

Extract `IntegrationPanelViewModelBase : ObservableObject` implementing `IIntegrationPanelViewModel` for the five shared output properties and all helper methods. Concrete ViewModels inherit it and implement only their device-specific logic. This extraction does not require any interface or DI changes — it is a pure refactor inside the App project.

This is escalated to P1 because with five implementations the drift risk is no longer theoretical. **This should be addressed before the next device panel is added.**

---

### PC-004

- `ID`: `PC-004`
- `Severity`: `P2`
- `Area`: `IntegratedMicrophonePanelViewModel not wired into running app`
- `File`: `platform/src/ExperimentalControlPlatform.App/App.xaml.cs`

#### Evidence

```csharp
// App.xaml.cs — only HyperCam is passed to MainViewModel:
var hyperCamPanel = new HyperCamPanelViewModel();
var mainViewModel = new MainViewModel(runtimeCoordinator, new[] { hyperCamPanel });
```

`IntegratedMicrophonePanelViewModel` is never instantiated or registered in `App.xaml.cs`. A DataTemplate for `IntegratedMicrophonePanelViewModel` was added to `App.xaml` (correct), but there is no code path that creates the ViewModel and passes it to the shell.

#### Expected

`IntegratedMicrophonePanelViewModel` is constructed with its `IMicrophoneService` dependency (or a placeholder implementation) and passed into `MainViewModel` alongside the camera panel, making the audio panel selectable in the device selector.

#### Actual

The microphone panel can never be reached at runtime. The DataTemplate registration in App.xaml is dead until App.xaml.cs is updated.

#### Why It Matters

The 1073-line `IntegratedMicrophonePanelViewModel` and the full `WasapiMicrophoneService` implementation cannot be integration-tested in the running application. Any behavioral bugs will not surface until the wiring is added.

#### Recommended Fix

Add to `App.xaml.cs`:

```csharp
var micService = new WasapiMicrophoneService();
var micPanel = new IntegratedMicrophonePanelViewModel(micService);
var mainViewModel = new MainViewModel(runtimeCoordinator, new[] { hyperCamPanel, micPanel });
```

(This assumes `IntegratedMicrophonePanelViewModel` accepts `IMicrophoneService` as a constructor parameter — verify the constructor signature first.)

---

### PC-005

- `ID`: `PC-005`
- `Severity`: `P3`
- `Area`: `HyperCam stub not marked as placeholder in any artifact or doc`
- `File`: `docs/architecture/AI_GUIDED_HARDWARE_INTEGRATION_SPEC.md`

#### Evidence

`HyperCamPanelViewModel` produces mock data and is wired into the app as if it were a completed integration. The `AI_GUIDED_HARDWARE_INTEGRATION_SPEC.md` (which was updated in this commit per the git stat) does not appear to mark HyperCam as an in-progress or stub integration.

#### Expected

The integration spec or a ROADMAP entry indicates that HyperCam's service layer is pending, so that Codex and Claude Code can track it as open work.

#### Actual

The stub is indistinguishable from a completed integration at the documentation level.

#### Recommended Fix

Add a "Status: stub — service layer pending" note to the HyperCam integration entry in `AI_GUIDED_HARDWARE_INTEGRATION_SPEC.md` or `DEVICE_ARCHETYPE_MAPPING.md`.

---

## Testing Performed

| Action | Result |
|--------|--------|
| `dotnet build platform/ExperimentalControlPlatform.sln` | **Passed — 0 errors, 0 warnings** |
| `dotnet test platform/ExperimentalControlPlatform.sln --no-build` | **Passed — 38/38** (Core 19, Devices 12, Runtime 7) |
| Static review: `IIntegrationPanelViewModel` and sealed output records | Passed — design is clean |
| Static review: `IMicrophoneService` / `WasapiMicrophoneService` | Issues found (PC-002) |
| Static review: `MicrophoneAnalysis` | Passed — RMS/peak/clipping logic correct |
| Static review: `HyperCamPanelViewModel` | Issues found (PC-001, PC-005) |
| Static review: App.xaml DataTemplate registrations | Issue found (PC-004) |
| Static review: boilerplate duplication | Issue found (PC-003) |
| Hardware test: real microphone / WASAPI | **Not verified** — hardware unavailable |
| Hardware test: HyperCam | **Not applicable** — stub only |

---

## Strengths

- **Contract placement is correct**: `IIntegrationPanelViewModel` in App/DevicePanels/Contracts is the right layer. Output records as sealed types with init-only properties give immutability.
- **Microphone service chain is complete**: `IMicrophoneService` → `WasapiMicrophoneService` → `IntegratedMicrophoneClient` → `IntegratedMicrophonePanelViewModel` follows the full integration pattern.
- **MicrophoneAnalysis is test-covered**: 12 new Devices.Tests cover RMS, peak, clipping, and channel selection. These are pure-function tests with no hardware dependency — appropriate for a CI pipeline.
- **AudioInputPanelTemplate wiring**: The `IntegratedMicrophonePanelViewModel → AudioInputPanelTemplate` DataTemplate is registered in App.xaml. This is the correct mechanism.
- **AsyncRelayCommand improvements land here**: The CC-003/CC-004 fixes (Interlocked, Debug.WriteLine fallback) are present in this commit. The fixes verify as expected.

---

## Assessment

**Two issues require Codex response before merge to main:**

| Finding | Severity | Merge-blocking? |
|---------|----------|-----------------|
| PC-001: HyperCam has no service layer | P2 | No — but should be acknowledged |
| PC-002: CaptureSnapshot blocks on UI thread risk | P2 | No — but should be tracked |
| PC-003: Base class extraction (CC-005 escalation) | P1 | **Yes** — five implementations; must be fixed before next device panel lands |
| PC-004: Microphone VM not wired in App.xaml.cs | P2 | **Yes** — the panel is unreachable at runtime |
| PC-005: HyperCam stub undocumented | P3 | No |

**PC-003 and PC-004 are blocking for this commit landing cleanly.** PC-004 is a small mechanical fix (one App.xaml.cs wiring addition). PC-003 is a refactor (extract base class) that protects all future device panels.

PC-001 and PC-002 are non-blocking for this commit but should have Codex responses logged before the branch is closed.
