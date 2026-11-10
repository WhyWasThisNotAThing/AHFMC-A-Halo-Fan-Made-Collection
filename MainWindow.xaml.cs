using System.IO;
using System.IO.Compression;
using System.Diagnostics;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using Microsoft.Web.WebView2.Core;
using Microsoft.Win32;
using System.Text.RegularExpressions;

namespace AHFMC;

public class Config
{
    public string? Halo3Exe { get; set; }
    public string? ReachExe { get; set; }
    public string? Halo3Version { get; set; }
    public string? ReachVersion { get; set; }
    public int Layout { get; set; }
    public bool HideMissing { get; set; }
    public bool StartScreen { get; set; } = true;
    public int CursorMode { get; set; }
    public int WindowMode { get; set; }
    public int Countdown { get; set; } = 2;
    public bool MaximizeGame { get; set; } = true;
    public bool CloseToTray { get; set; }
    public int MasterVolume { get; set; } = 100;
    public int MusicVolume { get; set; } = 100;
    public int MenuVolume { get; set; } = 100;
    public int CountdownVolume { get; set; } = 100;
    public bool Halo3Manual { get; set; }
    public bool ReachManual { get; set; }
    public string? OceExe { get; set; }
    public string? OceVersion { get; set; }
    public bool OceManual { get; set; }
    public Dictionary<string, string> OfficialExe { get; set; } = new();
    public string? GravemindExe { get; set; }
    public string? GravemindVersion { get; set; }
    public bool GravemindManual { get; set; }
    public string? RavenExe { get; set; }
    public string? RavenVersion { get; set; }
    public bool RavenManual { get; set; }
    public string? GroundsideExe { get; set; }
    public bool GroundsideManual { get; set; }
    public string? GroundsideStamp { get; set; }
    public Dictionary<string, PlayStat> PlayStats { get; set; } = new();
    public string? GroundsideSha { get; set; }
    public bool ShowNewsOnStart { get; set; } = true;
    public bool NewsHalo3 { get; set; } = true;
    public bool NewsReach { get; set; } = true;
    public bool NewsOce { get; set; } = true;
    public bool NewsGravemind { get; set; } = true;
    public bool NewsRaven { get; set; } = true;
    public bool NewsLauncher { get; set; } = true;
    public bool AutoUpdateLauncher { get; set; } = true;
    public bool DiscordPresence { get; set; } = true;
    public bool? SetupDone { get; set; }
    public Dictionary<string, string> NewsSeen { get; set; } = new();
    public List<CustomApp> CustomApps { get; set; } = new();
    public Dictionary<string, string> GameLogos { get; set; } = new();
    public Dictionary<string, string> MenuSounds { get; set; } = new();
    public bool BigPictureBlur { get; set; } = true;
}

public partial class MainWindow : Window
{
    [DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    private const int SW_MAXIMIZE = 3;

    private static readonly string Root = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AHFMC");
    private static readonly string ConfigPath = Path.Combine(Root, "config.json");

    private const string Halo3Repo = "ProjectReclaimer/project-reclaimer-releases";
    private const string Halo3Prefix = "project-reclaimer-client";
    private const string Halo3Ext = ".exe";

    private const string GravemindRepo = "BriRoz1/h2gravemind-releases";
    private const string GravemindPrefix = "";
    private const string GravemindExt = ".exe";

    private const string RavenRepo = "bryantmh/project-raven";
    private const string RavenPrefix = "raven";
    private const string RavenExt = ".exe";
    private const string RavenFallbackUrl = "https://github.com/bryantmh/project-raven/releases/download/v1.0.1/raven.exe";


    private const string ReachRepo = "NETF1ix/reach-renaissance-releases";
    private const string ReachPrefix = "ReachRenaissance";
    private const string ReachExt = ".zip";

    private const string CommunityUrl = "https://opencommunityedition.com/";

    private const string MccSteamUri = "steam://run/976730";
    private const string MccSteamNoEacUri = "steam://run/976730//-eac_launcher/";
    private const string MccXboxUri = "ms-xbl-38616168://";

    private Config _config = new();

    public MainWindow()
    {
        InitializeComponent();
        ClassicView.DefaultBackgroundColor = System.Drawing.Color.Black;
        LoadConfig();
        System.Windows.Application.Current.SessionEnding += (_, _) => _reallyExit = true;
        AHFMCChat.SoundEngine.Master = Math.Clamp(_config.MasterVolume, 0, 100);
        StartupCover.Visibility = IsWebLayout(_config.Layout) ? Visibility.Visible : Visibility.Collapsed;
        BuildSettings();
        UpdateSubtitles();
        RebuildMyApps();
        InitDiscord();
        StartGamepad();
        InitMenuSounds();
        PreviewMouseMove += (_, e) =>
        {
            if (_mouseHidden && (e.GetPosition(this) - _mouseRef).Length > 6) ShowMouse();
        };
        PreviewMouseDown += (_, _) => ShowMouse();
        SizeChanged += (_, _) => UpdateUiScale();
        Activated += async (_, _) => await RefreshInstalledAsync();
        var v = Assembly.GetExecutingAssembly().GetName().Version;
        VersionText.Text = v == null ? "" : $"v{v.Major}.{v.Minor}.{v.Build}";
    }

    private void ApplyFullscreen()
    {
        UpdateWinModeButtons();
        if (_config.WindowMode == 1) { ApplyWindowed(); return; }
        SetEdge(0);
        WindowState = WindowState.Normal;
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        System.Windows.Shell.WindowChrome.SetWindowChrome(this, null);
        Left = 0; Top = 0;
        Width = SystemParameters.PrimaryScreenWidth;
        Height = SystemParameters.PrimaryScreenHeight;
        Topmost = true;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        UpdateUiScale();
        ApplyFullscreen();
        ApplyCursorMode();
        _menuSoundsOn = true;
        RefreshOfficialPills();
        _ = RefreshInstalledAsync(force: true);
        _ = LoadNewsAsync();
        if (_config.Layout != 0) SetLayout(_config.Layout, save: false);
        if (_config.SetupDone == false) ShowSetup(firstTime: true);
        StartChatAutoSignIn();
        _ = AutoCheckLauncherUpdateAsync();
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape) return;
        if (_counting) return;
        PlayBackIfPaneOpen();
        if (_dlCts != null && DialogPane.Visibility != Visibility.Visible) { CancelDownload(); e.Handled = true; return; }
        if (SetupOpen && DialogPane.Visibility != Visibility.Visible) return;
        if (BrowserPane.Visibility == Visibility.Visible && DialogPane.Visibility != Visibility.Visible
            && ChatPane.Visibility != Visibility.Visible && (Keyboard.Modifiers & ModifierKeys.Shift) == 0) return;
        if (DialogPane.Visibility == Visibility.Visible) { AnswerDialog(false); return; }
        if (ChatPane.Visibility == Visibility.Visible) { HideChat(); return; }
        if (NewsPane.Visibility == Visibility.Visible) { NewsPane.Visibility = Visibility.Collapsed; return; }
        if (CreditsPane.Visibility == Visibility.Visible) { CreditsPane.Visibility = Visibility.Collapsed; return; }
        if (SettingsPane.Visibility == Visibility.Visible) { if (DropOpen) { CloseDrop(); return; } if (!SettingsStepBack()) CloseSettingsPane(); return; }
        if (BrowserPane.Visibility == Visibility.Visible) HideBrowser();
        else if (ClassicPane.Visibility != Visibility.Visible) Close();
    }

    private void Exit_Click(object sender, RoutedEventArgs e) => Close();

    private void WindowMode_Click(object sender, RoutedEventArgs e)
    {
        _config.WindowMode = 1 - _config.WindowMode;
        SaveConfig();
        ApplyFullscreen();
        PushClassicState();
        RefreshSettings();
    }

    private void UpdateWinModeButtons()
    {
        bool windowed = _config.WindowMode == 1;
        foreach (var b in new[] { WinModeBtn, WinModeBtn2 })
        {
            if (b == null) continue;
            b.Content = windowed ? "⛶" : "❐";
            b.ToolTip = windowed ? "Fullscreen" : "Windowed";
        }
    }

    private void Minimize_Click(object sender, RoutedEventArgs e)
    {
        Topmost = false;
        if (_gameRunning) HideToTray();
        else WindowState = WindowState.Minimized;
    }

    private void Window_StateChanged(object? sender, EventArgs e)
    {
        if (WindowState == WindowState.Minimized) { PauseHybrid(); return; }
        if (WindowState == WindowState.Normal && _config.WindowMode == 0) ApplyFullscreen();
        if (!_gameRunning) { SetPresenceBrowsing(); ResumeHybrid(); }
    }

    private void UpdateSubtitles()
    {
        bool reach = _config.ReachExe != null && File.Exists(_config.ReachExe);
        bool halo3 = _config.Halo3Exe != null && File.Exists(_config.Halo3Exe);
        ReachSub.Text = reach
            ? (_config.ReachManual ? "Installed: your own copy" : $"Ready to play {_config.ReachVersion}".TrimEnd())
            : "Not installed: click to download the latest version";
        Halo3Sub.Text = halo3
            ? (_config.Halo3Manual ? "Installed: your own copy" : $"Ready to play {_config.Halo3Version}".TrimEnd())
            : "Not installed: click to download the latest version";
        bool oce = _config.OceExe != null && File.Exists(_config.OceExe);
        OceSub.Text = oce
            ? (_config.OceManual ? "Installed: your own copy" : $"Ready to play {_config.OceVersion}".TrimEnd())
            : "Not installed: click to download the latest version";
        if (reach) ReachSub.Text += PlayedText("Halo Reach Renaissance");
        if (halo3) Halo3Sub.Text += PlayedText("Reclaimer");
        if (oce) OceSub.Text += PlayedText("OpenCE");
        Install.SetState(CommunityBtn, oce ? "Installed" : "Not installed");
        Install.SetState(ReachBtn, reach ? "Installed" : "Not installed");
        Install.SetState(Halo3Btn, halo3 ? "Installed" : "Not installed");
        UpdateOfficialPlayed();
        UpdateGravemindText();
        UpdateRavenText();
        UpdateGroundsideText();
        PushClassicState();
    }

    private void LoadConfig()
    {
        bool existed = File.Exists(ConfigPath);
        try
        {
            if (existed)
                _config = JsonSerializer.Deserialize<Config>(File.ReadAllText(ConfigPath)) ?? new Config();
        }
        catch { _config = new Config(); }

        if (!existed) _config.SetupDone = false;

        if (_config.GravemindExe != null && !_config.GravemindManual && _config.GravemindVersion == null
            && !_config.GravemindExe.StartsWith(Path.Combine(Root, "Halo2Gravemind"), StringComparison.OrdinalIgnoreCase))
            _config.GravemindManual = true;
    }

    private void SaveConfig()
    {
        Directory.CreateDirectory(Root);
        File.WriteAllText(ConfigPath, JsonSerializer.Serialize(_config, new JsonSerializerOptions { WriteIndented = true }));
    }

    private static readonly HttpClient Http = CreateHttp();

    private static HttpClient CreateHttp()
    {
        var c = new HttpClient();
        c.DefaultRequestHeaders.UserAgent.ParseAdd("AHFMC-Launcher");
        c.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        return c;
    }

    private record ReleaseInfo(string Tag, string AssetName, string DownloadUrl);

