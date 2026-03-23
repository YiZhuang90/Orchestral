using System;
using System.Collections.Generic;

namespace ExperimentalControlPlatform.App.Modals;

public sealed class CameraSettingsDialogViewModel
{
    public CameraSettingsDialogViewModel(
        string subtitle,
        IReadOnlyList<string> triggerModeOptions,
        string selectedTriggerMode,
        IReadOnlyList<string> colorToneOptions,
        string selectedColorTone)
    {
        Subtitle = subtitle;
        TriggerModeOptions = triggerModeOptions ?? throw new ArgumentNullException(nameof(triggerModeOptions));
        SelectedTriggerMode = selectedTriggerMode;
        ColorToneOptions = colorToneOptions ?? throw new ArgumentNullException(nameof(colorToneOptions));
        SelectedColorTone = selectedColorTone;
    }

    public string Subtitle { get; }

    public IReadOnlyList<string> TriggerModeOptions { get; }

    public string SelectedTriggerMode { get; set; }

    public IReadOnlyList<string> ColorToneOptions { get; }

    public string SelectedColorTone { get; set; }
}
