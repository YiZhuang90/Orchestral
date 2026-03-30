# Run Context And Metadata Response

## RCM-001

- Decision: `Accepted`
- Fix summary: added collection-boundary validation tests for `RunContextDefinition` so default `ArtifactId` values inside metadata keys, applied-settings references, decision-log ids, and artifact-reference ids are explicitly guarded.
- Changed files:
  - `platform/tests/ExperimentalControlPlatform.Core.Tests/Artifacts/ArtifactBoundaryValidationTests.cs`
- Retest status:
  - `dotnet test .\\platform\\tests\\ExperimentalControlPlatform.Core.Tests\\ExperimentalControlPlatform.Core.Tests.csproj --no-restore --filter "ArtifactBoundaryValidationTests|RunContextDefinitionTests" -nodeReuse:false`
  - passed

## RCM-002

- Decision: `Accepted`
- Fix summary: added explicit XML documentation on `RuntimeRunContext` clarifying the distinction between the ephemeral runtime `RunId` and the stable `RunContext.Id` artifact identity.
- Changed files:
  - `platform/src/ExperimentalControlPlatform.Runtime/RuntimeRunContext.cs`
- Retest status:
  - `dotnet test .\\platform\\tests\\ExperimentalControlPlatform.Runtime.Tests\\ExperimentalControlPlatform.Runtime.Tests.csproj --no-restore --filter Start_WithRunContext_Captures_Run_Metadata_And_Preserves_It_Through_Stop -nodeReuse:false`
  - passed

## RCM-003

- Decision: `Rejected`
- Technical reason: the current constructor shape is intentionally transitional. `RuntimeRunContext` must still support three real call paths in this branch:
  - bare runtime start,
  - experiment-only start,
  - run-context start.
- Why no fix was applied:
  - the existing mismatch guard keeps those paths safe without forcing a broader API churn across the runtime/app surface,
  - and a dedicated factory would add another seam without materially improving behavior in this slice.
- Follow-up note:
  - revisit constructor simplification only when bare and experiment-only starts are consolidated behind richer run-start inputs.

## RCM-004

- Decision: `Rejected`
- Technical reason: whitespace-only optional text collapsing to `null` is intentional for this first run-context unit.
- Why no fix was applied:
  - it matches the current operator-facing semantics for “empty note” rather than preserving meaningless placeholder whitespace,
  - and the behavior is now covered explicitly by `RunContextDefinitionTests`.
