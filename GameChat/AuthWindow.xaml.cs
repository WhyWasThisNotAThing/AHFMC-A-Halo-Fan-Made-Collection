using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace AHFMCChat;

public partial class AuthWindow : Window
{
    readonly EventHub _hub = new();
    ChatClient? _chat;
    bool _signup;
    int _step;

    public ChatClient? Chat { get; private set; }
    public EventHub Hub => _hub;

    public AuthWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => { EmailBox.Focus(); _ = TryResumeAsync(); };
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

    void Privacy_Click(object sender, RoutedEventArgs e) => LegalWindow.Show(this, true);
    void Rules_Click(object sender, RoutedEventArgs e) => RulesWindow.View(this);
    void Terms_Click(object sender, RoutedEventArgs e) => LegalWindow.Show(this, false);
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
            if (!RulesWindow.Confirm(this)) { Error("You need to read and accept the Chat Rules to create an account."); return; }
        }

        string? captcha = null;
        if (CaptchaWindow.Enabled)
        {
            captcha = await CaptchaWindow.GetTokenAsync(this);
            if (captcha == null) { Error("Human verification wasn't completed. Please try again."); return; }
        }

        _chat?.Dispose();
        _chat = new ChatClient(Config.SupabaseUrl, Config.AnonKey);
        _chat.OnEvent += _hub.Push;

        string? err = _signup ? await _chat.SignUpAsync(email, pw, captcha) : await _chat.LoginAsync(email, pw, captcha);
        if (err != null) { Error(err); return; }

        SessionStore.Enabled = RememberBox.IsChecked == true;
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
        if (await Task.Run(() => _chat.UnlockAsync(pass))) Finish();
        else { Error("Wrong passphrase."); UnlockBox.Clear(); }
    });

    Task CreateAsync() => Busy(async () =>
    {
        if (_chat == null) return;
        string name = NameBox.Text.Trim(), p1 = Pass1.Password;
        if (p1 != Pass2.Password) { Error("Passphrases don't match."); return; }
        string? err = await Task.Run(() => _chat.CreateAccountAsync(name, p1));
        if (err != null) { Error(err); return; }
        Finish();
    });

    void Finish()
    {
        Chat = _chat;
        DialogResult = true;
    }

    async Task TryResumeAsync()
    {
        string? token = SessionStore.Load();
        if (token == null) return;
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
            ShowStep(await _chat.HasProfileAsync() ? 1 : 2);
        });
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
            if (DialogResult != true) FocusCurrent();
        }
    }

    void Error(string msg) { ErrorText.Text = msg; ErrorText.Visibility = Visibility.Visible; }
    void ClearError() => ErrorText.Visibility = Visibility.Collapsed;

    void Window_Closed(object? sender, EventArgs e)
    {
        if (Chat == null) _chat?.Dispose();
    }
}
