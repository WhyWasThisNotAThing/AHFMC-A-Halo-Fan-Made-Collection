using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Threading;

namespace AHFMC;

public partial class MainWindow
{
    private const string LauncherRepo = "WhyWasThisNotAThing/AHFMC-A-Halo-Fan-Made-Collection";
    private const string SetupAssetPrefix = "AHFMC-Setup";
    private const string PortableAssetPrefix = "AHFMC-Portable";

    private static string AppExePath => Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName ?? "";
    private static string AppDir => Path.GetDirectoryName(AppExePath) ?? "";

    private static bool IsInstalledVersion => File.Exists(Path.Combine(AppDir, "unins000.exe"));

    private static Version CurrentLauncherVersion
    {
        get
        {
            var v = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(1, 0, 0);
            return new Version(v.Major, v.Minor, Math.Max(0, v.Build));
        }
    }

    private static bool TryParseVersion(string tag, out Version version)
    {
        version = new Version(0, 0, 0);
        var m = Regex.Match(tag ?? "", @"(\d+)\.(\d+)(?:\.(\d+))?");
        if (!m.Success) return false;
        version = new Version(int.Parse(m.Groups[1].Value), int.Parse(m.Groups[2].Value), m.Groups[3].Success ? int.Parse(m.Groups[3].Value) : 0);
        return true;
    }

    private string LauncherVersionText()
    {
        var v = CurrentLauncherVersion;
        return $"v{v.Major}.{v.Minor}.{v.Build}  ({(IsInstalledVersion ? "installed version" : "portable version")})";
    }

    private sealed record LauncherUpdate(string Tag, Version Version, string AssetName, string DownloadUrl, string Notes);

    private async Task<LauncherUpdate?> FindLauncherUpdateAsync()
    {
        string json = await Http.GetStringAsync($"https://api.github.com/repos/{LauncherRepo}/releases?per_page=10");
        using var doc = JsonDocument.Parse(json);
        string wanted = IsInstalledVersion ? SetupAssetPrefix : PortableAssetPrefix;
        LauncherUpdate? best = null;

        foreach (var rel in doc.RootElement.EnumerateArray())
        {
            if (rel.GetProperty("draft").GetBoolean()) continue;
            string tag = rel.GetProperty("tag_name").GetString() ?? "";
            if (!TryParseVersion(tag, out var ver)) continue;
            if (best != null && ver <= best.Version) continue;

            foreach (var asset in rel.GetProperty("assets").EnumerateArray())
            {
                string name = asset.GetProperty("name").GetString() ?? "";
                if (!name.StartsWith(wanted, StringComparison.OrdinalIgnoreCase) || !name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) continue;
                string body = rel.TryGetProperty("body", out var b) && b.ValueKind == JsonValueKind.String ? b.GetString() ?? "" : "";
                best = new LauncherUpdate(tag, ver, name, asset.GetProperty("browser_download_url").GetString()!, body);
                break;
            }
        }
        return best != null && best.Version > CurrentLauncherVersion ? best : null;
    }

    private bool _updateBusy;

    private async Task CheckLauncherUpdateAsync(bool manual)
    {
        if (_updateBusy) return;
        _updateBusy = true;
        try
        {
            if (_gameRunning) { if (manual) await ShowDialogAsync("Launcher update", "A game is running. Close it first, then check again.", "OK", null); return; }

            LauncherUpdate? up;
            try
            {
                if (manual) SetBusy(true, "Checking for launcher updates…");
                up = await FindLauncherUpdateAsync();
            }
            catch (Exception ex)
            {
                if (manual) { SetBusy(false, " "); await ShowDialogAsync("Launcher update", "Couldn't check for updates. Check your internet connection and try again.\n\n" + ex.Message, "OK", null); }
                return;
            }
            finally { if (manual) SetBusy(false, " "); }

            if (up == null)
            {
                if (manual) await ShowDialogAsync("Launcher update", $"You have the newest version ({LauncherVersionText()}).", "OK", null);
                return;
            }

            string notes = CleanNotes(up.Notes, 500);
            string kind = IsInstalledVersion ? "The installer is downloaded and run for you; the launcher closes and starts again." : "The new .exe replaces this one; the launcher closes and starts again.";
            bool yes = await ShowDialogAsync("Launcher update",
                $"AHFMC: A Halo Fan-Made Collection {up.Tag} is available (you have v{CurrentLauncherVersion}).\n\n{notes}\n\n{kind}", "Update now", "Not now");
            if (yes) await ApplyLauncherUpdateAsync(up);
        }
        finally { _updateBusy = false; }
    }

