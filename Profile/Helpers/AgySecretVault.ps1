#region ANTIGRAVITY SECRET VAULT
# ==============================================================================
#  DPAPI-encrypted local credential store for API keys and secrets.
# ==============================================================================

class AgySecretVault {
    static [string] GetSecretsFilePath() {
        $dir = "C:\Users\Public\.gemini"
        if (-not (Test-Path $dir)) {
            $null = New-Item -ItemType Directory -Path $dir -Force
        }
        return Join-Path $dir "secrets.json"
    }

    static [hashtable] LoadSecrets() {
        $file = [AgySecretVault]::GetSecretsFilePath()
        if (-not (Test-Path $file)) {
            return @{}
        }
        try {
            $raw = Get-Content -Path $file -Raw -ErrorAction SilentlyContinue
            if ($raw) {
                $json = ConvertFrom-Json $raw
                $hash = @{}
                foreach ($prop in $json.psobject.Properties) {
                    $hash[$prop.Name] = $prop.Value
                }
                return $hash
            }
        } catch {}
        return @{}
    }

    static [void] SaveSecrets([hashtable]$secrets) {
        $file = [AgySecretVault]::GetSecretsFilePath()
        try {
            $json = ConvertTo-Json $secrets
            $json | Out-File -FilePath $file -Force -Encoding utf8
        } catch {
            Write-Error "Failed to save secrets: $_"
        }
    }

    static [void] SetSecret([string]$Key, [string]$Value) {
        if ([string]::IsNullOrWhiteSpace($Key) -or [string]::IsNullOrWhiteSpace($Value)) {
            Write-Error "Key and Value cannot be empty."
            return
        }
        $secrets = [AgySecretVault]::LoadSecrets()
        try {
            $secure = ConvertTo-SecureString $Value -AsPlainText -Force
            $encrypted = ConvertFrom-SecureString $secure
            $secrets[$Key] = $encrypted
            [AgySecretVault]::SaveSecrets($secrets)
            Write-Host "Secret '$Key' saved and encrypted successfully." -ForegroundColor Green
        } catch {
            Write-Error "Failed to encrypt/save secret: $_"
        }
    }

    static [string] GetSecret([string]$Key) {
        if ([string]::IsNullOrWhiteSpace($Key)) {
            return ""
        }
        $secrets = [AgySecretVault]::LoadSecrets()
        if (-not $secrets.ContainsKey($Key)) {
            Write-Warning "Secret '$Key' not found."
            return ""
        }
        try {
            $encrypted = $secrets[$Key]
            $secure = ConvertTo-SecureString $encrypted
            $cred = [System.Net.NetworkCredential]::new("", $secure)
            return $cred.Password
        } catch {
            Write-Error "Failed to decrypt secret '$Key': $_"
            return ""
        }
    }

    static [void] RemoveSecret([string]$Key) {
        if ([string]::IsNullOrWhiteSpace($Key)) {
            return
        }
        $secrets = [AgySecretVault]::LoadSecrets()
        if ($secrets.ContainsKey($Key)) {
            $secrets.Remove($Key)
            [AgySecretVault]::SaveSecrets($secrets)
            Write-Host "Secret '$Key' removed successfully." -ForegroundColor Green
        } else {
            Write-Warning "Secret '$Key' not found."
        }
    }

    static [void] ListSecrets() {
        $secrets = [AgySecretVault]::LoadSecrets()
        if ($secrets.Count -eq 0) {
            Write-Host "No secrets stored." -ForegroundColor Yellow
            return
        }
        Write-Host "Stored Secret Keys:" -ForegroundColor Cyan
        foreach ($key in $secrets.Keys) {
            Write-Host "  * $key" -ForegroundColor Gray
        }
    }
}
#endregion
