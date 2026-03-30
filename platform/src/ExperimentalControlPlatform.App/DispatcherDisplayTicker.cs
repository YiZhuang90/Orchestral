using System;
using System.Windows.Threading;

namespace ExperimentalControlPlatform.App;

public sealed class DispatcherDisplayTicker : IDisplayTicker
{
    private readonly DispatcherTimer _timer;

    public DispatcherDisplayTicker(TimeSpan interval)
    {
        _timer = new DispatcherTimer
        {
            Interval = interval
        };
        _timer.Tick += HandleTick;
    }

    public event Action? Tick;

    public TimeSpan Interval
    {
        get => _timer.Interval;
        set => _timer.Interval = value;
    }

    public void Start()
    {
        _timer.Start();
    }

    public void Stop()
    {
        _timer.Stop();
    }

    public void Dispose()
    {
        _timer.Stop();
        _timer.Tick -= HandleTick;
    }

    private void HandleTick(object? sender, EventArgs e)
    {
        Tick?.Invoke();
    }
}
