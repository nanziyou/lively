## 上下文

Lively 是多进程架构：

- `Lively.exe`（WPF 核心进程）— 管理壁纸显示、系统托盘、gRPC 服务端
- `Lively.UI.WinUI.exe`（WinUI 3 界面进程）— 设置页面、壁纸库浏览
- 设置通过 `SettingsModel` 在核心进程持久化，UI 通过 gRPC 读写

壁纸库列表由 `LibraryViewModel` 在 UI 进程中维护，核心进程没有直接的库引用。但核心进程有 `IWallpaperLibraryFactory`（可以从目录创建 `LibraryModel`）和 `WallpaperDir`（壁纸存储根目录）。

现有 Fisher-Yates shuffle 在 `CommandHandler.cs:611` 和 `Systray.cs:352` 各有一份完全相同的实现。

## 目标 / 非目标

**目标：**
- 在核心进程实现自动切换定时器，不依赖 UI 是否打开
- 支持顺序和随机两种切换模式
- 随机模式使用 Fisher-Yates，每轮不重复
- 间隔支持秒级，0 表示关闭
- 顺序切换的顺序与界面壁纸库显示顺序一致

**非目标：**
- 不支持按显示器的独立切换调度（第一版全局统一切换）
- 不支持切换过渡动画
- 不监控文件系统变化

## 决策

### 1. 定时器放在核心进程

`WallpaperAutoChanger` 服务在 `App.xaml.cs` 的 `ConfigureServices()` 中注册为 Singleton，构造函数注入 `IUserSettingsService` 和 `IDesktopCore`。

理由：核心进程一直运行（托盘常驻），UI 进程可能被用户关闭。定时器在核心进程确保切换不受 UI 状态影响。

### 2. 轮询检测设置变化

定时器固定每 1 秒 tick，内部累计 `elapsedSeconds`，达到 `WallpaperChangeInterval` 时触发切换。同时每秒检测 interval 和 order 是否变化，变化时立即重置状态。

理由：避免依赖 gRPC 推送通知（不存在），保持简单。

### 3. 壁纸列表通过目录扫描获取

扫描 `WallpaperDir` 下的 `wallpapers/` 和 `SaveData/wptmp/` 两个子目录，通过 `IWallpaperLibraryFactory.CreateFromDirectory` 构建 `LibraryModel` 列表。加载后通过 `LivelyInfoUtil.GetLocalized` 本地化标题，再按 `CurrentCultureIgnoreCase` 排序。

理由：
- 排序用 `CurrentCultureIgnoreCase` 而非 `OrdinalIgnoreCase`，与 UI 的 `AdvancedCollectionView` 在 zh-CN 下的默认排序行为一致
- `LocalizeModel` 是 LibraryViewModel 的 private 方法，核心进程无法调用，改用 `LivelyInfoUtil` 达到同等效果
- 扫描两个目录因为 `wallpapers/` 存放内置网页壁纸，`SaveData/wptmp/` 存放用户导入的媒体壁纸

### 4. 顺序模式：维护索引指针

```csharp
currentIndex = (currentIndex + 1) % wallpapers.Count;
```

### 5. 随机模式：Fisher-Yates + 耗尽后重新洗牌

```csharp
if (shuffled == null || shuffleIndex >= shuffled.Count)
{
    shuffled = new List<LibraryModel>(wallpapers);
    shuffled.Shuffle();
    shuffleIndex = 0;
}
```

### 6. Fisher-Yates 抽取为公共扩展

在 `src/Lively/Lively.Common/Extensions/ListExtensions.cs` 新增 `Shuffle<T>()`。`CommandHandler` 和 `Systray` 中现有的两处实现改为调用此方法。

### 7. 设置模型 + gRPC 双向同步

`SettingsModel` 新增两个字段，并在 `settings.proto`、`UserSettingsClient.CreateGrpcSettings`、`UserSettingsServer.SetSettings`、`UserSettingsServer.GetSettings`、`UserSettingsClient.CreateSettingsFromGrpc` 五处同步。

### 8. 切换失败快速跳过

- HTML 网页壁纸在自动切换中直接跳过（管理员模式下 WebView2 不可靠），检查 `LivelyInfo.Type.IsWebWallpaper()`
- 切换后等待 2 秒验证 `desktopCore.Wallpapers.Count > 0`，失败则记录到 `failedWallpapers` 并换下一个
- 加 `switching` 标记防止并发重入

### 9. UI 使用下拉预设

ComboBox 提供：关闭 / 5秒 / 10秒 / 30秒 / 1分钟 / 5分钟 / 15分钟 / 30分钟 / 1小时 / 自定义。选"自定义"时展开 NumberBox 输入秒数。`PresetIntervals` 数组 `[0, 5, 10, 30, 60, 300, 900, 1800, 3600, -1]`，`-1` 代表自定义。

## 风险 / 权衡

| 风险 | 缓解 |
|------|------|
| 大量壁纸时每次扫描目录性能差 | `CreateFromDirectory` 只读 LivelyInfo.json，数百壁纸毫秒级 |
| 切换中用户手动换壁纸导致索引错乱 | 切换完成后的 `ForceRefreshWallpaperList` 重建列表并重置索引 |
| 用户删除壁纸导致切换失败 | 跳过该壁纸，记录到 failedWallpapers |
| HTML 壁纸在管理员模式下全崩 | 自动切换中直接跳过 web/url/webaudio/videostream 类型 |
| 1 秒轮询的性能开销 | DispatcherTimer tick 几乎无开销，每秒仅做整数比较 |
