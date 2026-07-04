## ADDED Requirements

### Requirement: 自动切换定时器

系统 SHALL 在核心进程中维护一个 1 秒轮询定时器，按用户设定的间隔自动切换壁纸。间隔为 0 时停止切换。

#### Scenario: 启用自动切换
- **WHEN** 用户设置切换间隔为大于 0 的秒数
- **THEN** 系统累计 elapsedSeconds，达到目标秒数时触发壁纸切换

#### Scenario: 关闭自动切换
- **WHEN** 用户将切换间隔设置为 0
- **THEN** 系统重置累计秒数，不再触发切换

#### Scenario: 修改间隔
- **WHEN** 定时器运行中用户修改了间隔值（gRPC 推送更新 `SettingsModel`）
- **THEN** 系统检测到间隔变化，重建壁纸列表并重置累计秒数

#### Scenario: 修改切换顺序
- **WHEN** 定时器运行中用户从"顺序"切换到"随机"（或反之）
- **THEN** 系统检测到顺序变化，通过 `ForceRefreshWallpaperList` 无条件重建列表并重置状态，立即生效

### Requirement: 顺序播放模式

顺序模式下 SHALL 按壁纸库界面显示顺序依次切换，到达末尾后回到开头。

#### Scenario: 顺序切换
- **WHEN** 切换顺序设置为 sequential，定时器到期
- **THEN** 系统切换到壁纸库中的下一个壁纸

#### Scenario: 排序与界面一致
- **WHEN** 顺序模式下加载壁纸列表
- **THEN** 列表使用 `CurrentCultureIgnoreCase` 按标题排序，与 UI 的 `AdvancedCollectionView` 排序一致

### Requirement: 随机播放模式

随机模式下 SHALL 使用 Fisher-Yates 算法对壁纸列表洗牌，确保每轮每个壁纸恰好出现一次，不重复。一轮耗尽后重新洗牌开始下一轮。

#### Scenario: 随机切换不重复
- **WHEN** 切换顺序设置为 shuffle，定时器到期
- **THEN** 系统从洗牌后的列表中按顺序取出下一个壁纸，当前轮次内不重复

#### Scenario: 一轮耗尽后重新洗牌
- **WHEN** 洗牌列表中的所有壁纸均已播放过
- **THEN** 系统重新洗牌生成新的播放顺序，开始下一轮

### Requirement: 壁纸列表构建

系统 SHALL 扫描 `WallpaperDir` 下的 `wallpapers/` 和 `SaveData/wptmp/` 两个目录，加载壁纸元数据并进行本地化。

#### Scenario: 加载壁纸元数据
- **WHEN** 系统构建壁纸列表
- **THEN** 通过 `IWallpaperLibraryFactory.CreateFromDirectory` 加载每个目录的 `LivelyInfo.json`

#### Scenario: 本地化标题
- **WHEN** 壁纸存在本地化文件（`LivelyInfo.loc.json`）
- **THEN** 通过 `LivelyInfoUtil.GetLocalized` 使用当前语言覆盖标题，保证与 UI 排序一致

### Requirement: 切换失败处理

系统 SHALL 快速跳过无法显示的壁纸类型，并在切换失败时换下一个。

#### Scenario: 跳过网页壁纸
- **WHEN** 壁纸类型为 web / webaudio / url / videostream
- **THEN** 系统直接跳过，不调用 `SetWallpaperAsync`

#### Scenario: 切换失败后重试
- **WHEN** `SetWallpaperAsync` 完成后 2 秒检测到 `Wallpapers.Count == 0`
- **THEN** 系统将该壁纸加入失败列表，取下一个壁纸继续尝试

#### Scenario: 防止并发
- **WHEN** 上一次切换还在进行中（2 秒等待期间）
- **THEN** `switching` 标记阻止新的 tick 进入，避免两个切换同时执行

### Requirement: 设置持久化

自动切换的间隔和顺序 SHALL 通过 SettingsModel → gRPC（proto + Client + Server 双向）持久化，程序重启后保留。

#### Scenario: 重启后恢复
- **WHEN** 用户重启 Lively
- **THEN** `GetSettings` gRPC 响应返回保存的 `WallpaperChangeInterval` 和 `WallpaperChangeOrder`

### Requirement: Fisher-Yates 公共方法

项目 SHALL 提供 `IList<T>.Shuffle()` 公共扩展方法，消除 `CommandHandler` 和 `Systray` 中的重复实现。

#### Scenario: 调用公共 Shuffle
- **WHEN** 任意代码需要对列表洗牌
- **THEN** 调用 `list.Shuffle()` 即可
