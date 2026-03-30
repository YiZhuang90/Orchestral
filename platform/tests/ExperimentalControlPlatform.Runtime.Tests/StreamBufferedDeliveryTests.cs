using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace ExperimentalControlPlatform.Runtime.Tests;

public sealed class StreamBufferedDeliveryTests
{
    [Fact]
    public async Task OrderedBufferedDelivery_PreservesSequence()
    {
        var port = new StreamOutputPort<int>();
        var received = new List<int>();
        using var subscription = port.Subscribe(
            StreamDeliveryPolicy.Ordered(),
            value =>
            {
                received.Add(value);
                return ValueTask.CompletedTask;
            });

        port.Publish(1);
        port.Publish(2);
        port.Publish(3);

        await WaitForConditionAsync(() => received.Count == 3);

        Assert.Equal([1, 2, 3], received);
        Assert.Equal(3, subscription.DeliveredItemCount);
        Assert.Equal(0, subscription.DroppedItemCount);
    }

    [Fact]
    public async Task LatestOnlyBufferedDelivery_CoalescesBurstBehindSlowConsumer()
    {
        var port = new StreamOutputPort<int>();
        var received = new List<int>();
        var firstDeliveryStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseFirstDelivery = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var subscription = port.Subscribe(
            StreamDeliveryPolicy.LatestOnly(),
            async value =>
            {
                received.Add(value);
                if (value == 1)
                {
                    firstDeliveryStarted.TrySetResult();
                    await releaseFirstDelivery.Task;
                }
            });

        port.Publish(1);
        await firstDeliveryStarted.Task;

        port.Publish(2);
        port.Publish(3);
        releaseFirstDelivery.TrySetResult();

        await WaitForConditionAsync(() => received.Count == 2);

        Assert.Equal([1, 3], received);
        Assert.Equal(1, subscription.DroppedItemCount);
    }

    [Fact]
    public async Task OrderedBufferedDelivery_DropsOldestWhenBoundedQueueOverflows()
    {
        var port = new StreamOutputPort<int>();
        var received = new List<int>();
        var firstDeliveryStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseFirstDelivery = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var subscription = port.Subscribe(
            StreamDeliveryPolicy.Ordered(maxBufferedItems: 2, overflowPolicy: StreamOverflowPolicy.DropOldest),
            async value =>
            {
                received.Add(value);
                if (value == 1)
                {
                    firstDeliveryStarted.TrySetResult();
                    await releaseFirstDelivery.Task;
                }
            });

        port.Publish(1);
        await firstDeliveryStarted.Task;

        port.Publish(2);
        port.Publish(3);
        port.Publish(4);
        releaseFirstDelivery.TrySetResult();

        await WaitForConditionAsync(() => received.Count == 3);

        Assert.Equal([1, 3, 4], received);
        Assert.Equal(1, subscription.DroppedItemCount);
    }

    [Fact]
    public async Task Dispose_ReturnsPromptly_WhenConsumerIsStillBusy()
    {
        var port = new StreamOutputPort<int>();
        var firstDeliveryStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseFirstDelivery = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var subscription = port.Subscribe(
            StreamDeliveryPolicy.LatestOnly(),
            async value =>
            {
                firstDeliveryStarted.TrySetResult();
                await releaseFirstDelivery.Task;
            });

        port.Publish(1);
        await firstDeliveryStarted.Task;

        var disposeTask = Task.Run(subscription.Dispose);
        await disposeTask.WaitAsync(TimeSpan.FromSeconds(1));

        releaseFirstDelivery.TrySetResult();

        Assert.True(disposeTask.IsCompletedSuccessfully);
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
}
