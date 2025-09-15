# ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
#  Enhanced PowerShell Profile
#  Combines Oh My Posh, custom hotkeys, and powerful command functions for
#  .NET, Git, Docker, and AWS workflows.
#
#  Refactored for clarity, maintainability, and integrated help.
#  Use 'Get-Help <command-name>' or '<command-name> -Help' for details.
# ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

#region CORE SETUP
# ------------------------------------------------------------------------------
#  Initial configuration for the shell environment, theme, and modules.
# ------------------------------------------------------------------------------

Write-Host "🚀 Loading Enhanced PowerShell Profile..." -ForegroundColor Cyan

# --- Oh My Posh Theme ---
$env:POSH_THEMES_PATH = Join-Path -Path $env:USERPROFILE -ChildPath "Documents\PowerShell\powershell-themes"
$env:THEME = "neko" # Change your theme here
$themePath = Join-Path -Path $env:POSH_THEMES_PATH -ChildPath "$($env:THEME).omp.json"
if (Test-Path $themePath) {
    oh-my-posh --init --shell pwsh --config $themePath | Invoke-Expression
} else {
    Write-Warning "Oh My Posh theme '$($env:THEME)' not found at '$themePath'."
}

# --- Module Loading ---
if ($Host.Name -eq 'ConsoleHost' -or $host.Name -eq 'Visual Studio Code Host') {
    try {
        Import-Module PSReadLine -Force -ErrorAction SilentlyContinue
        Import-Module Terminal-Icons -Force -ErrorAction SilentlyContinue
        if (Get-Module -ListAvailable -Name posh-git) {
            Import-Module posh-git -Force -ErrorAction SilentlyContinue
        }
    } catch {
        Write-Warning "An error occurred while loading modules: $_"
    }
}

# --- PSReadLine Options ---
Set-PSReadLineOption -EditMode Windows
Set-PSReadLineOption -PredictionSource History
Set-PSReadLineOption -PredictionViewStyle ListView
Set-PSReadLineOption -BellStyle None
Set-PSReadlineOption -Color @{
    "Command"          = [ConsoleColor]::Green
    "Parameter"        = [ConsoleColor]::Gray
    "Operator"         = [ConsoleColor]::Magenta
    "Variable"         = [ConsoleColor]::Yellow
    "String"           = [ConsoleColor]::Cyan
    "Number"           = [ConsoleColor]::White
    "Type"             = [ConsoleColor]::Blue
    "Comment"          = [ConsoleColor]::DarkGreen
    "Keyword"          = [ConsoleColor]::DarkYellow
    "Error"            = [ConsoleColor]::Red
    "InlinePrediction" = '#70A99F'
}

# --- PSReadLine Key Bindings ---
Set-PSReadLineKeyHandler -Key UpArrow -Function HistorySearchBackward
Set-PSReadLineKeyHandler -Key DownArrow -Function HistorySearchForward
Set-PSReadLineKeyHandler -Key 'Ctrl+Spacebar' -Function Complete
Set-PSReadLineKeyHandler -Key F7 -ScriptBlock {
    $command = Get-History | Out-GridView -Title 'Command History' -PassThru
    if ($command) {
        [Microsoft.PowerShell.PSConsoleReadLine]::RevertLine()
        [Microsoft.PowerShell.PSConsoleReadLine]::Insert($command.CommandLine)
    }
}
# .NET Hotkeys
Set-PSReadLineKeyHandler -Key 'Ctrl+Shift+b' -ScriptBlock { [Microsoft.PowerShell.PSConsoleReadLine]::RevertLine(); [Microsoft.PowerShell.PSConsoleReadLine]::Insert("db"); [Microsoft.PowerShell.PSConsoleReadLine]::AcceptLine() }
Set-PSReadLineKeyHandler -Key 'Ctrl+Shift+t' -ScriptBlock { [Microsoft.PowerShell.PSConsoleReadLine]::RevertLine(); [Microsoft.PowerShell.PSConsoleReadLine]::Insert("dt"); [Microsoft.PowerShell.PSConsoleReadLine]::AcceptLine() }
# Auto-pairing brackets and quotes
Set-PSReadLineKeyHandler -Key '(', '{', '[', '"', "'" -ScriptBlock {
    param($key, $arg)
    $openChar = $key.KeyChar
    $closeChar = switch ($openChar) { '(' { ')' } '{' { '}' } '[' { ']' } '"' { '"' } "'" { "'" } }
    [Microsoft.PowerShell.PSConsoleReadLine]::Insert("$openChar$closeChar")
    [Microsoft.PowerShell.PSConsoleReadLine]::BackwardChar()
}
# Smart backspace
Set-PSReadLineKeyHandler -Key 'Backspace' -ScriptBlock {
    $line = ''; $cursor = 0
    [Microsoft.PowerShell.PSConsoleReadLine]::GetBufferState([ref]$line, [ref]$cursor)
    if ($cursor -gt 0) {
        $charBehind = $line[$cursor - 1]
        $charAhead = if ($cursor -lt $line.Length) { $line[$cursor] } else { $null }
        $pairs = @{ '(' = ')'; '{' = '}'; '[' = ']'; '"' = '"'; "'" = "'" }
        if ($pairs.ContainsKey($charBehind) -and $pairs[$charBehind] -eq $charAhead) {
            [Microsoft.PowerShell.PSConsoleReadLine]::Delete($cursor - 1, 2)
        } else {
            [Microsoft.PowerShell.PSConsoleReadLine]::BackwardDeleteChar()
        }
    }
}

#endregion

#region ALIASES & QUICK NAVIGATION
# ------------------------------------------------------------------------------
#  Shortcuts for frequently used commands and directory navigation.
# ------------------------------------------------------------------------------

# --- Core Aliases ---
Set-Alias -Name ip -Value Get-NetIPConfiguration -Force
Set-Alias -Name cls -Value Clear-Host -Force
Set-Alias -Name grep -Value Select-String -Force
Set-Alias -Name which -Value Get-Command -Force
Set-Alias -Name v -Value nvim -Force
Set-Alias -Name o -Value ollama -Force
Set-Alias -Name go -Value Reload-Profile -Force
Set-Alias -Name commands -Value Get-CustomCommands -Force
Set-Alias -Name code -Value Open-Code -Force

# --- Quick Navigation ---
Set-Alias -Name .. -Value Set-LocationParent -Force
Set-Alias -Name ... -Value Set-LocationGrandParent -Force

<#
.SYNOPSIS
Navigates to the parent directory.
.CATEGORY
System & Utility Commands
#>
function Set-LocationParent { Set-Location .. }

<#
.SYNOPSIS
Navigates to the grandparent directory.
.CATEGORY
System & Utility Commands
#>
function Set-LocationGrandParent { Set-Location ../.. }

#endregion

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
    param(
        [switch]$Help
    )
    if ($Help) { Get-Help $MyInvocation.MyCommand.Name; return }
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
    param(
        [switch]$Help
    )
    if ($Help) { Get-Help $MyInvocation.MyCommand.Name; return }
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
    param(
        [switch]$Help
    )
    if ($Help) { Get-Help $MyInvocation.MyCommand.Name; return }
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
        [string]$DirName,
        [switch]$Help
    )
    if ($Help) { Get-Help $MyInvocation.MyCommand.Name; return }
    mkdir $DirName; cd $DirName
}

<#
.SYNOPSIS
Navigates to the PowerShell configuration directory and opens it in VS Code.
.CATEGORY
System & Utility Commands
#>
function pw {
    [CmdletBinding()]
    param(
        [switch]$Help
    )
    if ($Help) { Get-Help $MyInvocation.MyCommand.Name; return }
    cd "C:\Users\TruongNhon\Documents\PowerShell"; Open-Code .
}

<#
.SYNOPSIS
Fetches a random, useless fact.
.CATEGORY
Fun
#>
function fact {
    [CmdletBinding()]
    param(
        [switch]$Help
    )
    if ($Help) { Get-Help $MyInvocation.MyCommand.Name; return }
    irm -Uri https://uselessfacts.jsph.pl/random.json?language=en | Select -ExpandProperty text
}

<#
.SYNOPSIS
Fetches a random dad joke.
.CATEGORY
Fun
#>
function joke {
    [CmdletBinding()]
    param(
        [switch]$Help
    )
    if ($Help) { Get-Help $MyInvocation.MyCommand.Name; return }
    irm https://icanhazdadjoke.com/ -Headers @{accept = 'application/json' } | select -ExpandProperty joke
}

