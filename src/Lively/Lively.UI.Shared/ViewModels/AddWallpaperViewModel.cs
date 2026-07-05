using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lively.Common;
using Lively.Common.Extensions;
using Lively.Models.Enums;
using Lively.Common.Services;
using Lively.Grpc.Client;
using Lively.Models;
using Lively.UI.WinUI.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using ImageMagick;
using UAC = UACHelper.UACHelper;

namespace Lively.UI.Shared.ViewModels
{
    public partial class AddWallpaperViewModel : ObservableObject
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        public event EventHandler<List<string>> OnRequestAddFile;
        public event EventHandler<string> OnRequestAddUrl;
        public event EventHandler OnRequestOpenCreate;

        private readonly IUserSettingsClient userSettings;
        private readonly IDispatcherService dispatcher;
        private readonly IFileService fileService;

        public AddWallpaperViewModel(IUserSettingsClient userSettings, IDispatcherService dispatcher, IFileService fileService)
        {
            this.userSettings = userSettings;
            this.dispatcher = dispatcher;
            this.fileService = fileService;

            IsElevated = UAC.IsElevated;
            WebUrlText = userSettings.Settings.SavedURL;
        }

        public void UpdateSettingsConfigFile()
        {
            dispatcher.TryEnqueue(userSettings.Save<SettingsModel>);
        }

        [ObservableProperty]
        private string webUrlText;

        [ObservableProperty]
        private string errorMessage;

        public bool IsElevated { get; }

        [ObservableProperty]
        private bool excludePortrait;

        private RelayCommand _browseWebCommand;
        public RelayCommand BrowseWebCommand => _browseWebCommand ??= new RelayCommand(WebBrowseAction);

        private RelayCommand _createWallpaperCommand;
        public RelayCommand CreateWallpaperCommand => _createWallpaperCommand ??= 
            new RelayCommand(()=> OnRequestOpenCreate?.Invoke(this, EventArgs.Empty));

        private void WebBrowseAction()
        {
            if (!LinkUtil.TrySanitizeUrl(WebUrlText, out Uri uri))
                return;

            WebUrlText = uri.OriginalString;
            userSettings.Settings.SavedURL = WebUrlText;
            UpdateSettingsConfigFile();

            AddWallpaperLink(uri);
        }

        public void AddWallpaperLink(Uri uri) => OnRequestAddUrl?.Invoke(this, uri.OriginalString);

        private RelayCommand _browseFileCommand;
        public RelayCommand BrowseFileCommand => _browseFileCommand ??= new RelayCommand(async () => await FileBrowseAction());

        private async Task FileBrowseAction()
        {
            ErrorMessage = null;
            var files = await fileService.PickWallpaperFile(true);

            if (files.Count > 0)
            {
                if (files.Count == 1)
                    AddWallpaperFile(files[0]);
                else
                    AddWallpaperFiles(files.ToList());
            }
        }

        public void AddWallpaperFile(string path) => OnRequestAddFile?.Invoke(this, new List<string>() { path });

        public void AddWallpaperFiles(List<string> filePaths) => OnRequestAddFile?.Invoke(this, filePaths);

        private RelayCommand _browseFolderCommand;
        public RelayCommand BrowseFolderCommand => _browseFolderCommand ??= new RelayCommand(async () => await FolderBrowseAction());

        private async Task FolderBrowseAction()
        {
            var folder = await fileService.PickFolderAsync(["*"]);
            if (string.IsNullOrEmpty(folder))
                return;

            Logger.Info($"FolderBrowse: folder={folder}, excludePortrait={ExcludePortrait}");
            var files = await ScanDirectoryForWallpapersAsync(folder, ExcludePortrait);
            Logger.Info($"FolderBrowse: found {files.Count} files after filtering");
            if (files.Count > 0)
                AddWallpaperFiles(files);
        }

        private static async Task<List<string>> ScanDirectoryForWallpapersAsync(string rootPath, bool excludePortrait)
        {
            var allFiles = Directory.EnumerateFiles(rootPath, "*.*", SearchOption.AllDirectories)
                .Where(f => IsWallpaperFile(f) && !IsHiddenOrSystem(f))
                .ToList();

            if (!excludePortrait || allFiles.Count == 0)
                return allFiles;

            var result = new List<string>();
            foreach (var f in allFiles)
            {
                if (!await IsPortraitAsync(f))
                    result.Add(f);
            }
            return result;
        }

        private static async Task<bool> IsPortraitAsync(string filePath)
        {
            try
            {
                var info = new MagickImageInfo(filePath);
                if (info.Height > info.Width)
                {
                    Logger.Info($"Portrait IMAGE excluded: {Path.GetFileName(filePath)} ({info.Width}x{info.Height})");
                    return true;
                }
                return false;
            }
            catch { }

            try
            {
                var storageFile = await Windows.Storage.StorageFile.GetFileFromPathAsync(filePath);
                var props = await storageFile.Properties.GetVideoPropertiesAsync();
                Logger.Info($"Portrait check video: {Path.GetFileName(filePath)} {props.Width}x{props.Height}");
                if (props.Width > 0 && props.Height > 0 && props.Height > props.Width)
                {
                    Logger.Info($"Portrait VIDEO excluded: {Path.GetFileName(filePath)} ({props.Width}x{props.Height})");
                    return true;
                }
            }
            catch { }

            return false;
        }

        private static bool IsWallpaperFile(string path) =>
            FileTypes.GetFileType(path).IsMediaWallpaper() || FileTypes.IsWallpaperPackageExtension(path);

        private static bool IsHiddenOrSystem(string path)
        {
            try
            {
                var dir = Path.GetDirectoryName(path);
                if (dir != null)
                {
                    var attr = new DirectoryInfo(dir).Attributes;
                    return (attr & (FileAttributes.Hidden | FileAttributes.System)) != 0;
                }
            }
            catch (UnauthorizedAccessException) { return true; }
            return false;
        }
    }
}