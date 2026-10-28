using System.Runtime.InteropServices;
using System.Text;

namespace VncKeyBridge;

// Sends keystrokes through the Interception kernel filter driver, so they reach
// applications as if typed on a physical keyboard.
sealed class InterceptionKeyboard : IDisposable
{
    const int MaxKeyboards = 10;
    const ushort KeyUp = 0x01;
    const ushort KeyE0 = 0x02;

    [StructLayout(LayoutKind.Sequential)]
    struct KeyStroke
    {
        public ushort Code;
        public ushort State;
        public uint Information;
    }

    [DllImport("interception.dll", CallingConvention = CallingConvention.Cdecl)]
    static extern IntPtr interception_create_context();

    [DllImport("interception.dll", CallingConvention = CallingConvention.Cdecl)]
    static extern void interception_destroy_context(IntPtr context);

    [DllImport("interception.dll", CallingConvention = CallingConvention.Cdecl)]
    static extern int interception_send(IntPtr context, int device, ref KeyStroke stroke, uint strokeCount);

    [DllImport("interception.dll", CallingConvention = CallingConvention.Cdecl)]
    static extern uint interception_get_hardware_id(IntPtr context, int device, byte[] buffer, uint bufferSize);

    readonly IntPtr _context;
    int _device;

    public InterceptionKeyboard()
    {
        _context = interception_create_context();
        if (_context == IntPtr.Zero)
            throw new InvalidOperationException("Interception driver not found. Install it and reboot.");
    }

    public IReadOnlyList<(int Device, string HardwareId)> ListKeyboards()
    {
        var keyboards = new List<(int, string)>();
        var buffer = new byte[1024];
        for (int device = 1; device <= MaxKeyboards; device++)
        {
            uint length = interception_get_hardware_id(_context, device, buffer, (uint)buffer.Length);
            if (length > 0)
                keyboards.Add((device, Encoding.Unicode.GetString(buffer, 0, (int)length).Split('\0')[0]));
        }
        return keyboards;
    }

    // Picks the keyboard whose hardware ID contains the filter, or the first one when no filter is given.
    public string SelectKeyboard(string? hardwareIdFilter)
    {
        var match = ListKeyboards().FirstOrDefault(k =>
            hardwareIdFilter is null || k.HardwareId.Contains(hardwareIdFilter, StringComparison.OrdinalIgnoreCase));
        if (match.Device == 0)
            throw new InvalidOperationException($"No keyboard matches '{hardwareIdFilter}'. Run with --list.");
        _device = match.Device;
        return match.HardwareId;
    }

    public void Send(ushort scanCode, bool extended, bool keyUp)
    {
        var stroke = new KeyStroke
        {
            Code = scanCode,
            State = (ushort)((keyUp ? KeyUp : 0) | (extended ? KeyE0 : 0)),
        };
        interception_send(_context, _device, ref stroke, 1);
    }

    public void Dispose() => interception_destroy_context(_context);
}
