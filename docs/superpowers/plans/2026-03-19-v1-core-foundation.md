# Orchestral V1 Core Foundation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the first architecture-bearing implementation slice for Orchestral: the system language spec, core artifact models, runtime stop skeleton, and a minimally real app shell.

**Architecture:** Start with the smallest set of concrete artifacts that can carry the product architecture: typed experiment/device/protocol/capability/stop-condition models in `Core`, a stop-aware runtime skeleton in `Runtime`, and a simple WPF shell in `App`. Keep the scope tight and explicitly postpone real device integrations and full experiment migration.

**Tech Stack:** C#/.NET 8, WPF, xUnit, Markdown specs

---

### Task 1: Install the .NET SDK and verify the scaffold can build

**Files:**
- Verify: `platform/ExperimentalControlPlatform.sln`
- Verify: `platform/src/ExperimentalControlPlatform.App/ExperimentalControlPlatform.App.csproj`
- Verify: `platform/tests/ExperimentalControlPlatform.Core.Tests/ExperimentalControlPlatform.Core.Tests.csproj`
- Verify: `platform/tests/ExperimentalControlPlatform.Runtime.Tests/ExperimentalControlPlatform.Runtime.Tests.csproj`

- [ ] **Step 1: Install .NET 8 SDK**

Install the official .NET 8 SDK for Windows from Microsoft.

Expected: `dotnet --version` prints a valid SDK version.

- [ ] **Step 2: Run the current solution restore**

Run:

```powershell
dotnet restore "C:\Users\Yi Zhuang\OneDrive\Codes\Projects\Orchestral\platform\ExperimentalControlPlatform.sln"
```

Expected: restore succeeds.

- [ ] **Step 3: Run the placeholder tests**

Run:

```powershell
dotnet test "C:\Users\Yi Zhuang\OneDrive\Codes\Projects\Orchestral\platform\ExperimentalControlPlatform.sln" -v minimal
```

Expected: the placeholder tests pass.

- [ ] **Step 4: Commit**

```bash
git add platform
git commit -m "chore: verify dotnet scaffold"
```

### Task 2: Write the system language spec

**Files:**
- Create: `docs/architecture/SYSTEM_LANGUAGE_SPEC.md`
- Check: `docs/northstar/HIGH_LEVEL_PROJECT_TECHNICAL_INTRODUCTION.md`
- Check: `docs/architecture/V1_ARCHITECTURE_BLUEPRINT.md`
- Check: `docs/development/TECH_STACK_DECISIONS.md`

- [ ] **Step 1: Write the spec outline**

The spec should define the core nouns and semantics of the platform language:

- experiment
- device role
- device
- protocol
- capability
- parameter
- stream
- transform
- monitor
- stop condition
- output
- run manifest

- [ ] **Step 2: Add one end-to-end turbulence example**

Include one small example showing how the language would express:

- two camera roles
- one actuator role
- one temperature-sensing role
- one system-level stop condition

- [ ] **Step 3: Review the spec for over-design**

Check that the document does not introduce:

- visual editor syntax
- distributed orchestration concepts
- fully generic plugin packaging rules

- [ ] **Step 4: Commit**

```bash
git add docs/architecture/SYSTEM_LANGUAGE_SPEC.md
git commit -m "docs: add system language spec"
```

### Task 3: Replace core placeholders with typed artifact models

**Files:**
- Create: `platform/src/ExperimentalControlPlatform.Core/Artifacts/ExperimentDefinition.cs`
- Create: `platform/src/ExperimentalControlPlatform.Core/Artifacts/DeviceRoleDefinition.cs`
- Create: `platform/src/ExperimentalControlPlatform.Core/Artifacts/DeviceDefinition.cs`
- Create: `platform/src/ExperimentalControlPlatform.Core/Artifacts/ProtocolDefinition.cs`
- Create: `platform/src/ExperimentalControlPlatform.Core/Artifacts/CapabilityDefinition.cs`
- Create: `platform/src/ExperimentalControlPlatform.Core/Artifacts/StopConditionDefinition.cs`
- Create: `platform/src/ExperimentalControlPlatform.Core/Artifacts/MonitorDefinition.cs`
- Create: `platform/src/ExperimentalControlPlatform.Core/Artifacts/ParameterDefinition.cs`
- Create: `platform/src/ExperimentalControlPlatform.Core/Artifacts/OutputDefinition.cs`
- Create: `platform/src/ExperimentalControlPlatform.Core/Artifacts/RunManifestDefinition.cs`
- Create: `platform/src/ExperimentalControlPlatform.Core/Artifacts/ArtifactId.cs`
- Modify: `platform/src/ExperimentalControlPlatform.Core/Placeholder.cs`
- Test: `platform/tests/ExperimentalControlPlatform.Core.Tests/Artifacts/ExperimentDefinitionTests.cs`
- Test: `platform/tests/ExperimentalControlPlatform.Core.Tests/Artifacts/StopConditionDefinitionTests.cs`

