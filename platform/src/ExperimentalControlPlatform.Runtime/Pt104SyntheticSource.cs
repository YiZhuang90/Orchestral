using System;
using System.Collections.Generic;

namespace ExperimentalControlPlatform.Runtime;

public sealed class Pt104SyntheticSource : Pt104RuntimeSourceBase
{
    private readonly IReadOnlyList<int> _channels;
    private readonly double _meanTemperatureC;
    private readonly double _noiseAmplitudeC;
    private readonly Random _random;

    public Pt104SyntheticSource(
        string deviceId,
        string deviceName,
        IReadOnlyList<int> channels,
        double meanTemperatureC,
        double noiseAmplitudeC,
        int randomSeed = 0)
        : base(deviceId, deviceName, "Synthetic")
    {
        ArgumentNullException.ThrowIfNull(channels);
        if (channels.Count == 0)
        {
            throw new ArgumentException("At least one PT-104 channel is required.", nameof(channels));
        }

        _channels = channels;
        _meanTemperatureC = meanTemperatureC;
        _noiseAmplitudeC = Math.Abs(noiseAmplitudeC);
        _random = new Random(randomSeed);
    }

    public void PublishSampleSet(DateTimeOffset capturedAt)
    {
        foreach (var channel in _channels)
        {
            PublishReading(new Pt104Reading(
                DeviceId,
                channel,
                _meanTemperatureC + NextNoise(),
                capturedAt,
                "Synthetic"));
        }
    }

    private double NextNoise()
    {
        return (_random.NextDouble() * 2d - 1d) * _noiseAmplitudeC;
    }
}
