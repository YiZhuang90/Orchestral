using System;
using System.Threading.Tasks;
using ExperimentalControlPlatform.App.ExperimentMonitor;
using ExperimentalControlPlatform.Runtime;

namespace ExperimentalControlPlatform.App.Tests;

public sealed class ExperimentMonitorPanelViewModelTests
{
    [Fact]
    public void SelectedDisplayRateHz_Updates_DisplayTicker_Interval()
    {
        var snapshotPort = new SnapshotOutputPort<ExperimentMonitorSnapshot>(new ExperimentMonitorSnapshot());
        var ticker = new FakeDisplayTicker();
        using var viewModel = new ExperimentMonitorPanelViewModel(
            snapshotPort,
            initializeAsync: (_, _, _) => Task.FromResult(ExperimentMonitorInitializationResult.Ready("Ready.")),
            startAsync: () => Task.CompletedTask,
            stopAsync: () => Task.CompletedTask,
            closeWithoutApplyAsync: () => Task.CompletedTask,
            displayTicker: ticker);

        viewModel.SelectedDisplayRateHz = 5;

        Assert.Equal(TimeSpan.FromMilliseconds(200), ticker.Interval);
    }

    [Fact]
    public async Task Tick_Applies_Pending_Snapshot_To_Display_State()
    {
        var snapshotPort = new SnapshotOutputPort<ExperimentMonitorSnapshot>(new ExperimentMonitorSnapshot());
        var ticker = new FakeDisplayTicker();
        using var viewModel = new ExperimentMonitorPanelViewModel(
            snapshotPort,
            initializeAsync: (_, _, _) => Task.FromResult(ExperimentMonitorInitializationResult.Ready("Ready.")),
            startAsync: () => Task.CompletedTask,
            stopAsync: () => Task.CompletedTask,
            closeWithoutApplyAsync: () => Task.CompletedTask,
            displayTicker: ticker);

        snapshotPort.Publish(new ExperimentMonitorSnapshot
        {
            RunState = RunState.Running,
            RunDisplayName = "transition-demo-001",
            PrimaryControlSummary = "Re 1600 +/- 12",
            HighestSeverity = ExperimentMonitorSeverity.Warning,
            WarningCount = 1,
            Items =
            [
                new ExperimentMonitorItem(
                    "warn.controller_stale",
                    ExperimentMonitorSeverity.Warning,
                    "controller.re_primary",
                    "Measured value is stale.",
                    DateTimeOffset.Parse("2026-03-31T10:00:05+00:00"))
            ]
        });

        ticker.RaiseTick();

        Assert.Equal("Running", viewModel.CurrentRunStateLabel);
        Assert.Contains("transition-demo-001", viewModel.LiveSummary);
        Assert.Equal("Re 1600 +/- 12", viewModel.PrimaryControlSummary);
        Assert.Equal("Warnings: 1", viewModel.FooterHealthLabel);
        Assert.Single(viewModel.DisplayItems);

        await viewModel.CloseWithoutApplyAsync();
    }

    [Fact]
    public async Task Initialize_Start_And_Stop_Reset_Lifecycle_State()
    {
        var snapshotPort = new SnapshotOutputPort<ExperimentMonitorSnapshot>(new ExperimentMonitorSnapshot());
        var ticker = new FakeDisplayTicker();
        var initializeCalls = 0;
        var startCalls = 0;
        var stopCalls = 0;
        using var viewModel = new ExperimentMonitorPanelViewModel(
            snapshotPort,
            initializeAsync: (runIndex, target, note) =>
            {
                initializeCalls++;
                return Task.FromResult(ExperimentMonitorInitializationResult.Ready($"Initialized {runIndex} with {target}."));
            },
            startAsync: () =>
            {
                startCalls++;
                snapshotPort.Publish(new ExperimentMonitorSnapshot
                {
                    RunState = RunState.Running,
                    RunDisplayName = "transition-demo-001",
                    StateSummary = "Running healthy."
                });
                return Task.CompletedTask;
            },
            stopAsync: () =>
            {
                stopCalls++;
                snapshotPort.Publish(new ExperimentMonitorSnapshot
                {
                    RunState = RunState.Idle,
                    RunDisplayName = "transition-demo-001",
                    StateSummary = "Ready after stop: Manual stop."
                });
                return Task.CompletedTask;
            },
            closeWithoutApplyAsync: () => Task.CompletedTask,
            displayTicker: ticker);

        await viewModel.InitializeAsync();
        Assert.True(viewModel.CanStart);
        Assert.Equal(1, initializeCalls);

        await viewModel.StartAsync();
        ticker.RaiseTick();
        Assert.False(viewModel.CanStart);
        Assert.True(viewModel.CanStop);
        Assert.Equal(1, startCalls);

        await viewModel.StopAsync();
        ticker.RaiseTick();
        Assert.False(viewModel.CanStart);
        Assert.False(viewModel.CanStop);
        Assert.Equal("Not initialized.", viewModel.InitializationStatus);
        Assert.Equal(1, stopCalls);
    }

    [Fact]
    public async Task InitializeAsync_With_Invalid_Result_Keeps_Start_Disabled_And_Shows_Blocking_Items()
    {
        var snapshotPort = new SnapshotOutputPort<ExperimentMonitorSnapshot>(new ExperimentMonitorSnapshot());
        var ticker = new FakeDisplayTicker();
        using var viewModel = new ExperimentMonitorPanelViewModel(
            snapshotPort,
            initializeAsync: (_, _, _) => Task.FromResult(
                ExperimentMonitorInitializationResult.Blocked(
                    "Initialization blocked by cross-session validation.",
                    [
                        new ExperimentMonitorItem(
                            "alarm.cross_session_validation",
                            ExperimentMonitorSeverity.Alarm,
                            "cross-session validation",
                            "Control targets 'control.re_primary' and 'control.re_secondary' both command role 'role.control_center'.",
                            DateTimeOffset.Parse("2026-03-31T10:00:05+00:00"))
                    ])),
            startAsync: () => Task.CompletedTask,
            stopAsync: () => Task.CompletedTask,
            closeWithoutApplyAsync: () => Task.CompletedTask,
            displayTicker: ticker);

        await viewModel.InitializeAsync();
        ticker.RaiseTick();

        Assert.False(viewModel.CanStart);
        Assert.Equal("Initialization blocked by cross-session validation.", viewModel.InitializationStatus);
        Assert.Single(viewModel.DisplayItems);
        Assert.Equal(ExperimentMonitorSeverity.Alarm, viewModel.DisplayItems[0].Severity);
    }

    private sealed class FakeDisplayTicker : IDisplayTicker
    {
        public event Action? Tick;

        public TimeSpan Interval { get; set; }

        public void Start()
        {
        }

        public void Stop()
        {
        }

        public void RaiseTick() => Tick?.Invoke();

        public void Dispose()
        {
        }
    }
}
