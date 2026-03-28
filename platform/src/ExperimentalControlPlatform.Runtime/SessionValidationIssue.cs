namespace ExperimentalControlPlatform.Runtime;

public sealed record SessionValidationIssue(
    string Field,
    string Message);
