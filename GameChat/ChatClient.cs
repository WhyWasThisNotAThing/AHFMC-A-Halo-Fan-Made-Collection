using Konscious.Security.Cryptography;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace AHFMCChat;

public sealed record ChatEvent(string Kind, string Channel, string From, string Text, bool Verified, DateTime Time);
public sealed record Profile(string Id, string Username, string Ek, string Sk);
public sealed record PartyInfo(string Id, string Name, string Owner, int Epoch, bool IsOwner);
public sealed record LfgPost(long Id, string UserId, string Username, string Game, string Activity, string Note, int Needed, DateTime ExpiresAt);
public sealed record ReportItem(long Id, string ReportedId, string Reported, string Reporter, string Reason, string Evidence, string Status, DateTime Created);

sealed class Pin { public string Fp { get; set; } = ""; public bool Verified { get; set; } }

sealed class PartyState
{
    public string Id = "", Name = "", OwnerId = "";
    public int Epoch;
    public ConcurrentDictionary<int, byte[]> Keys = new();
}

static class Crypto
{
    const int Block = 128;

    public static byte[] Seal(byte[] key, byte[] plain, string aad)
    {
        int total = (2 + plain.Length + Block - 1) / Block * Block;
        var framed = new byte[total];
        framed[0] = (byte)(plain.Length >> 8);
        framed[1] = (byte)plain.Length;
        plain.CopyTo(framed, 2);
        RandomNumberGenerator.Fill(framed.AsSpan(2 + plain.Length));

        var nonce = RandomNumberGenerator.GetBytes(12);
        var ct = new byte[total];
        var tag = new byte[16];
        using var aes = new AesGcm(key, 16);
        aes.Encrypt(nonce, framed, ct, tag, Encoding.UTF8.GetBytes(aad));
        return [.. nonce, .. ct, .. tag];
    }

    public static byte[]? Open(byte[] key, byte[] blob, string aad)
    {
        try
        {
            if (blob.Length < 12 + 16 + 2) return null;
            var nonce = blob[..12];
            var tag = blob[^16..];
            var ct = blob[12..^16];
            var framed = new byte[ct.Length];
            using var aes = new AesGcm(key, 16);
            aes.Decrypt(nonce, ct, tag, framed, Encoding.UTF8.GetBytes(aad));
            int len = (framed[0] << 8) | framed[1];
            return 2 + len > framed.Length ? null : framed[2..(2 + len)];
        }
        catch { return null; }
    }

    public static string SealStr(byte[] key, string plain, string aad)
        => Convert.ToBase64String(Seal(key, Encoding.UTF8.GetBytes(plain), aad));

    public static string? OpenStr(byte[] key, string b64, string aad)
    {
        try
        {
            var p = Open(key, Convert.FromBase64String(b64), aad);
            return p == null ? null : Encoding.UTF8.GetString(p);
        }
        catch { return null; }
    }

    static byte[] Pbkdf2(string pass, byte[] salt)
        => Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(pass), salt, 600_000, HashAlgorithmName.SHA256, 32);

    static byte[] Argon2(string pass, byte[] salt)
    {
        using var a = new Argon2id(Encoding.UTF8.GetBytes(pass))
        {
            Salt = salt, DegreeOfParallelism = 4, Iterations = 3, MemorySize = 131072
        };
        return a.GetBytes(32);
    }

    public static string BackupSeal(string pass, byte[] plain)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        return "v2:" + Convert.ToBase64String([.. salt, .. Seal(Argon2(pass, salt), plain, "backup-v2")]);
    }

    public static byte[]? BackupOpen(string pass, string stored)
    {
        try
        {
            bool v2 = stored.StartsWith("v2:", StringComparison.Ordinal);
            var all = Convert.FromBase64String(v2 ? stored[3..] : stored);
            return v2 ? Open(Argon2(pass, all[..16]), all[16..], "backup-v2")
                      : Open(Pbkdf2(pass, all[..16]), all[16..], "backup-v1");
        }
        catch { return null; }
    }

    public static string GeneratePassphrase()
    {
        const string cons = "bdfgklmnprstvz", vow = "aeiou";
        var words = new List<string>();
        for (int w = 0; w < 4; w++)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < 3; i++)
            {
                sb.Append(cons[RandomNumberGenerator.GetInt32(cons.Length)]);
                sb.Append(vow[RandomNumberGenerator.GetInt32(vow.Length)]);
            }
            words.Add(sb.ToString());
        }
        return string.Join("-", words);
    }

    public static string SafetyCode(string nameA, string ekA, string skA, string nameB, string ekB, string skB)
    {
        var a = (name: nameA.ToLowerInvariant(), h: SHA256.HashData(Encoding.UTF8.GetBytes(ekA + "|" + skA)));
        var b = (name: nameB.ToLowerInvariant(), h: SHA256.HashData(Encoding.UTF8.GetBytes(ekB + "|" + skB)));
        if (string.CompareOrdinal(a.name, b.name) > 0) (a, b) = (b, a);
        var input = Encoding.UTF8.GetBytes("safety-v1|" + a.name + "|" + b.name + "|").Concat(a.h).Concat(b.h).ToArray();
        var d = SHA256.HashData(input);
        var groups = new List<string>();
        for (int i = 0; i < 5; i++) groups.Add((BitConverter.ToUInt32(d, i * 4) % 100000).ToString("D5"));
        return string.Join(" ", groups);
    }

    public static string Fingerprint(string ek, string sk)
    {
        var h = SHA256.HashData(Encoding.UTF8.GetBytes(ek + "|" + sk));
        string hex = Convert.ToHexString(h, 0, 12);
        return string.Join("-", Enumerable.Range(0, hex.Length / 4).Select(i => hex.Substring(i * 4, 4)));
    }

    public static bool Verify(string skB64, string data, string sigB64)
    {
        try
        {
            using var v = ECDsa.Create();
            v.ImportSubjectPublicKeyInfo(Convert.FromBase64String(skB64), out _);
            return v.VerifyData(Encoding.UTF8.GetBytes(data), Convert.FromBase64String(sigB64), HashAlgorithmName.SHA256);
        }
        catch { return false; }
    }
}

public sealed class ChatClient : IDisposable
{
    const int MaxChars = 500;
    static readonly Regex NameRx = new("^[A-Za-z0-9_]{3,16}$");
    static readonly Regex PartyRx = new("^[A-Za-z0-9_-]{1,24}$");

    readonly string _url, _anon;
    readonly HttpClient _http = new();
    readonly CancellationTokenSource _cts = new();
    readonly SemaphoreSlim _wsLock = new(1, 1);
    readonly object _pinLock = new();
    readonly ConcurrentDictionary<string, Profile> _byId = new();
    readonly ConcurrentDictionary<string, Profile> _byName = new(StringComparer.OrdinalIgnoreCase);
    readonly ConcurrentDictionary<string, PartyState> _parties = new();
    readonly ConcurrentDictionary<string, byte> _seen = new();
    Dictionary<string, Pin> _pins = new(StringComparer.OrdinalIgnoreCase);

