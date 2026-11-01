using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;

namespace SonarFlyout;

public sealed partial class ChannelRow : UserControl
{
    readonly bool _isMic;
    readonly string _routeGroup = Guid.NewGuid().ToString();
    bool _muted;
    bool _updating;

    public event Action<double>? VolumeChanged;
    public event Action<bool>? MutedChanged;
    public event Action<string>? DeviceChanged;

    public ChannelRow(string name, string? routeLabel = null, bool isMic = false)
    {
        InitializeComponent();
        _isMic = isMic;
        AutomationProperties.SetName(VolumeSlider, Strings.Format("ChannelVolumeName", name));
        AutomationProperties.SetName(MuteButton, Strings.Format("ChannelMuteName", name));
        if (routeLabel is not null)
        {
            Label.Text = routeLabel;
            RouteButton.Visibility = Visibility.Visible;
            AutomationProperties.SetName(RouteButton, Strings.Format("ChannelOutputName", name));
        }

        VolumeSlider.ValueChanged += (_, e) =>
        {
            ValueText.Text = ((int)e.NewValue).ToString();
            UpdateIcon();
            if (!_updating)
                VolumeChanged?.Invoke(e.NewValue / 100);
        };
        MuteButton.Click += (_, _) =>
        {
            _muted = !_muted;
            UpdateIcon();
            Motion.Pop(MuteIcon);
            MutedChanged?.Invoke(_muted);
        };
        RouteMenu.Opened += (_, _) => Motion.Rotate(Chevron, 180);
        RouteMenu.Closed += (_, _) => Motion.Rotate(Chevron, 0);
        Motion.FadeInWhenShown(RouteText);
    }

    public void SetVolume(ChannelVolume volume)
    {
        _updating = true;
        _muted = volume.Muted;
        VolumeSlider.Value = Math.Round(volume.Volume * 100);
        _updating = false;
        UpdateIcon();
    }

    public void SetRoute(IEnumerable<Device> devices, string? routedId, string? mainOutputId, bool canRoute)
    {
        RouteButton.IsHitTestVisible = canRoute;
        RouteButton.IsTabStop = canRoute;
        Chevron.Visibility = canRoute ? Visibility.Visible : Visibility.Collapsed;

        RouteMenu.Items.Clear();
        Device? routed = null;
        foreach (var device in devices)
        {
            var item = new RadioMenuFlyoutItem
            {
                Text = device.Name,
                GroupName = _routeGroup,
                IsChecked = device.Id == routedId,
                Icon = new FontIcon { Glyph = device.Glyph },
            };
            item.Click += (_, _) => DeviceChanged?.Invoke(device.Id);
            RouteMenu.Items.Add(item);
            if (device.Id == routedId)
                routed = device;
        }

        bool differsFromMain = routedId != mainOutputId;
        RouteText.Text = routed?.Name ?? Strings.Get("UnavailableDevice");
        RouteText.Visibility = differsFromMain ? Visibility.Visible : Visibility.Collapsed;
    }

    void UpdateIcon()
    {
        double volume = VolumeSlider.Value;
        MuteIcon.Glyph = (_isMic, _muted) switch
        {
            (true, true) => "\uF781",
            (true, false) => "\uE720",
            (false, true) => "\uE74F",
            _ when volume == 0 => "\uE992",
            _ when volume < 34 => "\uE993",
            _ when volume < 67 => "\uE994",
            _ => "\uE995",
        };
        ToolTipService.SetToolTip(MuteButton, Strings.Get(_muted ? "Unmute" : "Mute"));
    }
}