<#
.SYNOPSIS
Clears the entire command history, including the saved history file.
.CATEGORY
System & Utility Commands
#>
function Clear-SavedHistory {
    [CmdletBinding(ConfirmImpact = 'High', SupportsShouldProcess)]
    param(
        [switch]$Help
    )
    if ($Help) { Get-Help $MyInvocation.MyCommand.Name; return }

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
        [string]$Path = ".",
        [switch]$Help
    )
    if ($Help) { Get-Help $MyInvocation.MyCommand.Name; return }

    $vscodePaths = @(
        "$env:LOCALAPPDATA\Programs\Microsoft VS Code\Code.exe",
        "$env:ProgramFiles\Microsoft VS Code\Code.exe",
        "$env:ProgramFiles(x86)\Microsoft VS Code\Code.exe"
    )
    $vscodeExe = $null
    foreach ($vscodePath in $vscodePaths) {
        if (Test-Path $vscodePath) {
            $vscodeExe = $vscodePath
            break
        }
    }

    if ($vscodeExe) {
        Write-Host "✅ VS Code found! Opening path: $Path" -ForegroundColor Green
        & $vscodeExe $Path
    } else {
        Write-Host "❌ VS Code not found. Please ensure 'code' is in your system's PATH." -ForegroundColor Red
    }
}

#endregion

#region PROJECT NAVIGATION
# ------------------------------------------------------------------------------
#  Quickly navigate between project directories.
# ------------------------------------------------------------------------------
<#
.SYNOPSIS
Provides an interactive menu to navigate to project directories.
.DESCRIPTION
Scans a root project directory, lists all sub-folders, and allows you to jump
to one by selecting its number or a pre-defined short name.
Priority projects can be configured for quick access.
.CATEGORY
Project Navigation
#>
function proj {
    [CmdletBinding()]
    param(
        [switch]$Help
    )
    if ($Help) { Get-Help $MyInvocation.MyCommand.Name; return }

    # --- CONFIGURATION ---
    $projectsPath = "C:\Users\TruongNhon\Desktop"
    $priorityProjects = @(
        @{ Name = "nhonvo.github.io";    Short = "blog" },
        @{ Name = "senior-developer-study-plan";    Short = "senior" },
        @{ Name = "back-up";    Short = "ba" },
        @{ Name = "clean-architecture-net-8.0";    Short = "clean" },
        @{ Name = "profile";    Short = "profile" }
    )
    # --- END CONFIGURATION ---

    if (-not (Test-Path $projectsPath)) {
        Write-Host "❌ Projects directory not found: $projectsPath" -ForegroundColor Red
        return
    }

    $allProjects = Get-ChildItem -Path $projectsPath -Directory | Sort-Object Name
    if ($allProjects.Count -eq 0) {
        Write-Host "🟡 No project folders found in $projectsPath" -ForegroundColor Yellow
        return
    }

    $tableData = @()
    $number = 1

    foreach ($priority in $priorityProjects) {
        $project = $allProjects | Where-Object { $_.Name -eq $priority.Name }
        if ($project) {
            $tableData += [PSCustomObject]@{ Number = $number++; Short = $priority.Short; Project = $project.Name; Path = $project.FullName; IsPriority = $true }
        }
    }

    $priorityNames = $priorityProjects.Name
    $remainingProjects = $allProjects | Where-Object { $_.Name -notin $priorityNames }

    foreach ($project in $remainingProjects) {
        $words = $project.Name -split '[.\-_]+'
        $shortName = if ($words.Count -eq 1) { $words[0].Substring(0, [Math]::Min(3, $words[0].Length)) } else { ($words | ForEach-Object { $_.Substring(0, 1) }) -join '' }
        $shortName = $shortName.ToLower()
        $originalShort = $shortName
        $counter = 2
        while ($tableData.Short -contains $shortName) { $shortName = "$($originalShort)$($counter++)" }
        $tableData += [PSCustomObject]@{ Number = $number++; Short = $shortName; Project = $project.Name; Path = $project.FullName; IsPriority = $false }
    }

    Write-Host "Available Projects:" -ForegroundColor Cyan
    Write-Host ("-" * 40)
    foreach ($item in $tableData) {
        $line = "{0,3}. [{1,-7}] {2}" -f $item.Number, $item.Short, $item.Project
        if ($item.IsPriority) { Write-Host $line -ForegroundColor Cyan } else { Write-Host $line }
    }
    Write-Host ("-" * 40)

    $selection = Read-Host "Select a project (by short name or number) or '0' to stay"

    if ($selection -eq '0' -or [string]::IsNullOrWhiteSpace($selection)) {
        Write-Host "➡️ Staying in current directory." -ForegroundColor Green
        return
    }

    $selectedProject = $tableData | Where-Object { $_.Short -eq $selection.ToLower() }
    if (-not $selectedProject) {
        if ([int]::TryParse($selection, [ref]$null)) {
            $selectedProject = $tableData | Where-Object { $_.Number -eq [int]$selection }
        }
    }

    if ($selectedProject) {
        Write-Host "🚀 Navigating to: $($selectedProject.Project)" -ForegroundColor Green
        Set-Location $selectedProject.Path
        Get-ChildItem | Format-Table -AutoSize
    } else {
        Write-Host "❌ Invalid selection." -ForegroundColor Red
    }
}
#endregion

#region .NET DEVELOPMENT COMMANDS
# ------------------------------------------------------------------------------
#  Shortcuts for dotnet CLI commands.
# ------------------------------------------------------------------------------

<# 
.SYNOPSIS 
Runs 'dotnet run'. 
.CATEGORY
.NET Development Commands
#>
function dr { [CmdletBinding()] param([switch]$Help, [Parameter(ValueFromRemainingArguments=$true)][string[]]$args) if ($Help) { Get-Help $MyInvocation.MyCommand.Name; return } Write-Host "🚀 Running project..." -ForegroundColor Green; dotnet run @args }
<# 
.SYNOPSIS 
Runs 'dotnet watch'. 
.CATEGORY
.NET Development Commands
#>
function dw { [CmdletBinding()] param([switch]$Help, [Parameter(ValueFromRemainingArguments=$true)][string[]]$args) if ($Help) { Get-Help $MyInvocation.MyCommand.Name; return } Write-Host "👀 Watching for changes..." -ForegroundColor Cyan; dotnet watch @args }
<# 
.SYNOPSIS 
Runs 'dotnet build'. 
.CATEGORY
.NET Development Commands
#>
function db { [CmdletBinding()] param([switch]$Help, [Parameter(ValueFromRemainingArguments=$true)][string[]]$args) if ($Help) { Get-Help $MyInvocation.MyCommand.Name; return } Write-Host "🔨 Building project..." -ForegroundColor Blue; dotnet build @args }
<# 
.SYNOPSIS 
Runs 'dotnet format'. 
.CATEGORY
.NET Development Commands
#>
function df { [CmdletBinding()] param([switch]$Help, [Parameter(ValueFromRemainingArguments=$true)][string[]]$args) if ($Help) { Get-Help $MyInvocation.MyCommand.Name; return } Write-Host "💅 Formatting code..." -ForegroundColor Magenta; dotnet format @args }
<# 
.SYNOPSIS 
Runs 'dotnet test'. 
.CATEGORY
.NET Development Commands
#>
function dt { [CmdletBinding()] param([switch]$Help, [Parameter(ValueFromRemainingArguments=$true)][string[]]$args) if ($Help) { Get-Help $MyInvocation.MyCommand.Name; return } Write-Host "🧪 Running tests..." -ForegroundColor Yellow; dotnet test @args }

