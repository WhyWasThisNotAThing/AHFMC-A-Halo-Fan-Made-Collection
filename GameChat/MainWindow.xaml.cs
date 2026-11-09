using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace AHFMCChat;

public partial class MainWindow : Window
{
    readonly ChatClient _chat;
    readonly ObservableCollection<Conversation> _convs = new();
    readonly Dictionary<string, Conversation> _index = new(StringComparer.OrdinalIgnoreCase);
    readonly ICollectionView _view;
    readonly Conversation _lobby, _system;
    readonly DateTime _started = DateTime.Now;
    Conversation? _selected;
    DateTime _lastSound = DateTime.MinValue;
    readonly DispatcherTimer _idle = new() { Interval = TimeSpan.FromSeconds(10) };
    DateTime _lastInput = DateTime.Now;
    bool _locked;

    public bool LoggedOut { get; private set; }

    public MainWindow(ChatClient chat, EventHub hub)
    {
        InitializeComponent();
        _chat = chat;
        UserText.Text = chat.Username;
        Title = $"Chat - {chat.Username}";

        _lobby = Get("public", "lobby", "Public lobby");
        _system = Get("system", "log", "System log");

        _view = CollectionViewSource.GetDefaultView(_convs);
        _view.GroupDescriptions.Add(new PropertyGroupDescription(nameof(Conversation.Section)));
        _view.SortDescriptions.Add(new SortDescription(nameof(Conversation.SectionOrder), ListSortDirection.Ascending));
        _view.SortDescriptions.Add(new SortDescription(nameof(Conversation.Title), ListSortDirection.Ascending));
        NavList.ItemsSource = _view;

        _chat.PartiesChanged += () => Dispatcher.InvokeAsync(RefreshParties);
        hub.Attach(e => Dispatcher.InvokeAsync(() => OnChatEvent(e)));

        PreviewMouseMove += (_, _) => _lastInput = DateTime.Now;
        PreviewMouseDown += (_, _) => _lastInput = DateTime.Now;
        PreviewKeyDown += (_, _) => _lastInput = DateTime.Now;
        _idle.Tick += (_, _) =>
        {
            int m = LocalSettings.AutoLockMinutes;
            if (m > 0 && !_locked && (DateTime.Now - _lastInput).TotalMinutes >= m) Lock();
        };
        _idle.Start();

        RefreshParties();
        Select(_lobby);
    }

    Conversation Get(string kind, string key, string title)
    {
        string k = kind + ":" + key;
        if (_index.TryGetValue(k, out var c)) return c;
        c = new Conversation(kind, key, title);
        _index[k] = c;
        _convs.Add(c);
        return c;
    }

    void Remove(Conversation c)
    {
        _index.Remove(c.Kind + ":" + c.Key);
        _convs.Remove(c);
        if (_selected == c) Select(_lobby);
    }

    void Select(Conversation c) => NavList.SelectedItem = c;