- [ ] **Step 1: Write failing tests for core artifact construction**

Example test cases:

```csharp
[Fact]
public void ExperimentDefinition_Requires_Name_And_Purpose()
{
    var experiment = new ExperimentDefinition(
        Id: new ArtifactId("exp.turbulence"),
        Name: "Turbulence Transition",
        Purpose: "Track and control puff behavior",
        Roles: [],
        Parameters: [],
        Monitors: [],
        StopConditions: [],
        Outputs: []);

    Assert.Equal("Turbulence Transition", experiment.Name);
}
```

- [ ] **Step 2: Run the failing tests**

Run:

```powershell
dotnet test "C:\Users\Yi Zhuang\OneDrive\Codes\Projects\Orchestral\platform\tests\ExperimentalControlPlatform.Core.Tests\ExperimentalControlPlatform.Core.Tests.csproj" -v minimal
```

Expected: FAIL because the types do not exist yet.

- [ ] **Step 3: Implement the minimal typed models**

Use explicit immutable records or record classes. Keep validation simple and local. Do not add serialization or persistence concerns yet.

- [ ] **Step 4: Run the tests again**

Run:

```powershell
dotnet test "C:\Users\Yi Zhuang\OneDrive\Codes\Projects\Orchestral\platform\tests\ExperimentalControlPlatform.Core.Tests\ExperimentalControlPlatform.Core.Tests.csproj" -v minimal
```

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add platform/src/ExperimentalControlPlatform.Core platform/tests/ExperimentalControlPlatform.Core.Tests
git commit -m "feat: add core experiment artifact models"
```

### Task 4: Add protocol and capability primitives with clear boundaries

**Files:**
- Create: `platform/src/ExperimentalControlPlatform.Protocols/ProtocolKind.cs`
- Create: `platform/src/ExperimentalControlPlatform.Protocols/TransportKind.cs`
- Create: `platform/src/ExperimentalControlPlatform.Protocols/ProtocolSessionBehavior.cs`
- Create: `platform/src/ExperimentalControlPlatform.Devices/Capabilities/CapabilityKind.cs`
- Create: `platform/src/ExperimentalControlPlatform.Devices/Capabilities/DeviceCapabilityContract.cs`
- Test: `platform/tests/ExperimentalControlPlatform.Core.Tests/Artifacts/DeviceDefinitionTests.cs`

- [ ] **Step 1: Write a failing test for protocol and capability attachment**

The test should verify that a device definition can declare:

- a protocol kind
- one or more capabilities
- one health/status expectation field

- [ ] **Step 2: Run the failing test**

Run:

```powershell
dotnet test "C:\Users\Yi Zhuang\OneDrive\Codes\Projects\Orchestral\platform\tests\ExperimentalControlPlatform.Core.Tests\ExperimentalControlPlatform.Core.Tests.csproj" -v minimal
```

Expected: FAIL due to missing protocol/capability primitives.

- [ ] **Step 3: Implement the minimal primitives**

Keep them enum- and record-based. Do not implement real device drivers yet.

- [ ] **Step 4: Run the tests again**

Run:

```powershell
dotnet test "C:\Users\Yi Zhuang\OneDrive\Codes\Projects\Orchestral\platform\tests\ExperimentalControlPlatform.Core.Tests\ExperimentalControlPlatform.Core.Tests.csproj" -v minimal
```

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add platform/src/ExperimentalControlPlatform.Protocols platform/src/ExperimentalControlPlatform.Devices platform/tests/ExperimentalControlPlatform.Core.Tests
git commit -m "feat: add protocol and capability primitives"
```

### Task 5: Build the runtime stop and lifecycle skeleton

**Files:**
- Create: `platform/src/ExperimentalControlPlatform.Runtime/RunState.cs`
- Create: `platform/src/ExperimentalControlPlatform.Runtime/StopReason.cs`
- Create: `platform/src/ExperimentalControlPlatform.Runtime/RuntimeRunContext.cs`
- Create: `platform/src/ExperimentalControlPlatform.Runtime/IRuntimeCoordinator.cs`
- Create: `platform/src/ExperimentalControlPlatform.Runtime/RuntimeCoordinator.cs`
- Create: `platform/src/ExperimentalControlPlatform.Runtime/StopConditions/IStopConditionEvaluator.cs`
- Create: `platform/src/ExperimentalControlPlatform.Runtime/StopConditions/StopEvaluationResult.cs`
- Modify: `platform/src/ExperimentalControlPlatform.Runtime/Placeholder.cs`
- Test: `platform/tests/ExperimentalControlPlatform.Runtime.Tests/RuntimeCoordinatorTests.cs`

