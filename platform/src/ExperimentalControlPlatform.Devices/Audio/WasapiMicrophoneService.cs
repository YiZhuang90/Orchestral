using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Wave;

namespace ExperimentalControlPlatform.Devices.Audio;

public sealed class WasapiMicrophoneService : IMicrophoneService
{
    private static readonly TimeSpan SnapshotTimeout = TimeSpan.FromSeconds(5);
    private const int DefaultSampleRate = 48000;

    public IReadOnlyList<MicrophoneDeviceInfo> ListCaptureDevices()
    {
        var devices = new List<MicrophoneDeviceInfo>(WaveInEvent.DeviceCount);
        for (var index = 0; index < WaveInEvent.DeviceCount; index++)
        {
            var capabilities = WaveInEvent.GetCapabilities(index);
            devices.Add(new MicrophoneDeviceInfo(
                index,
                capabilities.ProductName,
                index.ToString(CultureInfo.InvariantCulture),
                DefaultSampleRate,
                Math.Max(1, capabilities.Channels)));
        }

        return devices;
    }

    public MicrophoneFrame CaptureSnapshot(MicrophoneCaptureSettings settings)
    {
        using var timeout = new CancellationTokenSource(SnapshotTimeout);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(timeout.Token);

        MicrophoneFrame? snapshot = null;
        StreamFramesAsync(
            settings,
            frame =>
            {
                snapshot = frame;
                linked.Cancel();
                return Task.CompletedTask;
            },
            linked.Token).GetAwaiter().GetResult();

        if (snapshot is null)
        {
            throw new InvalidOperationException("No microphone frame was captured.");
        }

        return snapshot;
    }

    public async Task StreamFramesAsync(
        MicrophoneCaptureSettings settings,
        Func<MicrophoneFrame, Task> onFrame,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(onFrame);
        if (!int.TryParse(settings.DeviceId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var deviceIndex) ||
            deviceIndex < 0 ||
            deviceIndex >= WaveInEvent.DeviceCount)
        {
            throw new InvalidOperationException($"Capture device '{settings.DeviceId}' was not found.");
        }

        var capabilities = WaveInEvent.GetCapabilities(deviceIndex);
        var channelCount = Math.Max(1, capabilities.Channels);
        var bufferMilliseconds = Math.Clamp(settings.WindowMilliseconds, 10, 200);
        using var capture = new WaveInEvent
        {
            DeviceNumber = deviceIndex,
            BufferMilliseconds = bufferMilliseconds,
            NumberOfBuffers = 3,
            WaveFormat = new WaveFormat(DefaultSampleRate, 16, channelCount)
        };
        using var registration = cancellationToken.Register(() =>
        {
            try
            {
                capture.StopRecording();
            }
            catch
            {
            }
        });

        var waveFormat = capture.WaveFormat;
        var windowFrameCount = Math.Max(1, (int)Math.Round(waveFormat.SampleRate * settings.WindowMilliseconds / 1000.0));
        var interleavedWindowSampleCount = Math.Max(waveFormat.Channels, windowFrameCount * waveFormat.Channels);
        var latestSamples = new List<float>(interleavedWindowSampleCount * 2);
        var minEmitTicks = settings.TargetUpdateRateHz > 0
            ? (long)Math.Round(Stopwatch.Frequency / settings.TargetUpdateRateHz)
            : 0L;
        var lastEmitTicks = 0L;
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Exception? callbackFailure = null;

        capture.DataAvailable += (_, args) =>
        {
            if (callbackFailure is not null || cancellationToken.IsCancellationRequested)
            {
                return;
            }

            var converted = ConvertToFloats(args.Buffer, args.BytesRecorded, waveFormat);
            if (converted.Length == 0)
            {
                return;
            }

            latestSamples.AddRange(converted);
            TrimBuffer(latestSamples, interleavedWindowSampleCount * 4);
            if (latestSamples.Count < interleavedWindowSampleCount)
            {
                return;
            }

            var nowTicks = Stopwatch.GetTimestamp();
            if (minEmitTicks > 0 && lastEmitTicks > 0 && nowTicks - lastEmitTicks < minEmitTicks)
            {
                return;
            }

            var rawWindow = latestSamples
                .Skip(latestSamples.Count - interleavedWindowSampleCount)
                .Take(interleavedWindowSampleCount)
                .ToArray();
            var selected = MicrophoneAnalysis.SelectChannel(rawWindow, waveFormat.Channels, settings.ChannelMode);
            var frame = new MicrophoneFrame(
                settings.DeviceId,
                capabilities.ProductName,
                waveFormat.SampleRate,
                waveFormat.Channels,
                settings.ChannelMode,
                selected,
                MicrophoneAnalysis.ComputeRmsDbfs(selected),
                MicrophoneAnalysis.ComputePeakDbfs(selected),
                MicrophoneAnalysis.DetectClipping(selected),
                Stopwatch.GetTimestamp(),
                TimeSpan.FromMilliseconds(settings.WindowMilliseconds));

            try
            {
                onFrame(frame).GetAwaiter().GetResult();
                lastEmitTicks = nowTicks;
            }
            catch (Exception ex)
            {
                callbackFailure = ex;
                capture.StopRecording();
            }
        };

        capture.RecordingStopped += (_, args) =>
        {
            if (callbackFailure is not null)
            {
                completion.TrySetException(callbackFailure);
                return;
            }

            if (args.Exception is not null)
            {
                completion.TrySetException(args.Exception);
                return;
            }

            completion.TrySetResult();
        };

        cancellationToken.ThrowIfCancellationRequested();
        capture.StartRecording();
        try
        {
            await completion.Task.ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
    }

    private static void TrimBuffer(List<float> samples, int maxLength)
    {
        if (samples.Count <= maxLength)
        {
            return;
        }

        samples.RemoveRange(0, samples.Count - maxLength);
    }

    private static float[] ConvertToFloats(byte[] buffer, int bytesRecorded, WaveFormat format)
    {
        if (bytesRecorded <= 0)
        {
            return [];
        }

        if (format.Encoding == WaveFormatEncoding.Pcm && format.BitsPerSample == 16)
        {
            return ConvertPcm16(buffer, bytesRecorded);
        }

        throw new NotSupportedException($"Wave format '{format.Encoding}' with {format.BitsPerSample} bits is not supported.");
    }

    private static float[] ConvertPcm16(byte[] buffer, int bytesRecorded)
    {
        var samples = new float[bytesRecorded / 2];
        for (var index = 0; index < samples.Length; index++)
        {
            samples[index] = BitConverter.ToInt16(buffer, index * 2) / 32768f;
        }

        return samples;
    }
}
