# Review: Panel Contract Enforcement Branch

- **Branch**: `codex/panel-contract-enforcement`
- **Worktree**: `C:\Users\Yi Zhuang\.config\superpowers\worktrees\Orchestral\panel-contract-enforcement`
- **Reviewer**: Claude Code
- **Date**: 2026-03-24

---

## Summary

The branch introduces a structured integration-panel contract (`IIntegrationPanelViewModel`) with output snapshots, lifecycle actions, and a reusable `LifecycleActionRow` widget. PT-104, IntegratedCamera, and HuaTeng panels all implement the contract. The architecture and pattern quality is high — but there is a **build-breaking regression** from deleting Core artifacts.

---

## Findings

### CC-001

- `ID`: `CC-001`
- `Severity`: `P0`
- `Area`: `Build / Core artifact deletion`
- `File`: `platform/src/ExperimentalControlPlatform.Core/Artifacts/` (deleted), `platform/src/ExperimentalControlPlatform.Devices/Capabilities/DeviceCapabilityContract.cs:2`, `platform/src/ExperimentalControlPlatform.Protocols/ProtocolSessionBehavior.cs:2`

#### Evidence

`dotnet build platform/ExperimentalControlPlatform.sln` fails with 7 errors. The entire `Core/Artifacts/` directory was deleted and replaced with `Placeholder.cs`. However, two files still reference `ExperimentalControlPlatform.Core.Artifacts`:

- `DeviceCapabilityContract.cs` uses `ArtifactId`, `CapabilityDefinition`
- `ProtocolSessionBehavior.cs` uses `ArtifactId`, `ProtocolDefinition`

#### Expected

`dotnet build` succeeds with zero errors.

#### Actual

7 CS0234/CS0246 errors. The Devices and Protocols projects fail to compile. The App project (which depends on Devices) also fails. Only Core, AI, and Runtime.Tests build.

#### Why It Matters

This is a total build break. No further testing, integration, or validation is possible until this is fixed. The 33/33 test pass mentioned in the branch status must have been from a prior commit before the artifact deletion, or from a different build state.

#### Recommended Fix

Either:
1. **Restore** the deleted artifact classes (`ArtifactId`, `CapabilityDefinition`, `ProtocolDefinition`, and all others referenced by Devices/Protocols), OR
2. **Update** `DeviceCapabilityContract.cs` and `ProtocolSessionBehavior.cs` to remove their dependency on Core.Artifacts (replace with new local types or remove the `ToDefinition()` projection methods)

Option 1 is the smallest correct change. The artifact classes are part of the core domain model and are still actively referenced.

---

### CC-002

- `ID`: `CC-002`
- `Severity`: `P0`
- `Area`: `Test deletion`
- `File`: `platform/tests/ExperimentalControlPlatform.Core.Tests/Artifacts/` (deleted — 7 test files)

#### Evidence

All 7 artifact test files were deleted:
- `ArtifactBoundaryValidationTests.cs`
- `ArtifactIdTests.cs`
- `ArtifactReferenceBoundaryTests.cs`
- `DeviceDefinitionTests.cs`
- `ExperimentDefinitionTests.cs`
- `RunManifestDefinitionTests.cs`
- `StopConditionDefinitionTests.cs`

The Core.Tests project now has zero test files.

#### Expected

Test coverage for artifact boundary validation is preserved. The 19 Core tests that previously passed (on main) should still pass.

#### Actual

All Core artifact tests are deleted. `Core.Tests` is an empty project. The branch has 14 fewer tests than main (33 on main → at most 19 survive if Runtime + Devices tests still work, but build failure prevents verification).

#### Why It Matters

These tests validate the fundamental domain model — field validation rules, artifact ID uniqueness, reference integrity. Deleting them without replacement removes the safety net for the entire artifact layer.

#### Recommended Fix

Restore all 7 test files along with the Core artifact source files (see CC-001). If the artifact classes are being intentionally restructured, the tests must be migrated to match the new structure — not deleted.

---

### CC-003

- `ID`: `CC-003`
- `Severity`: `P2`
- `Area`: `AsyncRelayCommand thread safety`
- `File`: `platform/src/ExperimentalControlPlatform.App/AsyncRelayCommand.cs:40`

