using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ExperimentalControlPlatform.App.DevicePanels;
using ExperimentalControlPlatform.App.DevicePanels.Contracts;
using ExperimentalControlPlatform.Core.Artifacts;
using ExperimentalControlPlatform.ExperimentLogic;
using ExperimentalControlPlatform.Runtime;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace ExperimentalControlPlatform.App;

public sealed class RunArtifactWriter
{
    private readonly string _rootDirectory;
    private readonly AdHocRunDefinitionFactory _adHocRunDefinitionFactory;
    private readonly ISerializer _serializer;

    public RunArtifactWriter(string rootDirectory, AdHocRunDefinitionFactory? adHocRunDefinitionFactory = null)
    {
        if (string.IsNullOrWhiteSpace(rootDirectory))
        {
            throw new ArgumentException("Root directory is required.", nameof(rootDirectory));
        }

        _rootDirectory = rootDirectory.Trim();
        _adHocRunDefinitionFactory = adHocRunDefinitionFactory ?? new AdHocRunDefinitionFactory();
        _serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
            .DisableAliases()
            .Build();
    }

    public RunRecordingResult Write(
        RuntimeRunContext snapshot,
        IReadOnlyList<IDeviceTestPanelViewModel> panels,
        IReadOnlyList<string> runtimeEvents,
        ExperimentMonitorSnapshot? monitorSnapshot = null,
        FlowReynoldsDerivedStateSnapshot? derivedStateSnapshot = null,
        IReadOnlyList<FlowReynoldsDerivedStateSample>? derivedStateSamples = null)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(panels);
        ArgumentNullException.ThrowIfNull(runtimeEvents);

        var resolvedExperiment = snapshot.RunContext?.Experiment
            ?? snapshot.Experiment
            ?? _adHocRunDefinitionFactory.Create(panels);
        var runDirectoryPath = BuildRunDirectoryPath(snapshot);
        var panelsDirectoryPath = Path.Combine(runDirectoryPath, "panels");
        Directory.CreateDirectory(runDirectoryPath);
        Directory.CreateDirectory(panelsDirectoryPath);

        var panelSnapshots = CapturePanelSnapshots(panels);
        var warningsOrFaults = CollectWarnings(snapshot, panelSnapshots).ToList();
        if (monitorSnapshot is not null)
        {
            warningsOrFaults.AddRange(
                monitorSnapshot.Items
                    .Where(static item => item.Severity is ExperimentMonitorSeverity.Warning or ExperimentMonitorSeverity.Alarm)
                    .Select(static item => $"{item.Source}: {item.Message}"));
        }
        var runKey = snapshot.RunId.ToString("N")[..8];
        var artifactPaths = new Dictionary<ArtifactId, string>();
        var outputArtifactIds = new List<ArtifactId>();

        var runtimeEventsId = new ArtifactId($"artifact.runtime_events.{runKey}");
        var runtimeEventsPath = Path.Combine(runDirectoryPath, "runtime-events.yaml");
        WriteYaml(
            runtimeEventsPath,
            new Dictionary<string, object?>
            {
                ["kind"] = "runtime_events",
                ["runId"] = snapshot.RunId.ToString(),
                ["events"] = runtimeEvents.Select(
                    static (message, index) => new Dictionary<string, object?>
                    {
                        ["index"] = index + 1,
                        ["message"] = message
                    }).ToArray()
            });
        artifactPaths[runtimeEventsId] = runtimeEventsPath;
        outputArtifactIds.Add(runtimeEventsId);

        var warningsId = new ArtifactId($"artifact.warnings_or_faults.{runKey}");
        var warningsPath = Path.Combine(runDirectoryPath, "warnings-or-faults.yaml");
        WriteYaml(
            warningsPath,
            new Dictionary<string, object?>
            {
                ["kind"] = "warnings_or_faults",
                ["runId"] = snapshot.RunId.ToString(),
                ["items"] = warningsOrFaults.ToArray()
            });
        artifactPaths[warningsId] = warningsPath;
        outputArtifactIds.Add(warningsId);

