using System.Windows;

namespace AHFMCChat;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        DispatcherUnhandledException += (_, a) =>
        {
            MessageBox.Show("Something went wrong: " + a.Exception.Message, "Chat", MessageBoxButton.OK, MessageBoxImage.Warning);
            a.Handled = true;
        };

        while (true)
        {
            var auth = new AuthWindow();
            if (auth.ShowDialog() != true || auth.Chat == null) break;

            var main = new MainWindow(auth.Chat, auth.Hub);
            main.ShowDialog();
            if (main.LoggedOut) SessionStore.Clear();
            if (!main.LoggedOut) break;
        }
        Shutdown();
    }
}
