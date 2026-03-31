using System;
using System.Collections.Generic;
using System.Linq;

namespace ExperimentalControlPlatform.Core.Artifacts;

public sealed record class CrossSessionValidationResult
{
    public CrossSessionValidationResult(IReadOnlyList<CrossSessionValidationIssue> issues)
    {
        if (issues is null)
        {
            throw new ArgumentNullException(nameof(issues));
        }

        Issues = issues.ToArray();
    }

    public IReadOnlyList<CrossSessionValidationIssue> Issues { get; }

    public bool IsValid => Issues.Count == 0;

    public string Summary =>
        IsValid
            ? "Cross-session validation passed."
            : string.Join(" | ", Issues.Select(static issue => $"{issue.Code}: {issue.Message}"));
}
