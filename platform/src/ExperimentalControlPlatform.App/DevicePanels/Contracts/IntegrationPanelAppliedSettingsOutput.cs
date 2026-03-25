using System;
using System.Collections.Generic;

namespace ExperimentalControlPlatform.App.DevicePanels.Contracts;

public sealed record IntegrationPanelAppliedSettingsOutput
{
    public DateTimeOffset? AppliedAt { get; init; }

    public IReadOnlyDictionary<string, string?>? DeviceSettings { get; init; }

    public IReadOnlyDictionary<string, string?>? EndpointSettings { get; init; }

    public IReadOnlyDictionary<string, string?>? SessionSettings { get; init; }

    public IReadOnlyList<string>? NormalizationNotes { get; init; }
}
