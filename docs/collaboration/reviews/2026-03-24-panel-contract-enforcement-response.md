# Response: Panel Contract Enforcement Branch

- **Branch**: `codex/panel-contract-enforcement`
- **Worktree**: `C:\Users\Yi Zhuang\.config\superpowers\worktrees\Orchestral\panel-contract-enforcement`
- **Response author**: Codex
- **Date**: 2026-03-24
- **Related review**: `2026-03-24-panel-contract-enforcement-review.md`

---

## Disposition

### CC-001

- **Status**: Accepted and fixed
- **Resolution**:
  - restored `platform/src/ExperimentalControlPlatform.Core/Artifacts/`
  - restored `platform/tests/ExperimentalControlPlatform.Core.Tests/Artifacts/`
- **Verification**:
  - `dotnet build .\ExperimentalControlPlatform.sln` passed

### CC-002

- **Status**: Accepted and fixed
- **Resolution**:
  - restored the deleted Core artifact tests along with the artifact source files
- **Verification**:
  - `dotnet test .\ExperimentalControlPlatform.sln --no-build` passed
  - Core tests: `19/19`

### CC-003

- **Status**: Accepted and fixed
- **Resolution**:
  - hardened `platform/src/ExperimentalControlPlatform.App/AsyncRelayCommand.cs`
  - replaced the re-entry `bool` with `Interlocked.CompareExchange` / `Volatile.Read`
- **Verification**:
  - solution build and tests passed after the change

### CC-004

- **Status**: Accepted and fixed
- **Resolution**:
  - added fallback `Debug.WriteLine(...)` when `AsyncRelayCommand` has no exception handler
- **Verification**:
  - solution build and tests passed after the change

### CC-005

- **Status**: Accepted as follow-up, not implemented in this pass
- **Reason**:
  - the duplication is real, but extracting a shared base class is refactoring, not a branch blocker
  - this branch first needed to become build-clean and semantically correct
- **Follow-up**:
  - candidate next cleanup after branch stabilization

### CC-006

- **Status**: Rejected as required work
- **Reason**:
  - `SupportedLifecycleActions` is non-null by contract in `IIntegrationPanelViewModel`
  - defensive null handling is optional cleanup, not a meaningful defect under the current interface rules

---

## Additional Work Completed During Review Handling

- fixed camera lifecycle truthfulness issues identified in a parallel review pass:
  - idle camera `Apply` now performs a bounded hardware apply path instead of only staging drafts
  - failed live restarts no longer get overwritten as false success
  - lifecycle command enablement is refreshed when `CanApplySettings` flips

---

## Final Verification

- `dotnet build .\ExperimentalControlPlatform.sln`
  - **Passed**
- `dotnet test .\ExperimentalControlPlatform.sln --no-build`
  - **Passed**
  - Runtime: `7/7`
  - Devices: `7/7`
  - Core: `19/19`
