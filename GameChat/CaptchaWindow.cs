using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace AHFMCChat;

public sealed class CaptchaWindow : Window
{
    public static bool Enabled => !string.IsNullOrWhiteSpace(Config.CaptchaSiteKey);

    readonly WebView2 _web = new();
    HttpListener? _server;
    string? _token;

    CaptchaWindow()
    {
        Title = "Verify you're human";
        Width = 360;
        Height = 210;
        ResizeMode = ResizeMode.NoResize;
        ShowInTaskbar = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = (Brush)Application.Current.FindResource("Bg");
        Content = _web;
        Loaded += async (_, _) => await InitAsync();
        Closed += (_, _) => { try { _server?.Close(); } catch { } };
    }

    public static Task<string?> GetTokenAsync(Window owner)
    {
        var w = new CaptchaWindow { Owner = owner };
        w.ShowDialog();
        return Task.FromResult(w._token);
    }

    async Task InitAsync()
    {
        try
        {
            string dir = Path.Combine(AppDir.Path_, "webview");
            var env = await CoreWebView2Environment.CreateAsync(null, dir);
            await _web.EnsureCoreWebView2Async(env);
            _web.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
            _web.CoreWebView2.Settings.AreDevToolsEnabled = false;
            _web.CoreWebView2.WebMessageReceived += (_, e) =>
            {
                string msg = e.TryGetWebMessageAsString() ?? "";
                if (msg.Length == 0 || msg == "!error")
                {
                    MessageBox.Show(this, "Verification failed. Check your internet connection and try again.", "Chat");
                    DialogResult = false;
                    return;
                }
                _token = msg;
                DialogResult = true;
            };

            int port = StartServer();
            _web.CoreWebView2.Navigate($"http://localhost:{port}/");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this,
                "Couldn't start the verification step (the Microsoft WebView2 runtime may be missing).\n\n" + ex.Message,
                "Chat");
            DialogResult = false;
        }
    }

    int StartServer()
    {
        var probe = new TcpListener(IPAddress.Loopback, 0);
        probe.Start();
        int port = ((IPEndPoint)probe.LocalEndpoint).Port;
        probe.Stop();

        _server = new HttpListener();
        _server.Prefixes.Add($"http://localhost:{port}/");
        _server.Start();

        byte[] page = Encoding.UTF8.GetBytes(Html(Config.CaptchaSiteKey));
        var server = _server;
        _ = Task.Run(async () =>
        {
            while (server.IsListening)
            {
                try
                {
                    var ctx = await server.GetContextAsync();
                    if (ctx.Request.Url?.AbsolutePath == "/")
                    {
                        ctx.Response.ContentType = "text/html; charset=utf-8";
                        ctx.Response.ContentLength64 = page.Length;
                        await ctx.Response.OutputStream.WriteAsync(page);
                    }
                    else ctx.Response.StatusCode = 404;
                    ctx.Response.Close();
                }
                catch { return; }
            }
        });
        return port;
    }

    static string Html(string siteKey) => $$"""
        <!doctype html>
        <html><head><meta charset="utf-8">
        <script src="https://challenges.cloudflare.com/turnstile/v0/api.js?render=explicit" async defer></script>
        <style>html,body{margin:0;height:100%;background:#14161B;color:#E6E8EC;font-family:Segoe UI,sans-serif}
        body{display:flex;flex-direction:column;align-items:center;justify-content:center;gap:10px}</style></head>
        <body><div id="w"></div><div style="font-size:12px;color:#8A91A0">Checking that you're not a bot...</div>
        <script>
        function post(m){ window.chrome.webview.postMessage(m); }
        var t = setInterval(function(){
          if (window.turnstile) {
            clearInterval(t);
            turnstile.render('#w', {
              sitekey: '{{siteKey}}', theme: 'dark',
              callback: function(tok){ post(tok); },
              'error-callback': function(){ post('!error'); }
            });
          }
        }, 100);
        </script></body></html>
        """;
}
