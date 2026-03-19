# Project Structure And Scaffold Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Reorganize the repository into a platform-first development workspace, isolate the old pipe-flow code under a legacy area, and scaffold the first C# platform project structure.

**Architecture:** The repository becomes a clean split between documentation, legacy reference material, and the new platform workspace. Existing Python experiment files are preserved as legacy knowledge. The new platform gets a starter .NET-oriented source tree with clear module boundaries, without mixing new architecture with old code.

**Tech Stack:** Markdown, filesystem reorganization, C#/.NET project scaffolding, Python legacy reference preservation

---

### Task 1: Create the new top-level folder structure

**Files:**
- Create: `docs/northstar/`
- Create: `docs/architecture/`
- Create: `docs/development/`
- Create: `docs/legacy-knowledge/`
- Create: `docs/superpowers/plans/`
- Create: `legacy/pipe-flow-reference/notebooks/`
- Create: `legacy/pipe-flow-reference/python/`
- Create: `legacy/pipe-flow-reference/vendor/`
- Create: `platform/src/`
- Create: `platform/tests/`
- Create: `schemas/`
- Create: `experiments/turbulence-transition/`
- Create: `tools/`

- [ ] **Step 1: Create the directory tree**

Run:

```powershell
New-Item -ItemType Directory -Force -Path `
  "docs/northstar", `
  "docs/architecture", `
  "docs/development", `
  "docs/legacy-knowledge", `
  "docs/superpowers/plans", `
  "legacy/pipe-flow-reference/notebooks", `
  "legacy/pipe-flow-reference/python", `
  "legacy/pipe-flow-reference/vendor", `
  "platform/src", `
  "platform/tests", `
  "schemas", `
  "experiments/turbulence-transition", `
  "tools"
```

Expected: all target directories exist.

- [ ] **Step 2: Verify the new directory tree**

Run:

```powershell
Get-ChildItem -Depth 2
```

Expected: the new directory structure is visible at the repository root.

### Task 2: Move existing documentation into the new docs layout

**Files:**
- Move: `HIGH_LEVEL_PROJECT_TECHNICAL_INTRODUCTION.md` -> `docs/northstar/HIGH_LEVEL_PROJECT_TECHNICAL_INTRODUCTION.md`
- Move: `V1_ARCHITECTURE_BLUEPRINT.md` -> `docs/architecture/V1_ARCHITECTURE_BLUEPRINT.md`
- Move: `DEVELOPMENT_STRATEGY.md` -> `docs/development/DEVELOPMENT_STRATEGY.md`
- Move: `TECH_STACK_DECISIONS.md` -> `docs/development/TECH_STACK_DECISIONS.md`
- Move: `ROADMAP_V1.md` -> `docs/development/ROADMAP_V1.md`
- Move: `AI_INTEGRATION_PLAN.md` -> `docs/development/AI_INTEGRATION_PLAN.md`
- Move: `LEGACY_KNOWLEDGE_EXTRACTION_PLAN.md` -> `docs/legacy-knowledge/LEGACY_KNOWLEDGE_EXTRACTION_PLAN.md`

- [ ] **Step 1: Move the markdown documents**

Run:

```powershell
Move-Item "HIGH_LEVEL_PROJECT_TECHNICAL_INTRODUCTION.md" "docs/northstar/"
Move-Item "V1_ARCHITECTURE_BLUEPRINT.md" "docs/architecture/"
Move-Item "DEVELOPMENT_STRATEGY.md" "docs/development/"
Move-Item "TECH_STACK_DECISIONS.md" "docs/development/"
Move-Item "ROADMAP_V1.md" "docs/development/"
Move-Item "AI_INTEGRATION_PLAN.md" "docs/development/"
Move-Item "LEGACY_KNOWLEDGE_EXTRACTION_PLAN.md" "docs/legacy-knowledge/"
```

Expected: root-level markdown docs are relocated into the new docs tree.

- [ ] **Step 2: Update internal cross-links after moves**

Modify:
- `docs/architecture/V1_ARCHITECTURE_BLUEPRINT.md`
- `docs/development/DEVELOPMENT_STRATEGY.md`

Expected: links resolve to the new absolute locations.

### Task 3: Isolate the old pipe-flow code under legacy

**Files:**
- Move: `Looping_exp_main.ipynb` -> `legacy/pipe-flow-reference/notebooks/Looping_exp_main.ipynb`
- Move: `exp_control_functions.py` -> `legacy/pipe-flow-reference/python/exp_control_functions.py`
- Move: `PipeFlowComputer.py` -> `legacy/pipe-flow-reference/python/PipeFlowComputer.py`
- Move: `pufffuncs.py` -> `legacy/pipe-flow-reference/python/pufffuncs.py`
- Move: `Statistics.py` -> `legacy/pipe-flow-reference/python/Statistics.py`
- Move: `mvsdk.py` -> `legacy/pipe-flow-reference/vendor/mvsdk.py`

- [ ] **Step 1: Move the old experiment assets**

Run:

```powershell
Move-Item "Looping_exp_main.ipynb" "legacy/pipe-flow-reference/notebooks/"
Move-Item "exp_control_functions.py" "legacy/pipe-flow-reference/python/"
Move-Item "PipeFlowComputer.py" "legacy/pipe-flow-reference/python/"
Move-Item "pufffuncs.py" "legacy/pipe-flow-reference/python/"
Move-Item "Statistics.py" "legacy/pipe-flow-reference/python/"
Move-Item "mvsdk.py" "legacy/pipe-flow-reference/vendor/"
```

Expected: the old system is fully removed from the repo root and preserved under `legacy/pipe-flow-reference/`.

- [ ] **Step 2: Verify that legacy files are present in their new locations**

Run:

```powershell
Get-ChildItem "legacy/pipe-flow-reference" -Recurse
```

Expected: notebook, Python modules, and vendor wrapper are present.

### Task 4: Create the platform workspace and starter C# project files

**Files:**
- Create: `platform/ExperimentalControlPlatform.sln`
- Create: `platform/Directory.Build.props`
- Create: `platform/README.md`
- Create: `platform/src/ExperimentalControlPlatform.Core/ExperimentalControlPlatform.Core.csproj`
- Create: `platform/src/ExperimentalControlPlatform.Core/Placeholder.cs`
- Create: `platform/src/ExperimentalControlPlatform.Runtime/ExperimentalControlPlatform.Runtime.csproj`
- Create: `platform/src/ExperimentalControlPlatform.Runtime/Placeholder.cs`
- Create: `platform/src/ExperimentalControlPlatform.Protocols/ExperimentalControlPlatform.Protocols.csproj`
- Create: `platform/src/ExperimentalControlPlatform.Protocols/Placeholder.cs`
- Create: `platform/src/ExperimentalControlPlatform.Devices/ExperimentalControlPlatform.Devices.csproj`
- Create: `platform/src/ExperimentalControlPlatform.Devices/Placeholder.cs`
- Create: `platform/src/ExperimentalControlPlatform.AI/ExperimentalControlPlatform.AI.csproj`
- Create: `platform/src/ExperimentalControlPlatform.AI/Placeholder.cs`
- Create: `platform/src/ExperimentalControlPlatform.App/ExperimentalControlPlatform.App.csproj`
- Create: `platform/src/ExperimentalControlPlatform.App/Placeholder.cs`
- Create: `platform/tests/ExperimentalControlPlatform.Core.Tests/ExperimentalControlPlatform.Core.Tests.csproj`
- Create: `platform/tests/ExperimentalControlPlatform.Core.Tests/PlaceholderTests.cs`
- Create: `platform/tests/ExperimentalControlPlatform.Runtime.Tests/ExperimentalControlPlatform.Runtime.Tests.csproj`
- Create: `platform/tests/ExperimentalControlPlatform.Runtime.Tests/PlaceholderTests.cs`

- [ ] **Step 1: Create the platform project folders**

Run:

```powershell
New-Item -ItemType Directory -Force -Path `
  "platform/src/ExperimentalControlPlatform.Core", `
  "platform/src/ExperimentalControlPlatform.Runtime", `
  "platform/src/ExperimentalControlPlatform.Protocols", `
  "platform/src/ExperimentalControlPlatform.Devices", `
  "platform/src/ExperimentalControlPlatform.AI", `
  "platform/src/ExperimentalControlPlatform.App", `
  "platform/tests/ExperimentalControlPlatform.Core.Tests", `
  "platform/tests/ExperimentalControlPlatform.Runtime.Tests"
