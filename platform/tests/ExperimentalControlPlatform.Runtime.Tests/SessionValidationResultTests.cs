using System;
using Xunit;

namespace ExperimentalControlPlatform.Runtime.Tests;

public sealed class SessionValidationResultTests
{
    [Fact]
    public void Valid_CreatesSuccessResult()
    {
        var result = SessionValidationResult.Valid("Validated microphone settings.");

        Assert.True(result.IsValid);
        Assert.Equal("Validated microphone settings.", result.Summary);
        Assert.Empty(result.Issues);
    }

    [Fact]
    public void Invalid_CreatesSingleIssueSummary()
    {
        var result = SessionValidationResult.Invalid("TargetUpdateRateHz", "Target update rate must be positive.");

        Assert.False(result.IsValid);
        Assert.Single(result.Issues);
        Assert.Equal("TargetUpdateRateHz", result.Issues[0].Field);
        Assert.Contains("Target update rate must be positive.", result.Summary, StringComparison.Ordinal);
    }

    [Fact]
    public void ThrowIfInvalid_ThrowsWithSummary()
    {
        var result = SessionValidationResult.Invalid("WindowMilliseconds", "Window must be positive.");

        var exception = Assert.Throws<InvalidOperationException>(() => result.ThrowIfInvalid());

        Assert.Contains("Window must be positive.", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void FromIssues_AggregatesMultipleMessages()
    {
        var result = SessionValidationResult.FromIssues(
            "Validated settings.",
            [
                new SessionValidationIssue("TargetFrameRate", "Target frame rate must be positive."),
                new SessionValidationIssue("CameraIndex", "Camera index does not match the session.")
            ]);

        Assert.False(result.IsValid);
        Assert.Equal(2, result.Issues.Count);
        Assert.Contains("Target frame rate must be positive.", result.Summary, StringComparison.Ordinal);
        Assert.Contains("Camera index does not match the session.", result.Summary, StringComparison.Ordinal);
    }
}
