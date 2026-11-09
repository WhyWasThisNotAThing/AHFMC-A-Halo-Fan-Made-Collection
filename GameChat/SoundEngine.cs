using System.IO;
using System.Media;
using System.Reflection;
using System.Text;
using Microsoft.Win32;

namespace AHFMCChat;

static class SoundEngine
{
    public static int Master = 100;

    static readonly Dictionary<string, SoundPlayer> _players = new();
    static readonly Dictionary<string, byte[]?> _cache = new();

    public static double Curve(int percent) => Math.Pow(Math.Clamp(percent, 0, 100) / 100.0, 2);

    public static byte[]? Resource(string name)
    {
        lock (_cache)
        {
            if (_cache.TryGetValue(name, out var hit)) return hit;
            byte[]? bytes = null;
            try
            {
                using var src = Assembly.GetExecutingAssembly().GetManifestResourceStream(name);
                if (src != null) { using var ms = new MemoryStream(); src.CopyTo(ms); bytes = ms.ToArray(); }
            }
            catch { }
            _cache[name] = bytes;
            return bytes;
        }
    }

    static byte[]? WindowsDing()
    {
        lock (_cache)
        {
            if (_cache.TryGetValue("windows-ding", out var hit)) return hit;
            byte[]? bytes = null;
            try
            {
                using var k = Registry.CurrentUser.OpenSubKey(@"AppEvents\Schemes\Apps\.Default\SystemAsterisk\.Current");
                string? path = k?.GetValue(null) as string;
                if (!string.IsNullOrWhiteSpace(path))
                {
                    path = Environment.ExpandEnvironmentVariables(path);
                    if (!File.Exists(path))
                        path = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Media", System.IO.Path.GetFileName(path));
                    if (File.Exists(path)) bytes = File.ReadAllBytes(path);
                }
            }
            catch { }
            _cache["windows-ding"] = bytes;
            return bytes;
        }
    }

    public static void Play(string channel, byte[] wav, double gain)
    {
        try
        {
            if (gain <= 0.0001) return;
            var data = Scale(wav, gain);
            lock (_players)
            {
                if (_players.TryGetValue(channel, out var old)) { try { old.Dispose(); } catch { } }
                var p = new SoundPlayer(new MemoryStream(data));
                _players[channel] = p;
                p.Play();
            }
        }
        catch { }
    }

    public static void PlayDing()
    {
        try
        {
            int mode = LocalSettings.NotificationSound;
            int vol = LocalSettings.NotificationVolume;
            if (mode == 0 || vol <= 0 || Master <= 0) return;
            double gain = Curve(Master) * Curve(vol);

            byte[]? wav = mode == 2 ? Resource("Sounds\\beep.wav") : mode == 3 ? Resource("Sounds\\go.wav") : null;
            wav ??= WindowsDing();
            if (wav == null) { if (gain >= 0.999) SystemSounds.Asterisk.Play(); return; }
            Play("ding", wav, gain);
        }
        catch { }
    }

    public static byte[] Scale(byte[] wav, double gain)
    {
        if (gain >= 0.999) return wav;
        try
        {
            if (wav.Length < 44 || Encoding.ASCII.GetString(wav, 0, 4) != "RIFF" || Encoding.ASCII.GetString(wav, 8, 4) != "WAVE") return wav;
            int pos = 12, tag = 0, bits = 0, dataPos = -1, dataLen = 0;
            while (pos + 8 <= wav.Length)
            {
                string id = Encoding.ASCII.GetString(wav, pos, 4);
                int size = BitConverter.ToInt32(wav, pos + 4);
                int body = pos + 8;
                if (id == "fmt " && body + 16 <= wav.Length)
                {
                    tag = BitConverter.ToUInt16(wav, body);
                    bits = BitConverter.ToUInt16(wav, body + 14);
                    if (tag == 0xFFFE && size >= 26 && body + 26 <= wav.Length) tag = BitConverter.ToUInt16(wav, body + 24);
                }
                else if (id == "data")
                {
                    dataPos = body;
                    dataLen = (size < 0 || size > wav.Length - body) ? wav.Length - body : size;
                    break;
                }
                if (size < 0) break;
                pos = body + size + (size & 1);
            }
            if (dataPos < 0) return wav;

            var o = (byte[])wav.Clone();
            int end = dataPos + dataLen;
            if (tag == 1 && bits == 16)
            {
                for (int i = dataPos; i + 1 < end; i += 2)
                {
                    short s = (short)(o[i] | (o[i + 1] << 8));
                    s = (short)Math.Clamp((int)Math.Round(s * gain), short.MinValue, short.MaxValue);
                    o[i] = (byte)(s & 0xFF); o[i + 1] = (byte)((s >> 8) & 0xFF);
                }
            }
            else if (tag == 1 && bits == 8)
            {
                for (int i = dataPos; i < end; i++) o[i] = (byte)Math.Clamp((int)Math.Round((o[i] - 128) * gain) + 128, 0, 255);
            }
            else if (tag == 1 && bits == 24)
            {
                for (int i = dataPos; i + 2 < end; i += 3)
                {
                    int s = o[i] | (o[i + 1] << 8) | (o[i + 2] << 16);
                    if ((s & 0x800000) != 0) s |= unchecked((int)0xFF000000);
                    s = (int)Math.Clamp(Math.Round(s * gain), -8388608, 8388607);
                    o[i] = (byte)(s & 0xFF); o[i + 1] = (byte)((s >> 8) & 0xFF); o[i + 2] = (byte)((s >> 16) & 0xFF);
                }
            }
            else if (tag == 1 && bits == 32)
            {
                for (int i = dataPos; i + 3 < end; i += 4)
                {
                    int s = (int)Math.Clamp(Math.Round(BitConverter.ToInt32(o, i) * gain), int.MinValue, int.MaxValue);
                    BitConverter.GetBytes(s).CopyTo(o, i);
                }
            }
            else if (tag == 3 && bits == 32)
            {
                for (int i = dataPos; i + 3 < end; i += 4)
                    BitConverter.GetBytes((float)(BitConverter.ToSingle(o, i) * gain)).CopyTo(o, i);
            }
            else return wav;
            return o;
        }
        catch { return wav; }
    }
}
