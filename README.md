# VncKeyBridge

Some Windows applications ignore keys injected by remote-control software (VNC, RealVNC) and only accept a physical keyboard. VncKeyBridge catches injected keystrokes with a low-level keyboard hook, blocks them, and re-sends them as scan codes through the [Interception](https://github.com/oblitum/Interception) kernel driver, so they reach applications as hardware input from a chosen keyboard.

The app talks to the driver directly (no `interception.dll`) and embeds the driver's signed installer, so the published executable is the only file needed.

## Build

Requires Windows x64 and the .NET 10 SDK.

```
dotnet publish -c Release --self-contained -p:PublishSingleFile=true -o publish
```

## Usage

Run as administrator on the machine running the VNC server.

```
VncKeyBridge.exe --install              # install the Interception driver, then reboot
VncKeyBridge.exe --list                 # list keyboards and hardware IDs
VncKeyBridge.exe                        # send through the first keyboard
VncKeyBridge.exe --device VID_1234      # send through the keyboard whose hardware ID contains the text
VncKeyBridge.exe --uninstall            # remove the driver, then reboot
VncKeyBridge.exe --scancode             # no driver or admin: re-send as SendInput scan codes
```

`--scancode` fixes applications that only reject input without scan codes. Windows still flags it as injected, so applications that reject any injected input need the driver mode.

Keys from the physical keyboard pass through untouched. Any injected input (AutoHotkey, on-screen keyboard) is also converted.

## Limitations

- Vendor-specific keys that have no standard scan code cannot be produced.
- Software with anti-cheat may block the Interception driver.

## Third-party

`third-party/interception/install-interception.exe` is the unmodified signed installer from Interception v1.0.1 by Francisco Lopes, distributed under the LGPL 3.0 for non-commercial use (see `LICENSE-LGPL-3.0.txt`). Commercial use requires a license from the author.
