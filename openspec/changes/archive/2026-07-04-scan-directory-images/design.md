## 上下文

Lively 当前的添加壁纸流程：

1. `AddWallpaperView` 展示 UI，包含"浏览文件"按钮和 URL 输入框
2. `AddWallpaperViewModel.BrowseFileCommand` 调用 `IFileService.PickWallpaperFile`，打开 Windows 文件选择器
3. 选择后通过事件 `OnRequestAddFile` 回调给 `DialogService`，再传入 `MainViewModel.AddWallpapers`
4. `MainViewModel.AddWallpapers` 显示进度条 → 调用 `LibraryViewModel.AddWallpapers` 批量处理

`IFileService` 已有 `PickFolderAsync` 方法（Windows 文件夹选择器），但当前未被添加壁纸流程使用。

## 目标 / 非目标

**目标：**
- 在添加壁纸界面增加"浏览文件夹"入口
- 递归扫描选定目录及其子目录，找出所有支持的壁纸文件
- 通过现有的批量导入管道处理，带进度显示

**非目标：**
- 不实现目录监控（watch folder changes）
- 不改变壁纸库的存储结构
- 不处理在线 URL 的批量导入
- 不支持程序壁纸（application/game wallpaper）的目录扫描——仅限媒体文件和壁纸包

## 决策

### 1. 入口位置：在 `AddWallpaperView.xaml` 新增 SettingsCard

在"浏览文件"卡片下方添加一个同风格的"浏览文件夹"卡片，绑定 `BrowseFolderCommand`。

```
AddWallpaperFileBrowse (现有)
AddWallpaperFolderBrowse (新增)
EnterUrl (现有)
AddWallpaperAdvanced (现有)
```

选择理由：`SettingsCard` 是项目已有的 UI 模式，与现有"浏览文件"按钮视觉一致。单独一个命令比修改现有按钮交互（右键/长按弹出菜单）更直观。

### 2. 扫描逻辑：在 ` ` 中新增方法

```csharp
// 伪代码
private async Task FolderBrowseAction()
{
    var folder = await fileService.PickFolderAsync(null);
    if (folder == null) return;

    var files = Directory.EnumerateFiles(folder, "*.*", SearchOption.AllDirectories)
        .Where(f => IsWallpaperFile(f))
        .ToList();

    OnRequestAddFile?.Invoke(this, files);
}

private static bool IsWallpaperFile(string path)
{
    return FileTypes.GetFileType(path).IsMediaWallpaper()
        || FileTypes.IsWallpaperPackageExtension(path);
}
```

选择理由：
- `System.IO` 是 BCL 自带，无需新依赖
- `EnumerateFiles` 延迟枚举，对超大目录内存友好
- 过滤逻辑复用现有的 `FileTypes.GetFileType` 和 `FileTypes.IsWallpaperPackageExtension`，与 `LibraryViewModel.AddWallpapers` 的过滤一致
- 不在此处判断应用壁纸（`IsApplicationWallpaper`），因为应用壁纸需要额外的参数输入，不适合批量导入

### 3. 批量导入：复用 `MainViewModel.AddWallpapers`

扫描结果直接通过已有的 `OnRequestAddFile` 事件传递，完全走现有管道：

```
FolderBrowseAction → OnRequestAddFile(fileList)
  → DialogService 回调 → MainViewModel.AddWallpapers
    → ImportNotification 进度显示
    → LibraryViewModel.AddWallpapers 批量处理
```

选择理由：无需重复实现进度、取消、错误处理逻辑，零风险。

### 4. 过滤策略：静默跳过不支持的文件

- 不支持的文件类型直接跳过，不弹错误提示
- 跳过隐藏目录和系统目录（`FileAttributes.Hidden | FileAttributes.System`）
- 访问被拒的目录捕获 `UnauthorizedAccessException`，跳过继续扫描

选择理由：批量操作的常见做法，用户不愿每遇到一个不支持的格式就点一次确认。

## 风险 / 权衡

| 风险 | 缓解 |
|------|------|
| 超大目录（数千文件）扫描时 UI 卡顿 | `EnumerateFiles` 是延迟枚举；若数量极大，后续可考虑 `IAsyncEnumerable` + `Channel<T>` 分批推送，但第一版先用同步扫描，实测 impact |
| 深层嵌套目录超出 `MAX_PATH` (260 字符) | .NET 9 默认启用长路径支持；`SearchOption.AllDirectories` 内部处理 |
| 用户选择了系统目录（如 `C:\Windows`）导致海量无关文件 | 可加的优化：扫描前检测子目录数量，超过阈值（如 500 个文件）弹确认框。第一版先不做，作为后续迭代项 |
| 壁纸包（.zip）在扫描阶段无法判断是否为 Lively 包 | `IsWallpaperPackageExtension` 只检查扩展名，真正验证需要解压读取 `LivelyInfo.json`。错误 zip 会在 `AddWallpapers` 阶段被自然跳过（`AddWallpaperFile` 会抛异常被 catch） |
