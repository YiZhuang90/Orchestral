using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ExperimentalControlPlatform.App.DevicePanels;
using ExperimentalControlPlatform.App.DevicePanels.Contracts;
using ExperimentalControlPlatform.App.DevicePanels.Microphone;
using ExperimentalControlPlatform.Devices.Audio;
using ExperimentalControlPlatform.Runtime;
using Xunit;

namespace ExperimentalControlPlatform.App.Tests;

public sealed class IntegratedMicrophonePanelViewModelTests
{
    private static readonly MicrophoneDeviceInfo TestDevice = new(0, "Integrated Microphone", "mic-001", 48000, 1);

    [Fact]
    public async Task ConnectedState_ExposesApplyAndExitLifecycleAction()
    {
        var service = new FakeMicrophoneService();
        var registry = new DeviceSessionRegistry();
        var viewModel = new IntegratedMicrophonePanelViewModel(service, registry);

        await viewModel.RefreshDevicesAsync();
        await viewModel.ConnectAsync();

        Assert.Contains(IntegrationPanelLifecycleAction.ApplyAndExit, viewModel.SupportedLifecycleActions);
    }

    [Fact]
    public async Task ApplyAndExitLifecycleAction_RequestsClose_AndDisposesSession()
    {
        var service = new FakeMicrophoneService();
        var registry = new DeviceSessionRegistry();
        var viewModel = new IntegratedMicrophonePanelViewModel(service, registry);
        var closeAware = (IPanelCloseViewModel)viewModel;
        var closeRequested = false;
        closeAware.CloseRequested += (_, _) => closeRequested = true;

        await viewModel.RefreshDevicesAsync();
        await viewModel.ConnectAsync();

        viewModel.LifecycleActionCommand.Execute(IntegrationPanelLifecycleAction.ApplyAndExit);
        await WaitForAsync(() => closeRequested && !viewModel.IsConnected && registry.Sessions.Count == 0);

        Assert.True(closeRequested);
        Assert.False(viewModel.IsConnected);
        Assert.Empty(registry.Sessions);
    }

    private sealed class FakeMicrophoneService : IMicrophoneService
    {
        public IReadOnlyList<MicrophoneDeviceInfo> ListCaptureDevices() => [TestDevice];

        public MicrophoneFrame CaptureSnapshot(MicrophoneCaptureSettings settings)
        {
            return new MicrophoneFrame(
                TestDevice.DeviceId,
                TestDevice.DisplayName,
                48000,
                1,
                MicrophoneChannelMode.MonoMix,
                [0.1f, -0.1f, 0.05f],
                -24.0,
                -12.0,
                false,
                123,
                TimeSpan.FromMilliseconds(50));
        }

        public Task StreamFramesAsync(
            MicrophoneCaptureSettings settings,
            Func<MicrophoneFrame, Task> onFrame,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private static async Task WaitForAsync(Func<bool> condition)
    {
        for (var attempt = 0; attempt < 50; attempt++)
        {
            if (condition())
            {
                return;
            }

            await Task.Delay(10);
        }

        Assert.True(condition(), "Condition was not reached before timeout.");
    }
}
