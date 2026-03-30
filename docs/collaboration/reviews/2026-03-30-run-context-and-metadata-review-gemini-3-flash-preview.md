I will start by reading the foundation architectural documents and the development plan to establish the context and requirements for the run context and metadata implementation.

I will now read the core and runtime definitions to evaluate the implementation of the run context and metadata.

I have reviewed the core and runtime files and will now examine the app layer and tests.

# Run Context and Metadata Slice Review

Overall, the implementation is architecturally sound and aligns well with the existing experiment-definition and runtime-coordinator models. The introduction of `RunContextDefinition` effectively bridges the gap between experiment intent and live execution state.

## Blocking Issues

No blocking issues found.

## Non-blocking Improvements

### 1. Missing collection validation in `ArtifactBoundaryValidationTests`
- **Severity:** Low
- **File:** `platform/tests/ExperimentalControlPlatform.Core.Tests/Artifacts/ArtifactBoundaryValidationTests.cs`
- **Exact Issue:** The tests for `RunContextDefinition` only verify that the primary `Id` cannot be default. They do not verify that elements within `MetadataValues`, `AppliedSettingsSnapshotIds`, `DecisionLogEntryIds`, or `ArtifactReferenceIds` are also validated against default values.
- **Why it matters:** While the implementation in `RunContextDefinition.cs` uses `ArtifactId.Require` correctly, the lack of boundary tests for these collections is inconsistent with other artifacts like `RunManifestDefinition`. It leaves the door open for future regressions where collection-level validation might be accidentally bypassed.
- **Recommended fix:** Add a test case to `ArtifactBoundaryValidationTests.cs` that specifically passes collections containing `default` (empty) `ArtifactId` values to the `RunContextDefinition` constructor and asserts that an `ArgumentException` is thrown.

### 2. Potential confusion between `RunId` (Guid) and `RunContext.Id` (ArtifactId)
- **Severity:** Low
- **File:** `platform/src/ExperimentalControlPlatform.Runtime/RuntimeRunContext.cs`
- **Exact Issue:** The runtime context carries both a `Guid RunId` and a `RunContextDefinition RunContext` (which contains an `ArtifactId`).
- **Why it matters:** In the context of "Orchestral," `RunId` appears to be the identity of a specific *execution instance* (a "flight"), whereas `RunContext.Id` is the identity of the *run definition/intent* artifact. Without documentation, developers might confuse these, especially when logging or referencing runs in downstream modules.
- **Recommended fix:** Add XML documentation to `RuntimeRunContext` clarifying the distinction: `RunId` is the unique ephemeral identifier for this specific runtime execution, while `RunContext.Id` is the stable identifier for the artifact defining the run's metadata and intent.

### 3. Redundant parameter logic in `RuntimeRunContext` constructor
- **Severity:** Low
- **File:** `platform/src/ExperimentalControlPlatform.Runtime/RuntimeRunContext.cs`
- **Exact Issue:** If `runContext` is provided, the constructor validates that the `experiment` argument matches `runContext.Experiment` and then overrides it anyway.
- **Why it matters:** This adds unnecessary complexity and branching to a core data record. Since `RunContextDefinition` always carries its associated experiment, requiring it as a separate argument in the runtime snapshot constructor when a context is present is redundant.
- **Recommended fix:** Consider adding a specialized constructor or a static factory method (e.g., `FromRunContext`) that takes a `RunContextDefinition` and correctly populates the `experiment` field, reducing the risk of mismatched arguments and simplifying the call site in `RuntimeCoordinator`.

### 4. Over-normalization of Optional Text
- **Severity:** Low
- **File:** `platform/src/ExperimentalControlPlatform.Core/Artifacts/RunContextDefinition.cs`
- **Exact Issue:** `NormalizeOptional` converts whitespace-only strings to `null`.
- **Why it matters:** While generally helpful for `OperatorNote`, if a user intentionally enters a space (unlikely but possible), it is lost. More importantly, this behavior is slightly different from `RequireText` which trims and then requires non-empty.
- **Recommended fix:** The current implementation is acceptable for a first pass, but ensure this "whitespace is null" behavior is documented or consistent with how the UI handles empty/placeholder text in these fields.

