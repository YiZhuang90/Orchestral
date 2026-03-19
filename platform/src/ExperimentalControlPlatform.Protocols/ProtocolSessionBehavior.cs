using System;
using ExperimentalControlPlatform.Core.Artifacts;

namespace ExperimentalControlPlatform.Protocols;

public sealed record class ProtocolSessionBehavior
{
    public ProtocolSessionBehavior(
        ProtocolKind kind,
        TransportKind transport,
        bool requiresPersistentSession)
    {
        Kind = RequireDeclared(kind, nameof(kind));
        Transport = RequireDeclared(transport, nameof(transport));
        RequiresPersistentSession = requiresPersistentSession;
    }

    public ProtocolKind Kind { get; }

    public TransportKind Transport { get; }

    public bool RequiresPersistentSession { get; }

    // Projects the runtime-facing protocol primitive into the Core artifact shape.
    public ProtocolDefinition ToDefinition(
        ArtifactId id,
        string name,
        string messageStyle,
        string timingExpectation,
        string failureBehavior,
        string retryExpectation) =>
        new(
            id,
            name,
            Transport.ToString(),
            messageStyle,
            RequiresPersistentSession ? "persistent" : "transient",
            timingExpectation,
            failureBehavior,
            retryExpectation);

    private static TEnum RequireDeclared<TEnum>(TEnum value, string paramName)
        where TEnum : struct, Enum
    {
        if (Convert.ToInt32(value) == 0)
        {
            throw new ArgumentOutOfRangeException(paramName, value, $"{ToDisplayName(paramName)} must be a declared value.");
        }

        return value;
    }

    private static string ToDisplayName(string paramName) =>
        char.ToUpperInvariant(paramName[0]) + paramName[1..];
}
