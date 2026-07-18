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
        . (Join-Path $ProfileDir "Core\TerminalMenu.ps1")
        . (Join-Path $ProfileDir "Helpers\LogHelper.ps1")
        . (Join-Path $ProfileDir "Helpers\0_AgyKeyringHelper.ps1")
        . (Join-Path $ProfileDir "Helpers\AgyAccountManager.ps1")
        . (Join-Path $ProfileDir "Helpers\ProfileNavigator.ps1")
        . (Join-Path $ProfileDir "Helpers\ThemeHelper.ps1")
        . (Join-Path $ProfileDir "Helpers\AiHelper.ps1")
        . (Join-Path $ProfileDir "Helpers\GitHelper.ps1")
        . (Join-Path $ProfileDir "Helpers\DockerHelper.ps1")
        . (Join-Path $ProfileDir "Helpers\SystemHelper.ps1")
        . (Join-Path $ProfileDir "Helpers\SshHelper.ps1")
        . (Join-Path $ProfileDir "Helpers\DotNetHelper.ps1")
        . (Join-Path $ProfileDir "Helpers\AwsHelper.ps1")
        . (Join-Path $ProfileDir "Core\ProfileEnvironment.ps1")
        . (Join-Path $ProfileDir "Core\Projects.ps1")
        . (Join-Path $ProfileDir "Core\ProfileHelp.ps1")
        . (Join-Path $ProfileDir "Core\Aliases.ps1")
    }

    Context "Navigation (20-Navigation.ps1)" {
        It "Set-LocationParent navigates up one level" {
            $global:navigatedTo = $null
            Mock Set-Location { param($Path) $global:navigatedTo = $Path }
            
            Set-LocationParent
            $global:navigatedTo | Should Be ".."
        }

        It "Set-LocationGrandParent navigates up two levels" {
            $global:navigatedTo = $null
            Mock Set-Location { param($Path) $global:navigatedTo = $Path }
            
            Set-LocationGrandParent
            $global:navigatedTo | Should Be "..\.."
        }
    }

    Context "System Helpers (30-System.ps1)" {
        It "Get-DiskSpace outputs table format" {
            $disk = Get-DiskSpace
            $disk | Should Not BeNullOrEmpty
        }

        It "Get-PublicIP runs and returns string or error" {
            $ip = Get-PublicIP
            $ip | Should Not BeNullOrEmpty
        }

        It "Stop-ProcessFriendly stops named process" {
            $global:processStopped = $null
            Mock Stop-Process { param($Name, $Force) $global:processStopped = $Name }
            
            Stop-ProcessFriendly -Name "notepad"
            $global:processStopped | Should Be "notepad"
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
            $global:itemsDeleted = @()
            Mock Remove-Item {
                param($Path, $LiteralPath, $Recurse, $Force)
                if ($Path) { $global:itemsDeleted += $Path }
                if ($LiteralPath) { $global:itemsDeleted += $LiteralPath }
            }
            Mock Test-Path { return $true }
            Mock Get-ChildItem {
                return @(
                    [PSCustomObject]@{ FullName = "C:\proj\bin"; PSPath = "C:\proj\bin" },
                    [PSCustomObject]@{ FullName = "C:\proj\obj"; PSPath = "C:\proj\obj" }
                )
            }
            
            Remove-BinObj
            $global:itemsDeleted -contains "C:\proj\bin" | Should Be $true
            $global:itemsDeleted -contains "C:\proj\obj" | Should Be $true
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

    Context "Antigravity Multi-Account Manager (61-Antigravity.ps1)" {
        It "Get-AgyActiveAccount returns current active account" {
            $env:GEMINI_HOME = "C:\Users\TruongNhon\.gemini_account1"
            $active = [AgyAccountManager]::GetActiveAccount()
            $active | Should Be "account1"
        }

        It "GetAccountDirectory resolves default and custom accounts correctly" {
            [AgyAccountManager]::GetAccountDirectory("default") | Should Be "C:\Users\Public\.gemini"
            [AgyAccountManager]::GetAccountDirectory("fptvttnhon2026") | Should Be "C:\Users\Public\.gemini_fptvttnhon2026"
        }

        It "Set-AgyActiveAccount switches account context" {
            $target = "C:\Users\Public\.gemini_account2"
            Mock Test-Path { return $true } -ParameterFilter { $Path -like "*_account2*" -or $Path -like "*keyring_token.txt*" }
            Mock Out-File { } -ParameterFilter { $FilePath -like "*keyring_token.txt*" }
            Mock ConvertTo-SecureString { } -ParameterFilter { $String -ne $null }
            Mock ConvertFrom-SecureString { } -ParameterFilter { $SecureString -ne $null }
            
            try {
                [AgyAccountManager]::SetActiveAccount("account2", $true)
                $env:GEMINI_HOME | Should Be $target
            } finally {
                if (Test-Path $target) {
                    Remove-Item -Path $target -Recurse -Force -ErrorAction SilentlyContinue
                }
            }
        }

        It "RestoreActiveToken handles missing files gracefully" {
            Mock Test-Path { return $false } -ParameterFilter { $Path -like "*keyring_token.txt*" }
            { [AgyAccountManager]::RestoreActiveToken("nonexistent") } | Should Not Throw
        }

        It "BackupActiveToken handles uninitialized account folder gracefully" {
            Mock Test-Path { return $false } -ParameterFilter { $Path -like "*keyring_token.txt*" -or $Path -like "*_default*" }
            Mock New-Item { return $null } -ParameterFilter { $Path -like "*_default*" }
            { [AgyAccountManager]::BackupActiveToken() } | Should Not Throw
        }

        It "GetAccountMetadata returns default values if no metadata file exists" {
            $meta = [AgyAccountManager]::GetAccountMetadata("nonexistent-test-acc")
            $meta.LastUsed | Should Be "Never"
            $meta.UsageCount | Should Be 0
        }

        It "UpdateAccountMetadata creates metadata file and increments counter" {
            $testAcc = "meta_test_acc"
            $testDir = [AgyAccountManager]::GetAccountDirectory($testAcc)
            $null = New-Item -ItemType Directory -Path $testDir -Force -ErrorAction SilentlyContinue
            
            try {
                [AgyAccountManager]::UpdateAccountMetadata($testAcc)
                $meta = [AgyAccountManager]::GetAccountMetadata($testAcc)
                $meta.UsageCount | Should Be 1
                $meta.LastUsed | Should Not Be "Never"

                [AgyAccountManager]::UpdateAccountMetadata($testAcc)
                $meta = [AgyAccountManager]::GetAccountMetadata($testAcc)
                $meta.UsageCount | Should Be 2
            } finally {
                if (Test-Path $testDir) {
                    Remove-Item -Path $testDir -Recurse -Force -ErrorAction SilentlyContinue
                }
            }
        }

        It "GetPrivateDirectorySize correctly measures files and skips junctions" {
            $testAcc = "size_test_acc"
            $testDir = [AgyAccountManager]::GetAccountDirectory($testAcc)
            $null = New-Item -ItemType Directory -Path $testDir -Force -ErrorAction SilentlyContinue
            $file1 = Join-Path $testDir "file1.txt"
            [System.IO.File]::WriteAllText($file1, "hello")
            
            $subPath = Join-Path $testDir "config"
            $srcPath = Join-Path ([AgyAccountManager]::AgySourceHome) "config"
            if (-not (Test-Path $srcPath)) {
                $null = New-Item -ItemType Directory -Path $srcPath -Force -ErrorAction SilentlyContinue
            }
            if (-not (Test-Path $subPath)) {
                $null = New-Item -ItemType Junction -Path $subPath -Value $srcPath -Force -ErrorAction SilentlyContinue
            }
            
            $srcFile = Join-Path $srcPath "shared_file.txt"
            [System.IO.File]::WriteAllText($srcFile, "shared data")
            
            try {
                $size = [AgyAccountManager]::GetPrivateDirectorySize($testDir)
                $file1Size = (Get-Item $file1).Length
                $size | Should Be $file1Size
            } finally {
                if (Test-Path $subPath) {
                    cmd /c rmdir "$subPath"
                }
                if (Test-Path $testDir) {
                    Remove-Item -Path $testDir -Recurse -Force -ErrorAction SilentlyContinue
                }
                if (Test-Path $srcFile) {
                    Remove-Item -Path $srcFile -Force -ErrorAction SilentlyContinue
                }
            }
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

    Context "New TUI Features Validation" {
        It "LogHelper::InvokeWithSpinner runs and completes action successfully" {
            $value = [LogHelper]::InvokeWithSpinner("Testing spinner", { return 42 })
            $value | Should Be 42
        }

        It "AiHelper::ShowAiDashboard is defined as a static method" {
            $method = [AiHelper].GetMethod("ShowAiDashboard")
            $method | Should Not Be $null
        }

        It "ProfileNavigator::LaunchTerminalIde is defined as a static method" {
            $method = [ProfileNavigator].GetMethod("LaunchTerminalIde")
            $method | Should Not Be $null
        }
    }
}

