# TGWatch for Windows

A lightweight Windows system-tray monitor for live DMR talkgroup activity from **HamThings** and **BrandMeister**.

TGWatch for Windows is designed to stay out of the way: it runs from the notification area, shows only a few active/recent talkgroups in a compact popup, and keeps configuration in a single simple window.

## Features

- Native Windows tray application
- Live HamThings and BrandMeister activity
- Compact always-on-top popup
- BrandMeister shown in orange, HamThings in blue
- Active talkgroups always first
- Numeric TG ordering within active and inactive groups
- Configurable maximum number of visible rows
- TG filters: all, prefix, or explicit list
- Optional BrandMeister private-call filtering based on feed metadata
- Draggable popup
- Configurable popup opacity
- Configurable retention time for ended transmissions
- Optional callsign and node/repeater display
- Start automatically with Windows
- No external NuGet dependencies

## Requirements

For development:

- Windows 10 or Windows 11
- .NET 8 SDK
- VS Code or any compatible editor

Check your SDK:

```powershell
dotnet --version
```

## Run from source

```powershell
dotnet run
```

The app starts in the Windows notification area.

- Double-click the tray icon to open settings
- Right-click the tray icon for the quick menu
- Use **Exit** to close the app completely

## Build

```powershell
dotnet build -c Release
```

## Publish a single self-contained EXE

```powershell
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true
```

The executable will be created under:

```text
bin\Release\net8.0-windows\win-x64\publish\
```

You can also run:

```powershell
.\publish-portable.ps1
```

## Configuration

Settings are stored in:

```text
%LOCALAPPDATA%\TGWatch\settings.json
```

The **Start with Windows** option writes only to the current user's startup registry key:

```text
HKCU\Software\Microsoft\Windows\CurrentVersion\Run
```

No administrator privileges are required.

## Feed endpoints

TGWatch for Windows connects directly to the public live feeds used by the original TGWatch project:

- HamThings: `wss://www.hamthings.it/socket.io/?EIO=4&transport=websocket`
- BrandMeister: `wss://api.brandmeister.network/lh/?EIO=4&transport=websocket`

No intermediate TGWatch server is required.

## License

TGWatch for Windows is released under the **MIT License**. See [LICENSE](LICENSE).

HamThings and BrandMeister are independent services. Their names, trademarks, infrastructure and data remain the property of their respective owners. See [THIRD_PARTY.md](THIRD_PARTY.md).

## Related project

This desktop application is based on the same monitoring concept and feed logic as the original **TGWatch** firmware project:

https://github.com/netrace/TGWatch
