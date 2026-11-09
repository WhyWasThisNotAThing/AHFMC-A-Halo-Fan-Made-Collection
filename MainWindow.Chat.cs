using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace AHFMC;

public partial class MainWindow
{
    private AHFMCChat.AuthView? _chatAuth;
    private AHFMCChat.ChatView? _chatView;
    private bool _settingsHidClassic;

    private void Chat_Click(object sender, RoutedEventArgs e) => ShowChat();

    private void ShowChat()
    {
        if (_chatView == null && _chatAuth == null) StartChatLogin();
        else if (_chatView == null) _ = _chatAuth?.AutoSignInAsync();

        ChatPane.Width = ChatLogicalWidth;
        ChatPane.Visibility = Visibility.Visible;
        UpdateUiScale();
        if (ClassicPane.Visibility == Visibility.Visible)
            ClassicSend(new { type = "chatOpen", value = true });
        Dispatcher.BeginInvoke(new Action(() => _chatView?.FocusInput()), DispatcherPriority.Input);
    }

    private void HideChat()
    {
        ChatPane.Visibility = Visibility.Collapsed;
        UpdateUiScale();
        ClassicSend(new { type = "chatOpen", value = false });
        if (ClassicPane.Visibility == Visibility.Visible) ClassicView.Focus();
    }

    private object ChatState() => new
    {
        user = _chatView?.Username ?? "",
        auto = AHFMCChat.LocalSettings.AutoSignIn,
        notif = AHFMCChat.LocalSettings.Notifications,
        prev = AHFMCChat.LocalSettings.ShowPreviews,
        lobby = AHFMCChat.LocalSettings.LobbyNotifications,
        overlay = AHFMCChat.LocalSettings.OverlayInGame,
        @lock = AHFMCChat.LocalSettings.AutoLockMinutes,
        warn = AHFMCChat.LocalSettings.WarnUnverified
    };

    private void ChatSettingChanged()
    {
        AHFMCChat.LocalSettings.Save();
        PushClassicState();
    }

    private void SetChatSetting(string key, int value)
    {
        switch (key)
        {
            case "auto":
                AHFMCChat.LocalSettings.AutoSignIn = value == 1;
                if (value != 1) AHFMCChat.SessionStore.Clear();
                break;
            case "notif": AHFMCChat.LocalSettings.Notifications = value == 1; break;
            case "prev": AHFMCChat.LocalSettings.ShowPreviews = value == 1; break;
            case "lobby": AHFMCChat.LocalSettings.LobbyNotifications = value == 1; break;
            case "overlay": AHFMCChat.LocalSettings.OverlayInGame = value == 1; break;
            case "lock": AHFMCChat.LocalSettings.AutoLockMinutes = (value is 5 or 10 or 30) ? value : 0; break;
            case "warn": AHFMCChat.LocalSettings.WarnUnverified = value == 1; break;
        }
        AHFMCChat.LocalSettings.Save();
        RefreshSettings();
    }

    private void CloseChat_Click(object sender, RoutedEventArgs e) => HideChat();

    private void StartChatAutoSignIn()
    {
        if (_chatView != null || _chatAuth != null) return;
        if (!AHFMCChat.LocalSettings.AutoSignIn || AHFMCChat.SessionStore.Load() == null) return;
        StartChatLogin();
    }