    private static async Task<ReleaseInfo> GetLatestAsync(string repo, string prefix, string ext)
    {
        string json = await Http.GetStringAsync($"https://api.github.com/repos/{repo}/releases?per_page=10");
        using var doc = JsonDocument.Parse(json);
        foreach (var rel in doc.RootElement.EnumerateArray())
        {
            if (rel.GetProperty("draft").GetBoolean()) continue;
            foreach (var asset in rel.GetProperty("assets").EnumerateArray())
            {
                string name = asset.GetProperty("name").GetString() ?? "";
                if (name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) &&
                    name.EndsWith(ext, StringComparison.OrdinalIgnoreCase) &&
                    !name.Contains("symbols", StringComparison.OrdinalIgnoreCase))
                {
                    return new ReleaseInfo(
                        rel.GetProperty("tag_name").GetString() ?? "",
                        name,
                        asset.GetProperty("browser_download_url").GetString()!);
                }
            }
        }
        throw new Exception($"No \"{prefix}*{ext}\" file found in the releases of {repo}.");
    }


    private static CancellationTokenSource? _dlCts;

    private static async Task DownloadAsync(string url, string dest, IProgress<double> progress)
    {
        var ct = _dlCts?.Token ?? default;
        try
        {
            using var resp = await Http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
            resp.EnsureSuccessStatusCode();
            long? total = resp.Content.Headers.ContentLength;
            await using var src = await resp.Content.ReadAsStreamAsync(ct);
            await using var fs = File.Create(dest);
            var buf = new byte[81920];
            long read = 0;
            int n;
            while ((n = await src.ReadAsync(buf, ct)) > 0)
            {
                await fs.WriteAsync(buf.AsMemory(0, n), ct);
                read += n;
                if (total > 0) progress.Report(read * 100.0 / total.Value);
            }
        }
        catch
        {
            try { File.Delete(dest); } catch { }
            throw;
        }
    }

    private async Task<ReleaseInfo?> CheckForNewerAsync(string repo, string prefix, string ext, string? installedTag)
    {
        try
        {
            SetBusy(true, "Checking for updates…");
            var rel = await GetLatestAsync(repo, prefix, ext);
            return rel.Tag != installedTag ? rel : null;
        }
        catch { return null; }
        finally { SetBusy(false, " "); }
    }

    private async Task InstallHalo3Async(ReleaseInfo rel)
    {
        string dir = Path.Combine(Root, "Halo3Reclaimer");
        Directory.CreateDirectory(dir);
        string dest = Path.Combine(dir, rel.AssetName);
        string tmp = dest + ".download";

        SetBusy(true, $"Downloading Reclaimer {rel.Tag}…", determinate: true);
        await DownloadAsync(rel.DownloadUrl, tmp, new Progress<double>(ReportProgress));

        foreach (var old in Directory.GetFiles(dir, Halo3Prefix + "*" + Halo3Ext))
            try { File.Delete(old); } catch { }
        File.Move(tmp, dest, true);

        _config.Halo3Exe = dest;
        _config.Halo3Manual = false;
        _config.Halo3Version = rel.Tag;
        SaveConfig();
        UpdateSubtitles();
    }

    private async Task PrepareHalo3Async()
    {
        bool installed = _config.Halo3Exe != null && File.Exists(_config.Halo3Exe);
        if (installed && _config.Halo3Manual) return;
        if (!installed)
        {
            SetBusy(true, "Finding the latest Reclaimer…");
            var rel = await GetLatestAsync(Halo3Repo, Halo3Prefix, Halo3Ext);
            await InstallHalo3Async(rel);
        }
        else
        {
            var newer = await CheckForNewerAsync(Halo3Repo, Halo3Prefix, Halo3Ext, _config.Halo3Version);
            if (newer != null && await AskAsync(
                    $"Reclaimer {newer.Tag} is available (you have {_config.Halo3Version ?? "an unknown version"}).\n\nUpdate now?",
                    "Update available"))
                await InstallHalo3Async(newer);
        }
    }

    private async void Halo3_Click(object sender, RoutedEventArgs e)
    {
        if (_counting || _busy) return;
        try
        {
            await RunWithCountdownAsync(true, PrepareHalo3Async);
            Launch(_config.Halo3Exe!, "Reclaimer");
        }
        catch (Exception ex) { ShowError(ex); }
        finally { SetBusy(false, " "); }
    }

    private async Task InstallReachAsync(ReleaseInfo rel)
    {
        string destDir = Path.Combine(Root, "HaloReachRenaissance");
        string zip = Path.Combine(Path.GetTempPath(), rel.AssetName);

        SetBusy(true, $"Downloading Halo Reach Renaissance {rel.Tag}…", determinate: true);
        await DownloadAsync(rel.DownloadUrl, zip, new Progress<double>(ReportProgress));

        SetBusy(true, "Installing Halo Reach Renaissance…");
        await Task.Run(() =>
        {
            Directory.CreateDirectory(destDir);
            ZipFile.ExtractToDirectory(zip, destDir, true);
        });
        try { File.Delete(zip); } catch { }

        string? exe = Directory
            .GetFiles(destDir, "Reach Renaissance.exe", SearchOption.AllDirectories)
            .FirstOrDefault();
        if (exe == null)
            throw new FileNotFoundException("The download didn't contain \"Reach Renaissance.exe\".");

        _config.ReachExe = exe;
        _config.ReachManual = false;
        _config.ReachVersion = rel.Tag;
        SaveConfig();
        UpdateSubtitles();
    }

    private async Task PrepareReachAsync()
    {
        bool installed = _config.ReachExe != null && File.Exists(_config.ReachExe);
        if (installed && _config.ReachManual) return;
        if (!installed)
        {
            SetBusy(true, "Finding the latest Halo Reach Renaissance…");
            var rel = await GetLatestAsync(ReachRepo, ReachPrefix, ReachExt);
            await InstallReachAsync(rel);
        }
        else
        {
            var newer = await CheckForNewerAsync(ReachRepo, ReachPrefix, ReachExt, _config.ReachVersion);
            if (newer != null && await AskAsync(
                    $"Halo Reach Renaissance {newer.Tag} is available (you have {_config.ReachVersion ?? "an unknown version"}).\n\nUpdate now?",
                    "Update available"))
                await InstallReachAsync(newer);
        }
    }

    private async void Reach_Click(object sender, RoutedEventArgs e)
    {
        if (_counting || _busy) return;
        try
        {
            await RunWithCountdownAsync(true, PrepareReachAsync);
            Launch(_config.ReachExe!, "Halo Reach Renaissance");
        }
        catch (Exception ex) { ShowError(ex); }
        finally { SetBusy(false, " "); }
    }

    private async Task InstallGravemindAsync(ReleaseInfo rel)
    {
        string dir = Path.Combine(Root, "Halo2Gravemind");
        Directory.CreateDirectory(dir);
        string dest = Path.Combine(dir, rel.AssetName);
        string tmp = dest + ".download";

        SetBusy(true, $"Downloading Halo 2 Gravemind {rel.Tag}…", determinate: true);
        await DownloadAsync(rel.DownloadUrl, tmp, new Progress<double>(ReportProgress));

        foreach (var old in Directory.GetFiles(dir, GravemindPrefix + "*" + GravemindExt))
            try { File.Delete(old); } catch { }
        File.Move(tmp, dest, true);

        _config.GravemindExe = dest;
        _config.GravemindManual = false;
        _config.GravemindVersion = rel.Tag;
        SaveConfig();
        UpdateSubtitles();
    }

    private async Task PrepareGravemindAsync()
    {
        bool installed = _config.GravemindExe != null && File.Exists(_config.GravemindExe);
        if (installed && _config.GravemindManual) return;
        if (!installed)
        {
            SetBusy(true, "Finding the latest Halo 2 Gravemind…");
            var rel = await GetLatestAsync(GravemindRepo, GravemindPrefix, GravemindExt);
            await InstallGravemindAsync(rel);
        }
        else
        {
            var newer = await CheckForNewerAsync(GravemindRepo, GravemindPrefix, GravemindExt, _config.GravemindVersion);
            if (newer != null && await AskAsync(
                    $"Halo 2 Gravemind {newer.Tag} is available (you have {_config.GravemindVersion ?? "an unknown version"}).\n\nUpdate now?",
                    "Update available"))
                await InstallGravemindAsync(newer);
        }
    }


    private async Task InstallRavenAsync(ReleaseInfo rel)
    {
        string dir = Path.Combine(Root, "ProjectRaven");
        Directory.CreateDirectory(dir);
        string dest = Path.Combine(dir, rel.AssetName);
        string tmp = dest + ".download";

        SetBusy(true, $"Downloading Project Raven {rel.Tag}…", determinate: true);
        await DownloadAsync(rel.DownloadUrl, tmp, new Progress<double>(ReportProgress));

        foreach (var old in Directory.GetFiles(dir, RavenPrefix + "*" + RavenExt))
            try { File.Delete(old); } catch { }
        File.Move(tmp, dest, true);

        _config.RavenExe = dest;
        _config.RavenManual = false;
        _config.RavenVersion = rel.Tag;
        SaveConfig();
        UpdateSubtitles();
    }

    private async Task PrepareRavenAsync()
    {
        bool installed = _config.RavenExe != null && File.Exists(_config.RavenExe);
        if (installed && _config.RavenManual) return;
        if (!installed)
        {
            SetBusy(true, "Finding the latest Project Raven…");
            ReleaseInfo rel;
            try { rel = await GetLatestAsync(RavenRepo, RavenPrefix, RavenExt); }
            catch { rel = new ReleaseInfo("v1.0.1", "raven.exe", RavenFallbackUrl); }
            await InstallRavenAsync(rel);
        }
        else
        {
            var newer = await CheckForNewerAsync(RavenRepo, RavenPrefix, RavenExt, _config.RavenVersion);
            if (newer != null && await AskAsync(
                    $"Project Raven {newer.Tag} is available (you have {_config.RavenVersion ?? "an unknown version"}).\n\nUpdate now?",
                    "Update available"))
                await InstallRavenAsync(newer);
        }
    }

    private bool _browserReady;

    private void ShowBrowser()
    {
        BrowserPane.Visibility = Visibility.Visible;
        SetPresencePlaying("OpenCE");
    }

    private void HideBrowser()
    {
        BrowserPane.Visibility = Visibility.Collapsed;
        ApplyCursorMode();
        if (_classicBehindBrowser) { _classicBehindBrowser = false; ClassicPane.Visibility = Visibility.Visible; ClassicView.Focus(); }
        if (!_gameRunning) SetPresenceBrowsing();
    }

    private string? _browserUrl;

    private async Task OpenInAppAsync(string url, bool showCommunityPresence)
    {
        if (showCommunityPresence)
        {
            if (_counting) return;
            await RunWithCountdownAsync(true, () => Task.CompletedTask);
        }
        try
        {
            if (ClassicPane.Visibility == Visibility.Visible) { ClassicPane.Visibility = Visibility.Collapsed; _classicBehindBrowser = true; }
            BrowserPane.Visibility = Visibility.Visible;
            _mouseHidden = false; Mouse.OverrideCursor = null;
            if (showCommunityPresence) SetPresencePlaying("OpenCE");

            if (!_browserReady)
            {
                var env = await GetEnvAsync();
                await Browser.EnsureCoreWebView2Async(env);
                Browser.CoreWebView2.NewWindowRequested += (s, a) =>
                {
                    a.Handled = true;
                    Browser.CoreWebView2.Navigate(a.Uri);
                };
                await Browser.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(
                    "window.addEventListener('keydown',function(e){if(e.key==='Escape'&&e.shiftKey){e.preventDefault();window.chrome.webview.postMessage('halo:closeBrowser');}},true);");
                Browser.CoreWebView2.WebMessageReceived += (s, a) =>
                {
                    try { if (a.TryGetWebMessageAsString() == "halo:closeBrowser") Dispatcher.Invoke(HideBrowser); } catch { }
                };
                _browserReady = true;
            }

            if (_browserUrl != url)
            {
                _browserUrl = url;
                Browser.CoreWebView2.Navigate(url);
            }
        }
        catch (Exception ex)
        {
            HideBrowser();
            ShowError(new Exception(
                "Couldn't start the built-in browser. Make sure the Microsoft Edge WebView2 Runtime is installed.\n\n" + ex.Message));
        }
    }

    private const string MccWorkshopUrl = "https://steamcommunity.com/app/976730/workshop/";
    private const string MccMyModsUrl = "https://steamcommunity.com/my/myworkshopfiles/?appid=976730&browsefilter=mysubscriptions";
    private async void MccModsInstall_Click(object sender, RoutedEventArgs e) => await OpenInAppAsync(MccWorkshopUrl, false);
    private async void MccModsManage_Click(object sender, RoutedEventArgs e) => await OpenInAppAsync(MccMyModsUrl, false);

    private const string OpenCeRepo = "OpenCommunityEdition/OpenCE";
    private const string OcePrefix = "halo-windows-release";
    private const string OceExt = ".zip";
    private record NewsItem(string Id, string Name, string Tag, string Label, string Date, string Body, string Url, bool IsNew);
    private List<NewsItem> _newsItems = new();
    private bool _newsLoaded, _newsDelivered;

    private bool NewsEnabled(string id) => id switch { "halo3" => _config.NewsHalo3, "reach" => _config.NewsReach, "gravemind" => _config.NewsGravemind, "launcher" => _config.NewsLauncher, "raven" => _config.NewsRaven, _ => _config.NewsOce };
    private List<NewsItem> VisibleNews() => _newsItems.Where(x => NewsEnabled(x.Id)).ToList();
    private bool NewsHasNew() => VisibleNews().Any(x => x.IsNew);

    private void MarkNewsSeen()
    {
        foreach (var x in VisibleNews()) _config.NewsSeen[x.Id] = x.Tag;
        SaveConfig();
    }

    private static string CleanNotes(string? body, int max)
    {
        if (string.IsNullOrWhiteSpace(body)) return "No release notes were written for this version.";
        string t = body.Replace("\r", "");
        t = Regex.Replace(t, @"!\[[^\]]*\]\([^)]*\)", "");
        t = Regex.Replace(t, @"\[([^\]]*)\]\([^)]*\)", "$1");
        t = Regex.Replace(t, @"<[^>]+>", "");
        t = Regex.Replace(t, @"^\s{0,3}#{1,6}\s*", "", RegexOptions.Multiline);
        t = Regex.Replace(t, @"^\s*[-*+]\s+", "\u2022 ", RegexOptions.Multiline);
        t = t.Replace("**", "").Replace("__", "").Replace("`", "");
        t = Regex.Replace(t, @"\n{3,}", "\n\n").Trim();
        if (t.Length > max) t = t[..max].TrimEnd() + "...";
        return t;
    }

    private async Task FetchNewsAsync()
    {
        var sources = new (string Id, string Name, string Repo)[]
        {
            ("launcher", "AHFMC: A Halo Fan-Made Collection (launcher)", LauncherRepo),
            ("halo3", "Reclaimer", Halo3Repo),
            ("reach", "Halo Reach Renaissance", ReachRepo),
            ("oce", "Open Community Edition", OpenCeRepo),
            ("gravemind", "Halo 2 Gravemind", GravemindRepo),
            ("raven", "Project Raven", RavenRepo),
        };
        var list = new List<NewsItem>();
        foreach (var s in sources)
        {
            try
            {
                string json = await Http.GetStringAsync($"https://api.github.com/repos/{s.Repo}/releases?per_page=5");
                using var doc = JsonDocument.Parse(json);
                foreach (var rel in doc.RootElement.EnumerateArray())
                {
                    if (rel.GetProperty("draft").GetBoolean()) continue;
                    string tag = rel.GetProperty("tag_name").GetString() ?? "";
                    string title = rel.TryGetProperty("name", out var n) && n.ValueKind == JsonValueKind.String ? n.GetString() ?? "" : "";
                    string date = rel.TryGetProperty("published_at", out var d) && d.ValueKind == JsonValueKind.String ? d.GetString() ?? "" : "";
                    if (date.Length >= 10) date = date[..10];
                    string body = rel.TryGetProperty("body", out var b) && b.ValueKind == JsonValueKind.String ? b.GetString() ?? "" : "";
                    string url = rel.TryGetProperty("html_url", out var u) && u.ValueKind == JsonValueKind.String ? u.GetString() ?? "" : $"https://github.com/{s.Repo}/releases";
                    string label = string.IsNullOrWhiteSpace(title) || title == tag ? tag : $"{tag} - {title}";
                    bool isNew = !_config.NewsSeen.TryGetValue(s.Id, out var seen) || seen != tag;
                    list.Add(new NewsItem(s.Id, s.Name, tag, label, date, body, url, isNew));
                    break;
                }
            }
            catch { }
        }
        _newsItems = list;
        _newsLoaded = true;
    }

    private async Task LoadNewsAsync()
    {
        await FetchNewsAsync();
        if (SetupOpen) return;
        if (IsWebLayout(_config.Layout)) PushNews();
        else if (_config.ShowNewsOnStart && NewsHasNew()) { _newsDelivered = true; ShowNewsPane(); MarkNewsSeen(); }
    }

    private void PushNews(bool allowShow = true)
    {
        if (!_classicReady || !_newsLoaded) return;
        bool show = allowShow && _config.ShowNewsOnStart && !_newsDelivered && NewsHasNew();
        ClassicSend(new
        {
            type = "news", show, total = _newsItems.Count,
            items = VisibleNews().Select(x => new { name = x.Name, label = x.Label, date = x.Date, url = x.Url, isNew = x.IsNew, notes = CleanNotes(x.Body, 900) }).ToArray()
        });
        if (show) { _newsDelivered = true; MarkNewsSeen(); }
    }

    private async void News_Click(object sender, RoutedEventArgs e)
    {
        if (!_newsLoaded) await FetchNewsAsync();
        ShowNewsPane();
        MarkNewsSeen();
    }

    private async Task InstallOceAsync(ReleaseInfo rel)
    {
        string destDir = Path.Combine(Root, "OpenCE");
        string zip = Path.Combine(Path.GetTempPath(), rel.AssetName);

        SetBusy(true, $"Downloading Open Community Edition {rel.Tag}…", determinate: true);
        await DownloadAsync(rel.DownloadUrl, zip, new Progress<double>(ReportProgress));

        SetBusy(true, "Installing Open Community Edition…");
        await Task.Run(() =>
        {
            Directory.CreateDirectory(destDir);
            ZipFile.ExtractToDirectory(zip, destDir, true);
        });
        try { File.Delete(zip); } catch { }

        string? exe = FindOceExe(destDir);
        if (exe == null)
            throw new FileNotFoundException("The download didn't contain a game .exe. You can pick it yourself in Settings > Games.");

        _config.OceExe = exe;
        _config.OceManual = false;
        _config.OceVersion = rel.Tag;
        SaveConfig();
        UpdateSubtitles();
    }

    private static string? FindOceExe(string dir)
    {
        var skip = new[] { "unins", "crash", "setup", "redist", "vc_", "dxsetup", "updater", "report" };
        var exes = Directory.GetFiles(dir, "*.exe", SearchOption.AllDirectories)
            .Where(f => !skip.Any(k => Path.GetFileName(f).Contains(k, StringComparison.OrdinalIgnoreCase))).ToList();
        if (exes.Count == 0) return null;
        return exes.OrderByDescending(f => Path.GetFileName(f).Contains("halo", StringComparison.OrdinalIgnoreCase) || Path.GetFileName(f).Contains("opence", StringComparison.OrdinalIgnoreCase))
                   .ThenBy(f => f.Count(c => c == Path.DirectorySeparatorChar))
                   .ThenByDescending(f => new FileInfo(f).Length)
                   .First();
    }

    private async Task PrepareOceAsync()
    {
        bool installed = _config.OceExe != null && File.Exists(_config.OceExe);
        if (installed && _config.OceManual) return;
        if (!installed)
        {
            SetBusy(true, "Finding the latest Open Community Edition…");
            var rel = await GetLatestAsync(OpenCeRepo, OcePrefix, OceExt);
            await InstallOceAsync(rel);
        }
        else
        {
            var newer = await CheckForNewerAsync(OpenCeRepo, OcePrefix, OceExt, _config.OceVersion);
            if (newer != null && await AskAsync(
                    $"Open Community Edition {newer.Tag} is available (you have {_config.OceVersion ?? "an unknown version"}).\n\nUpdate now?",
                    "Update available"))
                await InstallOceAsync(newer);
        }
    }

    private async void Community_Click(object sender, RoutedEventArgs e)
    {
        if (_counting || _busy) return;
        try
        {
            await RunWithCountdownAsync(true, PrepareOceAsync);
            Launch(_config.OceExe!, "OpenCE");
        }
        catch (Exception ex) { ShowError(ex); }
        finally { SetBusy(false, " "); }
    }

    private void CloseBrowser_Click(object sender, RoutedEventArgs e) => HideBrowser();

    private void MccSteam_Click(object sender, RoutedEventArgs e) => LaunchMcc(MccSteamUri);
    private void MccSteamNoEac_Click(object sender, RoutedEventArgs e) => LaunchMcc(MccSteamNoEacUri);
    private void MccXbox_Click(object sender, RoutedEventArgs e) => LaunchMcc(MccXboxUri);

    private static bool IsMccRunning()
    {
        foreach (var p in Process.GetProcesses())
        {
            try
            {
                if (p.ProcessName.StartsWith("MCC", StringComparison.OrdinalIgnoreCase)) return true;
            }
            catch { }
            finally { p.Dispose(); }
        }
        return false;
    }

    private async void LaunchMcc(string uri, string gameName = "Halo: The Master Chief Collection")
    {
        if (_counting) return;
        await RunWithCountdownAsync(false, () => Task.CompletedTask);
        try
        {
            Topmost = false;
            WindowState = WindowState.Minimized;

            try { Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true })?.Dispose(); }
            catch { ApplyFullscreen(); Activate(); throw; }

            EnterTray(gameName);

            StartWatchdog(IsMccRunning, 120);
        }
        catch (Exception ex) { ShowError(ex); }
    }

    private void Settings_Click(object sender, RoutedEventArgs e) => OpenSettings();

    private void CloseSettings_Click(object sender, RoutedEventArgs e) => CloseSettingsPane();

    private void SettingsScrim_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (ReferenceEquals(e.OriginalSource, SettingsPane)) CloseSettingsPane();
    }

    private void UpdateClassicToggleText() => RefreshSettings();

    private record GameOpt(string Label, string Uri, string Game);

    private static readonly Dictionary<string, GameOpt[]> OfficialGames = new()
    {
        ["mcc"] = new GameOpt[] { new("Steam", MccSteamUri, ""), new("Steam (no anti-cheat)", MccSteamNoEacUri, ""), new("Xbox / Microsoft Store", MccXboxUri, "") },
        ["h5forge"] = new GameOpt[] { new("Microsoft Store", "ms-windows-store://pdp/?ProductId=9nblggh51mwp", "Halo 5: Forge") },
        ["infinite"] = new GameOpt[] { new("Steam", "steam://run/1240440", "Halo Infinite"), new("Microsoft Store", "ms-windows-store://search/?query=Halo%20Infinite", "Halo Infinite") },
        ["ce2026"] = new GameOpt[] { new("Steam", "steam://run/2806050", "Halo: Campaign Evolved (2026)"), new("Microsoft Store", "ms-windows-store://search/?query=Halo%20Campaign%20Evolved", "Halo: Campaign Evolved (2026)") },
        ["hwde"] = new GameOpt[] { new("Steam", "steam://run/459220", "Halo Wars: Definitive Edition"), new("Microsoft Store", "ms-windows-store://search/?query=Halo%20Wars%20Definitive%20Edition", "Halo Wars: Definitive Edition") },
        ["hw2"] = new GameOpt[] { new("Microsoft Store", "ms-windows-store://pdp/?ProductId=9nblggh515bd", "Halo Wars 2") },
        ["spartanassault"] = new GameOpt[] { new("Steam", "steam://run/277430", "Halo: Spartan Assault"), new("Microsoft Store", "ms-windows-store://search/?query=Halo%20Spartan%20Assault", "Halo: Spartan Assault") },
        ["spartanstrike"] = new GameOpt[] { new("Steam", "steam://run/308220", "Halo: Spartan Strike"), new("Microsoft Store", "ms-windows-store://search/?query=Halo%20Spartan%20Strike", "Halo: Spartan Strike") },
    };

    private bool _classicReady, _classicBehindBrowser, _busy;
    private CoreWebView2Environment? _env;

    private static readonly string ClassicFolder = Path.Combine(Root, "ClassicHalo");
    private static readonly object ExtractLock = new();
    private static bool _classicExtracted;

    private static void EnsureClassicFiles()
    {
        lock (ExtractLock)
        {
            if (_classicExtracted) return;
            var asm = Assembly.GetExecutingAssembly();
            const string prefix = "ClassicHalo\\";
            var names = asm.GetManifestResourceNames()
                .Where(n => n.Replace('/', '\\').StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).ToList();
            if (names.Count == 0) throw new FileNotFoundException("The Classic Halo CE files are missing from this build of the launcher.");

            string logos = Path.Combine(ClassicFolder, "assets", "logos");
            try { if (Directory.Exists(logos)) Directory.Delete(logos, true); } catch { }

            foreach (var name in names)
            {
                string rel = name.Replace('/', '\\')[prefix.Length..];
                string dest = Path.Combine(ClassicFolder, rel);
                Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
                using var src = asm.GetManifestResourceStream(name)!;
                bool big = rel.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase) || rel.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase);
                if (big && File.Exists(dest) && new FileInfo(dest).Length == src.Length) continue;
                using var fs = File.Create(dest);
                src.CopyTo(fs);
            }
            _classicExtracted = true;
        }
    }

    private static readonly Dictionary<string, string[]> LogoNames = new()
    {
        ["mcc"] = new[] { "HaloMCC" },
        ["h5forge"] = new[] { "Halo5Forge" },
        ["infinite"] = new[] { "HaloInfinite" },
        ["ce2026"] = new[] { "HaloCe2026", "HsloCe2026" },
        ["hwde"] = new[] { "HaloWarsDE" },
        ["hw2"] = new[] { "HaloWars2" },
        ["spartanassault"] = new[] { "SpartanAssault" },
        ["spartanstrike"] = new[] { "SpartanStrike" },
        ["reach"] = new[] { "ReachRenaissance" },
        ["oce"] = new[] { "OpenCE" },
        ["halo3"] = new[] { "Halo3Reclaimer" },
        ["gravemind"] = new[] { "Halo2Gravemind" },
        ["raven"] = new[] { "ProjectRaven" },
        ["groundside"] = new[] { "ProjectGroundside" },
        ["addapp"] = new[] { "AddApp" },
    };

    private static Dictionary<string, string> FindLogos()
    {
        var found = new Dictionary<string, string>();
        try
        {
            string dir = Path.Combine(ClassicFolder, "assets", "logos");
            if (!Directory.Exists(dir)) return found;
            var files = Directory.GetFiles(dir);
            foreach (var (id, names) in LogoNames)
            {
                string? f = files.FirstOrDefault(x => names.Any(n => string.Equals(Path.GetFileNameWithoutExtension(x), n, StringComparison.OrdinalIgnoreCase)));
                if (f != null) found[id] = "assets/logos/" + Path.GetFileName(f) + "?v=" + File.GetLastWriteTimeUtc(f).Ticks;
            }
        }
        catch { }
        return found;
    }

    private Dictionary<string, string> AllLogos()
    {
        var all = FindLogos();
        foreach (var a in _config.CustomApps)
            if (AppImageUrl(a) is { } u) all["app:" + a.Id] = u;
        foreach (var kv in _config.GameLogos)
            if (GameLogoPath(kv.Key) != null) all[kv.Key] = "https://halo.apps/" + Uri.EscapeDataString(kv.Value);
        return all;
    }

    private async Task<CoreWebView2Environment> GetEnvAsync() =>
        _env ??= await CoreWebView2Environment.CreateAsync(null, Path.Combine(Root, "WebView2"),
            new CoreWebView2EnvironmentOptions { AdditionalBrowserArguments = "--autoplay-policy=no-user-gesture-required" });

    private async Task ShowClassicAsync()
    {
        try
        {
            await Task.Run(EnsureClassicFiles);
            var needed = new List<string> { "index.html" };
            if (_config.Layout >= 3) needed.AddRange(new[] { @"bigpicture\index.html", @"bigpicture\bigpicture.js", @"bigpicture\bigpicture.css" });
            foreach (var f in needed)
                if (!File.Exists(Path.Combine(ClassicFolder, f)))
                    throw new FileNotFoundException("The Classic Halo CE files are not set up correctly.\n\nMissing: " + Path.Combine(ClassicFolder, f));

            if (!_classicReady)
            {
                var env = await GetEnvAsync();
                await ClassicView.EnsureCoreWebView2Async(env);
                var core = ClassicView.CoreWebView2;
                core.SetVirtualHostNameToFolderMapping("halo.classic", ClassicFolder, CoreWebView2HostResourceAccessKind.Allow);
                Directory.CreateDirectory(AppImagesFolder);
                core.SetVirtualHostNameToFolderMapping("halo.apps", AppImagesFolder, CoreWebView2HostResourceAccessKind.Allow);
                Directory.CreateDirectory(MenuSoundsFolder);
                core.SetVirtualHostNameToFolderMapping("halo.menusounds", MenuSoundsFolder, CoreWebView2HostResourceAccessKind.Allow);
                core.Settings.AreDefaultContextMenusEnabled = false;
                core.Settings.AreDevToolsEnabled = false;
                core.Settings.IsStatusBarEnabled = false;
                core.Settings.IsZoomControlEnabled = false;
                core.Settings.AreBrowserAcceleratorKeysEnabled = false;
                UpdateUiScale();
                core.NewWindowRequested += (s, a) => a.Handled = true;
                core.NavigationStarting += (s, a) => { if (!a.Uri.StartsWith("https://halo.classic/", StringComparison.OrdinalIgnoreCase)) a.Cancel = true; };
                core.WebMessageReceived += Classic_WebMessage;
                core.NavigationCompleted += (s2, a2) => { if (!a2.IsSuccess) { ShowError(new Exception("The Classic Halo CE page failed to load.")); SetLayout(0); } };
                _classicReady = true;
                _webPage = WebPageUrl();
                core.Navigate(_webPage);
            }
            else
            {
                _padAnnounced = false;
                string url = WebPageUrl();
                if (!string.Equals(_webPage, url, StringComparison.Ordinal)) { _webPage = url; ClassicView.CoreWebView2.Navigate(url); }
                else ClassicSend(new { type = "show", start = _config.StartScreen });
            }

            SettingsPane.Visibility = Visibility.Collapsed;
            ClassicPane.Visibility = Visibility.Visible;
            StartupCover.Visibility = Visibility.Collapsed;
            ClassicView.Focus();
        }
        catch (Exception ex)
        {
            StartupCover.Visibility = Visibility.Collapsed;
            _config.Layout = 0;
            SaveConfig();
            ClassicPane.Visibility = Visibility.Collapsed;
            UpdateClassicToggleText();
            ShowError(new Exception("Couldn't start the Classic Halo CE menu.\n\n" + ex.Message));
        }
    }

    private static bool IsWebLayout(int layout) => layout == 1 || layout == 3 || layout == 4;

    private string _webPage = "";
    private string WebPageUrl() => _config.Layout switch
    {
        3 => "https://halo.classic/bigpicture/index.html?hybrid=0",
        4 => "https://halo.classic/bigpicture/index.html?hybrid=1",
        _ => "https://halo.classic/index.html?start=" + (_config.StartScreen ? "1" : "0")
    };

    private void SetLayout(int layout, bool save = true)
    {
        _config.Layout = layout;
        if (save) SaveConfig();
        try { ApplyHybrid(layout == 2); }
        catch (Exception ex)
        {
            _config.Layout = layout = 0;
            SaveConfig();
            ApplyHybrid(false);
            ShowError(ex);
        }
        Task shown = Task.CompletedTask;
        if (IsWebLayout(layout)) shown = ShowClassicAsync();
        else if (_classicReady)
        {
            ClassicSend(new { type = "stop" });
            ClassicPane.Visibility = Visibility.Collapsed;
        }
        UpdateClassicToggleText();
        if (save && (layout == 1 || layout == 2 || layout == 4) && !File.Exists(BgVideoPath)) _ = ShowBgHintAsync(shown);
    }

    private System.Windows.Media.MediaPlayer? _music;
    private bool _hybridOn;

    private void ApplyHybrid(bool on)
    {
        _hybridOn = on;
        if (!on)
        {
            HybridVideo.Stop();
            HybridVideo.Source = null;
            HybridVideo.Visibility = Visibility.Collapsed;
            HybridDim.Visibility = Visibility.Collapsed;
            _music?.Stop();
            return;
        }
        EnsureClassicFiles();
        string ui = Path.Combine(ClassicFolder, "assets", "ui");
        string vid = Path.Combine(ui, "bg-1080.mp4"), mus = Path.Combine(ui, "bg.mp3");
        if (!File.Exists(vid))
        {
            _hybridOn = false;
            HybridVideo.Visibility = Visibility.Collapsed;
            HybridDim.Visibility = Visibility.Collapsed;
            return;
        }

        bool soundInVideo = VideoHasAudio();
        HybridVideo.IsMuted = !soundInVideo;
        HybridVideo.Volume = soundInVideo ? HybridMusicVolume() : 0;
        HybridVideo.Source = new Uri(vid);
        HybridVideo.Visibility = Visibility.Visible;
        HybridDim.Visibility = Visibility.Visible;
        HybridVideo.Play();

        if (_music == null)
        {
            var m = new System.Windows.Media.MediaPlayer { Volume = HybridMusicVolume() };
            m.MediaEnded += (_, _) => { m.Position = TimeSpan.Zero; m.Play(); };
            _music = m;
        }
        if (soundInVideo) _music.Stop();
        else if (File.Exists(mus))
        {
            _music.Open(new Uri(mus));
            _music.Play();
        }
    }

    private async Task ShowBgHintAsync(Task wait)
    {
        try { await wait; } catch { }
        await ShowDialogAsync("You need to bring your own background",
            "This layout does not come with a background video or music. You need to bring your own: go to Settings > Launcher > Background video (and Background music) and pick an .mp4 and an .mp3 that you made or have the right to use. Until then you get a plain background.",
            "OK", null);
    }

    private static string BgFolder => Path.Combine(ClassicFolder, "assets", "ui");
    private static string BgVideoPath => Path.Combine(BgFolder, "bg-1080.mp4");
    private static string BgMusicPath => Path.Combine(BgFolder, "bg.mp3");

    private void ChooseBackground(bool video)
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Title = video ? "Pick your background video" : "Pick your background music",
            Filter = video ? "MP4 video (*.mp4)|*.mp4" : "MP3 audio (*.mp3)|*.mp3",
            CheckFileExists = true
        };
        if (dlg.ShowDialog(this) != true) return;
        try
        {
            Directory.CreateDirectory(BgFolder);
            try { _music?.Close(); } catch { }
            File.Copy(dlg.FileName, video ? BgVideoPath : BgMusicPath, true);
            ReapplyBackground();
        }
        catch (Exception ex) { ShowError(ex); }
    }

    private void ClearBackground(bool video)
    {
        try
        {
            try { _music?.Close(); } catch { }
            if (video) { HybridVideo.Stop(); HybridVideo.Source = null; }
            string f = video ? BgVideoPath : BgMusicPath;
            if (File.Exists(f)) File.Delete(f);
            ReapplyBackground();
        }
        catch (Exception ex) { ShowError(ex); }
    }

    private void ReapplyBackground()
    {
        try { ApplyHybrid(_config.Layout == 2); } catch (Exception ex) { ShowError(ex); }
        PushClassicState();
        RefreshSettings();
        if (IsWebLayout(_config.Layout)) _ = ShowClassicAsync();
    }

    private static string BgState(string path) => File.Exists(path) ? "Yours: your own file" : "Not set";

    private static string BgMusicState() =>
        File.Exists(BgMusicPath) ? "Yours: your own file" : VideoHasAudio() ? "Not needed: your video has its own sound" : "Not set";

    private static string? _audioKey;
    private static bool _audioVal;

    private static bool VideoHasAudio()
    {
        try
        {
            var fi = new FileInfo(BgVideoPath);
            if (!fi.Exists) return false;
            string key = fi.Length + "|" + fi.LastWriteTimeUtc.Ticks;
            if (key == _audioKey) return _audioVal;
            bool found = false;
            byte[] hdlr = { (byte)'h', (byte)'d', (byte)'l', (byte)'r' };
            byte[] soun = { (byte)'s', (byte)'o', (byte)'u', (byte)'n' };
            using var fs = new FileStream(fi.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var buf = new byte[1 << 20];
            int keep = 0;
            int n;
            while (!found && (n = fs.Read(buf, keep, buf.Length - keep)) > 0)
            {
                int total = keep + n;
                for (int i = 0; i + 16 <= total; i++)
                {
                    if (buf[i] == hdlr[0] && buf[i + 1] == hdlr[1] && buf[i + 2] == hdlr[2] && buf[i + 3] == hdlr[3]
                        && buf[i + 12] == soun[0] && buf[i + 13] == soun[1] && buf[i + 14] == soun[2] && buf[i + 15] == soun[3])
                    { found = true; break; }
                }
                keep = Math.Min(15, total);
                Array.Copy(buf, total - keep, buf, 0, keep);
            }
            _audioKey = key;
            _audioVal = found;
            return found;
        }
        catch { return false; }
    }

    private double HybridMusicVolume() => 0.8 * AHFMCChat.SoundEngine.Curve(_config.MasterVolume) * AHFMCChat.SoundEngine.Curve(_config.MusicVolume);

    private void PauseHybrid() { if (!_hybridOn) return; try { HybridVideo.Pause(); _music?.Pause(); } catch { } }
    private void ResumeHybrid() { if (!_hybridOn) return; try { HybridVideo.Play(); _music?.Play(); } catch { } }
    private void HybridVideo_Ended(object sender, RoutedEventArgs e) { HybridVideo.Position = TimeSpan.Zero; HybridVideo.Play(); }

    private void ApplyHideMissing()
    {
        bool hide = _config.HideMissing;
        var panels = new List<System.Windows.Controls.WrapPanel>();
        foreach (var b in FindButtons(this))
            if (b.Tag is string t && t.Contains('|') && b.Parent is System.Windows.Controls.WrapPanel wp && !panels.Contains(wp)) panels.Add(wp);

        foreach (var wp in panels)
        {
            bool none = wp.Children.OfType<System.Windows.Controls.Button>().All(b => Install.GetState(b) == "Not installed");
            var vis = hide && none ? Visibility.Collapsed : Visibility.Visible;
            wp.Visibility = vis;
            if (wp.Parent is System.Windows.Controls.StackPanel sp)
            {
                int i = sp.Children.IndexOf(wp);
                if (i > 0 && sp.Children[i - 1] is System.Windows.Controls.TextBlock) sp.Children[i - 1].Visibility = vis;
            }
        }
        foreach (var sp in panels.Select(p => p.Parent).OfType<System.Windows.Controls.StackPanel>().Distinct())
            if (sp.Parent is System.Windows.Controls.Border box)
                box.Visibility = sp.Children.OfType<System.Windows.Controls.WrapPanel>().All(w => w.Visibility == Visibility.Collapsed)
                    ? Visibility.Collapsed : Visibility.Visible;

        DependencyObject? d = MccSteamBtn;
        while (d != null && d is not System.Windows.Controls.Border) d = System.Windows.Media.VisualTreeHelper.GetParent(d);
        if (d is System.Windows.Controls.Border mcc)
            mcc.Visibility = hide && !HasLocated("mcc") && new[] { MccSteamBtn, MccNoEacBtn, MccXboxBtn }.All(b => Install.GetState(b) == "Not installed")
                ? Visibility.Collapsed : Visibility.Visible;
    }

    private void ClassicSend(object msg)
    {
        try { if (_classicReady) ClassicView.CoreWebView2.PostWebMessageAsJson(JsonSerializer.Serialize(msg)); }
        catch { }
    }

    private void ClassicPad(string code) => ClassicSend(new { type = "pad", code });

    private void ReportProgress(double p)
    {
        Progress.Value = p;
        ClassicSend(new { type = "progress", value = p });
    }

    private string SteamState(string id) => _steamInstalled.Contains(id) ? "Installed" : "Not installed";
    private string StoreState(string game) => _storeApps == null ? "" : (FindStoreApp(game) != null ? "Installed" : "Not installed");

    private string OptState(string id, GameOpt o)
    {
        if (id == "mcc") return o.Uri.StartsWith("steam://", StringComparison.OrdinalIgnoreCase) ? SteamState("976730") : StoreState("masterchiefcollection");
        var m = Regex.Match(o.Uri, @"^steam://run/(\d+)");
        return m.Success ? SteamState(m.Groups[1].Value) : StoreState(o.Game);
    }

    private void PushClassicState()
    {
        if (!_classicReady) return;
        var official = OfficialGames.ToDictionary(kv => kv.Key, kv => kv.Value.Select(o => OptState(kv.Key, o)).ToArray());
        ClassicSend(new { type = "state", bgVideo = BgState(BgVideoPath), bgMusic = BgMusicState(), bgAudio = VideoHasAudio(), played = OfficialPlayedAll(), layout = _config.Layout, video = _hybridOn, reach = ReachSub.Text, halo3 = Halo3Sub.Text, official, located = LocatedNames(), hide = _config.HideMissing, start = _config.StartScreen, cursor = _config.CursorMode, window = _config.WindowMode, countdown = _config.Countdown, closeTray = _config.CloseToTray, vol = SoundState(), maxGame = _config.MaximizeGame, newsStart = _config.ShowNewsOnStart, newsH3 = _config.NewsHalo3, newsReach = _config.NewsReach, newsOce = _config.NewsOce, newsGrave = _config.NewsGravemind, newsLauncher = _config.NewsLauncher, updAuto = _config.AutoUpdateLauncher, updText = LauncherVersionText(), discord = _config.DiscordPresence, loc = new { halo3 = GameLocation(_config.Halo3Exe, _config.Halo3Manual), reach = GameLocation(_config.ReachExe, _config.ReachManual), gravemind = GameLocation(_config.GravemindExe, _config.GravemindManual), oce = GameLocation(_config.OceExe, _config.OceManual), groundside = GameLocation(_config.GroundsideExe, _config.GroundsideManual) }, chat = ChatState(), logos = AllLogos(), customLogos = _config.GameLogos.Keys.Where(k => GameLogoPath(k) != null).ToArray(), menuSounds = MenuSoundUrls(), settings = SettingsModel(), blur = _config.BigPictureBlur,
            apps = _config.CustomApps.Select(x => new { id = x.Id, name = x.Name, ok = File.Exists(x.Path), file = Path.GetFileName(x.Path), hasImage = AppImagePath(x) != null }).ToArray(),
            gravemind = GravemindSub.Text, oce = OceSub.Text, groundside = GroundsideSub.Text, raven = RavenSub.Text,
            installed = new
            {
                reach = _config.ReachExe != null && File.Exists(_config.ReachExe),
                halo3 = _config.Halo3Exe != null && File.Exists(_config.Halo3Exe),
                oce = _config.OceExe != null && File.Exists(_config.OceExe),
                gravemind = HasGravemind,
                raven = HasRaven,
                groundside = HasGroundside
            },
            removable = new { reach = IsRemovable("reach"), halo3 = IsRemovable("halo3"), oce = IsRemovable("oce"), gravemind = IsRemovable("gravemind"), raven = IsRemovable("raven"), groundside = IsRemovable("groundside") } });
    }


    private void OpenOfficial(string id, int option)
    {
        if (option == 2 && DownloadedGames.ContainsKey(id)) { _ = UninstallGameAsync(id); return; }
        if (id == "gravemind") { if (option == 0) _ = PlayGravemindAsync(); else LocateGravemind(); return; }
        if (id == "raven") { if (option == 0) _ = PlayRavenAsync(); else LocateRaven(); return; }
        if (id == "groundside")
        {
            if (option == 1) LocateGroundside();
            else _ = PlayGroundsideAsync(option == 3);
            return;
        }
        if (id == "oce") { if (option == 0) Community_Click(this, new RoutedEventArgs()); else LocateOce(); return; }
        if (id.StartsWith("app:"))
        {
            string aid = id[4..];
            if (option == 0) _ = LaunchCustomAppAsync(aid);
            else if (option == 1) RenameCustomApp(aid);
            else if (option == 2) LocateCustomApp(aid);
            else if (option == 3) ChangeAppPicture(aid);
            else RemoveCustomApp(aid);
            return;
        }
        if (id == "reach" || id == "halo3")
        {
            bool reach = id == "reach";
            if (option == 0) { if (reach) Reach_Click(this, new RoutedEventArgs()); else Halo3_Click(this, new RoutedEventArgs()); }
            else LocateGame(reach);
            return;
        }
        if (OfficialGames.TryGetValue(id, out var shown) && option >= shown.Length)
        {
            if (option == shown.Length && HasLocated(id)) _ = LaunchLocatedAsync(id);
            else LocateOfficial(id);
            return;
        }
        if (!OfficialGames.TryGetValue(id, out var opts) || option < 0 || option >= opts.Length) return;
        var o = opts[option];
        if (id == "mcc") LaunchMcc(o.Uri);
        else _ = OpenUriAsync(o.Uri + "|" + o.Game);
    }

    private async void Classic_WebMessage(object? sender, CoreWebView2WebMessageReceivedEventArgs a)
    {
        try
        {
            using var doc = JsonDocument.Parse(a.WebMessageAsJson);
            var r = doc.RootElement;
            string type = r.GetProperty("type").GetString() ?? "";
            if ((_busy || _counting) && (type == "open" || type == "fan")) return;
            switch (type)
            {
                case "cancelDl": CancelDownload(); break;
                case "chat": ShowMouse(); ShowChat(); break;
                case "ready": _padAnnounced = false; PushClassicState(); PushNews(); break;
                case "open": OpenOfficial(r.GetProperty("game").GetString() ?? "", r.GetProperty("option").GetInt32()); break;
                case "fan":
                    switch (r.GetProperty("id").GetString())
                    {
                        case "reach": Reach_Click(this, new RoutedEventArgs()); break;
                        case "halo3": Halo3_Click(this, new RoutedEventArgs()); break;
                        case "oce": Community_Click(this, new RoutedEventArgs()); break;
                        case "gravemind": await PlayGravemindAsync(); break;
                        case "raven": await PlayRavenAsync(); break;
                        case "groundside": await PlayGroundsideAsync(false); break;
                        case "addapp": AddCustomApp(); break;
                        case var s when s != null && s.StartsWith("app:"): await LaunchCustomAppAsync(s[4..]); break;
                    }
                    break;
                case "link":
                    if (Uri.TryCreate(r.GetProperty("url").GetString(), UriKind.Absolute, out var u) && u.Scheme == Uri.UriSchemeHttps)
                        await OpenInAppAsync(u.AbsoluteUri, false);
                    break;
                case "setLayout":
                    SetLayout(r.GetProperty("value").GetString() switch { "classic" => 1, "hybrid" => 2, "bigpicture" => 3, "bigpicture-hybrid" => 4, _ => 0 });
                    break;
                case "setStart": _config.StartScreen = r.GetProperty("value").GetBoolean(); SaveConfig(); UpdateClassicToggleText(); break;
                case "setHide": _config.HideMissing = r.GetProperty("value").GetBoolean(); SaveConfig(); ApplyHideMissing(); UpdateClassicToggleText(); PushClassicState(); break;
                case "setNewsStart": _config.ShowNewsOnStart = r.GetProperty("value").GetBoolean(); SaveConfig(); UpdateClassicToggleText(); break;
                case "setNewsSrc":
                    {
                        string nid = r.GetProperty("id").GetString() ?? "";
                        bool on = r.GetProperty("value").GetBoolean();
                        if (nid == "halo3") _config.NewsHalo3 = on; else if (nid == "reach") _config.NewsReach = on; else if (nid == "oce") _config.NewsOce = on; else if (nid == "gravemind") _config.NewsGravemind = on; else if (nid == "launcher") _config.NewsLauncher = on; else if (nid == "raven") _config.NewsRaven = on;
                        SaveConfig(); UpdateClassicToggleText(); PushNews(false);
                        break;
                    }
                case "setUpdAuto": _config.AutoUpdateLauncher = r.GetProperty("value").GetBoolean(); SaveConfig(); UpdateClassicToggleText(); PushClassicState(); break;
                case "newsSeen": MarkNewsSeen(); break;
                case "act": ClassicAct(r.GetProperty("action").GetString() ?? "", r.GetProperty("target").GetString() ?? ""); break;
                case "mouse": ShowMouse(); break;
                case "setting":
                    {
                        int si = r.GetProperty("i").GetInt32();
                        if (si >= 0 && si < _settings.Count) { _settings[si].Change(r.GetProperty("d").GetInt32()); RefreshSettings(); PushClassicState(); }
                        break;
                    }
                case "settingPick":
                    {
                        int si = r.GetProperty("i").GetInt32();
                        var pick = si >= 0 && si < _settings.Count ? _settings[si].Pick : null;
                        if (pick != null) { pick(r.GetProperty("v").GetInt32()); RefreshSettings(); PushClassicState(); }
                        break;
                    }
                case "mail": OpenMail(); break;
                case "setCursor": _config.CursorMode = Math.Clamp(r.GetProperty("value").GetInt32(), 0, 2); SaveConfig(); ApplyCursorMode(); UpdateClassicToggleText(); break;
                case "setWindow": _config.WindowMode = Math.Clamp(r.GetProperty("value").GetInt32(), 0, 1); SaveConfig(); ApplyFullscreen(); UpdateClassicToggleText(); PushClassicState(); break;
                case "setCountdown": _config.Countdown = Math.Clamp(r.GetProperty("value").GetInt32(), 0, 2); SaveConfig(); UpdateClassicToggleText(); break;
                case "setChat": SetChatSetting(r.GetProperty("key").GetString() ?? "", r.GetProperty("value").GetInt32()); break;
                case "setCloseTray": _config.CloseToTray = r.GetProperty("value").GetBoolean(); SaveConfig(); UpdateClassicToggleText(); break;
                case "drag": if (_config.WindowMode == 1 && WindowState == WindowState.Normal) { try { DragMove(); } catch { } } break;
                case "setVolume": SetVolume(r.GetProperty("key").GetString() ?? "", r.GetProperty("value").GetInt32()); break;
                case "setDiscord": _config.DiscordPresence = r.GetProperty("value").GetBoolean(); SaveConfig(); ApplyDiscordSetting(); UpdateClassicToggleText(); break;
                case "setMaxGame": _config.MaximizeGame = r.GetProperty("value").GetBoolean(); SaveConfig(); UpdateClassicToggleText(); break;
                case "minimize": ShowMouse(); Minimize_Click(this, new RoutedEventArgs()); break;
                case "exit": Close(); break;
            }
        }
        catch { }
    }

    private void Credits_Click(object sender, RoutedEventArgs e)
    {
        CreditsPane.Visibility = Visibility.Visible;
        CreditsScroll.ScrollToTop();
        Dispatcher.BeginInvoke(new Action(() => CreditsFirst.Focus()), System.Windows.Threading.DispatcherPriority.Input);
    }

    private async void CreditLink_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button { Tag: string url }) return;
        if (url.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase)) { OpenMail(); return; }
        CreditsPane.Visibility = Visibility.Collapsed;
        await OpenInAppAsync(url, false);
    }

    private void CloseCredits_Click(object sender, RoutedEventArgs e) => CreditsPane.Visibility = Visibility.Collapsed;

    private void CreditsScrim_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (ReferenceEquals(e.OriginalSource, CreditsPane)) CreditsPane.Visibility = Visibility.Collapsed;
    }

    private async void Link_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
    {
        e.Handled = true;
        if (e.Uri.Scheme == "mailto") { OpenMail(); return; }
        CreditsPane.Visibility = Visibility.Collapsed;
        await OpenInAppAsync(e.Uri.AbsoluteUri, false);
    }

    private async void OpenUri_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button { Tag: string tag }) return;
        await OpenUriAsync(tag);
    }

    private async Task OpenUriAsync(string tag)
    {
        if (_counting) return;
        string uri = tag, game = "";
        int bar = tag.LastIndexOf('|');
        if (bar >= 0) { uri = tag[..bar]; game = tag[(bar + 1)..]; }

        var psi = new ProcessStartInfo(uri) { UseShellExecute = true };
        bool startsGame = uri.StartsWith("steam://", StringComparison.OrdinalIgnoreCase);

        if (uri.StartsWith("ms-windows-store://", StringComparison.OrdinalIgnoreCase) && game.Length > 0)
        {
            var app = FindStoreApp(game);
            if (app != null)
            {
                psi = new ProcessStartInfo("explorer.exe", "shell:AppsFolder\\" + app.Value.Aumid) { UseShellExecute = true };
                startsGame = true;
            }
        }

        Func<bool>? watch = null;
        if (startsGame && game.Length > 0)
        {
            string? wdir = null;
            var sm = Regex.Match(uri, @"^steam://run/(\d+)");
            if (sm.Success) { lock (SteamDirs) SteamDirs.TryGetValue(sm.Groups[1].Value, out wdir); }
            else
            {
                var sa = FindStoreApp(game);
                if (sa != null) { string pfn = sa.Value.Aumid.Split('!')[0]; lock (StoreDirs) StoreDirs.TryGetValue(pfn, out wdir); }
            }
            if (wdir != null && Directory.Exists(wdir)) { string d = wdir; watch = () => IsAnyProcessRunningFrom(d); }
        }

        if (startsGame && game.Length > 0) await RunWithCountdownAsync(false, () => Task.CompletedTask);

        try
        {
            Topmost = false;
            WindowState = WindowState.Minimized;
            try { Process.Start(psi)?.Dispose(); }
            catch { ApplyFullscreen(); Activate(); throw; }

            if (startsGame && game.Length > 0)
            {
                if (watch != null) { EnterTray(game); StartWatchdog(watch, 120); }
                else SetPresencePlaying(game);
            }
        }
        catch (Exception ex) { ShowError(ex); }
    }

    private HashSet<string> _steamInstalled = new();
    private List<(string Name, string Aumid)>? _storeApps;
    private DateTime _lastScan = DateTime.MinValue;
    private bool _scanning;

    private async Task RefreshInstalledAsync(bool force = false)
    {
        if (_scanning) return;
        if (!force && DateTime.UtcNow - _lastScan < TimeSpan.FromSeconds(20)) return;
        _scanning = true;
        try
        {
            var (steam, store) = await Task.Run(() => (ScanSteam(), ScanStoreApps()));
            _steamInstalled = steam;
            if (store != null) _storeApps = store;
            _lastScan = DateTime.UtcNow;
            ApplyInstallStates();
        }
        catch { }
        finally { _scanning = false; }
    }

    private static readonly Dictionary<string, string> SteamDirs = new();
    private static readonly Dictionary<string, string> StoreDirs = new();

    private static HashSet<string> ScanSteam()
    {
        var ids = new HashSet<string>();
        string? steam = null;
        try { using var k = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam"); steam = k?.GetValue("SteamPath") as string; }
        catch { }
        if (string.IsNullOrEmpty(steam)) return ids;

        var libs = new List<string> { steam.Replace('/', '\\') };
        try
        {
            string vdf = Path.Combine(steam, "steamapps", "libraryfolders.vdf");
            if (File.Exists(vdf))
                foreach (Match m in Regex.Matches(File.ReadAllText(vdf), "\"path\"\\s+\"([^\"]+)\""))
                    libs.Add(m.Groups[1].Value.Replace("\\\\", "\\"));
        }
        catch { }

        foreach (var lib in libs.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            string dir = Path.Combine(lib, "steamapps");
            if (!Directory.Exists(dir)) continue;
            foreach (var f in Directory.GetFiles(dir, "appmanifest_*.acf"))
            {
                try
                {
                    string text = File.ReadAllText(f);
                    var id = Regex.Match(text, "\"appid\"\\s+\"(\\d+)\"");
                    var flags = Regex.Match(text, "\"StateFlags\"\\s+\"(\\d+)\"");
                    if (id.Success && flags.Success && (long.Parse(flags.Groups[1].Value) & 4) != 0)
                    {
                        ids.Add(id.Groups[1].Value);
                        var inst = Regex.Match(text, "\"installdir\"\\s+\"([^\"]+)\"");
                        if (inst.Success) lock (SteamDirs) SteamDirs[id.Groups[1].Value] = Path.Combine(dir, "common", inst.Groups[1].Value);
                    }
                }
                catch { }
            }
        }
        return ids;
    }

    private static List<(string Name, string Aumid)>? ScanStoreApps()
    {
        try
        {
            var psi = new ProcessStartInfo("powershell.exe")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8
            };
            psi.ArgumentList.Add("-NoProfile");
            psi.ArgumentList.Add("-NonInteractive");
            psi.ArgumentList.Add("-Command");
            psi.ArgumentList.Add("[Console]::OutputEncoding=[System.Text.Encoding]::UTF8; Get-StartApps | ForEach-Object { 'A|' + $_.Name + '|' + $_.AppID }; try { Get-AppxPackage | ForEach-Object { 'P|' + $_.PackageFamilyName + '|' + $_.InstallLocation } } catch { }");

            using var p = Process.Start(psi);
            if (p == null) return null;
            var read = p.StandardOutput.ReadToEndAsync();
            if (!read.Wait(20000) || !p.WaitForExit(5000) || p.ExitCode != 0)
            {
                try { if (!p.HasExited) p.Kill(true); } catch { }
                return null;
            }
            string output = read.Result;

            var list = new List<(string Name, string Aumid)>();
            var pkgs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var raw in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                string line = raw.Trim();
                if (line.StartsWith("P|"))
                {
                    var parts = line.Split('|', 3);
                    if (parts.Length == 3 && parts[2].Length > 0) pkgs[parts[1]] = parts[2];
                    continue;
                }
                if (!line.StartsWith("A|")) continue;
                line = line[2..];
                int i = line.LastIndexOf('|');
                if (i <= 0) continue;
                string name = line[..i], id = line[(i + 1)..];
                if (id.Contains('!') && name.Contains("Halo", StringComparison.OrdinalIgnoreCase))
                    list.Add((name, id));
            }
            lock (StoreDirs)
            {
                StoreDirs.Clear();
                foreach (var app in list)
                {
                    string pfn = app.Aumid.Split('!')[0];
                    if (pkgs.TryGetValue(pfn, out var loc)) StoreDirs[pfn] = loc;
                }
            }
            return list;
        }
        catch { return null; }
    }

    private static string Norm(string s)
    {
        s = Regex.Replace(s, @"\(.*?\)", "");
        return new string(s.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
    }

    private (string Name, string Aumid)? FindStoreApp(string game)
    {
        if (_storeApps == null) return null;
        string key = Norm(game);
        foreach (var a in _storeApps)
            if (Norm(a.Name).Contains(key)) return a;
        return null;
    }

    private static IEnumerable<System.Windows.Controls.Button> FindButtons(DependencyObject root)
    {
        int n = System.Windows.Media.VisualTreeHelper.GetChildrenCount(root);
        for (int i = 0; i < n; i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(root, i);
            if (child is System.Windows.Controls.Button b) yield return b;
            foreach (var d in FindButtons(child)) yield return d;
        }
    }

    private void ApplyInstallStates()
    {
        string Steam(string id) => _steamInstalled.Contains(id) ? "Installed" : "Not installed";
        string Store(string game) => _storeApps == null ? "" : (FindStoreApp(game) != null ? "Installed" : "Not installed");

        foreach (var b in FindButtons(this))
        {
            if (b.Tag is not string tag) continue;
            int bar = tag.LastIndexOf('|');
            if (bar < 0) continue;
            string uri = tag[..bar], game = tag[(bar + 1)..];
            var m = Regex.Match(uri, @"^steam://run/(\d+)");
            if (m.Success) Install.SetState(b, Steam(m.Groups[1].Value));
            else if (uri.StartsWith("ms-windows-store://", StringComparison.OrdinalIgnoreCase)) Install.SetState(b, Store(game));
        }

        Install.SetState(MccSteamBtn, Steam("976730"));
        Install.SetState(MccNoEacBtn, Steam("976730"));
        Install.SetState(MccXboxBtn, Store("masterchiefcollection"));
        UpdateGravemindText();
        UpdateRavenText();
        UpdateGroundsideText();
        ApplyHideMissing();
        PushClassicState();
    }

    private bool _mouseHidden;
    private Point _mouseRef;

    private void HideMouse()
    {
        if (_mouseHidden || _config.CursorMode == 2 || BrowserPane.Visibility == Visibility.Visible || ChatPane.Visibility == Visibility.Visible) return;
        _mouseHidden = true;
        _mouseRef = Mouse.GetPosition(this);
        Mouse.OverrideCursor = Cursors.None;
    }

    private void ShowMouse()
    {
        if (!_mouseHidden || _config.CursorMode == 1) return;
        _mouseHidden = false;
        Mouse.OverrideCursor = null;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct XINPUT_GAMEPAD
    {
        public ushort wButtons;
        public byte bLeftTrigger, bRightTrigger;
        public short sThumbLX, sThumbLY, sThumbRX, sThumbRY;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct XINPUT_STATE
    {
        public uint dwPacketNumber;
        public XINPUT_GAMEPAD Gamepad;
    }

    [DllImport("xinput1_4.dll")]
    private static extern int XInputGetState(int userIndex, out XINPUT_STATE state);

    private System.Windows.Threading.DispatcherTimer? _padTimer;
    private int _padPrev;
    private DateTime _padRepeatAt;
    private bool _padAnnounced;

    private void StartGamepad()
    {
        _padTimer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(40) };
        _padTimer.Tick += (_, _) => PollGamepad();
        _padTimer.Start();
    }

    private void PollGamepad()
    {
        if (!IsVisible || WindowState == WindowState.Minimized || !IsActive || ChatPane.Visibility == Visibility.Visible) { _padPrev = 0; return; }

        XINPUT_STATE st = default;
        bool connected = false;
        try
        {
            for (int i = 0; i < 4 && !connected; i++)
                connected = XInputGetState(i, out st) == 0;
        }
        catch (DllNotFoundException) { _padTimer?.Stop(); return; }
        catch (EntryPointNotFoundException) { _padTimer?.Stop(); return; }

        if (!connected) { _padPrev = 0; _padAnnounced = false; return; }
        if (!_padAnnounced) { _padAnnounced = true; ClassicSend(new { type = "xinput" }); }

        int btn = st.Gamepad.wButtons;
        const int dz = 16000;
        if (st.Gamepad.sThumbLY > dz) btn |= 1;
        if (st.Gamepad.sThumbLY < -dz) btn |= 2;
        if (st.Gamepad.sThumbLX < -dz) btn |= 4;
        if (st.Gamepad.sThumbLX > dz) btn |= 8;

        if (_counting) { _padPrev = btn; return; }

        int pressed = btn & ~_padPrev;
        int dirs = btn & 0xF, fire = pressed & 0xF;
        var now = DateTime.UtcNow;
        if (dirs != 0)
        {
            if (fire != 0) _padRepeatAt = now.AddMilliseconds(380);
            else if (now >= _padRepeatAt) { fire = dirs; _padRepeatAt = now.AddMilliseconds(110); }
        }

        if (pressed != 0 || fire != 0) HideMouse();

        if (ClassicPane.Visibility == Visibility.Visible)
        {
            if ((fire & 1) != 0) ClassicPad("ArrowUp");
            if ((fire & 2) != 0) ClassicPad("ArrowDown");
            if ((fire & 4) != 0) ClassicPad("ArrowLeft");
            if ((fire & 8) != 0) ClassicPad("ArrowRight");
            if ((pressed & 0x1000) != 0) ClassicPad("Enter");
            if ((pressed & 0x2000) != 0) ClassicPad("Escape");
            if ((pressed & 0x0100) != 0) ClassicPad("KeyQ");
            if ((pressed & 0x0200) != 0) ClassicPad("KeyE");
            if ((pressed & 0x0010) != 0) ClassicPad("Start");
            _padPrev = btn;
            return;
        }

        if ((fire & 1) != 0) PadMove(FocusNavigationDirection.Up);
        if ((fire & 2) != 0) PadMove(FocusNavigationDirection.Down);
        if ((fire & 4) != 0) PadMove(FocusNavigationDirection.Left);
        if ((fire & 8) != 0) PadMove(FocusNavigationDirection.Right);
        if ((pressed & 0x1000) != 0) PadActivate();
        if ((pressed & 0x2000) != 0) PadBack();
        _padPrev = btn;
    }

    private bool FocusDefault()
    {
        foreach (var b in new[] { ReachBtn, CommunityBtn, Halo3Btn, MccSteamBtn })
            if (b.IsEnabled && b.IsVisible) return b.Focus();
        return false;
    }

    private void MoveWithin(UIElement pane, FocusNavigationDirection dir, System.Windows.Controls.Button fallback)
    {
        if (Keyboard.FocusedElement is System.Windows.Controls.Button b && b.IsVisible && pane.IsAncestorOf(b)) b.MoveFocus(new TraversalRequest(dir));
        else fallback.Focus();
    }

    private void ClickFocusedWithin(UIElement pane, System.Windows.Controls.Button fallback)
    {
        if (Keyboard.FocusedElement is System.Windows.Controls.Button { IsEnabled: true } b && b.IsVisible && pane.IsAncestorOf(b))
            b.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent, b));
        else fallback.Focus();
    }

    private void PadMove(FocusNavigationDirection dir)
    {
        if (_counting || ClassicPane.Visibility == Visibility.Visible) return;
        if (DialogPane.Visibility == Visibility.Visible) { MoveWithin(DialogPane, dir, DialogYes); return; }
        if (SetupOpen) { SetupPadMove(dir); return; }
        if (NewsPane.Visibility == Visibility.Visible) { PaneMove(NewsPane, NewsScroll, dir, _newsFirstBtn ?? NewsClose); return; }
        if (CreditsPane.Visibility == Visibility.Visible) { PaneMove(CreditsPane, CreditsScroll, dir, CreditsFirst); return; }
        if (BrowserPane.Visibility == Visibility.Visible)
        {
            int dy = dir == FocusNavigationDirection.Up ? -180 : dir == FocusNavigationDirection.Down ? 180 : 0;
            if (dy != 0 && _browserReady) _ = Browser.CoreWebView2.ExecuteScriptAsync($"window.scrollBy(0,{dy})");
            return;
        }
        if (SettingsPane.Visibility == Visibility.Visible) { SettingsPadMove(dir); return; }
        if (!(Keyboard.FocusedElement is System.Windows.Controls.Button fb && fb.IsVisible)) { FocusDefault(); return; }
        MoveSpatial(dir);
    }

    private void PadActivate()
    {
        if (_counting) return;
        if (DialogPane.Visibility == Visibility.Visible) { ClickFocusedWithin(DialogPane, DialogYes); return; }
        if (SetupOpen) { ClickFocusedWithin(SetupPane, SetupStart); return; }
        if (NewsPane.Visibility == Visibility.Visible) { ClickFocusedWithin(NewsPane, _newsFirstBtn ?? NewsClose); return; }
        if (CreditsPane.Visibility == Visibility.Visible) { ClickFocusedWithin(CreditsPane, CreditsFirst); return; }
        if (SettingsPane.Visibility == Visibility.Visible) { ClickFocusedWithin(SettingsPane, _firstSettingBtn ?? SettingsClose); return; }
        if (BrowserPane.Visibility == Visibility.Visible) return;
        if (Keyboard.FocusedElement is not System.Windows.Controls.Button { IsEnabled: true } btn) { FocusDefault(); return; }
        btn.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent, btn));
    }

    private void PadBack()
    {
        if (_counting) return;
        PlayBackIfPaneOpen();
        if (_dlCts != null && DialogPane.Visibility != Visibility.Visible) { CancelDownload(); return; }
        if (DialogPane.Visibility == Visibility.Visible) AnswerDialog(false);
        else if (SetupOpen) { }
        else if (NewsPane.Visibility == Visibility.Visible) NewsPane.Visibility = Visibility.Collapsed;
        else if (CreditsPane.Visibility == Visibility.Visible) CreditsPane.Visibility = Visibility.Collapsed;
        else if (SettingsPane.Visibility == Visibility.Visible) { if (DropOpen) CloseDrop(); else if (!SettingsStepBack()) CloseSettingsPane(); }
        else if (BrowserPane.Visibility == Visibility.Visible) HideBrowser();
    }

    private const string DiscordAppId = "1563768256626688110";
    private DiscordRPC.DiscordRpcClient? _discord;

    private string? _presenceGame;

    private void InitDiscord()
    {
        if (!_config.DiscordPresence) return;
        try
        {
            _discord = new DiscordRPC.DiscordRpcClient(DiscordAppId);
            _discord.Initialize();
            SetPresenceBrowsing();
        }
        catch { _discord = null; }
    }

    private void ApplyDiscordSetting()
    {
        if (!_config.DiscordPresence)
        {
            try { _discord?.ClearPresence(); _discord?.Dispose(); } catch { }
            _discord = null;
            return;
        }
        string? game = _presenceGame;
        if (_discord == null) InitDiscord();
        if (game != null) SetPresencePlaying(game); else SetPresenceBrowsing();
    }

    private void SetPresenceBrowsing()
    {
        _presenceGame = null;
        SetPresence("In the launcher", "Choosing a game", withTimer: false);
    }

    private void SetPresencePlaying(string gameName)
    {
        _presenceGame = gameName;
        SetPresence($"Playing {gameName}", "AHFMC: A Halo Fan-Made Collection", withTimer: true);
    }

    private void SetPresence(string details, string state, bool withTimer)
    {
        try
        {
            if (!_config.DiscordPresence || _discord == null) return;
            var presence = new DiscordRPC.RichPresence
            {
                Details = details,
                State = state,
                Assets = new DiscordRPC.Assets
                {
                    LargeImageKey = "logo",
                    LargeImageText = "AHFMC: A Halo Fan-Made Collection"
                }
            };
            if (withTimer) presence.Timestamps = DiscordRPC.Timestamps.Now;
            _discord.SetPresence(presence);
        }
        catch { }
    }

    [DllImport("user32.dll")] private static extern bool DestroyIcon(IntPtr hIcon);

    private System.Windows.Forms.NotifyIcon? _tray;
    private System.Drawing.Icon? _trayIcon;
    private bool _gameRunning;

    private void EnterTray(string gameName)
    {
        _gameRunning = true;
        BeginPlay(gameName);
        SetPresencePlaying(gameName);
        if (_tray == null) CreateTray();
        _tray!.Text = $"Playing {gameName}";
        _tray.Visible = true;
        HideToTray();
    }

    private void HideToTray()
    {
        PauseHybrid();
        Topmost = false;
        ShowInTaskbar = false;
        Hide();
    }

    private void RestoreFromTray()
    {
        bool wasParked = _parked;
        _parked = false;
        if (wasParked && !_gameRunning) SetPresenceBrowsing();
        Show();
        ShowInTaskbar = true;
        if (WindowState == WindowState.Minimized) WindowState = WindowState.Normal;
        ApplyFullscreen();
        Topmost = true; Activate();
        Topmost = _config.WindowMode == 0;
        if (ClassicPane.Visibility == Visibility.Visible) ClassicView.Focus();
        ResumeHybrid();
    }

    public void ShowFromSecondLaunch()
    {
        try
        {
            if (!IsVisible || _parked || WindowState == WindowState.Minimized) RestoreFromTray();
            else { Topmost = true; Activate(); Topmost = _config.WindowMode == 0; }
            UpdateChatTray();
        }
        catch { }
    }

    private void LeaveTray()
    {
        EndPlay();
        UpdateSubtitles();
        _gameRunning = false;
        SetPresenceBrowsing();
        UpdateChatTray();
        RestoreFromTray();
    }

    private void CreateTray()
    {
        try
        {
            var res = System.Windows.Application.GetResourceStream(new Uri("pack://application:,,,/Assets/logo.ico"));
            _trayIcon = new System.Drawing.Icon(res.Stream);
        }
        catch { _trayIcon = MakeTrayIcon(); }
        var menu = new System.Windows.Forms.ContextMenuStrip();
        menu.Items.Add("Open chat", null, (_, _) => Dispatcher.Invoke(OpenChatFromTray));
        menu.Items.Add("Show launcher", null, (_, _) => Dispatcher.Invoke(RestoreFromTray));
        menu.Items.Add("Exit launcher", null, (_, _) => Dispatcher.Invoke(() => { _reallyExit = true; Close(); }));

        _tray = new System.Windows.Forms.NotifyIcon { Icon = _trayIcon, ContextMenuStrip = menu };
        _tray.BalloonTipClicked += (_, _) => Dispatcher.Invoke(OpenChatFromTray);
        _tray.MouseClick += (_, e) =>
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
                Dispatcher.Invoke(RestoreFromTray);
        };
    }

    private static System.Drawing.Icon MakeTrayIcon()
    {
        using var bmp = new System.Drawing.Bitmap(32, 32);
        using (var g = System.Drawing.Graphics.FromImage(bmp))
        {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.Clear(System.Drawing.Color.Transparent);
            using var pen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(93, 180, 255), 5);
            g.DrawEllipse(pen, 4, 4, 23, 23);
        }
        IntPtr h = bmp.GetHicon();
        using var tmp = System.Drawing.Icon.FromHandle(h);
        var icon = (System.Drawing.Icon)tmp.Clone();
        DestroyIcon(h);
        return icon;
    }

    private bool _parked, _reallyExit;

    private bool TryPark()
    {
        try
        {
            if (_tray == null) CreateTray();
            if (_tray == null) return false;
            _parked = true;
            _tray.Visible = true;
            if (!_gameRunning)
            {
                _tray.Text = "AHFMC: A Halo Fan-Made Collection (in the background)";
                try { _discord?.ClearPresence(); } catch { }
            }
            HideToTray();
            return true;
        }
        catch { return false; }
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        if (_config.CloseToTray && !_reallyExit && TryPark()) { e.Cancel = true; return; }
        base.OnClosing(e);
    }

    protected override void OnClosed(EventArgs e)
    {
        try { EndPlay(); } catch { }
        try { _gameToast?.Close(); } catch { }
        try { _discord?.ClearPresence(); _discord?.Dispose(); } catch { }
        _padTimer?.Stop();
        ShutdownChat();
        try { _watchCts?.Cancel(); } catch { }
        try { Browser.Dispose(); } catch { }
        try { ClassicView.Dispose(); } catch { }
        try { _music?.Close(); } catch { }
        if (_tray != null) { _tray.Visible = false; _tray.Dispose(); }
        _trayIcon?.Dispose();
        base.OnClosed(e);
    }

    private void Launch(string exePath, string gameName, bool watchFolder = true)
    {
        var psi = new ProcessStartInfo(exePath)
        {
            WorkingDirectory = Path.GetDirectoryName(exePath)!,
            UseShellExecute = true
        };
        Topmost = false;
        WindowState = WindowState.Minimized;

        Process? proc;
        try { proc = Process.Start(psi); }
        catch { ApplyFullscreen(); Activate(); throw; }
        if (proc == null) { ApplyFullscreen(); Activate(); return; }

        EnterTray(gameName);

        if (_config.MaximizeGame) Task.Run(() =>
        {
            for (int i = 0; i < 60; i++)
            {
                try
                {
                    proc.Refresh();
                    if (proc.MainWindowHandle != IntPtr.Zero)
                    {
                        ShowWindow(proc.MainWindowHandle, SW_MAXIMIZE);
                        break;
                    }
                }
                catch { break; }
                Thread.Sleep(500);
            }
        });

        string watchDir = Path.GetDirectoryName(exePath)!;
        string procName = Path.GetFileNameWithoutExtension(exePath);
        StartWatchdog(() =>
        {
            bool alive = false;
            try { alive = !proc.HasExited; } catch { }
            if (alive) return true;
            return watchFolder ? IsAnyProcessRunningFrom(watchDir) : IsNamedRunning(procName);
        }, 60);
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenProcess(uint access, bool inherit, int pid);
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr handle);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool QueryFullProcessImageName(IntPtr handle, int flags,
        System.Text.StringBuilder name, ref int size);

    private static bool IsAnyProcessRunningFrom(string dir)
    {
        string prefix = dir.TrimEnd('\\') + "\\";
        foreach (var p in Process.GetProcesses())
        {
            try
            {
                IntPtr h = OpenProcess(0x1000, false, p.Id);
                if (h == IntPtr.Zero) continue;
                try
                {
                    var sb = new System.Text.StringBuilder(1024);
                    int size = sb.Capacity;
                    if (QueryFullProcessImageName(h, 0, sb, ref size) &&
                        sb.ToString().StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
                finally { CloseHandle(h); }
            }
            catch { }
            finally { p.Dispose(); }
        }
        return false;
    }

    private static bool AnyProcessImage(Func<string, bool> match)
    {
        foreach (var p in Process.GetProcesses())
        {
            try
            {
                IntPtr h = OpenProcess(0x1000, false, p.Id);
                if (h == IntPtr.Zero) continue;
                try
                {
                    var sb = new System.Text.StringBuilder(1024);
                    int size = sb.Capacity;
                    if (QueryFullProcessImageName(h, 0, sb, ref size) && match(sb.ToString())) return true;
                }
                finally { CloseHandle(h); }
            }
            catch { }
            finally { p.Dispose(); }
        }
        return false;
    }

    private static bool IsNamedRunning(string name)
    {
        try
        {
            foreach (var p in Process.GetProcessesByName(name)) { p.Dispose(); return true; }
        }
        catch { }
        return false;
    }

    private CancellationTokenSource? _watchCts;

    private void StartWatchdog(Func<bool> isRunning, int startWaitSeconds = 120)
    {
        _watchCts?.Cancel();
        var cts = _watchCts = new CancellationTokenSource();
        var token = cts.Token;
        Task.Run(async () =>
        {
            bool seen = false;
            int idle = 0, waited = 0;
            while (!token.IsCancellationRequested)
            {
                bool running;
                try { running = isRunning(); } catch { running = false; }
                if (running) { seen = true; idle = 0; }
                else if (seen) { if (++idle >= 3) break; }
                else if (++waited >= startWaitSeconds * 2) break;
                try { await Task.Delay(500, token); } catch { return; }
            }
            if (token.IsCancellationRequested) return;
            try { await Dispatcher.InvokeAsync(LeaveTray); } catch { }
        });
    }

    private void SetBusy(bool busy, string text, bool determinate = false)
    {
        ReachBtn.IsEnabled = Halo3Btn.IsEnabled = CommunityBtn.IsEnabled = GravemindBtn.IsEnabled = RavenBtn.IsEnabled = GroundsideBtn.IsEnabled =
            MccSteamBtn.IsEnabled = MccNoEacBtn.IsEnabled = MccXboxBtn.IsEnabled = !busy;
        _dlCts = busy && determinate ? new CancellationTokenSource() : null;
        CancelDlBtn.Visibility = busy && determinate ? Visibility.Visible : Visibility.Collapsed;
        Status.Text = text;
        if (_counting) CountStatus.Text = text.Trim().Length == 0 ? " " : text;
        Progress.Value = 0;
        Progress.IsIndeterminate = busy && !determinate;
        Progress.Visibility = busy ? Visibility.Visible : Visibility.Hidden;
        _busy = busy;
        ClassicSend(new { type = "busy", busy, text, determinate });
    }

    private void CancelDl_Click(object sender, RoutedEventArgs e) => CancelDownload();

    private void CancelDownload()
    {
        var c = _dlCts;
        if (c == null || c.IsCancellationRequested) return;
        c.Cancel();
        Status.Text = "Cancelling…";
    }

    private void ShowError(Exception ex) { if (ex is OperationCanceledException) return; _ = ShowDialogAsync("Something went wrong", ex.Message, "OK", null); }


}

public static class Install
{
    public static readonly DependencyProperty StateProperty =
        DependencyProperty.RegisterAttached("State", typeof(string), typeof(Install), new PropertyMetadata(""));

    public static string GetState(DependencyObject o) => (string)o.GetValue(StateProperty);
    public static void SetState(DependencyObject o, string value) => o.SetValue(StateProperty, value);
}

public class PlayStat
{
    public long Seconds { get; set; }
    public DateTime Last { get; set; }
}

public class CustomApp
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Path { get; set; } = "";
    public string Image { get; set; } = "";
}
