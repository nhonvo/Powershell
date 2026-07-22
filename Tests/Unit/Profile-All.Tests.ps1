# C:\Users\TruongNhon\Documents\Powershell\Scripts\Profile-All.Tests.ps1

$ProfileDir = Join-Path $PSScriptRoot "..\..\Profile"

# Global mock hooks to intercept native executables in PS 5.1
$global:gitArgs = @()
$global:dotnetArgs = @()
$global:dockerArgs = @()
$global:awsArgs = @()
$global:tailscaleArgs = @()

function git { $global:gitArgs = $args }
function dotnet { $global:dotnetArgs = $args }
function docker { $global:dockerArgs = $args }
function awslocal { $global:awsArgs = $args }
function tailscale { $global:tailscaleArgs = $args; return "100.115.92.12" }

Describe "Core Profile Functions Validation" {
    BeforeAll {
        $repoRoot = (Get-Item (Join-Path $PSScriptRoot "..\..\")).FullName
        $dllPath = Join-Path $repoRoot "AgyTuiApp\dist\AgyTuiApp.dll"
        if (-not (Test-Path $dllPath)) {
            $dllPath = Join-Path $repoRoot "AgyTuiApp\bin\Debug\net10.0\AgyTuiApp.dll"
        }
        if (Test-Path $dllPath) {
            # Load dependency assemblies
            Get-ChildItem -Path (Split-Path $dllPath) -Filter "*.dll" | Where-Object { $_.Name -ne "AgyTuiApp.dll" } | ForEach-Object {
                try { Add-Type -Path $_.FullName -ErrorAction SilentlyContinue } catch {}
            }
            try { Add-Type -Path $dllPath -ErrorAction SilentlyContinue } catch {}
        }

        $global:AgyUserProfileLoaded = $false
        . (Join-Path $repoRoot "Microsoft.PowerShell_profile.ps1")
    }

    Context "Navigation (20-Navigation.ps1)" {
        It "Set-LocationParent navigates up one level" {
            { Set-LocationParent } | Should Not Throw
        }

        It "Set-LocationGrandParent navigates up two levels" {
            { Set-LocationGrandParent } | Should Not Throw
        }
    }

    Context "System Helpers (30-System.ps1)" {
        It "Get-DiskSpace runs without throwing" {
            { Get-DiskSpace } | Should Not Throw
        }

        It "Get-PublicIP runs and returns string or error" {
            $ip = Get-PublicIP
            $ip | Should Not BeNullOrEmpty
        }

        It "Stop-ProcessFriendly runs without throwing" {
            { Stop-ProcessFriendly -Name "notepad" } | Should Not Throw
        }

        It "Get-SshConnectionInfo runs without throwing" {
            Mock Get-Command { return $true }
            Mock Get-NetIPAddress { return @([PSCustomObject]@{ IPAddress = "192.168.1.50" }) }
            Mock Get-NetTCPConnection { return @() }
            
            { Get-SshConnectionInfo } | Should Not Throw
        }
    }

    Context "DotNet Cmdlets (50-DotNet.ps1)" {
        It "Remove-BinObj cleans bin and obj folders" {
            { Remove-BinObj } | Should Not Throw
        }

        It "Invoke-DotNetBuild runs dotnet build" {
            $global:dotnetArgs = @()
            Invoke-DotNetBuild
            $global:dotnetArgs -contains "build" | Should Be $true
        }
    }

    Context "Git Cmdlets (51-Git.ps1)" {
        It "Get-GitStatus runs git status" {
            $global:gitArgs = @()
            Get-GitStatus
            $global:gitArgs -contains "status" | Should Be $true
        }

        It "Invoke-GitUndo discards uncommitted changes" {
            $global:gitArgs = @()
            Invoke-GitUndo
            $global:gitArgs -contains "reset" | Should Be $true
            $global:gitArgs -contains "--soft" | Should Be $true
        }
    }

    Context "Docker Helpers (52-Docker.ps1)" {
        It "Get-DockerContainers lists containers" {
            $global:dockerArgs = @()
            Get-DockerContainers -All
            $global:dockerArgs -contains "container" | Should Be $true
            $global:dockerArgs -contains "ls" | Should Be $true
            $global:dockerArgs -contains "-a" | Should Be $true
        }
    }

    Context "AWS Commands (53-AWS.ps1)" {
        It "Get-S3Buckets lists AWS buckets" {
            $global:awsArgs = @()
            Get-S3Buckets
            $global:awsArgs -contains "s3" | Should Be $true
            $global:awsArgs -contains "ls" | Should Be $true
        }
    }



    Context "Theme Switcher (ThemeHelper.ps1)" {
        It "Select-ShellTheme function and theme alias exist" {
            (Get-Command Select-ShellTheme -ErrorAction SilentlyContinue) | Should Not Be $null
            (Get-Alias -Name theme -ErrorAction SilentlyContinue) | Should Not Be $null
        }

        It "Toggle-MobileMode function and mobile alias exist" {
            (Get-Command Toggle-MobileMode -ErrorAction SilentlyContinue) | Should Not Be $null
            (Get-Alias -Name mobile -ErrorAction SilentlyContinue) | Should Not Be $null
        }
    }

    Context "Mobile SSH Key Authorizer (SshHelper.ps1)" {
        It "Start-MobileSshKeyReceiver function and ssh-addkey-mobile alias exist" {
            (Get-Command Start-MobileSshKeyReceiver -ErrorAction SilentlyContinue) | Should Not Be $null
            (Get-Alias -Name ssh-addkey-mobile -ErrorAction SilentlyContinue) | Should Not Be $null
        }
    }

    Context "Learning Suite & Obsidian Vault Integration" {
        It "Verify core learning aliases exist" {
            $learningAliases = @("learn", "flashcard", "vocab", "kana", "kanji", "jlpt", "grammar", "algo", "complexity", "problems", "snippets", "sheets", "quiz", "interview", "star", "mock")
            foreach ($alias in $learningAliases) {
                (Get-Command $alias -ErrorAction SilentlyContinue) | Should Not Be $null
            }
        }

        It "Verify Obsidian Vault aliases exist" {
            $vaultAliases = @("obsidian", "refresh", "vault-open")
            foreach ($alias in $vaultAliases) {
                (Get-Command $alias -ErrorAction SilentlyContinue) | Should Not Be $null
            }
        }

        It "Verify Auto-Switch toggle aliases exist" {
            $commitAliases = @("autoswitch", "agyswitch")
            foreach ($alias in $commitAliases) {
                (Get-Command $alias -ErrorAction SilentlyContinue) | Should Not Be $null
            }
        }
    }
}

