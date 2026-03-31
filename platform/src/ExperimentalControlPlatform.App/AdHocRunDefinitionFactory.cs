using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ExperimentalControlPlatform.App.DevicePanels;
using ExperimentalControlPlatform.App.DevicePanels.Contracts;
using ExperimentalControlPlatform.Core.Artifacts;

namespace ExperimentalControlPlatform.App;

public sealed class AdHocRunDefinitionFactory
{
    public ResolvedExperimentDefinition Create(IEnumerable<IDeviceTestPanelViewModel> panels)
    {
        return Create(panels, primaryControlTargetValue: null);
    }

    public ResolvedExperimentDefinition Create(IEnumerable<IDeviceTestPanelViewModel> panels, double? primaryControlTargetValue)
    {
        ArgumentNullException.ThrowIfNull(panels);

        var panelEntries = panels
            .Select((panel, index) => panel is IIntegrationPanelViewModel integrationPanel
                ? BuildPanelEntry(panel, integrationPanel, index)
                : null)
            .Where(static entry => entry is not null)
            .Cast<PanelEntry>()
            .ToArray();

        if (panelEntries.Length == 0)
        {
            panelEntries =
            [
                BuildFallbackPanelEntry()
            ];
        }

        var parameters = new List<ParameterDefinition>();
        var streams = new List<StreamDefinition>();
        var controlTargets = new List<ControlTargetDefinition>();
        var parameterValues = new Dictionary<ArtifactId, string>();

        var controlCenterRole = panelEntries
            .Select(static entry => entry.Role)
            .FirstOrDefault(static role => role.Id == new ArtifactId("role.control_center"));
        if (controlCenterRole is not null)
        {
            parameters.Add(new ParameterDefinition(
                FlowReynoldsArtifactIds.PulsesPerLiterParameterId,
                "Pulses Per Liter",
                "integer",
                "experiment",
                defaultValue: "80"));
            parameters.Add(new ParameterDefinition(
                FlowReynoldsArtifactIds.PipeInnerDiameterParameterId,
                "Pipe Inner Diameter",
                "float",
                "experiment",
                unit: "m",
                defaultValue: "0.00403"));
            parameters.Add(new ParameterDefinition(
                FlowReynoldsArtifactIds.PipeLengthParameterId,
                "Pipe Length",
                "float",
                "experiment",
                unit: "m",
                defaultValue: "1.0"));
            parameters.Add(new ParameterDefinition(
                FlowReynoldsArtifactIds.PipeRoughnessParameterId,
                "Pipe Roughness",
                "float",
                "experiment",
                unit: "m",
                defaultValue: "0.0"));
            parameters.Add(new ParameterDefinition(
                FlowReynoldsArtifactIds.ReferenceTemperatureParameterId,
                "Reference Temperature",
                "float",
                "experiment",
                unit: "C",
                defaultValue: "20.95"));
            parameters.Add(new ParameterDefinition(
                FlowReynoldsArtifactIds.FlowrateAverageCountParameterId,
                "Flowrate Average Count",
                "integer",
                "experiment",
                defaultValue: "100"));
            parameters.Add(new ParameterDefinition(
                FlowReynoldsArtifactIds.PulsePollIntervalMillisecondsParameterId,
                "Pulse Poll Interval",
                "integer",
                "experiment",
                unit: "ms",
                defaultValue: "500"));
            streams.Add(new StreamDefinition(
                FlowReynoldsArtifactIds.FlowRateStreamId,
                "Flow Rate",
                "transform",
                "scalar<double>",
                "runtime"));
            streams.Add(new StreamDefinition(
                FlowReynoldsArtifactIds.ReynoldsNumberStreamId,
                "Reynolds Number",
                "transform",
                "scalar<double>",
                "runtime"));
            streams.Add(new StreamDefinition(
                FlowReynoldsArtifactIds.MeanTemperatureStreamId,
                "Mean Temperature",
                "transform",
                "scalar<double>",
                "runtime"));
            streams.Add(new StreamDefinition(
                FlowReynoldsArtifactIds.TemperatureDeltaStreamId,
                "Temperature Delta",
                "transform",
                "scalar<double>",
                "runtime"));
        }

        if (primaryControlTargetValue.HasValue && controlCenterRole is not null)
        {
            parameters.Add(new ParameterDefinition(
                FlowReynoldsArtifactIds.PrimaryControlTargetParameterId,
                "Re Target",
                "float",
                "experiment",
                unit: "dimensionless"));
            controlTargets.Add(new ControlTargetDefinition(
                FlowReynoldsArtifactIds.PrimaryControlTargetId,
                "Primary Re Control",
                "Maintains a primary Reynolds-number target for ad hoc runtime experiments.",
                FlowReynoldsArtifactIds.ReynoldsNumberStreamId,
                controlCenterRole.Id,
                "constant",
                "closed_loop",
                targetParameterId: FlowReynoldsArtifactIds.PrimaryControlTargetParameterId));
            parameterValues[FlowReynoldsArtifactIds.PrimaryControlTargetParameterId] = primaryControlTargetValue.Value.ToString("0.###", CultureInfo.InvariantCulture);
        }

        var experiment = new ExperimentDefinition(
            new ArtifactId("exp.ad_hoc_runtime_run"),
            "Ad Hoc Runtime Run",
            "Fallback experiment package synthesized from active integration panels when no authored experiment package is attached to the runtime shell.",
            panelEntries.Select(static entry => entry.Role).ToArray(),
            parameters,
            streams,
            [],
            [],
            [],
            [],
            controlTargets);

        return new ResolvedExperimentDefinition(
            experiment,
            "ad-hoc.v1",
            panelEntries.Select(static entry => entry.Device).ToArray(),
            panelEntries.Select(static entry => entry.Binding).ToArray(),
            parameterValues);
    }

