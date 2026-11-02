using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using System.Runtime.InteropServices;
using Windows.Graphics;
using Windows.Media.Devices;
using Windows.System;
using static SonarFlyout.Native;

namespace SonarFlyout;

public sealed partial class FlyoutWindow : Window
{
    const double WidthDips = 360;
    const double MarginDips = 12;
    const string MasterChannel = SonarClient.MasterChannel;
    const string MicChannel = "chatCapture";
    const string MicRoute = "mic";

    static readonly (string Name, string Channel, string Route)[] s_mixer =
    [
        (Strings.Get("ChannelGame"), "game", "game"),
        (Strings.Get("ChannelChat"), "chatRender", "chat"),
        (Strings.Get("ChannelMedia"), "media", "media"),
        (Strings.Get("ChannelAux"), "aux", "aux"),
    ];

    static nint s_previousWndProc;

    readonly SonarClient _sonar;
    readonly nint _hwnd;
    readonly Dictionary<string, ChannelRow> _rows = [];
    readonly Dictionary<string, double> _pendingVolumes = [];
    readonly HashSet<string> _sendingVolumes = [];
    SonarState? _state;
    bool _confirmingFix;
    nint _monitor;
    string _mainChannel = "game";
    long _hiddenAt;
    bool _opening;
    bool _updating;

    public FlyoutWindow(SonarClient sonar)
    {
        InitializeComponent();
        Title = Strings.Get("WindowTitle");
        Root.FlowDirection = Strings.FlowDirection;
        _sonar = sonar;
        _hwnd = Win32Interop.GetWindowFromWindowId(AppWindow.Id);

        var presenter = OverlappedPresenter.Create();
        presenter.IsResizable = false;
        presenter.IsMaximizable = false;
        presenter.IsMinimizable = false;
        presenter.IsAlwaysOnTop = true;
        presenter.SetBorderAndTitleBar(false, false);
        AppWindow.SetPresenter(presenter);
        AppWindow.IsShownInSwitchers = false;
        unsafe
        {
            s_previousWndProc = SetWindowLongPtrW(_hwnd, GWLP_WNDPROC, (nint)(delegate* unmanaged<nint, uint, nint, nint, nint>)&WndProc);
        }
        SetWindowPos(_hwnd, 0, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE | SWP_FRAMECHANGED);
        DwmSetWindowAttribute(_hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, DWMWCP_ROUND, sizeof(int));
        SyncFrameTheme();
        Root.ActualThemeChanged += (_, _) => SyncFrameTheme();

        AppWindow.Closing += (_, e) =>
        {
            e.Cancel = true;
            Hide();
        };
        Activated += (_, e) =>
        {
            if (e.WindowActivationState == WindowActivationState.Deactivated)
                Hide();
        };
        Root.SizeChanged += (_, _) =>
        {
            if (!_opening)
                PlaceWindow();
        };
        Root.KeyDown += (_, e) =>
        {
            if (e.Key == VirtualKey.Escape)
                Hide();
        };

        MasterHost.Content = AddRow(Strings.Get("ChannelMaster"), MasterChannel);
        foreach (var (name, channel, route) in s_mixer)
            Channels.Children.Add(AddRow(name, channel, route, name));
        MicHost.Content = AddRow(Strings.Get("ChannelMic"), MicChannel, isMic: true);

        // ItemClick rather than SelectionChanged: clicking the already-highlighted device must still pull every channel onto it.
        Outputs.ItemClick += (_, e) =>
        {
            if (e.ClickedItem is not FrameworkElement { Tag: string id })
                return;
            Run(async () =>
            {
                foreach (var (_, _, route) in s_mixer)
                    await _sonar.SetOutputAsync(route, id);
                await LoadAsync();
            });
        };
        SyncOutputs.Checked += (_, _) => SetOutputsLinked(true);
        SyncOutputs.Unchecked += (_, _) => SetOutputsLinked(false);
        Inputs.SelectionChanged += (_, _) =>
        {
            if (!_updating && Inputs.SelectedItem is ListViewItem { Tag: string id })
                Run(() => _sonar.SetOutputAsync(MicRoute, id));
        };
        // InfoBar's 96px minimum button width is what pushes the button below even a short message in a 360px flyout.
        var useSonar = new Button { Content = Strings.Get("DefaultDevicesFix"), MinWidth = 0 };
        useSonar.Click += (_, _) => UseSonarDevices();
        DefaultDevicesBar.ActionButton = useSonar;
        MediaDevice.DefaultAudioRenderDeviceChanged += (_, _) => DispatcherQueue.TryEnqueue(OnDefaultDeviceChanged);
        MediaDevice.DefaultAudioCaptureDeviceChanged += (_, _) => DispatcherQueue.TryEnqueue(OnDefaultDeviceChanged);
        Motion.FadeInWhenShown(PerChannelHint);
        Run(WarmUpAsync);
        OpenGG.Click += (_, _) =>
        {
            Hide();
            SonarClient.OpenInGG();
        };
    }

