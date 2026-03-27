using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ExperimentalControlPlatform.Runtime.Tests;

public sealed class DeviceSessionRegistryTests
{
    [Fact]
    public void GetOrAdd_ReturnsExistingSessionForSameIdentity()
    {
        var registry = new DeviceSessionRegistry();
        var sessionId = new DeviceSessionId("Microphone", "mic-1");

        var first = registry.GetOrAdd(sessionId, () => new FakeSession(sessionId));
        var second = registry.GetOrAdd(sessionId, () => new FakeSession(sessionId));

        Assert.Same(first, second);
        Assert.Single(registry.Sessions);
    }

    [Fact]
    public async Task StopAllAsync_StopsEveryRegisteredSession()
    {
        var registry = new DeviceSessionRegistry();
        var first = registry.GetOrAdd(new DeviceSessionId("Microphone", "mic-1"), () => new FakeSession(new("Microphone", "mic-1")));
        var second = registry.GetOrAdd(new DeviceSessionId("Camera", "cam-1"), () => new FakeSession(new("Camera", "cam-1")));

        await registry.StopAllAsync(StopReason.UserRequested("Operator requested global stop."));

        Assert.Equal(1, first.StopCallCount);
        Assert.Equal(1, second.StopCallCount);
        Assert.Empty(registry.Sessions);
    }

    private sealed class FakeSession : IDeviceSession
    {
        public FakeSession(DeviceSessionId sessionId)
        {
            SessionId = sessionId;
        }

        public DeviceSessionId SessionId { get; }

        public int StopCallCount { get; private set; }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        public ValueTask StopAsync(StopReason reason, CancellationToken cancellationToken = default)
        {
            StopCallCount++;
            return ValueTask.CompletedTask;
        }
    }
}
