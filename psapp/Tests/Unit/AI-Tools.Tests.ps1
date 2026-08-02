# C:\Users\TruongNhon\Documents\Powershell\Scripts\AI-Tools.Tests.ps1

Describe "AI Tools Wrapper Functions" {
    BeforeAll {
        $Global:AgyUserProfileLoaded = $null
        $Global:AiProviderMode = "local"
        $repoRoot = Resolve-Path "$PSScriptRoot\..\..\.." | Select-Object -ExpandProperty Path
        $dllPath = Join-Path $repoRoot "csapp\AgyTui\bin\Debug\net9.0\AgyTui.dll"
        if (-not (Test-Path $dllPath)) {
            $dllPath = Join-Path $repoRoot "csapp\AgyTui\bin\Debug\net10.0\AgyTui.dll"
        }
        if (-not (Test-Path $dllPath)) {
            $dllPath = Join-Path $repoRoot "csapp\AgyTui\dist\AgyTui.dll"
        }
        if ((Test-Path $dllPath) -and -not ('AgyTui.AgyAiCore' -as [type])) {
            try {
                Get-ChildItem -Path (Split-Path $dllPath) -Filter "*.dll" | Where-Object { $_.Name -ne "AgyTui.dll" } | ForEach-Object {
                    try { Add-Type -Path $_.FullName } catch {}
                }
                Add-Type -Path $dllPath
            } catch {}
        }
        $profilePath = Join-Path $repoRoot "Microsoft.PowerShell_profile.ps1"
        if (Test-Path $profilePath) {
            . $profilePath
        }
    }
    
    Context "AI and Control Center Functions" {
        It "defines Invoke-MultiAgent and ai alias" {
            $cmd = Get-Command Invoke-MultiAgent -ErrorAction SilentlyContinue
            $cmd | Should Not Be $null
            (Get-Alias -Name ai -ErrorAction SilentlyContinue) | Should Not Be $null
        }

        It "defines Invoke-ControlCenter and cc alias" {
            $cmd = Get-Command Invoke-ControlCenter -ErrorAction SilentlyContinue
            $cmd | Should Not Be $null
            (Get-Alias -Name cc -ErrorAction SilentlyContinue) | Should Not Be $null
        }

        It "defines Reset-AgyAccountData and reset-agy alias" {
            $cmd = Get-Command Reset-AgyAccountData -ErrorAction SilentlyContinue
            $cmd | Should Not Be $null
            (Get-Alias -Name reset-agy -ErrorAction SilentlyContinue) | Should Not Be $null
        }
    }
}
