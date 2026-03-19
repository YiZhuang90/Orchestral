using System;
using ExperimentalControlPlatform.Core.Artifacts;
using Xunit;

namespace ExperimentalControlPlatform.Core.Tests.Artifacts;

public sealed class StopConditionDefinitionTests
{
    [Fact]
    public void Constructor_Captures_First_Class_Stop_Rule()
    {
        var stopCondition = new StopConditionDefinition(
            new ArtifactId("stop.over_temperature"),
            "Over Temperature",
            "safety-based",
            "temperature_inlet.samples > temperature_limit_c for 3s",
            "stop",
            "temperature limit exceeded");

        Assert.Equal("Over Temperature", stopCondition.Name);
        Assert.Equal("safety-based", stopCondition.Kind);
        Assert.Equal("temperature_inlet.samples > temperature_limit_c for 3s", stopCondition.Condition);
        Assert.Equal("stop", stopCondition.Action);
        Assert.Equal("temperature limit exceeded", stopCondition.Reason);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_Rejects_Blank_Condition_And_Reason(string invalidText)
    {
        var id = new ArtifactId("stop.over_temperature");

        var conditionException = Assert.Throws<ArgumentException>(() =>
            new StopConditionDefinition(
                id,
                "Over Temperature",
                "safety-based",
                invalidText,
                "stop",
                "temperature limit exceeded"));
        Assert.Contains("Condition", conditionException.Message, StringComparison.Ordinal);

        var reasonException = Assert.Throws<ArgumentException>(() =>
            new StopConditionDefinition(
                id,
                "Over Temperature",
                "safety-based",
                "temperature_inlet.samples > temperature_limit_c for 3s",
                "stop",
                invalidText));
        Assert.Contains("Reason", reasonException.Message, StringComparison.Ordinal);
    }
}
