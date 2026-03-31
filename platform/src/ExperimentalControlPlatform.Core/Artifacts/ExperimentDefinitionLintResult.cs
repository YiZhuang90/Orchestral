using System;
using System.Collections.Generic;
using System.Linq;

namespace ExperimentalControlPlatform.Core.Artifacts;

public sealed record class ExperimentDefinitionLintResult
{
    public ExperimentDefinitionLintResult(IReadOnlyList<ExperimentDefinitionLintIssue> issues)
    {
        ArgumentNullException.ThrowIfNull(issues);
        Issues = issues.ToArray();
    }

    public IReadOnlyList<ExperimentDefinitionLintIssue> Issues { get; }

    public bool IsValid => Issues.All(static issue => issue.Severity != ExperimentDefinitionLintSeverity.Error);

    public string Summary => Issues.Count == 0
        ? "No experiment-definition lint issues."
        : string.Join(Environment.NewLine, Issues.Select(static issue => $"- [{issue.Severity}] {issue.Message}"));
}
