using System;
using System.Collections.Generic;
using System.Linq;

namespace ExperimentalControlPlatform.Runtime;

public abstract class Pt104RuntimeSourceBase : IPt104RuntimeSource, IDisposable
{
    private static readonly int[] ChannelNumbers = [1, 2, 3, 4];
    private readonly SnapshotOutputPort<Pt104SessionState> _state;
    private readonly SnapshotOutputPort<DeviceDiagnosticsSnapshot> _diagnostics;
    private readonly SnapshotOutputPort<Pt104Reading?> _latestReading;
    private readonly StreamOutputPort<Pt104Reading> _readings;

    protected Pt104RuntimeSourceBase(string deviceId, string deviceName, string sourceLabel)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deviceId);
        ArgumentException.ThrowIfNullOrWhiteSpace(deviceName);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceLabel);

        DeviceId = deviceId;
        DeviceName = deviceName;
        SourceLabel = sourceLabel;
        _state = new SnapshotOutputPort<Pt104SessionState>(new Pt104SessionState
        {
            DeviceId = deviceId,
            DeviceName = deviceName,
            Connected = true,
            Busy = false,
            StatusMessage = $"{deviceName} ready.",
            Channels = CreateDefaultChannels()
        });
        _diagnostics = new SnapshotOutputPort<DeviceDiagnosticsSnapshot>(new DeviceDiagnosticsSnapshot
        {
            LastStateTransition = $"Initialized {sourceLabel} PT-104 runtime source."
        });
        _latestReading = new SnapshotOutputPort<Pt104Reading?>();
        _readings = new StreamOutputPort<Pt104Reading>();
    }

    public ISnapshotOutputPort<Pt104SessionState> State => _state;

    public ISnapshotOutputPort<DeviceDiagnosticsSnapshot> Diagnostics => _diagnostics;

    public ISnapshotOutputPort<Pt104Reading?> LatestReading => _latestReading;

    public IStreamOutputPort<Pt104Reading> Readings => _readings;

    protected string DeviceId { get; }

    protected string DeviceName { get; }

    protected string SourceLabel { get; }

    protected void PublishReading(Pt104Reading reading)
    {
        ArgumentNullException.ThrowIfNull(reading);

        if (!State.Current!.Channels.TryGetValue(reading.Channel, out var existingChannel))
        {
            throw new InvalidOperationException($"PT-104 source channel {reading.Channel} is not declared.");
        }

        var channels = State.Current!.Channels.ToDictionary(static pair => pair.Key, static pair => pair.Value);
        channels[reading.Channel] = existingChannel with
        {
            Available = true,
            LiveReading = true,
            LastSampleTimestamp = reading.CapturedAt,
            LastSampleValue = reading.ValueCelsius,
            LastSourceMode = reading.SourceMode
        };

        _state.Publish(State.Current! with
        {
            Busy = false,
            StatusMessage = $"{DeviceName} published {SourceLabel.ToLowerInvariant()} data.",
            Channels = channels
        });
        _diagnostics.Publish(Diagnostics.Current! with
        {
            LastCommand = $"Publish {SourceLabel} PT-104 reading",
            LastHardwareResponse = $"{reading.SourceMode} {reading.ValueCelsius:F3} C on channel {reading.Channel}.",
            LastStateTransition = $"Published {SourceLabel.ToLowerInvariant()} PT-104 sample.",
            LastError = null
        });
        _latestReading.Publish(reading);
        _readings.Publish(reading);
    }

    public virtual void Dispose()
    {
    }

    private static IReadOnlyDictionary<int, Pt104ChannelState> CreateDefaultChannels()
    {
        return ChannelNumbers.ToDictionary(
            static channel => channel,
            static channel => new Pt104ChannelState(
                channel,
                false,
                Pt104MeasurementMode.Pt100,
                4,
                50,
                true,
                false,
                null,
                null,
                null));
    }
}
