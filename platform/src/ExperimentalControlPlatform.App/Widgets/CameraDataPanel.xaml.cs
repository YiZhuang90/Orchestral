using System.Windows;
using System.Windows.Controls;

namespace ExperimentalControlPlatform.App.Widgets;

public partial class CameraDataPanel : UserControl
{
    public CameraDataPanel()
    {
        InitializeComponent();
    }

    public object? HeaderContent
    {
        get => GetValue(HeaderContentProperty);
        set => SetValue(HeaderContentProperty, value);
    }

    public static readonly DependencyProperty HeaderContentProperty =
        DependencyProperty.Register(nameof(HeaderContent), typeof(object), typeof(CameraDataPanel), new PropertyMetadata(null));

    public object? ToolbarContent
    {
        get => GetValue(ToolbarContentProperty);
        set => SetValue(ToolbarContentProperty, value);
    }

    public static readonly DependencyProperty ToolbarContentProperty =
        DependencyProperty.Register(nameof(ToolbarContent), typeof(object), typeof(CameraDataPanel), new PropertyMetadata(null));

    public object? ImageContent
    {
        get => GetValue(ImageContentProperty);
        set => SetValue(ImageContentProperty, value);
    }

    public static readonly DependencyProperty ImageContentProperty =
        DependencyProperty.Register(nameof(ImageContent), typeof(object), typeof(CameraDataPanel), new PropertyMetadata(null));

    public object? MetadataContent
    {
        get => GetValue(MetadataContentProperty);
        set => SetValue(MetadataContentProperty, value);
    }

    public static readonly DependencyProperty MetadataContentProperty =
        DependencyProperty.Register(nameof(MetadataContent), typeof(object), typeof(CameraDataPanel), new PropertyMetadata(null));
}