        if (monitorSnapshot is not null)
        {
            var monitorSnapshotId = new ArtifactId($"artifact.monitor_snapshot.{runKey}");
            var monitorSnapshotPath = Path.Combine(runDirectoryPath, "monitor-snapshot.yaml");
            WriteYaml(
                monitorSnapshotPath,
                new Dictionary<string, object?>
                {
                    ["kind"] = "monitor_snapshot",
                    ["runId"] = snapshot.RunId.ToString(),
                    ["runState"] = monitorSnapshot.RunState.ToString(),
                    ["runDisplayName"] = monitorSnapshot.RunDisplayName,
                    ["stateSummary"] = monitorSnapshot.StateSummary,
                    ["highestSeverity"] = monitorSnapshot.HighestSeverity.ToString(),
                    ["warningCount"] = monitorSnapshot.WarningCount,
                    ["alarmCount"] = monitorSnapshot.AlarmCount,
                    ["primaryControlSummary"] = monitorSnapshot.PrimaryControlSummary,
                    ["derivedStateSummary"] = monitorSnapshot.DerivedStateSummary,
                    ["derivedReynoldsNumber"] = monitorSnapshot.DerivedReynoldsNumber,
                    ["derivedFlowRateLitersPerMinute"] = monitorSnapshot.DerivedFlowRateLitersPerMinute,
                    ["derivedMeanTemperatureC"] = monitorSnapshot.DerivedMeanTemperatureC,
                    ["derivedStateIsStale"] = monitorSnapshot.DerivedStateIsStale,
                    ["items"] = monitorSnapshot.Items.Select(
                        static item => new Dictionary<string, object?>
                        {
                            ["id"] = item.Id,
                            ["severity"] = item.Severity.ToString(),
                            ["source"] = item.Source,
                            ["message"] = item.Message,
                            ["observedAt"] = item.ObservedAtUtc.ToString("O")
                        }).ToArray(),
                    ["devices"] = monitorSnapshot.Devices.Select(
                        static device => new Dictionary<string, object?>
                        {
                            ["sourceId"] = device.SourceId,
                            ["displayName"] = device.DisplayName,
                            ["deviceId"] = device.DeviceId,
                            ["sessionFamily"] = device.SessionFamily,
                            ["connected"] = device.Connected,
                            ["busy"] = device.Busy,
                            ["liveActive"] = device.LiveActive,
                            ["isCriticalControl"] = device.IsCriticalControl,
                            ["statusMessage"] = device.StatusMessage,
                            ["lastError"] = device.LastError,
                            ["lastObservedAt"] = FormatTimestamp(device.LastObservedAtUtc)
                        }).ToArray()
                });
            artifactPaths[monitorSnapshotId] = monitorSnapshotPath;
            outputArtifactIds.Add(monitorSnapshotId);
        }

        if (derivedStateSnapshot is not null)
        {
            var derivedSnapshotId = new ArtifactId($"artifact.flow_reynolds_snapshot.{runKey}");
            var derivedSnapshotPath = Path.Combine(runDirectoryPath, "flow-reynolds-snapshot.yaml");
            WriteYaml(
                derivedSnapshotPath,
                new Dictionary<string, object?>
                {
                    ["kind"] = "flow_reynolds_snapshot",
                    ["runId"] = snapshot.RunId.ToString(),
                    ["observedAt"] = FormatTimestamp(derivedStateSnapshot.ObservedAtUtc),
                    ["pulseObservedAt"] = FormatTimestamp(derivedStateSnapshot.PulseObservedAtUtc),
                    ["pulseTimestampSeconds"] = derivedStateSnapshot.PulseTimestampSeconds,
                    ["latestPulseCount"] = derivedStateSnapshot.LatestPulseCount,
                    ["temperatureObservedAt"] = FormatTimestamp(derivedStateSnapshot.TemperatureObservedAtUtc),
                    ["rawFlowRateLitersPerMinute"] = derivedStateSnapshot.RawFlowRateLitersPerMinute,
                    ["filteredFlowRateLitersPerMinute"] = derivedStateSnapshot.FilteredFlowRateLitersPerMinute,
                    ["meanTemperatureC"] = derivedStateSnapshot.MeanTemperatureC,
                    ["temperatureDeltaC"] = derivedStateSnapshot.TemperatureDeltaC,
                    ["bulkVelocityMetersPerSecond"] = derivedStateSnapshot.BulkVelocityMetersPerSecond,
                    ["reynoldsNumber"] = derivedStateSnapshot.ReynoldsNumber,
                    ["usesFallbackTemperature"] = derivedStateSnapshot.UsesFallbackTemperature,
                    ["pulseTelemetryIsStale"] = derivedStateSnapshot.PulseTelemetryIsStale,
                    ["temperatureIsStale"] = derivedStateSnapshot.TemperatureIsStale,
                    ["sampleSequence"] = derivedStateSnapshot.SampleSequence,
                    ["statusMessage"] = derivedStateSnapshot.StatusMessage
                });
            artifactPaths[derivedSnapshotId] = derivedSnapshotPath;
            outputArtifactIds.Add(derivedSnapshotId);
        }

