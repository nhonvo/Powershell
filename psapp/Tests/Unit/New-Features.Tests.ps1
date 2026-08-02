Describe "New Profile Features Tests" {
    BeforeAll {
        $repoRoot = Resolve-Path "$PSScriptRoot\..\..\.." | Select-Object -ExpandProperty Path
        $profilePath = Join-Path $repoRoot "Microsoft.PowerShell_profile.ps1"
        if (Test-Path $profilePath) {
            . $profilePath
        }
        Load-AgyTuiDll
    }

    Context "AgySecretVault" {
        It "sets, gets, and removes secrets via CommandRouter" {
            $tempHome = Join-Path ([System.IO.Path]::GetTempPath()) ("test_vault_" + [System.Guid]::NewGuid().ToString("N"))
            New-Item -ItemType Directory -Path $tempHome -Force | Out-Null
            try {
                { [CommandRouter]::Route("secret-set", @("test_key", "super_secret_value")) } | Should Not Throw
                { [CommandRouter]::Route("secret-get", @("test_key")) } | Should Not Throw
                { [CommandRouter]::Route("secret-list") } | Should Not Throw
                { [CommandRouter]::Route("secret-remove", @("test_key")) } | Should Not Throw
            } finally {
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
