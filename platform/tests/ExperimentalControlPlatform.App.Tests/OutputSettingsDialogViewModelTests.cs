using System.Collections.Generic;
using ExperimentalControlPlatform.App.DevicePanels.Contracts;
using ExperimentalControlPlatform.App.Modals;
using Xunit;

namespace ExperimentalControlPlatform.App.Tests;

public sealed class OutputSettingsDialogViewModelTests
{
    [Fact]
    public void BuildSettings_ReturnsSelectedValues_WhenFrequencyIsValid()
    {
        var dialog = new OutputSettingsDialogViewModel(
            [IntegrationPanelOutputPayloadType.Scalar, IntegrationPanelOutputPayloadType.Waveform],
            new IntegrationPanelOutputSettings(
                IntegrationPanelOutputPayloadType.Scalar,
                IntegrationPanelOutputEmissionMode.LatestOnly,
                2.5,
                true));

        dialog.SelectedPayloadType = IntegrationPanelOutputPayloadType.Waveform;
        dialog.SelectedEmissionMode = IntegrationPanelOutputEmissionMode.Periodic;
        dialog.OutputFrequencyHzInput = "5";
        dialog.IncludeMetadata = false;

        var settings = dialog.BuildSettings();

        Assert.Equal(IntegrationPanelOutputPayloadType.Waveform, settings.PayloadType);
        Assert.Equal(IntegrationPanelOutputEmissionMode.Periodic, settings.EmissionMode);
        Assert.Equal(5.0, settings.OutputFrequencyHz, 3);
        Assert.False(settings.IncludeMetadata);
    }

    [Fact]
    public void BuildSettings_Throws_WhenFrequencyIsInvalid()
    {
        var dialog = new OutputSettingsDialogViewModel(
            [IntegrationPanelOutputPayloadType.Image],
            new IntegrationPanelOutputSettings(
                IntegrationPanelOutputPayloadType.Image,
                IntegrationPanelOutputEmissionMode.Periodic,
                3.0,
                true))
        {
            OutputFrequencyHzInput = "0"
        };

        var exception = Assert.Throws<InvalidOperationException>(() => dialog.BuildSettings());

        Assert.Contains("positive number", exception.Message);
    }
}
