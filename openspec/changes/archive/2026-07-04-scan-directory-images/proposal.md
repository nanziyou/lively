## 背景

当前添加壁纸只能通过文件选择器逐个选取。用户如果有一个组织好的壁纸目录（例如 `D:\Wallpapers\Nature\`，子目录按分类存放），必须逐一手动选择或从资源管理器拖拽，没有"指一个文件夹、递归导入所有内容"的方式。这对磁盘上收藏了大量壁纸的用户很不友好。

## 改动内容

- 在添加壁纸界面新增"浏览文件夹"按钮，允许用户选择一个目录
- 递归扫描所选目录及所有子目录，查找支持的壁纸文件（图片、视频、GIF、壁纸包）
- 通过现有的 `AddWallpapers` 批量导入管道处理所有发现的文件，显示进度
- 自动跳过不支持的文件类型和隐藏/系统目录
- 完成后显示汇总信息（成功导入数量、跳过数量）

## 能力范围

### 新增能力

- `wallpaper-directory-scan`：递归扫描目录导入壁纸，支持批量处理与进度反馈

### 修改能力

<!-- 无：纯新增功能，不改变现有 spec 级别的行为 -->

## 影响范围

- **UI**：`AddWallpaperView.xaml` — 在"浏览文件"按钮旁增加"浏览文件夹"按钮
- **ViewModel**：`AddWallpaperViewModel.cs` — 新增文件夹浏览命令；`LibraryViewModel.cs` — 新增目录扫描辅助方法
- **FileService**：`IFileService.PickFolderAsync` 已存在，无需改动
- **无新依赖**：复用现有 `WallpaperType` 检测、`AddWallpapers` 批量管道和 `mediaFormatConverter`
