using System;
using System.Collections.Generic;
using System.Linq;

namespace ExperimentalControlPlatform.Runtime;

public sealed record SessionValidationResult
{
    private SessionValidationResult(string summary, IReadOnlyList<SessionValidationIssue> issues)
    {
        Summary = summary;
        Issues = issues;
    }

    public string Summary { get; }

    public IReadOnlyList<SessionValidationIssue> Issues { get; }

    public bool IsValid => Issues.Count == 0;

    public static SessionValidationResult Valid(string summary) =>
        new(summary, Array.Empty<SessionValidationIssue>());

    public static SessionValidationResult Invalid(string field, string message) =>
        new(message, [new SessionValidationIssue(field, message)]);

    public static SessionValidationResult FromIssues(string successSummary, IReadOnlyList<SessionValidationIssue> issues)
    {
        ArgumentNullException.ThrowIfNull(issues);

        if (issues.Count == 0)
        {
            return Valid(successSummary);
        }

        var summary = string.Join(" ", issues.Select(issue => issue.Message));
        return new SessionValidationResult(summary, issues.ToArray());
    }

    public void ThrowIfInvalid()
    {
        if (!IsValid)
        {
            throw new InvalidOperationException(Summary);
        }
    }
}
