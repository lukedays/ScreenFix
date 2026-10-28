# VncKeyBridge

Some Windows applications ignore keys typed through remote-control software (VNC, RealVNC), because the VNC server injects them with only a virtual-key code and no scan code. VncKeyBridge catches those injected keystrokes with a low-level keyboard hook, blocks them, and re-sends them with `SendInput` in scan-code mode, which those applications accept.

It needs no driver and no administrator rights. Keys from the physical keyboard pass through untouched.

## Build

Requires Windows x64 and the .NET 10 SDK.

```
dotnet publish -c Release --self-contained -p:PublishSingleFile=true -o publish
```

## Usage

Run `VncKeyBridge.exe` on the machine running the VNC server and keep it open while connected. Press Ctrl+C to stop.

## Limitations

- Windows still flags the re-sent keys as injected, so applications that reject any injected input will keep ignoring them.
- Any other injected input (AutoHotkey, on-screen keyboard) is also converted.
