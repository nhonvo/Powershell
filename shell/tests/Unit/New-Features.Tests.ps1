Describe "New Profile Features Tests" {
    BeforeAll {
        $repoRoot = Resolve-Path "$PSScriptRoot\..\..\.." | Select-Object -ExpandProperty Path
        $profilePath = Join-Path $repoRoot "Microsoft.PowerShell_profile.ps1"
        if (Test-Path $profilePath) {
            . $profilePath
        }
    }

    Context "SystemHelper - KillPort" {
        It "kills the process listening on a port" {
            { Invoke-KillPort -Port 12345 } | Should Not Throw
        }
    }
}
