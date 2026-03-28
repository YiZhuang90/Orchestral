using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ExperimentalControlPlatform.Runtime;

public sealed class IntegratedCameraSession : IDeviceSession
{
    private readonly IIntegratedCameraRuntimeService _service;
    private readonly object _syncRoot = new();
    private readonly int _cameraIndex;
    private readonly string _displayName;
    private CancellationTokenSource? _liveCancellation;
    private Task? _liveTask;

    public IntegratedCameraSession(IIntegratedCameraRuntimeService service, string deviceId, int cameraIndex, string displayName)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _cameraIndex = cameraIndex;
        _displayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
        SessionId = new DeviceSessionId("IntegratedCamera", deviceId);
        State = new SnapshotOutputPort<IntegratedCameraSessionState>(new IntegratedCameraSessionState
        {
            DeviceId = deviceId,
            DeviceName = displayName
        });
        AppliedSettings = new SnapshotOutputPort<IntegratedCameraCaptureSettings?>();
        Diagnostics = new SnapshotOutputPort<DeviceDiagnosticsSnapshot>(new DeviceDiagnosticsSnapshot());
        SessionEnd = new SnapshotOutputPort<DeviceSessionEndSnapshot?>();
        LatestFrame = new SnapshotOutputPort<IntegratedCameraFrame?>();
        Frames = new StreamOutputPort<IntegratedCameraFrame>();
    }

    public DeviceSessionId SessionId { get; }

    public ISnapshotOutputPort<IntegratedCameraSessionState> State { get; }

    public ISnapshotOutputPort<IntegratedCameraCaptureSettings?> AppliedSettings { get; }

    public ISnapshotOutputPort<DeviceDiagnosticsSnapshot> Diagnostics { get; }

    public ISnapshotOutputPort<DeviceSessionEndSnapshot?> SessionEnd { get; }

    public ISnapshotOutputPort<IntegratedCameraFrame?> LatestFrame { get; }

    public IStreamOutputPort<IntegratedCameraFrame> Frames { get; }

    public async Task ConnectAsync(IntegratedCameraCaptureSettings settings, CancellationToken cancellationToken = default)
    {
        var validation = ValidateSettings(settings);
        validation.ThrowIfInvalid();
        PublishSessionEnd(null);
        PublishDiagnostics(Diagnostics.Current! with
        {
            LastCommand = "Connect integrated camera session",
            LastValidationResult = validation.Summary,
            LastError = null
        });
        PublishState(State.Current! with
        {
            Busy = true,
            StatusMessage = $"Connecting to {_displayName}..."
        });

        try
        {
            var frame = _service.CaptureSnapshot(settings);
            AppliedSettingsPort.Publish(settings);
            PublishFrame(frame, "connect");
            PublishDiagnostics(Diagnostics.Current! with
            {
                LastHardwareResponse = "Integrated camera connection probe succeeded.",
                LastStateTransition = $"Connected to {_displayName}",
                LastError = null
            });
            PublishState(State.Current! with
            {
                Connected = true,
                Busy = false,
                LivePreviewing = false,
                StatusMessage = $"Connected to {_displayName}.",
                TargetFrameRate = settings.TargetFrameRate,
                ColorEnabled = settings.ColorEnabled
            });
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            PublishDiagnostics(Diagnostics.Current! with { LastError = ex.Message });
            PublishState(State.Current! with
            {
                Connected = false,
                Busy = false,
                LivePreviewing = false,
                StatusMessage = $"Unable to connect integrated camera: {ex.Message}"
            });
            throw;
        }
    }

    public async Task SnapFrameAsync(CancellationToken cancellationToken = default)
    {
        EnsureConnected();
        var settings = RequireAppliedSettings();
        PublishDiagnostics(Diagnostics.Current! with
        {
            LastCommand = "Capture integrated camera snapshot",
            LastError = null
        });
        PublishState(State.Current! with
        {
            Busy = true,
            StatusMessage = "Capturing integrated camera snapshot..."
        });

        try
        {
            var frame = _service.CaptureSnapshot(settings);
            PublishFrame(frame, "snapshot");
            PublishDiagnostics(Diagnostics.Current! with
            {
                LastHardwareResponse = "Integrated camera snapshot captured.",
                LastStateTransition = "Captured integrated camera snapshot",
                LastError = null
            });
            PublishState(State.Current! with
            {
                Busy = false,
                StatusMessage = "Integrated camera snapshot captured."
            });
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            PublishDiagnostics(Diagnostics.Current! with { LastError = ex.Message });
            PublishState(State.Current! with
            {
                Busy = false,
                StatusMessage = $"Snapshot failed: {ex.Message}"
            });
            throw;
        }
    }

    public async Task ApplySettingsAsync(IntegratedCameraCaptureSettings settings, CancellationToken cancellationToken = default)
    {
        var validation = ValidateSettings(settings);
        validation.ThrowIfInvalid();
        var previousSettings = AppliedSettings.Current;
        PublishDiagnostics(Diagnostics.Current! with
        {
            LastCommand = "Apply integrated camera settings",
            LastValidationResult = validation.Summary,
            LastError = null
        });

        var wasLive = State.Current!.LivePreviewing;
        if (wasLive)
        {
            await StopLiveAsync(cancellationToken).ConfigureAwait(false);
        }

        if (!State.Current!.Connected)
        {
            AppliedSettingsPort.Publish(settings);
            PublishDiagnostics(Diagnostics.Current! with { LastStateTransition = "Staged settings for the next capture" });
            PublishState(State.Current! with
            {
                TargetFrameRate = settings.TargetFrameRate,
                ColorEnabled = settings.ColorEnabled,
                StatusMessage = "Integrated camera settings staged for the next capture."
            });
            return;
        }

        try
        {
            var frame = _service.CaptureSnapshot(settings);
            AppliedSettingsPort.Publish(settings);
            PublishFrame(frame, "apply");
            PublishDiagnostics(Diagnostics.Current! with
            {
                LastHardwareResponse = "Applied settings snapshot captured.",
                LastStateTransition = wasLive
                    ? "Applied settings and restarted integrated camera live preview"
                    : "Applied settings",
                LastError = null
            });
            PublishState(State.Current! with
            {
                TargetFrameRate = settings.TargetFrameRate,
                ColorEnabled = settings.ColorEnabled,
                StatusMessage = wasLive
                    ? "Integrated camera settings applied. Restarting live preview..."
                    : "Integrated camera settings applied."
            });

            if (wasLive)
            {
                await StartLiveAsync().ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            if (previousSettings is not null)
            {
                AppliedSettingsPort.Publish(previousSettings);
            }

            PublishDiagnostics(Diagnostics.Current! with { LastError = ex.Message });
            PublishState(State.Current! with
            {
                TargetFrameRate = previousSettings?.TargetFrameRate ?? State.Current!.TargetFrameRate,
                ColorEnabled = previousSettings?.ColorEnabled ?? State.Current!.ColorEnabled,
                StatusMessage = $"Applying integrated camera settings failed: {ex.Message}"
            });

            if (wasLive && previousSettings is not null)
            {
                await StartLiveAsync().ConfigureAwait(false);
            }

            throw;
        }
    }

    public Task StartLiveAsync()
    {
        EnsureConnected();
        var settings = RequireAppliedSettings();

        lock (_syncRoot)
        {
            if (_liveTask is not null)
            {
                return Task.CompletedTask;
            }

            _liveCancellation = new CancellationTokenSource();
            _liveTask = RunLiveLoopAsync(settings, _liveCancellation.Token);
        }

        PublishDiagnostics(Diagnostics.Current! with
        {
            LastCommand = "Start integrated camera live preview",
            LastStateTransition = "Started integrated camera live preview",
            LastError = null
        });
        PublishState(State.Current! with
        {
            LivePreviewing = true,
            StatusMessage = "Integrated camera live preview started."
        });
        return Task.CompletedTask;
    }

    public async Task StopLiveAsync(CancellationToken cancellationToken = default)
    {
        Task? liveTask;
        CancellationTokenSource? liveCancellation;
        lock (_syncRoot)
        {
            liveTask = _liveTask;
            liveCancellation = _liveCancellation;
        }

        if (liveTask is null || liveCancellation is null)
        {
            return;
        }

        PublishDiagnostics(Diagnostics.Current! with { LastStateTransition = "Stopped integrated camera live preview" });
        liveCancellation.Cancel();
        await liveTask.ConfigureAwait(false);
    }

    public async Task DisconnectAsync(StopReason? reason = null, CancellationToken cancellationToken = default)
    {
        var stopReason = reason ?? StopReason.UserRequested("Disconnected integrated camera session.");
        var liveWasActive = State.Current!.LivePreviewing;
        await StopLiveAsync(cancellationToken).ConfigureAwait(false);

        PublishDiagnostics(Diagnostics.Current! with
        {
            LastStateTransition = "Disconnected integrated camera",
            LastError = null
        });
        PublishState(State.Current! with
        {
            Connected = false,
            Busy = false,
            LivePreviewing = false,
            StatusMessage = "Disconnected."
        });
        PublishSessionEnd(new DeviceSessionEndSnapshot
        {
            EndedAt = DateTimeOffset.UtcNow,
            ReasonCode = stopReason.Code,
            ReasonMessage = stopReason.Message,
            ConnectionClosed = true,
            LiveStopped = liveWasActive
        });
    }

    public ValueTask StopAsync(StopReason reason, CancellationToken cancellationToken = default)
    {
        return new ValueTask(DisconnectAsync(reason, cancellationToken));
    }

    public async ValueTask DisposeAsync()
    {
        await StopLiveAsync().ConfigureAwait(false);
        lock (_syncRoot)
        {
            _liveCancellation?.Dispose();
            _liveCancellation = null;
            _liveTask = null;
        }
    }

    private SnapshotOutputPort<IntegratedCameraCaptureSettings?> AppliedSettingsPort => (SnapshotOutputPort<IntegratedCameraCaptureSettings?>)AppliedSettings;
    private SnapshotOutputPort<DeviceDiagnosticsSnapshot> DiagnosticsPort => (SnapshotOutputPort<DeviceDiagnosticsSnapshot>)Diagnostics;
    private SnapshotOutputPort<DeviceSessionEndSnapshot?> SessionEndPort => (SnapshotOutputPort<DeviceSessionEndSnapshot?>)SessionEnd;
    private SnapshotOutputPort<IntegratedCameraSessionState> StatePort => (SnapshotOutputPort<IntegratedCameraSessionState>)State;
    private SnapshotOutputPort<IntegratedCameraFrame?> LatestFramePort => (SnapshotOutputPort<IntegratedCameraFrame?>)LatestFrame;
    private StreamOutputPort<IntegratedCameraFrame> FramesPort => (StreamOutputPort<IntegratedCameraFrame>)Frames;

    private async Task RunLiveLoopAsync(IntegratedCameraCaptureSettings settings, CancellationToken cancellationToken)
    {
        try
        {
            await _service.StreamFramesAsync(settings, frame =>
            {
                PublishFrame(frame, "live");
                PublishDiagnostics(Diagnostics.Current! with
                {
                    LastHardwareResponse = "Integrated camera live frame received.",
                    LastError = null
                });
                return Task.CompletedTask;
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            PublishDiagnostics(Diagnostics.Current! with { LastError = ex.Message });
            PublishState(State.Current! with { StatusMessage = $"Live preview failed: {ex.Message}" });
        }
        finally
        {
            lock (_syncRoot)
            {
                _liveCancellation?.Dispose();
                _liveCancellation = null;
                _liveTask = null;
            }

            var currentState = State.Current!;
            PublishState(currentState with
            {
                LivePreviewing = false,
                StatusMessage = currentState.Connected ? "Integrated camera live preview stopped." : currentState.StatusMessage
            });
        }
    }

    private void PublishFrame(IntegratedCameraFrame frame, string sourceMode)
    {
        var timestamp = DateTimeOffset.UtcNow;
        var currentState = State.Current!;
        PublishState(currentState with
        {
            Connected = true,
            Busy = false,
            LastFrameCapturedAt = timestamp,
            FrameSequence = currentState.FrameSequence + 1,
            LastSourceMode = sourceMode
        });
        LatestFramePort.Publish(frame);
        FramesPort.Publish(frame);
    }

    private void PublishDiagnostics(DeviceDiagnosticsSnapshot snapshot) => DiagnosticsPort.Publish(snapshot);
    private void PublishSessionEnd(DeviceSessionEndSnapshot? snapshot) => SessionEndPort.Publish(snapshot);
    private void PublishState(IntegratedCameraSessionState state) => StatePort.Publish(state);

    public SessionValidationResult ValidateSettings(IntegratedCameraCaptureSettings settings) =>
        ValidateSettings(SessionId.DeviceId, _cameraIndex, settings);

    public static SessionValidationResult ValidateSettings(
        string expectedDeviceId,
        int expectedCameraIndex,
        IntegratedCameraCaptureSettings settings)
    {
        var issues = new List<SessionValidationIssue>();
        if (!string.Equals(settings.DeviceId, expectedDeviceId, StringComparison.Ordinal))
        {
            issues.Add(new SessionValidationIssue(
                "DeviceId",
                $"Settings target '{settings.DeviceId}' does not match session device '{expectedDeviceId}'."));
        }

        if (settings.CameraIndex != expectedCameraIndex)
        {
            issues.Add(new SessionValidationIssue(
                "CameraIndex",
                "Integrated camera index does not match the session."));
        }

        if (settings.TargetFrameRate <= 0)
        {
            issues.Add(new SessionValidationIssue(
                "TargetFrameRate",
                "Target frame rate must be positive."));
        }

        return SessionValidationResult.FromIssues("Validated integrated camera settings.", issues);
    }

    private void EnsureConnected()
    {
        if (!State.Current!.Connected)
        {
            throw new InvalidOperationException("Integrated camera session is not connected.");
        }
    }

    private IntegratedCameraCaptureSettings RequireAppliedSettings()
    {
        return AppliedSettings.Current ?? throw new InvalidOperationException("Integrated camera session has no applied settings.");
    }
}
