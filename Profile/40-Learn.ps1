# 🛸 Profile Module: 40-Learn.ps1
# Learning Suite Router, Obsidian Vault Ingestion, and Study Suite Aliases.

function Invoke-MasterLearningSuite {
    param([string]$Topic)
    $dllPath = Join-Path $global:AGY_WORKSPACE_ROOT "AgyTuiApp\bin\Debug\net10.0\AgyTuiApp.dll"
    if (Test-Path $dllPath) {
        if ($Topic) {
            Start-Process -FilePath "dotnet" -ArgumentList "run --project `"$global:AGY_WORKSPACE_ROOT\AgyTuiApp\AgyTuiApp.csproj`" -- learn $Topic" -NoNewWindow -Wait
        } else {
            Start-Process -FilePath "dotnet" -ArgumentList "run --project `"$global:AGY_WORKSPACE_ROOT\AgyTuiApp\AgyTuiApp.csproj`" -- learn" -NoNewWindow -Wait
        }
    } else {
        Write-Host "⚠️ AgyTuiApp binary not built. Building now..." -ForegroundColor Yellow
        dotnet build "$global:AGY_WORKSPACE_ROOT\AgyTuiApp\AgyTuiApp.csproj" -c Release
    }
}

Set-Alias -Name learn -Value Invoke-MasterLearningSuite -Option ReadOnly, AllScope -ErrorAction SilentlyContinue
Set-Alias -Name obsidian -Value Invoke-MasterLearningSuite -Option ReadOnly, AllScope -ErrorAction SilentlyContinue
Set-Alias -Name refresh -Value Invoke-MasterLearningSuite -Option ReadOnly, AllScope -ErrorAction SilentlyContinue
Set-Alias -Name vault-open -Value Invoke-MasterLearningSuite -Option ReadOnly, AllScope -ErrorAction SilentlyContinue
Set-Alias -Name kana -Value Invoke-MasterLearningSuite -Option ReadOnly, AllScope -ErrorAction SilentlyContinue
Set-Alias -Name kanji -Value Invoke-MasterLearningSuite -Option ReadOnly, AllScope -ErrorAction SilentlyContinue
Set-Alias -Name jlpt -Value Invoke-MasterLearningSuite -Option ReadOnly, AllScope -ErrorAction SilentlyContinue
Set-Alias -Name grammar -Value Invoke-MasterLearningSuite -Option ReadOnly, AllScope -ErrorAction SilentlyContinue
Set-Alias -Name word-of-day -Value Invoke-MasterLearningSuite -Option ReadOnly, AllScope -ErrorAction SilentlyContinue
Set-Alias -Name vocab -Value Invoke-MasterLearningSuite -Option ReadOnly, AllScope -ErrorAction SilentlyContinue
Set-Alias -Name flashcard -Value Invoke-MasterLearningSuite -Option ReadOnly, AllScope -ErrorAction SilentlyContinue
Set-Alias -Name quiz -Value Invoke-MasterLearningSuite -Option ReadOnly, AllScope -ErrorAction SilentlyContinue
Set-Alias -Name snippets -Value Invoke-MasterLearningSuite -Option ReadOnly, AllScope -ErrorAction SilentlyContinue
Set-Alias -Name sheets -Value Invoke-MasterLearningSuite -Option ReadOnly, AllScope -ErrorAction SilentlyContinue
Set-Alias -Name algo -Value Invoke-MasterLearningSuite -Option ReadOnly, AllScope -ErrorAction SilentlyContinue
Set-Alias -Name complexity -Value Invoke-MasterLearningSuite -Option ReadOnly, AllScope -ErrorAction SilentlyContinue
Set-Alias -Name problems -Value Invoke-MasterLearningSuite -Option ReadOnly, AllScope -ErrorAction SilentlyContinue
Set-Alias -Name interview -Value Invoke-MasterLearningSuite -Option ReadOnly, AllScope -ErrorAction SilentlyContinue
Set-Alias -Name star -Value Invoke-MasterLearningSuite -Option ReadOnly, AllScope -ErrorAction SilentlyContinue
Set-Alias -Name mock -Value Invoke-MasterLearningSuite -Option ReadOnly, AllScope -ErrorAction SilentlyContinue
