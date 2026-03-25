using System;
using System.Globalization;
using System.Windows.Data;
using ExperimentalControlPlatform.App.DevicePanels.Contracts;

namespace ExperimentalControlPlatform.App.Widgets;

public sealed class LifecycleActionRowLabelConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value switch
        {
            IntegrationPanelLifecycleAction.Apply => "Apply",
            IntegrationPanelLifecycleAction.ApplyAndExit => "Apply and Exit",
            IntegrationPanelLifecycleAction.CloseWithoutApply => "Close Without Apply",
            IntegrationPanelLifecycleAction.DisconnectAndClose => "Disconnect and Close",
            _ => string.Empty,
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
