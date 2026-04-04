# Runtime IO Execution Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Land the first runtime-IO foundation in Orchestral, starting with the integrated microphone session, then extend the same runtime model into the first controlled-device pilot and the next acquisition migrations.

**Architecture:** Runtime truth must live in device sessions, not in panel view models. The rollout sequence is: land the microphone runtime slice, make the host own the session registry, validate one controlled-device session, then migrate PT-104 and the camera family onto the same session/port/registry model.

**Tech Stack:** C#/.NET 8, WPF, `ExperimentalControlPlatform.Runtime`, `ExperimentalControlPlatform.Devices`, xUnit, runtime session registry, snapshot/stream output ports, worktree-based landing flow.

---

## Required Starting Point

This plan assumes the current runtime-IO implementation work exists in the dedicated worktree branch:

- branch: `codex/runtime-io-microphone`
- worktree: `C:\Users\Yi Zhuang\.config\superpowers\worktrees\Orchestral\runtime-io-microphone`

The plan should not start from `main` directly because the microphone runtime session slice currently exists only in that worktree state.

## Required Foundation Docs

Required foundation:

- [DEVICE_RUNTIME_IO_ARCHITECTURE.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/architecture/DEVICE_RUNTIME_IO_ARCHITECTURE.md)
- [TIMING_AND_SYNCHRONIZATION_STRATEGY.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/architecture/TIMING_AND_SYNCHRONIZATION_STRATEGY.md)
- [INTEGRATION_PANEL_IO_CONTRACT.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/architecture/INTEGRATION_PANEL_IO_CONTRACT.md)
- [DEVICE_PANEL_CONTRACT.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/architecture/DEVICE_PANEL_CONTRACT.md)
- [DEVICE_ARCHETYPE_MAPPING.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/architecture/DEVICE_ARCHETYPE_MAPPING.md)
- [MODULE_DEVELOPMENT_WORKFLOW.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/development/MODULE_DEVELOPMENT_WORKFLOW.md)
- [REVIEW_PROTOCOL.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/collaboration/REVIEW_PROTOCOL.md)

Conflict winner:

- [DEVICE_RUNTIME_IO_ARCHITECTURE.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/architecture/DEVICE_RUNTIME_IO_ARCHITECTURE.md)

## File Map

### Runtime foundation files

- Modify: `platform/src/ExperimentalControlPlatform.Runtime/ExperimentalControlPlatform.Runtime.csproj`
- Create/Modify: `platform/src/ExperimentalControlPlatform.Runtime/DeviceSessionId.cs`
- Create/Modify: `platform/src/ExperimentalControlPlatform.Runtime/IDeviceSession.cs`
- Create/Modify: `platform/src/ExperimentalControlPlatform.Runtime/IDeviceSessionRegistry.cs`
- Create/Modify: `platform/src/ExperimentalControlPlatform.Runtime/DeviceSessionRegistry.cs`
- Create/Modify: `platform/src/ExperimentalControlPlatform.Runtime/ISnapshotOutputPort.cs`
- Create/Modify: `platform/src/ExperimentalControlPlatform.Runtime/IStreamOutputPort.cs`
- Create/Modify: `platform/src/ExperimentalControlPlatform.Runtime/SnapshotOutputPort.cs`
- Create/Modify: `platform/src/ExperimentalControlPlatform.Runtime/StreamOutputPort.cs`
- Create/Modify: `platform/src/ExperimentalControlPlatform.Runtime/IntegratedMicrophoneSession.cs`
- Create/Modify: `platform/src/ExperimentalControlPlatform.Runtime/IntegratedMicrophoneSessionState.cs`
- Create/Modify: `platform/src/ExperimentalControlPlatform.Runtime/DeviceDiagnosticsSnapshot.cs`
- Create/Modify: `platform/src/ExperimentalControlPlatform.Runtime/DeviceSessionEndSnapshot.cs`
- Create/Modify: `platform/src/ExperimentalControlPlatform.Runtime/MicrophoneFrameJournal.cs`
- Later modify: `platform/src/ExperimentalControlPlatform.Runtime/RuntimeCoordinator.cs`

### Microphone integration files

- Modify: `platform/src/ExperimentalControlPlatform.App/DevicePanels/Microphone/IntegratedMicrophonePanelViewModel.cs`
- Verify: `platform/src/ExperimentalControlPlatform.App/DevicePanels/Microphone/IntegratedMicrophoneClient.cs`
- Modify later if needed: `platform/src/ExperimentalControlPlatform.App/App.xaml.cs`

### Controlled-device pilot files