    private void StartChatLogin()
    {
        _chatAuth?.Cancel();
        var auth = new AHFMCChat.AuthView { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        auth.Completed += (chat, hub) =>
        {
            ChatHost.Children.Clear();
            _chatAuth = null;
            var view = new AHFMCChat.ChatView(chat, hub);
            view.LoggedOutRequested += () =>
            {
                AHFMCChat.SessionStore.Clear();
                view.Shutdown();
                _chatView = null;
                UpdateChatBadge(0);
                UpdateChatTray();
                PushClassicState();
                StartChatLogin();
            };
            view.Notify += OnChatNotify;
            view.UnreadChanged += UpdateChatBadge;
            view.SettingsRequested += OpenChatSettings;
            _chatView = view;
            ChatHost.Children.Add(view);
            UpdateChatTray();
            PushClassicState();
            if (ChatPane.Visibility == Visibility.Visible)
                Dispatcher.BeginInvoke(new Action(view.FocusInput), DispatcherPriority.Input);
        };
        _chatAuth = auth;
        ChatHost.Children.Clear();
        ChatHost.Children.Add(auth);
        _ = auth.AutoSignInAsync();
    }

    private void ShutdownChat()
    {
        try { _chatAuth?.Cancel(); } catch { }
        try { _chatView?.Shutdown(); } catch { }
    }

    private void UpdateChatBadge(int unread) => ChatBtn.Content = unread > 0 ? $"Chat  ({unread})" : "Chat";

    private void OnChatNotify(string title, string body)
    {
        try
        {
            bool looking = IsVisible && WindowState != WindowState.Minimized && IsActive;
            if (AHFMCChat.LocalSettings.OverlayInGame) { ShowGameToast(title, body); return; }

            if (looking && ClassicPane.Visibility != Visibility.Visible)
            {
                if (ChatPane.Visibility != Visibility.Visible) ShowToast(title, body);
                return;
            }
            UpdateChatTray();
            _tray?.ShowBalloonTip(5000, title, body, System.Windows.Forms.ToolTipIcon.None);
        }
        catch { }
    }

    [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "GetForegroundWindow")] private static extern IntPtr FsGetForegroundWindow();
    [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "GetWindowRect")] private static extern bool FsGetWindowRect(IntPtr h, out FsRect r);
    [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "MonitorFromWindow")] private static extern IntPtr FsMonitorFromWindow(IntPtr h, uint flags);
    [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "GetMonitorInfoW", CharSet = System.Runtime.InteropServices.CharSet.Unicode)] private static extern bool FsGetMonitorInfo(IntPtr m, ref FsMonitorInfo mi);
    [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "GetWindowThreadProcessId")] private static extern uint FsGetWindowThreadProcessId(IntPtr h, out uint pid);
    [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "GetClassNameW", CharSet = System.Runtime.InteropServices.CharSet.Unicode)] private static extern int FsGetClassName(IntPtr h, System.Text.StringBuilder sb, int max);
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)] private struct FsRect { public int Left, Top, Right, Bottom; }
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)] private struct FsMonitorInfo { public int cbSize; public FsRect rcMonitor, rcWork; public uint dwFlags; }

    private bool ForegroundIsFullscreen()
    {
        try
        {
            var h = FsGetForegroundWindow();
            if (h == IntPtr.Zero) return false;
            FsGetWindowThreadProcessId(h, out uint pid);
            if (pid == (uint)Environment.ProcessId) return false;
            var sb = new System.Text.StringBuilder(64);
            FsGetClassName(h, sb, 64);
            if (sb.ToString() is "Progman" or "WorkerW" or "Shell_TrayWnd" or "Shell_SecondaryTrayWnd") return false;
            if (!FsGetWindowRect(h, out var r)) return false;
            var mi = new FsMonitorInfo { cbSize = System.Runtime.InteropServices.Marshal.SizeOf<FsMonitorInfo>() };
            if (!FsGetMonitorInfo(FsMonitorFromWindow(h, 2), ref mi)) return false;
            return r.Left <= mi.rcMonitor.Left && r.Top <= mi.rcMonitor.Top && r.Right >= mi.rcMonitor.Right && r.Bottom >= mi.rcMonitor.Bottom;
        }
        catch { return false; }
    }

    private GameToastWindow? _gameToast;

    private void ShowGameToast(string title, string body)
    {
        try
        {
            if (_gameToast == null)
            {
                _gameToast = new GameToastWindow();
                _gameToast.Clicked += () => Dispatcher.Invoke(OpenChatFromNotification);
            }
            _gameToast.Pop(title, body);
        }
        catch { }
    }

    private DispatcherTimer? _toastTimer;

    private void ShowToast(string title, string body)
    {
        ToastTitle.Text = title;
        ToastBody.Text = body;
        ChatToast.Visibility = Visibility.Visible;
        if (_toastTimer == null)
        {
            _toastTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(6) };
            _toastTimer.Tick += (_, _) => { _toastTimer!.Stop(); ChatToast.Visibility = Visibility.Collapsed; };
        }
        _toastTimer.Stop();
        _toastTimer.Start();
    }

    private void ChatToast_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        ChatToast.Visibility = Visibility.Collapsed;
        OpenChatFromNotification();
    }

    private void OpenChatFromNotification()
    {
        OpenChatFromTray();
        if (_chatView != null) _chatView.OpenConversation(_chatView.LastNotifyKind, _chatView.LastNotifyKey);
    }

    private void SendTestNotification()
    {
        try
        {
            AHFMCChat.SoundEngine.PlayDing();
            if (AHFMCChat.LocalSettings.OverlayInGame) ShowGameToast("Test notification", "This is how a chat message looks on top of a game.");
            else if (ClassicPane.Visibility != Visibility.Visible && ChatPane.Visibility != Visibility.Visible)
                ShowToast("Test notification", "This is how a new chat message looks inside the launcher.");
            if (_tray == null) CreateTray();
            if (_tray == null) return;
            _tray.Visible = true;
            if (!AHFMCChat.LocalSettings.OverlayInGame) _tray.ShowBalloonTip(5000, "Test notification", "This is the Windows notification for a new chat message.", System.Windows.Forms.ToolTipIcon.None);
        }
        catch { }
    }

    private void UpdateChatTray()
    {
        if (_chatView != null)
        {
            if (_tray == null) CreateTray();
            if (_tray == null) return;
            if (!_gameRunning) _tray.Text = "AHFMC: A Halo Fan-Made Collection";
            _tray.Visible = true;
        }
        else if (!_gameRunning && !_parked && _tray != null) _tray.Visible = false;
    }

    private void OpenChatFromTray()
    {
        if (!IsVisible || WindowState == WindowState.Minimized || _parked) RestoreFromTray();
        else { Topmost = true; Activate(); Topmost = _config.WindowMode == 0; }
        ShowChat();
    }

    private void OpenChatSettings()
    {
        HideChat();
        int i = _cats.FindIndex(c => c.Name == "Chat");
        if (i >= 0) ShowCategory(_cats[i]);
        if (ClassicPane.Visibility == Visibility.Visible)
        {
            _settingsHidClassic = true;
            ClassicPane.Visibility = Visibility.Collapsed;
            SettingsPane.Background = Brushes.Black;
        }
        OpenSettings();
    }

    private void CloseSettingsPane()
    {
        SettingsPane.Visibility = Visibility.Collapsed;
        SettingsPane.Background = Br("#E6050810");
        if (_settingsHidClassic)
        {
            _settingsHidClassic = false;
            ClassicPane.Visibility = Visibility.Visible;
            ClassicView.Focus();
        }
    }

    private void ToggleAutoSignIn()
    {
        bool on = !AHFMCChat.LocalSettings.AutoSignIn;
        AHFMCChat.LocalSettings.AutoSignIn = on;
        AHFMCChat.LocalSettings.Save();
        PushClassicState();
        if (!on) AHFMCChat.SessionStore.Clear();
        else _ = ShowDialogAsync("Stay signed in", "You will be remembered the next time you log in to the chat.", "OK", null);
    }

    private async Task ShowFingerprintAsync()
    {
        if (_chatView == null) { await ShowDialogAsync("Chat", "Sign in to the chat first.", "OK", null); return; }
        try { await ShowDialogAsync("Your fingerprint", await _chatView.GetFingerprintAsync() + "\n\nContacts can compare it with theirs to check it's really you.", "Close", null); }
        catch (Exception ex) { await ShowDialogAsync("Chat", ex.Message, "OK", null); }
    }

    private async Task ChatAccountActionAsync(AHFMCChat.SettingsAction action)
    {
        if (_chatView == null) { await ShowDialogAsync("Chat", "Sign in to the chat first.", "OK", null); return; }
        switch (action)
        {
            case AHFMCChat.SettingsAction.Logout:
                if (!await ShowDialogAsync("Log out of the chat?", "You can log back in any time. This also forgets the saved login on this PC.", "Log out", "Cancel")) return;
                break;
            case AHFMCChat.SettingsAction.DeleteMessages:
                if (!await ShowDialogAsync("Delete all my messages?", "Every message you sent (lobby, parties and DMs) is deleted. DMs disappear for the other person too. This can't be undone.", "Delete them", "Keep them")) return;
                break;
            case AHFMCChat.SettingsAction.DeleteAccount:
                if (!await ShowDialogAsync("Delete my account?", "This permanently deletes your account, your keys and all your messages and parties. It cannot be undone.\n\nType DELETE to confirm:", "Delete account", "Cancel", "")) return;
                if (DialogInput.Text.Trim() != "DELETE") return;
                break;
        }
        if (_chatView != null) await _chatView.RunSettingsAction(action);
    }
}
