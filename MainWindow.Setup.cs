using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace AHFMC;

public partial class MainWindow
{
    private bool SetupOpen => SetupPane.Visibility == Visibility.Visible;

    private sealed class SetupRowInfo { public Action<int> Change = _ => { }; }

    private readonly List<Action> _setupRefresh = new();
    private Button? _setupFirstRow;
    private int _setupLayout;
    private bool _setupFirstTime, _classicHiddenForSetup;

    private static readonly string[] LayoutHelp =
    {
        "The usual launcher: all your games on one screen.",
        "The original Halo CE menu. You bring your own background video and music.",
        "The usual launcher with a background video and music you choose yourself.",
        "A big, controller-friendly screen like Steam Big Picture.",
        "Big Picture with a background video and music you choose yourself."
    };

    private void ShowSetup(bool firstTime)
    {
        _setupFirstTime = firstTime;
        _setupLayout = Math.Clamp(_config.Layout, 0, LayoutNames.Length - 1);
        SetupTitle.Text = firstTime ? "Welcome" : "First-time setup";
        SetupIntro.Text = firstTime
            ? "Looks like this is your first time here. Pick how you want the launcher to look and a few settings, then press Start. You can change all of it later in Settings."
            : "Pick the layout you want and a few settings, then press Start. All of it is also in Settings.";
        BuildSetup();
        ShowMouse();

        if (ClassicPane.Visibility == Visibility.Visible)
        {
            _classicHiddenForSetup = true;
            ClassicPane.Visibility = Visibility.Collapsed;
            SetupPane.Background = Brushes.Black;
        }
        else SetupPane.Background = Br("#F2050810");

        SetupPane.Visibility = Visibility.Visible;
        SetupScroll.ScrollToTop();
        Dispatcher.BeginInvoke(new Action(() => (_setupFirstRow ?? SetupStart).Focus()), DispatcherPriority.Input);
    }

    private void RefreshSetup()
    {
        foreach (var r in _setupRefresh) r();
    }

    private void SetupStart_Click(object sender, RoutedEventArgs e) => FinishSetup();

    private void FinishSetup()
    {
        SetupPane.Visibility = Visibility.Collapsed;
        _config.SetupDone = true;
        SaveConfig();

        if (_classicHiddenForSetup)
        {
            _classicHiddenForSetup = false;
            ClassicPane.Visibility = Visibility.Visible;
            ClassicView.Focus();
        }

        if (_setupLayout != _config.Layout) SetLayout(_setupLayout);
        PushClassicState();
        RefreshSettings();

        if (_setupFirstTime)
        {
            _setupFirstTime = false;
            if (IsWebLayout(_config.Layout)) PushNews();
            else if (_newsLoaded && _config.ShowNewsOnStart && NewsHasNew()) { _newsDelivered = true; ShowNewsPane(); MarkNewsSeen(); return; }
        }
        if (!IsWebLayout(_config.Layout)) Dispatcher.BeginInvoke(new Action(() => FocusDefault()), DispatcherPriority.Input);
    }

    private void SetupPadMove(FocusNavigationDirection dir)
    {
        if (Keyboard.FocusedElement is Button { Tag: SetupRowInfo info } f && SetupPane.IsAncestorOf(f)
            && (dir == FocusNavigationDirection.Left || dir == FocusNavigationDirection.Right))
        {
            info.Change(dir == FocusNavigationDirection.Right ? 1 : -1);
            RefreshSetup();
            return;
        }
        MoveWithin(SetupPane, dir, SetupStart);
    }

