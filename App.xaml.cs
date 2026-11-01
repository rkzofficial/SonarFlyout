using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace SonarFlyout;

public partial class App : Application
{
    FlyoutWindow? _flyout;
    TrayIcon? _tray;
    DispatcherQueueTimer? _volumeTimer;
    SonarClient? _sonar;
    int _pendingSteps;
    bool _sendingSteps;

    public App()
    {
        InitializeComponent();
        DispatcherShutdownMode = DispatcherShutdownMode.OnExplicitShutdown;
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var sonar = _sonar = new SonarClient();
        _flyout = new FlyoutWindow(sonar);
        var menu = new TrayMenu(Quit);
        _tray = new TrayIcon(_flyout.Toggle, menu.Show, ScrollVolume);

        // Sonar has no change notifications and the volume also moves from hotkeys and GG, so poll it for the icon.
        _volumeTimer = DispatcherQueue.GetForCurrentThread().CreateTimer();
        _volumeTimer.Interval = TimeSpan.FromSeconds(1);
        _volumeTimer.IsRepeating = false;
        _volumeTimer.Tick += async (timer, _) =>
        {
            try
            {
                var master = (await sonar.GetVolumesAsync())[SonarClient.MasterChannel];
                // A read that started before a scroll would put the old level back on the icon.
                if (!_sendingSteps)
                    _tray.SetVolume(master);
            }
            catch
            {
                // GG isn't reachable right now; keep the last icon until it is.
            }
            timer.Start();
        };
        _volumeTimer.Start();
    }

    // Wheel notches arrive faster than Sonar needs; apply whatever has piled up once the previous request completes.
    async void ScrollVolume(int steps)
    {
        _pendingSteps += steps;
        if (_sendingSteps)
            return;
        _sendingSteps = true;
        try
        {
            while (_pendingSteps != 0)
            {
                int notches = _pendingSteps;
                _pendingSteps = 0;
                var master = (await _sonar!.GetVolumesAsync())[SonarClient.MasterChannel];
                if (notches > 0 && master.Muted)
                    await _sonar.SetMutedAsync(SonarClient.MasterChannel, false);
                double target = Math.Clamp(Math.Round(master.Volume * 100) + 2 * notches, 0, 100) / 100;
                if (target != master.Volume)
                    await _sonar.SetVolumeAsync(SonarClient.MasterChannel, target);
                var volumes = await _sonar.GetVolumesAsync();
                _tray!.SetVolume(volumes[SonarClient.MasterChannel]);
                _flyout!.ShowVolumes(volumes);
            }
        }
        catch
        {
            _pendingSteps = 0;
        }
        finally
        {
            _sendingSteps = false;
        }
    }

    void Quit()
    {
        _volumeTimer?.Stop();
        _tray?.Dispose();
        Exit();
    }
}