    // The presenter keeps WS_DLGFRAME, which paints a light inner frame inside DWM's rounded border;
    // claiming the whole window as client area removes it.
    [UnmanagedCallersOnly]
    static nint WndProc(nint hwnd, uint message, nint wParam, nint lParam) =>
        message == WM_NCCALCSIZE && wParam != 0 ? 0 : CallWindowProcW(s_previousWndProc, hwnd, message, wParam, lParam);

    // Pin the border to the flyout stroke colour so the user's "accent colour on borders" setting doesn't outline it.
    void SyncFrameTheme()
    {
        bool dark = Root.ActualTheme == ElementTheme.Dark;
        DwmSetWindowAttribute(_hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, dark ? 1 : 0, sizeof(int));
        DwmSetWindowAttribute(_hwnd, DWMWA_BORDER_COLOR, dark ? 0x00404040 : 0x00E0E0E0, sizeof(int));
    }

    void SetOutputsLinked(bool linked)
    {
        if (_updating)
            return;
        Motion.Pop(SyncIcon);
        Run(async () =>
        {
            await _sonar.SetOutputsLinkedAsync(linked, _mainChannel);
            await LoadAsync();
        });
    }

    ChannelRow AddRow(string name, string channel, string? route = null, string? routeLabel = null, bool isMic = false)
    {
        var row = new ChannelRow(name, routeLabel, isMic);
        row.VolumeChanged += volume => QueueVolume(channel, volume);
        row.MutedChanged += muted => Run(() => _sonar.SetMutedAsync(channel, muted));
        if (route is not null)
            row.DeviceChanged += id => Run(async () =>
            {
                await _sonar.SetOutputAsync(route, id);
                await LoadAsync();
            });
        _rows[channel] = row;
        return row;
    }

    public async void Toggle()
    {
        if (_opening)
            return;
        if (AppWindow.IsVisible)
        {
            Hide();
            return;
        }
        // Clicking the tray icon deactivates (and so hides) the flyout just before the click reaches us.
        if (Environment.TickCount64 - _hiddenAt < 300)
            return;
        _opening = true;

        GetCursorPos(out var cursor);
        _monitor = MonitorFromPoint(cursor, MONITOR_DEFAULTTONEAREST);

        // Filling the lists before showing stops the window growing on screen; the cap covers Sonar being down.
        var load = RunAsync(LoadAsync);
        await Task.WhenAny(load, Task.Delay(300));
        // Before the first show the height is unknown and then settles over several layout passes.
        bool firstLayout = Root.ActualHeight == 0;
        if (firstLayout)
            AppWindow.Move(new PointInt32(-32000, -32000));
        else
            PlaceWindow();
        Motion.PrepareEnter(Root);
        AppWindow.Show(true);
        Activate();
        SetForegroundWindow(_hwnd);
        Outputs.Focus(FocusState.Pointer);
        if (firstLayout)
            await WaitForStableHeightAsync();
        _opening = false;
        PlaceWindow();
        Motion.Enter(Root);
        KeepFramesFlowing(true);
        // A window appearing under a still pointer keeps WinUI's busy cursor until the mouse moves.
        GetCursorPos(out var pointer);
        SetCursorPos(pointer.X + 1, pointer.Y);
        SetCursorPos(pointer.X, pointer.Y);
        await load;
    }

    // WinUI's first show of a window is slow and shows a busy cursor, so it happens off-screen at startup.
    async Task WarmUpAsync()
    {
        _opening = true;
        try
        {
            await LoadAsync();
            AppWindow.Move(new PointInt32(-32000, -32000));
            AppWindow.Show(false);
            await WaitForStableHeightAsync();
            AppWindow.Hide();
        }
        finally
        {
            _opening = false;
        }
    }

    async Task WaitForStableHeightAsync()
    {
        double last = -1;
        for (int i = 0; i < 30 && (Root.ActualHeight == 0 || Root.ActualHeight != last); i++)
        {
            last = Root.ActualHeight;
            await Task.Delay(16);
        }
    }

