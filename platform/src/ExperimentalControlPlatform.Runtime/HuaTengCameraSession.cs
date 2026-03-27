using System;
using System.Threading;
using System.Threading.Tasks;

namespace ExperimentalControlPlatform.Runtime;

public sealed class HuaTengCameraSession : IDeviceSession
{
    private readonly IHuaTengCameraRuntimeService _service;
    private readonly object _syncRoot = new();
    private readonly int _cameraIndex;
    private readonly string _displayName;
    private CancellationTokenSource? _liveCancellation;
    private Task? _liveTask;

    public HuaTengCameraSession(IHuaTengCameraRuntimeService service, string deviceId, int cameraIndex, string displayName)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _cameraIndex = cameraIndex;
        _displayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
        SessionId = new DeviceSessionId("HuaTengCamera", deviceId);
        State = new SnapshotOutputPort<HuaTengSessionState>(new HuaTengSessionState
        {
            DeviceId = deviceId,
            DeviceName = displayName
        });
        AppliedSettings = new SnapshotOutputPort<HuaTengCaptureSettings?>();
        Diagnostics = new SnapshotOutputPort<DeviceDiagnosticsSnapshot>(new DeviceDiagnosticsSnapshot());
        SessionEnd = new SnapshotOutputPort<DeviceSessionEndSnapshot?>();
        LatestFrame = new SnapshotOutputPort<HuaTengFrame?>();
        Frames = new StreamOutputPort<HuaTengFrame>();
    }

    public DeviceSessionId SessionId { get; }

    public ISnapshotOutputPort<HuaTengSessionState> State { get; }

    public ISnapshotOutputPort<HuaTengCaptureSettings?> AppliedSettings { get; }

    public ISnapshotOutputPort<DeviceDiagnosticsSnapshot> Diagnostics { get; }

    public ISnapshotOutputPort<DeviceSessionEndSnapshot?> SessionEnd { get; }

    public ISnapshotOutputPort<HuaTengFrame?> LatestFrame { get; }

    public IStreamOutputPort<HuaTengFrame> Frames { get; }

    public async Task ConnectAsync(HuaTengCaptureSettings settings, CancellationToken cancellationToken = default)
    {
        ValidateSettings(settings);
        PublishSessionEnd(null);
        PublishDiagnostics(Diagnostics.Current! with
        {
            LastCommand = "Connect HuaTeng camera session",
            LastError = null
        });
        PublishState(State.Current! with
        {
            Busy = true,
            StatusMessage = $"Connecting to {_displayName}..."
        });

        try
        {
            var frame = await _service.CaptureSnapshotAsync(settings, cancellationToken).ConfigureAwait(false);
            EnsureFrameSucceeded(frame, "connect");
            AppliedSettingsPort.Publish(settings);
            PublishFrame(frame, "connect");
            PublishDiagnostics(Diagnostics.Current! with
            {
                LastHardwareResponse = frame.Summary,
                LastStateTransition = $"Connected to {_displayName}",
                LastError = null
            });
            PublishState(State.Current! with
            {
                Connected = true,
                Busy = false,
                LivePreviewing = false,
                StatusMessage = $"Connected to {_displayName}.",
                PixelFormat = settings.PixelFormat,
                TriggerMode = settings.TriggerMode,
                ColorTone = settings.ColorTone,
                ExposureUs = settings.ExposureUs,
                TargetFrameRate = settings.TargetFrameRate,
                AppliedRoi = frame.Roi ?? settings.Roi
            });
        }
        catch (Exception ex)
        {
            PublishDiagnostics(Diagnostics.Current! with { LastError = ex.Message });
            PublishState(State.Current! with
            {
                Connected = false,
                Busy = false,
                LivePreviewing = false,
                StatusMessage = $"Unable to connect HuaTeng camera: {ex.Message}"
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
            LastCommand = "Capture HuaTeng camera snapshot",
            LastError = null
        });
        PublishState(State.Current! with
        {
            Busy = true,
            StatusMessage = "Capturing HuaTeng camera snapshot..."
        });

        try
        {
            var frame = await _service.CaptureSnapshotAsync(settings, cancellationToken).ConfigureAwait(false);
            EnsureFrameSucceeded(frame, "snapshot");
            PublishFrame(frame, "snapshot");
            PublishDiagnostics(Diagnostics.Current! with
            {
                LastHardwareResponse = frame.Summary,
                LastStateTransition = "Captured HuaTeng camera snapshot",
                LastError = null
            });
            PublishState(State.Current! with
            {
                Busy = false,
                StatusMessage = "HuaTeng camera snapshot captured.",
                AppliedRoi = frame.Roi ?? State.Current!.AppliedRoi
            });
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

    public async Task ApplySettingsAsync(HuaTengCaptureSettings settings, CancellationToken cancellationToken = default)
    {
        ValidateSettings(settings);
        var previousSettings = AppliedSettings.Current;
        PublishDiagnostics(Diagnostics.Current! with
        {
            LastCommand = "Apply HuaTeng camera settings",
            LastValidationResult = "Validated HuaTeng camera settings.",
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
                PixelFormat = settings.PixelFormat,
                TriggerMode = settings.TriggerMode,
                ColorTone = settings.ColorTone,
                ExposureUs = settings.ExposureUs,
                TargetFrameRate = settings.TargetFrameRate,
                AppliedRoi = settings.Roi,
                StatusMessage = "HuaTeng camera settings staged for the next capture."
            });
            return;
        }

        try
        {
            var frame = await _service.CaptureSnapshotAsync(settings, cancellationToken).ConfigureAwait(false);
            EnsureFrameSucceeded(frame, "apply");
            AppliedSettingsPort.Publish(settings);
            PublishFrame(frame, "apply");
            PublishDiagnostics(Diagnostics.Current! with
            {
                LastHardwareResponse = frame.Summary,
                LastStateTransition = wasLive
                    ? "Applied settings and restarted HuaTeng live preview"
                    : "Applied settings",
                LastError = null
            });
            PublishState(State.Current! with
            {
                PixelFormat = settings.PixelFormat,
                TriggerMode = settings.TriggerMode,
                ColorTone = settings.ColorTone,
                ExposureUs = settings.ExposureUs,
                TargetFrameRate = settings.TargetFrameRate,
                AppliedRoi = frame.Roi ?? settings.Roi,
                StatusMessage = wasLive
                    ? "HuaTeng camera settings applied. Restarting live preview..."
                    : "HuaTeng camera settings applied."
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
                PixelFormat = previousSettings?.PixelFormat ?? State.Current!.PixelFormat,
                TriggerMode = previousSettings?.TriggerMode ?? State.Current!.TriggerMode,
                ColorTone = previousSettings?.ColorTone ?? State.Current!.ColorTone,
                ExposureUs = previousSettings?.ExposureUs ?? State.Current!.ExposureUs,
                TargetFrameRate = previousSettings?.TargetFrameRate ?? State.Current!.TargetFrameRate,
                AppliedRoi = previousSettings?.Roi ?? State.Current!.AppliedRoi,
                StatusMessage = $"Applying HuaTeng camera settings failed: {ex.Message}"
            });

            if (wasLive && previousSettings is not null)
            {
                await StartLiveAsync().ConfigureAwait(false);
            }

            throw;
        }
    }

    public async Task ApplyRoiAsync(CaptureRegion? roi, CancellationToken cancellationToken = default)
    {
        var settings = RequireAppliedSettings() with { Roi = roi };
        await ApplySettingsAsync(settings, cancellationToken).ConfigureAwait(false);
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
            LastCommand = "Start HuaTeng camera live preview",
            LastStateTransition = "Started HuaTeng camera live preview",
            LastError = null
        });
        PublishState(State.Current! with
        {
            LivePreviewing = true,
            StatusMessage = "HuaTeng camera live preview started."
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

        PublishDiagnostics(Diagnostics.Current! with { LastStateTransition = "Stopped HuaTeng camera live preview" });
        liveCancellation.Cancel();
        await liveTask.ConfigureAwait(false);
    }

    public async Task DisconnectAsync(StopReason? reason = null, CancellationToken cancellationToken = default)
    {
        var stopReason = reason ?? StopReason.UserRequested("Disconnected HuaTeng camera session.");
        var liveWasActive = State.Current!.LivePreviewing;
        await StopLiveAsync(cancellationToken).ConfigureAwait(false);

        PublishDiagnostics(Diagnostics.Current! with
        {
            LastStateTransition = "Disconnected HuaTeng camera",
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

    private SnapshotOutputPort<HuaTengCaptureSettings?> AppliedSettingsPort => (SnapshotOutputPort<HuaTengCaptureSettings?>)AppliedSettings;
    private SnapshotOutputPort<DeviceDiagnosticsSnapshot> DiagnosticsPort => (SnapshotOutputPort<DeviceDiagnosticsSnapshot>)Diagnostics;
    private SnapshotOutputPort<DeviceSessionEndSnapshot?> SessionEndPort => (SnapshotOutputPort<DeviceSessionEndSnapshot?>)SessionEnd;
    private SnapshotOutputPort<HuaTengSessionState> StatePort => (SnapshotOutputPort<HuaTengSessionState>)State;
    private SnapshotOutputPort<HuaTengFrame?> LatestFramePort => (SnapshotOutputPort<HuaTengFrame?>)LatestFrame;
    private StreamOutputPort<HuaTengFrame> FramesPort => (StreamOutputPort<HuaTengFrame>)Frames;

    private async Task RunLiveLoopAsync(HuaTengCaptureSettings settings, CancellationToken cancellationToken)
    {
        try
        {
            await _service.StreamFramesAsync(settings, frame =>
            {
                EnsureFrameSucceeded(frame, "live");
                PublishFrame(frame, "live");
                PublishDiagnostics(Diagnostics.Current! with
                {
                    LastHardwareResponse = frame.Summary,
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
                StatusMessage = currentState.Connected ? "HuaTeng camera live preview stopped." : currentState.StatusMessage
            });
        }
    }

    private void PublishFrame(HuaTengFrame frame, string sourceMode)
    {
        var timestamp = DateTimeOffset.UtcNow;
        var currentState = State.Current!;
        PublishState(currentState with
        {
            Connected = true,
            Busy = false,
            LastFrameCapturedAt = timestamp,
            FrameSequence = currentState.FrameSequence + 1,
            LastSourceMode = sourceMode,
            AppliedRoi = frame.Roi ?? currentState.AppliedRoi
        });
        LatestFramePort.Publish(frame);
        FramesPort.Publish(frame);
    }

    private void PublishDiagnostics(DeviceDiagnosticsSnapshot snapshot) => DiagnosticsPort.Publish(snapshot);
    private void PublishSessionEnd(DeviceSessionEndSnapshot? snapshot) => SessionEndPort.Publish(snapshot);
    private void PublishState(HuaTengSessionState state) => StatePort.Publish(state);

    private void ValidateSettings(HuaTengCaptureSettings settings)
    {
        if (!string.Equals(settings.DeviceId, SessionId.DeviceId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Settings target '{settings.DeviceId}' does not match session device '{SessionId.DeviceId}'.");
        }

        if (settings.CameraIndex != _cameraIndex)
        {
            throw new InvalidOperationException("HuaTeng camera index does not match the session.");
        }

        if (settings.TargetFrameRate <= 0)
        {
            throw new InvalidOperationException("Target frame rate must be positive.");
        }
    }

    private void EnsureConnected()
    {
        if (!State.Current!.Connected)
        {
            throw new InvalidOperationException("HuaTeng camera session is not connected.");
        }
    }

    private HuaTengCaptureSettings RequireAppliedSettings()
    {
        return AppliedSettings.Current ?? throw new InvalidOperationException("HuaTeng camera session has no applied settings.");
    }

    private static void EnsureFrameSucceeded(HuaTengFrame frame, string operation)
    {
        if (!frame.Ok)
        {
            throw new InvalidOperationException($"HuaTeng {operation} failed: {frame.Summary}");
        }
    }
}
