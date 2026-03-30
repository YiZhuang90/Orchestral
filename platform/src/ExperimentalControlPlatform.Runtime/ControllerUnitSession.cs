using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ExperimentalControlPlatform.Core.Artifacts;

namespace ExperimentalControlPlatform.Runtime;

public sealed class ControllerUnitSession : IAsyncDisposable
{
    private readonly ControlTargetDefinition _controlTarget;
    private readonly DateTimeOffset _runStartedAtUtc;
    private readonly Func<ControllerDecision, CancellationToken, Task>? _applyDecisionAsync;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly double? _constantTargetValue;
    private readonly IReadOnlyList<ControlTargetSchedulePoint> _schedulePoints;
    private DateTimeOffset? _lastObservedAtUtc;
    private bool _stopped;

    public ControllerUnitSession(
        ResolvedExperimentDefinition experiment,
        ArtifactId controlTargetId,
        DateTimeOffset runStartedAtUtc,
        Func<ControllerDecision, CancellationToken, Task>? applyDecisionAsync = null)
    {
        ArgumentNullException.ThrowIfNull(experiment);
        _runStartedAtUtc = runStartedAtUtc;
        _applyDecisionAsync = applyDecisionAsync;

        if (!experiment.TryGetControlTarget(controlTargetId, out var controlTarget) || controlTarget is null)
        {
            throw new ArgumentException($"Resolved experiment does not contain control target '{controlTargetId}'.", nameof(controlTargetId));
        }

        _controlTarget = controlTarget;
        (_constantTargetValue, _schedulePoints) = ResolveTargetConfiguration(experiment, controlTarget);

        State = new SnapshotOutputPort<ControllerUnitState>(new ControllerUnitState
        {
            ControlTargetId = controlTarget.Id,
            ControlTargetName = controlTarget.Name,
            MeasuredSourceId = controlTarget.MeasuredSourceId,
            CommandRoleId = controlTarget.CommandRoleId,
            SetpointProfile = controlTarget.SetpointProfile,
            RegulationMode = controlTarget.RegulationMode,
            RunStartedAtUtc = runStartedAtUtc,
            StatusMessage = "Controller unit ready."
        });
        Diagnostics = new SnapshotOutputPort<DeviceDiagnosticsSnapshot>(new DeviceDiagnosticsSnapshot
        {
            LastStateTransition = $"Initialized controller unit '{controlTarget.Name}'."
        });
        SessionEnd = new SnapshotOutputPort<DeviceSessionEndSnapshot?>();
        LatestDecision = new SnapshotOutputPort<ControllerDecision?>();
        Decisions = new StreamOutputPort<ControllerDecision>();
    }

    public ISnapshotOutputPort<ControllerUnitState> State { get; }

    public ISnapshotOutputPort<DeviceDiagnosticsSnapshot> Diagnostics { get; }

    public ISnapshotOutputPort<DeviceSessionEndSnapshot?> SessionEnd { get; }

    public ISnapshotOutputPort<ControllerDecision?> LatestDecision { get; }

    public IStreamOutputPort<ControllerDecision> Decisions { get; }

    public async Task SampleAsync(DateTimeOffset observedAtUtc, double? measuredValue = null, CancellationToken cancellationToken = default)
    {
        if (observedAtUtc < _runStartedAtUtc)
        {
            throw new ArgumentOutOfRangeException(nameof(observedAtUtc), "Observed timestamp must not be earlier than run start.");
        }

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_stopped)
            {
                throw new InvalidOperationException("Controller unit session is already stopped.");
            }

            if (_lastObservedAtUtc.HasValue && observedAtUtc < _lastObservedAtUtc.Value)
            {
                throw new InvalidOperationException("Controller samples must not arrive out of order.");
            }

            _lastObservedAtUtc = observedAtUtc;

            PublishDiagnostics(Diagnostics.Current! with
            {
                LastCommand = "Evaluate controller sample",
                LastValidationResult = $"Sampled {_controlTarget.SetpointProfile}/{_controlTarget.RegulationMode} control target.",
                LastError = null
            });

