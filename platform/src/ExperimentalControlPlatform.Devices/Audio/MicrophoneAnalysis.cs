using System;

namespace ExperimentalControlPlatform.Devices.Audio;

public static class MicrophoneAnalysis
{
    public static double ComputeRmsDbfs(ReadOnlySpan<float> samples)
    {
        if (samples.IsEmpty)
        {
            return double.NegativeInfinity;
        }

        double sumSquares = 0;
        for (var i = 0; i < samples.Length; i++)
        {
            var sample = samples[i];
            sumSquares += sample * sample;
        }

        if (sumSquares <= 0)
        {
            return double.NegativeInfinity;
        }

        var rms = Math.Sqrt(sumSquares / samples.Length);
        return 20.0 * Math.Log10(rms);
    }

    public static double ComputePeakDbfs(ReadOnlySpan<float> samples)
    {
        if (samples.IsEmpty)
        {
            return double.NegativeInfinity;
        }

        var peak = 0.0f;
        for (var i = 0; i < samples.Length; i++)
        {
            var magnitude = MathF.Abs(samples[i]);
            if (magnitude > peak)
            {
                peak = magnitude;
            }
        }

        if (peak <= 0)
        {
            return double.NegativeInfinity;
        }

        return 20.0 * Math.Log10(peak);
    }

    public static bool DetectClipping(ReadOnlySpan<float> samples, float threshold = 0.999f)
    {
        for (var i = 0; i < samples.Length; i++)
        {
            if (MathF.Abs(samples[i]) >= threshold)
            {
                return true;
            }
        }

        return false;
    }

    public static float[] SelectChannel(ReadOnlySpan<float> interleavedSamples, int channels, MicrophoneChannelMode channelMode)
    {
        if (interleavedSamples.IsEmpty)
        {
            return [];
        }

        if (channels <= 1)
        {
            return interleavedSamples.ToArray();
        }

        var frameCount = interleavedSamples.Length / channels;
        var selected = new float[frameCount];

        for (var frame = 0; frame < frameCount; frame++)
        {
            var frameOffset = frame * channels;
            selected[frame] = channelMode switch
            {
                MicrophoneChannelMode.Left => interleavedSamples[frameOffset],
                MicrophoneChannelMode.Right => interleavedSamples[frameOffset + Math.Min(1, channels - 1)],
                _ => AverageFrame(interleavedSamples, frameOffset, channels)
            };
        }

        return selected;
    }

    public static float[] BuildWaveformEnvelope(ReadOnlySpan<float> samples, int pointCount)
    {
        if (samples.IsEmpty || pointCount <= 0)
        {
            return [];
        }

        if (samples.Length <= pointCount)
        {
            return samples.ToArray();
        }

        var envelope = new float[pointCount];
        for (var bucket = 0; bucket < pointCount; bucket++)
        {
            var start = bucket * samples.Length / pointCount;
            var end = Math.Max(start + 1, (bucket + 1) * samples.Length / pointCount);
            var peak = 0.0f;

            for (var index = start; index < end; index++)
            {
                var sample = samples[index];
                if (MathF.Abs(sample) > MathF.Abs(peak))
                {
                    peak = sample;
                }
            }

            envelope[bucket] = peak;
        }

        return envelope;
    }

    private static float AverageFrame(ReadOnlySpan<float> interleavedSamples, int frameOffset, int channels)
    {
        var sum = 0.0f;
        for (var channel = 0; channel < channels; channel++)
        {
            sum += interleavedSamples[frameOffset + channel];
        }

        return sum / channels;
    }
}
