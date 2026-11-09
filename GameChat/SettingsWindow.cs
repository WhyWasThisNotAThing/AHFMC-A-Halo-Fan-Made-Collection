using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace AHFMCChat;

public enum SettingsAction { None, Logout, DeleteMessages, DeleteAccount }

public sealed class SettingsWindow : Window
{
    public SettingsAction Result { get; private set; }

    public SettingsWindow(string username, string fingerprint, IReadOnlyList<string> blocked)
    {
        Title = "Settings";
        Width = 440;
        SizeToContent = SizeToContent.Height;
        ResizeMode = ResizeMode.NoResize;
        ShowInTaskbar = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = (Brush)Application.Current.FindResource("Bg");
        Foreground = (Brush)Application.Current.FindResource("Text");
        FontFamily = new FontFamily("Segoe UI");
        FontSize = 13;

        static TextBlock Label(string t) => new()
        {
            Text = t, Foreground = (Brush)Application.Current.FindResource("Muted"),
            Margin = new Thickness(0, 0, 0, 4), TextWrapping = TextWrapping.Wrap
        };

        var root = new StackPanel { Margin = new Thickness(20) };

        root.Children.Add(Label("Signed in as"));
        root.Children.Add(new TextBlock { Text = username, FontSize = 20, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 14) });

        root.Children.Add(Label("Your fingerprint (contacts can use it to check it's really you)"));
        root.Children.Add(new TextBox { Text = fingerprint, IsReadOnly = true, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 14) });

        root.Children.Add(new TextBlock
        {
            Text = "Privacy & security (all optional)", FontWeight = FontWeights.SemiBold, FontSize = 14,
            Margin = new Thickness(0, 0, 0, 8)
        });

        root.Children.Add(Label("Auto-lock after inactivity"));
        int[] mins = { 0, 5, 10, 30 };
        var lockBox = new ComboBox { Margin = new Thickness(0, 0, 0, 14) };
        foreach (var n in new[] { "Never", "5 minutes", "10 minutes", "30 minutes" }) lockBox.Items.Add(n);
        lockBox.SelectedIndex = Math.Max(0, Array.IndexOf(mins, LocalSettings.AutoLockMinutes));
        lockBox.SelectionChanged += (_, _) =>
        {
            if (lockBox.SelectedIndex < 0) return;
            LocalSettings.AutoLockMinutes = mins[lockBox.SelectedIndex];
            LocalSettings.Save();
        };
        root.Children.Add(lockBox);

        var warnBox = new CheckBox
        {
            Content = "Warn me about contacts I haven't verified",
            Foreground = (Brush)Application.Current.FindResource("Text"),
            IsChecked = LocalSettings.WarnUnverified,
            Margin = new Thickness(0, 0, 0, 14)
        };
        warnBox.Click += (_, _) => { LocalSettings.WarnUnverified = warnBox.IsChecked == true; LocalSettings.Save(); };
        root.Children.Add(warnBox);

        root.Children.Add(Label(blocked.Count == 0
            ? "Blocked users: none  (type /block name to block someone)"
            : "Blocked users: " + string.Join(", ", blocked) + "   (type /unblock name to undo)"));
        root.Children.Add(new Border { Height = 12 });

        Button Danger(string text, Action onClick)
        {
            var b = new Button { Content = text, Style = (Style)Application.Current.FindResource("DangerButton"), Margin = new Thickness(0, 0, 0, 8) };
            b.Click += (_, _) => onClick();
            return b;
        }

        root.Children.Add(Danger("Delete all my messages", () =>
        {
            if (MessageBox.Show(this, "Delete every message you have sent (lobby, parties and DMs)?\n\nDMs disappear for the other person too. This can't be undone.",
                    "Chat", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            { Result = SettingsAction.DeleteMessages; DialogResult = true; }
        }));

        root.Children.Add(Danger("Delete my account...", () =>
        {
            string? typed = InputDialog.Ask(this, "Delete account",
                "This permanently deletes your account, your keys and all your messages and parties. It cannot be undone.\n\nType DELETE to confirm:");
            if (typed == "DELETE") { Result = SettingsAction.DeleteAccount; DialogResult = true; }
        }));

        root.Children.Add(Danger("Log out", () =>
        {
            if (MessageBox.Show(this, "Log out of the chat?", "Chat", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            { Result = SettingsAction.Logout; DialogResult = true; }
        }));

        root.Children.Add(new Button { Content = "Close", IsCancel = true, Margin = new Thickness(0, 4, 0, 0) });
        Content = root;
    }
}