- Inspect/Modify later: control-center serial adapter under `platform/src/ExperimentalControlPlatform.Devices/`
- Create later: `platform/src/ExperimentalControlPlatform.Runtime/ControlCenterSession.cs`
- Create later: control-center panel/session client files in `platform/src/ExperimentalControlPlatform.App/`

### PT-104 migration files

- Modify later: `platform/src/ExperimentalControlPlatform.App/DevicePanels/Pt104/Pt104PanelViewModel.cs`
- Create later: `platform/src/ExperimentalControlPlatform.Runtime/Pt104Session.cs`

### Camera migration files

- Modify later: `platform/src/ExperimentalControlPlatform.App/DevicePanels/HuaTeng/HuaTengPanelViewModel.cs`
- Modify later: `platform/src/ExperimentalControlPlatform.App/DevicePanels/Integrated/IntegratedCameraPanelViewModel.cs`
- Create later: `platform/src/ExperimentalControlPlatform.Runtime/CameraSession.cs` or family-specific equivalents

### Tests

- Modify: `platform/tests/ExperimentalControlPlatform.Runtime.Tests/RuntimeCoordinatorTests.cs`
- Create/Modify: `platform/tests/ExperimentalControlPlatform.Runtime.Tests/DeviceOutputPortTests.cs`
- Create/Modify: `platform/tests/ExperimentalControlPlatform.Runtime.Tests/DeviceSessionRegistryTests.cs`
- Create/Modify: `platform/tests/ExperimentalControlPlatform.Runtime.Tests/IntegratedMicrophoneSessionTests.cs`
- Create later: controlled-device runtime tests
- Create later: PT-104 runtime tests
- Create later: camera runtime tests

### Temp verification harnesses

- Keep local-only unless promoted:
  - `temp/runtime-io-microphone-smoke/runtime-io-microphone-smoke.csproj`
  - `temp/runtime-io-microphone-smoke/Program.cs`

## Verification Gates

### Core commands

- [ ] `dotnet build .\platform\ExperimentalControlPlatform.sln`
- [ ] `dotnet test .\platform\ExperimentalControlPlatform.sln --no-build`

### Runtime-IO-specific expectations

- microphone runtime tests pass
- runtime registry tests pass
- runtime output port tests pass
- session client panel still builds and behaves truthfully
- at least one non-panel stream consumer works

### Review requirements

External review is required for:

- microphone runtime slice landing
- controlled-device runtime pilot landing
- runtime coordinator / stop-all integration
- PT-104 runtime migration
- camera-family runtime migration

## Task 1: Land The Existing Microphone Runtime Slice

**Files:**
- Verify/Modify: files listed in Runtime foundation files
- Verify/Modify: `platform/src/ExperimentalControlPlatform.App/DevicePanels/Microphone/IntegratedMicrophonePanelViewModel.cs`
- Test: `platform/tests/ExperimentalControlPlatform.Runtime.Tests/*`
- Local verification only: `temp/runtime-io-microphone-smoke/*`

- [ ] **Step 1: Re-read the required foundation docs**

Confirm the slice still matches:

- runtime session ownership,
- snapshot vs stream ports,
- panel-as-client model,
- delivery threading rules.

- [ ] **Step 2: Review the current worktree diff**

Run: `git -C "C:\Users\Yi Zhuang\.config\superpowers\worktrees\Orchestral\runtime-io-microphone" status --short`
Expected: only the runtime-IO microphone slice files and optional `temp/` harness files are present.

- [ ] **Step 3: Review the runtime test suite**

Run: `dotnet test .\platform\tests\ExperimentalControlPlatform.Runtime.Tests\ExperimentalControlPlatform.Runtime.Tests.csproj`
Expected: PASS

- [ ] **Step 4: Review full build and test**

Run:
- `dotnet build .\platform\ExperimentalControlPlatform.sln`
- `dotnet test .\platform\ExperimentalControlPlatform.sln --no-build`

Expected: PASS

- [ ] **Step 5: Run the real microphone smoke harness**

Run: `dotnet run --project .\temp\runtime-io-microphone-smoke\runtime-io-microphone-smoke.csproj`
Expected:
- `CONNECT_OK True`
- `FRAME_AFTER_CONNECT True`
- `LIVE_OK ...`
- `STOP_OK True`
- `DISCONNECT_OK True`

- [ ] **Step 6: Prepare external review package**

Write the review prompt and review artifact path under:

- `docs/collaboration/reviews/YYYY-MM-DD-runtime-io-microphone-review.md`

- [ ] **Step 7: Resolve findings and retest**