    // On an otherwise idle desktop the GPU and CPU power down and hover frames land late; a running animation on
    // an invisible pixel keeps frames flowing while the flyout is open.
    void KeepFramesFlowing(bool on)
    {
        var visual = ElementCompositionPreview.GetElementVisual(FrameKeeper);
        if (!on)
        {
            visual.StopAnimation("Opacity");
            return;
        }
        var flicker = visual.Compositor.CreateScalarKeyFrameAnimation();
        flicker.InsertKeyFrame(0, 0.01f);
        flicker.InsertKeyFrame(1, 0.02f);
        flicker.Duration = TimeSpan.FromSeconds(1);
        flicker.IterationBehavior = AnimationIterationBehavior.Forever;
        flicker.Direction = AnimationDirection.Alternate;
        visual.StartAnimation("Opacity", flicker);
    }

    void Hide()
    {
        if (!AppWindow.IsVisible)
            return;
        KeepFramesFlowing(false);
        AppWindow.Hide();
        _hiddenAt = Environment.TickCount64;
    }

    void PlaceWindow()
    {
        if (_monitor == 0)
            return;
        var info = new MONITORINFO { cbSize = (uint)Marshal.SizeOf<MONITORINFO>() };
        GetMonitorInfoW(_monitor, ref info);
        GetDpiForMonitor(_monitor, MDT_EFFECTIVE_DPI, out uint dpi, out _);
        double scale = dpi / 96.0;
        double heightDips = Root.ActualHeight;

        int width = (int)(WidthDips * scale);
        int height = (int)Math.Ceiling(heightDips * scale);
        int margin = (int)(MarginDips * scale);
        AppWindow.MoveAndResize(new RectInt32(
            Strings.IsRightToLeft ? info.rcWork.Left + margin : info.rcWork.Right - width - margin,
            info.rcWork.Bottom - height - margin, width, height));
    }

    async Task LoadAsync()
    {
        var state = await _sonar.GetStateAsync();
        if (state.Mode != "classic")
            throw new InvalidOperationException(Strings.Get("ErrorStreamerMode"));

        _updating = true;
        var majority = s_mixer
            .Where(c => state.EnabledChannels.Contains(c.Channel))
            .GroupBy(c => state.Routes.GetValueOrDefault(c.Route))
            .MaxBy(g => g.Count());
        var mainOutput = majority?.Key;
        _mainChannel = majority?.First().Channel ?? _mainChannel;

        SyncOutputs.IsChecked = state.OutputsLinked;
        Outputs.Visibility = state.OutputsLinked ? Visibility.Visible : Visibility.Collapsed;
        PerChannelHint.Visibility = state.OutputsLinked ? Visibility.Collapsed : Visibility.Visible;
        ToolTipService.SetToolTip(SyncOutputs, state.OutputsLinked
            ? Strings.Get("SyncedTooltip")
            : Strings.Get("UnsyncedTooltip"));

        _rows[MasterChannel].SetVolume(state.Volumes[MasterChannel]);
        foreach (var (_, channel, route) in s_mixer)
        {
            _rows[channel].Visibility = state.EnabledChannels.Contains(channel) ? Visibility.Visible : Visibility.Collapsed;
            _rows[channel].SetVolume(state.Volumes[channel]);
            _rows[channel].SetRoute(state.Outputs, state.Routes.GetValueOrDefault(route),
                state.OutputsLinked ? mainOutput : null, canRoute: !state.OutputsLinked);
        }
        _rows[MicChannel].SetVolume(state.Volumes[MicChannel]);

        FillDevices(Outputs, state.Outputs, mainOutput);
        FillDevices(Inputs, state.Inputs, state.Routes.GetValueOrDefault(MicRoute));
        _updating = false;
        _state = state;
        ShowDefaultDevices();
    }

    void ShowDefaultDevices()
    {
        var (sound, mic) = DefaultDevices.AreSonar(_state!);
        DefaultDevicesBar.Message = Strings.Get(sound ? "DefaultDevicesMic" : mic ? "DefaultDevicesSound" : "DefaultDevicesBoth");
        DefaultDevicesBar.IsOpen = !(sound && mic);
    }

    void OnDefaultDeviceChanged()
    {
        if (AppWindow.IsVisible && _state is not null && !_confirmingFix)
            Run(LoadAsync);
    }

