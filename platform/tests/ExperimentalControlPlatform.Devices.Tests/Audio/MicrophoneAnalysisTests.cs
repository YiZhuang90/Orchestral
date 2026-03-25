using System;
using System.Linq;
using ExperimentalControlPlatform.Devices.Audio;
using Xunit;

namespace ExperimentalControlPlatform.Devices.Tests.Audio;

public sealed class MicrophoneAnalysisTests
{
    [Fact]
    public void ComputeRmsDbfs_ReturnsNegativeInfinity_ForSilence()
    {
        var samples = new float[] { 0f, 0f, 0f, 0f };

        var rmsDbfs = MicrophoneAnalysis.ComputeRmsDbfs(samples);

        Assert.True(double.IsNegativeInfinity(rmsDbfs));
    }

    [Fact]
    public void ComputeRmsDbfs_ReturnsExpectedValue_ForHalfScaleSignal()
    {
        var samples = Enumerable.Repeat(0.5f, 16).ToArray();

        var rmsDbfs = MicrophoneAnalysis.ComputeRmsDbfs(samples);

        Assert.InRange(rmsDbfs, -6.1, -6.0);
    }

    [Fact]
    public void MixToMono_MonoMix_AveragesStereoPairs()
    {
        var interleavedStereo = new float[] { 0.5f, -0.5f, 1.0f, 0.0f, -1.0f, 1.0f };

        var mono = MicrophoneAnalysis.SelectChannel(interleavedStereo, channels: 2, MicrophoneChannelMode.MonoMix);

        Assert.Equal(new[] { 0.0f, 0.5f, 0.0f }, mono);
    }

    [Fact]
    public void BuildWaveformEnvelope_ReturnsRequestedNumberOfPoints_AndPreservesAmplitudeBounds()
    {
        var samples = new float[] { -1.0f, -0.5f, 0.0f, 0.25f, 0.5f, 0.75f, 1.0f, 0.5f };

        var envelope = MicrophoneAnalysis.BuildWaveformEnvelope(samples, pointCount: 4);

        Assert.Equal(4, envelope.Length);
        Assert.Equal(-1.0f, envelope.Min());
        Assert.Equal(1.0f, envelope.Max());
    }

    [Fact]
    public void DetectClipping_FindsNearFullScaleSamples()
    {
        var samples = new float[] { 0.1f, 0.9995f, -0.2f };

        var clipping = MicrophoneAnalysis.DetectClipping(samples);

        Assert.True(clipping);
    }
}