            var elapsed = observedAtUtc - _runStartedAtUtc;
            var targetValue = EvaluateTargetValue(elapsed);
            var currentState = State.Current!;
            var currentMeasuredValue = measuredValue ?? currentState.MeasuredValue;
            var measuredObservedAtUtc = measuredValue.HasValue
                ? observedAtUtc
                : currentState.MeasuredValueObservedAtUtc;
            var measuredValueIsStale = !measuredValue.HasValue && measuredObservedAtUtc.HasValue;
            var errorValue = currentMeasuredValue.HasValue ? targetValue - currentMeasuredValue.Value : 0d;
            var controlOutputValue = _controlTarget.RegulationMode == "closed_loop"
                ? errorValue
                : targetValue;
            var controlOutputInterpretation = _controlTarget.RegulationMode == "closed_loop"
                ? "proportional_error"
                : "setpoint_passthrough";

            var decision = new ControllerDecision
            {
                ControlTargetId = _controlTarget.Id,
                ControlTargetName = _controlTarget.Name,
                MeasuredSourceId = _controlTarget.MeasuredSourceId,
                CommandRoleId = _controlTarget.CommandRoleId,
                SetpointProfile = _controlTarget.SetpointProfile,
                RegulationMode = _controlTarget.RegulationMode,
                ObservedAtUtc = observedAtUtc,
                ElapsedRunTime = elapsed,
                TargetValue = targetValue,
                MeasuredValue = currentMeasuredValue,
                MeasuredValueObservedAtUtc = measuredObservedAtUtc,
                MeasuredValueIsStale = measuredValueIsStale,
                ErrorValue = errorValue,
                ControlOutputValue = controlOutputValue,
                ControlOutputInterpretation = controlOutputInterpretation
            };

            LatestDecisionPort.Publish(decision);
            DecisionsPort.Publish(decision);

            PublishState(currentState with
            {
                LastObservedAtUtc = observedAtUtc,
                LastElapsedRunTime = elapsed,
                TargetValue = targetValue,
                MeasuredValue = currentMeasuredValue,
                MeasuredValueObservedAtUtc = measuredObservedAtUtc,
                MeasuredValueIsStale = measuredValueIsStale,
                ErrorValue = errorValue,
                ControlOutputValue = controlOutputValue,
                ControlOutputInterpretation = controlOutputInterpretation,
                SampleSequence = currentState.SampleSequence + 1,
                StatusMessage = BuildStatusMessage(targetValue, currentMeasuredValue, errorValue, measuredValueIsStale)
            });

            if (_applyDecisionAsync is null)
            {
                PublishDiagnostics(Diagnostics.Current! with
                {
                    LastHardwareResponse = "Published controller decision without a command sink.",
                    LastStateTransition = $"Published controller decision for '{_controlTarget.Name}'.",
                    LastError = null
                });
                return;
            }

            try
            {
                await _applyDecisionAsync(decision, cancellationToken).ConfigureAwait(false);
                PublishDiagnostics(Diagnostics.Current! with
                {
                    LastHardwareResponse = "Applied controller decision through command sink.",
                    LastStateTransition = $"Applied controller decision for '{_controlTarget.Name}'.",
                    LastError = null
                });
            }
            catch (Exception ex)
            {
                PublishDiagnostics(Diagnostics.Current! with
                {
                    LastError = ex.Message
                });
                throw;
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    public async ValueTask StopAsync(StopReason reason, DateTimeOffset? stoppedAtUtc = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(reason);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_stopped)
            {
                return;
            }

            _stopped = true;

            PublishDiagnostics(Diagnostics.Current! with
            {
                LastStateTransition = $"Stopped controller unit '{_controlTarget.Name}'.",
                LastError = null
            });
            PublishState(State.Current! with
            {
                StatusMessage = $"Controller unit stopped: {reason.Code}."
            });
            SessionEndPort.Publish(new DeviceSessionEndSnapshot
            {
                EndedAt = stoppedAtUtc ?? DateTimeOffset.UtcNow,
                ReasonCode = reason.Code,
                ReasonMessage = reason.Message,
                ConnectionClosed = false,
                LiveStopped = true
            });
        }
        finally
        {
            _gate.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync(StopReason.UserRequested("Disposed controller unit session.")).ConfigureAwait(false);
    }