    void RefreshParties()
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var p in _chat.Parties)
        {
            names.Add(p.Name);
            Get("party", p.Name, p.Name).IsOwner = p.IsOwner;
        }
        foreach (var c in _convs.Where(x => x.Kind == "party" && !names.Contains(x.Key)).ToList())
            Remove(c);
        UpdateHeader();
    }

    void NavList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (NavList.SelectedItem is not Conversation c) return;
        if (_selected != null) _selected.Messages.CollectionChanged -= OnMessagesChanged;
        _selected = c;
        c.Unread = 0;
        c.Messages.CollectionChanged += OnMessagesChanged;
        MsgList.ItemsSource = c.Messages;
        UpdateHeader();
        ScrollToEnd();
        if (Composer.Visibility == Visibility.Visible) Input.Focus();
    }

    void OnMessagesChanged(object? s, NotifyCollectionChangedEventArgs e)
        => Dispatcher.InvokeAsync(ScrollToEnd, DispatcherPriority.Background);

    void ScrollToEnd()
    {
        int n = MsgList.Items.Count;
        if (n > 0) MsgList.ScrollIntoView(MsgList.Items[n - 1]);
    }

    static Visibility Vis(bool b) => b ? Visibility.Visible : Visibility.Collapsed;

    static Brush Br(string hex)
    {
        var b = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        b.Freeze();
        return b;
    }

    void UpdateHeader()
    {
        var c = _selected;
        if (c == null) return;

        TitleText.Text = c.Kind switch
        {
            "public" => "# Public lobby",
            "dm" => "@ " + c.Title,
            "party" => "◈ " + c.Title,
            "invite" => "Invitation: " + c.Title,
            _ => "System log"
        };

        switch (c.Kind)
        {
            case "public":
                Banner.Visibility = Visibility.Visible;
                Banner.Background = Br("#3A2F12");
                BannerText.Foreground = (Brush)FindResource("Warn");
                BannerText.Text = "Public lobby: messages here are NOT encrypted and visible to everyone.";
                break;
            case "dm" when LocalSettings.WarnUnverified && !_chat.IsContactVerified(c.Key):
                Banner.Visibility = Visibility.Visible;
                Banner.Background = Br("#3A2F12");
                BannerText.Foreground = (Brush)FindResource("Warn");
                BannerText.Text = "Encrypted, but this contact is NOT verified. Press Safety code, compare it with them in person or on a call, then Mark verified.";
                break;
            case "dm":
            case "party":
                Banner.Visibility = Visibility.Visible;
                Banner.Background = Br("#12301C");
                BannerText.Foreground = (Brush)FindResource("Ok");
                BannerText.Text = "End-to-end encrypted. Only people in this conversation can read it.";
                break;
            default:
                Banner.Visibility = Visibility.Collapsed;
                break;
        }

        bool party = c.Kind == "party";
        BtnMembers.Visibility = Vis(party);
        BtnInvite.Visibility = Vis(party && c.IsOwner);
        BtnKick.Visibility = Vis(party && c.IsOwner);
        BtnLeave.Visibility = Vis(party && !c.IsOwner);
        BtnDisband.Visibility = Vis(party && c.IsOwner);
        BtnFingerprint.Visibility = Vis(c.Kind == "dm");
        BtnVerify.Visibility = Vis(c.Kind == "dm");
        BtnAccept.Visibility = Vis(c.Kind == "invite");
        BtnReport.Visibility = Vis(c.Kind is "public" or "dm" or "party");
        BtnBlock.Visibility = Vis(c.Kind == "dm");
        if (c.Kind == "dm") BtnBlock.Content = _chat.IsBlocked(c.Key) ? "Unblock" : "Block";
        Composer.Visibility = Vis(c.Kind is "public" or "dm" or "party");
    }

    void OnChatEvent(ChatEvent e)
    {
        Conversation c;
        switch (e.Kind)
        {
            case "public": c = _lobby; break;
            case "dm": c = Get("dm", e.Channel, e.Channel); break;
            case "party": c = Get("party", e.Channel, e.Channel); break;
            case "invite": AddInvite(e); return;
            default: c = _system; break;
        }

        bool mine = e.From.Equals(_chat.Username, StringComparison.OrdinalIgnoreCase);
        c.Messages.Add(MessageVm.FromEvent(e, mine));

        if (e.Kind == "warn") SetStatus(e.Text, true);
        else if (e.Kind == "system" && e.Channel == "security") SetStatus(e.Text, false);

        if (mine || e.Time < _started) return;
        if (c != _selected || !IsActive) c.Unread++;
        if (c.Kind != "system") PlaySound();
    }

    void AddInvite(ChatEvent e)
    {
        var c = Get("invite", e.Channel, e.Channel);
        if (c.Messages.Count > 0) return;
        c.Messages.Add(new MessageVm
        {
            From = e.From,
            Text = $"{e.From} invited you to join the party '{e.Channel}'. Press \"Accept invite\" to join.",
            TimeText = e.Time.ToString("t"),
            NameBrush = Brushes.Goldenrod
        });
        if (e.Time >= _started || c != _selected) c.Unread++;
        PlaySound();
    }

    void PlaySound()
    {
        if (IsActive || (DateTime.Now - _lastSound).TotalSeconds < 3) return;
        _lastSound = DateTime.Now;
        SoundEngine.PlayDing();
    }

    void Window_Activated(object? sender, EventArgs e)
    {
        if (_selected != null) _selected.Unread = 0;
    }

    void Input_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) { e.Handled = true; _ = SendAsync(); }
    }

    void Send_Click(object sender, RoutedEventArgs e) => _ = SendAsync();

    async Task SendAsync()
    {
        var c = _selected;
        string text = Input.Text.Trim();
        if (c == null || text.Length == 0) return;

        if (text.Equals("/report", StringComparison.OrdinalIgnoreCase) || text.StartsWith("/report ", StringComparison.OrdinalIgnoreCase))
        {
            Input.Clear();
            string who = text[7..].Trim();
            await Guard(() => ReportFlowAsync(who.Length > 0 ? who : null));
            return;
        }

        if (text.StartsWith("/block ", StringComparison.OrdinalIgnoreCase))
        {
            Input.Clear();
            string who = text[7..].Trim();
            await Guard(async () =>
            {
                string canon = await _chat.ResolveUserAsync(who);
                SetStatus(_chat.Block(canon), false);
                UpdateHeader();
            });
            return;
        }
        if (text.StartsWith("/unblock ", StringComparison.OrdinalIgnoreCase))
        {
            Input.Clear();
            string who = text[9..].Trim();
            await Guard(() => { SetStatus(_chat.Unblock(who), false); UpdateHeader(); return Task.CompletedTask; });
            return;
        }
        if (text.Equals("/lock", StringComparison.OrdinalIgnoreCase))
        {
            Input.Clear();
            Lock();
            return;
        }

        Input.Clear();
        try
        {
            switch (c.Kind)
            {
                case "public": await _chat.SendPublicAsync(text); break;
                case "dm": await _chat.SendDmAsync(c.Key, text); break;
                case "party": await _chat.SendPartyAsync(c.Key, text); break;
            }
        }
        catch (Exception ex)
        {
            Input.Text = text;
            Input.CaretIndex = text.Length;
            SetStatus(ex.Message, true);
        }
    }

    async void NewDm_Click(object sender, RoutedEventArgs e) => await Guard(async () =>
    {
        string? name = InputDialog.Ask(this, "New direct message", "Username to message:");
        if (name == null) return;
        string canon = await _chat.ResolveUserAsync(name);
        Select(Get("dm", canon, canon));
    });

    async void NewParty_Click(object sender, RoutedEventArgs e) => await Guard(async () =>
    {
        string? name = InputDialog.Ask(this, "New party", "Party name (letters, numbers, - or _):");
        if (name == null) return;
        SetStatus(await _chat.CreatePartyAsync(name), false);
        RefreshParties();
        Select(Get("party", name, name));
    });

    async void Settings_Click(object sender, RoutedEventArgs e) => await Guard(async () =>
    {
        string fp = await _chat.FingerprintAsync(_chat.Username);
        var dlg = new SettingsWindow(_chat.Username, fp, _chat.BlockedUsers()) { Owner = this };
        dlg.ShowDialog();
        switch (dlg.Result)
        {
            case SettingsAction.Logout:
                LoggedOut = true;
                Close();
                break;
            case SettingsAction.DeleteMessages:
                SetStatus(await _chat.DeleteMyMessagesAsync(), false);
                break;
            case SettingsAction.DeleteAccount:
                await _chat.DeleteAccountAsync();
                LoggedOut = true;
                Close();
                break;
        }
    });

    void Window_Closed(object? sender, EventArgs e)
    {
        _idle.Stop();
        _chat.Dispose();
    }

    async void Members_Click(object sender, RoutedEventArgs e) => await Guard(async () =>
    {
        if (_selected is { Kind: "party" } c)
            MessageBox.Show(this, await _chat.MembersAsync(c.Key), "Members", MessageBoxButton.OK, MessageBoxImage.Information);
    });

    async void Invite_Click(object sender, RoutedEventArgs e) => await Guard(async () =>
    {
        if (_selected is not { Kind: "party" } c) return;
        string? who = InputDialog.Ask(this, "Invite to " + c.Title, "Username to invite:");
        if (who != null) SetStatus(await _chat.InviteAsync(c.Key, who), false);
    });

    async void Kick_Click(object sender, RoutedEventArgs e) => await Guard(async () =>
    {
        if (_selected is not { Kind: "party" } c) return;
        string? who = InputDialog.Ask(this, "Remove from " + c.Title, "Username to remove (the party key will be rotated):");
        if (who == null) return;
        SetStatus(await _chat.KickAsync(c.Key, who), false);
        RefreshParties();
    });

    async void Leave_Click(object sender, RoutedEventArgs e) => await Guard(async () =>
    {
        if (_selected is not { Kind: "party" } c) return;
        if (!Confirm($"Leave '{c.Title}'?")) return;
        SetStatus(await _chat.LeaveAsync(c.Key), false);
        RefreshParties();
    });

    async void Disband_Click(object sender, RoutedEventArgs e) => await Guard(async () =>
    {
        if (_selected is not { Kind: "party" } c) return;
        if (!Confirm($"Permanently delete '{c.Title}' for everyone?")) return;
        SetStatus(await _chat.DisbandAsync(c.Key), false);
        RefreshParties();
    });

    async void Accept_Click(object sender, RoutedEventArgs e) => await Guard(async () =>
    {
        if (_selected is not { Kind: "invite" } c) return;
        string name = c.Key;
        SetStatus(await _chat.AcceptAsync(name), false);
        Remove(c);
        RefreshParties();
        if (_index.TryGetValue("party:" + name, out var party)) Select(party);
    });

    async void Fingerprint_Click(object sender, RoutedEventArgs e) => await Guard(async () =>
    {
        if (_selected is { Kind: "dm" } c)
            MessageBox.Show(this, await _chat.SafetyCodeAsync(c.Key), "Safety code", MessageBoxButton.OK, MessageBoxImage.Information);
    });

    async void Verify_Click(object sender, RoutedEventArgs e) => await Guard(async () =>
    {
        if (_selected is not { Kind: "dm" } c) return;
        if (!Confirm($"Only mark {c.Key} as verified after you have compared safety codes with them in person or on a call.\n\nMark as verified?")) return;
        SetStatus(await _chat.VerifyAsync(c.Key), false);
        UpdateHeader();
    });

    async void Report_Click(object sender, RoutedEventArgs e) => await Guard(() => ReportFlowAsync(null));

    async Task ReportFlowAsync(string? target)
    {
        var c = _selected;
        if (c == null || c.Kind is not ("public" or "dm" or "party")) return;

        target ??= c.Kind == "dm" ? c.Key : InputDialog.Ask(this, "Report a user", "Username to report:");
        if (target == null) return;
        string? reason = InputDialog.Ask(this, "Report " + target, "What happened? (short description)");
        if (reason == null) return;

        var ans = MessageBox.Show(this,
            "Attach the last 20 messages from this conversation as evidence?\n\n" +
            "They will be sent to the app admins in READABLE form. That is your choice; messages are normally end-to-end encrypted. " +
            "Admins can't prove encrypted DMs are genuine, so this helps but isn't proof.",
            "Report " + target, MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
        if (ans == MessageBoxResult.Cancel) return;

        string evidence = $"Conversation: {c.Kind} {c.Title}";
        if (ans == MessageBoxResult.Yes)
            evidence += "\n" + string.Join("\n", c.Messages.TakeLast(20).Select(m => $"[{m.TimeText}] {m.From}: {m.Text}"));

        SetStatus(await _chat.ReportAsync(target, reason, evidence), false);
    }

    void Block_Click(object sender, RoutedEventArgs e)
    {
        if (_selected is not { Kind: "dm" } c) return;
        if (_chat.IsBlocked(c.Key)) SetStatus(_chat.Unblock(c.Key), false);
        else
        {
            if (!Confirm($"Block {c.Key}? You won't see their messages or invites, and you can't message them until you unblock."))
                return;
            SetStatus(_chat.Block(c.Key), false);
        }
        UpdateHeader();
    }

    void Lock_Click(object sender, RoutedEventArgs e) => Lock();

    void Lock()
    {
        _locked = true;
        LockBox.Clear();
        LockError.Text = "";
        LockOverlay.Visibility = Visibility.Visible;
        LockBox.Focus();
    }

    void LockBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) { e.Handled = true; _ = TryUnlockAsync(); }
    }

    void LockUnlock_Click(object sender, RoutedEventArgs e) => _ = TryUnlockAsync();

    async Task TryUnlockAsync()
    {
        string p = LockBox.Password;
        if (p.Length == 0) return;
        LockBox.IsEnabled = false;
        try
        {
            if (await _chat.CheckPassphraseAsync(p))
            {
                _locked = false;
                _lastInput = DateTime.Now;
                LockOverlay.Visibility = Visibility.Collapsed;
                Input.Focus();
            }
            else LockError.Text = "Wrong passphrase.";
        }
        catch (Exception ex) { LockError.Text = ex.Message; }
        finally
        {
            LockBox.IsEnabled = true;
            LockBox.Clear();
            if (_locked) LockBox.Focus();
        }
    }

    bool Confirm(string msg)
        => MessageBox.Show(this, msg, "Chat", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;

    async Task Guard(Func<Task> action)
    {
        try { await action(); }
        catch (Exception ex) { SetStatus(ex.Message, true); }
    }

    void SetStatus(string text, bool error)
    {
        StatusText.Text = text;
        StatusText.Foreground = (Brush)FindResource(error ? "Danger" : "Muted");
    }
}
