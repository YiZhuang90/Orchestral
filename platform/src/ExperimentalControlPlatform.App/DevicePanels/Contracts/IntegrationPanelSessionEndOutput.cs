using System;
using System.Collections.Generic;

namespace ExperimentalControlPlatform.App.DevicePanels.Contracts;

public sealed record IntegrationPanelSessionEndOutput
{
    public DateTimeOffset? EndedAt { get; init; }

    public string? ExitReason { get; init; }

    public bool ConnectionClosed { get; init; }

    public bool LiveStopped { get; init; }

    public IntegrationPanelAppliedSettingsOutput? AppliedSettingsSnapshot { get; init; }

    public IntegrationPanelStatusOutput? FinalStatus { get; init; }

    public IReadOnlyList<string>? OpenIssues { get; init; }
}