    async void UseSonarDevices()
    {
        if (_confirmingFix)
            return;
        _confirmingFix = true;
        try
        {
            _state = await _sonar.GetStateAsync();
            DefaultDevices.UseSonar(_state);
        }
        catch
        {
            DefaultDevicesBar.Message = Strings.Get("DefaultDevicesFailed");
            _confirmingFix = false;
            return;
        }

        DefaultDevicesBar.Severity = InfoBarSeverity.Success;
        DefaultDevicesBar.Message = Strings.Get("DefaultDevicesFixed");
        if (DefaultDevicesBar.ActionButton.FocusState != FocusState.Unfocused)
            (Outputs.Visibility == Visibility.Visible ? Outputs : Inputs).Focus(FocusState.Programmatic);
        DefaultDevicesBar.ActionButton.Visibility = Visibility.Collapsed;
        FrameworkElementAutomationPeer.CreatePeerForElement(DefaultDevicesBar).RaiseNotificationEvent(
            AutomationNotificationKind.ActionCompleted, AutomationNotificationProcessing.ImportantMostRecent,
            DefaultDevicesBar.Message, "DefaultDevicesFixed");
        await Task.Delay(1200);
        await Motion.CollapseAsync(DefaultDevicesHost, () => DefaultDevicesBar.IsOpen = false);
        DefaultDevicesBar.Severity = InfoBarSeverity.Warning;
        DefaultDevicesBar.ActionButton.Visibility = Visibility.Visible;
        _confirmingFix = false;
        await RunAsync(LoadAsync);
    }

    // Keeps existing items when the device set is unchanged, so selection moves with ListView's own
    // indicator animation instead of the list being rebuilt.
    static void FillDevices(ListView list, List<Device> devices, string? selectedId)
    {
        var items = list.Items.Cast<ListViewItem>().ToList();
        if (items.Select(i => (string)i.Tag).SequenceEqual(devices.Select(d => d.Id)))
        {
            list.SelectedItem = items.FirstOrDefault(i => (string)i.Tag == selectedId);
            return;
        }

        list.Items.Clear();
        foreach (var device in devices)
        {
            var item = new ListViewItem
            {
                Tag = device.Id,
                // ItemClick reports the content, not the ListViewItem, so the content carries the id too.
                Content = new StackPanel
                {
                    Tag = device.Id,
                    Orientation = Orientation.Horizontal,
                    Spacing = 12,
                    Children =
                    {
                        new FontIcon { Glyph = device.Glyph, FontSize = 16 },
                        new TextBlock { Text = device.Name, VerticalAlignment = VerticalAlignment.Center },
                    },
                },
            };
            AutomationProperties.SetName(item, device.Name);
            list.Items.Add(item);
            if (device.Id == selectedId)
                list.SelectedItem = item;
        }
    }

    async void QueueVolume(string channel, double volume)
    {
        // Slider drags fire far faster than Sonar needs; send only the latest value once the previous request completes.
        _pendingVolumes[channel] = volume;
        if (!_sendingVolumes.Add(channel))
            return;
        try
        {
            while (_pendingVolumes.Remove(channel, out double next))
            {
                await _sonar.SetVolumeAsync(channel, next);
                await RefreshOtherVolumesAsync(channel);
            }
            Error.IsOpen = false;
        }
        catch (Exception e)
        {
            ShowError(e);
        }
        finally
        {
            _sendingVolumes.Remove(channel);
        }
    }

    // Sonar couples the sliders itself: master scales every output channel, and raising a channel above master raises master.
    async Task RefreshOtherVolumesAsync(string changedChannel)
    {
        var volumes = await _sonar.GetVolumesAsync();
        foreach (var (channel, row) in _rows)
            if (channel != changedChannel && !_sendingVolumes.Contains(channel) && volumes.TryGetValue(channel, out var volume))
                row.SetVolume(volume);
    }

    public void ShowVolumes(Dictionary<string, ChannelVolume> volumes)
    {
        if (!AppWindow.IsVisible)
            return;
        foreach (var (channel, row) in _rows)
            if (!_sendingVolumes.Contains(channel) && volumes.TryGetValue(channel, out var volume))
                row.SetVolume(volume);
    }

    async void Run(Func<Task> action) => await RunAsync(action);

    async Task RunAsync(Func<Task> action)
    {
        try
        {
            await action();
            Error.IsOpen = false;
        }
        catch (Exception e)
        {
            ShowError(e);
        }
    }

    void ShowError(Exception e)
    {
        _updating = false;
        DefaultDevicesBar.IsOpen = false;
        Error.Message = e is InvalidOperationException ? e.Message : Strings.Get("ErrorUnreachable");
        Error.IsOpen = true;
    }
}