```

Expected: source and test project folders exist.

- [ ] **Step 2: Add manual starter solution and project files**

Because `.NET` SDK is not installed locally, create the `.sln` and `.csproj` files manually instead of using `dotnet new`.

Expected: the workspace has a valid starter project layout even before SDK installation.

- [ ] **Step 3: Add placeholder source and test files**

Expected: each project contains at least one minimal file showing intended ownership.

### Task 5: Add repo-level guidance files for the new structure

**Files:**
- Create: `README.md`
- Create: `legacy/pipe-flow-reference/README.md`
- Create: `experiments/turbulence-transition/README.md`
- Create: `schemas/README.md`
- Create: `tools/README.md`

- [ ] **Step 1: Write the root README**

Expected: explains the repo split between docs, legacy, and platform.

- [ ] **Step 2: Write the legacy README**

Expected: explains that legacy code is preserved as reference knowledge, not as the new architecture base.

- [ ] **Step 3: Write the experiment, schemas, and tools READMEs**

Expected: each directory has a short statement of purpose.

### Task 6: Verify the reorganization

**Files:**
- Verify: repo tree after move
- Verify: expected files exist
- Verify: no old code files remain at the root except intentional files

- [ ] **Step 1: List the new top-level tree**

Run:

```powershell
Get-ChildItem -Force
```

Expected: root now contains the new structure instead of mixed docs and old code.

- [ ] **Step 2: Check for moved legacy files**

Run:

```powershell
Get-ChildItem "legacy/pipe-flow-reference" -Recurse | Select-Object FullName
```

Expected: all old files exist in legacy locations.

- [ ] **Step 3: Check for platform scaffold files**

Run:

```powershell
Get-ChildItem "platform" -Recurse | Select-Object FullName
```

Expected: starter solution, projects, and placeholder source files are present.

- [ ] **Step 4: Commit**

If and only if this repository is later initialized as git:

```bash
git add .
git commit -m "chore: reorganize repo and scaffold platform workspace"
```

