Describe "Publish Release Script Tests" {
    Context "Project Script Paths" {
        It "build-release.ps1 exists and references valid build scripts" {
            $repoRoot = (Get-Item (Join-Path $PSScriptRoot "..\..\..\")).FullName
            $scriptPath = Join-Path $repoRoot "scripts\build-release.ps1"
            if (-not (Test-Path $scriptPath)) { $scriptPath = Join-Path $repoRoot "build-release.ps1" }
            Test-Path $scriptPath | Should Be $true

            $buildAppsScript = Join-Path $repoRoot "scripts\build-apps.ps1"
            Test-Path $buildAppsScript | Should Be $true
        }
    }
}
