# wallpaper-directory-scan Specification

## Purpose
TBD - created by archiving change scan-directory-images. Update Purpose after archive.
## Requirements
### Requirement: 文件夹选择入口

添加壁纸界面 SHALL 提供一个"浏览文件夹"按钮，用户点击后弹出 Windows 文件夹选择对话框。

#### Scenario: 用户点击浏览文件夹
- **WHEN** 用户在添加壁纸界面点击"浏览文件夹"按钮
- **THEN** 系统打开 Windows 原生文件夹选择对话框

#### Scenario: 用户取消选择
- **WHEN** 用户在文件夹选择对话框中点击取消
- **THEN** 系统关闭对话框，不做任何导入操作

#### Scenario: 用户选择有效文件夹
- **WHEN** 用户选择一个存在的文件夹并确认
- **THEN** 系统开始扫描该文件夹及子目录中的壁纸文件

### Requirement: 递归目录扫描

系统 SHALL 递归扫描用户选定目录及其所有子目录，查找支持的壁纸文件。

#### Scenario: 扫描包含图片和视频的目录
- **WHEN** 选定目录及其子目录中包含 `.jpg`、`.png`、`.mp4`、`.gif` 等支持的文件
- **THEN** 系统将所有匹配文件加入待导入列表

#### Scenario: 目录中包含壁纸包
- **WHEN** 选定目录中包含 `.zip` 文件
- **THEN** 系统将 `.zip` 文件加入待导入列表（实际验证在导入阶段进行）

#### Scenario: 目录中包含不支持的文件
- **WHEN** 选定目录中包含 `.txt`、`.docx`、`.exe` 等不支持的文件
- **THEN** 系统静默跳过这些文件，不弹错误提示

#### Scenario: 目录中包含隐藏或系统文件夹
- **WHEN** 扫描路径中包含属性为 `Hidden` 或 `System` 的文件夹
- **THEN** 系统跳过这些文件夹及其内容

#### Scenario: 子目录访问被拒绝
- **WHEN** 扫描过程中某个子目录因权限不足无法访问
- **THEN** 系统捕获异常，跳过该子目录，继续扫描其他目录

### Requirement: 批量导入与进度显示

扫描完成后 SHALL 通过现有的批量导入管道处理所有发现的文件，包含进度反馈。

#### Scenario: 正常批量导入
- **WHEN** 扫描发现 N 个壁纸文件
- **THEN** 系统调用 `MainViewModel.AddWallpapers` 逐个导入，进度条从 0% 走到 100%

#### Scenario: 扫描结果为空
- **WHEN** 选定目录及其子目录中无任何支持的壁纸文件
- **THEN** 系统不触发导入，不显示进度条

#### Scenario: 用户取消导入
- **WHEN** 批量导入进行中，用户点击取消按钮
- **THEN** 系统停止导入，已导入的壁纸保留，未处理的跳过

### Requirement: 导入完成反馈

导入完成后 SHALL 向用户展示汇总信息。

#### Scenario: 全部成功
- **WHEN** 所有文件导入成功
- **THEN** 壁纸库更新，显示新导入的壁纸

#### Scenario: 部分失败
- **WHEN** 部分文件因格式不支持或转换失败被跳过
- **THEN** 成功导入的壁纸出现在库中，失败文件静默跳过