Do not land until:

- accepted findings are fixed,
- response artifact exists,
- retest status is explicit.

- [ ] **Step 8: Create a clean landing branch if needed**

Use a clean branch if the worktree contains unrelated files or temp harnesses that should not be shipped.

- [ ] **Step 9: Commit the microphone runtime slice**

```bash
git add platform/src/ExperimentalControlPlatform.Runtime platform/src/ExperimentalControlPlatform.App/DevicePanels/Microphone platform/tests/ExperimentalControlPlatform.Runtime.Tests
git commit -m "feat: add microphone runtime session foundation"
```

- [ ] **Step 10: Push and merge the slice**

Push the clean branch or worktree branch, open review, land, and delete the temporary landing branch when complete.

## Task 2: Make The App Host Own The Runtime Registry Explicitly

**Files:**
- Modify: `platform/src/ExperimentalControlPlatform.App/App.xaml.cs`
- Modify if needed: `platform/src/ExperimentalControlPlatform.Runtime/RuntimeCoordinator.cs`
- Modify if needed: `platform/src/ExperimentalControlPlatform.Runtime/IRuntimeCoordinator.cs`

- [ ] **Step 1: Inspect how the app currently constructs panels**

Confirm how runtime coordinator, device services, and panel view models are currently created.

- [ ] **Step 2: Decide the host ownership boundary**

The app host should explicitly own:

- `IDeviceSessionRegistry`
- runtime coordinator
- long-lived device services if appropriate

- [ ] **Step 3: Write the failing test if host/session coordination becomes testable**

If coordinator-to-registry behavior is extracted into runtime-testable code, write tests first.

- [ ] **Step 4: Wire the app host to create and pass the registry**

Ensure panels are no longer creating their own runtime ownership islands.

- [ ] **Step 5: Add or update stop-all integration if introduced**

`RuntimeCoordinator` should be able to trigger stop behavior through a host-level bridge, not through UI assumptions.

- [ ] **Step 6: Run build and tests**

Run:
- `dotnet build .\platform\ExperimentalControlPlatform.sln`
- `dotnet test .\platform\ExperimentalControlPlatform.sln --no-build`

Expected: PASS

- [ ] **Step 7: Commit**

```bash
git add platform/src/ExperimentalControlPlatform.App/App.xaml.cs platform/src/ExperimentalControlPlatform.Runtime
git commit -m "feat: make app host own runtime session registry"
```

## Task 3: First Controlled-Device Runtime Pilot

**Files:**
- Inspect/Modify: control-center serial device adapter files in `platform/src/ExperimentalControlPlatform.Devices/`
- Create: `platform/src/ExperimentalControlPlatform.Runtime/ControlCenterSession.cs`
- Create/Modify: controlled-device tests in `platform/tests/ExperimentalControlPlatform.Runtime.Tests/`
- Create/Modify: corresponding panel/session-client files in `platform/src/ExperimentalControlPlatform.App/`

- [ ] **Step 1: Clarify the controlled-device pilot**

Use the real control-center serial device from:

- [DEVICE_INVENTORY.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/legacy-knowledge/DEVICE_INVENTORY.md)

This pilot should validate:

- command plane,
- acknowledgement/state feedback,
- diagnostics,
- session-end behavior.

- [ ] **Step 2: Inspect current adapter and panel code**

Identify:

- current command path,
- current feedback path,
- current shutdown path,
- files that own runtime truth today.

- [ ] **Step 3: Write the execution sub-plan if needed**

If the control-center pilot is large enough, write a dedicated sub-plan before coding.

- [ ] **Step 4: Write failing runtime tests**

Cover:

- connect/disconnect,
- command send,
- acknowledgement/state update,
- diagnostics snapshot,
- stop behavior.

- [ ] **Step 5: Implement the session**

Use the same session/registry/port model already proven by the microphone slice.

- [ ] **Step 6: Refactor the panel into a session client**

Do not let the panel keep hidden command or feedback ownership.

- [ ] **Step 7: Run build, tests, and device verification**

Expected: PASS plus truthful hardware or harness verification.

- [ ] **Step 8: External review**

Create review and response artifacts under `docs/collaboration/reviews/`.

- [ ] **Step 9: Commit**

```bash
git add platform/src/ExperimentalControlPlatform.Runtime platform/src/ExperimentalControlPlatform.App platform/tests/ExperimentalControlPlatform.Runtime.Tests
git commit -m "feat: add controlled-device runtime session pilot"
```

## Task 4: PT-104 Runtime Session Migration

