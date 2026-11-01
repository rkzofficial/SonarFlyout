using System.Globalization;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using static SonarFlyout.Native;

namespace SonarFlyout;

sealed unsafe class TrayIcon : IDisposable
{
    const string WindowClass = "SonarFlyout.Tray";
    const uint CallbackMessage = WM_APP + 1;

    static readonly uint s_taskbarCreated = RegisterWindowMessageW("TaskbarCreated");
    static TrayIcon? s_instance;

    readonly Action _onSelect;
    readonly Action<int, int> _onContextMenu;
    readonly nint _hwnd;
    nint _icon;
    string _state = "3";
    string _tooltip = Strings.Get("TrayTooltip");

    public TrayIcon(Action onSelect, Action<int, int> onContextMenu)
    {
        _onSelect = onSelect;
        _onContextMenu = onContextMenu;
        s_instance = this;

        var instance = GetModuleHandleW(null);
        fixed (char* className = WindowClass)
        {
            var wc = new WNDCLASSEXW
            {
                cbSize = (uint)sizeof(WNDCLASSEXW),
                lpfnWndProc = &WndProc,
                hInstance = instance,
                lpszClassName = className,
            };
            RegisterClassExW(&wc);
        }
        // A hidden top-level window rather than HWND_MESSAGE: message-only windows miss the TaskbarCreated broadcast.
        _hwnd = CreateWindowExW(0, WindowClass, null, 0, 0, 0, 0, 0, 0, 0, instance, 0);
        Add();
    }

    public void Dispose()
    {
        var data = NewData();
        Shell_NotifyIconW(NIM_DELETE, &data);
        DestroyIcon(_icon);
        DestroyWindow(_hwnd);
    }

    NOTIFYICONDATAW NewData() => new() { cbSize = (uint)sizeof(NOTIFYICONDATAW), hWnd = _hwnd, uID = 1 };

    void Add()
    {
        LoadIcon();
        var data = NewData();
        data.uFlags = NIF_MESSAGE | NIF_ICON | NIF_TIP | NIF_SHOWTIP;
        data.uCallbackMessage = CallbackMessage;
        data.hIcon = _icon;
        _tooltip.AsSpan().CopyTo(new Span<char>(data.szTip, 128));
        Shell_NotifyIconW(NIM_ADD, &data);
        data.uVersion = NOTIFYICON_VERSION_4;
        Shell_NotifyIconW(NIM_SETVERSION, &data);
    }

    public void SetVolume(ChannelVolume master)
    {
        int percent = (int)Math.Round(master.Volume * 100);
        var state = master.Muted ? "Muted" : percent == 0 ? "0" : percent < 34 ? "1" : percent < 67 ? "2" : "3";
        var tooltip = master.Muted
            ? Strings.Get("TrayMuted")
            : Strings.Format("TrayVolume", (percent / 100.0).ToString("P0", CultureInfo.CurrentCulture));
        if (state == _state && tooltip == _tooltip)
            return;
        _state = state;
        _tooltip = tooltip;
        UpdateIcon();
    }

    void UpdateIcon()
    {
        var old = _icon;
        LoadIcon();
        var data = NewData();
        data.uFlags = NIF_ICON | NIF_TIP | NIF_SHOWTIP;
        data.hIcon = _icon;
        _tooltip.AsSpan().CopyTo(new Span<char>(data.szTip, 128));
        Shell_NotifyIconW(NIM_MODIFY, &data);
        DestroyIcon(old);
    }

    void LoadIcon()
    {
        var file = $"{(IsTaskbarLight() ? "TrayLight" : "TrayDark")}{_state}.ico";
        int size = GetSystemMetricsForDpi(SM_CXSMICON, GetDpiForSystem());
        _icon = LoadImageW(0, Path.Combine(AppContext.BaseDirectory, "Assets", file), IMAGE_ICON, size, size, LR_LOADFROMFILE);
    }

    static bool IsTaskbarLight() =>
        Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
            "SystemUsesLightTheme", 0) is 1;

    [UnmanagedCallersOnly]
    static nint WndProc(nint hwnd, uint message, nint wParam, nint lParam)
    {
        var self = s_instance;
        if (self is not null)
        {
            if (message == CallbackMessage)
            {
                switch ((uint)(lParam & 0xFFFF))
                {
                    case NIN_SELECT:
                    case NIN_KEYSELECT:
                        self._onSelect();
                        break;
                    case WM_CONTEXTMENU:
                        self._onContextMenu((short)(wParam & 0xFFFF), (short)((wParam >> 16) & 0xFFFF));
                        break;
                }
                return 0;
            }
            if (message == s_taskbarCreated)
            {
                self.Add();
                return 0;
            }
            if (message == WM_SETTINGCHANGE && lParam != 0 && new string((char*)lParam) == "ImmersiveColorSet")
                self.UpdateIcon();
        }
        return DefWindowProcW(hwnd, message, wParam, lParam);
    }
}
