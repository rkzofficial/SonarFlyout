using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace SonarFlyout;

public partial class App : Application
{
    FlyoutWindow? _flyout;
    TrayIcon? _tray;
    DispatcherQueueTimer? _volumeTimer;

    public App()
    {
        InitializeComponent();
        DispatcherShutdownMode = DispatcherShutdownMode.OnExplicitShutdown;
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var sonar = new SonarClient();
        _flyout = new FlyoutWindow(sonar);
        var menu = new TrayMenu(Quit);
        _tray = new TrayIcon(_flyout.Toggle, menu.Show);

        // Sonar has no change notifications and the volume also moves from hotkeys and GG, so poll it for the icon.
        _volumeTimer = DispatcherQueue.GetForCurrentThread().CreateTimer();
        _volumeTimer.Interval = TimeSpan.FromSeconds(1);
        _volumeTimer.IsRepeating = false;
        _volumeTimer.Tick += async (timer, _) =>
        {
            try
            {
                _tray.SetVolume((await sonar.GetVolumesAsync())[SonarClient.MasterChannel]);
            }
            catch
            {
                // GG isn't reachable right now; keep the last icon until it is.
            }
            timer.Start();
        };
        _volumeTimer.Start();
    }

    void Quit()
    {
        _volumeTimer?.Stop();
        _tray?.Dispose();
        Exit();
    }
}
