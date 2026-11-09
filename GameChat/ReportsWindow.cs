using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace AHFMCChat;

public sealed class ReportsWindow : Window
{
    readonly ChatClient _chat;
    readonly ListBox _list = new() { BorderThickness = new Thickness(0), Background = Brushes.Transparent };
    readonly TextBlock _head = new() { FontSize = 17, FontWeight = FontWeights.SemiBold, TextTrimming = TextTrimming.CharacterEllipsis };
    readonly TextBlock _meta = new() { Margin = new Thickness(0, 4, 0, 10), TextWrapping = TextWrapping.Wrap };
    readonly TextBox _detail = new() { IsReadOnly = true, TextWrapping = TextWrapping.Wrap, AcceptsReturn = true, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
    readonly TextBlock _status = new() { FontSize = 11, TextTrimming = TextTrimming.CharacterEllipsis, Margin = new Thickness(0, 8, 0, 0) };
    readonly CheckBox _closed = new() { Content = "Show closed reports", Margin = new Thickness(14, 0, 14, 8), Foreground = Brushes.White };
    readonly Button _hold = new() { Content = "Hold (investigate)", Margin = new Thickness(0, 0, 8, 0), Padding = new Thickness(12, 6, 12, 6) };
    readonly Button _ban = new() { Content = "Ban", Margin = new Thickness(0, 0, 8, 0), Padding = new Thickness(14, 6, 14, 6) };
    readonly Button _release = new() { Content = "Release / dismiss", Margin = new Thickness(0, 0, 8, 0), Padding = new Thickness(12, 6, 12, 6) };
    readonly Button _appeal = new() { Content = "Open appeal chat", Padding = new Thickness(12, 6, 12, 6) };

    List<ReportItem> _items = new();
    Dictionary<string, string> _states = new();
    bool _busy;

    public event Action<string>? AppealRequested;
    public event Action? Changed;

    static Brush Res(string key) => (Brush)Application.Current.FindResource(key);
    static Style Sty(string key) => (Style)Application.Current.FindResource(key);

    public ReportsWindow(ChatClient chat)
    {
        _chat = chat;
        Title = "Reports";
        Width = 900; Height = 580; MinWidth = 720; MinHeight = 420;
        ShowInTaskbar = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = Res("Bg");
        Foreground = Res("Text");
        FontFamily = new FontFamily("Segoe UI");
        FontSize = 13;

        _ban.Style = Sty("DangerButton");
        _status.Foreground = Res("Muted");
        _meta.Foreground = Res("Muted");

        var refresh = new Button { Content = "Refresh", Margin = new Thickness(14, 12, 14, 8) };
        refresh.Click += async (_, _) => await RefreshAsync();
        _closed.Click += async (_, _) => await RefreshAsync();
        var top = new StackPanel();
        top.Children.Add(refresh);
        top.Children.Add(_closed);
        var left = new DockPanel { Background = Res("Panel") };
        DockPanel.SetDock(top, Dock.Top);
        left.Children.Add(top);
        left.Children.Add(new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Content = _list });

        _hold.Click += async (_, _) => await ActAsync("hold");
        _ban.Click += async (_, _) => await ActAsync("banned");
        _release.Click += async (_, _) => await ActAsync("clear");
        _appeal.Click += (_, _) => { if (Current is { } r) AppealRequested?.Invoke(r.Reported); };

        var buttons = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 12, 0, 0) };
        buttons.Children.Add(_hold);
        buttons.Children.Add(_ban);
        buttons.Children.Add(_release);
        buttons.Children.Add(_appeal);

        var bottom = new StackPanel();
        bottom.Children.Add(buttons);
        bottom.Children.Add(_status);

        var head = new StackPanel { Margin = new Thickness(0, 0, 0, 0) };
        head.Children.Add(_head);
        head.Children.Add(_meta);

        var right = new DockPanel { Margin = new Thickness(18, 14, 18, 14) };
        DockPanel.SetDock(head, Dock.Top);
        DockPanel.SetDock(bottom, Dock.Bottom);
        right.Children.Add(head);
        right.Children.Add(bottom);
        right.Children.Add(_detail);

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(300) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        Grid.SetColumn(left, 0);
        Grid.SetColumn(right, 1);
        grid.Children.Add(left);
        grid.Children.Add(right);
        Content = grid;

        _list.SelectionChanged += (_, _) => ShowCurrent();
        Loaded += async (_, _) => await RefreshAsync();
        ShowCurrent();
    }

    ReportItem? Current => (_list.SelectedItem as ListBoxItem)?.Tag as ReportItem;

    string StateOf(ReportItem r) => _states.TryGetValue(r.ReportedId, out var s) && s != "clear" ? s : "none";

    static string StateLabel(string s) => s switch { "hold" => "ON HOLD", "banned" => "BANNED", _ => "no restriction" };

    async Task RefreshAsync()
    {
        if (_busy) return;
        _busy = true;
        long keep = Current?.Id ?? -1;
        try
        {
            _items = await _chat.ListReportsAsync(_closed.IsChecked == true);
            _states = await _chat.ModerationStatesAsync();

            _list.Items.Clear();
            foreach (var r in _items)
            {
                string state = StateOf(r);
                var line = new StackPanel();
                line.Children.Add(new TextBlock { Text = $"#{r.Id}  {r.Reported}", FontWeight = FontWeights.SemiBold, TextTrimming = TextTrimming.CharacterEllipsis });
                line.Children.Add(new TextBlock
                {
                    Text = $"by {(r.Reporter.Length > 0 ? r.Reporter : "?")}  ·  {StateLabel(state)}" + (r.Status == "open" ? "" : $"  ·  {r.Status}"),
                    FontSize = 11, Foreground = state == "banned" ? Res("Danger") : state == "hold" ? Res("Warn") : Res("Muted"),
                    TextTrimming = TextTrimming.CharacterEllipsis
                });
                _list.Items.Add(new ListBoxItem { Content = line, Tag = r, Padding = new Thickness(14, 8, 14, 8) });
            }
            var again = _list.Items.OfType<ListBoxItem>().FirstOrDefault(i => (i.Tag as ReportItem)?.Id == keep);
            if (again != null) _list.SelectedItem = again;
            else if (_list.Items.Count > 0) _list.SelectedIndex = 0;

            SetStatus(_items.Count == 0 ? "No reports." : $"{_items.Count} report(s).", false);
        }
        catch (Exception ex) { SetStatus(ex.Message, true); }
        finally { _busy = false; ShowCurrent(); }
    }

    void ShowCurrent()
    {
        var r = Current;
        bool has = r != null;
        _hold.IsEnabled = _ban.IsEnabled = _release.IsEnabled = _appeal.IsEnabled = has;
        if (r == null)
        {
            _head.Text = "Reports";
            _meta.Text = "Pick a report on the left.";
            _detail.Text = "";
            return;
        }
        string state = StateOf(r);
        _head.Text = $"{r.Reported}  ←  {(r.Reporter.Length > 0 ? r.Reporter : "?")}";
        _meta.Text = $"Report #{r.Id}  ·  {r.Created:g}  ·  report is {r.Status}  ·  {r.Reported} is currently: {StateLabel(state)}";
        _detail.Text = "REASON\n" + r.Reason + "\n\nEVIDENCE (chosen by the reporter, not proof)\n" + (r.Evidence.Length > 0 ? r.Evidence : "(none attached)");
        _hold.IsEnabled = state != "hold" && state != "banned";
        _ban.IsEnabled = state != "banned";
        _release.IsEnabled = state != "none" || r.Status == "open";
        _appeal.IsEnabled = state == "banned" || r.Status != "open";
    }

    async Task ActAsync(string status)
    {
        if (Current is not { } r || _busy) return;
        string why = "";
        if (status == "banned")
        {
            string? reason = InputDialog.Ask(this, "Ban " + r.Reported, "Reason for the ban (the person will see it):");
            if (reason == null) return;
            why = reason;
            if (MessageBox.Show(this, $"Ban {r.Reported}?\n\nThey will stay in the app but can only message you in the appeal chat.",
                    "Ban", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        }
        try
        {
            SetStatus(await _chat.ModerateAsync(r.ReportedId, r.Reported, status, why), false);
            Changed?.Invoke();
        }
        catch (Exception ex) { SetStatus(ex.Message, true); return; }
        await RefreshAsync();
    }

    void SetStatus(string text, bool error)
    {
        _status.Text = text;
        _status.Foreground = Res(error ? "Danger" : "Muted");
    }
}