    private double EvaluateTargetValue(TimeSpan elapsed)
    {
        if (_controlTarget.SetpointProfile == "constant")
        {
            return _constantTargetValue!.Value;
        }

        if (_schedulePoints.Count == 0)
        {
            throw new InvalidOperationException("Scheduled controller unit has no schedule points.");
        }

        var selectedPoint = _schedulePoints[0];
        foreach (var point in _schedulePoints)
        {
            if (point.Offset > elapsed)
            {
                break;
            }

            selectedPoint = point;
        }

        return selectedPoint.Value;
    }

    private static (double? ConstantTargetValue, IReadOnlyList<ControlTargetSchedulePoint> SchedulePoints) ResolveTargetConfiguration(
        ResolvedExperimentDefinition experiment,
        ControlTargetDefinition controlTarget)
    {
        if (controlTarget.SetpointProfile == "constant")
        {
            if (!experiment.TryGetParameterValue(controlTarget.TargetParameterId!.Value, out var rawValue) || rawValue is null)
            {
                throw new ArgumentException($"Control target '{controlTarget.Id}' is missing a value for parameter '{controlTarget.TargetParameterId}'.");
            }

            return (ParseDouble(rawValue, controlTarget.TargetParameterId!.Value), Array.Empty<ControlTargetSchedulePoint>());
        }

        if (!experiment.TryGetParameterValue(controlTarget.ScheduleParameterId!.Value, out var scheduleValue) || scheduleValue is null)
        {
            throw new ArgumentException($"Control target '{controlTarget.Id}' is missing a schedule value for parameter '{controlTarget.ScheduleParameterId}'.");
        }

        return (null, ControlTargetScheduleParser.Parse(scheduleValue, controlTarget.ScheduleParameterId!.Value));
    }

    private static double ParseDouble(string value, ArtifactId parameterId)
    {
        if (!double.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var parsed))
        {
            throw new ArgumentException(
                $"Parameter '{parameterId}' must contain a finite numeric value.",
                nameof(value));
        }

        if (double.IsNaN(parsed) || double.IsInfinity(parsed))
        {
            throw new ArgumentException(
                $"Parameter '{parameterId}' must contain a finite numeric value.",
                nameof(value));
        }

        return parsed;
    }

    private static string BuildStatusMessage(double targetValue, double? measuredValue, double errorValue, bool measuredValueIsStale)
    {
        if (!measuredValue.HasValue)
        {
            return $"Target {targetValue:0.###}; awaiting measured value.";
        }

        return measuredValueIsStale
            ? $"Target {targetValue:0.###} +/- {errorValue:0.###} (stale measured value)."
            : $"Target {targetValue:0.###} +/- {errorValue:0.###}.";
    }

    private SnapshotOutputPort<DeviceDiagnosticsSnapshot> DiagnosticsPort => (SnapshotOutputPort<DeviceDiagnosticsSnapshot>)Diagnostics;

    private SnapshotOutputPort<ControllerDecision?> LatestDecisionPort => (SnapshotOutputPort<ControllerDecision?>)LatestDecision;

    private SnapshotOutputPort<DeviceSessionEndSnapshot?> SessionEndPort => (SnapshotOutputPort<DeviceSessionEndSnapshot?>)SessionEnd;

    private SnapshotOutputPort<ControllerUnitState> StatePort => (SnapshotOutputPort<ControllerUnitState>)State;

    private StreamOutputPort<ControllerDecision> DecisionsPort => (StreamOutputPort<ControllerDecision>)Decisions;

    private void PublishDiagnostics(DeviceDiagnosticsSnapshot snapshot)
    {
        DiagnosticsPort.Publish(snapshot);
    }

    private void PublishState(ControllerUnitState state)
    {
        StatePort.Publish(state);
    }
}