#### Evidence

The `_isExecuting` field is a plain `bool` with no synchronization:

```csharp
_isExecuting = true;            // line 40
NotifyCanExecuteChanged();       // line 41
await _executeAsync(parameter);  // line 42 — yields to dispatcher
```

If `Execute` is called from two sources before the first `await` yields (e.g., button double-click, command binding reevaluation), both calls could pass the `CanExecute` check before `_isExecuting` is set.

#### Expected

Re-entry guard is atomic — concurrent calls are reliably prevented.

#### Actual

There is a theoretical race window between `CanExecute` returning true and `_isExecuting = true`. In practice this is unlikely on the UI thread (single-threaded), but the class is not documented as UI-thread-only and could be used from background contexts.

#### Why It Matters

If the command is ever invoked from a non-UI thread (e.g., automated test, runtime orchestrator), the re-entry guard fails silently. The current implementation is safe for WPF button clicks (UI thread) but fragile as a general-purpose async command.

#### Recommended Fix

Either:
1. Add a comment documenting that this class must only be used from the UI thread, OR
2. Replace `_isExecuting` with `Interlocked.CompareExchange` for atomic guarding:

```csharp
private int _isExecuting;

public bool CanExecute(object? parameter)
{
    return Volatile.Read(ref _isExecuting) == 0 && (_canExecute?.Invoke(parameter) ?? true);
}

public async void Execute(object? parameter)
{
    if (Interlocked.CompareExchange(ref _isExecuting, 1, 0) != 0) return;
    ...
```

---

### CC-004

- `ID`: `CC-004`
- `Severity`: `P2`
- `Area`: `AsyncRelayCommand exception swallowing`
- `File`: `platform/src/ExperimentalControlPlatform.App/AsyncRelayCommand.cs:44-47`

#### Evidence

```csharp
catch (Exception ex)
{
    _onException?.Invoke(ex);   // if _onException is null, exception is silently swallowed
}
```

When constructed without an `onException` handler (which is optional), any exception from `_executeAsync` is silently caught and discarded. The `async void Execute` method cannot propagate the exception to the caller.

#### Expected

Exceptions are either handled or surfaced. Silent swallowing should not be the default.

#### Actual

If a caller constructs `new AsyncRelayCommand(action)` without an error handler, exceptions vanish silently. The panel code provides handlers (e.g., `HandleLifecycleCommandException`), but the class allows silent swallowing by design.

#### Why It Matters

During development or when adding new lifecycle actions, forgetting the exception handler produces invisible failures. The developer sees the button do nothing with no error message.

#### Recommended Fix

Add a fallback when `_onException` is null — at minimum, `Debug.WriteLine` or `Trace.TraceError`:

```csharp
catch (Exception ex)
{
    if (_onException is not null)
        _onException.Invoke(ex);
    else
        System.Diagnostics.Debug.WriteLine($"[AsyncRelayCommand] Unhandled: {ex}");
}
```

---

### CC-005

- `ID`: `CC-005`
- `Severity`: `P2`
- `Area`: `Duplicated contract boilerplate across three ViewModels`
- `File`: `Pt104PanelViewModel.cs`, `IntegratedCameraPanelViewModel.cs`, `HuaTengPanelViewModel.cs`

#### Evidence

All three panel ViewModels contain nearly identical implementations for:
- Five output backing fields (`_dataOutput`, `_appliedSettingsOutput`, `_statusOutput`, `_diagnosticsOutput`, `_sessionEndOutput`)
- Five diagnostic tracking fields (`_lastCommand`, `_lastHardwareResponse`, `_lastError`, `_lastStateTransition`, `_lastValidationResult`)
- `RefreshDiagnosticsOutput()` — identical across all three
- `SetLastCommand()`, `SetLastHardwareResponse()`, `SetLastError()`, `ClearLastError()`, `SetLastStateTransition()`, `SetLastValidationResult()` — identical across all three
- `HandleLifecycleCommandException()` — identical across all three

This is ~80 lines of identical code duplicated 3 times.

#### Expected

Shared behavior lives in a common base or helper to avoid copy-paste divergence.

#### Actual

