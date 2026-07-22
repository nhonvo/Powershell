Describe "New Profile Features Tests" {
    BeforeAll {
        $repoRoot = Resolve-Path "$PSScriptRoot\..\..\.." | Select-Object -ExpandProperty Path
        $profilePath = Join-Path $repoRoot "Microsoft.PowerShell_profile.ps1"
        if (Test-Path $profilePath) {
            . $profilePath
        }
    }

    Context "AgySecretVault" {
        It "sets, gets, and removes secrets" {
            $tempHome = Join-Path ([System.IO.Path]::GetTempPath()) ("test_vault_" + [System.Guid]::NewGuid().ToString("N"))
            New-Item -ItemType Directory -Path $tempHome -Force | Out-Null
            $origHome = [AgyTui.Config]::Current.System.AgySourceHome
            [AgyTui.Config]::Current.System.AgySourceHome = $tempHome
            try {
                { Invoke-SecretVault -Action "set" -Key "test_key" -Value "super_secret_value" } | Should Not Throw
                { Invoke-SecretVault -Action "get" -Key "test_key" } | Should Not Throw
                { Invoke-SecretVault -Action "list" } | Should Not Throw
                { Invoke-SecretVault -Action "remove" -Key "test_key" } | Should Not Throw
            } finally {
                [AgyTui.Config]::Current.System.AgySourceHome = $origHome
                Remove-Item $tempHome -Recurse -Force -ErrorAction SilentlyContinue
            }
        }
    }

    Context "SystemHelper - KillPort" {
        It "kills the process listening on a port" {
            { Invoke-KillPort -Port 12345 } | Should Not Throw
        }
    }
}
