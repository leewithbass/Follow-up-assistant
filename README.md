# FloatBrowser

FloatBrowser is a lightweight floating browser for Windows desktop scenarios, built with `C# + .NET 8 + WPF + WebView2`.

It is designed for quick side browsing while gaming, streaming, multitasking, or monitoring web content.

## Tech Stack

- `C#`
- `.NET 8`
- `WPF`
- `Microsoft.Web.WebView2`

## Requirements

- Windows 10/11 x64
- `.NET 8 SDK`
- WebView2 Runtime

## Current Features

- Single-window browser with WebView2
- Extended system menu actions:
  - Open URL
  - Home
  - Back / Forward
  - Refresh / Stop
  - Add bookmark / Bookmarks list
  - Toggle topmost
  - Opacity quick set (100/90/80/70)
  - Settings
- Global hotkeys (customizable)
- Local JSON persistence for settings and bookmarks

## Home Page Behavior

- Default home URL is `app://bookmarks`
- On startup, the app opens the bookmarks home page
- Bookmarks home page supports:
  - Delete mode
  - Select all
  - Delete selected

## Bookmark Management

- Add bookmark from current page
- Open bookmark
- Delete bookmark
- Multi-select delete in bookmark window

## Data Location

Runtime data is stored under:

`%LocalAppData%\FloatBrowser`

Main files:

- `settings.json`
- `bookmarks.json`
- `floatbrowser.log`
- `WebView2UserData/`

## Build And Publish

### Recommended (root publish output)

Run from repository root:

```powershell
.\build.cmd
```

or:

```powershell
.\build.ps1
```

Default publish output directory:

`.\publish`

Executable:

`.\publish\FloatBrowser.App.exe`

### Manual publish

```powershell
dotnet publish .\src\FloatBrowser.App\FloatBrowser.App.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=false -o .\publish
```

## Packaging

You can package the published output as zip, for example:

`.\dist\FloatBrowser-vX.Y.Z-win-x64.zip`

## Notes

- This is a Windows desktop app. It cannot run directly as a Linux GUI app in WSL.
- User settings in `%LocalAppData%\FloatBrowser\settings.json` take precedence over defaults.

## License

See repository root `LICENSE`.