        if (derivedStateSamples is not null && derivedStateSamples.Count > 0)
        {
            var derivedRecordId = new ArtifactId($"artifact.flow_reynolds_record.{runKey}");
            var derivedRecordPath = Path.Combine(runDirectoryPath, "flow-reynolds-record.yaml");
            WriteYaml(
                derivedRecordPath,
                new Dictionary<string, object?>
                {
                    ["kind"] = "flow_reynolds_record",
                    ["runId"] = snapshot.RunId.ToString(),
                    ["samples"] = derivedStateSamples.Select(
                        static sample => new Dictionary<string, object?>
                        {
                            ["observedAt"] = sample.ObservedAtUtc.ToString("O"),
                            ["rawFlowRateLitersPerMinute"] = sample.RawFlowRateLitersPerMinute,
                            ["filteredFlowRateLitersPerMinute"] = sample.FilteredFlowRateLitersPerMinute,
                            ["meanTemperatureC"] = sample.MeanTemperatureC,
                            ["temperatureDeltaC"] = sample.TemperatureDeltaC,
                            ["bulkVelocityMetersPerSecond"] = sample.BulkVelocityMetersPerSecond,
                            ["reynoldsNumber"] = sample.ReynoldsNumber,
                            ["usesFallbackTemperature"] = sample.UsesFallbackTemperature,
                            ["pulseTelemetryIsStale"] = sample.PulseTelemetryIsStale,
                            ["temperatureIsStale"] = sample.TemperatureIsStale
                        }).ToArray()
                });
            artifactPaths[derivedRecordId] = derivedRecordPath;
            outputArtifactIds.Add(derivedRecordId);
        }

        var usedPanelSlugs = new HashSet<string>(StringComparer.Ordinal);
        foreach (var panelSnapshot in panelSnapshots)
        {
            var panelSlug = AllocatePanelSlug(panelSnapshot.PanelTitle, usedPanelSlugs);
            var artifactId = new ArtifactId($"artifact.panel_snapshot.{panelSlug}.{runKey}");
            var path = Path.Combine(panelsDirectoryPath, $"{panelSlug}.yaml");
            WriteYaml(path, BuildPanelDocument(panelSnapshot));
            artifactPaths[artifactId] = path;
            outputArtifactIds.Add(artifactId);
        }