    private static PanelEntry BuildPanelEntry(
        IDeviceTestPanelViewModel panel,
        IIntegrationPanelViewModel integrationPanel,
        int index)
    {
        var titleSlug = Slugify(panel.Title, $"panel_{index + 1}");
        var deviceIdentity = integrationPanel.DataOutput?.DeviceId;
        var deviceSlug = Slugify(deviceIdentity, titleSlug);
        var capabilityId = DetermineCapabilityId(integrationPanel);
        var protocolId = new ArtifactId($"protocol.runtime_session.{deviceSlug}");
        var roleId = new ArtifactId($"role.{titleSlug}");
        var deviceId = new ArtifactId($"device.{deviceSlug}");
        var capability = new CapabilityDefinition(
            capabilityId,
            capabilityId.Value["cap.".Length..],
            $"Ad hoc capability inferred from panel '{panel.Title}'.");
        var protocol = new ProtocolDefinition(
            protocolId,
            $"{panel.Title} Runtime Session",
            "runtime",
            "snapshot-and-stream",
            "session",
            "best-effort",
            "fail-fast",
            "manual");
        var role = new DeviceRoleDefinition(
            roleId,
            panel.Title,
            $"Ad hoc role synthesized from panel '{panel.Title}'.",
            [capabilityId],
            protocolId);
        var device = new DeviceDefinition(
            deviceId,
            panel.Title,
            deviceIdentity?.Trim() ?? titleSlug,
            protocol,
            [capability],
            [],
            "unknown");
        var binding = new RoleBindingDefinition(
            roleId,
            deviceId,
            protocolId,
            [capabilityId]);

        return new PanelEntry(role, device, binding);
    }

    private static PanelEntry BuildFallbackPanelEntry()
    {
        var capabilityId = new ArtifactId("cap.integration_output");
        var protocolId = new ArtifactId("protocol.runtime_session.runtime_shell");
        var roleId = new ArtifactId("role.runtime_shell");
        var deviceId = new ArtifactId("device.runtime_shell");
        var capability = new CapabilityDefinition(
            capabilityId,
            "integration_output",
            "Fallback capability for a shell run with no active integration panels.");
        var protocol = new ProtocolDefinition(
            protocolId,
            "Runtime Shell Session",
            "runtime",
            "snapshot-and-stream",
            "session",
            "best-effort",
            "fail-fast",
            "manual");
        var role = new DeviceRoleDefinition(
            roleId,
            "Runtime Shell",
            "Fallback role for a shell run with no active integration panels.",
            [capabilityId],
            protocolId);
        var device = new DeviceDefinition(
            deviceId,
            "Runtime Shell",
            "runtime_shell",
            protocol,
            [capability],
            [],
            "unknown");
        var binding = new RoleBindingDefinition(
            roleId,
            deviceId,
            protocolId,
            [capabilityId]);

        return new PanelEntry(role, device, binding);
    }

    private static ArtifactId DetermineCapabilityId(IIntegrationPanelViewModel panel)
    {
        var payloadType = panel.DataOutput?.PayloadType;
        if (string.IsNullOrWhiteSpace(payloadType))
        {
            return new ArtifactId("cap.integration_output");
        }

        return NormalizePayloadType(payloadType) switch
        {
            "image" => new ArtifactId("cap.image"),
            "waveform" => new ArtifactId("cap.waveform"),
            "scalar" => new ArtifactId("cap.scalar"),
            "status" => new ArtifactId("cap.status"),
            "diagnostics" => new ArtifactId("cap.diagnostics"),
            "commandresult" => new ArtifactId("cap.command_result"),
            _ => new ArtifactId("cap.integration_output")
        };
    }

    private static string NormalizePayloadType(string payloadType) =>
        new string(payloadType.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();

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

    private sealed record PanelEntry(
        DeviceRoleDefinition Role,
        DeviceDefinition Device,
        RoleBindingDefinition Binding);
}
