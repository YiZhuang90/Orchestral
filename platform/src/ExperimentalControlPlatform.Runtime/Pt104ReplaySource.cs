using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ExperimentalControlPlatform.Runtime;

public sealed class Pt104ReplaySource : Pt104RuntimeSourceBase
{
    private readonly IReadOnlyList<Pt104Reading> _readings;

    public Pt104ReplaySource(string deviceId, string deviceName, IReadOnlyList<Pt104Reading> readings)
        : base(deviceId, deviceName, "Replay")
    {
        ArgumentNullException.ThrowIfNull(readings);
        _readings = readings;
    }

    public Task ReplayAsync(CancellationToken cancellationToken = default)
    {
        foreach (var reading in _readings)
        {
            cancellationToken.ThrowIfCancellationRequested();
            PublishReading(reading);
        }

        return Task.CompletedTask;
    }
}
