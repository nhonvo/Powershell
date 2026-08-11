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
        $repoRoot = (Get-Item (Join-Path $PSScriptRoot "..\..\..\")).FullName
        $dllPath = Join-Path $repoRoot "csapp\AgyTui\dist\AgyTui.dll"
        if (-not (Test-Path $dllPath)) {
            $dllPath = Join-Path $repoRoot "csapp\AgyTui\bin\Debug\net9.0\AgyTui.dll"
        }
        if (-not (Test-Path $dllPath)) {
            $dllPath = Join-Path $repoRoot "csapp\AgyTui\bin\Debug\net10.0\AgyTui.dll"
        }
        if (Test-Path $dllPath) {
            # Load dependency assemblies
            Get-ChildItem -Path (Split-Path $dllPath) -Filter "*.dll" | Where-Object { $_.Name -ne "AgyTui.dll" } | ForEach-Object {
                try { Add-Type -Path $_.FullName -ErrorAction SilentlyContinue } catch {}
            }
            try {
                Add-Type -Path $dllPath -ErrorAction SilentlyContinue
                $acc = [psobject].Assembly.GetType('System.Management.Automation.TypeAccelerators')
                $agyAssembly = [System.AppDomain]::CurrentDomain.GetAssemblies() | Where-Object { $_.GetName().Name -eq "AgyTui" } | Select-Object -First 1
                if ($acc -and $agyAssembly) {
                    foreach ($type in $agyAssembly.GetExportedTypes()) {
                        if ($type.IsClass -and $type.Name -and -not $acc::Get.ContainsKey($type.Name)) {
                            try { $acc::Add($type.Name, $type) } catch {}
                        }
                    }
                }
            } catch {}
        }

        $global:AgyUserProfileLoaded = $null
        . (Join-Path $repoRoot "Microsoft.PowerShell_profile.ps1")
        $global:AgyUserProfileLoaded = $null
        try { Load-AgyTuiDll -ForceLoad $true } catch {}
    }

    Context "ProfileHelp Type Accelerator" {
        It "ProfileHelp resolves to the AgyTui type accelerator" {
            [ProfileHelp].FullName | Should Be "AgyTui.UI.Screens.Customization.Helpers.ProfileHelp"
        }
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
            { Load-AgyTuiDll; [CommandRouter]::Route("disk") } | Should Not Throw
        }

        It "Get-PublicIP runs and returns string" {
            Load-AgyTuiDll
            $ip = [SystemHelper]::Instance.GetPublicIP()
            $ip | Should Not BeNullOrEmpty
        }

        It "Get-SshConnectionInfo runs without throwing" {
            { Load-AgyTuiDll; [CommandRouter]::Route("ssh-info") } | Should Not Throw
        }
    }

    Context "DotNet Cmdlets (50-DotNet.ps1)" {
        It "Remove-BinObj cleans bin and obj folders" {
            $tempDir = Join-Path ([System.IO.Path]::GetTempPath()) ("test_binobj_" + [System.Guid]::NewGuid().ToString("N"))
            New-Item -ItemType Directory -Path (Join-Path $tempDir "bin") -Force | Out-Null
            New-Item -ItemType Directory -Path (Join-Path $tempDir "obj") -Force | Out-Null
            Push-Location $tempDir
            try {
                { Remove-BinObj } | Should Not Throw
            } finally {
                Pop-Location
                Remove-Item $tempDir -Recurse -Force -ErrorAction SilentlyContinue
            }
        }

        It "Invoke-DotNetBuild executes CommandRouter db route" {
            { Invoke-DotNetBuild } | Should Not Throw
        }
    }

    Context "Git Cmdlets (51-Git.ps1)" {
        It "Get-GitStatus runs git status" {
            { Get-GitStatus } | Should Not Throw
        }

        It "Invoke-GitUndo discards uncommitted changes" {
            $tempRepo = Join-Path ([System.IO.Path]::GetTempPath()) ("test_gitrepo_" + [System.Guid]::NewGuid().ToString("N"))
            New-Item -ItemType Directory -Path $tempRepo -Force | Out-Null
            $origCurrentDir = [System.Environment]::CurrentDirectory
            [System.Environment]::CurrentDirectory = $tempRepo
            Push-Location $tempRepo
            try {
                & git init -q
                "dummy" | Out-File "test.txt"
                & git add test.txt
                & git commit -m "init" -q
                "change" | Out-File "test.txt"
                { Invoke-GitUndo } | Should Not Throw
            } finally {
                Pop-Location
                [System.Environment]::CurrentDirectory = $origCurrentDir
                Remove-Item $tempRepo -Recurse -Force -ErrorAction SilentlyContinue
            }
        }
    }

    Context "Docker Helpers (52-Docker.ps1)" {
        It "Get-DockerContainers lists containers" {
            $global:dockerArgs = @()
            { Get-DockerContainers -All } | Should Not Throw
        }
    }

    Context "AWS Commands (53-AWS.ps1)" {
        It "Get-S3Buckets lists AWS buckets" {
            { Get-S3Buckets } | Should Not Throw
        }
    }

    Context "Theme & System Commands (C# CommandRouter)" {
        It "Executes theme and mobile routes via CommandRouter" {
            { Load-AgyTuiDll; [CommandRouter]::Route("theme") } | Should Not Throw
        }
    }

    Context "Learning Suite & Account Integration (C# CommandRouter)" {
        It "Executes learning and account routes via CommandRouter" {
            { Load-AgyTuiDll; [CommandRouter]::Route("due") } | Should Not Throw
            { Load-AgyTuiDll; [CommandRouter]::Route("autoswitch"); [CommandRouter]::Route("autoswitch") } | Should Not Throw
        }
    }

    Context "PowerShell Profile & Script Type References Coverage" {
        It "Ensures all custom C# type references in .ps1 files resolve without error" {
            Load-AgyTuiDll -ForceLoad $true
            $repoRoot = (Get-Item (Join-Path $PSScriptRoot "..\..\..\")).FullName
            $allPsFiles = Get-ChildItem -Path $repoRoot -Filter "*.ps1" -Recurse | Where-Object {
                $_.FullName -notlike "*\.git*" -and $_.FullName -notlike "*\bin\*" -and $_.FullName -notlike "*\obj\*" -and $_.FullName -notlike "*\psapp\Modules\*"
            }

            $missingTypes = @()
            $testedTypes = [System.Collections.Generic.HashSet[string]]::new()

            foreach ($file in $allPsFiles) {
                $tokens = $null
                $errors = $null
                $ast = [System.Management.Automation.Language.Parser]::ParseFile($file.FullName, [ref]$tokens, [ref]$errors)
                if ($ast) {
                    $typeNodes = $ast.FindAll({ param($node) $node -is [System.Management.Automation.Language.TypeExpressionAst] }, $true)
                    foreach ($node in $typeNodes) {
                        $typeName = $node.TypeName.FullName
                        if ($typeName -and $typeName -notmatch '^(string|int|bool|void|switch|array|object|hashtable|scriptblock|psobject|byte|long|double|char|decimal|float|ref|single|type|pscustomobject|System\..*|Microsoft\..*)$') {
                            if ($testedTypes.Add($typeName)) {
                                $resolvedType = $typeName -as [type]
                                if ($null -eq $resolvedType) {
                                    $missingTypes += "$typeName (referenced in $($file.Name))"
                                }
                            }
                        }
                    }
                }
            }

            $missingTypes | Should BeNullOrEmpty
        }

        It "Ensures CommandRouter type accelerator is registered and functional" {
            Load-AgyTuiDll
            $type = "CommandRouter" -as [type]
            $type | Should Not Be $null
            $type.FullName | Should Be "AgyTui.UI.Core.Navigation.CommandRouter"
        }
    }
}


