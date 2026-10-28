using System.Runtime.InteropServices;

namespace VncKeyBridge;

// Re-sends keystrokes with SendInput in scan-code mode, filling in the scan code that
// VNC servers usually leave empty.
static class ScanCodeSender
{
    // Tags our own input so the hook lets it through instead of capturing it again.
    public static readonly UIntPtr Signature = 0x564B_4252;

    const uint INPUT_KEYBOARD = 1;
    const uint KEYEVENTF_EXTENDEDKEY = 0x01;
    const uint KEYEVENTF_KEYUP = 0x02;
    const uint KEYEVENTF_SCANCODE = 0x08;

    [StructLayout(LayoutKind.Sequential)]
    struct KeyboardInput
    {
        public ushort VirtualKey;
        public ushort ScanCode;
        public uint Flags;
        public uint Time;
        public UIntPtr ExtraInfo;
    }

    [StructLayout(LayoutKind.Explicit, Size = 40)]
    struct Input
    {
        [FieldOffset(0)] public uint Type;
        [FieldOffset(8)] public KeyboardInput Keyboard;
    }

    [DllImport("user32.dll", SetLastError = true)]
    static extern uint SendInput(uint count, Input[] inputs, int size);

    public static void Send(ushort scanCode, bool extended, bool keyUp)
    {
        var input = new Input
        {
            Type = INPUT_KEYBOARD,
            Keyboard = new KeyboardInput
            {
                ScanCode = scanCode,
                Flags = KEYEVENTF_SCANCODE | (extended ? KEYEVENTF_EXTENDEDKEY : 0) | (keyUp ? KEYEVENTF_KEYUP : 0),
                ExtraInfo = Signature,
            },
        };
        SendInput(1, [input], Marshal.SizeOf<Input>());
    }
}
