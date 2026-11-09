using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace AHFMCChat;

public sealed class RulesWindow : Window
{
    public static bool Confirm(Window? owner)
    {
        var w = new RulesWindow(true);
        if (owner != null && owner.IsVisible) w.Owner = owner;
        else w.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        return w.ShowDialog() == true;
    }

    public static void View(Window? owner)
    {
        var w = new RulesWindow(false);
        if (owner != null && owner.IsVisible) w.Owner = owner;
        else w.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        w.ShowDialog();
    }

    RulesWindow(bool confirm)
    {
        Title = "Chat Rules";
        Width = 560; Height = 680; MinWidth = 380; MinHeight = 340;
        ShowInTaskbar = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = Res("Bg", "#0F1722");
        FontFamily = new FontFamily("Segoe UI");

        var grid = new Grid { Margin = new Thickness(22) };
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var text = new TextBlock
        {
            Text = LegalText.Rules.Replace("\r\n", "\n"),
            TextWrapping = TextWrapping.Wrap, FontSize = 13, LineHeight = 20,
            Foreground = Res("Text", "#E6EDF5")
        };
        grid.Children.Add(new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Content = text, Padding = new Thickness(0, 0, 12, 0) });

        var row = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 14, 0, 0) };
        Style? primary = Application.Current?.TryFindResource("Primary") as Style;
        if (confirm)
        {
            var no = new Button { Content = "Cancel", IsCancel = true, Padding = new Thickness(22, 6, 22, 6), Margin = new Thickness(0, 0, 10, 0) };
            var yes = new Button { Content = "I understand and agree", IsDefault = true, Padding = new Thickness(22, 6, 22, 6) };
            if (primary != null) yes.Style = primary;
            yes.Click += (_, _) => { DialogResult = true; };
            row.Children.Add(no); row.Children.Add(yes);
        }
        else
        {
            var close = new Button { Content = "Close", IsCancel = true, IsDefault = true, Padding = new Thickness(26, 6, 26, 6) };
            if (primary != null) close.Style = primary;
            row.Children.Add(close);
        }
        Grid.SetRow(row, 1);
        grid.Children.Add(row);
        Content = grid;
    }

    static Brush Res(string key, string fallback) =>
        Application.Current?.TryFindResource(key) as Brush ?? (Brush)new BrushConverter().ConvertFromString(fallback)!;
}
