using System;
using System.Collections.Generic;
using System.Globalization;
using ExperimentalControlPlatform.App.DevicePanels;
using ExperimentalControlPlatform.App.DevicePanels.Contracts;

namespace ExperimentalControlPlatform.App.Modals;

public sealed class OutputSettingsDialogViewModel : ObservableObject
{
    private IntegrationPanelOutputPayloadType _selectedPayloadType;
    private IntegrationPanelOutputEmissionMode _selectedEmissionMode;
    private string _outputFrequencyHzInput = string.Empty;
    private bool _includeMetadata;

    public OutputSettingsDialogViewModel(
        IReadOnlyList<IntegrationPanelOutputPayloadType> payloadTypeOptions,
        IntegrationPanelOutputSettings currentSettings)
    {
        PayloadTypeOptions = payloadTypeOptions ?? throw new ArgumentNullException(nameof(payloadTypeOptions));
        if (PayloadTypeOptions.Count == 0)
        {
            throw new ArgumentException("At least one payload type is required.", nameof(payloadTypeOptions));
        }

        SelectedPayloadType = currentSettings.PayloadType;
        SelectedEmissionMode = currentSettings.EmissionMode;
        OutputFrequencyHzInput = currentSettings.OutputFrequencyHz.ToString("0.###", CultureInfo.InvariantCulture);
        IncludeMetadata = currentSettings.IncludeMetadata;
    }

    public string Subtitle => "Control what leaves the device session on the runtime bus.";

    public IReadOnlyList<IntegrationPanelOutputPayloadType> PayloadTypeOptions { get; }

    public IReadOnlyList<IntegrationPanelOutputEmissionMode> EmissionModeOptions { get; } =
    [
        IntegrationPanelOutputEmissionMode.LatestOnly,
        IntegrationPanelOutputEmissionMode.Periodic,
        IntegrationPanelOutputEmissionMode.OnChange
    ];

    public IntegrationPanelOutputPayloadType SelectedPayloadType
    {
        get => _selectedPayloadType;
        set => SetProperty(ref _selectedPayloadType, value);
    }

    public IntegrationPanelOutputEmissionMode SelectedEmissionMode
    {
        get => _selectedEmissionMode;
        set
        {
            if (SetProperty(ref _selectedEmissionMode, value))
            {
                OnPropertyChanged(nameof(IsOutputFrequencyRelevant));
                OnPropertyChanged(nameof(OutputFrequencyHint));
            }
        }
    }

    public string OutputFrequencyHzInput
    {
        get => _outputFrequencyHzInput;
        set => SetProperty(ref _outputFrequencyHzInput, value);
    }

    public bool IncludeMetadata
    {
        get => _includeMetadata;
        set => SetProperty(ref _includeMetadata, value);
    }

    public bool IsOutputFrequencyRelevant => SelectedEmissionMode == IntegrationPanelOutputEmissionMode.Periodic;

    public string OutputFrequencyHint => IsOutputFrequencyRelevant
        ? "Used to throttle periodic output on the runtime bus."
        : "Only used when emission mode is set to Periodic.";

    public IntegrationPanelOutputSettings BuildSettings()
    {
        if (!double.TryParse(OutputFrequencyHzInput, NumberStyles.Float, CultureInfo.InvariantCulture, out var frequencyHz) || frequencyHz <= 0)
        {
            throw new InvalidOperationException("Output frequency must be a positive number.");
        }

        return new IntegrationPanelOutputSettings(
            SelectedPayloadType,
            SelectedEmissionMode,
            frequencyHz,
            IncludeMetadata);
    }
}
