using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ExperimentalControlPlatform.Core.Artifacts;
using ExperimentalControlPlatform.Devices.ControlCenter;

namespace ExperimentalControlPlatform.Runtime;

public sealed class FlowReynoldsDerivedStateSession : IAsyncDisposable
{
    private static readonly double[] DensityCoefficients = [-3.983035, 301.797, 522528.90, 69.34881, 999.97495];
    private static readonly double[] DynamicViscosityCoefficients = [-3.7188, 578.919, -137.546];
    private static readonly TimeSpan DefaultSourceStaleThreshold = TimeSpan.FromSeconds(3);
    private readonly object _syncRoot = new();
    private readonly Func<DateTimeOffset> _clock;
    private readonly TimeSpan _sourceStaleThreshold;
    private readonly double _pulsesPerLiter;
    private readonly double _pipeInnerDiameterMeters;
    private readonly double _pipeLengthMeters;
    private readonly double _pipeRoughnessMeters;
    private readonly double _referenceTemperatureC;
    private readonly int _flowrateAverageCount;
    private readonly TimeSpan _pulsePollInterval;
    private readonly SnapshotOutputPort<FlowReynoldsDerivedStateSnapshot> _state = new(new FlowReynoldsDerivedStateSnapshot());
    private readonly SnapshotOutputPort<DeviceDiagnosticsSnapshot> _diagnostics = new(new DeviceDiagnosticsSnapshot());
    private readonly StreamOutputPort<FlowReynoldsDerivedStateSample> _samples = new();
    private readonly List<FlowReynoldsDerivedStateSample> _recordedSamples = [];
    private readonly Queue<double> _recentRawFlowRates = [];
    private readonly Dictionary<int, Pt104Reading> _temperatureChannels = [];
    private ControlCenterPulseReadback? _previousPulseReadback;
    private CancellationTokenSource? _pulsePollingCancellation;
    private Task? _pulsePollingTask;
    private ControlCenterSession? _controlCenterSession;
    private Pt104Session? _pt104Session;
    private bool _disposed;

    public FlowReynoldsDerivedStateSession(
        ResolvedExperimentDefinition experiment,
        Func<DateTimeOffset>? clock = null,
        TimeSpan? sourceStaleThreshold = null)
    {
        ArgumentNullException.ThrowIfNull(experiment);

        _clock = clock ?? (() => DateTimeOffset.UtcNow);
        _sourceStaleThreshold = sourceStaleThreshold ?? DefaultSourceStaleThreshold;
        _pulsesPerLiter = ReadPositiveDouble(experiment, FlowReynoldsArtifactIds.PulsesPerLiterParameterId);
        _pipeInnerDiameterMeters = ReadPositiveDouble(experiment, FlowReynoldsArtifactIds.PipeInnerDiameterParameterId);
        _pipeLengthMeters = ReadPositiveDouble(experiment, FlowReynoldsArtifactIds.PipeLengthParameterId);
        _pipeRoughnessMeters = ReadNonNegativeDouble(experiment, FlowReynoldsArtifactIds.PipeRoughnessParameterId);
        _referenceTemperatureC = ReadFiniteDouble(experiment, FlowReynoldsArtifactIds.ReferenceTemperatureParameterId);
        _flowrateAverageCount = ReadPositiveInt(experiment, FlowReynoldsArtifactIds.FlowrateAverageCountParameterId);
        _pulsePollInterval = TimeSpan.FromMilliseconds(ReadPositiveInt(experiment, FlowReynoldsArtifactIds.PulsePollIntervalMillisecondsParameterId));

        PublishDiagnostics(new DeviceDiagnosticsSnapshot
        {
            LastValidationResult = "Validated flow/Reynolds derived-state configuration.",
            LastStateTransition = "Initialized flow/Reynolds derived-state session."
        });
        PublishState(State.Current! with
        {
            StatusMessage = "Awaiting pulse telemetry."
        });
    }

