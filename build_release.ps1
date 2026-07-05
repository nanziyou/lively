# Build and package Lively Wallpaper.
# Usage:
#   .\build_release.ps1                        # Full build + assemble into Release\
#   .\build_release.ps1 -Dev                   # Build + sync changed files to core bin (for dev loop)
#   .\build_release.ps1 -Package               # Full build + zip

param(
    [string]$Configuration = "Release",
    [string]$Platform = "x64",
    [switch]$Dev,
    [switch]$Package
)

$ErrorActionPreference = "Stop"
$root = $PSScriptRoot
$srcDir = Join-Path $root "src\Lively"

# ---- Build ----
Write-Host "=== Building Lively.sln ===" -ForegroundColor Cyan
dotnet build "$srcDir\Lively.sln" -c $Configuration -p:Platform=$Platform
if ($LASTEXITCODE -ne 0) { throw "Build failed" }

$coreOut = Join-Path $srcDir "Lively\bin\$Platform\$Configuration\net9.0-windows10.0.18362.0"
$uiOut   = Join-Path $srcDir "Lively.UI.WinUI\bin\$Platform\$Configuration\net9.0-windows10.0.22621.0"
if (-not (Test-Path $coreOut)) { throw "Core build output not found: $coreOut" }
if (-not (Test-Path $uiOut))   { throw "UI build output not found: $uiOut" }

$copyIfNewer = {
    param($src, $dst)
    $srcTime = (Get-Item $src).LastWriteTime
    $dstTime = if (Test-Path $dst) { (Get-Item $dst).LastWriteTime } else { [DateTime]::MinValue }
    if ($srcTime -gt $dstTime) { Copy-Item $src $dst -Force }
}

if ($Dev) {
    # ---- Dev mode: sync changed binaries into core bin ----
    Write-Host "=== Dev sync to core bin ===" -ForegroundColor Cyan

    $uiDst = Join-Path $coreOut "plugins\UI"
    if (-not (Test-Path $uiDst)) { New-Item -ItemType Directory -Path $uiDst -Force | Out-Null }
    Get-ChildItem $uiOut -File | ForEach-Object { & $copyIfNewer $_.FullName (Join-Path $uiDst $_.Name) }

    @(
        @{N="webview2"; S="Lively.Player.WebView2\bin\$Platform\$Configuration"}
        @{N="wmf";      S="Lively.Player.Wmf\bin\$Platform\$Configuration"}
        @{N="cef";      S="Lively.Player.CefSharp\bin\$Platform\$Configuration"}
        @{N="libvlc";   S="Lively.Player.Vlc\bin\$Platform\$Configuration"}
        @{N="watchdog"; S="Lively.Utility.Watchdog\bin\$Platform\$Configuration"}
    ) | ForEach-Object {
        $s = Join-Path $srcDir $_.S
        if (Test-Path $s) {
            $d = Join-Path $coreOut "plugins\$($_.N)"
            if (-not (Test-Path $d)) { New-Item -ItemType Directory -Path $d -Force | Out-Null }
            Get-ChildItem $s -File | ForEach-Object { & $copyIfNewer $_.FullName (Join-Path $d $_.Name) }
        }
    }

    Write-Host "`nDev sync done. Run: $coreOut\Lively.exe" -ForegroundColor Green
}
else {
    # ---- Release mode: full assembly into Release\ ----
    $outDir = Join-Path $root "Release"
    if (Test-Path $outDir) { Remove-Item -Recurse -Force $outDir }
    New-Item -ItemType Directory -Path $outDir | Out-Null

    Write-Host "=== Copying core ===" -ForegroundColor Cyan
    Copy-Item -Recurse "$coreOut\*" "$outDir\"

    # Remove stale plugins copied from core (replaced by fresh builds below)
    $pluginsDir = Join-Path $outDir "plugins"
    if (Test-Path $pluginsDir) { Remove-Item -Recurse -Force $pluginsDir }

    Write-Host "=== Copying plugins ===" -ForegroundColor Cyan
    $copyPlugin = {
        param($name, $srcRel)
        $s = Join-Path $srcDir $srcRel
        if (Test-Path $s) {
            $d = Join-Path $outDir "plugins\$name"
            New-Item -ItemType Directory -Path $d -Force | Out-Null
            robocopy $s $d /E /NFL /NDL /NJH /NJS > $null
        }
    }

    & $copyPlugin "UI" "Lively.UI.WinUI\bin\$Platform\$Configuration\net9.0-windows10.0.22621.0"
    & $copyPlugin "webview2" "Lively.Player.WebView2\bin\$Platform\$Configuration"
    & $copyPlugin "wmf"      "Lively.Player.Wmf\bin\$Platform\$Configuration"
    & $copyPlugin "cef"      "Lively.Player.CefSharp\bin\$Platform\$Configuration"
    & $copyPlugin "libvlc"   "Lively.Player.Vlc\bin\$Platform\$Configuration"
    & $copyPlugin "watchdog" "Lively.Utility.Watchdog\bin\$Platform\$Configuration"

    # WMF rename fix
    $wrong = Join-Path $outDir "plugins\wmf\Lively.Player.Wmf.exe"
    $right = Join-Path $outDir "plugins\wmf\Lively.PlayerWmf.exe"
    if ((Test-Path $wrong) -and -not (Test-Path $right)) { Rename-Item $wrong $right }

    # Screensaver (flat, not plugin)
    $ssSrc = Join-Path $srcDir "Lively.Utility.Screensaver\bin\$Platform\$Configuration"
    if (Test-Path $ssSrc) { Copy-Item -Recurse "$ssSrc\*" "$outDir\" }

    $count = (Get-ChildItem $outDir -Recurse -File).Count
    Write-Host "`n=== Release ready: $outDir ($count files) ===" -ForegroundColor Green
    Write-Host "Run: $outDir\Lively.exe"

    if ($Package) {
        $zipName = "Lively_Wallpaper_Custom_$(Get-Date -Format yyyyMMdd).zip"
        $zipPath = Join-Path $root $zipName
        if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
        Compress-Archive -Path "$outDir\*" -DestinationPath $zipPath
        Write-Host "`n=== Package: $zipPath ===" -ForegroundColor Green
    }
}
