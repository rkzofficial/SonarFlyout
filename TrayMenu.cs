using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using System.Runtime.InteropServices;
using Windows.ApplicationModel;
using Windows.Graphics;
using Windows.System;
using static SonarFlyout.Native;

namespace SonarFlyout;

// The Windows 11 volume icon's menu is a XAML MenuFlyout, which needs a XAML root; this invisible,
// click-through 1x1 window provides one.
sealed class TrayMenu
{
    // Matches the startupTask TaskId in Package.appxmanifest.
    const string StartupTaskId = "SonarFlyoutStartup";

    readonly Window _host = new() { Content = new Grid { FlowDirection = Strings.FlowDirection } };
    readonly MenuFlyout _menu = new() { ShouldConstrainToRootBounds = false };
    // A plain item whose icon shows the state: a ToggleMenuFlyoutItem next to icon items makes the menu reserve
    // a checkmark column and an icon column, pushing every row right.
    readonly MenuFlyoutItem _startup = new() { Text = Strings.Get("MenuStartWithWindows") };
    readonly nint _hwnd;
    double _menuWidth;

    public TrayMenu(Action exit)
    {
        _hwnd = Win32Interop.GetWindowFromWindowId(_host.AppWindow.Id);
        var presenter = OverlappedPresenter.Create();
        presenter.SetBorderAndTitleBar(false, false);
        presenter.IsResizable = false;
        presenter.IsAlwaysOnTop = true;
        _host.AppWindow.SetPresenter(presenter);
        _host.AppWindow.IsShownInSwitchers = false;
        SetWindowLongPtrW(_hwnd, GWL_EXSTYLE, GetWindowLongPtrW(_hwnd, GWL_EXSTYLE) | WS_EX_LAYERED | WS_EX_TRANSPARENT);
        SetLayeredWindowAttributes(_hwnd, 0, 0, LWA_ALPHA);

        var openGG = new MenuFlyoutItem { Text = Strings.Get("MenuOpenGG"), Icon = new FontIcon { Glyph = "\uE713" } };
        openGG.Click += (_, _) => SonarClient.OpenInGG();
        _startup.Click += async (_, _) =>
        {
            var task = await StartupTask.GetAsync(StartupTaskId);
            if (task.State == StartupTaskState.Enabled)
                task.Disable();
            // Once the user has turned it off in Settings or Task Manager, only they can turn it back on there.
            else if (await task.RequestEnableAsync() is StartupTaskState.DisabledByUser or StartupTaskState.DisabledByPolicy)
                await Launcher.LaunchUriAsync(new Uri("ms-settings:startupapps"));
        };
        var quit = new MenuFlyoutItem { Text = Strings.Get("MenuExit"), Icon = new FontIcon { Glyph = "\uE711" } };
        quit.Click += (_, _) => exit();

        _menu.Items.Add(openGG);
        _menu.Items.Add(_startup);
        _menu.Items.Add(new MenuFlyoutSeparator());
        _menu.Items.Add(quit);
        // The shell's own tray menus use mouse density, tighter than MenuFlyout's default item padding.
        foreach (var item in new MenuFlyoutItem[] { openGG, _startup, quit })
            item.Padding = new Thickness(11, 4, 11, 5);
        _menu.Closed += (_, _) => _host.AppWindow.Hide();
        WarmUp();
    }

    // A context menu opens at its anchor whatever Placement says, so centring it needs its width, which is only
    // known once it has been open.
    async void WarmUp()
    {
        _host.AppWindow.MoveAndResize(new RectInt32(-32000, -32000, 1, 1));
        _host.AppWindow.Show(false);
        await OpenAtAsync(new Windows.Foundation.Point(0, 0));
        _menu.Hide();
    }

    public async void Show(int x, int y)
    {
        var state = (await StartupTask.GetAsync(StartupTaskId)).State;
        bool startsWithWindows = state is StartupTaskState.Enabled or StartupTaskState.EnabledByPolicy;
        _startup.Icon = new FontIcon { Glyph = startsWithWindows ? "\uE73E" : "\uE7E8" };
        AutomationProperties.SetName(_startup, Strings.Get(startsWithWindows ? "MenuStartWithWindowsOn" : "MenuStartWithWindowsOff"));

        // 12px is the gap above the taskbar measured on the native volume menu.
        var monitor = MonitorFromPoint(new POINT { X = x, Y = y }, MONITOR_DEFAULTTONEAREST);
        var info = new MONITORINFO { cbSize = (uint)Marshal.SizeOf<MONITORINFO>() };
        GetMonitorInfoW(monitor, ref info);
        GetDpiForMonitor(monitor, MDT_EFFECTIVE_DPI, out uint dpi, out _);
        int gap = (int)Math.Round(12 * dpi / 96.0);
        int halfWidth = (int)Math.Round(_menuWidth * dpi / 96.0 / 2);
        _host.AppWindow.MoveAndResize(new RectInt32(x - halfWidth, Math.Min(y, info.rcWork.Bottom - gap) - 1, 1, 1));
        _host.AppWindow.Show(true);
        _host.Activate();
        // Light dismiss only works while the host owns the foreground.
        SetForegroundWindow(_hwnd);
        await OpenAtAsync(new Windows.Foundation.Point(0, 0));
    }

    async Task OpenAtAsync(Windows.Foundation.Point position)
    {
        var root = (FrameworkElement)_host.Content;
        if (!root.IsLoaded)
        {
            var loaded = new TaskCompletionSource();
            root.Loaded += (_, _) => loaded.TrySetResult();
            await loaded.Task;
        }
        var opened = new TaskCompletionSource();
        void OnOpened(object? sender, object e) => opened.TrySetResult();
        _menu.Opened += OnOpened;
        _menu.ShowAt(root, new FlyoutShowOptions { Position = position });
        await opened.Task;
        _menu.Opened -= OnOpened;

        // The menu's visual tree only exists while it's open and is laid out just after Opened.
        await Task.Delay(50);
        // The presenter surfaces as its ItemsControl base here, not as MenuFlyoutPresenter.
        for (DependencyObject? parent = _menu.Items[0]; parent is not null; parent = VisualTreeHelper.GetParent(parent))
        {
            if (parent is ItemsControl { ActualWidth: > 0 } presenter)
            {
                _menuWidth = presenter.ActualWidth;
                break;
            }
        }
    }
}
