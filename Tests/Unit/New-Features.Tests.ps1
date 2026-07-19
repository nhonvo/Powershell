Describe "New Profile Features Tests" {
    BeforeAll {
        . "C:\Users\TruongNhon\Documents\Powershell\Microsoft.PowerShell_profile.ps1"
    }

    Context "AgySecretVault" {
        It "sets, gets, and removes secrets" {
            { Invoke-SecretVault -Action "set" -Key "test_key" -Value "super_secret_value" } | Should Not Throw
            { Invoke-SecretVault -Action "get" -Key "test_key" } | Should Not Throw
            { Invoke-SecretVault -Action "list" } | Should Not Throw
            { Invoke-SecretVault -Action "remove" -Key "test_key" } | Should Not Throw
        }
    }

    Context "SystemHelper - KillPort" {
        It "kills the process listening on a port" {
            { Invoke-KillPort -Port 12345 } | Should Not Throw
        }
    }
}