    public ISnapshotOutputPort<FlowReynoldsDerivedStateSnapshot> State => _state;

    public ISnapshotOutputPort<DeviceDiagnosticsSnapshot> Diagnostics => _diagnostics;

    public IStreamOutputPort<FlowReynoldsDerivedStateSample> Samples => _samples;

    public IReadOnlyList<FlowReynoldsDerivedStateSample> RecordedSamples
    {
        get
        {
            lock (_syncRoot)
            {
                return _recordedSamples.ToArray();
            }
        }
    }

    public void RecordTemperatureSample(Pt104Reading reading)
    {
        lock (_syncRoot)
        {
            ThrowIfDisposed();
            _temperatureChannels[reading.Channel] = reading;
            PublishState(BuildTemperatureOnlySnapshot(State.Current!, reading.CapturedAt));
        }
    }

    public void RecordPulseReadback(ControlCenterPulseReadback readback)
    {
        lock (_syncRoot)
        {
            ThrowIfDisposed();

            PublishDiagnostics(Diagnostics.Current! with
            {
                LastCommand = "Process pulse telemetry",
                LastError = null
            });

            if (_previousPulseReadback is null)
            {
                _previousPulseReadback = readback;
                PublishState(State.Current! with
                {
                    PulseObservedAtUtc = readback.ReceivedAt,
                    PulseTimestampSeconds = readback.ControllerTimestampSeconds,
                    LatestPulseCount = readback.PulseCount,
                    StatusMessage = "Captured initial pulse baseline; awaiting next pulse interval."
                });
                return;
            }

            var deltaTimeSeconds = readback.ControllerTimestampSeconds - _previousPulseReadback.ControllerTimestampSeconds;
            var deltaPulseCount = readback.PulseCount - _previousPulseReadback.PulseCount;
            if (deltaTimeSeconds <= 0 || deltaPulseCount < 0)
            {
                PublishDiagnostics(Diagnostics.Current! with
                {
                    LastError = "Pulse telemetry must advance monotonically."
                });
                _previousPulseReadback = readback;
                PublishState(State.Current! with
                {
                    PulseObservedAtUtc = readback.ReceivedAt,
                    PulseTimestampSeconds = readback.ControllerTimestampSeconds,
                    LatestPulseCount = readback.PulseCount,
                    StatusMessage = "Pulse telemetry advanced out of order; sample ignored."
                });
                return;
            }

            var rawFlowRate = deltaPulseCount / _pulsesPerLiter * 60d / deltaTimeSeconds;
            _recentRawFlowRates.Enqueue(rawFlowRate);
            while (_recentRawFlowRates.Count > _flowrateAverageCount)
            {
                _recentRawFlowRates.Dequeue();
            }

            var filteredFlowRate = _recentRawFlowRates.Average();
            var temperatureSelection = SelectTemperature();
            var observedAtUtc = readback.ReceivedAt;
            var computation = ComputeHydraulicState(filteredFlowRate, temperatureSelection.MeanTemperatureC);
            var pulseIsStale = false;
            var temperatureIsStale = temperatureSelection.ObservedAtUtc.HasValue
                && observedAtUtc - temperatureSelection.ObservedAtUtc.Value > _sourceStaleThreshold;

            var sample = new FlowReynoldsDerivedStateSample(
                observedAtUtc,
                rawFlowRate,
                filteredFlowRate,
                temperatureSelection.MeanTemperatureC,
                temperatureSelection.TemperatureDeltaC,
                computation.BulkVelocityMetersPerSecond,
                computation.ReynoldsNumber,
                temperatureSelection.UsesFallbackTemperature,
                pulseIsStale,
                temperatureIsStale);

            _recordedSamples.Add(sample);
            _previousPulseReadback = readback;

            PublishDiagnostics(Diagnostics.Current! with
            {
                LastHardwareResponse = $"Derived Reynolds number {sample.ReynoldsNumber:0.###}.",
                LastStateTransition = "Published flow/Reynolds derived-state sample.",
                LastError = null
            });
            PublishState(new FlowReynoldsDerivedStateSnapshot
            {
                ObservedAtUtc = observedAtUtc,
                PulseObservedAtUtc = readback.ReceivedAt,
                PulseTimestampSeconds = readback.ControllerTimestampSeconds,
                LatestPulseCount = readback.PulseCount,
                TemperatureObservedAtUtc = temperatureSelection.ObservedAtUtc,
                RawFlowRateLitersPerMinute = rawFlowRate,
                FilteredFlowRateLitersPerMinute = filteredFlowRate,
                MeanTemperatureC = temperatureSelection.MeanTemperatureC,
                TemperatureDeltaC = temperatureSelection.TemperatureDeltaC,
                BulkVelocityMetersPerSecond = computation.BulkVelocityMetersPerSecond,
                ReynoldsNumber = computation.ReynoldsNumber,
                UsesFallbackTemperature = temperatureSelection.UsesFallbackTemperature,
                PulseTelemetryIsStale = pulseIsStale,
                TemperatureIsStale = temperatureIsStale,
                SampleSequence = _recordedSamples.Count,
                StatusMessage = BuildStatusMessage(sample)
            });
            SamplesPort.Publish(sample);
        }
    }

