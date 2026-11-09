using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace AHFMCChat;

public sealed class LegalWindow : Window
{
    public static void Show(Window? owner, bool privacy)
    {
        var w = new LegalWindow(privacy);
        if (owner != null && owner.IsVisible) w.Owner = owner;
        else w.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        w.ShowDialog();
    }

    LegalWindow(bool privacy)
    {
        Title = privacy ? "Privacy Policy" : "Terms of Service";
        Width = 560; Height = 640; MinWidth = 380; MinHeight = 300;
        ShowInTaskbar = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = Res("Bg", "#0F1722");
        FontFamily = new FontFamily("Segoe UI");

        var grid = new Grid { Margin = new Thickness(22) };
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var text = new TextBlock
        {
            Text = (privacy ? LegalText.Privacy : LegalText.Terms).Replace("\r\n", "\n"),
            TextWrapping = TextWrapping.Wrap, FontSize = 13, LineHeight = 20,
            Foreground = Res("Text", "#E6EDF5")
        };
        grid.Children.Add(new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Content = text, Padding = new Thickness(0, 0, 12, 0) });

        var close = new Button { Content = "Close", IsCancel = true, IsDefault = true, Margin = new Thickness(0, 14, 0, 0), Padding = new Thickness(26, 6, 26, 6), HorizontalAlignment = HorizontalAlignment.Right };
        if (Application.Current?.TryFindResource("Primary") is Style st) close.Style = st;
        close.Click += (_, _) => Close();
        Grid.SetRow(close, 1);
        grid.Children.Add(close);
        Content = grid;
    }

    static Brush Res(string key, string fallback) =>
        Application.Current?.TryFindResource(key) as Brush ?? (Brush)new BrushConverter().ConvertFromString(fallback)!;
}
