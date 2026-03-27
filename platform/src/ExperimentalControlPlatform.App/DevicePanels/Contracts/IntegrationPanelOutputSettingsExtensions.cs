using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ExperimentalControlPlatform.App.DevicePanels.Contracts;

public static class IntegrationPanelOutputSettingsExtensions
{
    public static IReadOnlyDictionary<string, string?> WithOutputSettings(
        this IReadOnlyDictionary<string, string?>? sessionSettings,
        IntegrationPanelOutputSettings settings)
    {
        var merged = sessionSettings is null
            ? new Dictionary<string, string?>()
            : sessionSettings.ToDictionary(pair => pair.Key, pair => pair.Value);

        merged["OutputPayloadType"] = settings.PayloadType.ToString();
        merged["OutputEmissionMode"] = settings.EmissionMode.ToString();
        merged["OutputFrequencyHz"] = settings.OutputFrequencyHz.ToString("0.###", CultureInfo.InvariantCulture);
        merged["OutputIncludeMetadata"] = settings.IncludeMetadata ? "true" : "false";
        return merged;
    }
}
