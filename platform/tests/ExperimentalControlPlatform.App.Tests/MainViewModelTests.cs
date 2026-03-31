using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using ExperimentalControlPlatform.App.DevicePanels;
using ExperimentalControlPlatform.App.DevicePanels.Contracts;
using ExperimentalControlPlatform.App.ExperimentMonitor;
using ExperimentalControlPlatform.Core.Artifacts;
using ExperimentalControlPlatform.Devices.ControlCenter;
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
            using var viewModel = new MainViewModel(coordinator, new FakeRegistry(), new[] { panel }, recorder);

            Assert.IsType<ExperimentMonitorPanelViewModel>(viewModel.CurrentDevicePanel);

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
            using var viewModel = new MainViewModel(coordinator, new FakeRegistry(), new[] { panel }, recorder);

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

    [Fact]
    public async Task StartRuntime_With_ControlCenterSession_Writes_FlowReynolds_Artifacts_On_Stop()
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
            var service = new FakeControlCenterService(
                new[]
                {
                    new ControlCenterPulseReadback("control_center_01", "Control Center", 1.0, 10, DateTimeOffset.Parse("2026-03-31T14:00:01+02:00")),
                    new ControlCenterPulseReadback("control_center_01", "Control Center", 2.0, 14, DateTimeOffset.Parse("2026-03-31T14:00:02+02:00")),
                    new ControlCenterPulseReadback("control_center_01", "Control Center", 3.0, 18, DateTimeOffset.Parse("2026-03-31T14:00:03+02:00"))
                });
            var controlCenterSession = new ControlCenterSession(
                service,
                new ControlCenterDeviceInfo("COM9", "Control Center", "control_center_01"));
            await controlCenterSession.ConnectAsync();

            var registry = new FakeRegistry(controlCenterSession);
            var coordinator = new RuntimeCoordinator(registry);
            var recorder = new RunRecorder(rootDirectory);
            using var viewModel = new MainViewModel(coordinator, registry, new[] { panel }, recorder);

            await viewModel.StartRuntimeAsync();
            await service.WaitForReadCountAsync(2, TimeSpan.FromSeconds(3));
            await viewModel.StopRuntimeAsync();

            Assert.NotNull(viewModel.LastRunRecording);
            Assert.Contains(
                viewModel.LastRunRecording!.ArtifactPaths.Values,
                path => path.EndsWith("flow-reynolds-snapshot.yaml", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(
                viewModel.LastRunRecording.ArtifactPaths.Values,
                path => path.EndsWith("flow-reynolds-record.yaml", StringComparison.OrdinalIgnoreCase));

            await controlCenterSession.DisposeAsync();
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
    public async Task InitializeRuntimeAsync_With_Lint_Errors_Blocks_Before_CrossSession_Validation()
    {
        var coordinator = new RuntimeCoordinator(new FakeRegistry());
        using var viewModel = new MainViewModel(
            coordinator,
            new FakeRegistry(),
            new[] { new FakeIntegrationPanel("Camera", null) },
            runRecorder: null,
            adHocRunDefinitionFactory: new InvalidLintRunDefinitionFactory());

        var result = await viewModel.InitializeRuntimeAsync("run-001", null, null);

        Assert.False(result.IsReady);
        Assert.Equal("Initialization blocked by experiment-definition linting.", result.StatusMessage);
        Assert.Contains(result.ValidationItems, item => item.Source == "experiment-definition linting");
    }

    private sealed class FakeRegistry : IDeviceSessionRegistry
    {
        private readonly IReadOnlyCollection<IDeviceSession> _sessions;

        public FakeRegistry(params IDeviceSession[] sessions)
        {
            _sessions = sessions;
        }

#pragma warning disable CS0067
        public event Action? SessionsChanged;
#pragma warning restore CS0067

        public IReadOnlyCollection<IDeviceSession> Sessions => _sessions;

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

    private sealed class InvalidLintRunDefinitionFactory : IRunDefinitionFactory
    {
        public ResolvedExperimentDefinition Create(IEnumerable<IDeviceTestPanelViewModel> panels)
        {
            return Create(panels, primaryControlTargetValue: null);
        }

        public ResolvedExperimentDefinition Create(IEnumerable<IDeviceTestPanelViewModel> panels, double? primaryControlTargetValue)
        {
            var experiment = new ExperimentDefinition(
                new ArtifactId("exp.invalid_lint_block"),
                "Invalid Lint Block",
                "Returns an authored package with a broken monitor reference.",
                [
                    new DeviceRoleDefinition(
                        new ArtifactId("role.camera_upstream"),
                        "Upstream Camera",
                        "Captures the upstream view.",
                        [new ArtifactId("cap.frame_stream")],
                        new ArtifactId("protocol.vendor_sdk_camera_v1"))
                ],
                [],
                [
                    new StreamDefinition(
                        new ArtifactId("stream.frame"),
                        "Frame",
                        "device",
                        "image",
                        "sample")
                ],
                [],
                [
                    new MonitorDefinition(
                        new ArtifactId("monitor.signal"),
                        "Signal Monitor",
                        "Watches a missing stream.",
                        "display-only",
                        [new ArtifactId("stream.missing_signal")])
                ],
                [],
                []);

            var device = new DeviceDefinition(
                new ArtifactId("device.camera_01"),
                "Camera 01",
                "camera_01",
                new ProtocolDefinition(
                    new ArtifactId("protocol.vendor_sdk_camera_v1"),
                    "Vendor Camera",
                    "sdk",
                    "request-response",
                    "session",
                    "best-effort",
                    "fail-fast",
                    "manual"),
                [
                    new CapabilityDefinition(
                        new ArtifactId("cap.frame_stream"),
                        "frame_stream",
                        "Produces image frames.")
                ],
                [],
                "healthy");
            var binding = new RoleBindingDefinition(
                new ArtifactId("role.camera_upstream"),
                device.Id,
                device.Protocol.Id,
                [new ArtifactId("cap.frame_stream")]);

            return new ResolvedExperimentDefinition(
                experiment,
                "test.v1",
                [device],
                [binding],
                new Dictionary<ArtifactId, string>());
        }
    }

    private sealed class FakeControlCenterService : IControlCenterService
    {
        private readonly object _syncRoot = new();
        private readonly Queue<ControlCenterPulseReadback> _readbacks;
        private ControlCenterPulseReadback? _lastReadback;
        private int _readCount;

        public FakeControlCenterService(IEnumerable<ControlCenterPulseReadback> readbacks)
        {
            _readbacks = new Queue<ControlCenterPulseReadback>(readbacks);
        }

        public IReadOnlyList<ControlCenterDeviceInfo> ListDevices() => [];

        public IControlCenterConnection Open(ControlCenterDeviceInfo device) => new FakeControlCenterConnection(device);

        public void SendCommand(IControlCenterConnection connection, ControlCenterCommand command)
        {
        }

        public ControlCenterPulseReadback ReadPulseCount(IControlCenterConnection connection)
        {
            lock (_syncRoot)
            {
                if (_readbacks.Count > 0)
                {
                    _lastReadback = _readbacks.Dequeue();
                }

                _readCount++;
                return _lastReadback ?? throw new InvalidOperationException("No pulse readbacks configured.");
            }
        }

        public async Task WaitForReadCountAsync(int expectedReadCount, TimeSpan timeout)
        {
            var startedAt = DateTimeOffset.UtcNow;
            while (true)
            {
                lock (_syncRoot)
                {
                    if (_readCount >= expectedReadCount)
                    {
                        return;
                    }
                }

                if (DateTimeOffset.UtcNow - startedAt > timeout)
                {
                    throw new TimeoutException($"Timed out waiting for {expectedReadCount} pulse reads.");
                }

                await Task.Delay(25);
            }
        }
    }

    private sealed class FakeControlCenterConnection : IControlCenterConnection
    {
        public FakeControlCenterConnection(ControlCenterDeviceInfo device)
        {
            Device = device;
        }

        public ControlCenterDeviceInfo Device { get; }

        public void Dispose()
        {
        }
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
