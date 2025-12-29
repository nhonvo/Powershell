#region HELP AND DOCUMENTATION
# ------------------------------------------------------------------------------
#  Command to display a summary of all custom functions and aliases.
# ------------------------------------------------------------------------------
<#
.SYNOPSIS
Displays a categorized list of all custom commands available in the profile.
#>
function Get-CustomCommands {
    [CmdletBinding()]
    param()

    Write-Host "`n╔═════════════════════════════════════╗" -ForegroundColor Cyan
    Write-Host "║      Custom PowerShell Commands     ║" -ForegroundColor Cyan
    Write-Host "╚═════════════════════════════════════╝`n" -ForegroundColor Cyan

    $commandHelp = @{
        "System & Navigation" = @(
            "go                     - Reload this profile"
            "edit-profile           - Open this profile in VS Code"
            "code <path>            - Open file/folder in VS Code"
            "proj                   - Interactive project navigator"
            "folder                 - Open current directory in File Explorer"
            "mkcd <dir>             - Make a directory and enter it"
            "pw                     - Go to PowerShell config folder"
            "ip, cls, grep, which   - Core aliases (ipconfig, clear, select-string, get-command)"
            ".., ...                - Navigate up one or two directories"
            "Clear-SavedHistory     - Wipes all command history"
        )
        ".NET"   = @(
            "db, dt, dr, dw, df     - Build, Test, Run, Watch, Format"
            "du, da, dd, dremove    - EF Core: Update, Add, Drop, Remove Migration"
            "console, webapi        - Create new .NET projects"
        )
        "Git"    = @(
            "gs, ga, gcmt, gca      - Status, Add All, Commit, Amend"
            "co, cob, gb, gbd       - Checkout, Create Branch, List Branches, Delete Branch"
            "gpu, gus, guf, gf      - Pull, Push, Force Push, Fetch"
            "glg                    - Show pretty log graph"
            "gms <branch>           - Squash merge a branch"
            "gr                     - Reset last commit"
        )
        "Docker" = @(
            "dkcl [-All]            - List containers (add -All for stopped)"
            "dkstac, dkrmac         - Stop/Remove ALL containers (with confirmation)"
            "dkcpu, dkcpub, dkcpd   - Docker Compose: Up, Up --build, Down"
            "fix-volume, fix-image  - Prune unused volumes or images"
        )
    }

    foreach ($category in $commandHelp.Keys) {
        Write-Host "$category" -ForegroundColor Yellow
        Write-Host ('-' * $category.Length) -ForegroundColor Gray
        foreach ($command in $commandHelp[$category]) { Write-Host "  $command" -ForegroundColor White }
        Write-Host "" # Newline for spacing
    }
    Write-Host "`nType a command with '-Help' (e.g., 'proj -Help') for more details." -ForegroundColor Cyan
    Write-Host "Use 'Get-PSReadlineKeyHandler' to see all hotkeys." -ForegroundColor Cyan
}

#endregion