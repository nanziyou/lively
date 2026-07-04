## 1. ViewModel 层

- [x] 1.1 在 `AddWallpaperViewModel` 新增 `BrowseFolderCommand` 命令，调用 `IFileService.PickFolderAsync`
- [x] 1.2 在 `AddWallpaperViewModel` 新增 `ScanDirectoryForWallpapers` 辅助方法，用 `Directory.EnumerateFiles` + `SearchOption.AllDirectories` 递归扫描
- [x] 1.3 实现扫描过滤逻辑：通过 `FileTypes.GetFileType` 和 `FileTypes.IsWallpaperPackageExtension` 过滤，跳过隐藏/系统目录，捕获 `UnauthorizedAccessException`

## 2. UI 层

- [x] 2.1 在 `AddWallpaperView.xaml` 新增 "浏览文件夹" `SettingsCard`，绑定 `BrowseFolderCommand`，放在 "浏览文件" 卡片下方
- [x] 2.2 添加对应的本地化字符串资源（`AddWallpaperFolderBrowse` 的 Header 和 Description）— en-US 和 zh-CN

## 3. 集成测试

- [x] 3.1 编译运行，验证"浏览文件夹"按钮可见且可点击
- [x] 3.2 选择一个包含混合文件（图片 + 视频 + 非壁纸文件）的目录，验证只导入支持的格式
- [x] 3.3 选择一个包含子目录的目录，验证递归扫描正常工作
- [x] 3.4 验证导入进度条显示正确、取消功能正常
