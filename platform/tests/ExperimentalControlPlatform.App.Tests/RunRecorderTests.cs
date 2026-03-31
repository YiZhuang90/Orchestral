using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Input;
using ExperimentalControlPlatform.App.DevicePanels;
using ExperimentalControlPlatform.App.DevicePanels.Contracts;
using ExperimentalControlPlatform.Core.Artifacts;
using ExperimentalControlPlatform.Runtime;

namespace ExperimentalControlPlatform.App.Tests;

public sealed class RunRecorderTests
{
    [Fact]
    public void AdHocRunDefinitionFactory_Builds_Resolved_Experiment_From_Integration_Panels()
    {
        var factory = new AdHocRunDefinitionFactory();
        var panels = new IDeviceTestPanelViewModel[]
        {
            new FakeIntegrationPanel(
                "Integrated Camera",
                new IntegrationPanelDataOutput
                {
                    DeviceId = "camera_01",
                    PayloadType = IntegrationPanelOutputPayloadType.Image.ToString(),
                    PayloadValue = "640x480"
                }),
            new FakeIntegrationPanel(
                "PT-104",
                new IntegrationPanelDataOutput
                {
                    DeviceId = "pt104_01",
                    PayloadType = IntegrationPanelOutputPayloadType.Scalar.ToString(),
                    PayloadValue = "20.4"
                })
        };

        var resolved = factory.Create(panels);

        Assert.Equal("Ad Hoc Runtime Run", resolved.Experiment.Name);
        Assert.Equal("ad-hoc.v1", resolved.Version);
        Assert.Equal(2, resolved.RoleBindings.Count);
        Assert.Contains(resolved.Devices, device => device.Id == new ArtifactId("device.camera_01"));
        Assert.Contains(resolved.RoleBindings, binding => binding.RoleId == new ArtifactId("role.integrated_camera"));
        Assert.Contains(
            resolved.RoleBindings.Single(binding => binding.RoleId == new ArtifactId("role.pt_104")).SatisfiedCapabilityIds,
            capabilityId => capabilityId == new ArtifactId("cap.scalar"));
    }

    [Fact]
    public void AdHocRunDefinitionFactory_Adds_Primary_Control_Target_When_Target_Value_Is_Provided()
    {
        var factory = new AdHocRunDefinitionFactory();
        var panels = new IDeviceTestPanelViewModel[]
        {
            new FakeIntegrationPanel(
                "Control Center",
                new IntegrationPanelDataOutput
                {
                    DeviceId = "control_center_01",
                    PayloadType = IntegrationPanelOutputPayloadType.CommandResult.ToString(),
                    PayloadValue = "PulseCount=42"
                })
        };

        var resolved = factory.Create(panels, primaryControlTargetValue: 1600);

        var controlTarget = Assert.Single(resolved.ControlTargets);
        Assert.Equal("Primary Re Control", controlTarget.Name);
        Assert.Equal("constant", controlTarget.SetpointProfile);
        Assert.Equal("closed_loop", controlTarget.RegulationMode);
        Assert.True(resolved.TryGetParameterValue(new ArtifactId("param.re_target"), out var value));
        Assert.Equal("1600", value);
    }

