using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ExperimentalControlPlatform.Devices.ControlCenter;
using Xunit;

namespace ExperimentalControlPlatform.Runtime.Tests;

public sealed class ControlCenterSessionTests
{
    private static readonly ControlCenterDeviceInfo TestDevice = ControlCenterDeviceInfo.FromPortName("COM5");

    [Fact]
    public async Task ConnectAsync_OpensTransportAndPublishesConnectedState()
    {
        var service = new FakeControlCenterService();
        var session = new ControlCenterSession(service, TestDevice);

        await session.ConnectAsync();

        var currentState = Assert.IsType<ControlCenterSessionState>(session.State.Current);
        Assert.True(currentState.Connected);
        Assert.False(currentState.Busy);
        Assert.Equal($"Connected to {TestDevice.DisplayName}.", currentState.StatusMessage);
        var diagnostics = Assert.IsType<DeviceDiagnosticsSnapshot>(session.Diagnostics.Current);
        Assert.Equal("Connect control center session", diagnostics.LastCommand);
        Assert.Equal($"Opened {TestDevice.PortName} at {ControlCenterProtocol.DefaultBaudRate} baud.", diagnostics.LastHardwareResponse);
    }

    [Fact]
    public async Task ApplyCommandAsync_SendsCommandAndPublishesAppliedState()
    {
        var service = new FakeControlCenterService();
        var session = new ControlCenterSession(service, TestDevice);
        var command = new ControlCenterCommand(PuffEnabled: true, LaserEnabled: false, StepCount: 42);

        await session.ConnectAsync();
        await session.ApplyCommandAsync(command);

        Assert.Equal(command, Assert.Single(service.SentCommands));
        var applied = Assert.IsType<ControlCenterAppliedState>(session.AppliedState.Current);
        Assert.True(applied.PuffEnabled);
        Assert.False(applied.LaserEnabled);
        Assert.Equal(42, applied.StepCount);
        var currentState = Assert.IsType<ControlCenterSessionState>(session.State.Current);
        Assert.True(currentState.Connected);
        Assert.True(currentState.LastCommandedPuffEnabled);
        Assert.False(currentState.LastCommandedLaserEnabled);
        Assert.Equal(42, currentState.LastStepCount);
        Assert.Equal(1, currentState.CommandSequence);
    }

    [Fact]
    public async Task ReadPulseCountAsync_PublishesSnapshotAndStream()
    {
        var service = new FakeControlCenterService
        {
            PulseReadbacks =
            [
                new ControlCenterPulseReadback(TestDevice.DeviceId, TestDevice.DisplayName, 2.5, 96, DateTimeOffset.UtcNow)
            ]
        };
        var session = new ControlCenterSession(service, TestDevice);
        var received = new List<ControlCenterPulseReadback>();
        session.PulseReads.Produced += received.Add;

        await session.ConnectAsync();
        await session.ReadPulseCountAsync();

        var latest = Assert.IsType<ControlCenterPulseReadback>(session.LatestPulse.Current);
        Assert.Equal(96, latest.PulseCount);
        Assert.Single(received);
        var currentState = Assert.IsType<ControlCenterSessionState>(session.State.Current);
        Assert.Equal(96, currentState.LastPulseCount);
        Assert.NotNull(currentState.LastControllerTimestampSeconds);
        Assert.Equal(2.5, currentState.LastControllerTimestampSeconds!.Value, 3);
        Assert.Equal(1, currentState.PulseSequence);
    }

    [Fact]
    public async Task DisconnectAsync_PublishesSessionEndSnapshot()
    {
        var service = new FakeControlCenterService();
        var session = new ControlCenterSession(service, TestDevice);

        await session.ConnectAsync();
        await session.DisconnectAsync(StopReason.UserRequested("Operator disconnected control center."));

        Assert.True(service.ConnectionDisposed);
        var currentState = Assert.IsType<ControlCenterSessionState>(session.State.Current);
        Assert.False(currentState.Connected);
        var sessionEnd = Assert.IsType<DeviceSessionEndSnapshot>(session.SessionEnd.Current);
        Assert.Equal("UserRequested", sessionEnd.ReasonCode);
        Assert.True(sessionEnd.ConnectionClosed);
    }

    [Fact]
    public async Task DisconnectAsync_CanBeCalledTwice()
    {
        var service = new FakeControlCenterService();
        var session = new ControlCenterSession(service, TestDevice);

        await session.ConnectAsync();
        await session.DisconnectAsync();
        await session.DisconnectAsync();

        var currentState = Assert.IsType<ControlCenterSessionState>(session.State.Current);
        Assert.False(currentState.Connected);
    }

