using System;

namespace ExperimentalControlPlatform.App.DevicePanels;

public interface IDeviceTestPanelViewModel : IDisposable
{
    string Title { get; }
}
