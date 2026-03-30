using System;
using ExperimentalControlPlatform.Core.Artifacts;
using Xunit;

namespace ExperimentalControlPlatform.Core.Tests.Artifacts;

public sealed class ControlTargetDefinitionTests
{
    [Fact]
    public void Constructor_Captures_Constant_Control_Target()
    {
        var target = new ControlTargetDefinition(
            new ArtifactId("control.re_primary"),
            "Primary Re Control",
            "Keeps Reynolds number near the requested target.",
            new ArtifactId("stream.reynolds_number"),
            new ArtifactId("role.flow_actuator"),
            "constant",
            "closed_loop",
            targetParameterId: new ArtifactId("param.re_target"));

        Assert.Equal(new ArtifactId("control.re_primary"), target.Id);
        Assert.Equal("constant", target.SetpointProfile);
        Assert.Equal("closed_loop", target.RegulationMode);
        Assert.Equal(new ArtifactId("param.re_target"), target.TargetParameterId);
        Assert.Null(target.ScheduleParameterId);
    }

    [Fact]
    public void Constructor_Captures_Scheduled_Control_Target()
    {
        var target = new ControlTargetDefinition(
            new ArtifactId("control.re_schedule"),
            "Scheduled Re Control",
            "Changes Reynolds number by run-time schedule.",
            new ArtifactId("stream.reynolds_number"),
            new ArtifactId("role.flow_actuator"),
            "scheduled",
            "closed_loop",
            scheduleParameterId: new ArtifactId("param.re_schedule"));

        Assert.Equal("scheduled", target.SetpointProfile);
        Assert.Equal(new ArtifactId("param.re_schedule"), target.ScheduleParameterId);
        Assert.Null(target.TargetParameterId);
    }

    [Fact]
    public void Constructor_Rejects_Constant_Target_Without_Target_Parameter()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            new ControlTargetDefinition(
                new ArtifactId("control.re_primary"),
                "Primary Re Control",
                "Keeps Reynolds number near the requested target.",
                new ArtifactId("stream.reynolds_number"),
                new ArtifactId("role.flow_actuator"),
                "constant",
                "closed_loop"));

        Assert.Contains("targetParameterId", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Constructor_Rejects_Scheduled_Target_Without_Schedule_Parameter()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            new ControlTargetDefinition(
                new ArtifactId("control.re_schedule"),
                "Scheduled Re Control",
                "Changes Reynolds number by run-time schedule.",
                new ArtifactId("stream.reynolds_number"),
                new ArtifactId("role.flow_actuator"),
                "scheduled",
                "closed_loop"));

        Assert.Contains("scheduleParameterId", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
}
