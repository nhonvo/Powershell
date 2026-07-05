# C:\Users\TruongNhon\Documents\Powershell\Scripts\AI-Tools.Tests.ps1

$profilePath = Join-Path $PSScriptRoot "..\Profile\60-AI.ps1"

Describe "AI Tools Wrapper Functions" {
    # Import functions under test
    . $profilePath
    
    Context "Ensure-OllamaServer" {
        It "does not restart the server if Ollama is running" {
            Mock Invoke-RestMethod { return "Ollama is running" }
            Mock Initialize-OllamaServer { throw "Should not be called" }
            
            Ensure-OllamaServer
        }
        
        It "restarts the server if Ollama is not running" {
            Mock Invoke-RestMethod { throw "Connection refused" }
            $global:initCalled = $false
            Mock Initialize-OllamaServer { $global:initCalled = $true }
            
            Ensure-OllamaServer
            $global:initCalled | Should Be $true
        }
    }

    Context "Initialize-OllamaServer" {
        It "kills existing process on port 11434 and starts server" {
            # Mock Get-NetTCPConnection to return an owning process
            Mock Get-NetTCPConnection {
                return [PSCustomObject]@{ OwningProcess = 99999 }
            }
            $global:killed = $false
            Mock Get-Process {
                return [PSCustomObject]@{ Id = 99999; Name = "ollama" }
            }
            Mock Stop-Process { $global:killed = $true }
            
            # Mock Start-Process and REST method
            $global:started = $false
            Mock Start-Process { $global:started = $true }
            Mock Invoke-RestMethod { return "Ollama is running" }
            
            Initialize-OllamaServer
            
            $global:killed | Should Be $true
            $global:started | Should Be $true
        }
    }
    
    Context "Invoke-Claude-By-Ollama" {
        It "configures NODE_OPTIONS and launches claude with default model" {
            Mock Ensure-OllamaServer {}
            
            $global:commandRun = $null
            $global:argsPassed = $null
            
            Mock ollama.exe {
                $global:commandRun = "ollama.exe"
                $global:argsPassed = $args
            }
            
            Invoke-Claude-By-Ollama
            
            $global:commandRun | Should Be "ollama.exe"
            $global:argsPassed -contains "launch" | Should Be $true
            $global:argsPassed -contains "claude" | Should Be $true
            $global:argsPassed -contains "--model" | Should Be $true
            $global:argsPassed -contains "qwen3:1.7b" | Should Be $true
        }

        It "passes custom parameters and handles NODE_OPTIONS cleanup" {
            Mock Ensure-OllamaServer {}
            Mock ollama.exe {}
            
            $env:NODE_OPTIONS = "existing-options"
            
            Invoke-Claude-By-Ollama --model my-model --help
            
            $env:NODE_OPTIONS | Should Be "existing-options"
        }
    }

    Context "Invoke-Codex-By-Ollama" {
        It "sets environment variables and launches codex" {
            Mock Ensure-OllamaServer {}
            
            $global:commandRun = $null
            $global:argsPassed = $null
            
            Mock codex.cmd {
                $global:commandRun = "codex.cmd"
                $global:argsPassed = $args
            }
            
            Invoke-Codex-By-Ollama
            
            $global:commandRun | Should Be "codex.cmd"
            $global:argsPassed -contains "--oss" | Should Be $true
            $global:argsPassed -contains "--local-provider" | Should Be $true
            $global:argsPassed -contains "ollama" | Should Be $true
            $global:argsPassed -contains "--model" | Should Be $true
            $global:argsPassed -contains "qwen3:1.7b" | Should Be $true
        }
    }

    Context "Invoke-OpenClaw-By-Ollama" {
        It "launches openclaw with the default model" {
            Mock Ensure-OllamaServer {}
            
            $global:commandRun = $null
            $global:argsPassed = $null
            
            Mock ollama.exe {
                $global:commandRun = "ollama.exe"
                $global:argsPassed = $args
            }
            
            Invoke-OpenClaw-By-Ollama
            
            $global:argsPassed -contains "launch" | Should Be $true
            $global:argsPassed -contains "openclaw" | Should Be $true
            $global:argsPassed -contains "qwen3:1.7b" | Should Be $true
        }
    }

    Context "Invoke-Hermes-By-Ollama" {
        It "launches hermes with the default model" {
            Mock Ensure-OllamaServer {}
            
            $global:commandRun = $null
            $global:argsPassed = $null
            
            Mock ollama.exe {
                $global:commandRun = "ollama.exe"
                $global:argsPassed = $args
            }
            
            Invoke-Hermes-By-Ollama
            
            $global:argsPassed -contains "launch" | Should Be $true
            $global:argsPassed -contains "hermes" | Should Be $true
            $global:argsPassed -contains "qwen3:1.7b" | Should Be $true
        }
    }

    Context "Install-AIIntegrations" {
        It "installs missing integrations via npm" {
            Mock Get-Command { return $null }
            
            $global:npmCalls = @()
            Mock Invoke-Npm {
                $global:npmCalls += ,$ArgsList
            }
            
            Install-AIIntegrations
            
            $global:npmCalls.Count | Should Be 3
            $global:npmCalls[0] -contains "@anthropic-ai/claude-code" | Should Be $true
            $global:npmCalls[1] -contains "@openai/codex" | Should Be $true
            $global:npmCalls[2] -contains "openclaw" | Should Be $true
        }
    }
}
