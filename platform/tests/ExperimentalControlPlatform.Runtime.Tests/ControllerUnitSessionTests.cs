using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ExperimentalControlPlatform.Core.Artifacts;
using Xunit;

namespace ExperimentalControlPlatform.Runtime.Tests;

public sealed class ControllerUnitSessionTests
{
    private static readonly DateTimeOffset RunStartedAt = new(2026, 3, 30, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task SampleAsync_Computes_Constant_ClosedLoop_Target_And_Error()
    {
        var applied = new List<ControllerDecision>();
        var session = new ControllerUnitSession(
            CreateResolvedExperimentDefinition(
                new ControlTargetDefinition(
                    new ArtifactId("control.re_primary"),
                    "Primary Re Control",
                    "Keeps Reynolds number near the requested target.",
                    new ArtifactId("stream.reynolds_number"),
                    new ArtifactId("role.flow_actuator"),
                    "constant",
                    "closed_loop",
                    targetParameterId: new ArtifactId("param.re_target"))),
            new ArtifactId("control.re_primary"),
            RunStartedAt,
            applyDecisionAsync: (decision, _) =>
            {
                applied.Add(decision);
                return Task.CompletedTask;
            });

        await session.SampleAsync(RunStartedAt.AddSeconds(5), 1588);

        var state = Assert.IsType<ControllerUnitState>(session.State.Current);
        Assert.Equal(1600, state.TargetValue);
        Assert.Equal(1588, state.MeasuredValue);
        Assert.Equal(12, state.ErrorValue);
        Assert.Equal(12, state.ControlOutputValue);
        Assert.Equal("proportional_error", state.ControlOutputInterpretation);
        Assert.False(state.MeasuredValueIsStale);
        Assert.Equal(RunStartedAt.AddSeconds(5), state.MeasuredValueObservedAtUtc);
        Assert.Single(applied);
        Assert.Equal(12, applied[0].ControlOutputValue);
        Assert.Equal("proportional_error", applied[0].ControlOutputInterpretation);
        Assert.False(applied[0].MeasuredValueIsStale);
        Assert.Equal(RunStartedAt.AddSeconds(5), applied[0].MeasuredValueObservedAtUtc);
    }

    [Fact]
    public async Task SampleAsync_Computes_Constant_OpenLoop_Target_Without_Feedback_Adjustment()
    {
        var session = new ControllerUnitSession(
            CreateResolvedExperimentDefinition(
                new ControlTargetDefinition(
                    new ArtifactId("control.re_primary"),
                    "Primary Re Control",
                    "Uses a fixed target without feedback adjustment.",
                    new ArtifactId("stream.reynolds_number"),
                    new ArtifactId("role.flow_actuator"),
                    "constant",
                    "open_loop",
                    targetParameterId: new ArtifactId("param.re_target"))),
            new ArtifactId("control.re_primary"),
            RunStartedAt);

        await session.SampleAsync(RunStartedAt.AddSeconds(5), 1588);

        var state = Assert.IsType<ControllerUnitState>(session.State.Current);
        Assert.Equal(1600, state.TargetValue);
        Assert.Equal(1588, state.MeasuredValue);
        Assert.Equal(12, state.ErrorValue);
        Assert.Equal(1600, state.ControlOutputValue);
        Assert.Equal("setpoint_passthrough", state.ControlOutputInterpretation);
    }

    [Fact]
    public async Task SampleAsync_Evaluates_Scheduled_Target_Against_RunRelative_Time()
    {
        var session = new ControllerUnitSession(
            CreateResolvedExperimentDefinition(
                new ControlTargetDefinition(
                    new ArtifactId("control.re_schedule"),
                    "Scheduled Re Control",
                    "Changes target value over run time.",
                    new ArtifactId("stream.reynolds_number"),
                    new ArtifactId("role.flow_actuator"),
                    "scheduled",
                    "closed_loop",
                    scheduleParameterId: new ArtifactId("param.re_schedule"))),
            new ArtifactId("control.re_schedule"),
            RunStartedAt);

        await session.SampleAsync(RunStartedAt.AddSeconds(15), 1695);

        var state = Assert.IsType<ControllerUnitState>(session.State.Current);
        Assert.Equal(1700, state.TargetValue);
        Assert.Equal(1695, state.MeasuredValue);
        Assert.Equal(5, state.ErrorValue);
    }

    [Fact]
    public async Task SampleAsync_Carries_Forward_Previous_Measurement_As_Stale()
    {
        var session = new ControllerUnitSession(
            CreateResolvedExperimentDefinition(
                new ControlTargetDefinition(
                    new ArtifactId("control.re_primary"),
                    "Primary Re Control",
                    "Keeps Reynolds number near the requested target.",
                    new ArtifactId("stream.reynolds_number"),
                    new ArtifactId("role.flow_actuator"),
                    "constant",
                    "closed_loop",
                    targetParameterId: new ArtifactId("param.re_target"))),
            new ArtifactId("control.re_primary"),
            RunStartedAt);

        await session.SampleAsync(RunStartedAt.AddSeconds(5), 1588);
        await session.SampleAsync(RunStartedAt.AddSeconds(6));

        var state = Assert.IsType<ControllerUnitState>(session.State.Current);
        Assert.Equal(1588, state.MeasuredValue);
        Assert.True(state.MeasuredValueIsStale);
        Assert.Equal(RunStartedAt.AddSeconds(5), state.MeasuredValueObservedAtUtc);
        Assert.Contains("stale measured value", state.StatusMessage, StringComparison.OrdinalIgnoreCase);

        var decision = Assert.IsType<ControllerDecision>(session.LatestDecision.Current);
        Assert.True(decision.MeasuredValueIsStale);
        Assert.Equal(RunStartedAt.AddSeconds(5), decision.MeasuredValueObservedAtUtc);
    }

    [Fact]
    public async Task SampleAsync_Rejects_OutOfOrder_Timestamps()
    {
        var session = new ControllerUnitSession(
            CreateResolvedExperimentDefinition(
                new ControlTargetDefinition(
                    new ArtifactId("control.re_primary"),
                    "Primary Re Control",
                    "Keeps Reynolds number near the requested target.",
                    new ArtifactId("stream.reynolds_number"),
                    new ArtifactId("role.flow_actuator"),
                    "constant",
                    "closed_loop",
                    targetParameterId: new ArtifactId("param.re_target"))),
            new ArtifactId("control.re_primary"),
            RunStartedAt);

        await session.SampleAsync(RunStartedAt.AddSeconds(5), 1588);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => session.SampleAsync(RunStartedAt.AddSeconds(4), 1589));

        Assert.Contains("out of order", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task StopAsync_Uses_Caller_Supplied_Stop_Timestamp()
    {
        var session = new ControllerUnitSession(
            CreateResolvedExperimentDefinition(
                new ControlTargetDefinition(
                    new ArtifactId("control.re_primary"),
                    "Primary Re Control",
                    "Keeps Reynolds number near the requested target.",
                    new ArtifactId("stream.reynolds_number"),
                    new ArtifactId("role.flow_actuator"),
                    "constant",
                    "closed_loop",
                    targetParameterId: new ArtifactId("param.re_target"))),
            new ArtifactId("control.re_primary"),
            RunStartedAt);

        var stoppedAt = RunStartedAt.AddSeconds(42);
        await session.StopAsync(StopReason.UserRequested("Stopped by test."), stoppedAt);

        var sessionEnd = Assert.IsType<DeviceSessionEndSnapshot>(session.SessionEnd.Current);
        Assert.Equal(stoppedAt, sessionEnd.EndedAt);
    }

    [Fact]
    public async Task SampleAsync_Serializes_Overlapping_Apply_Callbacks()
    {
        var enteredFirstApply = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseFirstApply = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var applyCount = 0;
        var concurrentApplyCount = 0;
        var maxConcurrentApplyCount = 0;

        var session = new ControllerUnitSession(
            CreateResolvedExperimentDefinition(
                new ControlTargetDefinition(
                    new ArtifactId("control.re_primary"),
                    "Primary Re Control",
                    "Keeps Reynolds number near the requested target.",
                    new ArtifactId("stream.reynolds_number"),
                    new ArtifactId("role.flow_actuator"),
                    "constant",
                    "closed_loop",
                    targetParameterId: new ArtifactId("param.re_target"))),
            new ArtifactId("control.re_primary"),
            RunStartedAt,
            applyDecisionAsync: async (_, _) =>
            {
                var currentApplyCount = Interlocked.Increment(ref applyCount);
                var currentConcurrent = Interlocked.Increment(ref concurrentApplyCount);
                maxConcurrentApplyCount = Math.Max(maxConcurrentApplyCount, currentConcurrent);

                if (currentApplyCount == 1)
                {
                    enteredFirstApply.SetResult();
                    await releaseFirstApply.Task;
                }

                Interlocked.Decrement(ref concurrentApplyCount);
            });

        var firstSampleTask = session.SampleAsync(RunStartedAt.AddSeconds(5), 1588);
        await enteredFirstApply.Task;

        var secondSampleTask = session.SampleAsync(RunStartedAt.AddSeconds(6), 1590);
        await Task.Delay(100);
        Assert.False(secondSampleTask.IsCompleted);

        releaseFirstApply.SetResult();
        await Task.WhenAll(firstSampleTask, secondSampleTask);

        Assert.Equal(1, maxConcurrentApplyCount);
        Assert.Equal(2, session.State.Current!.SampleSequence);
    }

    private static ResolvedExperimentDefinition CreateResolvedExperimentDefinition(ControlTargetDefinition controlTarget)
    {
        var experiment = new ExperimentDefinition(
            new ArtifactId("exp.turbulence_transition_v1"),
            "Turbulence Transition",
            "Observe and hold Reynolds number near the requested target.",
            [
                new DeviceRoleDefinition(
                    new ArtifactId("role.flow_actuator"),
                    "Flow Actuator",
                    "Controls the downstream actuator.",
                    [new ArtifactId("cap.pulse_actuation"), new ArtifactId("cap.status_report")],
                    new ArtifactId("protocol.serial_ascii_v1"))
            ],
            [
                new ParameterDefinition(
                    new ArtifactId("param.re_target"),
                    "Re Target",
                    "float",
                    "experiment",
                    unit: "dimensionless"),
                new ParameterDefinition(
                    new ArtifactId("param.re_schedule"),
                    "Re Schedule",
                    "time_series",
                    "experiment",
                    unit: "dimensionless")
            ],
            [
                new StreamDefinition(
                    new ArtifactId("stream.reynolds_number"),
                    "Reynolds Number",
                    "transform",
                    "scalar<double>",
                    "runtime")
            ],
            [],
            [],
            [],
            [],
            [controlTarget]);
        var device = new DeviceDefinition(
            new ArtifactId("device.controller_01"),
            "Controller 01",
            "controller_01",
            new ProtocolDefinition(
                new ArtifactId("protocol.serial_ascii_v1"),
                "Serial ASCII",
                "serial",
                "request-response",
                "session",
                "best-effort",
                "fail-fast",
                "manual"),
            [
                new CapabilityDefinition(new ArtifactId("cap.pulse_actuation"), "pulse_actuation", "Issues trigger pulses."),
                new CapabilityDefinition(new ArtifactId("cap.status_report"), "status_report", "Reports actuator state.")
            ],
            [],
            "healthy");
        var binding = new RoleBindingDefinition(
            new ArtifactId("role.flow_actuator"),
            device.Id,
            device.Protocol.Id,
            [new ArtifactId("cap.pulse_actuation"), new ArtifactId("cap.status_report")]);

        return new ResolvedExperimentDefinition(
            experiment,
            "1.0.0",
            [device],
            [binding],
            new Dictionary<ArtifactId, string>
            {
                [new ArtifactId("param.re_target")] = "1600",
                [new ArtifactId("param.re_schedule")] = "0:1600;10:1700;20:1800"
            });
    }
}
