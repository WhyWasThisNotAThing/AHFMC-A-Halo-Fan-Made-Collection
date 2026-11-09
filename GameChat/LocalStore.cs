using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace AHFMCChat;

static class Dpapi
{
    [StructLayout(LayoutKind.Sequential)]
    struct DataBlob { public int cbData; public IntPtr pbData; }

    [DllImport("crypt32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    static extern bool CryptProtectData(ref DataBlob dataIn, string? description, IntPtr entropy, IntPtr reserved, IntPtr prompt, int flags, out DataBlob dataOut);

    [DllImport("crypt32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    static extern bool CryptUnprotectData(ref DataBlob dataIn, IntPtr description, IntPtr entropy, IntPtr reserved, IntPtr prompt, int flags, out DataBlob dataOut);

    [DllImport("kernel32.dll")]
    static extern IntPtr LocalFree(IntPtr mem);

    public static byte[] Protect(byte[] data) => Run(data, true);
    public static byte[] Unprotect(byte[] data) => Run(data, false);

    static byte[] Run(byte[] data, bool protect)
    {
        var input = new DataBlob { cbData = data.Length, pbData = Marshal.AllocHGlobal(data.Length) };
        try
        {
            Marshal.Copy(data, 0, input.pbData, data.Length);
            DataBlob output;
            bool ok = protect
                ? CryptProtectData(ref input, null, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 1, out output)
                : CryptUnprotectData(ref input, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 1, out output);
            if (!ok) throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            try
            {
                var result = new byte[output.cbData];
                Marshal.Copy(output.pbData, result, 0, result.Length);
                return result;
            }
            finally { LocalFree(output.pbData); }
        }
        finally { Marshal.FreeHGlobal(input.pbData); }
    }
}

static class AppDir
{
    public static readonly string Path_ = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AHFMC", "Chat");

    static AppDir()
    {
        try
        {
            string old = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SecureChat");
            if (!Directory.Exists(Path_) && Directory.Exists(old)) CopyDir(old, Path_);
            Directory.CreateDirectory(Path_);
        }
        catch { }
    }

    static void CopyDir(string from, string to)
    {
        Directory.CreateDirectory(to);
        foreach (var f in Directory.GetFiles(from)) File.Copy(f, System.IO.Path.Combine(to, System.IO.Path.GetFileName(f)), true);
        foreach (var d in Directory.GetDirectories(from))
            if (System.IO.Path.GetFileName(d) != "webview") CopyDir(d, System.IO.Path.Combine(to, System.IO.Path.GetFileName(d)));
    }
}

static class SessionStore
{
    public static bool Enabled;
    static string FilePath => System.IO.Path.Combine(AppDir.Path_, "session.bin");

    public static void Save(string token)
    {
        if (!Enabled || token.Length == 0) return;
        try
        {
            Directory.CreateDirectory(AppDir.Path_);
            File.WriteAllBytes(FilePath, Dpapi.Protect(Encoding.UTF8.GetBytes(token)));
        }
        catch { }
    }

    public static string? Load()
    {
        try { return File.Exists(FilePath) ? Encoding.UTF8.GetString(Dpapi.Unprotect(File.ReadAllBytes(FilePath))) : null; }
        catch { return null; }
    }

    public static void Clear()
    {
        Enabled = false;
        try { File.Delete(FilePath); } catch { }
        ClearPassphrase();
    }

    static string PassPath => System.IO.Path.Combine(AppDir.Path_, "unlock.bin");

    public static void SavePassphrase(string pass)
    {
        if (!Enabled || pass.Length == 0) return;
        try
        {
            Directory.CreateDirectory(AppDir.Path_);
            File.WriteAllBytes(PassPath, Dpapi.Protect(Encoding.UTF8.GetBytes(pass)));
        }
        catch { }
    }

    public static string? LoadPassphrase()
    {
        try { return File.Exists(PassPath) ? Encoding.UTF8.GetString(Dpapi.Unprotect(File.ReadAllBytes(PassPath))) : null; }
        catch { return null; }
    }

    public static void ClearPassphrase()
    {
        try { File.Delete(PassPath); } catch { }
    }
}

static class LocalSettings
{
    sealed class Data
    {
        public int AutoLockMinutes { get; set; } = 0;
        public bool WarnUnverified { get; set; } = false;
        public bool AutoSignIn { get; set; } = true;
        public bool Notifications { get; set; } = true;
        public bool ShowPreviews { get; set; } = true;
        public bool LobbyNotifications { get; set; } = false;
        public bool OverlayInGame { get; set; } = true;
        public int NotificationSound { get; set; } = 1;
        public int NotificationVolume { get; set; } = 100;
    }
    static Data _d = Load();
    static string FilePath => System.IO.Path.Combine(AppDir.Path_, "settings.json");

    public static int AutoLockMinutes
    {
        get => _d.AutoLockMinutes;
        set => _d.AutoLockMinutes = value;
    }

    public static bool WarnUnverified
    {
        get => _d.WarnUnverified;
        set => _d.WarnUnverified = value;
    }

    public static bool AutoSignIn
    {
        get => _d.AutoSignIn;
        set => _d.AutoSignIn = value;
    }

    public static bool Notifications
    {
        get => _d.Notifications;
        set => _d.Notifications = value;
    }

    public static bool LobbyNotifications
    {
        get => _d.LobbyNotifications;
        set => _d.LobbyNotifications = value;
    }

    public static bool OverlayInGame
    {
        get => _d.OverlayInGame;
        set => _d.OverlayInGame = value;
    }

    public static int NotificationSound
    {
        get => Math.Clamp(_d.NotificationSound, 0, 3);
        set => _d.NotificationSound = Math.Clamp(value, 0, 3);
    }

    public static int NotificationVolume
    {
        get => Math.Clamp(_d.NotificationVolume, 0, 100);
        set => _d.NotificationVolume = Math.Clamp(value, 0, 100);
    }

    public static bool ShowPreviews
    {
        get => _d.ShowPreviews;
        set => _d.ShowPreviews = value;
    }

    static Data Load()
    {
        try
        {
            string p = System.IO.Path.Combine(AppDir.Path_, "settings.json");
            return File.Exists(p) ? JsonSerializer.Deserialize<Data>(File.ReadAllText(p)) ?? new Data() : new Data();
        }
        catch { return new Data(); }
    }

    public static void Save()
    {
        try { Directory.CreateDirectory(AppDir.Path_); File.WriteAllText(FilePath, JsonSerializer.Serialize(_d)); } catch { }
    }
}
