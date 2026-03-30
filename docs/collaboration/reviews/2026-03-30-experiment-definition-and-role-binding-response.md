# Experiment Definition And Role Binding Response

## Finding Responses

### P1-01

- Status: `Accepted`
- Fix summary:
  - `ResolvedExperimentDefinition.Validate(...)` now rejects missing experiment parameters when no default value is declared
  - the resolved experiment package therefore enforces the “required parameters are present and valid” rule from the system language spec
- Changed files:
  - `platform/src/ExperimentalControlPlatform.Core/Artifacts/ResolvedExperimentDefinition.cs`
  - `platform/tests/ExperimentalControlPlatform.Core.Tests/Artifacts/ResolvedExperimentDefinitionTests.cs`
- Retest status:
  - covered by `Validate_Returns_Issue_For_Missing_Required_Experiment_Parameter`

### P2-01

- Status: `Accepted`
- Fix summary:
  - `RoleBindingDefinition` now carries binding-level parameter values
  - `ResolvedExperimentDefinition.Validate(...)` now validates those values against the bound device parameter schema
  - `RunManifestDefinition` now has a dedicated `RoleBindingParameterValues` surface so those values can be preserved in the authoritative run record instead of being lost or flattened later
- Changed files:
  - `platform/src/ExperimentalControlPlatform.Core/Artifacts/RoleBindingDefinition.cs`
  - `platform/src/ExperimentalControlPlatform.Core/Artifacts/ResolvedExperimentDefinition.cs`
  - `platform/src/ExperimentalControlPlatform.Core/Artifacts/RunManifestDefinition.cs`
  - `platform/tests/ExperimentalControlPlatform.Core.Tests/Artifacts/RoleBindingDefinitionTests.cs`
  - `platform/tests/ExperimentalControlPlatform.Core.Tests/Artifacts/ResolvedExperimentDefinitionTests.cs`
  - `platform/tests/ExperimentalControlPlatform.Core.Tests/Artifacts/RunManifestDefinitionTests.cs`
  - `platform/tests/ExperimentalControlPlatform.Core.Tests/Artifacts/ArtifactBoundaryValidationTests.cs`
- Retest status:
  - covered by the core artifact test suite

### P2-02

- Status: `Rejected`
- Technical reason:
  - the current branch still needs a bare runtime start path because the app shell and session-host path are not yet experiment-package-driven end to end
  - removing or renaming `Start()` now would create UI churn without yet delivering the missing experiment-selection surface
- Why the finding is not adopted now:
  - the existence of `Start(ResolvedExperimentDefinition experiment)` is the new preferred path
  - the no-argument `Start()` remains a transitional compatibility seam during migration, not the long-term canonical path
- Follow-up:
  - revisit once the app shell starts from a resolved experiment package by default

### P3-01

- Status: `Rejected`
- Technical reason:
  - the current runtime contracts do not yet expose a reliable “session is present and ready for this bound device” truth surface
  - `IDeviceSessionRegistry` can enumerate sessions, but the coordinator still lacks the binding-to-session readiness contract needed to make this check truthful
- Why the finding is deferred:
  - this belongs in the next coordinator-adjacent slice, where experiment bindings meet active sessions and readiness validation becomes explicit
  - adding a fake readiness check now would overclaim correctness

### P3-02

- Status: `Accepted`
- Fix summary:
  - added explicit negative coverage for missing required experiment parameters
  - added explicit negative coverage for missing required device-binding parameters
- Changed files:
  - `platform/tests/ExperimentalControlPlatform.Core.Tests/Artifacts/ResolvedExperimentDefinitionTests.cs`
- Retest status:
  - covered by the core artifact test suite

## Review Channel Note

- preferred Claude review was unavailable because local Claude CLI quota was exhausted
- this review round was executed through Gemini CLI
- the `gemini-3-flash-preview` artifact completed with retry noise due model-capacity pressure but still produced actionable findings

## Verification

- `dotnet test .\\platform\\tests\\ExperimentalControlPlatform.Core.Tests\\ExperimentalControlPlatform.Core.Tests.csproj -nodeReuse:false`
- `dotnet test .\\platform\\tests\\ExperimentalControlPlatform.Runtime.Tests\\ExperimentalControlPlatform.Runtime.Tests.csproj -nodeReuse:false`
- full solution build/test rerun pending as the final clean verification pass after review-response closeout