**Files:**
- Modify: `platform/src/ExperimentalControlPlatform.App/DevicePanels/Pt104/Pt104PanelViewModel.cs`
- Create: `platform/src/ExperimentalControlPlatform.Runtime/Pt104Session.cs`
- Create/Modify: PT-104 runtime tests

- [ ] **Step 1: Re-read PT-104 architecture truths**

Confirm:

- shared device scope,
- channel endpoint scope,
- live/session scope,
- truthful apply/live semantics.

- [ ] **Step 2: Inspect current PT-104 ownership**

Identify what is still panel-owned and what should move into the session.

- [ ] **Step 3: Write failing tests**

Cover:

- one physical session with multiple endpoint channels,
- channel switching,
- connected/live/apply behavior,
- diagnostics snapshots,
- session-end output.

- [ ] **Step 4: Implement `Pt104Session`**

Keep:

- one session per PT-104 device identity,
- channels as endpoints inside the session.

- [ ] **Step 5: Refactor the PT-104 panel into a session client**

Keep the panel as operator UI only.

- [ ] **Step 6: Run build, tests, and PT-104 verification**

Expected: PASS plus truthful hardware verification when hardware is available.

- [ ] **Step 7: External review**

Required because PT-104 is a shared-hardware, multi-endpoint runtime migration.

- [ ] **Step 8: Commit**

```bash
git add platform/src/ExperimentalControlPlatform.Runtime platform/src/ExperimentalControlPlatform.App/DevicePanels/Pt104 platform/tests/ExperimentalControlPlatform.Runtime.Tests
git commit -m "feat: migrate PT-104 to runtime session model"
```

## Task 5: Camera Family Runtime Migration

**Files:**
- Modify: `platform/src/ExperimentalControlPlatform.App/DevicePanels/HuaTeng/HuaTengPanelViewModel.cs`
- Modify: `platform/src/ExperimentalControlPlatform.App/DevicePanels/Integrated/IntegratedCameraPanelViewModel.cs`
- Create: camera runtime session files under `platform/src/ExperimentalControlPlatform.Runtime/`
- Create/Modify: camera runtime tests

- [ ] **Step 1: Inspect shared camera ownership**

Identify what is common across camera-family runtime behavior and what stays device-specific.

- [ ] **Step 2: Write failing tests**

Cover:

- connect,
- snap,
- live start/stop,
- apply while idle,
- restart semantics,
- diagnostics,
- session-end output.

- [ ] **Step 3: Implement runtime camera session(s)**

Keep the abstraction only as generic as the shared behavior actually supports.

- [ ] **Step 4: Refactor both camera panels into session clients**

Do not keep duplicate runtime behavior in two view models.

- [ ] **Step 5: Run build, tests, and real camera verification**

Expected: PASS plus truthful hardware verification where devices are available.

- [ ] **Step 6: External review**

Required because this is shared-family runtime migration.

- [ ] **Step 7: Commit**

```bash
git add platform/src/ExperimentalControlPlatform.Runtime platform/src/ExperimentalControlPlatform.App/DevicePanels/HuaTeng platform/src/ExperimentalControlPlatform.App/DevicePanels/Integrated platform/tests/ExperimentalControlPlatform.Runtime.Tests
git commit -m "feat: migrate camera family to runtime session model"
```

## Task 6: Close The Runtime Foundation Slice

**Files:**
- Modify: `docs/development/V1_EXECUTION_TRACK.md`
- Modify: `docs/development/ROADMAP_V1.md` only if a strategic note is needed
- Modify: runtime docs only if implementation revealed real doc mismatch

- [ ] **Step 1: Update execution status**

Mark which runtime slices are landed and which remain.

- [ ] **Step 2: Review architecture/doc drift**

If code changed semantics, update docs.

- [ ] **Step 3: Decide whether a simulator/replay harness is now the next runtime hardening task**

If yes, write the next execution plan.

- [ ] **Step 4: Commit the execution/doc updates if needed**

```bash
git add docs/development docs/architecture docs/collaboration/reviews
git commit -m "docs: update runtime io execution status"
```

## Definition Of Done

The runtime-IO program reaches its first meaningful completion when:

- the microphone runtime slice is landed,
- the app host explicitly owns the session registry,
- one controlled-device runtime pilot is landed,
- PT-104 is migrated to the session model,
- one camera-family runtime migration is landed,
- panels are session clients rather than runtime owners,
- review artifacts exist for the shared-foundation slices,
- and [V1_EXECUTION_TRACK.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/development/V1_EXECUTION_TRACK.md) reflects the shipped state.
