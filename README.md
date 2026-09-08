# TGWatch for Windows

**A lightweight, tray-first DMR activity monitor for Windows, with live HamThings and BrandMeister feeds.**

TGWatch for Windows keeps an eye on DMR talkgroup activity without taking over your desktop. It lives quietly in the Windows notification area and shows a small, configurable popup only when there is something worth seeing.

It connects directly to **HamThings** and **BrandMeister**, normalizes both live feeds into a single view, and prioritizes active talkgroups so the information that matters is always at the top.

The application is intentionally simple: no dashboard to keep open, no browser window, no local server, no database and no external NuGet dependencies. Just one small Windows utility that can start with the system and stay out of the way.

## Highlights

- **Tray-first UX** — runs in the Windows notification area with no permanent main window
- **Live DMR activity** from HamThings and BrandMeister
- **Compact passive popup** that does not steal focus
- **Network colors at a glance** — BrandMeister in orange, HamThings in blue
- **Active TGs always first**, with numeric ordering inside each group
- **Configurable filtering** — all TGs, numeric prefix or an explicit TG list
- **Draggable popup** with adjustable transparency
- **Configurable retention** for recently ended transmissions
- Optional **callsign** and **node/repeater** display
- Optional **BrandMeister private-call filtering** based on feed metadata
- **Start with Windows** support without administrator privileges
- **Single self-contained EXE** available from GitHub Releases
- Built with **C# / .NET 8 / WinForms**
- **No external NuGet dependencies**

## How it behaves

TGWatch is designed to be informative without becoming distracting.

When DMR traffic appears, the popup shows only a small number of relevant talkgroups. Active transmissions always win and stay at the top; active TGs are ordered numerically, followed by recently ended TGs, also in numeric order. Once the configured retention period expires, ended activity disappears automatically.

The popup can be moved anywhere on screen and its opacity can be adjusted from the settings window.

A double-click on the tray icon opens the complete configuration interface. A right-click gives quick access to activity, pause, settings, startup and exit.

## Download

For normal use, download the latest **`TGWatch.exe`** from the GitHub Releases page.

The release build is a **single self-contained Windows x64 executable**, so the target PC does not need a separate .NET installation or supporting DLLs next to the program.

Each release also includes a `SHA256SUMS.txt` file for integrity verification.

## Requirements

### End users

- Windows 10 or Windows 11, x64
- Internet access to reach the HamThings and/or BrandMeister live feeds

### Development

- Windows 10 or Windows 11
- .NET 8 SDK
- VS Code, Visual Studio or another compatible editor

Check the installed SDK with:

```powershell
dotnet --version
```

## Run from source

```powershell
dotnet run
```

The app starts directly in the Windows notification area.

- Double-click the tray icon to open settings
- Right-click the tray icon for the quick menu
- Use **Exit** to close the application completely

## Build

```powershell
dotnet build -c Release
```

## Publish a single self-contained EXE

```powershell
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true
```

The executable is produced under:

```text
bin\Release\net8.0-windows\win-x64\publish\
```

You can also use:

```powershell
.\publish-portable.ps1
```

## Configuration

User settings are stored in:

```text
%LOCALAPPDATA%\TGWatch\settings.json
```

The **Start with Windows** option uses the current user's startup registry key:

```text
HKCU\Software\Microsoft\Windows\CurrentVersion\Run
```

No administrator privileges are required.

## Live feed endpoints

TGWatch for Windows connects directly to the same public live sources used by the original TGWatch project:

- **HamThings** — `wss://www.hamthings.it/socket.io/?EIO=4&transport=websocket`
- **BrandMeister** — `wss://api.brandmeister.network/lh/?EIO=4&transport=websocket`

There is no intermediate TGWatch server. Feed events are parsed and normalized locally by the application.

## Privacy and network behavior

TGWatch does not require an account and does not run its own telemetry or analytics service. The application connects to the selected external DMR feeds to receive live activity data and stores its own configuration locally on the PC.

HamThings and BrandMeister are independent third-party services; their availability and data formats may change independently of TGWatch.

## Automated builds and releases

GitHub Actions is used to:

- compile the project on Windows for continuous integration;
- publish a self-contained `win-x64` single-file build;
- generate a SHA-256 checksum;
- create tagged GitHub Releases automatically.

## License

TGWatch for Windows is released under the **MIT License**. See [LICENSE](LICENSE).

HamThings and BrandMeister are independent services. Their names, trademarks, infrastructure and data remain the property of their respective owners. See [THIRD_PARTY.md](THIRD_PARTY.md).

## Related project

TGWatch for Windows is the desktop companion to the original **TGWatch** firmware project, sharing the same goal: make live DMR talkgroup activity easy to follow at a glance.

Original TGWatch project: https://github.com/netrace/TGWatch
