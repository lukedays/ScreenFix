using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace VncKeyBridge;

// Sends keystrokes straight to the Interception kernel filter driver, so they reach
// applications as if typed on a physical keyboard.
sealed class InterceptionKeyboard : IDisposable
{
    const int MaxKeyboards = 10;
    const ushort KeyUp = 0x01;
    const ushort KeyE0 = 0x02;

    // CTL_CODE(FILE_DEVICE_UNKNOWN, function, METHOD_BUFFERED, FILE_ANY_ACCESS)
    const uint IoctlWrite = 0x22_0000 | (0x820 << 2);
    const uint IoctlGetHardwareId = 0x22_0000 | (0x880 << 2);

    const uint GENERIC_READ = 0x8000_0000;
    const uint OPEN_EXISTING = 3;

    [StructLayout(LayoutKind.Sequential)]
    struct KeyboardInputData
    {
        public ushort UnitId;
        public ushort MakeCode;
        public ushort Flags;
        public ushort Reserved;
        public uint ExtraInformation;
    }

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    static extern SafeFileHandle CreateFileW(string name, uint access, uint share, IntPtr security, uint disposition, uint flags, IntPtr template);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool DeviceIoControl(SafeFileHandle device, uint code, ref KeyboardInputData input, int inputSize, IntPtr output, int outputSize, out int returned, IntPtr overlapped);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool DeviceIoControl(SafeFileHandle device, uint code, IntPtr input, int inputSize, byte[] output, int outputSize, out int returned, IntPtr overlapped);

    readonly SafeFileHandle[] _devices;
    SafeFileHandle? _selected;

    public InterceptionKeyboard()
    {
        _devices = Enumerable.Range(0, MaxKeyboards).Select(OpenDevice).ToArray();
    }

    static SafeFileHandle OpenDevice(int index)
    {
        var handle = CreateFileW($@"\\.\interception{index:D2}", GENERIC_READ, 0, IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero);
        if (handle.IsInvalid)
            throw new InvalidOperationException("Interception driver not found. Run with --install and reboot.", new Win32Exception());
        return handle;
    }

    public IReadOnlyList<(int Device, string HardwareId)> ListKeyboards()
    {
        var keyboards = new List<(int, string)>();
        var buffer = new byte[1024];
        for (int index = 0; index < _devices.Length; index++)
        {
            DeviceIoControl(_devices[index], IoctlGetHardwareId, IntPtr.Zero, 0, buffer, buffer.Length, out int length, IntPtr.Zero);
            if (length > 0)
                keyboards.Add((index + 1, Encoding.Unicode.GetString(buffer, 0, length).Split('\0')[0]));
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
        _selected = _devices[match.Device - 1];
        return match.HardwareId;
    }

    public void Send(ushort scanCode, bool extended, bool keyUp)
    {
        if (_selected is null)
            return;
        var stroke = new KeyboardInputData
        {
            MakeCode = scanCode,
            Flags = (ushort)((keyUp ? KeyUp : 0) | (extended ? KeyE0 : 0)),
        };
        DeviceIoControl(_selected, IoctlWrite, ref stroke, Marshal.SizeOf<KeyboardInputData>(), IntPtr.Zero, 0, out _, IntPtr.Zero);
    }

    public void Dispose()
    {
        foreach (var device in _devices)
            device.Dispose();
    }
}