    [Fact]
    public async Task EmergencyStopAsync_SendsSafeCommand_AndLeavesSessionConnected()
    {
        var service = new FakeControlCenterService();
        var session = new ControlCenterSession(service, TestDevice);

        await session.ConnectAsync();
        await session.EmergencyStopAsync();

        Assert.Equal(new ControlCenterCommand(false, false, 0), Assert.Single(service.SentCommands));
        var currentState = Assert.IsType<ControlCenterSessionState>(session.State.Current);
        Assert.True(currentState.Connected);
        Assert.False(currentState.LastCommandedLaserEnabled);
        Assert.False(currentState.LastCommandedPuffEnabled);
        Assert.Equal(0, currentState.LastStepCount);
    }

    [Fact]
    public async Task EmergencyStopAsync_WhenDisconnected_DoesNotThrow_AndPublishesDiagnosticNote()
    {
        var service = new FakeControlCenterService();
        var session = new ControlCenterSession(service, TestDevice);

        await session.EmergencyStopAsync();

        Assert.Empty(service.SentCommands);
        Assert.Equal(
            "Skipped emergency stop because the session is already disconnected.",
            session.Diagnostics.Current!.LastValidationResult);
        Assert.Equal(
            "Emergency stop ignored because the control center is disconnected.",
            session.State.Current!.StatusMessage);
    }

    [Fact]
    public async Task ApplyCommandAsync_WhenTransportFails_PublishesFailureStatus()
    {
        var service = new FakeControlCenterService
        {
            ThrowOnSend = new InvalidOperationException("Serial write failed.")
        };
        var session = new ControlCenterSession(service, TestDevice);

        await session.ConnectAsync();
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => session.ApplyCommandAsync(
            new ControlCenterCommand(PuffEnabled: false, LaserEnabled: true, StepCount: 1)));

        Assert.Contains("Serial write failed", exception.Message, StringComparison.Ordinal);
        var currentState = Assert.IsType<ControlCenterSessionState>(session.State.Current);
        Assert.Contains("failed", currentState.StatusMessage, StringComparison.OrdinalIgnoreCase);
        var diagnostics = Assert.IsType<DeviceDiagnosticsSnapshot>(session.Diagnostics.Current);
        Assert.Equal("Serial write failed.", diagnostics.LastError);
    }

    [Fact]
    public async Task ApplyCommandAsync_WhenDisconnected_Throws()
    {
        var service = new FakeControlCenterService();
        var session = new ControlCenterSession(service, TestDevice);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => session.ApplyCommandAsync(
            new ControlCenterCommand(PuffEnabled: false, LaserEnabled: true, StepCount: 1)));

        Assert.Contains("not connected", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateCommand_ReturnsInvalidResult_WhenStepCountIsNegative()
    {
        var result = ControlCenterSession.ValidateCommand(new ControlCenterCommand(PuffEnabled: false, LaserEnabled: true, StepCount: -1));

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, issue => issue.Field == "StepCount");
    }

    private sealed class FakeControlCenterService : IControlCenterService
    {
        private readonly FakeConnection _connection;
        private int _pulseIndex;

        public FakeControlCenterService()
        {
            _connection = new FakeConnection(TestDevice);
        }

        public List<ControlCenterCommand> SentCommands { get; } = [];

        public IReadOnlyList<ControlCenterPulseReadback> PulseReadbacks { get; init; } = [];

        public Exception? ThrowOnSend { get; init; }

        public bool ConnectionDisposed => _connection.Disposed;

        public IReadOnlyList<ControlCenterDeviceInfo> ListDevices() => [TestDevice];

        public IControlCenterConnection Open(ControlCenterDeviceInfo device) => _connection;

        public void SendCommand(IControlCenterConnection connection, ControlCenterCommand command)
        {
            if (ThrowOnSend is not null)
            {
                throw ThrowOnSend;
            }

            SentCommands.Add(command);
        }

        public ControlCenterPulseReadback ReadPulseCount(IControlCenterConnection connection)
        {
            if (PulseReadbacks.Count == 0)
            {
                return new ControlCenterPulseReadback(TestDevice.DeviceId, TestDevice.DisplayName, 0.5, 80, DateTimeOffset.UtcNow);
            }

            var index = Math.Min(_pulseIndex, PulseReadbacks.Count - 1);
            _pulseIndex++;
            return PulseReadbacks[index];
        }

        private sealed class FakeConnection : IControlCenterConnection
        {
            public FakeConnection(ControlCenterDeviceInfo device)
            {
                Device = device;
            }

            public ControlCenterDeviceInfo Device { get; }

            public bool Disposed { get; private set; }

            public void Dispose()
            {
                Disposed = true;
            }
        }
    }
}
