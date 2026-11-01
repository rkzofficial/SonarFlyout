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
    readonly Action<int> _onWheel;
    readonly nint _hwnd;
    nint _icon;
    string _state = "3";
    string _tooltip = Strings.Get("TrayTooltip");
    bool _wheelRegistered;
    int _wheelRemainder;

    public TrayIcon(Action onSelect, Action<int, int> onContextMenu, Action<int> onWheel)
    {
        _onSelect = onSelect;
        _onContextMenu = onContextMenu;
        _onWheel = onWheel;
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
        if (_wheelRegistered)
            RegisterWheel(false);
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

    // The shell never forwards wheel messages to notification icons, so listen to raw mouse input,
    // but only while the pointer is over the icon: raw input reports every mouse movement system-wide.
    void RegisterWheel(bool register)
    {
        var device = new RAWINPUTDEVICE
        {
            usUsagePage = 1,
            usUsage = 2,
            dwFlags = register ? RIDEV_INPUTSINK : RIDEV_REMOVE,
            hwndTarget = register ? _hwnd : 0,
        };
        RegisterRawInputDevices(&device, 1, (uint)sizeof(RAWINPUTDEVICE));
        _wheelRegistered = register;
        _wheelRemainder = 0;
    }

    bool IsCursorOverIcon()
    {
        var id = new NOTIFYICONIDENTIFIER { cbSize = (uint)sizeof(NOTIFYICONIDENTIFIER), hWnd = _hwnd, uID = 1 };
        RECT rect;
        return Shell_NotifyIconGetRect(&id, &rect) >= 0 && GetCursorPos(out var cursor)
            && cursor.X >= rect.Left && cursor.X < rect.Right && cursor.Y >= rect.Top && cursor.Y < rect.Bottom;
    }

    void OnRawInput(nint handle)
    {
        if (!IsCursorOverIcon())
        {
            RegisterWheel(false);
            return;
        }
        RAWINPUT input;
        uint size = (uint)sizeof(RAWINPUT);
        if (GetRawInputData(handle, RID_INPUT, &input, &size, (uint)sizeof(RAWINPUTHEADER)) == unchecked((uint)-1)
            || input.header.dwType != RIM_TYPEMOUSE || (input.mouse.usButtonFlags & RI_MOUSE_WHEEL) == 0)
            return;
        _wheelRemainder += input.mouse.usButtonData;
        int notches = _wheelRemainder / WHEEL_DELTA;
        if (notches == 0)
            return;
        _wheelRemainder %= WHEEL_DELTA;
        _onWheel(notches);
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
                    case WM_MOUSEMOVE when !self._wheelRegistered:
                        self.RegisterWheel(true);
                        break;
                }
                return 0;
            }
            if (message == WM_INPUT && self._wheelRegistered)
                self.OnRawInput(lParam);
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
