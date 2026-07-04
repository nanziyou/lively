## 背景

Lively Wallpaper 目前没有自动切换壁纸的功能。用户设置壁纸后，除非手动更换，否则一直显示同一张。Windows 自带的壁纸功能支持定时切换和随机顺序，Lively 缺失这个基础能力。

## 改动内容

- 新增"自动切换壁纸"设置项：切换间隔（关闭 / 5秒 / 15秒 / 30秒 / 1分钟 / 5分钟 / 自定义秒数）
- 新增"切换顺序"设置项：顺序播放 / 随机播放
- 随机模式使用 Fisher-Yates shuffle，确保每轮不重复
- 将 Fisher-Yates 从 `CommandHandler` 和 `Systray` 中抽取为公共扩展方法 `IList<T>.Shuffle()`，三处共用
- 后台定时器按设定间隔从壁纸库中选取下一个壁纸，调用现有 `SetWallpaperAsync` 切换
- 设置为"关闭"时保持现有行为不变

## 能力范围

### 新增能力

- `wallpaper-auto-change`：定时自动切换壁纸，支持顺序和随机两种模式

### 修改能力

<!-- 无 -->

## 影响范围

- **Common**：`ListExtensions.cs` — 抽取 Fisher-Yates shuffle 为公共扩展方法
- **Model**：`SettingsModel.cs` — 新增 `WallpaperChangeInterval`（秒）、`WallpaperChangeOrder` 字段
- **Enum**：新增 `WallpaperChangeOrder` 枚举（sequential / shuffle）
- **Service**：新增 `WallpaperAutoChanger` 服务，基于 `DispatcherTimer` 实现定时切换
- **UI**：`SettingsWallpaperView.xaml` — 新增切换间隔输入框和顺序下拉框
- **ViewModel**：`SettingsWallpaperViewModel.cs` — 新增对应属性和命令
- **CommandHandler / Systray**：改用公共 `Shuffle()` 扩展方法
