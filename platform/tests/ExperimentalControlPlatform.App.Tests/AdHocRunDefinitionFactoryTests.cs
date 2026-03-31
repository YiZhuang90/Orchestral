using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Input;
using ExperimentalControlPlatform.App.DevicePanels;
using ExperimentalControlPlatform.App.DevicePanels.Contracts;
using ExperimentalControlPlatform.Core.Artifacts;
using Xunit;

namespace ExperimentalControlPlatform.App.Tests;

public sealed class AdHocRunDefinitionFactoryTests
{
    [Fact]
    public void Create_Returns_Experiment_That_Passes_Lint()
    {
        var factory = new AdHocRunDefinitionFactory();
        var experiment = factory.Create(
            [
                new FakeIntegrationPanel(
                    "Control Center",
                    new IntegrationPanelDataOutput
                    {
                        DeviceId = "control_center_01",
                        PayloadType = IntegrationPanelOutputPayloadType.CommandResult.ToString(),
                        PayloadValue = "PulseCount=42"
                    })
            ],
            primaryControlTargetValue: 1600).Experiment;

        var lint = experiment.Lint();

        Assert.True(lint.IsValid);
        Assert.Empty(lint.Issues);
    }

    private sealed class FakeIntegrationPanel : IDeviceTestPanelViewModel, IIntegrationPanelViewModel
    {
        public FakeIntegrationPanel(string title, IntegrationPanelDataOutput? dataOutput)
        {
            Title = title;
            DataOutput = dataOutput;
        }

        public event PropertyChangedEventHandler? PropertyChanged
        {
            add { }
            remove { }
        }

        public string Title { get; }

        public IntegrationPanelDataOutput? DataOutput { get; }

        public IntegrationPanelAppliedSettingsOutput? AppliedSettingsOutput => null;

        public IntegrationPanelStatusOutput? StatusOutput => null;

        public IntegrationPanelDiagnosticsOutput? DiagnosticsOutput => null;

        public IntegrationPanelSessionEndOutput? SessionEndOutput => null;

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