- [ ] **Step 1: Write the failing runtime tests**

Cover at least:

- initial state is `Idle`
- start transitions to `Starting` or `Running`
- stop request produces a named `StopReason`
- runtime rejects duplicate start while already active

- [ ] **Step 2: Run the failing runtime tests**

Run:

```powershell
dotnet test "C:\Users\Yi Zhuang\OneDrive\Codes\Projects\Orchestral\platform\tests\ExperimentalControlPlatform.Runtime.Tests\ExperimentalControlPlatform.Runtime.Tests.csproj" -v minimal
```

Expected: FAIL because the runtime types do not exist yet.

- [ ] **Step 3: Implement the minimal stop-aware runtime skeleton**

Do not connect real devices yet. This step should only establish:

- named runtime states
- explicit stop reasons
- one coordinator abstraction
- one simple in-memory implementation

- [ ] **Step 4: Run the runtime tests again**

Run:

```powershell
dotnet test "C:\Users\Yi Zhuang\OneDrive\Codes\Projects\Orchestral\platform\tests\ExperimentalControlPlatform.Runtime.Tests\ExperimentalControlPlatform.Runtime.Tests.csproj" -v minimal
```

Expected: PASS.

- [ ] **Step 5: Run the full solution tests**

Run:

```powershell
dotnet test "C:\Users\Yi Zhuang\OneDrive\Codes\Projects\Orchestral\platform\ExperimentalControlPlatform.sln" -v minimal
```

Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add platform/src/ExperimentalControlPlatform.Runtime platform/tests/ExperimentalControlPlatform.Runtime.Tests
git commit -m "feat: add runtime lifecycle and stop skeleton"
```

### Task 6: Make the app shell reflect the real runtime boundary

**Files:**
- Modify: `platform/src/ExperimentalControlPlatform.App/MainWindow.xaml`
- Modify: `platform/src/ExperimentalControlPlatform.App/MainWindow.xaml.cs`
- Create: `platform/src/ExperimentalControlPlatform.App/MainViewModel.cs`
- Create: `platform/src/ExperimentalControlPlatform.App/RuntimeStatusViewModel.cs`

- [ ] **Step 1: Write a minimal app shell design**

The main window should expose:

- product name
- current runtime state
- placeholder start button
- placeholder stop button
- placeholder area for future monitor panels

- [ ] **Step 2: Implement the minimal view-model-backed shell**

Bind the UI to runtime state rather than hardcoded text. Keep it intentionally small.

- [ ] **Step 3: Build the app project**

Run:

```powershell
dotnet build "C:\Users\Yi Zhuang\OneDrive\Codes\Projects\Orchestral\platform\src\ExperimentalControlPlatform.App\ExperimentalControlPlatform.App.csproj" -v minimal
```

Expected: BUILD SUCCESSFUL.

- [ ] **Step 4: Commit**

```bash
git add platform/src/ExperimentalControlPlatform.App
git commit -m "feat: add runtime-aware app shell"
```

### Task 7: Document the implemented core slice

**Files:**
- Modify: `platform/README.md`
- Modify: `README.md`
- Modify: `docs/development/ROADMAP_V1.md`

- [ ] **Step 1: Update platform README**

Describe what now exists concretely:

- core artifact models
- protocol/capability primitives
- runtime stop skeleton
- minimal app shell

- [ ] **Step 2: Update root README**

Add a short current-status section describing the first implemented core slice.

- [ ] **Step 3: Update roadmap status**

Mark the relevant early milestone as started or partially completed.

- [ ] **Step 4: Commit**

```bash
git add platform/README.md README.md docs/development/ROADMAP_V1.md
git commit -m "docs: update status after core foundation slice"
```

### Task 8: Final verification

**Files:**
- Verify: `platform/ExperimentalControlPlatform.sln`
- Verify: `docs/architecture/SYSTEM_LANGUAGE_SPEC.md`
- Verify: test projects

- [ ] **Step 1: Run the full solution tests**

Run:

```powershell
dotnet test "C:\Users\Yi Zhuang\OneDrive\Codes\Projects\Orchestral\platform\ExperimentalControlPlatform.sln" -v minimal
```

Expected: PASS.

- [ ] **Step 2: Build the app project**

Run:

```powershell
dotnet build "C:\Users\Yi Zhuang\OneDrive\Codes\Projects\Orchestral\platform\src\ExperimentalControlPlatform.App\ExperimentalControlPlatform.App.csproj" -v minimal
```

Expected: BUILD SUCCESSFUL.

- [ ] **Step 3: Manually review the changed file set**

Check that the implementation stayed within the intended first slice and did not drift into:

- real device-specific code
- real protocol sessions
- YAML persistence
- turbulence experiment migration

- [ ] **Step 4: Commit**

```bash
git add .
git commit -m "chore: complete v1 core foundation slice"
```
