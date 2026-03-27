using System;
using ExperimentalControlPlatform.Devices.Audio;

namespace ExperimentalControlPlatform.Runtime;

public sealed class MicrophoneFrameJournal : IDisposable
{
    private readonly object _syncRoot = new();
    private readonly IStreamOutputPort<MicrophoneFrame> _frames;
    private MicrophoneFrame? _lastFrame;
    private long _frameCount;

    public MicrophoneFrameJournal(IStreamOutputPort<MicrophoneFrame> frames)
    {
        _frames = frames ?? throw new ArgumentNullException(nameof(frames));
        _frames.Produced += OnProduced;
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
        _frames.Produced -= OnProduced;
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
