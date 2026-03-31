using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ExperimentalControlPlatform.App.DevicePanels;
using ExperimentalControlPlatform.App.DevicePanels.Contracts;
using ExperimentalControlPlatform.App.DevicePanels.ControlCenter;
using ExperimentalControlPlatform.Devices.ControlCenter;
using ExperimentalControlPlatform.Runtime;
using Xunit;

namespace ExperimentalControlPlatform.App.Tests;

public sealed class ControlCenterPanelViewModelTests
{
    private static readonly ControlCenterDeviceInfo TestDevice = ControlCenterDeviceInfo.FromPortName("COM9");

    [Fact]
    public async Task ConnectedState_ExposesApplyAndExitLifecycleAction()
    {
        var service = new FakeControlCenterService();
        var registry = new DeviceSessionRegistry();
        var viewModel = new ControlCenterPanelViewModel(service, registry);

        await viewModel.RefreshDevicesAsync();
        await viewModel.ConnectAsync();

        Assert.Contains(IntegrationPanelLifecycleAction.ApplyAndExit, viewModel.SupportedLifecycleActions);
    }

    [Fact]
    public async Task ApplyAndExitAsync_StagesSettings_LeavesDeviceIdle_AndDisposesSession()
    {
        var service = new FakeControlCenterService();
        var registry = new DeviceSessionRegistry();
        var viewModel = new ControlCenterPanelViewModel(service, registry);
        var closeAware = (IPanelCloseViewModel)viewModel;
        var closeRequested = false;
        closeAware.CloseRequested += (_, _) => closeRequested = true;

        await viewModel.RefreshDevicesAsync();
        await viewModel.ConnectAsync();
        viewModel.SelectedLaserStateItem = new ControlStateOption("On", true);
        viewModel.SelectedPuffStateItem = new ControlStateOption("On", true);
        viewModel.StepCountInputDraft = "8";

        await viewModel.ApplyAndExitAsync();

        Assert.False(viewModel.IsConnected);
        Assert.Empty(registry.Sessions);
        Assert.Equal(new ControlCenterCommand(false, false, 0), service.SentCommands.Last());
        var appliedSettings = Assert.IsType<IntegrationPanelAppliedSettingsOutput>(viewModel.AppliedSettingsOutput);
        var appliedEndpointSettings = Assert.IsAssignableFrom<IReadOnlyDictionary<string, string?>>(appliedSettings.EndpointSettings);
        Assert.Equal("0", appliedEndpointSettings["puff.step_count"]);
        var appliedNotes = Assert.IsAssignableFrom<IReadOnlyList<string>>(appliedSettings.NormalizationNotes);
        Assert.Contains(
            appliedNotes,
            note => note.Contains("StepCount=8", StringComparison.Ordinal));
        var sessionEnd = Assert.IsType<IntegrationPanelSessionEndOutput>(viewModel.SessionEndOutput);
        Assert.Equal("Applied settings were staged and the control center returned to idle wait state.", sessionEnd.ExitReason);
        var sessionEndSnapshot = Assert.IsType<IntegrationPanelAppliedSettingsOutput>(sessionEnd.AppliedSettingsSnapshot);
        var sessionEndEndpointSettings = Assert.IsAssignableFrom<IReadOnlyDictionary<string, string?>>(sessionEndSnapshot.EndpointSettings);
        Assert.Equal("0", sessionEndEndpointSettings["puff.step_count"]);
        Assert.True(service.ConnectionDisposed);
        Assert.True(closeRequested);
    }

    [Fact]
    public async Task CanApplyCommand_IsFalse_WhenDraftStepCountIsNegative()
    {
        var service = new FakeControlCenterService();
        var registry = new DeviceSessionRegistry();
        var viewModel = new ControlCenterPanelViewModel(service, registry);

        await viewModel.RefreshDevicesAsync();
        await viewModel.ConnectAsync();
        viewModel.StepCountInputDraft = "-1";

        Assert.False(viewModel.CanApplyCommand);
    }

    [Fact]
    public async Task ApplyCommandAsync_UsesCapabilityScopedEndpointSettings()
    {
        var service = new FakeControlCenterService();
        var registry = new DeviceSessionRegistry();
        var viewModel = new ControlCenterPanelViewModel(service, registry);

        await viewModel.RefreshDevicesAsync();
        await viewModel.ConnectAsync();
        viewModel.SelectedLaserStateItem = new ControlStateOption("On", true);
        viewModel.SelectedPuffStateItem = new ControlStateOption("On", true);
        viewModel.StepCountInputDraft = "12";

        await viewModel.ApplyCommandAsync();

        var appliedSettings = Assert.IsType<IntegrationPanelAppliedSettingsOutput>(viewModel.AppliedSettingsOutput);
        var endpointSettings = Assert.IsAssignableFrom<IReadOnlyDictionary<string, string?>>(appliedSettings.EndpointSettings);
        Assert.Equal("true", endpointSettings["laser.enabled"]);
        Assert.Equal("true", endpointSettings["puff.enabled"]);
        Assert.Equal("12", endpointSettings["puff.step_count"]);
    }

    private sealed class FakeControlCenterService : IControlCenterService
    {
        private readonly FakeConnection _connection = new(TestDevice);

        public List<ControlCenterCommand> SentCommands { get; } = [];

        public bool ConnectionDisposed => _connection.Disposed;

        public IReadOnlyList<ControlCenterDeviceInfo> ListDevices() => [TestDevice];

        public IControlCenterConnection Open(ControlCenterDeviceInfo device) => _connection;

        public void SendCommand(IControlCenterConnection connection, ControlCenterCommand command)
        {
            SentCommands.Add(command);
        }

        public ControlCenterPulseReadback ReadPulseCount(IControlCenterConnection connection)
        {
            return new ControlCenterPulseReadback(TestDevice.DeviceId, TestDevice.DisplayName, 1.25, 77, DateTimeOffset.UtcNow);
        }

        private sealed class FakeConnection(ControlCenterDeviceInfo device) : IControlCenterConnection
        {
            public ControlCenterDeviceInfo Device { get; } = device;

            public bool Disposed { get; private set; }

            public void Dispose()
            {
                Disposed = true;
            }
        }
    }
}
