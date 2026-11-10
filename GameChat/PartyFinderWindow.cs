using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace AHFMCChat;

public sealed class PartyFinderWindow : Window
{
    static readonly (string Id, string Name)[] Games =
    {
        ("ce2026", "Halo: Campaign Evolved (2026)"),
        ("mcc", "Halo: The Master Chief Collection"),
        ("infinite", "Halo Infinite"),
        ("h5forge", "Halo 5: Forge"),
        ("hwde", "Halo Wars: Definitive Edition"),
        ("hw2", "Halo Wars 2"),
        ("spartanassault", "Halo: Spartan Assault"),
        ("spartanstrike", "Halo: Spartan Strike"),
        ("halo3", "Reclaimer"),
        ("reach", "Halo Reach Renaissance"),
        ("oce", "Open Community Edition"),
        ("gravemind", "Halo 2 Gravemind"),
        ("other", "Other Halo game")
    };
    static readonly string[] Activities = { "Campaign", "Co-op campaign", "Multiplayer", "Custom games", "Anything" };
    static readonly string[] DurationNames = { "15 minutes", "30 minutes", "1 hour", "2 hours", "4 hours", "8 hours", "12 hours", "24 hours", "Custom..." };
    static readonly int[] DurationMinutes = { 15, 30, 60, 120, 240, 480, 720, 1440, 0 };
    const int MaxPlayers = 128, MaxMinutes = 1440, MinMinutes = 15;

