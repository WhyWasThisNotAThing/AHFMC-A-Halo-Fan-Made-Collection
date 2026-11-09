using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace AHFMCChat;

public partial class AuthView : UserControl
{
    readonly EventHub _hub = new();
    ChatClient? _chat;
    bool _signup;
    int _step;

    public ChatClient? Chat { get; private set; }
    bool _done;
    Window Win => Window.GetWindow(this)!;

    public event Action<ChatClient, EventHub>? Completed;
    public EventHub Hub => _hub;

    public AuthView()
    {
        InitializeComponent();
        RememberBox.IsChecked = LocalSettings.AutoSignIn;
        Loaded += (_, _) => { EmailBox.Focus(); _ = AutoSignInAsync(); };
    }

    void ShowStep(int step)
    {
        _step = step;
        StepLogin.Visibility = step == 0 ? Visibility.Visible : Visibility.Collapsed;
        StepUnlock.Visibility = step == 1 ? Visibility.Visible : Visibility.Collapsed;
        StepSetup.Visibility = step == 2 ? Visibility.Visible : Visibility.Collapsed;
        ClearError();
        FocusCurrent();
    }

    void FocusCurrent()
    {
        switch (_step)
        {
            case 0: (string.IsNullOrEmpty(EmailBox.Text) ? EmailBox : (UIElement)PassBox).Focus(); break;
            case 1: UnlockBox.Focus(); break;
            default: NameBox.Focus(); break;
        }
    }

    void Mode_Click(object sender, RoutedEventArgs e)
    {
        _signup = !_signup;
        ConfirmPanel.Visibility = _signup ? Visibility.Visible : Visibility.Collapsed;
        AgreePanel.Visibility = _signup ? Visibility.Visible : Visibility.Collapsed;
        SignupNamePanel.Visibility = _signup ? Visibility.Visible : Visibility.Collapsed;
        IdLabel.Visibility = EmailBox.Visibility = _signup ? Visibility.Collapsed : Visibility.Visible;
        LoginBtn.Content = _signup ? "Create account" : "Log in";
        ModeLink.Content = _signup ? "Already have an account? Log in" : "New here? Create an account";
        ClearError();
    }