    string _access = "", _refresh = "", _dir = "";
    byte[]? _localKey;
    readonly HashSet<string> _blocked = new(StringComparer.OrdinalIgnoreCase);
    DateTime _expires = DateTime.UtcNow;
    ClientWebSocket? _ws;
    ECDiffieHellman? _ecdh;
    ECDsa? _ecdsa;
    Profile _me = new("", "", "", "");

    public event Action<ChatEvent>? OnEvent;
    public event Action? PartiesChanged;
    public event Action<string>? RefreshTokenChanged;
    public string RefreshToken => _refresh;
    public string UserId { get; private set; } = "";
    public string Username => _me.Username;

    public ChatClient(string supabaseUrl, string anonKey)
    {
        _url = supabaseUrl.TrimEnd('/');
        _anon = anonKey;
        _http.DefaultRequestHeaders.Add("apikey", anonKey);
    }

    public async Task<string?> LoginOrSignUpAsync(string email, string password)
    {
        var (ok, body) = await AuthCall("token?grant_type=password", new { email, password });
        if (!ok)
        {
            if (!body.Contains("invalid_credentials")) return ErrMsg(body);
            (ok, body) = await AuthCall("signup", new { email, password });
            if (!ok) return body.Contains("user_already_exists") ? "Wrong password for this email." : ErrMsg(body);
        }
        var j = JsonNode.Parse(body)!;
        if (j["access_token"] is null)
            return "Account created, but email confirmation is on. Turn off 'Confirm email' in Supabase (Authentication > Sign In / Providers > Email).";
        SetSession(j);
        return null;
    }

    public static string AuthEmail(string loginName)
    {
        loginName = loginName.Trim();
        return loginName.Contains('@') ? loginName : $"{loginName.ToLowerInvariant()}@{Config.UsernameEmailDomain}";
    }