# --- Entity Framework ---
<# 
.SYNOPSIS 
Runs 'dotnet ef database update'. 
.CATEGORY
.NET Development Commands
#>
function du { [CmdletBinding()] param([string]$Context, [switch]$Help) if ($Help) { Get-Help $MyInvocation.MyCommand.Name; return } Write-Host "📈 Updating database..." -ForegroundColor Green; $cmd = "dotnet ef database update"; if ($Context) { $cmd += " --context $Context" }; Invoke-Expression $cmd }
<# 
.SYNOPSIS 
Runs 'dotnet ef migrations add'. 
.CATEGORY
.NET Development Commands
#>
function da { [CmdletBinding()] param([Parameter(Mandatory=$true)][string]$MigrationName, [string]$Context, [switch]$Help) if ($Help) { Get-Help $MyInvocation.MyCommand.Name; return } Write-Host "➕ Adding migration: $MigrationName" -ForegroundColor Cyan; $cmd = "dotnet ef migrations add $MigrationName"; if ($Context) { $cmd += " --context $Context" }; Invoke-Expression $cmd }
<# 
.SYNOPSIS 
Runs 'dotnet ef database drop'. 
.CATEGORY
.NET Development Commands
#>
function dd { [CmdletBinding()] param([string]$Context, [switch]$Help) if ($Help) { Get-Help $MyInvocation.MyCommand.Name; return } Write-Host "🔥 Dropping database..." -ForegroundColor Red; if ((Read-Host "Are you sure? (y/N)") -eq 'y') { $cmd = "dotnet ef database drop --force"; if ($Context) { $cmd += " --context $Context" }; Invoke-Expression $cmd; Write-Host "Database dropped." } else { Write-Host "Cancelled." } }
<# 
.SYNOPSIS 
Runs 'dotnet ef migrations remove'. 
.CATEGORY
.NET Development Commands
#>
function dremove { [CmdletBinding()] param([string]$Context, [switch]$Help) if ($Help) { Get-Help $MyInvocation.MyCommand.Name; return } Write-Host "⏪ Removing last migration..." -ForegroundColor Yellow; $cmd = "dotnet ef migrations remove"; if ($Context) { $cmd += " --context $Context" }; Invoke-Expression $cmd }

# --- Project Scaffolding ---
Set-Alias -Name console -Value New-ConsoleProject
Set-Alias -Name webapi -Value New-WebApiProject

<# 
.SYNOPSIS 
Creates a new .NET Console project with Git initialized. 
.CATEGORY
.NET Development Commands
#>
function New-ConsoleProject { [CmdletBinding()] param([Parameter(Mandatory=$true)][string]$ProjectName, [switch]$SkipGit, [switch]$Help) if ($Help) { Get-Help $MyInvocation.MyCommand.Name; return } Write-Host "Creating new Console project: $ProjectName" -ForegroundColor Green; mkdir $ProjectName; cd $ProjectName; dotnet new console -n $ProjectName; dotnet new gitignore; if (-not $SkipGit) { git init; git add .; git commit -m "Initial commit" }; Open-Code .; dotnet run }
<# 
.SYNOPSIS 
Creates a new .NET Web API project with Git initialized. 
.CATEGORY
.NET Development Commands
#>
function New-WebApiProject { [CmdletBinding()] param([Parameter(Mandatory=$true)][string]$ProjectName, [switch]$SkipGit, [switch]$Help) if ($Help) { Get-Help $MyInvocation.MyCommand.Name; return } Write-Host "Creating new Web API project: $ProjectName" -ForegroundColor Green; mkdir $ProjectName; cd $ProjectName; dotnet new webapi -n $ProjectName; dotnet new gitignore; if (-not $SkipGit) { git init; git add .; git commit -m "Initial commit" }; Open-Code .; dotnet run }

#endregion

#region GIT COMMANDS
# ------------------------------------------------------------------------------
#  Shortcuts for common Git operations.
# ------------------------------------------------------------------------------