Three independent copies that must be kept in sync manually. Any bug fix to diagnostics tracking must be applied to all three files.

#### Why It Matters

When the fourth device integration is added, this boilerplate will be copied again. Drift between the copies will cause subtle behavioral differences across panels.

#### Recommended Fix

Extract a base class `IntegrationPanelViewModelBase : ObservableObject` that owns the five output properties, five diagnostic fields, and all the `Set*`/`Refresh*` helper methods. The concrete ViewModels inherit from it instead of directly from `ObservableObject`.

This is non-blocking but should be addressed before adding more device panels.

---

### CC-006

- `ID`: `CC-006`
- `Severity`: `P3`
- `Area`: `LifecycleActionRow — missing null-safety on SupportedLifecycleActions`
- `File`: `platform/src/ExperimentalControlPlatform.App/Widgets/LifecycleActionRow.xaml.cs:65`

#### Evidence

```csharp
Visibility = panelViewModel.SupportedLifecycleActions.Any()
    ? Visibility.Visible : Visibility.Collapsed;
```

`SupportedLifecycleActions` is typed as `IReadOnlyList<...>` (non-nullable) on the interface, but the widget accesses it without a null check. If a future implementation returns `null` (violating the contract), this will throw `NullReferenceException`.

#### Expected

Defensive null check or `?.Any() == true` pattern.

#### Actual

Relies on the contract guarantee that `SupportedLifecycleActions` is never null.

#### Why It Matters

Minor robustness concern. The interface declares it as non-nullable, so this is technically correct. Flagging as P3 cleanup.

#### Recommended Fix

Use `panelViewModel.SupportedLifecycleActions?.Any() == true` for defensive safety.

---

## Strengths

- **Contract design is clean**: `IIntegrationPanelViewModel` with typed snapshot records is a solid data contract. Sealed records with init-only properties give immutability guarantees.
- **LifecycleActionRow is well-engineered**: Proper `PropertyChanged` attach/detach lifecycle, auto-sync from DataContext, visibility management. Good widget reuse pattern.
- **PT-104 lifecycle semantics are truthful**: Apply only exposed when connected and not live reading. This matches the hardware reality.
- **DeviceControlPanel extension is clean**: Adding `LifecycleContent` as a new slot with auto-collapsing visibility is the right approach.
- **Diagnostics tracking**: Structured breadcrumbs (last command, response, error, state transition) provide excellent observability.
- **Session end snapshots**: Capturing final status and applied settings at disconnect is a strong pattern for post-run analysis.

---

## Testing Performed

| Action | Result |
|--------|--------|
| `dotnet build platform/ExperimentalControlPlatform.sln` | **FAILED** — 7 errors (CC-001) |
| `dotnet test platform/ExperimentalControlPlatform.sln` | **Not run** — blocked by build failure |
| Static review of contract interfaces | Passed |
| Static review of output record types | Passed |
| Static review of AsyncRelayCommand | Issues found (CC-003, CC-004) |
| Static review of LifecycleActionRow | Minor issue (CC-006) |
| Static review of PT-104 contract impl | Passed — lifecycle semantics are correct |
| Static review of camera panel impls | Passed — structurally consistent with PT-104 |
| Hardware retest (PT-104) | **Not verified** — build failure prevents runtime testing |

---

## Assessment

**Blocked on P0 findings (CC-001, CC-002).** The Core artifact deletion breaks the build. Once restored, the contract enforcement work itself is solid and ready to proceed.

| Category | Status |
|----------|--------|
| Contract layer design | Ready |
| LifecycleActionRow widget | Ready |
| PT-104 contract wiring | Ready (pending hardware retest) |
| Camera contract wiring | Ready (pending build fix + runtime test) |
| Build | Broken |
| Tests | Cannot verify |

**Next steps for implementer:**
1. Fix CC-001 and CC-002 (restore Core artifacts and tests) — **blocking**
2. Fix CC-004 (exception swallowing fallback) — **important**
3. Consider CC-003 (thread safety documentation or fix) — **important**
4. Plan CC-005 (base class extraction) — **before next device panel**
5. Optionally fix CC-006 (null-safety cleanup) — **non-blocking**
