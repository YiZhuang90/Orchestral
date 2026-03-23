using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ExperimentalControlPlatform.App.Shell;

public partial class DevicePanelShell : UserControl
{
    public DevicePanelShell()
    {
        InitializeComponent();
    }

    public string PanelTitle
    {
        get => (string)GetValue(PanelTitleProperty);
        set => SetValue(PanelTitleProperty, value);
    }

    public static readonly DependencyProperty PanelTitleProperty =
        DependencyProperty.Register(nameof(PanelTitle), typeof(string), typeof(DevicePanelShell), new PropertyMetadata(string.Empty));

    public object? TitleContent
    {
        get => GetValue(TitleContentProperty);
        set => SetValue(TitleContentProperty, value);
    }

    public static readonly DependencyProperty TitleContentProperty =
        DependencyProperty.Register(nameof(TitleContent), typeof(object), typeof(DevicePanelShell), new PropertyMetadata(null));

    public string ContextLabel
    {
        get => (string)GetValue(ContextLabelProperty);
        set => SetValue(ContextLabelProperty, value);
    }

    public static readonly DependencyProperty ContextLabelProperty =
        DependencyProperty.Register(nameof(ContextLabel), typeof(string), typeof(DevicePanelShell), new PropertyMetadata(string.Empty));

    public object? TabsSource
    {
        get => GetValue(TabsSourceProperty);
        set => SetValue(TabsSourceProperty, value);
    }

    public static readonly DependencyProperty TabsSourceProperty =
        DependencyProperty.Register(nameof(TabsSource), typeof(object), typeof(DevicePanelShell), new PropertyMetadata(null));

    public object? SelectedTab
    {
        get => GetValue(SelectedTabProperty);
        set => SetValue(SelectedTabProperty, value);
    }

    public static readonly DependencyProperty SelectedTabProperty =
        DependencyProperty.Register(nameof(SelectedTab), typeof(object), typeof(DevicePanelShell), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public DataTemplate? TabItemTemplate
    {
        get => (DataTemplate?)GetValue(TabItemTemplateProperty);
        set => SetValue(TabItemTemplateProperty, value);
    }

    public static readonly DependencyProperty TabItemTemplateProperty =
        DependencyProperty.Register(nameof(TabItemTemplate), typeof(DataTemplate), typeof(DevicePanelShell), new PropertyMetadata(null));

    public GridLength LeftRailWidth
    {
        get => (GridLength)GetValue(LeftRailWidthProperty);
        set => SetValue(LeftRailWidthProperty, value);
    }

    public static readonly DependencyProperty LeftRailWidthProperty =
        DependencyProperty.Register(nameof(LeftRailWidth), typeof(GridLength), typeof(DevicePanelShell), new PropertyMetadata(new GridLength(167)));

    public object? LeftRailContent
    {
        get => GetValue(LeftRailContentProperty);
        set => SetValue(LeftRailContentProperty, value);
    }

    public static readonly DependencyProperty LeftRailContentProperty =
        DependencyProperty.Register(nameof(LeftRailContent), typeof(object), typeof(DevicePanelShell), new PropertyMetadata(null));

    public object? WorkspaceContent
    {
        get => GetValue(WorkspaceContentProperty);
        set => SetValue(WorkspaceContentProperty, value);
    }

    public static readonly DependencyProperty WorkspaceContentProperty =
        DependencyProperty.Register(nameof(WorkspaceContent), typeof(object), typeof(DevicePanelShell), new PropertyMetadata(null));

    public object? FooterContent
    {
        get => GetValue(FooterContentProperty);
        set => SetValue(FooterContentProperty, value);
    }

    public static readonly DependencyProperty FooterContentProperty =
        DependencyProperty.Register(nameof(FooterContent), typeof(object), typeof(DevicePanelShell), new PropertyMetadata(null));

    public object? HeaderContent
    {
        get => GetValue(HeaderContentProperty);
        set => SetValue(HeaderContentProperty, value);
    }

    public static readonly DependencyProperty HeaderContentProperty =
        DependencyProperty.Register(nameof(HeaderContent), typeof(object), typeof(DevicePanelShell), new PropertyMetadata(null));

    private void TitleBar_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var window = Window.GetWindow(this);
        if (window is null)
        {
            return;
        }

        if (e.ClickCount == 2)
        {
            ToggleWindowState(window);
            return;
        }

        if (e.ButtonState == MouseButtonState.Pressed)
        {
            window.DragMove();
        }
    }

    private void MinimizeButton_OnClick(object sender, RoutedEventArgs e)
    {
        var window = Window.GetWindow(this);
        if (window is not null)
        {
            window.WindowState = WindowState.Minimized;
        }
    }

    private void MaximizeRestoreButton_OnClick(object sender, RoutedEventArgs e)
    {
        var window = Window.GetWindow(this);
        if (window is not null)
        {
            ToggleWindowState(window);
        }
    }

    private void CloseButton_OnClick(object sender, RoutedEventArgs e)
    {
        Window.GetWindow(this)?.Close();
    }

    private static void ToggleWindowState(Window window)
    {
        window.WindowState = window.WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }
}
