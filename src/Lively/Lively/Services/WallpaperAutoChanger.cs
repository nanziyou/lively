using Lively.Common;
using Lively.Common.Extensions;
using Lively.Common.Helpers;
using Lively.Common.Factories;
using Lively.Common.Services;
using Lively.Core;
using Lively.Core.Display;
using Lively.Models;
using Lively.Models.Enums;
using Lively.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace Lively.Services
{
    public class WallpaperAutoChanger : IDisposable
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly IUserSettingsService userSettings;
        private readonly IDesktopCore desktopCore;
        private readonly IWallpaperLibraryFactory wallpaperLibraryFactory;
        private readonly IDisplayManager displayManager;
        private readonly DispatcherTimer timer;

        private List<LibraryModel> wallpapers;
        private int currentIndex;
        private List<LibraryModel> shuffled;
        private int shuffleIndex;
        private double elapsedSeconds;
        private int lastLoggedInterval = -1;
        private readonly HashSet<string> failedWallpapers = new();
        private WallpaperChangeOrder lastOrder;
        private bool switching, disposedValue;

        public WallpaperAutoChanger(
            IUserSettingsService userSettings,
            IDesktopCore desktopCore,
            IWallpaperLibraryFactory wallpaperLibraryFactory,
            IDisplayManager displayManager)
        {
            this.userSettings = userSettings;
            this.desktopCore = desktopCore;
            this.wallpaperLibraryFactory = wallpaperLibraryFactory;
            this.displayManager = displayManager;

            timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            timer.Tick += Timer_Tick;
        }

        public void Start()
        {
            timer.Start();
            Logger.Info("AutoChanger: polling started");
        }

        private async void Timer_Tick(object sender, EventArgs e)
        {
            try
            {
                var interval = userSettings.Settings.WallpaperChangeInterval;

                var order = userSettings.Settings.WallpaperChangeOrder;
                if (interval != lastLoggedInterval || order != lastOrder)
                {
                    Logger.Info($"AutoChanger: interval={interval}s, order={order}");
                    lastLoggedInterval = interval;
                    lastOrder = order;
                    ForceRefreshWallpaperList();
                    elapsedSeconds = 0;
                }

                if (interval <= 0)
                {
                    elapsedSeconds = 0;
                    return;
                }

                elapsedSeconds += 1;
                if (elapsedSeconds < interval) return;

                elapsedSeconds = 0;
                if (switching) return;
                switching = true;
                await TrySwitchWallpaper();
                switching = false;
            }
            catch (Exception ex)
            {
                Logger.Error($"AutoChanger: {ex}");
            }
        }

        private async Task TrySwitchWallpaper()
        {
            int attempts = 0;
            while (attempts < (wallpapers?.Count ?? 0))
            {
                var next = GetNextWallpaper();
                if (next == null) return;

                if (failedWallpapers.Contains(next.LivelyInfoFolderPath))
                {
                    attempts++;
                    continue;
                }

                // Skip web wallpapers during auto-change (WebView2 unreliable in elevated mode)
                if (next.LivelyInfo.Type.IsWebWallpaper() || next.LivelyInfo.Type == WallpaperType.url || next.LivelyInfo.Type == WallpaperType.videostream)
                {
                    Logger.Debug($"AutoChanger: skipping {next.Title} (web/stream wallpaper)");
                    attempts++;
                    continue;
                }

                Logger.Info($"AutoChanger: switching to {next.Title}");
                try
                {
                    foreach (var display in displayManager.DisplayMonitors)
                        await desktopCore.SetWallpaperAsync(next, display);

                    await Task.Delay(2000);
                    if (desktopCore.Wallpapers.Count > 0)
                    {
                        failedWallpapers.Clear();
                        return;
                    }

                    Logger.Info($"AutoChanger: {next.Title} failed, trying next");
                    failedWallpapers.Add(next.LivelyInfoFolderPath);
                }
                catch (Exception ex)
                {
                    Logger.Error($"AutoChanger: switch error for {next.Title}: {ex.Message}");
                    failedWallpapers.Add(next.LivelyInfoFolderPath);
                }
                attempts++;
            }

            Logger.Info("AutoChanger: all wallpapers tried this round");
        }

        private LibraryModel GetNextWallpaper()
        {
            RefreshWallpaperList();
            if (wallpapers == null || wallpapers.Count == 0)
                return null;

            return userSettings.Settings.WallpaperChangeOrder switch
            {
                WallpaperChangeOrder.shuffle => GetShuffleNext(),
                _ => GetSequentialNext(),
            };
        }

        private LibraryModel GetSequentialNext()
        {
            currentIndex = (currentIndex + 1) % wallpapers.Count;
            return wallpapers[currentIndex];
        }

        private LibraryModel GetShuffleNext()
        {
            if (shuffled == null || shuffleIndex >= shuffled.Count)
            {
                shuffled = new List<LibraryModel>(wallpapers);
                shuffled.Shuffle();
                shuffleIndex = 0;
            }
            return shuffled[shuffleIndex++];
        }


        private void ForceRefreshWallpaperList()
        {
            wallpapers = null;
            shuffled = null;
            shuffleIndex = 0;
            failedWallpapers.Clear();
            RefreshWallpaperList();
        }

        private void RefreshWallpaperList()
        {
            var baseDir = userSettings.Settings.WallpaperDir;
            var dirs = new List<string>();

            // Scan both wallpapers/ and SaveData/wptmp/
            foreach (var subDir in new[] { Constants.CommonPartialPaths.WallpaperInstallDir, Constants.CommonPartialPaths.WallpaperInstallTempDir })
            {
                var path = Path.Combine(baseDir, subDir);
                if (Directory.Exists(path))
                {
                    try { dirs.AddRange(Directory.GetDirectories(path)); }
                    catch (Exception ex) { Logger.Error($"AutoChanger: scan error {path}: {ex.Message}"); }
                }
            }

            if (wallpapers != null && wallpapers.Count == dirs.Count) return;

            wallpapers = new List<LibraryModel>();
            foreach (var dir in dirs)
            {
                try
                {
                    var model = wallpaperLibraryFactory.CreateFromDirectory(dir);
                    if (model != null)
                    {
                        // Localize to match UI sort order
                        var localized = LivelyInfoUtil.GetLocalized(model.LivelyInfoLocalizationPath, userSettings.Settings.Language);
                        model.Title = localized?.Title ?? model.LivelyInfo.Title;
                        wallpapers.Add(model);
                    }
                }
                catch { }
            }

            if (wallpapers.Count > 0)
            {
                // Sort by title to match library UI order
                wallpapers.Sort((a, b) => string.Compare(a.Title, b.Title, StringComparison.CurrentCultureIgnoreCase));
                currentIndex = wallpapers.Count - 1;
                shuffled = null;
                shuffleIndex = 0;
                failedWallpapers.Clear();
                Logger.Info($"AutoChanger: loaded {wallpapers.Count} wallpapers from both dirs");
                Logger.Info($"AutoChanger: order preview (first 15): {string.Join(", ", wallpapers.Take(15).Select(w => w.Title))}");
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing) timer.Stop();
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
