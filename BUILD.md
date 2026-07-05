# Lively Wallpaper 编译指南

## 环境要求

- Windows 10 1903 或更高
- [.NET 9 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/9.0)
- [.NET Framework 4.7.2 Targeting Pack](https://dotnet.microsoft.com/en-us/download/dotnet-framework/net472)（或安装 Visual Studio Build Tools 2022 时选中 Managed Desktop Build Tools workload）
- [Windows App SDK](https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/downloads)（WinUI 3 项目需要）

## 编译

```powershell
cd src\Lively
dotnet build Lively.sln -c Release -p:Platform=x64
```

## WSL 下编译

前提：Windows 侧已安装 .NET 9 SDK、.NET Framework 4.7.2 Targeting Pack、Windows App SDK。

WSL 内可直接调用 Windows 的 `dotnet.exe`，无需在 WSL 中重复安装：

```bash
# 进入项目目录
cd /mnt/d/SoftwareInstall/lively_command_utility/lively/src/Lively

# 调用 Windows dotnet 编译
"/mnt/c/Program Files/dotnet/dotnet.exe" build Lively.sln -c Release -p:Platform=x64
```

注意事项：
- 编译前必须关闭正在运行的 `Lively.exe`，否则输出 DLL 被锁定会导致 `MSB3027` 错误
- 项目路径必须位于 Windows 文件系统（`/mnt/d/`、`/mnt/c/`），不能放在 WSL 原生 ext4 分区
- net472 子项目依赖 .NET Framework，Linux dotnet 不支持，必须调用 Windows dotnet
- WinUI 3 项目依赖 Windows App SDK，同样只能通过 Windows dotnet 编译

## csproj 格式说明

以下 6 个子项目已从旧式 `.csproj` 格式转换为 SDK 格式，以支持纯 `dotnet build` 编译：

| 项目 | TargetFramework | 类型 | 说明 |
|------|----------------|------|------|
| Lively.Player.CefSharp | net472 | WinForms | CefSharp WebView 播放器 |
| Lively.Player.WebView2 | net472 | WinForms | WebView2 播放器 |
| Lively.Player.Vlc | net472 | WinForms | VLC 播放器，需 System.Resources.Extensions |
| Lively.Player.Wmf | net472 | WPF | WMF 播放器 |
| Lively.Utility.Watchdog | net472 | 控制台 | 看门狗进程 |
| Lively.Utility.Screensaver | net472 | 控制台 | 屏保工具 |

转换要点：
- `<TargetFrameworkVersion>v4.7.2</TargetFrameworkVersion>` → `<TargetFramework>net472</TargetFramework>`
- 移除了显式的 `<Reference Include="System..."/>`（SDK 自动引用）
- 移除了显式的 `<Compile Include="..."/>`（SDK 自动包含 `*.cs`）
- 移除了显式的 `<EmbeddedResource Include="..."/>`（SDK 自动包含 `*.resx`）
- WinForms 项目添加 `<UseWindowsForms>true</UseWindowsForms>`
- WPF 项目添加 `<UseWPF>true</UseWPF>`
- 添加 `<GenerateAssemblyInfo>false</GenerateAssemblyInfo>` 以保留原有 `AssemblyInfo.cs`
- 添加 `<AppendTargetFrameworkToOutputPath>false</AppendTargetFrameworkToOutputPath>` 保持输出路径与旧格式一致

## 已知警告

- `NU1901`/`NU1902`/`NU1903`：Magick.NET-Q8-AnyCPU 14.9.1 存在已知安全漏洞，建议关注上游更新
- `MVVMTK0045`：WinUI 3 项目的 MVVM Toolkit 使用 `[ObservableProperty]` 字段在 AOT 场景下不兼容，不影响正常使用
- `NU1605`：部分包版本降级警告，不阻断编译

## VSCode 配置

安装扩展 `C# Dev Kit`，打开 `src/Lively/Lively.sln` 即可。

## 编译同步到运行目录

编译完成后需将各子项目的输出同步到核心进程的 `plugins/` 目录：

```powershell
# PowerShell（开发模式，仅同步变更文件）
.\build_release.ps1 -Dev
```

```bash
# WSL
"/mnt/c/Windows/System32/WindowsPowerShell/v1.0/powershell.exe" -File build_release.ps1 -Dev
```

编译前必须关闭正在运行的 `Lively.exe`，否则输出 DLL 被锁定会导致 `MSB3027` 错误。
```powershell
taskkill /im Lively.exe /f
```
