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
            $script:initCalled = $false
            Mock Initialize-OllamaServer { $script:initCalled = $true }
            
            Ensure-OllamaServer
            $script:initCalled | Should Be $true
        }
    }

    Context "Initialize-OllamaServer" {
        It "kills existing process on port 11434 and starts server" {
            # Mock Get-NetTCPConnection to return an owning process
            Mock Get-NetTCPConnection {
                return [PSCustomObject]@{ OwningProcess = 99999 }
            }
            $script:killed = $false
            Mock Get-Process {
                return [PSCustomObject]@{ Id = 99999; Name = "ollama" }
            }
            Mock Stop-Process { $script:killed = $true }
            
            # Mock Start-Process and REST method
            $script:started = $false
            Mock Start-Process { $script:started = $true }
            Mock Invoke-RestMethod { return "Ollama is running" }
            
            Initialize-OllamaServer
            
            $script:killed | Should Be $true
            $script:started | Should Be $true
        }
    }
    
    Context "Invoke-Claude-By-Ollama" {
        It "configures NODE_OPTIONS and launches claude with default model" {
            Mock Ensure-OllamaServer {}
            
            $script:commandRun = $null
            $script:argsPassed = $null
            
            Mock ollama.exe {
                $script:commandRun = "ollama.exe"
                $script:argsPassed = $args
            }
            
            Invoke-Claude-By-Ollama
            
            $script:commandRun | Should Be "ollama.exe"
            $script:argsPassed -contains "launch" | Should Be $true
            $script:argsPassed -contains "claude" | Should Be $true
            $script:argsPassed -contains "--model" | Should Be $true
            $script:argsPassed -contains "qwen3:1.7b" | Should Be $true
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
            
            $script:commandRun = $null
            $script:argsPassed = $null
            
            Mock codex.cmd {
                $script:commandRun = "codex.cmd"
                $script:argsPassed = $args
            }
            
            Invoke-Codex-By-Ollama
            
            $script:commandRun | Should Be "codex.cmd"
            $script:argsPassed -contains "--oss" | Should Be $true
            $script:argsPassed -contains "--local-provider" | Should Be $true
            $script:argsPassed -contains "ollama" | Should Be $true
            $script:argsPassed -contains "--model" | Should Be $true
            $script:argsPassed -contains "qwen3:1.7b" | Should Be $true
        }
    }

    Context "Invoke-OpenClaw-By-Ollama" {
        It "launches openclaw with the default model" {
            Mock Ensure-OllamaServer {}
            
            $script:commandRun = $null
            $script:argsPassed = $null
            
            Mock ollama.exe {
                $script:commandRun = "ollama.exe"
                $script:argsPassed = $args
            }
            
            Invoke-OpenClaw-By-Ollama
            
            $script:argsPassed -contains "launch" | Should Be $true
            $script:argsPassed -contains "openclaw" | Should Be $true
            $script:argsPassed -contains "qwen3:1.7b" | Should Be $true
        }
    }

    Context "Invoke-Hermes-By-Ollama" {
        It "launches hermes with the default model" {
            Mock Ensure-OllamaServer {}
            
            $script:commandRun = $null
            $script:argsPassed = $null
            
            Mock ollama.exe {
                $script:commandRun = "ollama.exe"
                $script:argsPassed = $args
            }
            
            Invoke-Hermes-By-Ollama
            
            $script:argsPassed -contains "launch" | Should Be $true
            $script:argsPassed -contains "hermes" | Should Be $true
            $script:argsPassed -contains "qwen3:1.7b" | Should Be $true
        }
    }

    Context "Install-AIIntegrations" {
        It "installs missing integrations via npm" {
            Mock Get-Command { return $null }
            
            $script:npmCalls = @()
            Mock npm {
                $script:npmCalls += ,$args
            }
            
            Install-AIIntegrations
            
            $script:npmCalls.Count | Should Be 3
            $script:npmCalls[0] -contains "@anthropic-ai/claude-code" | Should Be $true
            $script:npmCalls[1] -contains "@openai/codex" | Should Be $true
            $script:npmCalls[2] -contains "openclaw" | Should Be $true
        }
    }
}