    public async Task AttachRuntimeSourcesAsync(
        ControlCenterSession? controlCenterSession,
        Pt104Session? pt104Session,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (ReferenceEquals(_controlCenterSession, controlCenterSession) && ReferenceEquals(_pt104Session, pt104Session))
        {
            return;
        }

        await DetachRuntimeSourcesAsync().ConfigureAwait(false);

        if (pt104Session is not null)
        {
            _pt104Session = pt104Session;
            _pt104Session.Readings.Produced += HandlePt104ReadingProduced;
            SeedTemperatureChannels(pt104Session.State.Current?.Channels);
        }

        if (controlCenterSession is not null)
        {
            _controlCenterSession = controlCenterSession;
            _controlCenterSession.PulseReads.Produced += HandlePulseReadProduced;
            _pulsePollingCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _pulsePollingTask = Task.Run(() => RunPulsePollingLoopAsync(controlCenterSession, _pulsePollingCancellation.Token), CancellationToken.None);
        }
    }

    public async ValueTask StopAsync(StopReason reason, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(reason);
        await DetachRuntimeSourcesAsync(cancellationToken).ConfigureAwait(false);
        PublishDiagnostics(Diagnostics.Current! with
        {
            LastStateTransition = $"Stopped flow/Reynolds derived-state session: {reason.Code}.",
            LastError = null
        });
        PublishState(State.Current! with
        {
            StatusMessage = $"Flow/Reynolds derived-state stopped: {reason.Code}."
        });
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        await StopAsync(StopReason.UserRequested("Disposed flow/Reynolds derived-state session.")).ConfigureAwait(false);
    }