<# 
.SYNOPSIS 
Runs 'git status'. 
.CATEGORY
Git Commands
#>
function gs { [CmdletBinding()] param([switch]$Help, [Parameter(ValueFromRemainingArguments=$true)][string[]]$args) if ($Help) { Get-Help $MyInvocation.MyCommand.Name; return } Write-Host "🔍 Git Status" -ForegroundColor Yellow; git status @args }
<# 
.SYNOPSIS 
Stages all changes ('git add .'). 
.CATEGORY
Git Commands
#>
function ga { [CmdletBinding()] param([switch]$Help) if ($Help) { Get-Help $MyInvocation.MyCommand.Name; return } Write-Host "✅ Staging all changes..." -ForegroundColor Green; git add . }
<# 
.SYNOPSIS 
Commits with a message ('git commit -m'). 
.CATEGORY
Git Commands
#>
function gcmt { [CmdletBinding()] param([Parameter(Mandatory=$true, ValueFromRemainingArguments=$true)][string[]]$Message, [switch]$Help) if ($Help) { Get-Help $MyInvocation.MyCommand.Name; return } $commitMessage = $Message -join ' '; Write-Host "📝 Committing with message: `"$commitMessage`"" -ForegroundColor Cyan; git commit -m "$commitMessage" }
<# 
.SYNOPSIS 
Amends the previous commit ('git commit --amend'). 
.CATEGORY
Git Commands
#>
function gca { [CmdletBinding()] param([switch]$Help, [Parameter(ValueFromRemainingArguments=$true)][string[]]$args) if ($Help) { Get-Help $MyInvocation.MyCommand.Name; return } Write-Host "✍️ Amending previous commit..." -ForegroundColor Cyan; git commit --amend @args }
<# 
.SYNOPSIS 
Checks out a branch ('git checkout'). 
.CATEGORY
Git Commands
#>
function co { [CmdletBinding()] param([string]$branchName, [switch]$Help) if ($Help) { Get-Help $MyInvocation.MyCommand.Name; return } Write-Host "Check out branch: $branchName" -ForegroundColor Green; git checkout $branchName }
<# 
.SYNOPSIS 
Creates and checks out a new branch ('git checkout -b'). 
.CATEGORY
Git Commands
#>
function cob { [CmdletBinding()] param([string]$branchName, [switch]$Help) if ($Help) { Get-Help $MyInvocation.MyCommand.Name; return } Write-Host "Check out and create a new branch: $branchName" -ForegroundColor Green; git checkout -b $branchName }
<# 
.SYNOPSIS 
Shows a compact, graphical log ('git log --graph...'). 
.CATEGORY
Git Commands
#>
function glg { [CmdletBinding()] param([switch]$Help) if ($Help) { Get-Help $MyInvocation.MyCommand.Name; return } git log --graph --oneline --decorate --all }
<# 
.SYNOPSIS 
Pulls from remote ('git pull'). 
.CATEGORY
Git Commands
#>
function gpu { [CmdletBinding()] param([switch]$Help, [Parameter(ValueFromRemainingArguments=$true)][string[]]$args) if ($Help) { Get-Help $MyInvocation.MyCommand.Name; return } Write-Host "⏬ Pulling changes from remote..." -ForegroundColor Blue; git pull @args }
<# 
.SYNOPSIS 
Pushes to remote ('git push'). 
.CATEGORY
Git Commands
#>
function gus { [CmdletBinding()] param([switch]$Help, [Parameter(ValueFromRemainingArguments=$true)][string[]]$args) if ($Help) { Get-Help $MyInvocation.MyCommand.Name; return } Write-Host "⏫ Pushing changes to remote..." -ForegroundColor Blue; git push @args }
<# 
.SYNOPSIS 
Force pushes to remote ('git push --force'). 
.CATEGORY
Git Commands
#>
function guf { [CmdletBinding()] param([switch]$Help, [Parameter(ValueFromRemainingArguments=$true)][string[]]$args) if ($Help) { Get-Help $MyInvocation.MyCommand.Name; return } Write-Host "⏫ Force pushing changes..." -ForegroundColor Red; git push --force @args }
<# 
.SYNOPSIS 
Fetches from all remotes ('git fetch --all --prune'). 
.CATEGORY
Git Commands
#>
function gf { [CmdletBinding()] param([switch]$Help) if ($Help) { Get-Help $MyInvocation.MyCommand.Name; return } Write-Host "🔎 Fetching from all remotes..." -ForegroundColor Blue; git fetch --all --prune }
<# 
.SYNOPSIS 
Lists branches ('git branch'). 
.CATEGORY
Git Commands
#>
function gb { [CmdletBinding()] param([switch]$Help, [Parameter(ValueFromRemainingArguments=$true)][string[]]$args) if ($Help) { Get-Help $MyInvocation.MyCommand.Name; return } Write-Host "🌿 Branches:" -ForegroundColor Green; git branch @args }
<# 
.SYNOPSIS 
Deletes a local branch ('git branch -d'). 
.CATEGORY
Git Commands
#>
function gbd { [CmdletBinding()] param([string]$branchName, [switch]$Help) if ($Help) { Get-Help $MyInvocation.MyCommand.Name; return } git branch -d $branchName }
<# 
.SYNOPSIS 
Squash merges a branch ('git merge --squash'). 
.CATEGORY
Git Commands
#>
function gms { [CmdletBinding()] param([Parameter(Mandatory=$true)][string]$BranchName, [switch]$Help) if ($Help) { Get-Help $MyInvocation.MyCommand.Name; return } Write-Host "Squash merging branch: $BranchName" —ForegroundColor Yellow; git merge --squash $BranchName; Write-Host "Commit the squashed changes!" —ForegroundColor Cyan }
<# 
.SYNOPSIS 
Resets the last commit ('git reset HEAD~'). 
.CATEGORY
Git Commands
#>
function gr { [CmdletBinding()] param([switch]$Help) if ($Help) { Get-Help $MyInvocation.MyCommand.Name; return } git reset HEAD~ }

#endregion

#region DOCKER COMMANDS
# ------------------------------------------------------------------------------
#  Shortcuts for Docker and Docker Compose.
# ------------------------------------------------------------------------------

<# 
.SYNOPSIS 
Lists Docker containers ('docker container ls'). Use -All to see stopped containers. 
.CATEGORY
Docker Commands
#>
function dkcl { [CmdletBinding()] param([switch]$All, [switch]$Help) if ($Help) { Get-Help $MyInvocation.MyCommand.Name; return } Write-Host "🐳 Docker Containers:" -ForegroundColor Blue; if ($All) { docker container ls -a } else { docker container ls } }
<# 
.SYNOPSIS 
Removes all Docker containers with confirmation. 
.CATEGORY
Docker Commands
#>
function dkrmac { [CmdletBinding()] param([switch]$Help) if ($Help) { Get-Help $MyInvocation.MyCommand.Name; return } Write-Host "🗑️ Removing ALL containers..." -ForegroundColor Red; if ((Read-Host "This will remove ALL containers. Are you sure? (y/N)") -eq 'y') { $c = docker ps -aq; if ($c) { docker rm $c }; Write-Host "All containers removed." } else { Write-Host "Cancelled." } }
<# 
.SYNOPSIS 
Stops all running Docker containers. 
.CATEGORY
Docker Commands
#>
function dkstac { [CmdletBinding()] param([switch]$Help) if ($Help) { Get-Help $MyInvocation.MyCommand.Name; return } Write-Host "⏹️ Stopping ALL running containers..." -ForegroundColor Yellow; $c = docker ps -q; if ($c) { docker stop $c }; Write-Host "All containers stopped." }
<# 
.SYNOPSIS 
Runs 'docker-compose up'. 
.CATEGORY
Docker Commands
#>
function dkcpu { [CmdletBinding()] param([switch]$Help, [Parameter(ValueFromRemainingArguments=$true)][string[]]$args) if ($Help) { Get-Help $MyInvocation.MyCommand.Name; return } Write-Host "🚀 Starting Docker Compose..." -ForegroundColor Green; docker-compose up @args }
<# 
.SYNOPSIS 
Runs 'docker-compose up --build'. 
.CATEGORY
Docker Commands
#>
function dkcpub { [CmdletBinding()] param([switch]$Help, [Parameter(ValueFromRemainingArguments=$true)][string[]]$args) if ($Help) { Get-Help $MyInvocation.MyCommand.Name; return } Write-Host "🔨 Building and starting Docker Compose..." -ForegroundColor Blue; docker-compose up --build @args }
<# 
.SYNOPSIS 
Runs 'docker-compose down'. 
.CATEGORY
Docker Commands
#>
function dkcpd { [CmdletBinding()] param([switch]$Help, [Parameter(ValueFromRemainingArguments=$true)][string[]]$args) if ($Help) { Get-Help $MyInvocation.MyCommand.Name; return } Write-Host "🛑 Stopping Docker Compose..." -ForegroundColor Yellow; docker-compose down @args }
<# 
.SYNOPSIS 
Prunes unused Docker volumes ('docker volume prune'). 
.CATEGORY
Docker Commands
#>
function fix-volume { [CmdletBinding()] param([switch]$Help) if ($Help) { Get-Help $MyInvocation.MyCommand.Name; return } Write-Host "🧹 Pruning Docker volumes..." -ForegroundColor Magenta; docker volume prune }
<# 
.SYNOPSIS 
Prunes unused Docker images ('docker image prune'). 
.CATEGORY
Docker Commands
#>
function fix-image { [CmdletBinding()] param([switch]$Help) if ($Help) { Get-Help $MyInvocation.MyCommand.Name; return } Write-Host "🧹 Pruning Docker images..." -ForegroundColor Magenta; docker image prune }

#endregion

#region AWS LOCALSTACK COMMANDS
# ------------------------------------------------------------------------------
#  Commands for interacting with AWS services via LocalStack.
# ------------------------------------------------------------------------------

$localStackUrl = "http://127.0.0.1:4566"

<# 
.SYNOPSIS 
Lists SQS queues. 
.CATEGORY
AWS LocalStack Commands
#>
function list-queue { [CmdletBinding()] param([switch]$Help) if ($Help) { Get-Help $MyInvocation.MyCommand.Name; return } awslocal --endpoint-url=$localStackUrl sqs list-queues }
<# 
.SYNOPSIS 
Creates an SQS queue. 
.CATEGORY
AWS LocalStack Commands
#>
function create-queue { [CmdletBinding()] param([string]$QueueName, [switch]$Help) if ($Help) { Get-Help $MyInvocation.MyCommand.Name; return } awslocal --endpoint-url=$localStackUrl sqs create-queue --queue-name=$QueueName }
<# 
.SYNOPSIS 
Purges all messages from an SQS queue. 
.CATEGORY
AWS LocalStack Commands
#>
function clear-queue { [CmdletBinding()] param([string]$QueueUrl, [switch]$Help) if ($Help) { Get-Help $MyInvocation.MyCommand.Name; return } awslocal --endpoint-url=$localStackUrl sqs purge-queue --queue-url $QueueUrl }
<# 
.SYNOPSIS 
Sends a message to an SQS queue. 
.CATEGORY
AWS LocalStack Commands
#>
function send-mess { [CmdletBinding()] param([string]$QueueUrl, [string]$MessageBody, [string]$GroupId = "default-group", [switch]$Help) if ($Help) { Get-Help $MyInvocation.MyCommand.Name; return } awslocal --endpoint-url=$localStackUrl sqs send-message --queue-url $QueueUrl --message-body $MessageBody --message-group-id $GroupId }
<# 
.SYNOPSIS 
Receives messages from an SQS queue. 
.CATEGORY
AWS LocalStack Commands
#>
function receive-mess { [CmdletBinding()] param([string]$QueueUrl, [switch]$Help) if ($Help) { Get-Help $MyInvocation.MyCommand.Name; return } awslocal --endpoint-url=$localStackUrl sqs receive-message --queue-url $QueueUrl }
<# 
.SYNOPSIS 
Gets all attributes for an SQS queue. 
.CATEGORY
AWS LocalStack Commands
#>
function number-mes { [CmdletBinding()] param([string]$QueueUrl, [switch]$Help) if ($Help) { Get-Help $MyInvocation.MyCommand.Name; return } awslocal --endpoint-url=$localStackUrl sqs get-queue-attributes --queue-url $QueueUrl --attribute-names All }

#endregion

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
    param(
        [switch]$Help
    )
    if ($Help) { Get-Help $MyInvocation.MyCommand.Name; return }

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
Get-CustomCommands

#endregion


#region CORE SETUP
# ------------------------------------------------------------------------------
#  Initial configuration for the shell environment, theme, and modules.
# ------------------------------------------------------------------------------

Write-Host "🚀 Loading Enhanced PowerShell Profile..." -ForegroundColor Cyan

# --- Oh My Posh Theme ---
$env:POSH_THEMES_PATH = Join-Path -Path $env:USERPROFILE -ChildPath "Documents\PowerShell\powershell-themes"
$env:THEME = "neko" # Change your theme here
$themePath = Join-Path -Path $env:POSH_THEMES_PATH -ChildPath "$($env:THEME).omp.json"
if (Test-Path $themePath) {
    oh-my-posh --init --shell pwsh --config $themePath | Invoke-Expression
} else {
    Write-Warning "Oh My Posh theme '$($env:THEME)' not found at '$themePath'."
}

# --- Module Loading ---
if ($Host.Name -eq 'ConsoleHost' -or $host.Name -eq 'Visual Studio Code Host') {
    try {
        Import-Module PSReadLine -Force -ErrorAction SilentlyContinue
        Import-Module Terminal-Icons -Force -ErrorAction SilentlyContinue
        if (Get-Module -ListAvailable -Name posh-git) {
            Import-Module posh-git -Force -ErrorAction SilentlyContinue
        }
    } catch {
        Write-Warning "An error occurred while loading modules: $_"
    }
}

# --- PSReadLine Options ---
Set-PSReadLineOption -EditMode Windows
Set-PSReadLineOption -PredictionSource History
Set-PSReadLineOption -PredictionViewStyle ListView
Set-PSReadLineOption -BellStyle None
Set-PSReadlineOption -Color @{
    "Command"          = [ConsoleColor]::Green
    "Parameter"        = [ConsoleColor]::Gray
    "Operator"         = [ConsoleColor]::Magenta
    "Variable"         = [ConsoleColor]::Yellow
    "String"           = [ConsoleColor]::Cyan
    "Number"           = [ConsoleColor]::White
    "Type"             = [ConsoleColor]::Blue
    "Comment"          = [ConsoleColor]::DarkGreen
    "Keyword"          = [ConsoleColor]::DarkYellow
    "Error"            = [ConsoleColor]::Red
    "InlinePrediction" = '#70A99F'
}

# --- PSReadLine Key Bindings ---
Set-PSReadLineKeyHandler -Key UpArrow -Function HistorySearchBackward
Set-PSReadLineKeyHandler -Key DownArrow -Function HistorySearchForward
Set-PSReadLineKeyHandler -Key 'Ctrl+Spacebar' -Function Complete
Set-PSReadLineKeyHandler -Key F7 -ScriptBlock {
    $command = Get-History | Out-GridView -Title 'Command History' -PassThru
    if ($command) {
        [Microsoft.PowerShell.PSConsoleReadLine]::RevertLine()
        [Microsoft.PowerShell.PSConsoleReadLine]::Insert($command.CommandLine)
    }
}
# .NET Hotkeys
Set-PSReadLineKeyHandler -Key 'Ctrl+Shift+b' -ScriptBlock { [Microsoft.PowerShell.PSConsoleReadLine]::RevertLine(); [Microsoft.PowerShell.PSConsoleReadLine]::Insert("db"); [Microsoft.PowerShell.PSConsoleReadLine]::AcceptLine() }
Set-PSReadLineKeyHandler -Key 'Ctrl+Shift+t' -ScriptBlock { [Microsoft.PowerShell.PSConsoleReadLine]::RevertLine(); [Microsoft.PowerShell.PSConsoleReadLine]::Insert("dt"); [Microsoft.PowerShell.PSConsoleReadLine]::AcceptLine() }
# Auto-pairing brackets and quotes
Set-PSReadLineKeyHandler -Key '(', '{', '[', '"', "'" -ScriptBlock {
    param($key, $arg)
    $openChar = $key.KeyChar
    $closeChar = switch ($openChar) { '(' { ')' } '{' { '}' } '[' { ']' } '"' { '"' } "'" { "'" } }
    [Microsoft.PowerShell.PSConsoleReadLine]::Insert("$openChar$closeChar")
    [Microsoft.PowerShell.PSConsoleReadLine]::BackwardChar()
}
# Smart backspace
Set-PSReadLineKeyHandler -Key 'Backspace' -ScriptBlock {
    $line = ''; $cursor = 0
    [Microsoft.PowerShell.PSConsoleReadLine]::GetBufferState([ref]$line, [ref]$cursor)
    if ($cursor -gt 0) {
        $charBehind = $line[$cursor - 1]
        $charAhead = if ($cursor -lt $line.Length) { $line[$cursor] } else { $null }
        $pairs = @{ '(' = ')'; '{' = '}'; '[' = ']'; '"' = '"'; "'" = "'" }
        if ($pairs.ContainsKey($charBehind) -and $pairs[$charBehind] -eq $charAhead) {
            [Microsoft.PowerShell.PSConsoleReadLine]::Delete($cursor - 1, 2)
        } else {
            [Microsoft.PowerShell.PSConsoleReadLine]::BackwardDeleteChar()
        }
    }
}

#endregion

#region ALIASES & QUICK NAVIGATION
# ------------------------------------------------------------------------------
#  Shortcuts for frequently used commands and directory navigation.
# ------------------------------------------------------------------------------

# --- Core Aliases ---
Set-Alias -Name ip -Value Get-NetIPConfiguration -Force
Set-Alias -Name cls -Value Clear-Host -Force
Set-Alias -Name grep -Value Select-String -Force
Set-Alias -Name which -Value Get-Command -Force
Set-Alias -Name v -Value nvim -Force
Set-Alias -Name o -Value ollama -Force
Set-Alias -Name go -Value Reload-Profile -Force
Set-Alias -Name commands -Value Get-CustomCommands -Force
Set-Alias -Name code -Value Open-Code -Force

# --- Quick Navigation ---
Set-Alias -Name .. -Value Set-LocationParent -Force
Set-Alias -Name ... -Value Set-LocationGrandParent -Force

<#
.SYNOPSIS
Navigates to the parent directory.
.CATEGORY
System & Utility Commands
#>
function Set-LocationParent { Set-Location .. }

<#
.SYNOPSIS
Navigates to the grandparent directory.
.CATEGORY
System & Utility Commands
#>
function Set-LocationGrandParent { Set-Location ../.. }

#endregion

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
    param(
        [switch]$Help
    )
    if ($Help) { Get-Help $MyInvocation.MyCommand.Name; return }
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
    param(
        [switch]$Help
    )
    if ($Help) { Get-Help $MyInvocation.MyCommand.Name; return }
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
    param(
        [switch]$Help
    )
    if ($Help) { Get-Help $MyInvocation.MyCommand.Name; return }
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
        [string]$DirName,
        [switch]$Help
    )
    if ($Help) { Get-Help $MyInvocation.MyCommand.Name; return }
    mkdir $DirName; cd $DirName
}

<#
.SYNOPSIS
Navigates to the PowerShell configuration directory and opens it in VS Code.
.CATEGORY
System & Utility Commands
#>
function pw {
    [CmdletBinding()]
    param(
        [switch]$Help
    )
    if ($Help) { Get-Help $MyInvocation.MyCommand.Name; return }
    cd "C:\Users\TruongNhon\Documents\PowerShell"; Open-Code .
}

<#
.SYNOPSIS
Fetches a random, useless fact.
.CATEGORY
Fun
#>
function fact {
    [CmdletBinding()]
    param(
        [switch]$Help
    )
    if ($Help) { Get-Help $MyInvocation.MyCommand.Name; return }
    irm -Uri https://uselessfacts.jsph.pl/random.json?language=en | Select -ExpandProperty text
}

<#
.SYNOPSIS
Fetches a random dad joke.
.CATEGORY
Fun
#>
function joke {
    [CmdletBinding()]
    param(
        [switch]$Help
    )
    if ($Help) { Get-Help $MyInvocation.MyCommand.Name; return }
    irm https://icanhazdadjoke.com/ -Headers @{accept = 'application/json' } | select -ExpandProperty joke
}

<#
.SYNOPSIS
Clears the entire command history, including the saved history file.
.CATEGORY
System & Utility Commands
#>
function Clear-SavedHistory {
    [CmdletBinding(ConfirmImpact = 'High', SupportsShouldProcess)]
    param(
        [switch]$Help
    )
    if ($Help) { Get-Help $MyInvocation.MyCommand.Name; return }

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
        [string]$Path = ".",
        [switch]$Help
    )
    if ($Help) { Get-Help $MyInvocation.MyCommand.Name; return }

    $vscodePaths = @(
        "$env:LOCALAPPDATA\Programs\Microsoft VS Code\Code.exe",
        "$env:ProgramFiles\Microsoft VS Code\Code.exe",
        "$env:ProgramFiles(x86)\Microsoft VS Code\Code.exe"
    )
    $vscodeExe = $null
    foreach ($vscodePath in $vscodePaths) {
        if (Test-Path $vscodePath) {
            $vscodeExe = $vscodePath
            break
        }
    }

    if ($vscodeExe) {
        Write-Host "✅ VS Code found! Opening path: $Path" -ForegroundColor Green
        & $vscodeExe $Path
    } else {
        Write-Host "❌ VS Code not found. Please ensure 'code' is in your system's PATH." -ForegroundColor Red
    }
}

#endregion

#region PROJECT NAVIGATION
# ------------------------------------------------------------------------------
#  Quickly navigate between project directories.
# ------------------------------------------------------------------------------
<#
.SYNOPSIS
Provides an interactive menu to navigate to project directories.
.DESCRIPTION
Scans a root project directory, lists all sub-folders, and allows you to jump
to one by selecting its number or a pre-defined short name.
Priority projects can be configured for quick access.
.CATEGORY
Project Navigation
#>
function proj {
    [CmdletBinding()]
    param(
        [switch]$Help
    )
    if ($Help) { Get-Help $MyInvocation.MyCommand.Name; return }

    # --- CONFIGURATION ---
    $projectsPath = "C:\Users\TruongNhon\Desktop"
    $priorityProjects = @(
        @{ Name = "nhonvo.github.io";    Short = "blog" },
        @{ Name = "senior-developer-study-plan";    Short = "senior" },
        @{ Name = "back-up";    Short = "ba" },
        @{ Name = "clean-architecture-net-8.0";    Short = "clean" },
        @{ Name = "profile";    Short = "profile" }
    )
    # --- END CONFIGURATION ---

    if (-not (Test-Path $projectsPath)) {
        Write-Host "❌ Projects directory not found: $projectsPath" -ForegroundColor Red
        return
    }

    $allProjects = Get-ChildItem -Path $projectsPath -Directory | Sort-Object Name
    if ($allProjects.Count -eq 0) {
        Write-Host "🟡 No project folders found in $projectsPath" -ForegroundColor Yellow
        return
    }

    $tableData = @()
    $number = 1

    foreach ($priority in $priorityProjects) {
        $project = $allProjects | Where-Object { $_.Name -eq $priority.Name }
        if ($project) {
            $tableData += [PSCustomObject]@{ Number = $number++; Short = $priority.Short; Project = $project.Name; Path = $project.FullName; IsPriority = $true }
        }
    }

    $priorityNames = $priorityProjects.Name
    $remainingProjects = $allProjects | Where-Object { $_.Name -notin $priorityNames }

    foreach ($project in $remainingProjects) {
        $words = $project.Name -split '[.\-_]+'
        $shortName = if ($words.Count -eq 1) { $words[0].Substring(0, [Math]::Min(3, $words[0].Length)) } else { ($words | ForEach-Object { $_.Substring(0, 1) }) -join '' }
        $shortName = $shortName.ToLower()
        $originalShort = $shortName
        $counter = 2
        while ($tableData.Short -contains $shortName) { $shortName = "$($originalShort)$($counter++)" }
        $tableData += [PSCustomObject]@{ Number = $number++; Short = $shortName; Project = $project.Name; Path = $project.FullName; IsPriority = $false }
    }

    Write-Host "Available Projects:" -ForegroundColor Cyan
    Write-Host ("-" * 40)
    foreach ($item in $tableData) {
        $line = "{0,3}. [{1,-7}] {2}" -f $item.Number, $item.Short, $item.Project
        if ($item.IsPriority) { Write-Host $line -ForegroundColor Cyan } else { Write-Host $line }
    }
    Write-Host ("-" * 40)

    $selection = Read-Host "Select a project (by short name or number) or '0' to stay"

    if ($selection -eq '0' -or [string]::IsNullOrWhiteSpace($selection)) {
        Write-Host "➡️ Staying in current directory." -ForegroundColor Green
        return
    }

    $selectedProject = $tableData | Where-Object { $_.Short -eq $selection.ToLower() }
    if (-not $selectedProject) {
        if ([int]::TryParse($selection, [ref]$null)) {
            $selectedProject = $tableData | Where-Object { $_.Number -eq [int]$selection }
        }
    }

    if ($selectedProject) {
        Write-Host "🚀 Navigating to: $($selectedProject.Project)" -ForegroundColor Green
        Set-Location $selectedProject.Path
        Get-ChildItem | Format-Table -AutoSize
    } else {
        Write-Host "❌ Invalid selection." -ForegroundColor Red
    }
}
#endregion

#region .NET DEVELOPMENT COMMANDS
# ------------------------------------------------------------------------------
#  Shortcuts for dotnet CLI commands.
# ------------------------------------------------------------------------------

<# 
.SYNOPSIS 
Runs 'dotnet run'. 
.CATEGORY
.NET Development Commands
#>
function dr { [CmdletBinding()] param([switch]$Help, [Parameter(ValueFromRemainingArguments=$true)][string[]]$args) if ($Help) { Get-Help $MyInvocation.MyCommand.Name; return } Write-Host "🚀 Running project..." -ForegroundColor Green; dotnet run @args }
<# 
.SYNOPSIS 
Runs 'dotnet watch'. 
.CATEGORY
.NET Development Commands
#>
function dw { [CmdletBinding()] param([switch]$Help, [Parameter(ValueFromRemainingArguments=$true)][string[]]$args) if ($Help) { Get-Help $MyInvocation.MyCommand.Name; return } Write-Host "👀 Watching for changes..." -ForegroundColor Cyan; dotnet watch @args }
<# 
.SYNOPSIS 
Runs 'dotnet build'. 
.CATEGORY
.NET Development Commands
#>
function db { [CmdletBinding()] param([switch]$Help, [Parameter(ValueFromRemainingArguments=$true)][string[]]$args) if ($Help) { Get-Help $MyInvocation.MyCommand.Name; return } Write-Host "🔨 Building project..." -ForegroundColor Blue; dotnet build @args }
<# 
.SYNOPSIS 
Runs 'dotnet format'. 
.CATEGORY
.NET Development Commands
#>
function df { [CmdletBinding()] param([switch]$Help, [Parameter(ValueFromRemainingArguments=$true)][string[]]$args) if ($Help) { Get-Help $MyInvocation.MyCommand.Name; return } Write-Host "💅 Formatting code..." -ForegroundColor Magenta; dotnet format @args }
<# 
.SYNOPSIS 
Runs 'dotnet test'. 
.CATEGORY
.NET Development Commands
#>
function dt { [CmdletBinding()] param([switch]$Help, [Parameter(ValueFromRemainingArguments=$true)][string[]]$args) if ($Help) { Get-Help $MyInvocation.MyCommand.Name; return } Write-Host "🧪 Running tests..." -ForegroundColor Yellow; dotnet test @args }

# --- Entity Framework ---
<# 
.SYNOPSIS 
Runs 'dotnet ef database update'. 
.CATEGORY
.NET Development Commands
#>
function du { [CmdletBinding()] param([string]$Context, [switch]$Help) if ($Help) { Get-Help $MyInvocation.MyCommand.Name; return } Write-Host "📈 Updating database..." -ForegroundColor Green; $cmd = "dotnet ef database update"; if ($Context) { $cmd += " --context $Context" }; Invoke-Expression $cmd }
<# 
.SYNOPSIS 
Runs 'dotnet ef migrations add'. 
.CATEGORY
.NET Development Commands
#>
function da { [CmdletBinding()] param([Parameter(Mandatory=$true)][string]$MigrationName, [string]$Context, [switch]$Help) if ($Help) { Get-Help $MyInvocation.MyCommand.Name; return } Write-Host "➕ Adding migration: $MigrationName" -ForegroundColor Cyan; $cmd = "dotnet ef migrations add $MigrationName"; if ($Context) { $cmd += " --context $Context" }; Invoke-Expression $cmd }
<# 
.SYNOPSIS 
Runs 'dotnet ef database drop'. 
.CATEGORY
.NET Development Commands
#>
function dd { [CmdletBinding()] param([string]$Context, [switch]$Help) if ($Help) { Get-Help $MyInvocation.MyCommand.Name; return } Write-Host "🔥 Dropping database..." -ForegroundColor Red; if ((Read-Host "Are you sure? (y/N)") -eq 'y') { $cmd = "dotnet ef database drop --force"; if ($Context) { $cmd += " --context $Context" }; Invoke-Expression $cmd; Write-Host "Database dropped." } else { Write-Host "Cancelled." } }
<# 
.SYNOPSIS 
Runs 'dotnet ef migrations remove'. 
.CATEGORY
.NET Development Commands
#>
function dremove { [CmdletBinding()] param([string]$Context, [switch]$Help) if ($Help) { Get-Help $MyInvocation.MyCommand.Name; return } Write-Host "⏪ Removing last migration..." -ForegroundColor Yellow; $cmd = "dotnet ef migrations remove"; if ($Context) { $cmd += " --context $Context" }; Invoke-Expression $cmd }

# --- Project Scaffolding ---
Set-Alias -Name console -Value New-ConsoleProject
Set-Alias -Name webapi -Value New-WebApiProject

<# 
.SYNOPSIS 
Creates a new .NET Console project with Git initialized. 
.CATEGORY
.NET Development Commands
#>
function New-ConsoleProject { [CmdletBinding()] param([Parameter(Mandatory=$true)][string]$ProjectName, [switch]$SkipGit, [switch]$Help) if ($Help) { Get-Help $MyInvocation.MyCommand.Name; return } Write-Host "Creating new Console project: $ProjectName" -ForegroundColor Green; mkdir $ProjectName; cd $ProjectName; dotnet new console -n $ProjectName; dotnet new gitignore; if (-not $SkipGit) { git init; git add .; git commit -m "Initial commit" }; Open-Code .; dotnet run }
<# 
.SYNOPSIS 
Creates a new .NET Web API project with Git initialized. 
.CATEGORY
.NET Development Commands
#>
function New-WebApiProject { [CmdletBinding()] param([Parameter(Mandatory=$true)][string]$ProjectName, [switch]$SkipGit, [switch]$Help) if ($Help) { Get-Help $MyInvocation.MyCommand.Name; return } Write-Host "Creating new Web API project: $ProjectName" -ForegroundColor Green; mkdir $ProjectName; cd $ProjectName; dotnet new webapi -n $ProjectName; dotnet new gitignore; if (-not $SkipGit) { git init; git add .; git commit -m "Initial commit" }; Open-Code .; dotnet run }

#endregion

#region GIT COMMANDS
# ------------------------------------------------------------------------------
#  Shortcuts for common Git operations.
# ------------------------------------------------------------------------------

<# 
.SYNOPSIS 
Runs 'git status'. 
.CATEGORY
Git Commands
#>
function gs { [CmdletBinding()] param([switch]$Help, [Parameter(ValueFromRemainingArguments=$true)][string[]]$args) if ($Help) { Get-Help $MyInvocation.MyCommand.Name; return } Write-Host "🔍 Git Status" -ForegroundColor Yellow; git status @args }
<# 
.SYNOPSIS 
Stages all changes ('git add .'). 
.CATEGORY
Git Commands
#>
function ga { [CmdletBinding()] param([switch]$Help) if ($Help) { Get-Help $MyInvocation.MyCommand.Name; return } Write-Host "✅ Staging all changes..." -ForegroundColor Green; git add . }
<# 
.SYNOPSIS 
Commits with a message ('git commit -m'). 
.CATEGORY
Git Commands
#>
function gcmt { [CmdletBinding()] param([Parameter(Mandatory=$true, ValueFromRemainingArguments=$true)][string[]]$Message, [switch]$Help) if ($Help) { Get-Help $MyInvocation.MyCommand.Name; return } $commitMessage = $Message -join ' '; Write-Host "📝 Committing with message: `"$commitMessage`"" -ForegroundColor Cyan; git commit -m "$commitMessage" }
<# 
.SYNOPSIS 
Amends the previous commit ('git commit --amend'). 
.CATEGORY
Git Commands
#>
function gca { [CmdletBinding()] param([switch]$Help, [Parameter(ValueFromRemainingArguments=$true)][string[]]$args) if ($Help) { Get-Help $MyInvocation.MyCommand.Name; return } Write-Host "✍️ Amending previous commit..." -ForegroundColor Cyan; git commit --amend @args }
<# 
.SYNOPSIS 
Checks out a branch ('git checkout'). 
.CATEGORY
Git Commands
#>
function co { [CmdletBinding()] param([string]$branchName, [switch]$Help) if ($Help) { Get-Help $MyInvocation.MyCommand.Name; return } Write-Host "Check out branch: $branchName" -ForegroundColor Green; git checkout $branchName }
<# 
.SYNOPSIS 
Creates and checks out a new branch ('git checkout -b'). 
.CATEGORY
Git Commands
#>
function cob { [CmdletBinding()] param([string]$branchName, [switch]$Help) if ($Help) { Get-Help $MyInvocation.MyCommand.Name; return } Write-Host "Check out and create a new branch: $branchName" -ForegroundColor Green; git checkout -b $branchName }
<# 
.SYNOPSIS 
Shows a compact, graphical log ('git log --graph...'). 
.CATEGORY
Git Commands
#>
function glg { [CmdletBinding()] param([switch]$Help) if ($Help) { Get-Help $MyInvocation.MyCommand.Name; return } git log --graph --oneline --decorate --all }
<# 
.SYNOPSIS 
Pulls from remote ('git pull'). 
.CATEGORY
Git Commands
#>
function gpu { [CmdletBinding()] param([switch]$Help, [Parameter(ValueFromRemainingArguments=$true)][string[]]$args) if ($Help) { Get-Help $MyInvocation.MyCommand.Name; return } Write-Host "⏬ Pulling changes from remote..." -ForegroundColor Blue; git pull @args }
<# 
.SYNOPSIS 
Pushes to remote ('git push'). 
.CATEGORY
Git Commands
#>
function gus { [CmdletBinding()] param([switch]$Help, [Parameter(ValueFromRemainingArguments=$true)][string[]]$args) if ($Help) { Get-Help $MyInvocation.MyCommand.Name; return } Write-Host "⏫ Pushing changes to remote..." -ForegroundColor Blue; git push @args }
<# 
.SYNOPSIS 
Force pushes to remote ('git push --force'). 
.CATEGORY
Git Commands
#>
function guf { [CmdletBinding()] param([switch]$Help, [Parameter(ValueFromRemainingArguments=$true)][string[]]$args) if ($Help) { Get-Help $MyInvocation.MyCommand.Name; return } Write-Host "⏫ Force pushing changes..." -ForegroundColor Red; git push --force @args }
<# 
.SYNOPSIS 
Fetches from all remotes ('git fetch --all --prune'). 
.CATEGORY
Git Commands
#>
function gf { [CmdletBinding()] param([switch]$Help) if ($Help) { Get-Help $MyInvocation.MyCommand.Name; return } Write-Host "🔎 Fetching from all remotes..." -ForegroundColor Blue; git fetch --all --prune }
<# 
.SYNOPSIS 
Lists branches ('git branch'). 
.CATEGORY
Git Commands
#>
function gb { [CmdletBinding()] param([switch]$Help, [Parameter(ValueFromRemainingArguments=$true)][string[]]$args) if ($Help) { Get-Help $MyInvocation.MyCommand.Name; return } Write-Host "🌿 Branches:" -ForegroundColor Green; git branch @args }
<# 
.SYNOPSIS 
Deletes a local branch ('git branch -d'). 
.CATEGORY
Git Commands
#>
function gbd { [CmdletBinding()] param([string]$branchName, [switch]$Help) if ($Help) { Get-Help $MyInvocation.MyCommand.Name; return } git branch -d $branchName }
<# 
.SYNOPSIS 
Squash merges a branch ('git merge --squash'). 
.CATEGORY
Git Commands
#>
function gms { [CmdletBinding()] param([Parameter(Mandatory=$true)][string]$BranchName, [switch]$Help) if ($Help) { Get-Help $MyInvocation.MyCommand.Name; return } Write-Host "Squash merging branch: $BranchName" —ForegroundColor Yellow; git merge --squash $BranchName; Write-Host "Commit the squashed changes!" —ForegroundColor Cyan }
<# 
.SYNOPSIS 
Resets the last commit ('git reset HEAD~'). 
.CATEGORY
Git Commands
#>
function gr { [CmdletBinding()] param([switch]$Help) if ($Help) { Get-Help $MyInvocation.MyCommand.Name; return } git reset HEAD~ }

#endregion

#region DOCKER COMMANDS
# ------------------------------------------------------------------------------
#  Shortcuts for Docker and Docker Compose.
# ------------------------------------------------------------------------------

<# 
.SYNOPSIS 
Lists Docker containers ('docker container ls'). Use -All to see stopped containers. 
.CATEGORY
Docker Commands
#>
function dkcl { [CmdletBinding()] param([switch]$All, [switch]$Help) if ($Help) { Get-Help $MyInvocation.MyCommand.Name; return } Write-Host "🐳 Docker Containers:" -ForegroundColor Blue; if ($All) { docker container ls -a } else { docker container ls } }
<# 
.SYNOPSIS 
Removes all Docker containers with confirmation. 
.CATEGORY
Docker Commands
#>
function dkrmac { [CmdletBinding()] param([switch]$Help) if ($Help) { Get-Help $MyInvocation.MyCommand.Name; return } Write-Host "🗑️ Removing ALL containers..." -ForegroundColor Red; if ((Read-Host "This will remove ALL containers. Are you sure? (y/N)") -eq 'y') { $c = docker ps -aq; if ($c) { docker rm $c }; Write-Host "All containers removed." } else { Write-Host "Cancelled." } }
<# 
.SYNOPSIS 
Stops all running Docker containers. 
.CATEGORY
Docker Commands
#>
function dkstac { [CmdletBinding()] param([switch]$Help) if ($Help) { Get-Help $MyInvocation.MyCommand.Name; return } Write-Host "⏹️ Stopping ALL running containers..." -ForegroundColor Yellow; $c = docker ps -q; if ($c) { docker stop $c }; Write-Host "All containers stopped." }
<# 
.SYNOPSIS 
Runs 'docker-compose up'. 
.CATEGORY
Docker Commands
#>
function dkcpu { [CmdletBinding()] param([switch]$Help, [Parameter(ValueFromRemainingArguments=$true)][string[]]$args) if ($Help) { Get-Help $MyInvocation.MyCommand.Name; return } Write-Host "🚀 Starting Docker Compose..." -ForegroundColor Green; docker-compose up @args }
<# 
.SYNOPSIS 
Runs 'docker-compose up --build'. 
.CATEGORY
Docker Commands
#>
function dkcpub { [CmdletBinding()] param([switch]$Help, [Parameter(ValueFromRemainingArguments=$true)][string[]]$args) if ($Help) { Get-Help $MyInvocation.MyCommand.Name; return } Write-Host "🔨 Building and starting Docker Compose..." -ForegroundColor Blue; docker-compose up --build @args }
<# 
.SYNOPSIS 
Runs 'docker-compose down'. 
.CATEGORY
Docker Commands
#>
function dkcpd { [CmdletBinding()] param([switch]$Help, [Parameter(ValueFromRemainingArguments=$true)][string[]]$args) if ($Help) { Get-Help $MyInvocation.MyCommand.Name; return } Write-Host "🛑 Stopping Docker Compose..." -ForegroundColor Yellow; docker-compose down @args }
<# 
.SYNOPSIS 
Prunes unused Docker volumes ('docker volume prune'). 
.CATEGORY
Docker Commands
#>
function fix-volume { [CmdletBinding()] param([switch]$Help) if ($Help) { Get-Help $MyInvocation.MyCommand.Name; return } Write-Host "🧹 Pruning Docker volumes..." -ForegroundColor Magenta; docker volume prune }
<# 
.SYNOPSIS 
Prunes unused Docker images ('docker image prune'). 
.CATEGORY
Docker Commands
#>
function fix-image { [CmdletBinding()] param([switch]$Help) if ($Help) { Get-Help $MyInvocation.MyCommand.Name; return } Write-Host "🧹 Pruning Docker images..." -ForegroundColor Magenta; docker image prune }

#endregion

#region AWS LOCALSTACK COMMANDS
# ------------------------------------------------------------------------------
#  Commands for interacting with AWS services via LocalStack.
# ------------------------------------------------------------------------------

$localStackUrl = "http://127.0.0.1:4566"

<# 
.SYNOPSIS 
Lists SQS queues. 
.CATEGORY
AWS LocalStack Commands
#>
function list-queue { [CmdletBinding()] param([switch]$Help) if ($Help) { Get-Help $MyInvocation.MyCommand.Name; return } awslocal --endpoint-url=$localStackUrl sqs list-queues }
<# 
.SYNOPSIS 
Creates an SQS queue. 
.CATEGORY
AWS LocalStack Commands
#>
function create-queue { [CmdletBinding()] param([string]$QueueName, [switch]$Help) if ($Help) { Get-Help $MyInvocation.MyCommand.Name; return } awslocal --endpoint-url=$localStackUrl sqs create-queue --queue-name=$QueueName }
<# 
.SYNOPSIS 
Purges all messages from an SQS queue. 
.CATEGORY
AWS LocalStack Commands
#>
function clear-queue { [CmdletBinding()] param([string]$QueueUrl, [switch]$Help) if ($Help) { Get-Help $MyInvocation.MyCommand.Name; return } awslocal --endpoint-url=$localStackUrl sqs purge-queue --queue-url $QueueUrl }
<# 
.SYNOPSIS 
Sends a message to an SQS queue. 
.CATEGORY
AWS LocalStack Commands
#>
function send-mess { [CmdletBinding()] param([string]$QueueUrl, [string]$MessageBody, [string]$GroupId = "default-group", [switch]$Help) if ($Help) { Get-Help $MyInvocation.MyCommand.Name; return } awslocal --endpoint-url=$localStackUrl sqs send-message --queue-url $QueueUrl --message-body $MessageBody --message-group-id $GroupId }
<# 
.SYNOPSIS 
Receives messages from an SQS queue. 
.CATEGORY
AWS LocalStack Commands
#>
function receive-mess { [CmdletBinding()] param([string]$QueueUrl, [switch]$Help) if ($Help) { Get-Help $MyInvocation.MyCommand.Name; return } awslocal --endpoint-url=$localStackUrl sqs receive-message --queue-url $QueueUrl }
<# 
.SYNOPSIS 
Gets all attributes for an SQS queue. 
.CATEGORY
AWS LocalStack Commands
#>
function number-mes { [CmdletBinding()] param([string]$QueueUrl, [switch]$Help) if ($Help) { Get-Help $MyInvocation.MyCommand.Name; return } awslocal --endpoint-url=$localStackUrl sqs get-queue-attributes --queue-url $QueueUrl --attribute-names All }

#endregion

#region HELP AND DOCUMENTATION
# ------------------------------------------------------------------------------
#  Command to display a summary of all custom functions and aliases.
# ------------------------------------------------------------------------------
<#
.SYNOPSIS
Displays a categorized list of all custom commands available in the profile.
.CATEGORY
Help and Documentation
#>
function Get-CustomCommands {
    [CmdletBinding()]
    param([switch]$Help)
    if ($Help) { Get-Help $MyInvocation.MyCommand.Name; return }

    Write-Host "`n╔═════════════════════════════════════╗" -ForegroundColor Cyan
    Write-Host "║      Custom PowerShell Commands     ║" -ForegroundColor Cyan
    Write-Host "╚═════════════════════════════════════╝`n" -ForegroundColor Cyan

    $profilePath = $PROFILE
    if ($PROFILE -is [string]){
        $profilePath = $PROFILE
    } else {
        $profilePath = $PROFILE.CurrentUserCurrentHost
    }

    $profileFunctions = Get-Command -CommandType Function | Where-Object { $_.ScriptBlock.File -eq $profilePath }
    $categorizedCommands = @{}

    foreach ($func in $profileFunctions) {
        if ($func.Name -eq 'Get-CustomCommands') { continue }

        $helpContent = $func.ScriptBlock.Ast.GetHelpContent()
        if ($helpContent -and $helpContent.Synopsis) {
            $synopsis = $helpContent.Synopsis.Trim()
            
            $category = "Uncategorized"
            $customCategory = $helpContent.CustomBlocks | Where-Object { $_.TypeName -eq 'CATEGORY' }
            if ($customCategory) {
                $category = $customCategory.Content.Trim()
            }

            if (-not $categorizedCommands.ContainsKey($category)) {
                $categorizedCommands[$category] = [System.Collections.Generic.List[string]]::new()
            }
            
            $aliases = (Get-Command -CommandType Alias | Where-Object { $_.ResolvedCommand.Name -eq $func.Name }).Name
            $displayName = if ($aliases) { "$($func.Name) ($($aliases -join ', '))" } else { $func.Name }

            $categorizedCommands[$category].Add(("  {0,-30} - {1}" -f $displayName, $synopsis))
        }
    }

    $categoryOrder = @(
        "System & Utility Commands",
        "Project Navigation",
        ".NET Development Commands",
        "Git Commands",
        "Docker Commands",
        "AWS LocalStack Commands",
        "Fun"
    )

    foreach ($category in $categoryOrder) {
        if ($categorizedCommands.ContainsKey($category)) {
            Write-Host $category -ForegroundColor Yellow
            Write-Host ('-' * $category.Length) -ForegroundColor Gray
            $categorizedCommands[$category] | Sort-Object | ForEach-Object { Write-Host $_ -ForegroundColor White }
            Write-Host ""
            $categorizedCommands.Remove($category)
        }
    }

    if ($categorizedCommands.Count -gt 0) {
        $categorizedCommands.Keys | Sort-Object | ForEach-Object {
            $category = $_
            Write-Host $category -ForegroundColor Yellow
            Write-Host ('-' * $category.Length) -ForegroundColor Gray
            $categorizedCommands[$category] | Sort-Object | ForEach-Object { Write-Host $_ -ForegroundColor White }
            Write-Host ""
        }
    }

    Write-Host "`nType a command with '-Help' (e.g., 'proj -Help') for more details." -ForegroundColor Cyan
}
Get-CustomCommands
#endregion
