using System;
using System.Collections.Generic;
using System.Linq;
using ExperimentalControlPlatform.Runtime;

namespace ExperimentalControlPlatform.App.ExperimentMonitor;

public sealed record class ExperimentMonitorInitializationResult
{
    public ExperimentMonitorInitializationResult(
        bool isReady,
        string statusMessage,
        IReadOnlyList<ExperimentMonitorItem>? validationItems = null)
    {
        IsReady = isReady;
        StatusMessage = string.IsNullOrWhiteSpace(statusMessage)
            ? throw new ArgumentException("StatusMessage is required.", nameof(statusMessage))
            : statusMessage.Trim();
        ValidationItems = validationItems?.ToArray() ?? Array.Empty<ExperimentMonitorItem>();
    }

    public bool IsReady { get; }

    public string StatusMessage { get; }

    public IReadOnlyList<ExperimentMonitorItem> ValidationItems { get; }

    public static ExperimentMonitorInitializationResult Ready(string statusMessage) =>
        new(true, statusMessage);

    public static ExperimentMonitorInitializationResult Blocked(
        string statusMessage,
        IReadOnlyList<ExperimentMonitorItem> validationItems) =>
        new(false, statusMessage, validationItems);
}