        var manifestId = new ArtifactId($"artifact.run_manifest.{runKey}");
        var manifestPath = Path.Combine(runDirectoryPath, "manifest.yaml");
        artifactPaths[manifestId] = manifestPath;
        var manifest = new RunManifestDefinition(
            manifestId,
            resolvedExperiment.Experiment.Id,
            resolvedExperiment.Version,
            resolvedExperiment.RoleBindings.ToDictionary(static binding => binding.RoleId, static binding => binding.DeviceId),
            resolvedExperiment.RoleBindings.ToDictionary(static binding => binding.RoleId, static binding => binding.ProtocolId),
            resolvedExperiment.RoleBindings.ToDictionary(
                static binding => binding.RoleId,
                static binding => (IReadOnlyDictionary<ArtifactId, string>)binding.ParameterValues),
            resolvedExperiment.ParameterValues,
            snapshot.StartedAtUtc ?? DateTimeOffset.UtcNow,
            snapshot.StoppedAtUtc,
            activatedStopConditionId: null,
            stopReason: snapshot.StopReason is null ? null : $"{snapshot.StopReason.Code}: {snapshot.StopReason.Message}",
            outputArtifactIds,
            runtimeEvents.ToArray(),
            warningsOrFaults.ToArray(),
            artifactPaths.ToDictionary(
                entry => entry.Key,
                entry => NormalizeRelativePath(Path.GetRelativePath(runDirectoryPath, entry.Value))));

        WriteYaml(
            manifestPath,
            BuildManifestDocument(manifest));

