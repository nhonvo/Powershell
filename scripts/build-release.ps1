# build-release.ps1 — Master Production Release Publish Script
[CmdletBinding()]
param(
    [string]$OutputDir = "",
    [string]$Version,
    [string]$Command,
    [switch]$SkipTests = $false
)

$ErrorActionPreference = 'Stop'
$repoRoot = $PSScriptRoot
if (-not (Test-Path (Join-Path $repoRoot "archive\agytui\AgyTui\AgyTui.csproj")) -and -not (Test-Path (Join-Path $repoRoot "apps\agytui\AgyTui\AgyTui.csproj")) -and -not (Test-Path (Join-Path $repoRoot "csapp\AgyTui\AgyTui.csproj"))) {
    $repoRoot = Split-Path -Parent $PSScriptRoot
}
$projectRelPath = if (Test-Path (Join-Path $repoRoot "archive\agytui\AgyTui\AgyTui.csproj")) { "archive/agytui/AgyTui/AgyTui.csproj" } elseif (Test-Path (Join-Path $repoRoot "apps\agytui\AgyTui\AgyTui.csproj")) { "apps/agytui/AgyTui/AgyTui.csproj" } else { "csapp/AgyTui/AgyTui.csproj" }
$testProjectRelPath = if (Test-Path (Join-Path $repoRoot "archive\agytui\AgyTui.Tests\AgyTui.Tests.csproj")) { "archive/agytui/AgyTui.Tests/AgyTui.Tests.csproj" } elseif (Test-Path (Join-Path $repoRoot "apps\agytui\AgyTui.Tests\AgyTui.Tests.csproj")) { "apps/agytui/AgyTui.Tests/AgyTui.Tests.csproj" } else { "csapp/AgyTui.Tests/AgyTui.Tests.csproj" }
if ([string]::IsNullOrWhiteSpace($OutputDir)) {
    $OutputDir = (Split-Path $projectRelPath) + "/dist"
}
$projectPath = Join-Path $repoRoot ($projectRelPath -replace '/', '\')
$testProjectPath = Join-Path $repoRoot ($testProjectRelPath -replace '/', '\')

function Unlock-Binaries {
    param([string]$Dir)
    if (Test-Path $Dir) {
        Get-ChildItem -Path $Dir -Include "*.dll", "*.exe", "*.pdb" -Recurse -ErrorAction SilentlyContinue | ForEach-Object {
            $rand = Get-Random
            Rename-Item -Path $_.FullName -NewName "$($_.Name).old_$rand" -Force -ErrorAction SilentlyContinue
        }
    }
}

Write-Host "🚀 Publishing AgyTui [Production Release]..." -ForegroundColor Cyan
if (![string]::IsNullOrEmpty($Version)) {
    Write-Host "📌 Release Version: $Version" -ForegroundColor Yellow
}

pushd $repoRoot
try {
    $projectDir = Split-Path -Parent $projectPath
    Unlock-Binaries -Dir (Join-Path $projectDir "bin")
    Unlock-Binaries -Dir (Join-Path $projectDir "obj")
    Unlock-Binaries -Dir (Join-Path $repoRoot $OutputDir)

    if (-not $SkipTests) {
        Write-Host "🧪 Executing C# unit test suite validation..." -ForegroundColor Cyan
        dotnet test "$testProjectPath" -c Release --verbosity quiet
        if ($LASTEXITCODE -ne 0) {
            Write-Error "❌ C# unit tests failed. Aborting release publish."
            return
        }

        Write-Host "🔨 Building AgyTui assembly for PowerShell test validation..." -ForegroundColor Cyan
        dotnet build "$projectPath" -c Release --verbosity quiet
        if ($LASTEXITCODE -ne 0) {
            Write-Error "❌ Assembly build failed. Aborting release publish."
            return
        }

        Write-Host "🧪 Validating PowerShell Profile & PS1 C# Type References..." -ForegroundColor Cyan
        $runTests = Join-Path $repoRoot "shell\tests\run_tests.ps1"
        if (-not (Test-Path $runTests)) { $runTests = Join-Path $repoRoot "psapp\Tests\run_tests.ps1" }
        if (Test-Path $runTests) {
            pwsh -NoProfile -ExecutionPolicy Bypass -File $runTests
            if ($LASTEXITCODE -ne 0) {
                Write-Error "❌ PowerShell profile type reference validation failed. Aborting release publish."
                return
            }
        }
    }

    Unlock-Binaries -Dir (Join-Path $repoRoot $OutputDir)

    dotnet publish "$projectPath" `
        -c Release `
        -r win-x64 `
        --self-contained true `
        -p:PublishSingleFile=true `
        -p:EnableCompressionInSingleFile=true `
        -p:IncludeNativeLibrariesForSelfExtract=true `
        -o $OutputDir

    if ($LASTEXITCODE -eq 0) {
        Write-Host "📦 Copying AgyTui assembly DLLs to $OutputDir for PowerShell profile type integration..." -ForegroundColor Cyan
        $binRelease = Join-Path $projectDir "bin/Release/net9.0"
        if (-not (Test-Path $binRelease)) {
            $binRelease = Join-Path $projectDir "bin/Release/net10.0"
        }
        if (Test-Path $binRelease) {
            Get-ChildItem -Path $binRelease -Filter "*.dll" | ForEach-Object {
                Copy-Item -Path $_.FullName -Destination $OutputDir -Force -ErrorAction SilentlyContinue
            }
        }
        Write-Host "✅ Release Publish Succeeded! Single-file binary located at $OutputDir\AgyTui.exe and assembly at $OutputDir\AgyTui.dll" -ForegroundColor Green

        if (-not [string]::IsNullOrWhiteSpace($Command)) {
            Write-Host "🚀 Launching target command: '$Command'..." -ForegroundColor Yellow
            $exe = Join-Path $repoRoot "$OutputDir\AgyTui.exe"
            if (Test-Path $exe) {
                & $exe $Command
            }
        }
    } else {
        Write-Error "❌ Release Publish Failed."
    }
} finally {
    if ($projectDir -and (Test-Path $projectDir)) {
        Get-ChildItem -Path $projectDir -Filter "*.old_*" -Recurse -ErrorAction SilentlyContinue | Remove-Item -Force -ErrorAction SilentlyContinue
    }
    popd
}
