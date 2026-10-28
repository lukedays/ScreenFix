# VncKeyBridge

Some Windows applications ignore keys injected by remote-control software (VNC, RealVNC) and only accept a physical keyboard. VncKeyBridge catches injected keystrokes with a low-level keyboard hook, blocks them, and re-sends them as scan codes through the [Interception](https://github.com/oblitum/Interception) kernel driver, so they reach applications as hardware input from a chosen keyboard.

## Requirements

- Windows x64, .NET 10 SDK to build.
- Interception driver: download the [latest release](https://github.com/oblitum/Interception/releases), run `install-interception.exe /install` as administrator and reboot.
- Copy `library/x64/interception.dll` from that release into the project folder before building.

## Build

```
dotnet publish -c Release --self-contained -p:PublishSingleFile=true -o publish
```

## Usage

Run as administrator on the machine running the VNC server.

```
VncKeyBridge.exe --list                 # list keyboards and hardware IDs
VncKeyBridge.exe                        # send through the first keyboard
VncKeyBridge.exe --device VID_1234      # send through the keyboard whose hardware ID contains the text
```

Keys from the physical keyboard pass through untouched. Any injected input (AutoHotkey, on-screen keyboard) is also converted.

## Limitations

- Vendor-specific keys that have no standard scan code cannot be produced.
- Software with anti-cheat may block the Interception driver.
- Interception is free for non-commercial use; check its license for other uses.
