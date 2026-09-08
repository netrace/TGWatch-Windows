# TGWatch for Windows v0.6

First public Windows release of TGWatch.

## Highlights

- Lightweight tray-first UX
- Direct HamThings and BrandMeister live feeds
- Compact, draggable, semi-transparent activity popup
- Distinct network colors: orange for BrandMeister, blue for HamThings
- Active talkgroups always appear first, with ascending numeric TG order
- Configurable TG filtering and popup behavior
- Single-window settings UI
- Optional automatic startup with Windows
- No external NuGet dependencies

## Build target

- .NET 8
- Windows x64

For a single self-contained executable:

```powershell
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true
```
