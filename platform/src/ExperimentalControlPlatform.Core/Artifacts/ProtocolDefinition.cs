using System;

namespace ExperimentalControlPlatform.Core.Artifacts;

public sealed record class ProtocolDefinition
{
    public ProtocolDefinition(
        ArtifactId id,
        string name,
        string transportFamily,
        string messageStyle,
        string connectionLifecycle,
        string timingExpectation,
        string failureBehavior,
        string retryExpectation)
    {
        Id = ArtifactId.Require(id, nameof(id));
        Name = RequireText(name, nameof(name));
        TransportFamily = RequireText(transportFamily, nameof(transportFamily));
        MessageStyle = RequireText(messageStyle, nameof(messageStyle));
        ConnectionLifecycle = RequireText(connectionLifecycle, nameof(connectionLifecycle));
        TimingExpectation = RequireText(timingExpectation, nameof(timingExpectation));
        FailureBehavior = RequireText(failureBehavior, nameof(failureBehavior));
        RetryExpectation = RequireText(retryExpectation, nameof(retryExpectation));
    }

    public ArtifactId Id { get; }

    public string Name { get; }

    public string TransportFamily { get; }

    public string MessageStyle { get; }

    public string ConnectionLifecycle { get; }

    public string TimingExpectation { get; }

    public string FailureBehavior { get; }

    public string RetryExpectation { get; }

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
