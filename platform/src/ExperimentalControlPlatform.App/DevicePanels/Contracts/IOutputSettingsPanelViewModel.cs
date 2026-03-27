using System.Collections.Generic;
using System.Threading.Tasks;

namespace ExperimentalControlPlatform.App.DevicePanels.Contracts;

public interface IOutputSettingsPanelViewModel
{
    IReadOnlyList<IntegrationPanelOutputPayloadType> SupportedOutputPayloadTypes { get; }

    IntegrationPanelOutputSettings CurrentOutputSettings { get; }

    string OutputSettingsSubtitle { get; }

    Task ApplyOutputSettingsAsync(IntegrationPanelOutputSettings settings);
}
