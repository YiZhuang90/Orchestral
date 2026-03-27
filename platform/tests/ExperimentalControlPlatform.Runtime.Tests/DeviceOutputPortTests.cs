using System.Collections.Generic;
using Xunit;

namespace ExperimentalControlPlatform.Runtime.Tests;

public sealed class DeviceOutputPortTests
{
    [Fact]
    public void SnapshotPort_PublishUpdatesCurrentAndNotifiesSubscribers()
    {
        var port = new SnapshotOutputPort<string>();
        var received = new List<string>();
        port.Changed += received.Add;

        port.Publish("first");
        port.Publish("second");

        Assert.Equal("second", port.Current);
        Assert.Equal(["first", "second"], received);
    }

    [Fact]
    public void StreamPort_PublishNotifiesSubscribers()
    {
        var port = new StreamOutputPort<int>();
        var received = new List<int>();
        port.Produced += received.Add;

        port.Publish(3);
        port.Publish(7);

        Assert.Equal([3, 7], received);
    }
}