    [Fact]
    public void AdHocRunDefinitionFactory_Adds_FlowReynolds_Streams_And_Default_Parameters_When_ControlCenter_Is_Present()
    {
        var factory = new AdHocRunDefinitionFactory();
        var panels = new IDeviceTestPanelViewModel[]
        {
            new FakeIntegrationPanel(
                "Control Center",
                new IntegrationPanelDataOutput
                {
                    DeviceId = "control_center_01",
                    PayloadType = IntegrationPanelOutputPayloadType.CommandResult.ToString(),
                    PayloadValue = "PulseCount=42"
                }),
            new FakeIntegrationPanel(
                "PT-104",
                new IntegrationPanelDataOutput
                {
                    DeviceId = "pt104_01",
                    PayloadType = IntegrationPanelOutputPayloadType.Scalar.ToString(),
                    PayloadValue = "20.4"
                })
        };

        var resolved = factory.Create(panels);

        Assert.Contains(resolved.Experiment.Streams, stream => stream.Id == FlowReynoldsArtifactIds.FlowRateStreamId);
        Assert.Contains(resolved.Experiment.Streams, stream => stream.Id == FlowReynoldsArtifactIds.ReynoldsNumberStreamId);
        Assert.Contains(resolved.Experiment.Streams, stream => stream.Id == FlowReynoldsArtifactIds.MeanTemperatureStreamId);
        Assert.Contains(resolved.Experiment.Parameters, parameter => parameter.Id == FlowReynoldsArtifactIds.PulsesPerLiterParameterId);
        Assert.Contains(resolved.Experiment.Parameters, parameter => parameter.Id == FlowReynoldsArtifactIds.PipeInnerDiameterParameterId);
        Assert.True(resolved.TryGetParameterValue(FlowReynoldsArtifactIds.ReferenceTemperatureParameterId, out var fallbackTemperature));
        Assert.Equal("20.95", fallbackTemperature);
        Assert.True(resolved.TryGetParameterValue(FlowReynoldsArtifactIds.PulsesPerLiterParameterId, out var pulsesPerLiter));
        Assert.Equal("80", pulsesPerLiter);
    }

