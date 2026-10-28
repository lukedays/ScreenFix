using System.Runtime.InteropServices;
using VncKeyBridge;

using var hook = new InjectedKeyHook(ScanCodeSender.Send);
Console.WriteLine("VncKeyBridge running: injected keys are re-sent as scan codes. Ctrl+C to stop.");
MessageLoop.Run();

static class MessageLoop
{
    [StructLayout(LayoutKind.Sequential)]
    struct Msg
    {
        public IntPtr Hwnd;
        public uint Message;
        public IntPtr WParam;
        public IntPtr LParam;
        public uint Time;
        public int X;
        public int Y;
    }

    [DllImport("user32.dll")]
    static extern int GetMessageW(out Msg msg, IntPtr hwnd, uint filterMin, uint filterMax);

    // Low-level hooks are only called while the installing thread pumps messages.
    public static void Run()
    {
        while (GetMessageW(out _, IntPtr.Zero, 0, 0) > 0) { }
    }
}