    private async Task DetachRuntimeSourcesAsync(CancellationToken cancellationToken = default)
    {
        ControlCenterSession? controlCenterSession;
        Pt104Session? pt104Session;
        CancellationTokenSource? pulsePollingCancellation;
        Task? pulsePollingTask;

        lock (_syncRoot)
        {
            controlCenterSession = _controlCenterSession;
            pt104Session = _pt104Session;
            pulsePollingCancellation = _pulsePollingCancellation;
            pulsePollingTask = _pulsePollingTask;
            _controlCenterSession = null;
            _pt104Session = null;
            _pulsePollingCancellation = null;
            _pulsePollingTask = null;
        }

        if (controlCenterSession is not null)
        {
            controlCenterSession.PulseReads.Produced -= HandlePulseReadProduced;
        }

        if (pt104Session is not null)
        {
            pt104Session.Readings.Produced -= HandlePt104ReadingProduced;
        }

        if (pulsePollingCancellation is not null)
        {
            pulsePollingCancellation.Cancel();
        }

        if (pulsePollingTask is not null)
        {
            try
            {
                await pulsePollingTask.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
        }

        pulsePollingCancellation?.Dispose();
    }

    private void HandlePulseReadProduced(ControlCenterPulseReadback readback)
    {
        RecordPulseReadback(readback);
    }

    private void HandlePt104ReadingProduced(Pt104Reading reading)
    {
        RecordTemperatureSample(reading);
    }

    private async Task RunPulsePollingLoopAsync(ControlCenterSession controlCenterSession, CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await controlCenterSession.ReadPulseCountAsync(cancellationToken).ConfigureAwait(false);
                    PublishDiagnostics(Diagnostics.Current! with
                    {
                        LastCommand = "Poll control-center pulse telemetry",
                        LastValidationResult = $"Polling every {_pulsePollInterval.TotalMilliseconds:0} ms.",
                        LastError = null
                    });
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    PublishDiagnostics(Diagnostics.Current! with
                    {
                        LastCommand = "Poll control-center pulse telemetry",
                        LastError = ex.Message
                    });
                }

                await Task.Delay(_pulsePollInterval, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private void SeedTemperatureChannels(IReadOnlyDictionary<int, Pt104ChannelState>? channels)
    {
        if (channels is null)
        {
            return;
        }

        foreach (var channel in channels.Values.Where(static channel => channel.LastSampleValue.HasValue && channel.LastSampleTimestamp.HasValue))
        {
            RecordTemperatureSample(new Pt104Reading(
                "pt104",
                channel.Channel,
                channel.LastSampleValue!.Value,
                channel.LastSampleTimestamp!.Value,
                channel.LastSourceMode ?? "Seed"));
        }
    }

    private FlowReynoldsDerivedStateSnapshot BuildTemperatureOnlySnapshot(
        FlowReynoldsDerivedStateSnapshot current,
        DateTimeOffset observedAtUtc)
    {
        var selection = SelectTemperature();
        return current with
        {
            TemperatureObservedAtUtc = selection.ObservedAtUtc,
            MeanTemperatureC = selection.MeanTemperatureC,
            TemperatureDeltaC = selection.TemperatureDeltaC,
            UsesFallbackTemperature = selection.UsesFallbackTemperature,
            TemperatureIsStale = selection.ObservedAtUtc.HasValue && observedAtUtc - selection.ObservedAtUtc.Value > _sourceStaleThreshold,
            StatusMessage = current.RawFlowRateLitersPerMinute.HasValue
                ? current.StatusMessage
                : "Updated temperature state; awaiting pulse telemetry."
        };
    }

    private (double MeanTemperatureC, double? TemperatureDeltaC, DateTimeOffset? ObservedAtUtc, bool UsesFallbackTemperature) SelectTemperature()
    {
        if (_temperatureChannels.Count == 0)
        {
            return (_referenceTemperatureC, null, null, true);
        }

        var values = _temperatureChannels.Values.OrderBy(static reading => reading.Channel).ToArray();
        var mean = values.Average(static reading => reading.ValueCelsius);
        var delta = values.Length > 1
            ? (double?)(values.Max(static reading => reading.ValueCelsius) - values.Min(static reading => reading.ValueCelsius))
            : null;
        var observedAtUtc = values.Max(static reading => reading.CapturedAt);
        return (mean, delta, observedAtUtc, false);
    }

    private (double BulkVelocityMetersPerSecond, double ReynoldsNumber) ComputeHydraulicState(
        double filteredFlowRateLitersPerMinute,
        double meanTemperatureC)
    {
        var density = (1 - Math.Pow(meanTemperatureC + DensityCoefficients[0], 2) * (meanTemperatureC + DensityCoefficients[1]) / (meanTemperatureC + DensityCoefficients[3]) / DensityCoefficients[2]) * DensityCoefficients[4];
        var dynamicViscosity = Math.Exp(DynamicViscosityCoefficients[0] + DynamicViscosityCoefficients[1] / (DynamicViscosityCoefficients[2] + meanTemperatureC + 273.15)) / 1000;
        var kinematicViscosity = dynamicViscosity / density;
        var bulkVelocity = filteredFlowRateLitersPerMinute / 60 / 1000 / Math.PI / Math.Pow(_pipeInnerDiameterMeters, 2) * 4;
        var reynoldsNumber = bulkVelocity * _pipeInnerDiameterMeters / kinematicViscosity;
        // Pipe length and roughness are retained in the experiment package for the next
        // pressure-drop/friction-factor slice; V1 only derives bulk velocity and Reynolds number.
        _ = _pipeLengthMeters;
        _ = _pipeRoughnessMeters;
        return (bulkVelocity, reynoldsNumber);
    }

    private static string BuildStatusMessage(FlowReynoldsDerivedStateSample sample)
    {
        var temperatureSuffix = sample.UsesFallbackTemperature ? " (fallback temperature)" : string.Empty;
        var staleSuffix = sample.PulseTelemetryIsStale || sample.TemperatureIsStale ? " (stale)" : string.Empty;
        return $"Derived Re {sample.ReynoldsNumber:0.###} at {sample.FilteredFlowRateLitersPerMinute:0.###} L/min and {sample.MeanTemperatureC:0.###} C{temperatureSuffix}{staleSuffix}.";
    }

    private static double ReadFiniteDouble(ResolvedExperimentDefinition experiment, ArtifactId parameterId)
    {
        if (!experiment.TryGetParameterValue(parameterId, out var value) || value is null)
        {
            throw new ArgumentException($"Missing required flow/Reynolds parameter '{parameterId}'.", nameof(experiment));
        }

        if (!double.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var parsed)
            || double.IsNaN(parsed)
            || double.IsInfinity(parsed))
        {
            throw new ArgumentException($"Parameter '{parameterId}' must contain a finite numeric value.", nameof(experiment));
        }

        return parsed;
    }

    private static double ReadPositiveDouble(ResolvedExperimentDefinition experiment, ArtifactId parameterId)
    {
        var parsed = ReadFiniteDouble(experiment, parameterId);
        if (parsed <= 0)
        {
            throw new ArgumentException($"Parameter '{parameterId}' must be greater than zero.", nameof(experiment));
        }

        return parsed;
    }

    private static double ReadNonNegativeDouble(ResolvedExperimentDefinition experiment, ArtifactId parameterId)
    {
        var parsed = ReadFiniteDouble(experiment, parameterId);
        if (parsed < 0)
        {
            throw new ArgumentException($"Parameter '{parameterId}' must be zero or greater.", nameof(experiment));
        }

        return parsed;
    }

    private static int ReadPositiveInt(ResolvedExperimentDefinition experiment, ArtifactId parameterId)
    {
        if (!experiment.TryGetParameterValue(parameterId, out var value) || value is null)
        {
            throw new ArgumentException($"Missing required flow/Reynolds parameter '{parameterId}'.", nameof(experiment));
        }

        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) || parsed <= 0)
        {
            throw new ArgumentException($"Parameter '{parameterId}' must contain a positive integer value.", nameof(experiment));
        }

        return parsed;
    }

    private SnapshotOutputPort<FlowReynoldsDerivedStateSnapshot> StatePort => _state;

    private SnapshotOutputPort<DeviceDiagnosticsSnapshot> DiagnosticsPort => _diagnostics;

    private StreamOutputPort<FlowReynoldsDerivedStateSample> SamplesPort => _samples;

    private void PublishState(FlowReynoldsDerivedStateSnapshot snapshot)
    {
        StatePort.Publish(snapshot);
    }

    private void PublishDiagnostics(DeviceDiagnosticsSnapshot snapshot)
    {
        DiagnosticsPort.Publish(snapshot);
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(FlowReynoldsDerivedStateSession));
        }
    }
}