    private async Task AutoCheckLauncherUpdateAsync()
    {
        if (!_config.AutoUpdateLauncher) return;
        try
        {
            await Task.Delay(4000);
            for (int i = 0; i < 40 && (SetupOpen || DialogPane.Visibility == Visibility.Visible || _busy || _counting); i++)
                await Task.Delay(1500);
            if (SetupOpen || DialogPane.Visibility == Visibility.Visible || _busy || _counting) return;
            await CheckLauncherUpdateAsync(manual: false);
        }
        catch { }
    }

    private async Task ApplyLauncherUpdateAsync(LauncherUpdate up)
    {
        string dir = Path.Combine(Root, "Updates");
        string file = Path.Combine(dir, up.AssetName);
        string tmp = file + ".download";
        try
        {
            Directory.CreateDirectory(dir);
            SetBusy(true, $"Downloading launcher {up.Tag}…", determinate: true);
            await DownloadAsync(up.DownloadUrl, tmp, new Progress<double>(ReportProgress));
            File.Move(tmp, file, true);
        }
        catch (Exception ex)
        {
            SetBusy(false, " ");
            try { File.Delete(tmp); } catch { }
            ShowError(ex);
            return;
        }

        try
        {
            SetBusy(true, "Installing the update…");
            if (IsInstalledVersion) StartInstaller(file);
            else StartPortableSwap(file);
        }
        catch (Exception ex)
        {
            SetBusy(false, " ");
            ShowError(ex);
            return;
        }

        _reallyExit = true;
        Close();
    }

    private static void StartInstaller(string setupExe)
    {
        Process.Start(new ProcessStartInfo(setupExe, "/SILENT /SUPPRESSMSGBOXES /NORESTART /CLOSEAPPLICATIONS /RELAUNCH=1") { UseShellExecute = true });
    }

    private static void StartPortableSwap(string newExe)
    {
        string target = AppExePath;
        string cmd = Path.Combine(Path.GetTempPath(), "AHFMC-update.cmd");
        var lines = new[]
        {
            "@echo off",
            "chcp 65001 >nul",
            $"set \"PID={Environment.ProcessId}\"",
            $"set \"NEW={newExe}\"",
            $"set \"TARGET={target}\"",
            "set TRIES=0",
            ":wait",
            "tasklist /FI \"PID eq %PID%\" 2>nul | find \"%PID%\" >nul",
            "if not errorlevel 1 (",
            "  ping -n 2 127.0.0.1 >nul",
            "  goto wait",
            ")",
            ":copy",
            "copy /y \"%NEW%\" \"%TARGET%\" >nul 2>&1",
            "if errorlevel 1 (",
            "  set /a TRIES+=1",
            "  if %TRIES% GEQ 30 goto launch",
            "  ping -n 2 127.0.0.1 >nul",
            "  goto copy",
            ")",
            "del \"%NEW%\" >nul 2>&1",
            ":launch",
            "start \"\" \"%TARGET%\"",
            "(goto) 2>nul & del \"%~f0\""
        };
        File.WriteAllText(cmd, string.Join("\r\n", lines) + "\r\n", new System.Text.UTF8Encoding(false));
        Process.Start(new ProcessStartInfo("cmd.exe", $"/c \"{cmd}\"") { CreateNoWindow = true, UseShellExecute = false, WindowStyle = ProcessWindowStyle.Hidden });
    }
}
