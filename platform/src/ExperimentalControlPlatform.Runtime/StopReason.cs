using System;

namespace ExperimentalControlPlatform.Runtime;

public sealed record class StopReason
{
    public StopReason(string code, string message)
    {
        Code = RequireText(code, nameof(code));
        Message = RequireText(message, nameof(message));
    }

    public string Code { get; }

    public string Message { get; }

    public static StopReason UserRequested(string message) => new("UserRequested", message);

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
