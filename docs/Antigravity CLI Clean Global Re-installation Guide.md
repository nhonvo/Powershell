# Antigravity CLI Clean Global Re-installation Guide 🛸

Follow this step-by-step guide to cleanly wipe any existing local user-level installations of Antigravity CLI (`agy-cli`) and reinstall it as a shared global binary available to all users.

> [!WARNING]
> Do **NOT** attempt to delete the `%USERPROFILE%\.gemini` or `%USERPROFILE%\.gemini\antigravity` folder. These directories contain the active database, logs, and state files for your running AI agent session (which will throw file lock errors if you try to delete them).

---

## 🛑 Step 1: Terminate Active Processes & Clear Hooks

Before deleting files, make sure no active processes are locking the binary:

```powershell
# Terminate any running agy instances
Stop-Process -Name agy -Force -ErrorAction SilentlyContinue
```

---

## 🧹 Step 2: Clean Existing User-Level Installations

Delete the local user directories to ensure a completely clean state:

```powershell
# 1. Remove the local installer folder
$localInstallFolder = "$env:LOCALAPPDATA\Antigravity"
if (Test-Path $localInstallFolder) {
    Remove-Item -Path $localInstallFolder -Recurse -Force
    Write-Host "🧹 Cleared User-Level Installation directory." -ForegroundColor Green
}

# 2. Clear default public agy credentials and configurations
$publicConfigFolder = "C:\Users\Public\.gemini"
if (Test-Path $publicConfigFolder) {
    Remove-Item -Path $publicConfigFolder -Recurse -Force
    Write-Host "🧹 Cleared Public default agy configuration directory." -ForegroundColor Green
}

# 3. Clear all isolated secondary accounts configurations
$secondaryConfigs = Get-ChildItem -Path "C:\Users\Public" -Directory -Filter ".gemini_*" -ErrorAction SilentlyContinue
foreach ($cfg in $secondaryConfigs) {
    Remove-Item -Path $cfg.FullName -Recurse -Force
    Write-Host "🧹 Cleared isolated account: $($cfg.Name)" -ForegroundColor Green
}
```

---

## 🛠️ Step 3: Remove Old User-Level Path References

Ensure there are no old user-level PATH entries:

1. Press `Win + R`, type `sysdm.cpl`, and press **Enter**.
2. Click the **Advanced** tab, then click **Environment Variables**.
3. Under **User Variables** for your user, select **Path** and click **Edit**.
4. Remove any references pointing to:
   - `%LOCALAPPDATA%\Antigravity`
   - `C:\Users\Public\.gemini`

---

## 🌎 Step 4: Install Globally for All Users

We will install `agy` into `C:\ProgramData\agy\bin`, which is configured as the global shared directory.

### 1. Download and Place Binary
Run the following script in an **Administrator PowerShell Session**:

```powershell
# 1. Create the global shared bin directory
$globalBin = "C:\ProgramData\agy\bin"
if (-not (Test-Path $globalBin)) {
    $null = New-Item -ItemType Directory -Path $globalBin -Force
}

# 2. Download the latest verified 1.0.16 binary directly using curl.exe
$downloadUrl = "https://storage.googleapis.com/antigravity-public/antigravity-cli/1.0.16-4893150192467968/windows-x64/cli_windows_x64.exe"
$destPath = Join-Path $globalBin "agy.exe"
$expectedHash = "a25aa14ba15b271aaeec4615a1f9e01153959c7e10636a79444df74ac63d5629a607cb6a2547a885a63d7d100ed628b3d2407a2a200293dc6edb263522dcb2cc"

Write-Host "📥 Downloading Antigravity CLI version 1.0.16 to global directory..." -ForegroundColor Cyan
curl.exe -L $downloadUrl -o $destPath

# 3. Verify the file and unblock
if (Test-Path $destPath) {
    $actualHash = (Get-FileHash $destPath -Algorithm SHA512).Hash.ToLower()
    if ($actualHash -eq $expectedHash) {
        Unblock-File -Path $destPath -ErrorAction SilentlyContinue
        Write-Host "✅ Global binary successfully placed and verified at: $destPath" -ForegroundColor Green
    } else {
        Write-Error "❌ Hash verification failed! Expected $expectedHash but got $actualHash"
    }
} else {
    Write-Error "❌ Failed to download binary."
}
```

### 2. Configure System-Wide Environment Variable
Add the global bin directory to the **System-Wide (All Users) PATH**:

```powershell
# Add C:\ProgramData\agy\bin to System Path
$systemPath = [Environment]::GetEnvironmentVariable("Path", "Machine")
if ($systemPath -split ';' -notcontains $globalBin) {
    [Environment]::SetEnvironmentVariable("Path", "$systemPath;$globalBin", "Machine")
    Write-Host "⚙️ System PATH updated globally for all users." -ForegroundColor Green
} else {
    Write-Host "ℹ️ System PATH already contains global folder." -ForegroundColor Cyan
}
```

---

## 🏁 Step 5: Verification

Open a **new** terminal session (to load the updated environment variables) and verify that the global installation resolves successfully:

```powershell
# Verify command resolution
Get-Command agy

# Verify running version
agy --version
```
