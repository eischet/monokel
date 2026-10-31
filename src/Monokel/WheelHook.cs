using System.ComponentModel;
using System.Runtime.InteropServices;
using static Monokel.NativeMethods;

namespace Monokel;

/// <summary>
/// Globale Low-Level-Hooks: fängt Zoomtaste + Mausrad ab und verschluckt es, damit die Anwendung
/// unter dem Cursor nicht selbst reagiert. Muss auf einem Thread mit Message-Loop installiert werden.
/// </summary>
internal sealed class WheelHook : IDisposable
{
    private readonly Action<int> _onWheel;
    // Referenzen halten, sonst sammelt der GC die Delegates ein
    private readonly LowLevelHookProc _mouseProc;
    private readonly LowLevelHookProc _keyboardProc;
    private IntPtr _mouseHook;
    private IntPtr _keyboardHook;

    private bool _winDown;
    private bool _startMenuMasked;

    /// <param name="onWheel">Wird mit dem Rad-Delta aufgerufen (120 = eine Raste, Touchpads liefern kleinere Werte). Muss sofort zurückkehren.</param>
    public WheelHook(ZoomModifier modifier, Action<int> onWheel)
    {
        Modifier = modifier;
        _onWheel = onWheel;
        _mouseProc = MouseProc;
        _keyboardProc = KeyboardProc;
        _mouseHook = Install(WH_MOUSE_LL, _mouseProc);
        _keyboardHook = Install(WH_KEYBOARD_LL, _keyboardProc);
    }

    public ZoomModifier Modifier { get; set; }

    private static IntPtr Install(int id, LowLevelHookProc proc)
    {
        var hook = SetWindowsHookEx(id, proc, GetModuleHandle(null), 0);
        if (hook == IntPtr.Zero)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "SetWindowsHookEx fehlgeschlagen");
        }
        return hook;
    }

    private static bool IsDown(int vk) => (GetAsyncKeyState(vk) & 0x8000) != 0;

    private bool ModifierDown() => Modifier switch
    {
        ZoomModifier.Windows => IsDown(VK_LWIN) || IsDown(VK_RWIN),
        _ => IsDown(VK_CONTROL),
    };

    private IntPtr MouseProc(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && wParam == WM_MOUSEWHEEL && ModifierDown())
        {
            if (Modifier == ZoomModifier.Windows)
            {
                MaskStartMenu();
            }
            var info = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
            _onWheel((short)(info.mouseData >> 16));
            return 1;
        }
        return CallNextHookEx(_mouseHook, nCode, wParam, lParam);
    }

    /// <summary>Merkt sich, wann die Windows-Taste neu gedrückt wird; Auto-Repeat zählt nicht als neuer Druck.</summary>
    private IntPtr KeyboardProc(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            var info = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
            if ((info.flags & LLKHF_INJECTED) == 0 && info.vkCode is VK_LWIN or VK_RWIN)
            {
                var msg = (int)wParam;
                if (msg is WM_KEYDOWN or WM_SYSKEYDOWN && !_winDown)
                {
                    _winDown = true;
                    _startMenuMasked = false;
                }
                else if (msg is WM_KEYUP or WM_SYSKEYUP)
                {
                    _winDown = false;
                }
            }
        }
        return CallNextHookEx(_keyboardHook, nCode, wParam, lParam);
    }

    /// <summary>
    /// Lässt man die Windows-Taste allein los, öffnet sich das Startmenü. Ein eingeschobener Druck auf eine
    /// unbelegte Taste, solange Windows noch gedrückt ist, verhindert das; einmal pro Druck auf die Windows-Taste genügt.
    /// </summary>
    private void MaskStartMenu()
    {
        if (_startMenuMasked)
        {
            return;
        }
        _startMenuMasked = true;
        var inputs = new INPUT[]
        {
            new() { type = INPUT_KEYBOARD, u = new InputUnion { ki = new KEYBDINPUT { wVk = VK_MASK } } },
            new() { type = INPUT_KEYBOARD, u = new InputUnion { ki = new KEYBDINPUT { wVk = VK_MASK, dwFlags = KEYEVENTF_KEYUP } } },
        };
        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
    }

    public void Dispose()
    {
        if (_mouseHook != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_mouseHook);
            _mouseHook = IntPtr.Zero;
        }
        if (_keyboardHook != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_keyboardHook);
            _keyboardHook = IntPtr.Zero;
        }
    }
}
