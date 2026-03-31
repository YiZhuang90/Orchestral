## Implementer Response

### Review status

- No blocking findings were reported.
- Minor coverage observations were addressed before landing.

### Observation 1

- Finding: transform reference test only asserted the missing input stream path.
- Decision: Accepted.
- Fix summary:
  - added explicit coverage for `unknown_transform_output_stream`
- Changed files:
  - `platform/tests/ExperimentalControlPlatform.Core.Tests/Artifacts/ExperimentDefinitionLintingTests.cs`
- Retest status:
  - covered by targeted Core test rerun and full solution test run

### Observation 2

- Finding: duplicate detection only had explicit coverage for streams.
- Decision: Accepted.
- Fix summary:
  - added duplicate role-id coverage to confirm a second collection is wired through the generic duplicate-definition lint path
- Changed files:
  - `platform/tests/ExperimentalControlPlatform.Core.Tests/Artifacts/ExperimentDefinitionLintingTests.cs`
- Retest status:
  - covered by targeted Core test rerun and full solution test run

### Observation 3

- Finding: no test exercised the `ScheduleParameterId` lint path.
- Decision: Accepted.
- Fix summary:
  - added a scheduled control-target case that asserts `unknown_control_target_schedule_parameter`
- Changed files:
  - `platform/tests/ExperimentalControlPlatform.Core.Tests/Artifacts/ExperimentDefinitionLintingTests.cs`
- Retest status:
  - covered by targeted Core test rerun and full solution test run

### Residual-risk follow-up

- Observation: ad hoc factory output was not directly proven to pass lint.
- Decision: Accepted.
- Fix summary:
  - added a round-trip test that synthesizes an ad hoc experiment package from a control-center panel and asserts `Lint().IsValid`
- Changed files:
  - `platform/tests/ExperimentalControlPlatform.App.Tests/AdHocRunDefinitionFactoryTests.cs`
- Retest status:
  - covered by targeted App test rerun and full solution test run

### Not adopted

- The helper duplication note in `ExperimentDefinition.cs` and `ExperimentDefinitionLintIssue.cs` was left unchanged.
- Reason:
  - the duplication is small, local, and not yet repeated broadly enough to justify a new shared helper seam in this slice.

### Verification

- `dotnet test .\\platform\\ExperimentalControlPlatform.sln --no-restore --filter "FullyQualifiedName~ExperimentDefinitionLintingTests|FullyQualifiedName~AdHocRunDefinitionFactoryTests|FullyQualifiedName~MainViewModelTests.InitializeRuntimeAsync_With_Lint_Errors_Blocks_Before_CrossSession_Validation" -nodeReuse:false -v:minimal`
- `dotnet build .\\platform\\ExperimentalControlPlatform.sln -nodeReuse:false -v:minimal`
- `dotnet test .\\platform\\ExperimentalControlPlatform.sln --no-build -nodeReuse:false -v:minimal`
