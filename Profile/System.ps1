#region SYSTEM & UTILITY COMMANDS
# ------------------------------------------------------------------------------
#  Functions for system interaction, profile management, and fun.
# ------------------------------------------------------------------------------

<#
.SYNOPSIS
Reloads the PowerShell profile.
.CATEGORY
System & Utility Commands
#>
function Reload-Profile {
    [CmdletBinding()]
    param()
    . $PROFILE; Write-Host "✅ Profile reloaded." -ForegroundColor Green
}

<#
.SYNOPSIS
Opens the current directory in File Explorer.
.CATEGORY
System & Utility Commands
#>
function folder {
    [CmdletBinding()]
    param()
    Invoke-Item .
}

<#
.SYNOPSIS
Opens the PowerShell profile in VS Code for editing.
.CATEGORY
System & Utility Commands
#>
function edit-profile {
    [CmdletBinding()]
    param()
    Open-Code $PROFILE
}

<#
.SYNOPSIS
Creates a new directory and changes the current location to it.
.CATEGORY
System & Utility Commands
#>
function mkcd {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$true)]
        [string]$DirName
    )
    mkdir $DirName; cd $DirName
}

<#
.SYNOPSIS
Clears the entire command history, including the saved history file.
.CATEGORY
System & Utility Commands
#>
function Clear-SavedHistory {
    [CmdletBinding(ConfirmImpact = 'High', SupportsShouldProcess)]
    param()

    if ($pscmdlet.ShouldProcess("entire command history, including the saved history file")) {
        Clear-Host
        Remove-Item (Get-PSReadlineOption).HistorySavePath -ErrorAction SilentlyContinue
        [Microsoft.PowerShell.PSConsoleReadLine]::ClearHistory()
        Clear-History
        Write-Host "🧹 All command history has been cleared." -ForegroundColor Yellow
    }
}

<#
.SYNOPSIS
Opens a file or folder in Visual Studio Code.
.DESCRIPTION
This function robustly finds the VS Code executable and opens the specified path.
It defaults to the current directory.
.PARAMETER Path
The file or folder path to open.
.CATEGORY
System & Utility Commands
#>
function Open-Code {
    [CmdletBinding()]
    param(
        [Parameter(ValueFromRemainingArguments=$true)]
        [string]$Path = "."
    )
    
    $vscodeExe = Get-Command code -ErrorAction SilentlyContinue
    if ($vscodeExe) {
        Write-Host "✅ VS Code found! Opening path: $Path" -ForegroundColor Green
        & $vscodeExe $Path
    } else {
        Write-Host "❌ VS Code not found. Please ensure 'code' is in your system's PATH." -ForegroundColor Red
    }
}

#endregion