    private void BuildSetup()
    {
        SetupList.Children.Clear();
        _setupRefresh.Clear();
        _setupFirstRow = null;

        void Heading(string text)
        {
            SetupList.Children.Add(new TextBlock
            {
                Text = text, FontSize = 14, FontWeight = FontWeights.Bold, Foreground = Br("#5F7690"),
                Margin = new Thickness(4, SetupList.Children.Count == 0 ? 0 : 18, 0, 10)
            });
        }

        Heading("LAYOUT");
        for (int i = 0; i < LayoutNames.Length; i++)
        {
            int idx = i;
            AddSetupRow(LayoutNames[i], LayoutHelp[i], () => _setupLayout == idx ? "●" : "○", _ => { }, kind: 2, click: () => { _setupLayout = idx; });
        }

        Heading("LAUNCHER");
        SetupChoice("Window", "Fullscreen covers the whole screen, taskbar included. Windowed is a normal window you can move and resize.",
            new[] { "Fullscreen", "Windowed" }, () => _config.WindowMode,
            v => { _config.WindowMode = v; SaveConfig(); ApplyFullscreen(); PushClassicState(); });
        SetupChoice("Launch countdown", "3, 2, 1, GO with sounds while the launcher checks for updates and gets the game ready.",
            new[] { "Off", "Fan games only", "All games" }, () => _config.Countdown,
            v => { _config.Countdown = v; SaveConfig(); PushClassicState(); });
        SetupToggle("Maximize game window", "Make the game window big after it starts. Turn this off to leave it as the game opens it.",
            () => _config.MaximizeGame, v => { _config.MaximizeGame = v; SaveConfig(); PushClassicState(); });
        SetupToggle("Hide games I don't own", "Leave out the games that are not installed.",
            () => _config.HideMissing, v => { _config.HideMissing = v; SaveConfig(); ApplyHideMissing(); PushClassicState(); });
        SetupToggle("Start screen", "Classic Halo CE menu: show \"Press Enter / A to start\" first, with only the music and the background.",
            () => _config.StartScreen, v => { _config.StartScreen = v; SaveConfig(); PushClassicState(); });
        SetupChoice("Hide cursor", "Hide the mouse pointer while you use a controller or the arrow keys, always, or never.",
            new[] { "With controller", "Always", "Never" }, () => _config.CursorMode,
            v => { _config.CursorMode = v; SaveConfig(); ApplyCursorMode(); PushClassicState(); });
        SetupToggle("Keep running in tray when I close the app", "Off: closing exits the launcher. On: it hides in the system tray and clicking the .exe again brings it back.",
            () => _config.CloseToTray, v => { _config.CloseToTray = v; SaveConfig(); PushClassicState(); });
        SetupToggle("Discord Rich Presence", "Show what you are playing on your Discord profile. Off: nothing is shown and the launcher does not connect to Discord.",
            () => _config.DiscordPresence, v => { _config.DiscordPresence = v; SaveConfig(); ApplyDiscordSetting(); PushClassicState(); });

        Heading("SOUND");
        SetupChoice("Master volume", "How loud everything is: the music, the menu sounds, the countdown and the chat ding.",
            new[] { "Muted", "10%", "20%", "30%", "40%", "50%", "60%", "70%", "80%", "90%", "100%" }, () => Math.Clamp((int)Math.Round(_config.MasterVolume / 10.0), 0, 10),
            v => { _config.MasterVolume = v * 10; VolumeChanged("count"); });

        Heading("NEWS");
        SetupToggle("Show what's new", "Open the news when the launcher starts and one of the projects below has a new release.",
            () => _config.ShowNewsOnStart, v => { _config.ShowNewsOnStart = v; SaveConfig(); PushClassicState(); });
        SetupToggle("Halo 3 Reclaimer news", "Include the Halo 3 Reclaimer release notes.",
            () => _config.NewsHalo3, v => { _config.NewsHalo3 = v; SaveConfig(); PushClassicState(); PushNews(false); });
        SetupToggle("Halo Reach Renaissance news", "Include the Halo Reach Renaissance release notes.",
            () => _config.NewsReach, v => { _config.NewsReach = v; SaveConfig(); PushClassicState(); PushNews(false); });
        SetupToggle("Open Community Edition news", "Include the Open Community Edition release notes.",
            () => _config.NewsOce, v => { _config.NewsOce = v; SaveConfig(); PushClassicState(); PushNews(false); });
        SetupToggle("Halo 2 Gravemind news", "Include the Halo 2 Gravemind release notes.",
            () => _config.NewsGravemind, v => { _config.NewsGravemind = v; SaveConfig(); PushClassicState(); PushNews(false); });

        RefreshSetup();
    }

