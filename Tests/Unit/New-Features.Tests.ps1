Describe "New Profile Features Tests" {
    . (Join-Path $PSScriptRoot "..\..\Profile\Helpers\AgySecretVault.ps1")
    . (Join-Path $PSScriptRoot "..\..\Profile\Helpers\ProjectScaffolder.ps1")
    . (Join-Path $PSScriptRoot "..\..\Profile\Helpers\SystemHelper.ps1")
    . (Join-Path $PSScriptRoot "..\..\Profile\Helpers\DockerHelper.ps1")

    Context "AgySecretVault" {
        It "sets, gets, and removes secrets" {
            try {
                [AgySecretVault]::SetSecret("test_key", "super_secret_value")
                $val = [AgySecretVault]::GetSecret("test_key")
                $val | Should Be "super_secret_value"
                
                [AgySecretVault]::RemoveSecret("test_key")
                $val2 = [AgySecretVault]::GetSecret("test_key")
                $val2 | Should Be ""
            } finally {
                $file = [AgySecretVault]::GetSecretsFilePath()
                if (Test-Path $file) {
                    $secrets = [AgySecretVault]::LoadSecrets()
                    if ($secrets.ContainsKey("test_key")) {
                        $secrets.Remove("test_key")
                        [AgySecretVault]::SaveSecrets($secrets)
                    }
                }
            }
        }
    }

    Context "SystemHelper - KillPort" {
        It "kills the process listening on a port" {
            Mock Get-NetTCPConnection {
                return [PSCustomObject]@{ OwningProcess = 98765 }
            }
            Mock Get-Process {
                return [PSCustomObject]@{ Id = 98765; Name = "testportlistener" }
            }
            $global:killedProcess = $false
            Mock Stop-Process {
                param($Id, $Force)
                if ($Id -eq 98765) { $global:killedProcess = $true }
            }
            
            [SystemHelper]::KillPort(12345)
            $global:killedProcess | Should Be $true
        }
    }
}