    void Privacy_Click(object sender, RoutedEventArgs e) => LegalWindow.Show(Window.GetWindow(this), true);
    void Rules_Click(object sender, RoutedEventArgs e) => RulesWindow.View(Win);
    void Terms_Click(object sender, RoutedEventArgs e) => LegalWindow.Show(Window.GetWindow(this), false);
    void Conduct_Click(object sender, RoutedEventArgs e) => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("https://github.com/WhyWasThisNotAThing/AHFMC-A-Halo-Fan-Made-Collection/blob/HEAD/CODE_OF_CONDUCT.md") { UseShellExecute = true });

    void Back_Click(object sender, RoutedEventArgs e)
    {
        _chat?.Dispose();
        _chat = null;
        SessionStore.Clear();
        SuggestBox.Clear();
        SuggestBox.Visibility = SuggestHint.Visibility = Visibility.Collapsed;
        UnlockBox.Clear(); Pass1.Clear(); Pass2.Clear(); PassBox.Clear(); ConfirmBox.Clear();
        ShowStep(0);
    }

    void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || BusyBar.Visibility == Visibility.Visible) return;
        e.Handled = true;
        _ = _step switch { 0 => LoginAsync(), 1 => UnlockAsync(), _ => CreateAsync() };
    }

    async void Login_Click(object sender, RoutedEventArgs e) => await LoginAsync();
    async void Unlock_Click(object sender, RoutedEventArgs e) => await UnlockAsync();
    async void Create_Click(object sender, RoutedEventArgs e) => await CreateAsync();

    Task LoginAsync() => Busy(async () =>
    {
        string email = EmailBox.Text.Trim();
        string pw = PassBox.Password;
        string username = "";
        if (_signup)
        {
            username = SignupNameBox.Text.Trim();
            if (!Regex.IsMatch(username, "^[A-Za-z0-9_]{3,16}$")) { Error("Username must be 3-16 letters, numbers or underscores."); return; }
            email = username;
        }
        else if (email.Length == 0) { Error("Enter your username or email."); return; }
        if (pw.Length == 0) { Error("Enter your password."); return; }
        if (_signup)
        {
            if (pw.Length < 8) { Error("Password must be at least 8 characters."); return; }
            if (pw != ConfirmBox.Password) { Error("Passwords don't match."); return; }
            if (AgreeBox.IsChecked != true) { Error("To create an account, tick the box to confirm you are 16 or older and agree to the Privacy Policy, Terms of Service and Code of Conduct."); return; }
            if (!RulesWindow.Confirm(Win)) { Error("You need to read and accept the Chat Rules to create an account."); return; }
        }

        _chat?.Dispose();
        _chat = new ChatClient(Config.SupabaseUrl, Config.AnonKey);
        _chat.OnEvent += _hub.Push;

        if (_signup && await _chat.UsernameTooCloseAsync(username)) { Error(ChatClient.NameTooCloseMessage); return; }

        string? captcha = null;
        if (CaptchaWindow.Enabled)
        {
            captcha = await CaptchaWindow.GetTokenAsync(Win);
            if (captcha == null) { Error("Human verification wasn't completed. Please try again."); return; }
        }

        string? err = _signup ? await _chat.SignUpAsync(email, pw, captcha) : await _chat.LoginAsync(email, pw, captcha);
        if (err != null) { Error(CaptchaHelp(err)); return; }

        SessionStore.Enabled = RememberBox.IsChecked == true;
        LocalSettings.AutoSignIn = SessionStore.Enabled;
        LocalSettings.Save();
        if (SessionStore.Enabled) { SessionStore.Save(_chat.RefreshToken); _chat.RefreshTokenChanged += SessionStore.Save; }
        else SessionStore.Clear();

        bool hasProfile = await _chat.HasProfileAsync();
        if (!hasProfile)
        {
            bool usernameOnly = !email.Contains('@');
            NameBox.Text = _signup ? username : (usernameOnly ? email : "");
            NameBox.IsReadOnly = usernameOnly;
        }
        ShowStep(hasProfile ? 1 : 2);
    });

    Task UnlockAsync() => Busy(async () =>
    {
        if (_chat == null) return;
        string pass = UnlockBox.Password;
        if (pass.Length == 0) { Error("Enter your passphrase."); return; }
        if (await Task.Run(() => _chat.UnlockAsync(pass))) { SessionStore.SavePassphrase(pass); Finish(); }
        else { Error("Wrong passphrase."); UnlockBox.Clear(); }
    });

    Task CreateAsync() => Busy(async () =>
    {
        if (_chat == null) return;
        string name = NameBox.Text.Trim(), p1 = Pass1.Password;
        if (p1 != Pass2.Password) { Error("Passphrases don't match."); return; }
        string? err = await Task.Run(() => _chat.CreateAccountAsync(name, p1));
        if (err != null) { Error(err); return; }
        SessionStore.SavePassphrase(p1);
        Finish();
    });

    void Finish()
    {
        Chat = _chat;
        _done = true;
        Completed?.Invoke(_chat!, _hub);
    }

    bool _resuming;

    public async Task AutoSignInAsync()
    {
        if (_resuming || _done || _chat != null) return;
        string? token = SessionStore.Load();
        if (token == null) return;
        _resuming = true;
        try
        {
            await Busy(async () =>
            {
                var chat = new ChatClient(Config.SupabaseUrl, Config.AnonKey);
                chat.OnEvent += _hub.Push;
                bool ok;
                try { ok = await chat.ResumeAsync(token); }
                catch { chat.Dispose(); return; }
                if (!ok) { chat.Dispose(); SessionStore.Clear(); return; }

                _chat = chat;
                SessionStore.Enabled = true;
                _chat.RefreshTokenChanged += SessionStore.Save;
                RememberBox.IsChecked = true;

                if (!await _chat.HasProfileAsync()) { ShowStep(2); return; }

                string? saved = SessionStore.LoadPassphrase();
                if (saved != null)
                {
                    bool unlocked = false;
                    try { unlocked = await Task.Run(() => _chat.UnlockAsync(saved)); } catch { }
                    if (unlocked) { Finish(); return; }
                    SessionStore.ClearPassphrase();
                }
                ShowStep(1);
            });
        }
        finally { _resuming = false; }
    }

    void Suggest_Click(object sender, RoutedEventArgs e)
    {
        string p = ChatClient.GeneratePassphrase();
        SuggestBox.Text = p;
        Pass1.Password = p;
        Pass2.Password = p;
        SuggestBox.Visibility = SuggestHint.Visibility = Visibility.Visible;
    }

    async Task Busy(Func<Task> work)
    {
        BusyBar.Visibility = Visibility.Visible;
        IsEnabled = false;
        ClearError();
        try { await work(); }
        catch (Exception ex) { Error(ex.Message); }
        finally
        {
            BusyBar.Visibility = Visibility.Collapsed;
            IsEnabled = true;
            if (!_done) FocusCurrent();
        }
    }

    static string CaptchaHelp(string err) =>
        err.Contains("captcha", StringComparison.OrdinalIgnoreCase) && !CaptchaWindow.Enabled
            ? err + "\n\nThe server requires CAPTCHA but this app has no CAPTCHA site key. Put your Cloudflare Turnstile site key in GameChat/Config.cs (CaptchaSiteKey), or turn off CAPTCHA protection in Supabase (Authentication > Attack Protection)."
            : err;

    void Error(string msg) { ErrorText.Text = msg; ErrorText.Visibility = Visibility.Visible; }
    void ClearError() => ErrorText.Visibility = Visibility.Collapsed;

    public void Cancel()
    {
        if (!_done) _chat?.Dispose();
    }
}
