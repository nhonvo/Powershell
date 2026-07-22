<#
.SYNOPSIS
    Script to downsize / compress video files using FFmpeg.
.DESCRIPTION
    Downsizes video resolution (e.g. to 1080p, 720p, 480p) and applies efficient H.264 / H.265 encoding to shrink file size.
.EXAMPLE
    .\compress_video.ps1 -InputPath "C:\path\to\video.mp4" -TargetResolution "720p" -Quality "medium"
.EXAMPLE
    .\compress_video.ps1 -InputDirectory "C:\path\to\videos" -TargetResolution "1080p"
#>

param (
    [Parameter(Mandatory=$false)]
    [string]$InputPath,

    [Parameter(Mandatory=$false)]
    [string]$InputDirectory,

    [Parameter(Mandatory=$false)]
    [ValidateSet("1080p", "720p", "480p", "original")]
    [string]$TargetResolution = "720p",

    [Parameter(Mandatory=$false)]
    [ValidateSet("high", "medium", "low")]
    [string]$Quality = "medium",

    [Parameter(Mandatory=$false)]
    [string]$Codec = "libx264" # Use "libx265" for even smaller size if supported
)

# 1. Check if FFmpeg is installed
$ffmpegCmd = Get-Command ffmpeg -ErrorAction SilentlyContinue
if (-not $ffmpegCmd) {
    Write-Host "[!] FFmpeg is not installed or not in PATH." -ForegroundColor Yellow
    Write-Host "    You can install it automatically using WinGet by running:" -ForegroundColor Cyan
    Write-Host "    winget install Gyan.FFmpeg" -ForegroundColor Green
    Write-Host ""
    $installNow = Read-Host "Would you like to install FFmpeg now via WinGet? (Y/N)"
    if ($installNow -eq 'Y' -or $installNow -eq 'y') {
        winget install Gyan.FFmpeg --accept-package-agreements --accept-source-agreements
        $env:Path = [System.Environment]::GetEnvironmentVariable("Path","Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path","User")
        $ffmpegCmd = Get-Command ffmpeg -ErrorAction SilentlyContinue
        if (-not $ffmpegCmd) {
            Write-Host "[X] FFmpeg installed, but please restart PowerShell to refresh PATH." -ForegroundColor Yellow
            return
        }
    } else {
        return
    }
}

# Map CRF quality values (lower CRF = higher quality / larger file, higher CRF = smaller file)
$crfMap = @{
    "high"   = 21
    "medium" = 26
    "low"    = 30
}
$crf = $crfMap[$Quality]

# Map Scale filters
$scaleMap = @{
    "1080p"    = "scale=-2:1080"
    "720p"     = "scale=-2:720"
    "480p"     = "scale=-2:480"
    "original" = ""
}
$scaleFilter = $scaleMap[$TargetResolution]

function Compress-SingleFile {
    param ([string]$file)

    if (-not (Test-Path $file)) {
        Write-Host "[X] File not found: $file" -ForegroundColor Red
        return
    }

    $item = Get-Item $file
    $outputFile = Join-Path $item.DirectoryName ($item.BaseName + "_compressed" + $item.Extension)

    Write-Host "--------------------------------------------------" -ForegroundColor Cyan
    Write-Host "Compressing: $($item.Name)" -ForegroundColor Yellow
    Write-Host "Original Size: $([math]::Round($item.Length / 1MB, 2)) MB"
    Write-Host "Resolution: $TargetResolution | Quality Preset: $Quality (CRF: $crf)"

    $ffmpegArgs = @("-y", "-i", "`"$file`"")

    if ($scaleFilter -ne "") {
        $ffmpegArgs += @("-vf", $scaleFilter)
    }

    $ffmpegArgs += @(
        "-c:v", $Codec,
        "-crf", $crf,
        "-preset", "faster",
        "-c:a", "aac",
        "-b:a", "128k",
        "`"$outputFile`""
    )

    $cmdLine = "ffmpeg " + ($ffmpegArgs -join " ")
    Invoke-Expression $cmdLine

    if (Test-Path $outputFile) {
        $newItem = Get-Item $outputFile
        $origMb = [math]::Round($item.Length / 1MB, 2)
        $newMb = [math]::Round($newItem.Length / 1MB, 2)
        $savedPercent = [math]::Round((1 - ($newItem.Length / $item.Length)) * 100, 1)

        Write-Host "--------------------------------------------------" -ForegroundColor Green
        Write-Host "[✓] Compression Completed!" -ForegroundColor Green
        Write-Host "Output File : $($newItem.FullName)"
        Write-Host "New Size    : $newMb MB (Reduced by $savedPercent%)" -ForegroundColor Green
    } else {
        Write-Host "[X] Compression failed." -ForegroundColor Red
    }
}

if ($InputPath) {
    Compress-SingleFile -file $InputPath
} elseif ($InputDirectory) {
    $videoExtensions = "*.mp4", "*.mkv", "*.mov", "*.avi", "*.wmv"
    $files = Get-ChildItem -Path $InputDirectory -Include $videoExtensions -Recurse | Where-Object { $_.Name -notlike "*_compressed*" }
    Write-Host "Found $($files.Count) video file(s) in $InputDirectory" -ForegroundColor Cyan
    foreach ($f in $files) {
        Compress-SingleFile -file $f.FullName
    }
} else {
    Write-Host "Usage Examples:" -ForegroundColor Yellow
    Write-Host "  Compress single file:    .\compress_video.ps1 -InputPath 'C:\path\to\video.mp4' -TargetResolution '720p'"
    Write-Host "  Compress folder:         .\compress_video.ps1 -InputDirectory 'C:\path\to\videos' -Quality 'medium'"
}
