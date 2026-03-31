using System;

namespace ExperimentalControlPlatform.Core.Artifacts;

public sealed record class ExperimentDefinitionLintIssue
{
    public ExperimentDefinitionLintIssue(
        string code,
        ExperimentDefinitionLintSeverity severity,
        string message,
        ArtifactId? artifactId = null,
        ArtifactId? relatedArtifactId = null)
    {
        Code = RequireText(code, nameof(code));
        Severity = severity;
        Message = RequireText(message, nameof(message));
        ArtifactId = NormalizeOptionalArtifactId(artifactId, nameof(artifactId));
        RelatedArtifactId = NormalizeOptionalArtifactId(relatedArtifactId, nameof(relatedArtifactId));
    }

    public string Code { get; }

    public ExperimentDefinitionLintSeverity Severity { get; }

    public string Message { get; }

    public ArtifactId? ArtifactId { get; }

    public ArtifactId? RelatedArtifactId { get; }

    private static ArtifactId? NormalizeOptionalArtifactId(ArtifactId? value, string paramName)
    {
        if (!value.HasValue)
        {
            return null;
        }

        return ExperimentalControlPlatform.Core.Artifacts.ArtifactId.Require(value.Value, paramName);
    }

    private static string RequireText(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{ToDisplayName(paramName)} is required.", paramName);
        }

        return value.Trim();
    }

    private static string ToDisplayName(string paramName) =>
        char.ToUpperInvariant(paramName[0]) + paramName[1..];
}