        return new RunRecordingResult(
            snapshot.RunId,
            runDirectoryPath,
            manifestId,
            manifestPath,
            manifest,
            artifactPaths);
    }

    private string BuildRunDirectoryPath(RuntimeRunContext snapshot)
    {
        var timestamp = snapshot.StartedAtUtc ?? DateTimeOffset.UtcNow;
        var folderName = $"{timestamp:yyyyMMdd-HHmmss}_{snapshot.RunId:N}";
        return Path.Combine(_rootDirectory, folderName);
    }

    private static IReadOnlyList<PanelSnapshot> CapturePanelSnapshots(IReadOnlyList<IDeviceTestPanelViewModel> panels)
    {
        var snapshots = new List<PanelSnapshot>();
        foreach (var panel in panels)
        {
            if (panel is not IIntegrationPanelViewModel integrationPanel)
            {
                continue;
            }

            snapshots.Add(new PanelSnapshot(
                panel.Title,
                integrationPanel.DataOutput,
                integrationPanel.AppliedSettingsOutput,
                integrationPanel.StatusOutput,
                integrationPanel.DiagnosticsOutput,
                integrationPanel.SessionEndOutput));
        }

        return snapshots;
    }

    private static IReadOnlyList<string> CollectWarnings(RuntimeRunContext snapshot, IReadOnlyList<PanelSnapshot> panelSnapshots)
    {
        var warnings = new List<string>();
        if (snapshot.StopReason is not null && !string.Equals(snapshot.StopReason.Code, "UserRequested", StringComparison.Ordinal))
        {
            warnings.Add($"{snapshot.StopReason.Code}: {snapshot.StopReason.Message}");
        }

        foreach (var panelSnapshot in panelSnapshots)
        {
            if (!string.IsNullOrWhiteSpace(panelSnapshot.DiagnosticsOutput?.LastError))
            {
                warnings.Add($"{panelSnapshot.PanelTitle}: {panelSnapshot.DiagnosticsOutput.LastError}");
            }

            if (!string.IsNullOrWhiteSpace(panelSnapshot.StatusOutput?.FaultState))
            {
                warnings.Add($"{panelSnapshot.PanelTitle}: FaultState={panelSnapshot.StatusOutput.FaultState}");
            }

            if (panelSnapshot.SessionEndOutput?.OpenIssues is not null)
            {
                warnings.AddRange(
                    panelSnapshot.SessionEndOutput.OpenIssues
                        .Where(static issue => !string.IsNullOrWhiteSpace(issue))
                        .Select(issue => $"{panelSnapshot.PanelTitle}: {issue!.Trim()}"));
            }
        }

        return warnings;
    }

    private static string AllocatePanelSlug(string panelTitle, ISet<string> usedPanelSlugs)
    {
        var baseSlug = Slugify(panelTitle, "panel");
        var slug = baseSlug;
        var suffix = 2;
        while (!usedPanelSlugs.Add(slug))
        {
            slug = $"{baseSlug}_{suffix++}";
        }

        return slug;
    }

    private static Dictionary<string, object?> BuildManifestDocument(RunManifestDefinition manifest)
    {
        return new Dictionary<string, object?>
        {
            ["id"] = manifest.Id.Value,
            ["experimentId"] = manifest.ExperimentId.Value,
            ["experimentVersion"] = manifest.ExperimentVersion,
            ["startedAt"] = FormatTimestamp(manifest.StartedAt),
            ["stoppedAt"] = FormatTimestamp(manifest.StoppedAt),
            ["stopReason"] = manifest.StopReason,
            ["activatedStopConditionId"] = manifest.ActivatedStopConditionId?.Value,
            ["roleBindings"] = manifest.RoleBindings.ToDictionary(entry => entry.Key.Value, entry => entry.Value.Value),
            ["protocolBindings"] = manifest.ProtocolBindings.ToDictionary(entry => entry.Key.Value, entry => entry.Value.Value),
            ["roleBindingParameterValues"] = manifest.RoleBindingParameterValues.ToDictionary(
                entry => entry.Key.Value,
                entry => (object?)entry.Value.ToDictionary(nested => nested.Key.Value, nested => nested.Value)),
            ["parameterValues"] = manifest.ParameterValues.ToDictionary(entry => entry.Key.Value, entry => entry.Value),
            ["outputIds"] = manifest.OutputIds.Select(static artifactId => artifactId.Value).ToArray(),
            ["runtimeEvents"] = manifest.RuntimeEvents.ToArray(),
            ["warningsOrFaults"] = manifest.WarningsOrFaults.ToArray(),
            ["artifacts"] = manifest.Artifacts.ToDictionary(entry => entry.Key.Value, entry => entry.Value)
        };
    }

    private static Dictionary<string, object?> BuildPanelDocument(PanelSnapshot snapshot)
    {
        return new Dictionary<string, object?>
        {
            ["kind"] = "panel_snapshot",
            ["panelTitle"] = snapshot.PanelTitle,
            ["dataOutput"] = snapshot.DataOutput is null ? null : new Dictionary<string, object?>
            {
                ["timestamp"] = FormatTimestamp(snapshot.DataOutput.Timestamp),
                ["deviceId"] = snapshot.DataOutput.DeviceId,
                ["endpointId"] = snapshot.DataOutput.EndpointId,
                ["payloadType"] = snapshot.DataOutput.PayloadType,
                ["payloadValue"] = snapshot.DataOutput.PayloadValue,
                ["units"] = snapshot.DataOutput.Units,
                ["sequenceNumber"] = snapshot.DataOutput.SequenceNumber,
                ["captureRate"] = snapshot.DataOutput.CaptureRate,
                ["sourceMode"] = snapshot.DataOutput.SourceMode,
                ["outputEmissionMode"] = snapshot.DataOutput.OutputEmissionMode,
                ["outputFrequencyHz"] = snapshot.DataOutput.OutputFrequencyHz,
                ["metadataIncluded"] = snapshot.DataOutput.MetadataIncluded
            },
            ["appliedSettingsOutput"] = snapshot.AppliedSettingsOutput is null ? null : new Dictionary<string, object?>
            {
                ["appliedAt"] = FormatTimestamp(snapshot.AppliedSettingsOutput.AppliedAt),
                ["deviceSettings"] = snapshot.AppliedSettingsOutput.DeviceSettings,
                ["endpointSettings"] = snapshot.AppliedSettingsOutput.EndpointSettings,
                ["sessionSettings"] = snapshot.AppliedSettingsOutput.SessionSettings,
                ["normalizationNotes"] = snapshot.AppliedSettingsOutput.NormalizationNotes
            },
            ["statusOutput"] = snapshot.StatusOutput is null ? null : new Dictionary<string, object?>
            {
                ["connected"] = snapshot.StatusOutput.Connected,
                ["readyState"] = snapshot.StatusOutput.ReadyState,
                ["faultState"] = snapshot.StatusOutput.FaultState,
                ["liveState"] = snapshot.StatusOutput.LiveState,
                ["selectedEndpoint"] = snapshot.StatusOutput.SelectedEndpoint,
                ["backgroundActiveEndpoints"] = snapshot.StatusOutput.BackgroundActiveEndpoints
            },
            ["diagnosticsOutput"] = snapshot.DiagnosticsOutput is null ? null : new Dictionary<string, object?>
            {
                ["lastCommand"] = snapshot.DiagnosticsOutput.LastCommand,
                ["lastHardwareResponse"] = snapshot.DiagnosticsOutput.LastHardwareResponse,
                ["lastError"] = snapshot.DiagnosticsOutput.LastError,
                ["lastStateTransition"] = snapshot.DiagnosticsOutput.LastStateTransition,
                ["lastValidationResult"] = snapshot.DiagnosticsOutput.LastValidationResult
            },
            ["sessionEndOutput"] = snapshot.SessionEndOutput is null ? null : new Dictionary<string, object?>
            {
                ["endedAt"] = FormatTimestamp(snapshot.SessionEndOutput.EndedAt),
                ["exitReason"] = snapshot.SessionEndOutput.ExitReason,
                ["connectionClosed"] = snapshot.SessionEndOutput.ConnectionClosed,
                ["liveStopped"] = snapshot.SessionEndOutput.LiveStopped,
                ["appliedSettingsSnapshot"] = snapshot.SessionEndOutput.AppliedSettingsSnapshot is null ? null : new Dictionary<string, object?>
                {
                    ["appliedAt"] = FormatTimestamp(snapshot.SessionEndOutput.AppliedSettingsSnapshot.AppliedAt),
                    ["deviceSettings"] = snapshot.SessionEndOutput.AppliedSettingsSnapshot.DeviceSettings,
                    ["endpointSettings"] = snapshot.SessionEndOutput.AppliedSettingsSnapshot.EndpointSettings,
                    ["sessionSettings"] = snapshot.SessionEndOutput.AppliedSettingsSnapshot.SessionSettings,
                    ["normalizationNotes"] = snapshot.SessionEndOutput.AppliedSettingsSnapshot.NormalizationNotes
                },
                ["finalStatus"] = snapshot.SessionEndOutput.FinalStatus is null ? null : new Dictionary<string, object?>
                {
                    ["connected"] = snapshot.SessionEndOutput.FinalStatus.Connected,
                    ["readyState"] = snapshot.SessionEndOutput.FinalStatus.ReadyState,
                    ["faultState"] = snapshot.SessionEndOutput.FinalStatus.FaultState,
                    ["liveState"] = snapshot.SessionEndOutput.FinalStatus.LiveState,
                    ["selectedEndpoint"] = snapshot.SessionEndOutput.FinalStatus.SelectedEndpoint,
                    ["backgroundActiveEndpoints"] = snapshot.SessionEndOutput.FinalStatus.BackgroundActiveEndpoints
                },
                ["openIssues"] = snapshot.SessionEndOutput.OpenIssues
            }
        };
    }

    private void WriteYaml(string path, object document)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, _serializer.Serialize(document));
    }

    private static string Slugify(string? value, string fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        var characters = value
            .Trim()
            .ToLowerInvariant()
            .Select(static character => char.IsLetterOrDigit(character) ? character : '_')
            .ToArray();
        var slug = new string(characters);
        while (slug.Contains("__", StringComparison.Ordinal))
        {
            slug = slug.Replace("__", "_", StringComparison.Ordinal);
        }

        slug = slug.Trim('_');
        return string.IsNullOrWhiteSpace(slug) ? fallback : slug;
    }

    private static string? FormatTimestamp(DateTimeOffset? value) =>
        value?.ToString("O");

    private static string NormalizeRelativePath(string value) =>
        value.Replace('\\', '/');

    private sealed record PanelSnapshot(
        string PanelTitle,
        IntegrationPanelDataOutput? DataOutput,
        IntegrationPanelAppliedSettingsOutput? AppliedSettingsOutput,
        IntegrationPanelStatusOutput? StatusOutput,
        IntegrationPanelDiagnosticsOutput? DiagnosticsOutput,
        IntegrationPanelSessionEndOutput? SessionEndOutput);
}
