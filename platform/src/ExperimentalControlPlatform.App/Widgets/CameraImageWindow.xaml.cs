using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ExperimentalControlPlatform.App.Widgets;

public partial class CameraImageWindow : UserControl
{
    private bool _isDraggingRoi;
    private bool _isResizingRoi;
    private Point _dragStartPoint;
    private Rect _dragStartRoi;

    public event EventHandler? RoiEditStarted;
    public event EventHandler? RoiEditCompleted;

    public CameraImageWindow()
    {
        InitializeComponent();
    }

    public ImageSource? BackgroundImage
    {
        get => (ImageSource?)GetValue(BackgroundImageProperty);
        set => SetValue(BackgroundImageProperty, value);
    }

    public static readonly DependencyProperty BackgroundImageProperty =
        DependencyProperty.Register(nameof(BackgroundImage), typeof(ImageSource), typeof(CameraImageWindow), new PropertyMetadata(null, OnImageLayoutPropertyChanged));

    public ImageSource? PreviewImage
    {
        get => (ImageSource?)GetValue(PreviewImageProperty);
        set => SetValue(PreviewImageProperty, value);
    }

    public static readonly DependencyProperty PreviewImageProperty =
        DependencyProperty.Register(nameof(PreviewImage), typeof(ImageSource), typeof(CameraImageWindow), new PropertyMetadata(null, OnImageLayoutPropertyChanged));

    public string EmptyStateText
    {
        get => (string)GetValue(EmptyStateTextProperty);
        set => SetValue(EmptyStateTextProperty, value);
    }

    public static readonly DependencyProperty EmptyStateTextProperty =
        DependencyProperty.Register(nameof(EmptyStateText), typeof(string), typeof(CameraImageWindow), new PropertyMetadata("No frame captured"));

    public Visibility RoiPlaceholderVisible
    {
        get => (Visibility)GetValue(RoiPlaceholderVisibleProperty);
        set => SetValue(RoiPlaceholderVisibleProperty, value);
    }

    public static readonly DependencyProperty RoiPlaceholderVisibleProperty =
        DependencyProperty.Register(nameof(RoiPlaceholderVisible), typeof(Visibility), typeof(CameraImageWindow), new PropertyMetadata(Visibility.Collapsed));

    public Visibility RoiHandleVisibility
    {
        get => (Visibility)GetValue(RoiHandleVisibilityProperty);
        set => SetValue(RoiHandleVisibilityProperty, value);
    }

    public static readonly DependencyProperty RoiHandleVisibilityProperty =
        DependencyProperty.Register(nameof(RoiHandleVisibility), typeof(Visibility), typeof(CameraImageWindow), new PropertyMetadata(Visibility.Collapsed));

    public Visibility RoiLiveVisibility
    {
        get => (Visibility)GetValue(RoiLiveVisibilityProperty);
        set => SetValue(RoiLiveVisibilityProperty, value);
    }

    public static readonly DependencyProperty RoiLiveVisibilityProperty =
        DependencyProperty.Register(nameof(RoiLiveVisibility), typeof(Visibility), typeof(CameraImageWindow), new PropertyMetadata(Visibility.Collapsed));

    public Rect RoiRectNormalized
    {
        get => (Rect)GetValue(RoiRectNormalizedProperty);
        set => SetValue(RoiRectNormalizedProperty, value);
    }

    public static readonly DependencyProperty RoiRectNormalizedProperty =
        DependencyProperty.Register(
            nameof(RoiRectNormalized),
            typeof(Rect),
            typeof(CameraImageWindow),
            new FrameworkPropertyMetadata(
                new Rect(0.2, 0.2, 0.6, 0.6),
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnRoiLayoutPropertyChanged));

