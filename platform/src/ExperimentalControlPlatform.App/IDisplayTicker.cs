using System;

namespace ExperimentalControlPlatform.App;

public interface IDisplayTicker : IDisposable
{
    event Action? Tick;

    TimeSpan Interval { get; set; }

    void Start();

    void Stop();
}