    [Fact]
    public void RunArtifactWriter_Writes_Manifest_And_Panel_Snapshot_Artifacts()
    {
        var rootDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(rootDirectory);

        try
        {
            var writer = new RunArtifactWriter(rootDirectory);
            var runSnapshot = new RuntimeRunContext(
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                RunState.Idle,
                startedAtUtc: DateTimeOffset.Parse("2026-03-30T14:00:00+02:00"),
                stoppedAtUtc: DateTimeOffset.Parse("2026-03-30T14:05:00+02:00"),
                stopReason: StopReason.UserRequested("Operator stopped the run."));
            var panels = new[]
            {
                new FakeIntegrationPanel(
                    "Integrated Camera",
                    new IntegrationPanelDataOutput
                    {
                        Timestamp = DateTimeOffset.Parse("2026-03-30T14:04:58+02:00"),
                        DeviceId = "camera_01",
                        PayloadType = IntegrationPanelOutputPayloadType.Image.ToString(),
                        PayloadValue = "640x480",
                        SourceMode = "Live",
                        MetadataIncluded = true
                    },
                    new IntegrationPanelAppliedSettingsOutput
                    {
                        AppliedAt = DateTimeOffset.Parse("2026-03-30T14:00:05+02:00"),
                        DeviceSettings = new Dictionary<string, string?> { ["ExposureMs"] = "0.35" }
                    },
                    new IntegrationPanelStatusOutput
                    {
                        Connected = false,
                        ReadyState = "Idle"
                    },
                    new IntegrationPanelDiagnosticsOutput
                    {
                        LastCommand = "StopLive",
                        LastError = "Frame buffer overflow"
                    },
                    new IntegrationPanelSessionEndOutput
                    {
                        EndedAt = DateTimeOffset.Parse("2026-03-30T14:05:00+02:00"),
                        ExitReason = "Operator requested stop.",
                        ConnectionClosed = true,
                        LiveStopped = true
                    })
            };

            var result = writer.Write(
                runSnapshot,
                panels,
                [
                    "Run started.",
                    "Run stopped."
                ]);

            Assert.True(File.Exists(result.ManifestPath));
            Assert.Contains(result.ArtifactPaths.Values, path => path.EndsWith("runtime-events.yaml", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(result.ArtifactPaths.Values, path => path.EndsWith("warnings-or-faults.yaml", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(result.ArtifactPaths.Values, path => path.EndsWith(Path.Combine("panels", "integrated_camera.yaml"), StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(result.ArtifactPaths.Values, path => path.EndsWith("artifact-index.yaml", StringComparison.OrdinalIgnoreCase));

            var manifestYaml = File.ReadAllText(result.ManifestPath);
            Assert.Contains("experimentId: exp.ad_hoc_runtime_run", manifestYaml);
            Assert.Contains("stopReason: 'UserRequested: Operator stopped the run.'", manifestYaml);
            Assert.Contains("outputIds:", manifestYaml);
            Assert.Contains("artifacts:", manifestYaml);

            var panelSnapshotPath = result.ArtifactPaths.Single(entry => entry.Value.EndsWith(Path.Combine("panels", "integrated_camera.yaml"), StringComparison.OrdinalIgnoreCase)).Value;
            var panelYaml = File.ReadAllText(panelSnapshotPath);
            Assert.Contains("panelTitle: Integrated Camera", panelYaml);
            Assert.Contains("payloadValue: 640x480", panelYaml);
            Assert.Contains("lastError: Frame buffer overflow", panelYaml);
        }
        finally
        {
            if (Directory.Exists(rootDirectory))
            {
                Directory.Delete(rootDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public void RunArtifactWriter_Writes_Monitor_Snapshot_Artifact_And_Includes_Monitor_Warnings()
    {
        var rootDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(rootDirectory);

        try
        {
            var writer = new RunArtifactWriter(rootDirectory);
            var runSnapshot = new RuntimeRunContext(
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                RunState.Idle,
                startedAtUtc: DateTimeOffset.Parse("2026-03-30T14:00:00+02:00"),
                stoppedAtUtc: DateTimeOffset.Parse("2026-03-30T14:05:00+02:00"),
                stopReason: StopReason.UserRequested("Operator stopped the run."));
            var monitorSnapshot = new ExperimentMonitorSnapshot
            {
                RunId = runSnapshot.RunId,
                RunState = RunState.Idle,
                RunDisplayName = "transition-demo-001",
                HighestSeverity = ExperimentMonitorSeverity.Warning,
                WarningCount = 1,
                Items =
                [
                    new ExperimentMonitorItem(
                        "warn.controller_stale",
                        ExperimentMonitorSeverity.Warning,
                        "controller.re_primary",
                        "Measured value is stale.",
                        DateTimeOffset.Parse("2026-03-30T14:04:59+02:00"))
                ]
            };

            var result = writer.Write(runSnapshot, [], ["Run started.", "Run stopped."], monitorSnapshot);

            Assert.Contains(result.ArtifactPaths.Values, path => path.EndsWith("monitor-snapshot.yaml", StringComparison.OrdinalIgnoreCase));

            var warningsPath = result.ArtifactPaths.Single(entry => entry.Value.EndsWith("warnings-or-faults.yaml", StringComparison.OrdinalIgnoreCase)).Value;
            var warningsYaml = File.ReadAllText(warningsPath);
            Assert.Contains("controller.re_primary", warningsYaml);
            Assert.Contains("Measured value is stale.", warningsYaml);
        }
        finally
        {
            if (Directory.Exists(rootDirectory))
            {
                Directory.Delete(rootDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public void RunArtifactWriter_Writes_FlowReynolds_Derived_State_Artifacts()
    {
        var rootDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(rootDirectory);

        try
        {
            var writer = new RunArtifactWriter(rootDirectory);
            var runSnapshot = new RuntimeRunContext(
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                RunState.Idle,
                startedAtUtc: DateTimeOffset.Parse("2026-03-31T14:00:00+02:00"),
                stoppedAtUtc: DateTimeOffset.Parse("2026-03-31T14:05:00+02:00"),
                stopReason: StopReason.UserRequested("Operator stopped the run."));
            var derivedSnapshot = new FlowReynoldsDerivedStateSnapshot
            {
                ObservedAtUtc = DateTimeOffset.Parse("2026-03-31T14:04:59+02:00"),
                RawFlowRateLitersPerMinute = 1.24,
                FilteredFlowRateLitersPerMinute = 1.22,
                MeanTemperatureC = 20.95,
                TemperatureDeltaC = 0.12,
                BulkVelocityMetersPerSecond = 1.59,
                ReynoldsNumber = 2310.5,
                StatusMessage = "Derived Reynolds state ready."
            };
            var derivedSamples = new[]
            {
                new FlowReynoldsDerivedStateSample(
                    DateTimeOffset.Parse("2026-03-31T14:04:58+02:00"),
                    RawFlowRateLitersPerMinute: 1.23,
                    FilteredFlowRateLitersPerMinute: 1.21,
                    MeanTemperatureC: 20.94,
                    TemperatureDeltaC: 0.10,
                    BulkVelocityMetersPerSecond: 1.58,
                    ReynoldsNumber: 2308.4,
                    UsesFallbackTemperature: false,
                    PulseTelemetryIsStale: false,
                    TemperatureIsStale: false),
                new FlowReynoldsDerivedStateSample(
                    DateTimeOffset.Parse("2026-03-31T14:04:59+02:00"),
                    RawFlowRateLitersPerMinute: 1.24,
                    FilteredFlowRateLitersPerMinute: 1.22,
                    MeanTemperatureC: 20.95,
                    TemperatureDeltaC: 0.12,
                    BulkVelocityMetersPerSecond: 1.59,
                    ReynoldsNumber: 2310.5,
                    UsesFallbackTemperature: false,
                    PulseTelemetryIsStale: false,
                    TemperatureIsStale: false)
            };

            var result = writer.Write(
                runSnapshot,
                [],
                ["Run started.", "Run stopped."],
                derivedStateSnapshot: derivedSnapshot,
                derivedStateSamples: derivedSamples,
                monitorSnapshot: null);

            Assert.Contains(result.ArtifactPaths.Values, path => path.EndsWith("flow-reynolds-snapshot.yaml", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(result.ArtifactPaths.Values, path => path.EndsWith("flow-reynolds-record.yaml", StringComparison.OrdinalIgnoreCase));

            var snapshotPath = result.ArtifactPaths.Single(entry => entry.Value.EndsWith("flow-reynolds-snapshot.yaml", StringComparison.OrdinalIgnoreCase)).Value;
            var snapshotYaml = File.ReadAllText(snapshotPath);
            Assert.Contains("reynoldsNumber: 2310.5", snapshotYaml);
            Assert.Contains("meanTemperatureC: 20.95", snapshotYaml);

            var recordPath = result.ArtifactPaths.Single(entry => entry.Value.EndsWith("flow-reynolds-record.yaml", StringComparison.OrdinalIgnoreCase)).Value;
            var recordYaml = File.ReadAllText(recordPath);
            Assert.Contains("filteredFlowRateLitersPerMinute: 1.21", recordYaml);
            Assert.Contains("reynoldsNumber: 2310.5", recordYaml);
        }
        finally
        {
            if (Directory.Exists(rootDirectory))
            {
                Directory.Delete(rootDirectory, recursive: true);
            }
        }
    }

    private sealed class FakeIntegrationPanel : IDeviceTestPanelViewModel, IIntegrationPanelViewModel
    {
        public FakeIntegrationPanel(
            string title,
            IntegrationPanelDataOutput? dataOutput,
            IntegrationPanelAppliedSettingsOutput? appliedSettingsOutput = null,
            IntegrationPanelStatusOutput? statusOutput = null,
            IntegrationPanelDiagnosticsOutput? diagnosticsOutput = null,
            IntegrationPanelSessionEndOutput? sessionEndOutput = null)
        {
            Title = title;
            DataOutput = dataOutput;
            AppliedSettingsOutput = appliedSettingsOutput;
            StatusOutput = statusOutput;
            DiagnosticsOutput = diagnosticsOutput;
            SessionEndOutput = sessionEndOutput;
        }

        public event PropertyChangedEventHandler? PropertyChanged
        {
            add { }
            remove { }
        }

        public string Title { get; }

        public IntegrationPanelDataOutput? DataOutput { get; }

        public IntegrationPanelAppliedSettingsOutput? AppliedSettingsOutput { get; }

        public IntegrationPanelStatusOutput? StatusOutput { get; }

        public IntegrationPanelDiagnosticsOutput? DiagnosticsOutput { get; }

        public IntegrationPanelSessionEndOutput? SessionEndOutput { get; }

        public IReadOnlyList<IntegrationPanelLifecycleAction> SupportedLifecycleActions { get; } = [];

        public ICommand LifecycleActionCommand { get; } = new NoOpCommand();

        public void Dispose()
        {
        }
    }

    private sealed class NoOpCommand : ICommand
    {
        public event EventHandler? CanExecuteChanged
        {
            add { }
            remove { }
        }

        public bool CanExecute(object? parameter) => false;

        public void Execute(object? parameter)
        {
        }
    }
}
