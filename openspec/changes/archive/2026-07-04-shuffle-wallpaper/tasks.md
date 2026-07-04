## 1. 基础设施

- [x] 1.1 新增 `WallpaperChangeOrder` 枚举（sequential / shuffle）到 `Lively.Models/Enums/`
- [x] 1.2 在 `SettingsModel` 中新增 `WallpaperChangeInterval`（int，秒）和 `WallpaperChangeOrder` 字段，默认值 0 / sequential
- [x] 1.3 新增 `ListExtensions.Shuffle<T>()` 公共扩展方法到 `Lively.Common/Extensions/`
- [x] 1.4 重构 `CommandHandler.cs` 和 `Systray.cs`，用公共 `Shuffle()` 替换内联 Fisher-Yates
- [x] 1.5 修改 `settings.proto`、`UserSettingsClient`（序列化+反序列化）、`UserSettingsServer`（接收+响应），保证 gRPC 双向同步新字段

## 2. 核心服务

- [x] 2.1 创建 `WallpaperAutoChanger` 服务类，构造函数注入 `IUserSettingsService`、`IDesktopCore`、`IWallpaperLibraryFactory`、`IDisplayManager`
- [x] 2.2 每 1 秒轮询检测 `WallpaperChangeInterval` 和 `WallpaperChangeOrder` 变化，变化时 `ForceRefreshWallpaperList` 并重置状态
- [x] 2.3 实现顺序模式：扫描 `wallpapers/` + `SaveData/wptmp/` 两个目录，`LivelyInfoUtil.GetLocalized` 本地化标题，`CurrentCultureIgnoreCase` 排序
- [x] 2.4 实现随机模式：Fisher-Yates 洗牌，耗尽重新洗牌
- [x] 2.5 跳过 web/webaudio/url/videostream 壁纸（管理员模式 WebView2 崩溃）
- [x] 2.6 切换后 2 秒验证 `Wallpapers.Count`，失败换下一个，`switching` 标记防并发
- [x] 2.7 在 `App.xaml.cs` 中注册为 Singleton，`AppInitializer` 之后启动

## 3. UI 设置

- [x] 3.1 在 `SettingsWallpaperViewModel` 新增 `SelectedWallpaperChangeIntervalIndex`（ComboBox）、`WallpaperChangeIntervalCustom`（NumberBox）、`IsCustomInterval`（Visibility）
- [x] 3.2 在 `SettingsWallpaperView.xaml` 新增下拉预设 ComboBox + 自定义秒数 NumberBox（仅选"自定义"时可见）
- [x] 3.3 新增切换顺序 ComboBox（顺序 / 随机）
- [x] 3.4 添加本地化字符串（en-US / zh-CN）

## 4. 集成测试

- [x] 4.1 编译运行，验证设置界面新选项可见
- [x] 4.2 设置间隔 5 秒、顺序模式，验证顺序与界面显示一致
- [x] 4.3 设置随机模式，验证不重复
- [x] 4.4 设置间隔为 0，验证停止切换
- [x] 4.5 重启程序，验证设置保留
- [x] 4.6 切换 web 壁纸时不弹错误通知
