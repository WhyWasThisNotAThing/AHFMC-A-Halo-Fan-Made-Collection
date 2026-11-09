using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace AHFMC;

public sealed class GameToastWindow : Window
{
    [DllImport("user32.dll")] private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
    [DllImport("user32.dll")] private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int value);
    private const int GWL_EXSTYLE = -20, WS_EX_TOOLWINDOW = 0x80, WS_EX_NOACTIVATE = 0x08000000;

    private readonly TextBlock _title = new() { FontSize = 15, FontWeight = FontWeights.SemiBold, Foreground = Brushes.White, TextTrimming = TextTrimming.CharacterEllipsis };
    private readonly TextBlock _body = new() { FontSize = 13, Foreground = new SolidColorBrush(Color.FromRgb(0xC3, 0xCE, 0xDC)), TextWrapping = TextWrapping.Wrap, MaxHeight = 56, Margin = new Thickness(0, 3, 0, 0) };
    public event Action? Clicked;

    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(6) };

    public GameToastWindow()
    {
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        ShowInTaskbar = false;
        ShowActivated = false;
        Focusable = false;
        Topmost = true;
        ResizeMode = ResizeMode.NoResize;
        SizeToContent = SizeToContent.WidthAndHeight;
        FontFamily = new FontFamily("Segoe UI Variable Display, Segoe UI");

        var accent = new Border { Width = 4, CornerRadius = new CornerRadius(2), Background = new SolidColorBrush(Color.FromRgb(0x5D, 0xB4, 0xFF)), Margin = new Thickness(0, 0, 12, 0) };
        var text = new StackPanel { Width = 300 };
        text.Children.Add(_title);
        text.Children.Add(_body);
        text.Children.Add(new TextBlock { Text = "Click to open the chat", FontSize = 11, Foreground = new SolidColorBrush(Color.FromRgb(0x6B, 0x82, 0x9C)), Margin = new Thickness(0, 6, 0, 0) });
        var row = new DockPanel();
        DockPanel.SetDock(accent, Dock.Left);
        row.Children.Add(accent);
        row.Children.Add(text);

        Content = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(0xF0, 0x0B, 0x12, 0x1E)),
            BorderBrush = new SolidColorBrush(Color.FromArgb(0x55, 0x8F, 0xE0, 0xFF)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(14, 12, 16, 12),
            Margin = new Thickness(8),
            Child = row,
            Cursor = System.Windows.Input.Cursors.Hand,
            Effect = new System.Windows.Media.Effects.DropShadowEffect { BlurRadius = 18, ShadowDepth = 2, Opacity = 0.6 }
        };

        MouseLeftButtonUp += (_, _) => { _timer.Stop(); Hide(); Clicked?.Invoke(); };
        MouseEnter += (_, _) => { if (IsVisible) { BeginAnimation(OpacityProperty, null); Opacity = 1; _timer.Stop(); } };
        MouseLeave += (_, _) => { if (IsVisible) { _timer.Stop(); _timer.Start(); } };
        _timer.Tick += (_, _) => { _timer.Stop(); FadeOut(); };
        SourceInitialized += (_, _) =>
        {
            var h = new WindowInteropHelper(this).Handle;
            SetWindowLong(h, GWL_EXSTYLE, GetWindowLong(h, GWL_EXSTYLE) | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE);
        };
        SizeChanged += (_, _) => Place();
    }

    private void Place()
    {
        Left = SystemParameters.PrimaryScreenWidth - ActualWidth - 16;
        Top = 16;
    }

    public void Pop(string title, string body)
    {
        _title.Text = title;
        _body.Text = body;
        _body.Visibility = string.IsNullOrWhiteSpace(body) ? Visibility.Collapsed : Visibility.Visible;
        BeginAnimation(OpacityProperty, null);
        Opacity = 1;
        if (!IsVisible) Show();
        Place();
        Topmost = false; Topmost = true;
        _timer.Stop();
        _timer.Start();
    }

    private void FadeOut()
    {
        var a = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(350));
        a.Completed += (_, _) => { if (!_timer.IsEnabled) Hide(); };
        BeginAnimation(OpacityProperty, a);
    }
}