    readonly ChatClient _chat;
    readonly StackPanel _gameList = new();
    readonly StackPanel _cards = new();
    readonly TextBlock _heading = new() { FontSize = 17, FontWeight = FontWeights.SemiBold, TextTrimming = TextTrimming.CharacterEllipsis };
    readonly TextBlock _status = new() { FontSize = 11, TextTrimming = TextTrimming.CharacterEllipsis };
    readonly TextBlock _formTitle = new() { FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 8) };
    readonly TextBox _note = new() { MaxLength = 120, Margin = new Thickness(0, 8, 0, 0) };
    readonly ComboBox _activityBox = new(), _neededBox = new(), _durationBox = new();
    readonly TextBox _custom = new() { Width = 110, ToolTip = "For example 90, 90m, 1.5h, 1:30 or 2h30 (15 minutes to 24 hours)" };
    readonly TextBlock _customHint = new() { Margin = new Thickness(10, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center, FontSize = 12 };
    readonly StackPanel _customRow = new() { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 8, 0, 0), Visibility = Visibility.Collapsed };
    readonly Button _postBtn;
    readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(10) };
    readonly DispatcherTimer _clock = new() { Interval = TimeSpan.FromSeconds(20) };

    List<LfgPost> _posts = new();
    string _game = "ce2026";
    bool _busy, _loaded;

    public event Action<string>? MessageRequested;

    static Brush Res(string key) => (Brush)Application.Current.FindResource(key);
    static Style Sty(string key) => (Style)Application.Current.FindResource(key);
    static string GameName(string id) => Games.FirstOrDefault(g => g.Id == id).Name ?? id;

    public PartyFinderWindow(ChatClient chat)
    {
        _chat = chat;
        Title = "Party finder";
        Width = 860; Height = 600; MinWidth = 700; MinHeight = 480;
        ShowInTaskbar = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = Res("Bg");
        Foreground = Res("Text");
        FontFamily = new FontFamily("Segoe UI");
        FontSize = 13;

        foreach (var x in Activities) _activityBox.Items.Add(x);
        for (int i = 1; i <= MaxPlayers; i++) _neededBox.Items.Add(i == 1 ? "1 player" : i + " players");
        foreach (var x in DurationNames) _durationBox.Items.Add(x);
        _activityBox.SelectedIndex = 0; _neededBox.SelectedIndex = 1; _durationBox.SelectedIndex = 3;
        _activityBox.Margin = _neededBox.Margin = _durationBox.Margin = new Thickness(0, 0, 8, 4);
        _activityBox.MinWidth = 140; _neededBox.MinWidth = 110; _durationBox.MinWidth = 120;
        _durationBox.SelectionChanged += (_, _) =>
        {
            bool custom = DurationMinutes[Math.Max(0, _durationBox.SelectedIndex)] == 0;
            _customRow.Visibility = custom ? Visibility.Visible : Visibility.Collapsed;
            if (custom) { UpdateCustomHint(); Dispatcher.BeginInvoke(new Action(() => { _custom.Focus(); _custom.SelectAll(); }), DispatcherPriority.Input); }
        };
        _custom.TextChanged += (_, _) => UpdateCustomHint();
        _customRow.Children.Add(new TextBlock { Text = "Custom time:", Foreground = Res("Muted"), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) });
        _customRow.Children.Add(_custom);
        _customRow.Children.Add(_customHint);
        _postBtn = new Button { Style = Sty("Primary"), Padding = new Thickness(16, 7, 16, 7) };
        _postBtn.Click += async (_, _) => await PostAsync();
        _status.Foreground = Res("Muted");

        var left = new DockPanel { Background = Res("Panel") };
        var leftTitle = new TextBlock { Text = "Games", FontWeight = FontWeights.SemiBold, Foreground = Res("Muted"), Margin = new Thickness(16, 14, 0, 6) };
        DockPanel.SetDock(leftTitle, Dock.Top);
        left.Children.Add(leftTitle);
        left.Children.Add(new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Content = _gameList });

        var refresh = new Button { Content = "Refresh", Margin = new Thickness(10, 0, 0, 0) };
        refresh.Click += async (_, _) => await RefreshAsync();
        var head = new DockPanel { Margin = new Thickness(18, 14, 18, 8) };
        DockPanel.SetDock(refresh, Dock.Right);
        head.Children.Add(refresh);
        head.Children.Add(_heading);

        var form = new Border
        {
            Background = Res("Panel"), BorderBrush = Res("Line"), BorderThickness = new Thickness(0, 1, 0, 0), Padding = new Thickness(18, 12, 18, 12)
        };
        var row = new WrapPanel { Orientation = Orientation.Horizontal };
        row.Children.Add(new TextBlock { Text = "Playing:", Foreground = Res("Muted"), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) });
        row.Children.Add(_activityBox);
        row.Children.Add(new TextBlock { Text = "Players needed:", Foreground = Res("Muted"), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 8, 0) });
        row.Children.Add(_neededBox);
        row.Children.Add(new TextBlock { Text = "For:", Foreground = Res("Muted"), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 8, 0) });
        row.Children.Add(_durationBox);
        var formStack = new StackPanel();
        formStack.Children.Add(_formTitle);
        formStack.Children.Add(row);
        formStack.Children.Add(_customRow);
        var noteRow = new DockPanel { Margin = new Thickness(0, 8, 0, 0) };
        DockPanel.SetDock(_postBtn, Dock.Right);
        _postBtn.Margin = new Thickness(10, 0, 0, 0);
        noteRow.Children.Add(_postBtn);
        _note.Margin = new Thickness(0);
        noteRow.Children.Add(_note);
        formStack.Children.Add(new TextBlock { Text = "Note (optional): for example \"Legendary, headset on\"", Foreground = Res("Muted"), FontSize = 11, Margin = new Thickness(0, 8, 0, 2) });
        formStack.Children.Add(noteRow);
        form.Child = formStack;

        var statusBar = new Border { Background = Res("Panel"), BorderBrush = Res("Line"), BorderThickness = new Thickness(0, 1, 0, 0), Padding = new Thickness(12, 4, 12, 4), Child = _status };

        var right = new DockPanel();
        DockPanel.SetDock(head, Dock.Top);
        DockPanel.SetDock(statusBar, Dock.Bottom);
        DockPanel.SetDock(form, Dock.Bottom);
        right.Children.Add(head);
        right.Children.Add(statusBar);
        right.Children.Add(form);
        right.Children.Add(new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Content = _cards, Padding = new Thickness(0, 0, 0, 6) });

        var root = new Grid();
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(260) });
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        Grid.SetColumn(right, 1);
        root.Children.Add(left);
        root.Children.Add(right);
        Content = root;

        UpdateForm();
        BuildGameList();
        BuildCards();

        _timer.Tick += async (_, _) => { if (!_busy) await RefreshAsync(); };
        _clock.Tick += (_, _) => { if (_loaded) { BuildGameList(); BuildCards(); } };
        Loaded += async (_, _) => { _timer.Start(); _clock.Start(); await RefreshAsync(); };
        Closed += (_, _) => { _timer.Stop(); _clock.Stop(); };
    }

    void BuildGameList()
    {
        _gameList.Children.Clear();
        void Item(string id, string name)
        {
            int n = _posts.Count(p => p.ExpiresAt > DateTime.UtcNow && (p.Needed > 0 || p.UserId == _chat.UserId) && (id == "" || p.Game == id));
            var t = new DockPanel { LastChildFill = true };
            var count = new TextBlock { Text = n == 0 ? "" : n.ToString(), Foreground = Res("Accent"), FontWeight = FontWeights.SemiBold, Margin = new Thickness(8, 0, 0, 0) };
            DockPanel.SetDock(count, Dock.Right);
            t.Children.Add(count);
            t.Children.Add(new TextBlock { Text = name, TextTrimming = TextTrimming.CharacterEllipsis });
            var holder = new Border { Background = _game == id ? Res("AccentSel") : Brushes.Transparent, CornerRadius = new CornerRadius(6), Margin = new Thickness(6, 1, 6, 1), Padding = new Thickness(10, 8, 10, 8), Cursor = System.Windows.Input.Cursors.Hand, Child = t };
            holder.MouseEnter += (_, _) => { if (_game != id) holder.Background = Res("PanelHi"); };
            holder.MouseLeave += (_, _) => { if (_game != id) holder.Background = Brushes.Transparent; };
            holder.MouseLeftButtonUp += (_, _) => { _game = id; UpdateForm(); BuildGameList(); BuildCards(); };
            _gameList.Children.Add(holder);
        }
        Item("", "All games");
        foreach (var g in Games) Item(g.Id, g.Name);
    }

    void BuildCards()
    {
        _cards.Children.Clear();
        var shown = _posts.Where(p => p.ExpiresAt > DateTime.UtcNow && (p.Needed > 0 || p.UserId == _chat.UserId) && (_game == "" || p.Game == _game))
                          .OrderByDescending(p => p.UserId == _chat.UserId).ThenBy(p => p.ExpiresAt).ToList();
        int others = shown.Count(p => p.UserId != _chat.UserId);
        int stillNeeded = shown.Sum(p => p.Needed);
        _heading.Text = _game == "" ? "Everyone looking for players" : GameName(_game);

        if (!_loaded) { _cards.Children.Add(Info("Loading...")); return; }
        if (shown.Count == 0)
        {
            _cards.Children.Add(Info(_game == ""
                ? "Nobody is looking right now. Pick a game and post below so others can find you."
                : "Nobody else has said they want to play this yet. Post below and the next person to look will see you."));
            return;
        }
        if (_game != "")
            _cards.Children.Add(new TextBlock
            {
                Text = (others == 0 ? "Only you so far." : others == 1 ? "1 other person wants to play this." : others + " other people want to play this.")
                       + "  ·  " + (stillNeeded == 1 ? "1 player still needed" : stillNeeded + " players still needed") + " in total",
                Foreground = Res("Muted"), Margin = new Thickness(18, 0, 18, 6)
            });
        foreach (var p in shown) _cards.Children.Add(Card(p));
    }

    TextBlock Info(string text) => new() { Text = text, Foreground = Res("Muted"), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(18, 8, 18, 8) };

    Border Card(LfgPost p)
    {
        bool mine = p.UserId == _chat.UserId;
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        if (mine)
        {
            var rm = new Button { Content = "Remove", Style = Sty("DangerButton") };
            rm.Click += async (_, _) => await Run(async () => { await _chat.RemoveLfgAsync(p.Id); Say("Your post is removed.", false); });
            buttons.Children.Add(rm);
        }
        else
        {
            var msg = new Button { Content = "Message", Margin = new Thickness(0, 0, 6, 0) };
            msg.Click += (_, _) => { MessageRequested?.Invoke(p.Username); Close(); };
            buttons.Children.Add(msg);
            if (p.PartyId.Length == 0)
            {
                var inv = new Button { Content = "Invite to party  ▾", Style = Sty("Primary") };
                inv.Click += (_, _) => ShowInviteMenu(inv, p);
                buttons.Children.Add(inv);
            }
            else if (_chat.Parties.Any(x => x.Id == p.PartyId))
                buttons.Children.Add(new Button { Content = "In your party", IsEnabled = false });
            else if (p.Needed <= 0)
                buttons.Children.Add(new Button { Content = "Party full", IsEnabled = false });
            else
            {
                var join = new Button { Content = "Join party", Style = Sty("Primary"), Padding = new Thickness(16, 5, 16, 5) };
                join.Click += async (_, _) => await Run(async () => Say(await _chat.JoinLfgAsync(p), false));
                buttons.Children.Add(join);
            }
        }

        var title = new StackPanel { Orientation = Orientation.Horizontal };
        title.Children.Add(new TextBlock { Text = p.Username, FontWeight = FontWeights.SemiBold, FontSize = 15 });
        if (mine) title.Children.Add(new TextBlock { Text = "  (you)", Foreground = Res("Muted"), VerticalAlignment = VerticalAlignment.Bottom });

        var mins = Math.Max(0, (int)Math.Ceiling((p.ExpiresAt - DateTime.UtcNow).TotalMinutes));
        string left = FormatMinutes(mins) + " left";
        string what = (_game == "" ? GameName(p.Game) + "  ·  " : "") + (p.Activity.Length > 0 ? p.Activity + "  ·  " : "")
                      + (p.Needed <= 0 ? "party full" : p.Needed == 1 ? "still needs 1 player" : $"still needs {p.Needed} players")
                      + (p.Joined > 0 ? $"  ·  {p.Joined} joined" : "") + "  ·  " + left;

        var text = new StackPanel { Margin = new Thickness(0, 0, 12, 0) };
        text.Children.Add(title);
        text.Children.Add(new TextBlock { Text = what, Foreground = Res("Muted"), FontSize = 12, TextWrapping = TextWrapping.Wrap });
        if (mine && _chat.Parties.FirstOrDefault(x => x.Id == p.PartyId) is { } mp)
            text.Children.Add(new TextBlock { Text = $"You are in party '{mp.Name}'. People who join are added automatically.", Foreground = Res("Accent"), FontSize = 12, Margin = new Thickness(0, 2, 0, 0) });
        if (p.Note.Length > 0) text.Children.Add(new TextBlock { Text = p.Note, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 4, 0, 0) });

        var dock = new DockPanel();
        DockPanel.SetDock(buttons, Dock.Right);
        dock.Children.Add(buttons);
        dock.Children.Add(text);
        return new Border
        {
            Background = Res("Panel"), BorderBrush = Res(mine ? "Accent" : "Line"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(8),
            Padding = new Thickness(14, 10, 14, 10), Margin = new Thickness(18, 0, 18, 8), Child = dock
        };
    }

    void ShowInviteMenu(Button anchor, LfgPost p)
    {
        var menu = new ContextMenu { PlacementTarget = anchor, Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom };
        foreach (var party in _chat.Parties.Where(x => x.IsOwner))
        {
            var name = party.Name;
            var mi = new MenuItem { Header = "Invite to " + name };
            mi.Click += async (_, _) => await Run(async () => Say(await _chat.InviteAsync(name, p.Username), false));
            menu.Items.Add(mi);
        }
        var fresh = new MenuItem { Header = "+  New party for " + GameName(p.Game) + " and invite" };
        fresh.Click += async (_, _) => await Run(async () =>
        {
            string name = $"{p.Game}-{Random.Shared.Next(100, 1000)}";
            await _chat.CreatePartyAsync(name);
            Say(await _chat.InviteAsync(name, p.Username), false);
        });
        menu.Items.Add(fresh);
        menu.IsOpen = true;
    }

    void UpdateForm()
    {
        bool all = _game == "";
        bool have = _posts.Any(x => x.UserId == _chat.UserId && x.Game == _game);
        _formTitle.Text = all ? "Pick a game on the left to post that you are looking" : "I want to play  " + GameName(_game);
        _postBtn.Content = have ? "Update my post" : "Post";
        _postBtn.IsEnabled = !all;
        _activityBox.IsEnabled = _neededBox.IsEnabled = _durationBox.IsEnabled = _custom.IsEnabled = _note.IsEnabled = !all;
    }

    void UpdateCustomHint()
    {
        if (_custom.Text.Trim().Length == 0) { _customHint.Text = "Type a time like 90, 90m, 1.5h, 1:30 or 2h30"; _customHint.Foreground = Res("Muted"); return; }
        if (!TryParseMinutes(_custom.Text, out int m)) { _customHint.Text = "Not a valid time. Try 90, 1.5h or 2h30"; _customHint.Foreground = Res("Danger"); return; }
        if (m < MinMinutes) { _customHint.Text = $"Too short: the minimum is {MinMinutes} minutes"; _customHint.Foreground = Res("Danger"); return; }
        if (m > MaxMinutes) { _customHint.Text = "Too long: the maximum is 24 hours"; _customHint.Foreground = Res("Danger"); return; }
        _customHint.Text = "= " + FormatMinutes(m);
        _customHint.Foreground = Res("Muted");
    }

    static string FormatMinutes(int mins) => mins >= 60 ? (mins % 60 == 0 ? $"{mins / 60} h" : $"{mins / 60} h {mins % 60} min") : $"{mins} min";

    async Task PostAsync()
    {
        if (_game == "") return;
        await Run(async () =>
        {
            int minutes = DurationMinutes[Math.Max(0, _durationBox.SelectedIndex)];
            if (minutes == 0)
            {
                if (!TryParseMinutes(_custom.Text, out minutes)) throw new InvalidOperationException("Type a time like 90, 90m, 1.5h or 2h30 (15 minutes to 24 hours).");
                if (minutes < MinMinutes) throw new InvalidOperationException($"The shortest time is {MinMinutes} minutes.");
                if (minutes > MaxMinutes) throw new InvalidOperationException("The longest time is 24 hours.");
            }
            Say(await _chat.PostLfgAsync(_game, Activities[Math.Max(0, _activityBox.SelectedIndex)], _note.Text, _neededBox.SelectedIndex + 1, minutes), false);
        });
    }

    static bool TryParseMinutes(string text, out int minutes)
    {
        minutes = 0;
        string t = (text ?? "").Trim().ToLowerInvariant().Replace(" ", "").Replace(',', '.');
        if (t.Length == 0) return false;
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        var m = System.Text.RegularExpressions.Regex.Match(t, @"^(\d+)(?:h|:|hr|hours?)(\d+)?(?:m|min|minutes?)?$");
        if (m.Success) { minutes = int.Parse(m.Groups[1].Value) * 60 + (m.Groups[2].Success ? int.Parse(m.Groups[2].Value) : 0); return minutes > 0; }
        m = System.Text.RegularExpressions.Regex.Match(t, @"^(\d+(?:\.\d+)?)(h|hr|hours?)$");
        if (m.Success) { minutes = (int)Math.Round(double.Parse(m.Groups[1].Value, inv) * 60); return minutes > 0; }
        m = System.Text.RegularExpressions.Regex.Match(t, @"^(\d+)(m|min|minutes?)?$");
        if (m.Success) { minutes = int.Parse(m.Groups[1].Value); return minutes > 0; }
        return false;
    }

    void Say(string text, bool error)
    {
        _status.Text = text;
        _status.Foreground = Res(error ? "Danger" : "Muted");
    }

    async Task Run(Func<Task> action)
    {
        if (_busy) return;
        _busy = true;
        try { await action(); await LoadAsync(); }
        catch (Exception ex) { Say(Friendly(ex), true); }
        finally { _busy = false; }
    }

    async Task RefreshAsync()
    {
        if (_busy) return;
        _busy = true;
        try { await LoadAsync(); }
        catch (Exception ex) { _loaded = true; Say(Friendly(ex), true); BuildCards(); }
        finally { _busy = false; }
    }

    async Task LoadAsync()
    {
        _posts = await _chat.ListLfgAsync();
        _loaded = true;
        UpdateForm();
        BuildGameList();
        BuildCards();
        if (_status.Text.Length == 0) Say("Live: updates every 10 seconds.", false);
    }

    static string Friendly(Exception ex)
        => ex.Message.Contains("lfg_posts_needed") || ex.Message.Contains("needed_check")
            ? "The server still only allows 15 players. Run the updated chat-all.sql in the Supabase SQL Editor to allow up to 128."
            : ex.Message.Contains("party_id") || ex.Message.Contains("joined") || ex.Message.Contains("lfg_joins")
            ? "The server needs the party finder update. Run the updated chat-all.sql in the Supabase SQL Editor."
            : ex.Message.Contains("lfg_posts") || ex.Message.StartsWith("404")
            ? "The party finder isn't set up on the server yet. Run chat-all.sql in the Supabase SQL Editor first."
            : ex.Message;
}