    private void SetupToggle(string label, string desc, Func<bool> get, Action<bool> set) =>
        AddSetupRow(label, desc, () => get() ? "Enabled" : "Disabled", d => set(!get()), kind: 1);

    private void SetupChoice(string label, string desc, string[] names, Func<int> get, Action<int> set) =>
        AddSetupRow(label, desc, () => names[Math.Clamp(get(), 0, names.Length - 1)], d => set(Wrap(get(), d, names.Length)), kind: 0);

    private void AddSetupRow(string label, string desc, Func<string> value, Action<int> change, int kind, Action? click = null)
    {
        var info = new SetupRowInfo { Change = change };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var left = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 24, 0) };
        left.Children.Add(new TextBlock { Text = label, FontSize = 19, FontWeight = FontWeights.SemiBold, Foreground = Brushes.White });
        left.Children.Add(new TextBlock { Text = desc, FontSize = 13, Foreground = Br("#8FA3BA"), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 2, 0, 0) });
        Grid.SetColumn(left, 0);

        var right = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        if (kind == 1)
        {
            var tick = new TextBlock { Text = "✓", FontSize = 20, FontWeight = FontWeights.Bold, Foreground = Br("#FFD779"), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            var box = new Border { Width = 34, Height = 34, CornerRadius = new CornerRadius(8), BorderThickness = new Thickness(2), Margin = new Thickness(12, 0, 6, 0), Child = tick };
            _setupRefresh.Add(() =>
            {
                bool on = value() == "Enabled";
                tick.Visibility = on ? Visibility.Visible : Visibility.Hidden;
                box.BorderBrush = Br(on ? "#FFD779" : "#5F7690");
                box.Background = Br(on ? "#33FFD779" : "#2A1C2E49");
            });
            right.Children.Add(box);
        }
        else if (kind == 2)
        {
            var mark = new TextBlock { FontSize = 26, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(12, 0, 10, 2) };
            _setupRefresh.Add(() =>
            {
                string v = value();
                mark.Text = v;
                mark.Foreground = Br(v == "●" ? "#FFD779" : "#5F7690");
            });
            right.Children.Add(mark);
        }
        else
        {
            TextBlock Arrow(string glyph, int delta)
            {
                var t = new TextBlock
                {
                    Text = glyph, FontSize = 18, Foreground = Br("#5F7690"), Padding = new Thickness(12, 6, 12, 6),
                    Background = Brushes.Transparent, Cursor = Cursors.Hand, VerticalAlignment = VerticalAlignment.Center
                };
                t.MouseEnter += (_, _) => t.Foreground = Br("#FFD779");
                t.MouseLeave += (_, _) => t.Foreground = Br("#5F7690");
                t.PreviewMouseLeftButtonDown += (_, e) => { e.Handled = true; change(delta); RefreshSetup(); };
                return t;
            }
            var text = new TextBlock
            {
                MinWidth = 190, TextAlignment = TextAlignment.Center, FontSize = 18, Foreground = Br("#FFD779"),
                VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 8, 0)
            };
            _setupRefresh.Add(() => text.Text = value());
            right.Children.Add(Arrow("◀", -1));
            right.Children.Add(new Border { CornerRadius = new CornerRadius(14), Background = Br("#2A1C2E49"), Padding = new Thickness(6, 4, 6, 4), Child = text });
            right.Children.Add(Arrow("▶", 1));
        }
        Grid.SetColumn(right, 1);
        grid.Children.Add(left);
        grid.Children.Add(right);

        var row = new Button { Style = (Style)FindResource("SettingRow"), Content = grid, Tag = info };
        row.Click += (_, _) =>
        {
            if (click != null) click(); else change(1);
            RefreshSetup();
        };
        row.MouseRightButtonUp += (_, _) => { if (kind == 0) { change(-1); RefreshSetup(); } };
        SetupList.Children.Add(row);
        _setupFirstRow ??= row;
    }
}