    private static void OnImageLayoutPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((CameraImageWindow)d).UpdateRoiOverlay();
    }

    private static void OnRoiLayoutPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((CameraImageWindow)d).UpdateRoiOverlay();
    }

    private void CameraImageRoot_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateRoiOverlay();
    }

    private void UpdateRoiOverlay()
    {
        if (RoiBorder is null || LiveRoiImage is null || OverlayCanvas is null)
        {
            return;
        }

        BaseImage.Source = BackgroundImage ?? PreviewImage;

        var contentRect = GetImageContentRect();
        if (contentRect.Width <= 0 || contentRect.Height <= 0)
        {
            return;
        }

        var normalized = RoiRectNormalized;
        if (RoiPlaceholderVisible != Visibility.Visible && RoiLiveVisibility != Visibility.Visible)
        {
            LiveRoiImage.Width = 0;
            LiveRoiImage.Height = 0;
            Canvas.SetLeft(LiveRoiImage, contentRect.X);
            Canvas.SetTop(LiveRoiImage, contentRect.Y);
            return;
        }

        if (normalized.IsEmpty || normalized.Width <= 0 || normalized.Height <= 0)
        {
            normalized = new Rect(0, 0, 1, 1);
        }

        var left = Math.Clamp(normalized.X, 0, 1);
        var top = Math.Clamp(normalized.Y, 0, 1);
        var width = Math.Clamp(normalized.Width, 0.05, 1 - left);
        var height = Math.Clamp(normalized.Height, 0.05, 1 - top);

        var roiLeft = contentRect.X + (contentRect.Width * left);
        var roiTop = contentRect.Y + (contentRect.Height * top);
        var roiWidth = contentRect.Width * width;
        var roiHeight = contentRect.Height * height;

        RoiBorder.Width = Math.Max(0, roiWidth);
        RoiBorder.Height = Math.Max(0, roiHeight);
        Canvas.SetLeft(RoiBorder, roiLeft);
        Canvas.SetTop(RoiBorder, roiTop);

        LiveRoiImage.Width = Math.Max(0, roiWidth);
        LiveRoiImage.Height = Math.Max(0, roiHeight);
        Canvas.SetLeft(LiveRoiImage, roiLeft);
        Canvas.SetTop(LiveRoiImage, roiTop);
        LiveRoiImage.Clip = null;

        PositionThumb(TopLeftThumb, roiLeft, roiTop);
        PositionThumb(TopRightThumb, roiLeft + roiWidth, roiTop);
        PositionThumb(BottomLeftThumb, roiLeft, roiTop + roiHeight);
        PositionThumb(BottomRightThumb, roiLeft + roiWidth, roiTop + roiHeight);

        TopMask.Width = contentRect.Width;
        TopMask.Height = Math.Max(0, roiTop - contentRect.Y);
        Canvas.SetLeft(TopMask, contentRect.X);
        Canvas.SetTop(TopMask, contentRect.Y);

        LeftMask.Width = Math.Max(0, roiLeft - contentRect.X);
        LeftMask.Height = roiHeight;
        Canvas.SetLeft(LeftMask, contentRect.X);
        Canvas.SetTop(LeftMask, roiTop);

        RightMask.Width = Math.Max(0, (contentRect.X + contentRect.Width) - (roiLeft + roiWidth));
        RightMask.Height = roiHeight;
        Canvas.SetLeft(RightMask, roiLeft + roiWidth);
        Canvas.SetTop(RightMask, roiTop);

        BottomMask.Width = contentRect.Width;
        BottomMask.Height = Math.Max(0, (contentRect.Y + contentRect.Height) - (roiTop + roiHeight));
        Canvas.SetLeft(BottomMask, contentRect.X);
        Canvas.SetTop(BottomMask, roiTop + roiHeight);
    }

    private Rect GetImageContentRect()
    {
        var hostWidth = Math.Max(0, ActualWidth - 32);
        var hostHeight = Math.Max(0, ActualHeight - 32);
        if (hostWidth <= 0 || hostHeight <= 0)
        {
            return Rect.Empty;
        }

        var source = BackgroundImage ?? PreviewImage;
        if (source is not BitmapSource bitmap || bitmap.PixelWidth <= 0 || bitmap.PixelHeight <= 0)
        {
            return new Rect(0, 0, hostWidth, hostHeight);
        }

        var scale = Math.Min(hostWidth / bitmap.PixelWidth, hostHeight / bitmap.PixelHeight);
        var width = bitmap.PixelWidth * scale;
        var height = bitmap.PixelHeight * scale;
        var x = (hostWidth - width) / 2;
        var y = (hostHeight - height) / 2;
        return new Rect(x, y, width, height);
    }

    private void RoiBorder_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (RoiPlaceholderVisible != Visibility.Visible)
        {
            return;
        }

        RoiEditStarted?.Invoke(this, EventArgs.Empty);
        _isDraggingRoi = true;
        _dragStartPoint = e.GetPosition(OverlayCanvas);
        _dragStartRoi = RoiRectNormalized;
        RoiBorder.CaptureMouse();
        e.Handled = true;
    }

    private void CameraImageRoot_OnMouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDraggingRoi || OverlayCanvas is null)
        {
            return;
        }

        var contentRect = GetImageContentRect();
        if (contentRect.Width <= 0 || contentRect.Height <= 0)
        {
            return;
        }

        var current = e.GetPosition(OverlayCanvas);
        var deltaX = (current.X - _dragStartPoint.X) / contentRect.Width;
        var deltaY = (current.Y - _dragStartPoint.Y) / contentRect.Height;

        var newX = Math.Clamp(_dragStartRoi.X + deltaX, 0, 1 - _dragStartRoi.Width);
        var newY = Math.Clamp(_dragStartRoi.Y + deltaY, 0, 1 - _dragStartRoi.Height);
        RoiRectNormalized = new Rect(newX, newY, _dragStartRoi.Width, _dragStartRoi.Height);
    }

    private void CameraImageRoot_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        EndRoiDrag();
    }

    private void CameraImageRoot_OnLostMouseCapture(object sender, MouseEventArgs e)
    {
        EndRoiDrag();
    }

    private void EndRoiDrag()
    {
        if (!_isDraggingRoi)
        {
            return;
        }

        _isDraggingRoi = false;
        if (RoiBorder.IsMouseCaptured)
        {
            RoiBorder.ReleaseMouseCapture();
        }

        RoiEditCompleted?.Invoke(this, EventArgs.Empty);
    }

    private void ResizeThumb_OnDragStarted(object sender, DragStartedEventArgs e)
    {
        RoiEditStarted?.Invoke(this, EventArgs.Empty);
        _isResizingRoi = true;
        _dragStartRoi = RoiRectNormalized;
    }

    private void TopLeftThumb_OnDragDelta(object sender, DragDeltaEventArgs e)
    {
        ResizeFromDelta(e.HorizontalChange, e.VerticalChange, moveLeft: true, moveTop: true);
    }

    private void TopRightThumb_OnDragDelta(object sender, DragDeltaEventArgs e)
    {
        ResizeFromDelta(e.HorizontalChange, e.VerticalChange, moveLeft: false, moveTop: true);
    }

    private void BottomLeftThumb_OnDragDelta(object sender, DragDeltaEventArgs e)
    {
        ResizeFromDelta(e.HorizontalChange, e.VerticalChange, moveLeft: true, moveTop: false);
    }

    private void BottomRightThumb_OnDragDelta(object sender, DragDeltaEventArgs e)
    {
        ResizeFromDelta(e.HorizontalChange, e.VerticalChange, moveLeft: false, moveTop: false);
    }

    private void ResizeThumb_OnDragCompleted(object sender, DragCompletedEventArgs e)
    {
        if (!_isResizingRoi)
        {
            return;
        }

        _isResizingRoi = false;
        RoiEditCompleted?.Invoke(this, EventArgs.Empty);
    }

    private void ResizeFromDelta(double horizontalChange, double verticalChange, bool moveLeft, bool moveTop)
    {
        var contentRect = GetImageContentRect();
        if (contentRect.Width <= 0 || contentRect.Height <= 0)
        {
            return;
        }

        var deltaX = horizontalChange / contentRect.Width;
        var deltaY = verticalChange / contentRect.Height;
        const double minSize = 0.05;

        var x = RoiRectNormalized.X;
        var y = RoiRectNormalized.Y;
        var width = RoiRectNormalized.Width;
        var height = RoiRectNormalized.Height;

        if (moveLeft)
        {
            var newX = Math.Clamp(x + deltaX, 0, x + width - minSize);
            width = (x + width) - newX;
            x = newX;
        }
        else
        {
            width = Math.Clamp(width + deltaX, minSize, 1 - x);
        }

        if (moveTop)
        {
            var newY = Math.Clamp(y + deltaY, 0, y + height - minSize);
            height = (y + height) - newY;
            y = newY;
        }
        else
        {
            height = Math.Clamp(height + deltaY, minSize, 1 - y);
        }

        RoiRectNormalized = new Rect(x, y, width, height);
    }

    private static void PositionThumb(FrameworkElement thumb, double centerX, double centerY)
    {
        Canvas.SetLeft(thumb, centerX - (thumb.Width / 2));
        Canvas.SetTop(thumb, centerY - (thumb.Height / 2));
    }
}
