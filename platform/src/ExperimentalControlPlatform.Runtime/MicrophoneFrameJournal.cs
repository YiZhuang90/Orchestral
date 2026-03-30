using System;
using System.Threading.Tasks;
using ExperimentalControlPlatform.Devices.Audio;

namespace ExperimentalControlPlatform.Runtime;

public sealed class MicrophoneFrameJournal : IDisposable
{
    private readonly object _syncRoot = new();
    private readonly IStreamDeliverySubscription _subscription;
    private MicrophoneFrame? _lastFrame;
    private long _frameCount;

    public MicrophoneFrameJournal(IStreamOutputPort<MicrophoneFrame> frames)
    {
        ArgumentNullException.ThrowIfNull(frames);
        _subscription = frames.Subscribe(
            StreamDeliveryPolicy.Ordered(),
            frame =>
            {
                OnProduced(frame);
                return ValueTask.CompletedTask;
            });
    }

    public long FrameCount
    {
        get
        {
            lock (_syncRoot)
            {
                return _frameCount;
            }
        }
    }

    public MicrophoneFrame? LastFrame
    {
        get
        {
            lock (_syncRoot)
            {
                return _lastFrame;
            }
        }
    }

    public void Dispose()
    {
        _subscription.Dispose();
    }

    private void OnProduced(MicrophoneFrame frame)
    {
        lock (_syncRoot)
        {
            _frameCount++;
            _lastFrame = frame;
        }
    }
}
