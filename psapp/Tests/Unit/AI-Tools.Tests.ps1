# C:\Users\TruongNhon\Documents\Powershell\Scripts\AI-Tools.Tests.ps1

Describe "AI Tools Wrapper Functions" {
    BeforeAll {
        $Global:AgyUserProfileLoaded = $null
        $Global:AiProviderMode = "local"
        $repoRoot = Resolve-Path "$PSScriptRoot\..\..\.." | Select-Object -ExpandProperty Path
        $dllPath = Join-Path $repoRoot "csapp\AgyTuiApp\bin\Debug\net10.0\AgyTuiApp.dll"
        if (-not (Test-Path $dllPath)) {
            $dllPath = Join-Path $repoRoot "csapp\AgyTuiApp\dist\AgyTuiApp.dll"
        }
        if ((Test-Path $dllPath) -and -not ('AgyTui.AgyAiCore' -as [type])) {
            try {
                Get-ChildItem -Path (Split-Path $dllPath) -Filter "*.dll" | Where-Object { $_.Name -ne "AgyTuiApp.dll" } | ForEach-Object {
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
    
    Context "Ollama Helpers" {
        It "defines Ensure-OllamaServer mapping to AgyAiCore" {
            $cmd = Get-Command Ensure-OllamaServer -ErrorAction SilentlyContinue
            $cmd | Should Not Be $null
            $cmd.Definition | Should Match "AgyAiCore"
        }

        It "defines Initialize-OllamaServer mapping to AgyAiCore" {
            $cmd = Get-Command Initialize-OllamaServer -ErrorAction SilentlyContinue
            $cmd | Should Not Be $null
            $cmd.Definition | Should Match "AgyAiCore"
        }
    }
    

    Context "AI Wrapper Mappings" {
        It "defines Invoke-Claude-By-Ollama wrapper mapping to AgyAiCore" {
            $cmd = Get-Command Invoke-Claude-By-Ollama -ErrorAction SilentlyContinue
            $cmd | Should Not Be $null
            $cmd.Definition | Should Match "Invoke-AiTool|AgyAiCore"
        }

        It "defines Invoke-Codex-By-Ollama wrapper mapping to AgyAiCore" {
            $cmd = Get-Command Invoke-Codex-By-Ollama -ErrorAction SilentlyContinue
            $cmd | Should Not Be $null
            $cmd.Definition | Should Match "Invoke-AiTool|AgyAiCore"
        }

        It "defines Invoke-OpenClaw-By-Ollama wrapper mapping to AgyAiCore" {
            $cmd = Get-Command Invoke-OpenClaw-By-Ollama -ErrorAction SilentlyContinue
            $cmd | Should Not Be $null
            $cmd.Definition | Should Match "Invoke-AiTool|AgyAiCore"
        }

        It "defines Invoke-Hermes-By-Ollama wrapper mapping to AgyAiCore" {
            $cmd = Get-Command Invoke-Hermes-By-Ollama -ErrorAction SilentlyContinue
            $cmd | Should Not Be $null
            $cmd.Definition | Should Match "Invoke-AiTool|AgyAiCore"
        }

        It "defines Install-AIIntegrations wrapper mapping to AgyAiCore" {
            $cmd = Get-Command Install-AIIntegrations -ErrorAction SilentlyContinue
            $cmd | Should Not Be $null
            $cmd.Definition | Should Match "AgyAiCore"
        }
    }
}
