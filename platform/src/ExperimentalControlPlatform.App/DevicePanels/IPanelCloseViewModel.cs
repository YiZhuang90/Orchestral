using System;
using System.Threading.Tasks;

namespace ExperimentalControlPlatform.App.DevicePanels;

public interface IPanelCloseViewModel
{
    event EventHandler? CloseRequested;

    Task CloseWithoutApplyAsync();
}
