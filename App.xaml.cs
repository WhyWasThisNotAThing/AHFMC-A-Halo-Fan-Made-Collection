using System.Windows;

namespace AHFMC;

public partial class App : System.Windows.Application
{
    private const string InstanceId = "AHFMC.SingleInstance.7f3c9a52";
    private Mutex? _mutex;
    private EventWaitHandle? _showSignal;
    private bool _owner;
    private AHFMC.MainWindow? _window;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            _mutex = new Mutex(true, "Local\\" + InstanceId, out _owner);
            _showSignal = new EventWaitHandle(false, EventResetMode.AutoReset, "Local\\" + InstanceId + ".show");
        }
        catch { _owner = true; }

        if (!_owner)
        {
            try { _showSignal?.Set(); } catch { }
            Shutdown();
            return;
        }

        _window = new AHFMC.MainWindow();
        MainWindow = _window;
        _window.Show();

        if (_showSignal != null)
        {
            var signal = _showSignal;
            new Thread(() =>
            {
                while (true)
                {
                    try { signal.WaitOne(); } catch { return; }
                    try { Dispatcher.BeginInvoke(new Action(() => _window?.ShowFromSecondLaunch())); } catch { return; }
                }
            }) { IsBackground = true, Name = "SingleInstanceListener" }.Start();
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        try { if (_owner) _mutex?.ReleaseMutex(); } catch { }
        try { _mutex?.Dispose(); } catch { }
        base.OnExit(e);
    }
}
