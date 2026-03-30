using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using ExperimentalControlPlatform.App.DevicePanels;
using ExperimentalControlPlatform.App.DevicePanels.Contracts;
using ExperimentalControlPlatform.Runtime;

namespace ExperimentalControlPlatform.App.Tests;

public sealed class MainViewModelTests
{
    [Fact]
    public async Task StopRuntimeAsync_Completes_Run_Recording_And_Stores_LastRunRecording()
    {
        var rootDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(rootDirectory);

        try
        {
            var panel = new FakeIntegrationPanel(
                "Control Center",
                new IntegrationPanelDataOutput
                {
                    DeviceId = "control_center_01",
                    PayloadType = IntegrationPanelOutputPayloadType.CommandResult.ToString(),
                    PayloadValue = "PulseCount=42"
                },
                diagnosticsOutput: new IntegrationPanelDiagnosticsOutput
                {
                    LastCommand = "ReadPulseCount",
                    LastStateTransition = "Running -> Idle"
                },
                sessionEndOutput: new IntegrationPanelSessionEndOutput
                {
                    EndedAt = DateTimeOffset.Parse("2026-03-30T14:05:00+02:00"),
                    ExitReason = "Operator requested stop.",
                    ConnectionClosed = true,
                    LiveStopped = true
                });
            var coordinator = new RuntimeCoordinator(new FakeRegistry());
            var recorder = new RunRecorder(rootDirectory);
            using var viewModel = new MainViewModel(coordinator, new[] { panel }, recorder);

            viewModel.StartRuntime();
            await viewModel.StopRuntimeAsync();

            Assert.NotNull(viewModel.LastRunRecording);
            Assert.True(File.Exists(viewModel.LastRunRecording!.ManifestPath));
            Assert.Contains("Ready after stop", viewModel.RuntimeStatus.StateSummary);
        }
        finally
        {
            if (Directory.Exists(rootDirectory))
            {
                Directory.Delete(rootDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task FinalizeRunRecordingForLatestStoppedRun_Completes_Recording_After_External_Stop()
    {
        var rootDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(rootDirectory);

        try
        {
            var panel = new FakeIntegrationPanel(
                "Control Center",
                new IntegrationPanelDataOutput
                {
                    DeviceId = "control_center_01",
                    PayloadType = IntegrationPanelOutputPayloadType.CommandResult.ToString(),
                    PayloadValue = "PulseCount=42"
                });
            var coordinator = new RuntimeCoordinator(new FakeRegistry());
            var recorder = new RunRecorder(rootDirectory);
            using var viewModel = new MainViewModel(coordinator, new[] { panel }, recorder);

            viewModel.StartRuntime();
            await coordinator.EnsureStoppedAsync(StopReason.UserRequested("Application shutdown."));
            viewModel.FinalizeRunRecordingForLatestStoppedRun();

            Assert.NotNull(viewModel.LastRunRecording);
            Assert.True(File.Exists(viewModel.LastRunRecording!.ManifestPath));
        }
        finally
        {
            if (Directory.Exists(rootDirectory))
            {
                Directory.Delete(rootDirectory, recursive: true);
            }
        }
    }

    private sealed class FakeRegistry : IDeviceSessionRegistry
    {
        public IReadOnlyCollection<IDeviceSession> Sessions => Array.Empty<IDeviceSession>();

        public TSession GetOrAdd<TSession>(DeviceSessionId sessionId, Func<TSession> factory)
            where TSession : class, IDeviceSession => throw new NotSupportedException();

        public bool TryGet<TSession>(DeviceSessionId sessionId, out TSession? session)
            where TSession : class, IDeviceSession
        {
            session = null;
            return false;
        }

        public bool Remove(DeviceSessionId sessionId) => false;

        public Task StopAllAsync(StopReason reason, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakeIntegrationPanel : IDeviceTestPanelViewModel, IIntegrationPanelViewModel
    {
        public FakeIntegrationPanel(
            string title,
            IntegrationPanelDataOutput? dataOutput,
            IntegrationPanelAppliedSettingsOutput? appliedSettingsOutput = null,
            IntegrationPanelStatusOutput? statusOutput = null,
            IntegrationPanelDiagnosticsOutput? diagnosticsOutput = null,
            IntegrationPanelSessionEndOutput? sessionEndOutput = null)
        {
            Title = title;
            DataOutput = dataOutput;
            AppliedSettingsOutput = appliedSettingsOutput;
            StatusOutput = statusOutput;
            DiagnosticsOutput = diagnosticsOutput;
            SessionEndOutput = sessionEndOutput;
        }

        public event PropertyChangedEventHandler? PropertyChanged
        {
            add { }
            remove { }
        }

        public string Title { get; }

        public IntegrationPanelDataOutput? DataOutput { get; }

        public IntegrationPanelAppliedSettingsOutput? AppliedSettingsOutput { get; }

        public IntegrationPanelStatusOutput? StatusOutput { get; }

        public IntegrationPanelDiagnosticsOutput? DiagnosticsOutput { get; }

        public IntegrationPanelSessionEndOutput? SessionEndOutput { get; }

        public IReadOnlyList<IntegrationPanelLifecycleAction> SupportedLifecycleActions { get; } = [];

        public ICommand LifecycleActionCommand { get; } = new NoOpCommand();

        public void Dispose()
        {
        }
    }

    private sealed class NoOpCommand : ICommand
    {
        public event EventHandler? CanExecuteChanged
        {
            add { }
            remove { }
        }

        public bool CanExecute(object? parameter) => false;

        public void Execute(object? parameter)
        {
        }
    }
}
