using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace ExperimentalControlPlatform.Runtime;

public sealed class StreamOutputPort<T> : IStreamOutputPort<T>
{
    private readonly object _syncRoot = new();
    private Action<T>? _produced;
    private List<BufferedSubscription>? _bufferedSubscriptions;
    private BufferedSubscription[] _bufferedSubscriptionSnapshot = [];

    public event Action<T>? Produced
    {
        add
        {
            lock (_syncRoot)
            {
                _produced += value;
            }
        }
        remove
        {
            lock (_syncRoot)
            {
                _produced -= value;
            }
        }
    }

    public IStreamDeliverySubscription Subscribe(StreamDeliveryPolicy policy, Func<T, ValueTask> onItem)
    {
        ArgumentNullException.ThrowIfNull(policy);
        ArgumentNullException.ThrowIfNull(onItem);
        policy.Validate();

        var subscription = new BufferedSubscription(policy, onItem, RemoveSubscription);
        lock (_syncRoot)
        {
            (_bufferedSubscriptions ??= []).Add(subscription);
            _bufferedSubscriptionSnapshot = _bufferedSubscriptions.ToArray();
        }

        return subscription;
    }

    public void Publish(T value)
    {
        Action<T>? handlers;
        BufferedSubscription[] bufferedSubscriptions;
        lock (_syncRoot)
        {
            handlers = _produced;
            bufferedSubscriptions = _bufferedSubscriptionSnapshot;
        }

        handlers?.Invoke(value);
        foreach (var subscription in bufferedSubscriptions)
        {
            subscription.Enqueue(value);
        }
    }

    private void RemoveSubscription(BufferedSubscription subscription)
    {
        lock (_syncRoot)
        {
            _bufferedSubscriptions?.Remove(subscription);
            if (_bufferedSubscriptions is { Count: 0 })
            {
                _bufferedSubscriptions = null;
            }

            _bufferedSubscriptionSnapshot = _bufferedSubscriptions?.ToArray() ?? [];
        }
    }

    private sealed class BufferedSubscription : IStreamDeliverySubscription
    {
        private readonly object _syncRoot = new();
        private readonly Func<T, ValueTask> _onItem;
        private readonly Action<BufferedSubscription> _disposeCallback;
        private readonly SemaphoreSlim _signal = new(0);
        private readonly CancellationTokenSource _cancellation = new();
        private readonly Task _pumpTask;
        private readonly Queue<T> _queue = new();
        private T? _latestItem;
        private bool _hasLatestItem;
        private bool _disposed;
        private long _deliveredItemCount;
        private long _droppedItemCount;
        private long? _lastDeliveredTimestamp;

        public BufferedSubscription(
            StreamDeliveryPolicy policy,
            Func<T, ValueTask> onItem,
            Action<BufferedSubscription> disposeCallback)
        {
            Policy = policy;
            _onItem = onItem;
            _disposeCallback = disposeCallback;
            _pumpTask = Task.Run(PumpAsync);
            _ = _pumpTask.ContinueWith(
                task => _ = task.Exception,
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
        }

        public StreamDeliveryPolicy Policy { get; }

        public long DeliveredItemCount => Interlocked.Read(ref _deliveredItemCount);

        public long DroppedItemCount => Interlocked.Read(ref _droppedItemCount);

        public void Enqueue(T value)
        {
            var shouldSignal = false;
            lock (_syncRoot)
            {
                if (_disposed)
                {
                    return;
                }

                switch (Policy.Mode)
                {
                    case StreamDeliveryMode.LatestOnly:
                        shouldSignal = !_hasLatestItem;
                        if (_hasLatestItem)
                        {
                            Interlocked.Increment(ref _droppedItemCount);
                        }

                        _latestItem = value;
                        _hasLatestItem = true;
                        break;

                    case StreamDeliveryMode.Ordered:
                        shouldSignal = _queue.Count == 0;
                        if (Policy.MaxBufferedItems.HasValue && _queue.Count >= Policy.MaxBufferedItems.Value)
                        {
                            if (Policy.OverflowPolicy == StreamOverflowPolicy.DropNewest)
                            {
                                Interlocked.Increment(ref _droppedItemCount);
                                return;
                            }

                            _queue.Dequeue();
                            Interlocked.Increment(ref _droppedItemCount);
                        }

                        _queue.Enqueue(value);
                        break;

                    default:
                        throw new InvalidOperationException($"Unsupported delivery mode '{Policy.Mode}'.");
                }
            }

            if (shouldSignal)
            {
                _signal.Release();
            }
        }

        public void Dispose()
        {
            lock (_syncRoot)
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
            }

            _disposeCallback(this);
            _cancellation.Cancel();
            _signal.Release();
        }

        private async Task PumpAsync()
        {
            try
            {
                while (true)
                {
                    await _signal.WaitAsync(_cancellation.Token).ConfigureAwait(false);

                    while (TryDequeue(out var nextItem))
                    {
                        await DelayIfRequiredAsync(_cancellation.Token).ConfigureAwait(false);
                        await _onItem(nextItem).ConfigureAwait(false);
                        Interlocked.Increment(ref _deliveredItemCount);
                        _lastDeliveredTimestamp = Stopwatch.GetTimestamp();
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception)
            {
            }
        }

        private async Task DelayIfRequiredAsync(CancellationToken cancellationToken)
        {
            if (!Policy.MaxDeliveryRateHz.HasValue || !_lastDeliveredTimestamp.HasValue)
            {
                return;
            }

            var minimumInterval = TimeSpan.FromSeconds(1.0 / Policy.MaxDeliveryRateHz.Value);
            var elapsed = Stopwatch.GetElapsedTime(_lastDeliveredTimestamp.Value, Stopwatch.GetTimestamp());
            if (elapsed < minimumInterval)
            {
                await Task.Delay(minimumInterval - elapsed, cancellationToken).ConfigureAwait(false);
            }
        }

        private bool TryDequeue(out T value)
        {
            lock (_syncRoot)
            {
                switch (Policy.Mode)
                {
                    case StreamDeliveryMode.LatestOnly:
                        if (!_hasLatestItem)
                        {
                            value = default!;
                            return false;
                        }

                        value = _latestItem!;
                        _latestItem = default;
                        _hasLatestItem = false;
                        return true;

                    case StreamDeliveryMode.Ordered:
                        if (_queue.Count == 0)
                        {
                            value = default!;
                            return false;
                        }

                        value = _queue.Dequeue();
                        return true;

                    default:
                        throw new InvalidOperationException($"Unsupported delivery mode '{Policy.Mode}'.");
                }
            }
        }
    }
}
