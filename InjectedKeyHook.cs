using System.ComponentModel;
using System.Runtime.InteropServices;

namespace ScreenFix;

// Low-level keyboard hook that swallows software-injected keys (such as those
// from a VNC server) and hands them to a callback as scan codes.
sealed class InjectedKeyHook : IDisposable
{
    public delegate void InjectedKeyCallback(ushort scanCode, bool extended, bool keyUp);

    const int WH_KEYBOARD_LL = 13;
    const uint LLKHF_EXTENDED = 0x01;
    const uint LLKHF_INJECTED = 0x10;
    const uint LLKHF_UP = 0x80;
    const uint MAPVK_VK_TO_VSC_EX = 4;

    [StructLayout(LayoutKind.Sequential)]
    struct KbdLlHookStruct
    {
        public uint VkCode;
        public uint ScanCode;
        public uint Flags;
        public uint Time;
        public UIntPtr ExtraInfo;
    }

    delegate IntPtr LowLevelKeyboardProc(int code, IntPtr wParam, ref KbdLlHookStruct info);

    [DllImport("user32.dll", SetLastError = true)]
    static extern IntPtr SetWindowsHookExW(int hookId, LowLevelKeyboardProc proc, IntPtr module, uint threadId);

    [DllImport("user32.dll")]
    static extern bool UnhookWindowsHookEx(IntPtr hook);

    [DllImport("user32.dll")]
    static extern IntPtr CallNextHookEx(IntPtr hook, int code, IntPtr wParam, ref KbdLlHookStruct info);

    [DllImport("user32.dll")]
    static extern uint MapVirtualKeyW(uint code, uint mapType);

    [DllImport("kernel32.dll")]
    static extern IntPtr GetModuleHandleW(string? moduleName);

    readonly InjectedKeyCallback _onInjectedKey;
    readonly LowLevelKeyboardProc _proc;
    readonly IntPtr _hook;

    public InjectedKeyHook(InjectedKeyCallback onInjectedKey)
    {
        _onInjectedKey = onInjectedKey;
        _proc = HookProc;
        _hook = SetWindowsHookExW(WH_KEYBOARD_LL, _proc, GetModuleHandleW(null), 0);
        if (_hook == IntPtr.Zero)
            throw new Win32Exception(Marshal.GetLastWin32Error());
    }

    IntPtr HookProc(int code, IntPtr wParam, ref KbdLlHookStruct info)
    {
        if (code < 0 || (info.Flags & LLKHF_INJECTED) == 0 || info.ExtraInfo == ScanCodeSender.Signature)
            return CallNextHookEx(_hook, code, wParam, ref info);

        var (scanCode, extended) = ResolveScanCode(info);
        if (scanCode == 0)
            return CallNextHookEx(_hook, code, wParam, ref info);

        _onInjectedKey(scanCode, extended, (info.Flags & LLKHF_UP) != 0);
        return 1;
    }

    // VNC servers often inject only the virtual-key code; derive the scan code when missing.
    static (ushort ScanCode, bool Extended) ResolveScanCode(KbdLlHookStruct info)
    {
        if (info.ScanCode != 0)
            return ((ushort)info.ScanCode, (info.Flags & LLKHF_EXTENDED) != 0);

        uint mapped = MapVirtualKeyW(info.VkCode, MAPVK_VK_TO_VSC_EX);
        bool extended = (mapped & 0xFF00) == 0xE000 || (info.Flags & LLKHF_EXTENDED) != 0;
        return ((ushort)(mapped & 0xFF), extended);
    }

    public void Dispose() => UnhookWindowsHookEx(_hook);
}
