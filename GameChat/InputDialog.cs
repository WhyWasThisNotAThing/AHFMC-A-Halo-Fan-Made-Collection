using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace AHFMCChat;

public sealed class InputDialog : Window
{
    readonly TextBox _box = new();

    InputDialog(string title, string prompt)
    {
        Title = title;
        Width = 360;
        SizeToContent = SizeToContent.Height;
        ResizeMode = ResizeMode.NoResize;
        ShowInTaskbar = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = (Brush)Application.Current.FindResource("Bg");
        Foreground = (Brush)Application.Current.FindResource("Text");
        FontFamily = new FontFamily("Segoe UI");
        FontSize = 13;

        var ok = new Button
        {
            Content = "OK", IsDefault = true, MinWidth = 76, Margin = new Thickness(0, 0, 8, 0),
            Style = (Style)Application.Current.FindResource("Primary")
        };
        ok.Click += (_, _) => DialogResult = true;
        var cancel = new Button { Content = "Cancel", IsCancel = true, MinWidth = 76 };

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
            Margin = new Thickness(0, 14, 0, 0)
        };
        buttons.Children.Add(ok);
        buttons.Children.Add(cancel);

        var root = new StackPanel { Margin = new Thickness(18) };
        root.Children.Add(new TextBlock { Text = prompt, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 8) });
        root.Children.Add(_box);
        root.Children.Add(buttons);
        Content = root;

        Loaded += (_, _) => _box.Focus();
    }

    public static string? Ask(Window owner, string title, string prompt)
    {
        var d = new InputDialog(title, prompt) { Owner = owner };
        if (d.ShowDialog() != true) return null;
        string v = d._box.Text.Trim();
        return v.Length == 0 ? null : v;
    }
}
