# build-release.ps1 — Compile single-file Production Release binary
[CmdletBinding()]
param(
    [string]$OutputDir = "csapp/AgyTui/dist"
)

Write-Host "📦 Publishing AgyTui single-file Production Release binary..." -ForegroundColor Cyan

dotnet publish csapp/AgyTui/AgyTui.csproj `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:EnableCompressionInSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -o $OutputDir

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Production release binary built successfully in $OutputDir\AgyTui.exe!" -ForegroundColor Green
} else {
    Write-Host "❌ Failed to publish release binary." -ForegroundColor Red
}
