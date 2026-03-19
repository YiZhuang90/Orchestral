using System;
using Xunit;

namespace ExperimentalControlPlatform.Runtime.Tests;

public sealed class RuntimeCoordinatorTests
{
    [Fact]
    public void InitialState_IsIdle()
    {
        var coordinator = new RuntimeCoordinator();

        Assert.Equal(RunState.Idle, coordinator.LatestSnapshot.State);
    }

    [Fact]
    public void Start_TransitionsRuntimeToRunning()
    {
        var coordinator = new RuntimeCoordinator();

        var context = coordinator.Start();

        Assert.Equal(RunState.Running, context.State);
        Assert.Equal(RunState.Running, coordinator.LatestSnapshot.State);
    }

    [Fact]
    public void StopRequest_ReturnsCompletedStopSnapshot()
    {
        var coordinator = new RuntimeCoordinator();
        coordinator.Start();

        var stopped = coordinator.RequestStop(StopReason.UserRequested("Operator stopped the run."));

        Assert.Equal(RunState.Idle, stopped.State);
        Assert.NotNull(stopped.StartedAtUtc);
        Assert.NotNull(stopped.StoppedAtUtc);
        Assert.NotNull(stopped.StopReason);
        Assert.Equal("UserRequested", stopped.StopReason!.Code);
        Assert.Equal("Operator stopped the run.", stopped.StopReason.Message);
        Assert.Equal(RunState.Idle, coordinator.LatestSnapshot.State);
        Assert.Equal(stopped, coordinator.LatestSnapshot);
    }

    [Fact]
    public void Start_RejectsDuplicateStartWhileAlreadyActive()
    {
        var coordinator = new RuntimeCoordinator();
        coordinator.Start();

        var exception = Assert.Throws<InvalidOperationException>(() => coordinator.Start());

        Assert.Contains("already active", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Stop_RejectsRequestWhileIdle()
    {
        var coordinator = new RuntimeCoordinator();

        var exception = Assert.Throws<InvalidOperationException>(
            () => coordinator.RequestStop(StopReason.UserRequested("Operator stopped the run.")));

        Assert.Contains("not active", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Start_AfterStop_BeginsNewRunAndClearsPreviousStopRequest()
    {
        var coordinator = new RuntimeCoordinator();
        var firstRun = coordinator.Start();
        coordinator.RequestStop(StopReason.UserRequested("Operator stopped the run."));

        var restarted = coordinator.Start();

        Assert.Equal(RunState.Running, restarted.State);
        Assert.NotEqual(firstRun.RunId, restarted.RunId);
        Assert.Null(restarted.StoppedAtUtc);
        Assert.Null(restarted.StopReason);
    }

    [Fact]
    public void LatestSnapshot_RetainsStoppedRunDetailsAfterStopCompletes()
    {
        var coordinator = new RuntimeCoordinator();
        var started = coordinator.Start();

        var stopped = coordinator.RequestStop(StopReason.UserRequested("Operator stopped the run."));

        Assert.Equal(started.RunId, coordinator.LatestSnapshot.RunId);
        Assert.Equal(stopped.StoppedAtUtc, coordinator.LatestSnapshot.StoppedAtUtc);
        Assert.Equal(stopped.StopReason, coordinator.LatestSnapshot.StopReason);
    }
}
