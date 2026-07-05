# Repository Guidelines

## Project Structure & Module Organization

**Lively Wallpaper** — animated desktop wallpapers for Windows, built on .NET 9 and WinUI 3.

```
src/Lively/Lively.sln          # Main solution
src/Lively/Lively/             # WinUI 3 host app
src/Lively/Lively.Common/      # Shared utilities, helpers, exceptions, factories
src/Lively/Lively.Common.Services/  # Core service abstractions
src/Lively/Lively.Models/      # Models, enums, message types
src/Lively/Lively.UI.Shared/   # Shared ViewModels and factories
src/Lively/Lively.UI.WinUI/    # WinUI views, controls, strings
src/Lively/Lively.ML/          # ML inference (depth estimation)
src/Lively/Lively.Gallery.Client/  # Submissions gallery API client
src/Lively/Lively.Grpc.Client/     # gRPC client for inter-process comms
src/Lively/Lively.Grpc.Common/     # Shared gRPC protos and types
src/Lively/Lively.Player.*/    # Wallpaper players (CefSharp, WebView2, VLC, WMF)
src/Lively/Lively.Utility.*/   # Companion tools (screensaver, watchdog, commandline)
src/installer/                 # Inno Setup installer scripts and assets
schemas/                       # JSON schemas (livelyPropertiesSchema.json)
resources/                     # Promotional imagery
```

## Build, Test, and Development Commands

Build from the solution root:

```powershell
cd src\Lively
dotnet build Lively.sln -c Release -p:Platform=x64
```

- **Prerequisites**: .NET 9 SDK, .NET Framework 4.7.2 Targeting Pack, Windows App SDK, Windows 10 1903+.
- **IDE**: Visual Studio 2022 (C# workload), or VS Code with C# Dev Kit.
- Six `net472` player/utility projects use SDK-style `.csproj` with `<UseWindowsForms>` or `<UseWPF>`.

No automated tests exist yet. Manual testing verifies wallpaper playback, screensaver behavior, and CLI integration.

## Coding Style & Naming Conventions

- Standard C# conventions: PascalCase types/public members, camelCase locals/parameters.
- MVVM via `CommunityToolkit.Mvvm` source generators (`[ObservableProperty]`, `[RelayCommand]`).
- Folder conventions: `Services/`, `Helpers/`, `Extensions/`, `ViewModels/`, `Views/`, `Converters/`, `Factories/`.
- Localization strings in `src/Lively/Lively.UI.WinUI/Strings/`; community translations via Crowdin.

## Commit & Pull Request Guidelines

- Short imperative summaries: "Fix Mpv slider serialization culture", "Added screensaver wait ui translation".
- **Contributing**: [Wiki guidelines](https://github.com/rocksdanister/lively/wiki/Contributing-Guidelines).
- **Issues**: Use `.github/ISSUE_TEMPLATE/` (bug report or feature request).
- **PRs**: Link issues, attach screenshots/videos for UI changes, test on Windows 10 and 11.

## Security & Configuration

- Security disclosures: see [SECURITY.md](SECURITY.md).
- gRPC for IPC between the main app and companion utilities.
- Never commit API keys/secrets; use env vars or gitignored local configs.

## Packaging & Release

The project is split into multiple processes — `dotnet build` outputs each to its own `bin/` directory. A separate step is needed to assemble them:

```powershell
.\build_release.ps1              # Dev: build + sync
.\build_release.ps1 -Package     # Full: build + zip for distribution
```

This collects all projects into a single `Release\` folder matching the runtime layout expected by `Lively.exe` (`plugins/UI/`, `plugins/webview2/`, `plugins/mpv/`, etc.). The official installer is built with Inno Setup (`src/installer/Script_x64.iss`), which packs `Release\` plus VC++ redist and .NET runtime into `lively_installer.exe`.

External dependencies (`mpv.exe`, `vlc.exe`) are not compiled by this repo and must be placed manually into `plugins/mpv/` and `plugins/vlc/` before packaging.

## Running Locally

After building, the runnable output is under the core project's `bin/` directory. Plugins (UI, players, watchdog) must be assembled manually or via the build script.

```bash
# Start (WSL):
"/mnt/c/Windows/System32/cmd.exe" /c start "" "D:\\SoftwareInstall\\lively_command_utility\\lively\\src\\Lively\\Lively\\bin\\x64\\Release\\net9.0-windows10.0.18362.0\\Lively.exe"
```

```bash
# Stop (WSL):
"/mnt/c/Windows/System32/cmd.exe" /c "taskkill /im Lively.exe /f"
```

See [shell/lively_stop.bat](shell/lively_stop.bat) for a convenient stop script.