    public async Task<string> ReportAsync(string reportedUser, string reason, string evidence)
    {
        var t = await RequireUserAsync(reportedUser);
        static string Cut(string s, int n) => s.Length <= n ? s : s[..n];
        try
        {
            await RestAsync(HttpMethod.Post, "reports", new
            {
                reported_id = t.Id,
                reported_username = t.Username,
                reason = Cut(reason, 500),
                evidence = Cut(evidence, 6000)
            });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"You already have an open report about {t.Username}. It is being looked at.");
        }
        return $"Report about {t.Username} sent. Thank you.";
    }

    static object AuthPayload(string email, string password, string? captchaToken)
        => captchaToken == null
            ? new { email, password }
            : new { email, password, gotrue_meta_security = new { captcha_token = captchaToken } };

    public async Task<string?> LoginAsync(string email, string password, string? captchaToken = null)
    {
        var (ok, body) = await AuthCall("token?grant_type=password", AuthPayload(AuthEmail(email), password, captchaToken));
        if (!ok) return body.Contains("invalid_credentials")
            ? "Wrong username/email or password. (If you signed up with an email, log in with that email.)" : ErrMsg(body);
        SetSession(JsonNode.Parse(body)!);
        return null;
    }

    public async Task<string?> SignUpAsync(string email, string password, string? captchaToken = null)
    {
        var (ok, body) = await AuthCall("signup", AuthPayload(AuthEmail(email), password, captchaToken));
        if (!ok) return body.Contains("user_already_exists") ? "An account with this username or email already exists. Log in instead." : ErrMsg(body);
        var j = JsonNode.Parse(body)!;
        if (j["access_token"] is null)
            return "Account created. Check your email to confirm it, then log in.";
        SetSession(j);
        return null;
    }

    public async Task<bool> ResumeAsync(string refreshToken)
    {
        var (ok, body) = await AuthCall("token?grant_type=refresh_token", new { refresh_token = refreshToken });
        if (!ok) return false;
        SetSession(JsonNode.Parse(body)!);
        return UserId.Length > 0;
    }

    public async Task<bool> CheckPassphraseAsync(string pass)
    {
        var rows = await GetAsync($"key_backups?select=blob&user_id=eq.{UserId}");
        if (rows.Count == 0) return false;
        string blob = S(rows[0]!["blob"]);
        return await Task.Run(() => Crypto.BackupOpen(pass, blob)) != null;
    }

    public static string GeneratePassphrase() => Crypto.GeneratePassphrase();

    public async Task<string> ResolveUserAsync(string name) => (await RequireUserAsync(name)).Username;

    async Task<(bool, string)> AuthCall(string path, object payload)
    {
        using var r = await _http.PostAsJsonAsync($"{_url}/auth/v1/{path}", payload);
        return (r.IsSuccessStatusCode, await r.Content.ReadAsStringAsync());
    }

    void SetSession(JsonNode j)
    {
        _access = S(j["access_token"]);
        if (j["refresh_token"] is JsonNode rt) { _refresh = S(rt); RefreshTokenChanged?.Invoke(_refresh); }
        int exp = int.TryParse(S(j["expires_in"]), out var e) ? e : 3600;
        _expires = DateTime.UtcNow.AddSeconds(exp);
        if (j["user"]?["id"] is JsonNode u) UserId = S(u);
    }

    static string ErrMsg(string body)
    {
        try { var j = JsonNode.Parse(body); return S(j?["msg"]) is { Length: > 0 } m ? m : body; }
        catch { return body; }
    }

    public async Task<bool> HasProfileAsync()
    {
        var rows = await GetAsync($"profiles?select=username,ek,sk&user_id=eq.{UserId}");
        if (rows.Count == 0) return false;
        _me = new Profile(UserId, S(rows[0]!["username"]), S(rows[0]!["ek"]), S(rows[0]!["sk"]));
        return true;
    }

    public async Task<bool> UnlockAsync(string passphrase)
    {
        var rows = await GetAsync($"key_backups?select=blob&user_id=eq.{UserId}");
        if (rows.Count == 0) return false;
        var plain = Crypto.BackupOpen(passphrase, S(rows[0]!["blob"]));
        if (plain == null) return false;

        var d = JsonNode.Parse(plain)!;
        var ecdh = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
        var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        ecdh.ImportPkcs8PrivateKey(Convert.FromBase64String(S(d["ecdh"])), out _);
        ecdsa.ImportPkcs8PrivateKey(Convert.FromBase64String(S(d["ecdsa"])), out _);

        if (B64(ecdh.ExportSubjectPublicKeyInfo()) != _me.Ek || B64(ecdsa.ExportSubjectPublicKeyInfo()) != _me.Sk)
            return false;

        _ecdh = ecdh; _ecdsa = ecdsa;
        await StartAsync();
        return true;
    }

    public async Task<string?> CreateAccountAsync(string username, string passphrase)
    {
        if (!NameRx.IsMatch(username)) return "Username must be 3-16 letters, numbers or underscores.";
        string? weak = PassphraseProblem(passphrase);
        if (weak != null) return weak;

        var existing = await GetAsync($"profiles?select=username&username=ilike.{username}");
        if (existing.Any(r => S(r!["username"]).Equals(username, StringComparison.OrdinalIgnoreCase)))
            return "That username is already taken.";
        if (await UsernameTooCloseAsync(username)) return NameTooCloseMessage;

        var ecdh = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
        var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var secret = JsonSerializer.SerializeToUtf8Bytes(new
        {
            ecdh = B64(ecdh.ExportPkcs8PrivateKey()),
            ecdsa = B64(ecdsa.ExportPkcs8PrivateKey())
        });
        string ek = B64(ecdh.ExportSubjectPublicKeyInfo()), sk = B64(ecdsa.ExportSubjectPublicKeyInfo());

        try { await RestAsync(HttpMethod.Post, "key_backups", new { blob = Crypto.BackupSeal(passphrase, secret) }); }
        catch (Exception ex) { return ex.Message; }

        try { await RestAsync(HttpMethod.Post, "profiles", new { username, ek, sk, device_hash = DeviceId.Get() }); }
        catch (Exception ex)
        {
            try { await RestAsync(HttpMethod.Delete, $"key_backups?user_id=eq.{UserId}"); } catch { }
            return ex.Message;
        }

        _ecdh = ecdh; _ecdsa = ecdsa;
        _me = new Profile(UserId, username, ek, sk);
        await StartAsync();
        return null;
    }

    public static string? PassphraseProblem(string p)
    {
        if (p.Length < 12) return "Passphrase must be at least 12 characters. Tip: use 4 or more random words.";
        if (p.Distinct().Count() < 6) return "Passphrase is too repetitive.";
        string low = p.ToLowerInvariant();
        foreach (var bad in new[] { "password", "passphrase", "qwerty", "123456", "abcdef", "letmein", "iloveyou", "welcome", "secretchat", "securechat" })
            if (low.Contains(bad)) return "Passphrase is too easy to guess. Use several unrelated random words.";
        return null;
    }

    public bool IsContactVerified(string username) => IsVerified(username);

    async Task StartAsync()
    {
        await RegisterDeviceAsync();
        await RefreshModerationAsync();
        await CheckAdminAsync();

        _byId[_me.Id] = _me;
        _byName[_me.Username] = _me;

        string dir = Path.Combine(AppDir.Path_, UserId);
        Directory.CreateDirectory(dir);
        _dir = dir;

        _localKey = HKDF.DeriveKey(HashAlgorithmName.SHA256, _ecdh!.ExportPkcs8PrivateKey(), 32,
                                   salt: null, info: Encoding.UTF8.GetBytes("securechat-local-v1"));
        string sealedPins = Path.Combine(dir, "pins.sealed"), legacyPins = Path.Combine(dir, "pins.json");
        try
        {
            string? pj = LoadSealed("pins.sealed");
            if (pj != null)
                _pins = new(JsonSerializer.Deserialize<Dictionary<string, Pin>>(pj) ?? new(), StringComparer.OrdinalIgnoreCase);
            else if (File.Exists(sealedPins))
            {
                File.Move(sealedPins, sealedPins + ".bad", true);
                Emit("warn", "security", "", "Your saved contact-key file was modified or corrupted. Contacts' keys will be learned again, so re-verify the people you care about.");
            }
            else if (File.Exists(legacyPins))
            {
                _pins = new(JsonSerializer.Deserialize<Dictionary<string, Pin>>(File.ReadAllText(legacyPins)) ?? new(), StringComparer.OrdinalIgnoreCase);
                SavePins();
                File.Delete(legacyPins);
            }
            string? bj = LoadSealed("blocks.sealed");
            if (bj != null)
                foreach (var n in JsonSerializer.Deserialize<List<string>>(bj) ?? new()) _blocked.Add(n);
        }
        catch { }

        foreach (var m in await GetAsync($"party_members?select=party_id&user_id=eq.{UserId}"))
            await LoadPartyAsync(S(m!["party_id"]));

        _ = Task.Run(RealtimeLoopAsync);
        _ = Task.Run(RefreshLoopAsync);
        _ = Task.Run(ModerationLoopAsync);
    }

    public async Task SendPublicAsync(string text)
    {
        RequireCanSend();
        CheckLen(text);
        var rows = JsonNode.Parse(await RestAsync(HttpMethod.Post, "messages", new { body = text }, true))!.AsArray();
        if (_seen.TryAdd("m:" + S(rows[0]!["id"]), 0)) Emit("public", "lobby", Username, text, true);
    }

    public async Task SendDmAsync(string user, string text)
    {
        RequireCanSend();
        if (IsBlocked(user)) throw new InvalidOperationException($"You blocked {user}. Unblock them first.");
        CheckLen(text);
        var p = await RequireUserAsync(user);
        string body = Crypto.SealStr(PairKey(p.Ek), text, $"DM|{UserId}|{p.Id}");
        var rows = JsonNode.Parse(await RestAsync(HttpMethod.Post, "dms", new { recipient_id = p.Id, body }, true))!.AsArray();
        if (_seen.TryAdd("d:" + S(rows[0]!["id"]), 0)) Emit("dm", p.Username, Username, text, true);
    }

    public async Task SendPartyAsync(string sel, string text)
    {
        RequireCanSend();
        CheckLen(text);
        var party = Party(sel);
        int ep = party.Epoch;
        if (!party.Keys.TryGetValue(ep, out var key)) throw new InvalidOperationException("You don't have the current party key yet.");

        string body = Crypto.SealStr(key, text, $"PM|{party.Id}|{ep}|{UserId}");
        string sig = B64(_ecdsa!.SignData(Encoding.UTF8.GetBytes($"PM|{party.Id}|{ep}|{UserId}|{body}"), HashAlgorithmName.SHA256));
        var rows = JsonNode.Parse(await RestAsync(HttpMethod.Post, "party_messages",
            new { party_id = party.Id, epoch = ep, body, sig }, true))!.AsArray();
        if (_seen.TryAdd("p:" + S(rows[0]!["id"]), 0)) Emit("party", party.Name, Username, text, true);
    }

    public IReadOnlyList<PartyInfo> Parties => _parties.Values
        .Select(p => new PartyInfo(p.Id, p.Name, _byId.TryGetValue(p.OwnerId, out var o) ? o.Username : "?", p.Epoch, p.OwnerId == UserId))
        .OrderBy(p => p.Name).ToList();

    public async Task<string> CreatePartyAsync(string name)
    {
        if (!PartyRx.IsMatch(name)) throw new InvalidOperationException("Party names: 1-24 letters, numbers, - or _.");
        var rows = JsonNode.Parse(await RestAsync(HttpMethod.Post, "parties", new { name }, true))!.AsArray();
        string pid = S(rows[0]!["id"]);

        byte[] key = RandomNumberGenerator.GetBytes(32);
        await RestAsync(HttpMethod.Post, "party_members", new { party_id = pid, user_id = UserId });
        await RestAsync(HttpMethod.Post, "party_keys", new { party_id = pid, epoch = 0, user_id = UserId, wrapped = WrapKey(key, _me, pid, 0) });

        var st = new PartyState { Id = pid, Name = name, OwnerId = UserId, Epoch = 0 };
        st.Keys[0] = key;
        _parties[pid] = st;
        PartiesChanged?.Invoke();
        return $"Party '{name}' [{pid[..8]}] created. Use /invite <user>.";
    }

    public async Task<string> InviteAsync(string sel, string user)
    {
        var party = Party(sel); RequireOwner(party);
        var t = await RequireUserAsync(user);

        var already = await GetAsync($"party_members?select=user_id&party_id=eq.{party.Id}&user_id=eq.{t.Id}");
        if (already.Count > 0) throw new InvalidOperationException($"{t.Username} is already in the party.");

        try
        {
            await RestAsync(HttpMethod.Post, "party_keys", new
            {
                party_id = party.Id, epoch = party.Epoch, user_id = t.Id,
                wrapped = WrapKey(party.Keys[party.Epoch], t, party.Id, party.Epoch)
            });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("exists")) { }

        try { await RestAsync(HttpMethod.Post, "party_invites", new { party_id = party.Id, invitee_id = t.Id }); }
        catch (InvalidOperationException ex) when (ex.Message.Contains("exists")) { throw new InvalidOperationException($"{t.Username} already has a pending invite."); }

        return $"Invited {t.Username} to '{party.Name}'. They must /accept to join.";
    }

    public async Task<List<string>> ListInvitesAsync()
    {
        var res = new List<string>();
        foreach (var i in await GetAsync($"party_invites?select=party_id&invitee_id=eq.{UserId}"))
        {
            string pid = S(i!["party_id"]);
            var pr = await GetAsync($"parties?select=name,owner_id&id=eq.{pid}");
            if (pr.Count == 0) continue;
            var owner = await ProfileByIdAsync(S(pr[0]!["owner_id"]));
            res.Add($"{S(pr[0]!["name"])} [{pid[..8]}] from {owner?.Username ?? "?"}  ->  /accept {pid[..8]}");
        }
        return res;
    }

    public async Task<string> AcceptAsync(string sel)
    {
        string? pid = null, pname = null;
        foreach (var i in await GetAsync($"party_invites?select=party_id&invitee_id=eq.{UserId}"))
        {
            string id = S(i!["party_id"]);
            var pr = await GetAsync($"parties?select=name&id=eq.{id}");
            string name = pr.Count > 0 ? S(pr[0]!["name"]) : "";
            if (id.StartsWith(sel, StringComparison.OrdinalIgnoreCase) || name.Equals(sel, StringComparison.OrdinalIgnoreCase))
            { pid = id; pname = name; break; }
        }
        if (pid == null) throw new InvalidOperationException("No such invite.");

        await RestAsync(HttpMethod.Post, "party_members", new { party_id = pid, user_id = UserId });
        await RestAsync(HttpMethod.Delete, $"party_invites?party_id=eq.{pid}&invitee_id=eq.{UserId}");
        await LoadPartyAsync(pid);
        _ = Task.Run(() => CatchUpAsync(pid));
        return $"Joined '{pname}'.";
    }

    public async Task<string> KickAsync(string sel, string user)
    {
        var party = Party(sel); RequireOwner(party);
        var t = await RequireUserAsync(user);

        await RestAsync(HttpMethod.Delete, $"party_members?party_id=eq.{party.Id}&user_id=eq.{t.Id}");
        await RestAsync(HttpMethod.Delete, $"party_invites?party_id=eq.{party.Id}&invitee_id=eq.{t.Id}");
        await RestAsync(HttpMethod.Delete, $"party_keys?party_id=eq.{party.Id}&user_id=eq.{t.Id}");

        int newEpoch = party.Epoch + 1;
        byte[] newKey = RandomNumberGenerator.GetBytes(32);
        var ids = new HashSet<string> { UserId };
        foreach (var m in await GetAsync($"party_members?select=user_id&party_id=eq.{party.Id}")) ids.Add(S(m!["user_id"]));
        foreach (var m in await GetAsync($"party_invites?select=invitee_id&party_id=eq.{party.Id}")) ids.Add(S(m!["invitee_id"]));
        ids.Remove(t.Id);

        var rows = new List<object>();
        foreach (var id in ids)
        {
            var p = id == UserId ? _me : await ProfileByIdAsync(id);
            if (p == null || (p = Trust(p)) == null) continue;
            rows.Add(new { party_id = party.Id, epoch = newEpoch, user_id = id, wrapped = WrapKey(newKey, p, party.Id, newEpoch) });
        }
        await RestAsync(HttpMethod.Post, "party_keys", rows);
        await RestAsync(HttpMethod.Patch, $"parties?id=eq.{party.Id}", new { epoch = newEpoch });

        party.Keys[newEpoch] = newKey;
        party.Epoch = newEpoch;
        PartiesChanged?.Invoke();
        return $"{t.Username} removed. Key rotated (epoch {newEpoch}); they can't read anything new.";
    }

    public async Task<string> LeaveAsync(string sel)
    {
        var party = Party(sel);
        if (party.OwnerId == UserId) throw new InvalidOperationException("You own this party. Use /disband to delete it.");
        await RestAsync(HttpMethod.Delete, $"party_members?party_id=eq.{party.Id}&user_id=eq.{UserId}");
        await RestAsync(HttpMethod.Delete, $"party_keys?party_id=eq.{party.Id}&user_id=eq.{UserId}");
        _parties.TryRemove(party.Id, out _);
        PartiesChanged?.Invoke();
        return $"Left '{party.Name}'.";
    }

    public async Task<string> DisbandAsync(string sel)
    {
        var party = Party(sel); RequireOwner(party);
        await RestAsync(HttpMethod.Delete, $"parties?id=eq.{party.Id}");
        _parties.TryRemove(party.Id, out _);
        PartiesChanged?.Invoke();
        return $"Party '{party.Name}' deleted.";
    }

    public async Task<string> MembersAsync(string sel)
    {
        var party = Party(sel);
        var names = new List<string>();
        foreach (var m in await GetAsync($"party_members?select=user_id&party_id=eq.{party.Id}"))
            names.Add((await ProfileByIdAsync(S(m!["user_id"])))?.Username ?? "?");
        return $"'{party.Name}': {string.Join(", ", names)}";
    }

    public async Task<string> FingerprintAsync(string user)
    {
        var p = await RequireUserAsync(user, allowSelf: true);
        return $"{p.Username}: {Crypto.Fingerprint(p.Ek, p.Sk)} {(IsVerified(p.Username) ? "[verified]" : "[NOT verified - compare in person or by call]")}";
    }

    public async Task<string> VerifyAsync(string user)
    {
        var p = await RequireUserAsync(user);
        lock (_pinLock) { _pins[p.Username].Verified = true; SavePins(); }
        return $"{p.Username} marked as verified.";
    }

    bool IsVerified(string username)
    {
        if (username.Equals(Username, StringComparison.OrdinalIgnoreCase)) return true;
        lock (_pinLock) return _pins.TryGetValue(username, out var p) && p.Verified;
    }

    Profile? Trust(Profile p)
    {
        if (p.Id == UserId) return p;
        string fp = Crypto.Fingerprint(p.Ek, p.Sk);
        bool isNew = false;
        lock (_pinLock)
        {
            if (_pins.TryGetValue(p.Username, out var pin))
            {
                if (pin.Fp != fp)
                {
                    Emit("warn", "security", "", $"KEY CHANGED for '{p.Username}'. Possible impersonation or server tampering. Refusing to use it.");
                    return null;
                }
            }
            else { _pins[p.Username] = new Pin { Fp = fp }; SavePins(); isNew = true; }
        }
        if (isNew) Emit("system", "security", "", $"New contact '{p.Username}', fingerprint {fp}. Compare it with them, then /verify {p.Username}.");
        return p;
    }

    void SavePins() => SaveSealed("pins.sealed", JsonSerializer.Serialize(_pins));

    void SaveSealed(string name, string json)
    {
        try { File.WriteAllText(Path.Combine(_dir, name), Crypto.SealStr(_localKey!, json, "local|" + name)); } catch { }
    }

    string? LoadSealed(string name)
    {
        string path = Path.Combine(_dir, name);
        if (!File.Exists(path)) return null;
        return Crypto.OpenStr(_localKey!, File.ReadAllText(path).Trim(), "local|" + name);
    }

    public bool IsBlocked(string user) { lock (_pinLock) return _blocked.Contains(user); }
    public IReadOnlyList<string> BlockedUsers() { lock (_pinLock) return _blocked.OrderBy(x => x).ToList(); }

    public string Block(string user)
    {
        if (user.Equals(Username, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("You can't block yourself.");
        lock (_pinLock) _blocked.Add(user);
        SaveBlocks();
        return $"{user} blocked. You won't see their messages or invites.";
    }

    public string Unblock(string user)
    {
        bool had;
        lock (_pinLock) had = _blocked.Remove(user);
        SaveBlocks();
        return had ? $"{user} unblocked." : $"{user} wasn't blocked.";
    }

    void SaveBlocks()
    {
        string json;
        lock (_pinLock) json = JsonSerializer.Serialize(_blocked.ToList());
        SaveSealed("blocks.sealed", json);
    }

    public async Task<string> SafetyCodeAsync(string user)
    {
        var p = await RequireUserAsync(user);
        string code = Crypto.SafetyCode(_me.Username, _me.Ek, _me.Sk, p.Username, p.Ek, p.Sk);
        return $"Safety code for you and {p.Username}:\n\n{code}\n\n" +
               (IsVerified(p.Username) ? "You have marked this contact as verified.\n\n" : "NOT verified yet.\n\n") +
               $"Compare this code with {p.Username} in person or on a call. If it matches exactly, press Mark verified.";
    }

    public async Task<string> DeleteMyMessagesAsync()
    {
        await RestAsync(HttpMethod.Delete, $"dms?sender_id=eq.{UserId}");
        await RestAsync(HttpMethod.Delete, $"messages?sender_id=eq.{UserId}");
        await RestAsync(HttpMethod.Delete, $"party_messages?sender_id=eq.{UserId}");
        return "Your messages were deleted from the server.";
    }

    public async Task DeleteAccountAsync()
    {
        await RestAsync(HttpMethod.Post, "rpc/delete_my_account", new { });
        try { Directory.Delete(_dir, true); } catch { }
    }

    async Task<Profile?> ProfileByIdAsync(string id)
    {
        if (_byId.TryGetValue(id, out var c)) return c;
        if (!Guid.TryParse(id, out _)) return null;
        var rows = await GetAsync($"profiles?select=user_id,username,ek,sk&user_id=eq.{id}");
        return rows.Count == 0 ? null : Cache(rows[0]!);
    }

    async Task<Profile?> ProfileByNameAsync(string name)
    {
        if (_byName.TryGetValue(name, out var c)) return c;
        if (!NameRx.IsMatch(name)) return null;
        var rows = await GetAsync($"profiles?select=user_id,username,ek,sk&username=ilike.{name}");
        var row = rows.FirstOrDefault(r => S(r!["username"]).Equals(name, StringComparison.OrdinalIgnoreCase));
        return row == null ? null : Cache(row);
    }

    Profile Cache(JsonNode r)
    {
        var p = new Profile(S(r["user_id"]), S(r["username"]), S(r["ek"]), S(r["sk"]));
        _byId[p.Id] = p; _byName[p.Username] = p;
        return p;
    }

    async Task<Profile> RequireUserAsync(string name, bool allowSelf = false)
    {
        var p = await ProfileByNameAsync(name) ?? throw new InvalidOperationException($"No such user '{name}'.");
        if (p.Id == UserId && !allowSelf) throw new InvalidOperationException("That's you.");
        return Trust(p) ?? throw new InvalidOperationException("Key mismatch - refusing to talk to this account.");
    }

    PartyState Party(string sel) => _parties.Values.FirstOrDefault(p =>
            p.Name.Equals(sel, StringComparison.OrdinalIgnoreCase) || p.Id.StartsWith(sel, StringComparison.OrdinalIgnoreCase))
        ?? throw new InvalidOperationException("You're not in that party.");

    void RequireOwner(PartyState p)
    {
        if (p.OwnerId != UserId) throw new InvalidOperationException("Only the party owner can do that.");
    }

    async Task LoadPartyAsync(string pid)
    {
        var pr = await GetAsync($"parties?select=id,name,owner_id,epoch&id=eq.{pid}");
        if (pr.Count == 0) { _parties.TryRemove(pid, out _); return; }

        var owner = await ProfileByIdAsync(S(pr[0]!["owner_id"]));
        if (owner == null || (owner = Trust(owner)) == null) return;

        var party = _parties.GetOrAdd(pid, _ => new PartyState { Id = pid });
        party.Name = S(pr[0]!["name"]);
        party.OwnerId = owner.Id;
        party.Epoch = int.Parse(S(pr[0]!["epoch"]));

        foreach (var k in await GetAsync($"party_keys?select=epoch,wrapped&party_id=eq.{pid}&user_id=eq.{UserId}"))
        {
            int ep = int.Parse(S(k!["epoch"]));
            var raw = Crypto.OpenStr(PairKey(owner.Ek), S(k["wrapped"]), $"PK|{pid}|{ep}|{UserId}");
            if (raw != null) party.Keys[ep] = Convert.FromBase64String(raw);
        }
        PartiesChanged?.Invoke();
    }

    async Task HandleAsync(string table, JsonNode rec)
    {
        DateTime when = DateTime.TryParse(S(rec["created_at"]), out var t) ? t.ToLocalTime() : DateTime.Now;

        switch (table)
        {
            case "messages":
            {
                if (!_seen.TryAdd("m:" + S(rec["id"]), 0)) return;
                var p = await ProfileByIdAsync(S(rec["sender_id"]));
                Emit("public", "lobby", p?.Username ?? "?", S(rec["body"]), p != null && IsVerified(p.Username), when);
                break;
            }
            case "dms":
            {
                string key = "d:" + S(rec["id"]);
                if (!_seen.TryAdd(key, 0)) return;
                string s = S(rec["sender_id"]), r = S(rec["recipient_id"]);
                var other = await ProfileByIdAsync(s == UserId ? r : s);
                if (other == null || (other = Trust(other)) == null) { _seen.TryRemove(key, out _); return; }
                string? text = Crypto.OpenStr(PairKey(other.Ek), S(rec["body"]), $"DM|{s}|{r}");
                Emit("dm", other.Username, s == UserId ? Username : other.Username, text ?? "[could not decrypt]", IsVerified(other.Username), when);
                break;
            }
            case "party_messages":
            {
                string key = "p:" + S(rec["id"]);
                if (!_seen.TryAdd(key, 0)) return;
                string pid = S(rec["party_id"]), sid = S(rec["sender_id"]), body = S(rec["body"]);
                int ep = int.Parse(S(rec["epoch"]));
                if (!_parties.TryGetValue(pid, out var party)) { _seen.TryRemove(key, out _); return; }

                var sender = await ProfileByIdAsync(sid);
                if (sender == null || (sender = Trust(sender)) == null) { _seen.TryRemove(key, out _); return; }
                if (!Crypto.Verify(sender.Sk, $"PM|{pid}|{ep}|{sid}|{body}", S(rec["sig"])))
                {
                    Emit("warn", party.Name, sender.Username, "Message with an INVALID signature was dropped (forged or tampered).", false, when);
                    return;
                }
                if (!party.Keys.TryGetValue(ep, out var k))
                {
                    _seen.TryRemove(key, out _);
                    return;
                }
                string? text = Crypto.OpenStr(k, body, $"PM|{pid}|{ep}|{sid}");
                Emit("party", party.Name, sender.Username, text ?? "[could not decrypt]", IsVerified(sender.Username), when);
                break;
            }
            case "party_invites":
            {
                if (S(rec["invitee_id"]) != UserId) return;
                string pid = S(rec["party_id"]);
                if (!_seen.TryAdd("i:" + pid, 0)) return;
                var pr = await GetAsync($"parties?select=name,owner_id&id=eq.{pid}");
                if (pr.Count == 0) return;
                var owner = await ProfileByIdAsync(S(pr[0]!["owner_id"]));
                Emit("invite", S(pr[0]!["name"]), owner?.Username ?? "?", $"invited you to '{S(pr[0]!["name"])}'. Type /accept {pid[..8]}");
                break;
            }
            case "appeal_messages":
            {
                if (!_seen.TryAdd("a:" + S(rec["id"]), 0)) return;
                string owner = S(rec["user_id"]), sid = S(rec["sender_id"]);
                if (owner != UserId && !IsAdmin) return;
                var o = await ProfileByIdAsync(owner);
                var snd = await ProfileByIdAsync(sid);
                Emit("appeal", o?.Username ?? "?", snd?.Username ?? "?", S(rec["body"]), false, when);
                break;
            }
            case "reports":
            {
                if (!IsAdmin) return;
                if (!_seen.TryAdd("r:" + S(rec["id"]), 0)) return;
                string by = Clean(S(rec["reporter_username"])), who = Clean(S(rec["reported_username"]));
                ReportReceived?.Invoke($"{(by.Length > 0 ? by : "Someone")} reported {who}");
                break;
            }
            case "party_keys":
            {
                if (S(rec["user_id"]) != UserId) return;
                string pid = S(rec["party_id"]);
                if (!_parties.ContainsKey(pid)) return;
                await LoadPartyAsync(pid);
                _ = Task.Run(() => CatchUpAsync(pid));
                break;
            }
        }
    }

    async Task CatchUpAsync(string? onlyParty = null)
    {
        try
        {
            if (onlyParty == null)
            {
                foreach (var r in Ordered(await GetAsync("messages?select=id,sender_id,body,created_at&order=id.desc&limit=30")))
                    await HandleAsync("messages", r);
                foreach (var r in Ordered(await GetAsync("dms?select=id,sender_id,recipient_id,body,created_at&order=id.desc&limit=50")))
                    await HandleAsync("dms", r);
                foreach (var r in await GetAsync($"party_invites?select=party_id,invitee_id&invitee_id=eq.{UserId}"))
                    await HandleAsync("party_invites", r!);
                if (IsAdmin || IsBanned)
                    foreach (var r in Ordered(await GetAsync("appeal_messages?select=id,user_id,sender_id,body,created_at&order=id.desc&limit=100")))
                        await HandleAsync("appeal_messages", r);
            }
            foreach (var pid in onlyParty != null ? new[] { onlyParty } : _parties.Keys.ToArray())
                foreach (var r in Ordered(await GetAsync($"party_messages?select=id,party_id,epoch,sender_id,body,sig,created_at&party_id=eq.{pid}&order=id.desc&limit=50")))
                    await HandleAsync("party_messages", r);
        }
        catch (Exception ex) { Emit("warn", "system", "", "Catch-up failed: " + ex.Message); }
    }

    static IEnumerable<JsonNode> Ordered(JsonArray rows)
        => rows.Where(r => r != null).Select(r => r!).OrderBy(r => long.Parse(S(r["id"])));

    JsonObject JoinMessage() => new()
    {
        ["topic"] = "realtime:chat",
        ["event"] = "phx_join",
        ["ref"] = "1",
        ["payload"] = new JsonObject
        {
            ["config"] = new JsonObject
            {
                ["postgres_changes"] = PgChanges()
            },
            ["access_token"] = _access
        }
    };

    JsonArray PgChanges()
    {
        var a = new JsonArray(
            TableSub("messages"), TableSub("dms"), TableSub("party_messages"),
            TableSub("party_invites"), TableSub("party_keys"), TableSub("appeal_messages"));
        if (IsAdmin) a.Add(TableSub("reports"));
        return a;
    }

    static JsonObject TableSub(string table) => new() { ["event"] = "INSERT", ["schema"] = "public", ["table"] = table };

    async Task RealtimeLoopAsync()
    {
        var ct = _cts.Token;
        int wait = 1;
        while (!ct.IsCancellationRequested)
        {
            try
            {
                using var ws = new ClientWebSocket();
                var uri = new Uri($"wss://{new Uri(_url).Host}/realtime/v1/websocket?apikey={_anon}&vsn=1.0.0");
                await ws.ConnectAsync(uri, ct);
                _ws = ws;
                await WsSendAsync(JoinMessage());

                using var hb = CancellationTokenSource.CreateLinkedTokenSource(ct);
                _ = HeartbeatAsync(hb.Token);
                _ = Task.Run(() => CatchUpAsync(), ct);
                wait = 1;

                while (ws.State == WebSocketState.Open)
                {
                    string? msg = await ReceiveAsync(ws, ct);
                    if (msg == null) break;
                    try { await OnWsMessageAsync(msg); }
                    catch (Exception ex) { Emit("warn", "system", "", "Bad realtime message: " + ex.Message); }
                }
                hb.Cancel();
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { return; }
            catch (Exception ex) { Emit("warn", "system", "", $"Realtime lost ({ex.Message}). Reconnecting..."); }

            _ws = null;
            try { await Task.Delay(wait * 1000, ct); } catch { return; }
            wait = Math.Min(wait * 2, 30);
        }
    }

    async Task HeartbeatAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                await Task.Delay(25_000, ct);
                await WsSendAsync(new JsonObject { ["topic"] = "phoenix", ["event"] = "heartbeat", ["payload"] = new JsonObject(), ["ref"] = "hb" });
            }
        }
        catch { }
    }

    async Task OnWsMessageAsync(string msg)
    {
        var j = JsonNode.Parse(msg);
        string ev = S(j?["event"]);
        var payload = j?["payload"];

        if (ev == "postgres_changes")
        {
            var d = payload?["data"];
            if (d != null && S(d["type"]) == "INSERT" && d["record"] is JsonNode rec)
                await HandleAsync(S(d["table"]), rec);
        }
        else if (S(payload?["status"]) == "error")
        {
            Emit("warn", "system", "", "Realtime error: " + (S(payload?["message"]) is { Length: > 0 } m ? m : S(payload?["response"])));
        }
    }

    async Task WsSendAsync(JsonNode msg)
    {
        var ws = _ws;
        if (ws is not { State: WebSocketState.Open }) return;
        var data = Encoding.UTF8.GetBytes(msg.ToJsonString());
        await _wsLock.WaitAsync();
        try { await ws.SendAsync(new ArraySegment<byte>(data), WebSocketMessageType.Text, true, _cts.Token); }
        finally { _wsLock.Release(); }
    }

    static async Task<string?> ReceiveAsync(ClientWebSocket ws, CancellationToken ct)
    {
        var buf = new byte[16384];
        using var ms = new MemoryStream();
        while (true)
        {
            var r = await ws.ReceiveAsync(new ArraySegment<byte>(buf), ct);
            if (r.MessageType == WebSocketMessageType.Close) return null;
            ms.Write(buf, 0, r.Count);
            if (r.EndOfMessage) return Encoding.UTF8.GetString(ms.ToArray());
        }
    }

    async Task RefreshLoopAsync()
    {
        var ct = _cts.Token;
        while (!ct.IsCancellationRequested)
        {
            var wait = _expires - DateTime.UtcNow - TimeSpan.FromMinutes(5);
            if (wait < TimeSpan.FromSeconds(30)) wait = TimeSpan.FromSeconds(30);
            try { await Task.Delay(wait, ct); } catch { return; }

            try
            {
                var (ok, body) = await AuthCall("token?grant_type=refresh_token", new { refresh_token = _refresh });
                if (!ok) { Emit("warn", "system", "", "Session refresh failed. Restart the app and log in again."); continue; }
                SetSession(JsonNode.Parse(body)!);
                await WsSendAsync(new JsonObject
                {
                    ["topic"] = "realtime:chat", ["event"] = "access_token", ["ref"] = "2",
                    ["payload"] = new JsonObject { ["access_token"] = _access }
                });
            }
            catch (Exception ex) { Emit("warn", "system", "", "Session refresh error: " + ex.Message); }
        }
    }

    public string ModStatus { get; private set; } = "none";
    public string ModReason { get; private set; } = "";
    public bool IsOnHold => ModStatus == "hold";
    public bool IsBanned => ModStatus == "banned";
    public bool IsAdmin { get; private set; }

    public event Action? ModerationChanged;
    public event Action<string>? ReportReceived;

    public const string NameTooCloseMessage =
        "That name is too close to an existing name. Every name must differ from all other names by at least 3 characters (letters added, removed or changed). Try another.";

    void RequireCanSend()
    {
        if (IsBanned) throw new InvalidOperationException("You are banned. You can only talk to the admin in the appeal chat.");
        if (IsOnHold) throw new InvalidOperationException("Your chats are on hold while a report is looked at. You can read, but not send.");
    }

    void RequireAdmin()
    {
        if (!IsAdmin) throw new InvalidOperationException("Admins only.");
    }

    public async Task<bool> UsernameTooCloseAsync(string name)
    {
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, $"{_url}/rest/v1/rpc/username_too_close");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", string.IsNullOrEmpty(_access) ? _anon : _access);
            req.Content = JsonContent.Create(new { candidate = name });
            using var r = await _http.SendAsync(req, _cts.Token);
            if (!r.IsSuccessStatusCode) return false;
            return (await r.Content.ReadAsStringAsync()).Trim() == "true";
        }
        catch { return false; }
    }

    async Task RegisterDeviceAsync()
    {
        try { await RestAsync(HttpMethod.Post, "rpc/register_device", new { h = DeviceId.Get() }); }
        catch (InvalidOperationException ex) when (ex.Message.Contains("linked to an account", StringComparison.OrdinalIgnoreCase)) { throw; }
        catch { }
    }

    async Task CheckAdminAsync()
    {
        try { IsAdmin = JsonNode.Parse(await RestAsync(HttpMethod.Post, "rpc/is_admin", new { }))?.GetValue<bool>() == true; }
        catch { IsAdmin = false; }
    }

    public async Task RefreshModerationAsync()
    {
        string st = "none", why = "";
        try
        {
            var rows = await GetAsync($"moderation?select=status,reason&user_id=eq.{UserId}");
            if (rows.Count > 0)
            {
                st = S(rows[0]!["status"]);
                why = Clean(S(rows[0]!["reason"]));
                if (st == "clear") { st = "none"; why = ""; }
            }
        }
        catch { return; }
        if (st == ModStatus && why == ModReason) return;
        ModStatus = st; ModReason = why;
        ModerationChanged?.Invoke();
        if (st == "banned") _ = Task.Run(() => CatchUpAsync());
    }

    async Task ModerationLoopAsync()
    {
        var ct = _cts.Token;
        while (!ct.IsCancellationRequested)
        {
            try { await Task.Delay(10_000, ct); } catch { return; }
            try { await RefreshModerationAsync(); } catch { }
        }
    }

    static string CleanText(string s) => new(s.Where(c => c == '\n' || !char.IsControl(c)).ToArray());

    public async Task<List<ReportItem>> ListReportsAsync(bool includeClosed)
    {
        RequireAdmin();
        string filter = includeClosed ? "" : "&status=eq.open";
        var rows = await GetAsync($"reports?select=id,reported_id,reported_username,reporter_username,reason,evidence,status,created_at{filter}&order=id.desc&limit=200");
        var list = new List<ReportItem>();
        foreach (var r in rows)
        {
            if (r == null || !long.TryParse(S(r["id"]), out long id)) continue;
            DateTime when = DateTime.TryParse(S(r["created_at"]), out var t) ? t.ToLocalTime() : DateTime.Now;
            list.Add(new ReportItem(id, S(r["reported_id"]), Clean(S(r["reported_username"])), Clean(S(r["reporter_username"])),
                CleanText(S(r["reason"])), CleanText(S(r["evidence"])), Clean(S(r["status"])), when));
        }
        return list;
    }

    public async Task<Dictionary<string, string>> ModerationStatesAsync()
    {
        RequireAdmin();
        var d = new Dictionary<string, string>();
        foreach (var r in await GetAsync("moderation?select=user_id,status&limit=1000"))
            if (r != null) d[S(r["user_id"])] = S(r["status"]);
        return d;
    }

    public async Task<int> CountOpenReportsAsync()
    {
        if (!IsAdmin) return 0;
        return (await GetAsync("reports?select=id&status=eq.open&limit=1000")).Count;
    }

    public async Task<string> ModerateAsync(string userId, string username, string status, string why = "")
    {
        RequireAdmin();
        await RestAsync(HttpMethod.Post, "rpc/mod_set_status", new { target = userId, new_status = status, why });
        return status switch
        {
            "hold" => $"{username} is on hold: they can read their chats but not send.",
            "banned" => $"{username} is banned. They can only message you in the appeal chat.",
            _ => $"{username} was released."
        };
    }

    public async Task<string?> ResolveUserIdAsync(string name) => (await ProfileByNameAsync(name))?.Id;

    public async Task SendAppealAsync(string text, string? threadUserId = null)
    {
        CheckLen(text);
        string thread = threadUserId ?? UserId;
        if (thread == UserId && !IsBanned && !IsAdmin)
            throw new InvalidOperationException("The appeal chat is only for people who are banned.");
        var rows = JsonNode.Parse(await RestAsync(HttpMethod.Post, "appeal_messages", new { user_id = thread, body = text }, true))!.AsArray();
        if (_seen.TryAdd("a:" + S(rows[0]!["id"]), 0))
        {
            string owner = thread == UserId ? Username : (await ProfileByIdAsync(thread))?.Username ?? "?";
            Emit("appeal", owner, Username, text, false);
        }
    }

    static readonly Regex GameRx = new("^[a-z0-9_-]{1,24}$");

    public async Task<List<LfgPost>> ListLfgAsync()
    {
        var rows = await GetAsync("lfg_posts?select=id,user_id,username,game,activity,note,needed,expires_at&order=created_at.desc&limit=200");
        var list = new List<LfgPost>();
        foreach (var r in rows)
        {
            if (r == null) continue;
            string name = Clean(S(r["username"])), game = Clean(S(r["game"]));
            if (!NameRx.IsMatch(name) || !GameRx.IsMatch(game)) continue;
            if (!name.Equals(Username, StringComparison.OrdinalIgnoreCase) && IsBlocked(name)) continue;
            if (!long.TryParse(S(r["id"]), out long id)) continue;
            int.TryParse(S(r["needed"]), out int needed);
            if (!DateTime.TryParse(S(r["expires_at"]), System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal, out var exp)) continue;
            list.Add(new LfgPost(id, S(r["user_id"]), name, game, Clean(S(r["activity"])), Clean(S(r["note"])), Math.Clamp(needed, 1, 128), exp));
        }
        return list;
    }

    public async Task<string> PostLfgAsync(string game, string activity, string note, int needed, int minutes)
    {
        RequireCanSend();
        if (!GameRx.IsMatch(game)) throw new InvalidOperationException("Pick a game first.");
        static string Cut(string s, int n) => s.Length <= n ? s : s[..n];
        await RestAsync(HttpMethod.Delete, $"lfg_posts?user_id=eq.{UserId}&game=eq.{game}");
        await RestAsync(HttpMethod.Post, "lfg_posts", new
        {
            game,
            activity = Cut(Clean(activity), 30),
            note = Cut(Clean(note).Trim(), 120),
            needed = Math.Clamp(needed, 1, 128),
            expires_at = DateTime.UtcNow.AddMinutes(Math.Clamp(minutes, 15, 1440)).ToString("o")
        });
        return "Posted. Other players can now see that you are looking.";
    }

    public async Task RemoveLfgAsync(long id)
        => await RestAsync(HttpMethod.Delete, $"lfg_posts?id=eq.{id}&user_id=eq.{UserId}");

    async Task<string> RestAsync(HttpMethod method, string path, object? body = null, bool returning = false)
    {
        using var req = new HttpRequestMessage(method, $"{_url}/rest/v1/{path}");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _access);
        req.Headers.Add("Prefer", returning ? "return=representation" : "return=minimal");
        if (body != null) req.Content = JsonContent.Create(body);
        using var r = await _http.SendAsync(req, _cts.Token);
        string s = await r.Content.ReadAsStringAsync();
        if (!r.IsSuccessStatusCode) throw new InvalidOperationException(Friendly((int)r.StatusCode, s));
        return s;
    }

    async Task<JsonArray> GetAsync(string path) => JsonNode.Parse(await RestAsync(HttpMethod.Get, path))!.AsArray();

    static string Friendly(int status, string body)
    {
        try
        {
            var j = JsonNode.Parse(body);
            string code = S(j?["code"]);
            if (code == "P0001") return S(j?["message"]);
            if (code == "23505") return "That already exists.";
            if (code == "23514") return "Rejected by the server's size/format limits.";
            if (code == "42501") return "Not allowed (blocked by the server's security rules).";
            return $"{status}: {S(j?["message"])}";
        }
        catch { return status.ToString(); }
    }

    static byte[] PairKeyFrom(ECDiffieHellman mine, string theirEk)
    {
        using var peer = ECDiffieHellman.Create();
        peer.ImportSubjectPublicKeyInfo(Convert.FromBase64String(theirEk), out _);
        byte[] secret = mine.DeriveRawSecretAgreement(peer.PublicKey);
        return HKDF.DeriveKey(HashAlgorithmName.SHA256, secret, 32, info: Encoding.UTF8.GetBytes("wwtanat-pair-v1"));
    }

    byte[] PairKey(string theirEk) => PairKeyFrom(_ecdh!, theirEk);

    string WrapKey(byte[] partyKey, Profile target, string pid, int epoch)
        => Crypto.SealStr(PairKey(target.Ek), B64(partyKey), $"PK|{pid}|{epoch}|{target.Id}");

    static void CheckLen(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) throw new InvalidOperationException("Empty message.");
        if (text.Length > MaxChars) throw new InvalidOperationException($"Too long (max {MaxChars} characters).");
    }

    static string S(JsonNode? n) => n?.ToString() ?? "";
    static string B64(byte[] b) => Convert.ToBase64String(b);

    static string Clean(string s) => new(s.Where(c => !char.IsControl(c)).ToArray());

    void Emit(string kind, string channel, string from, string text, bool verified = false, DateTime? when = null)
    {
        string f = Clean(from);
        if ((kind is "dm" or "party" or "public" or "invite") && f.Length > 0
            && !f.Equals(Username, StringComparison.OrdinalIgnoreCase) && IsBlocked(f)) return;
        OnEvent?.Invoke(new ChatEvent(kind, Clean(channel), f, Clean(text), verified, when ?? DateTime.Now));
    }

    public void Dispose()
    {
        _cts.Cancel();
        try { _ws?.Abort(); } catch { }
        _http.Dispose();
    }
}
