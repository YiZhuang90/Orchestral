using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ExperimentalControlPlatform.Runtime.Tests;

public sealed class Pt104SessionTests
{
    [Fact]
    public async Task ConnectAsync_VerifiesChannelsAndPublishesConnectedState()
    {
        using var driver = new FakePt104Driver();
        var session = new Pt104Session(driver);
        var configuration = new Pt104ChannelConfiguration(4, Pt104MeasurementMode.Pt100, 4, 50, true);

        await session.ConnectAsync(configuration);

        var state = Assert.IsType<Pt104SessionState>(session.State.Current);
        Assert.True(state.Connected);
        Assert.Equal("PT104-USB-1", state.DeviceId);
        Assert.Equal(4, state.Channels.Count);
        Assert.All(state.Channels.Values, channel => Assert.True(channel.Available));
        Assert.Equal(configuration, session.AppliedSettings.Current);
    }

    [Fact]
    public async Task ReadOnceAsync_PublishesLatestReadingAndStream()
    {
        using var driver = new FakePt104Driver();
        var session = new Pt104Session(driver);
        var configuration = new Pt104ChannelConfiguration(2, Pt104MeasurementMode.Pt1000, 3, 60, false);
        var received = new List<Pt104Reading>();
        session.Readings.Produced += received.Add;

        await session.ConnectAsync(configuration);
        await session.ReadOnceAsync(configuration);

        var reading = Assert.IsType<Pt104Reading>(session.LatestReading.Current);
        Assert.Equal(2, reading.Channel);
        Assert.Equal("ReadOnce", reading.SourceMode);
        Assert.Single(received);
        Assert.Equal(reading, received[0]);
    }

    [Fact]
    public async Task StartLiveAsync_ProducesReadingsUntilStopped()
    {
        using var driver = new FakePt104Driver();
        var session = new Pt104Session(driver);
        var configuration = new Pt104ChannelConfiguration(1, Pt104MeasurementMode.Pt100, 4, 50, true);
        var received = new List<Pt104Reading>();
        session.Readings.Produced += received.Add;

        await session.ConnectAsync(configuration);
        await session.StartLiveAsync(1);
        await WaitForConditionAsync(() => received.Count >= 1);
        await session.StopLiveAsync(1);

        var state = Assert.IsType<Pt104SessionState>(session.State.Current);
        Assert.False(state.Channels[1].LiveReading);
        Assert.True(received.Count >= 1);
        Assert.All(received, reading => Assert.Equal(1, reading.Channel));
    }

    [Fact]
    public async Task DisposeAsync_WhenConnected_DisconnectsAndPublishesSessionEnd()
    {
        using var driver = new FakePt104Driver();
        var session = new Pt104Session(driver);
        var configuration = new Pt104ChannelConfiguration(3, Pt104MeasurementMode.Pt100, 4, 50, true);

        await session.ConnectAsync(configuration);
        await session.DisposeAsync();

        var state = Assert.IsType<Pt104SessionState>(session.State.Current);
        Assert.False(state.Connected);
        Assert.False(driver.IsConnected);
        var sessionEnd = Assert.IsType<DeviceSessionEndSnapshot>(session.SessionEnd.Current);
        Assert.Equal("UserRequested", sessionEnd.ReasonCode);
    }

    [Fact]
    public async Task ReadOnceAsync_WhenDisconnected_Throws()
    {
        using var driver = new FakePt104Driver();
        var session = new Pt104Session(driver);
        var configuration = new Pt104ChannelConfiguration(2, Pt104MeasurementMode.Pt100, 4, 50, true);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => session.ReadOnceAsync(configuration));

        Assert.Contains("not connected", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static async Task WaitForConditionAsync(Func<bool> condition)
    {
        var started = DateTime.UtcNow;
        while (!condition())
        {
            if (DateTime.UtcNow - started > TimeSpan.FromSeconds(3))
            {
                throw new TimeoutException("Condition was not reached before timeout.");
            }

            await Task.Delay(20);
        }
    }

    private sealed class FakePt104Driver : IPt104RuntimeDriver
    {
        private Pt104ChannelConfiguration? _lastConfiguration;

        public bool IsConnected { get; private set; }

        public string? ConnectedDeviceId => IsConnected ? "PT104-USB-1" : null;

        public IReadOnlyList<string> EnumerateUsbUnits() => ["PT104-USB-1"];

        public void Connect(Pt104ChannelConfiguration configuration)
        {
            IsConnected = true;
            _lastConfiguration = configuration;
        }

        public void ApplySettings(Pt104ChannelConfiguration configuration)
        {
            _lastConfiguration = configuration;
        }

        public void ConfigureChannel(Pt104ChannelConfiguration configuration)
        {
            _lastConfiguration = configuration;
        }

        public double ReadTemperatureC(bool filtered, int attempts = 10, int delayMilliseconds = 800, bool allowRepeatValue = true)
        {
            var channel = _lastConfiguration?.Channel ?? 1;
            return 20.0 + channel + (filtered ? 0.1 : 0.2);
        }

        public double ReadTemperatureC(int channel, bool filtered, int attempts = 10, int delayMilliseconds = 800, bool allowRepeatValue = true)
        {
            return 20.0 + channel + (filtered ? 0.1 : 0.2);
        }

        public void Disconnect()
        {
            IsConnected = false;
        }

        public void Dispose()
        {
            Disconnect();
        }
    }
}
