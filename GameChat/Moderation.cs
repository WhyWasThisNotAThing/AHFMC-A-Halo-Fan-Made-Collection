using Microsoft.Win32;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace AHFMCChat;

static class DeviceId
{
    static string? _cached;

    public static string Get()
    {
        if (_cached != null) return _cached;

        string raw = "";
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Cryptography");
            raw = key?.GetValue("MachineGuid") as string ?? "";
        }
        catch { }
        if (raw.Length == 0) raw = LocalFallback();

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes("securechat-device-v1|" + raw));
        _cached = Convert.ToHexString(hash).ToLowerInvariant();
        return _cached;
    }

    static string LocalFallback()
    {
        try
        {
            string path = Path.Combine(AppDir.Path_, "device.id");
            if (File.Exists(path)) return File.ReadAllText(path).Trim();
            string id = Guid.NewGuid().ToString("N");
            File.WriteAllText(path, id);
            return id;
        }
        catch { return "unknown-device"; }
    }
}
