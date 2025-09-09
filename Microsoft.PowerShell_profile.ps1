# ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
#  Enhanced PowerShell Profile
#  Combines Oh My Posh, custom hotkeys, and powerful command functions for
#  .NET, Git, Docker, and AWS workflows.
# ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

#region Initial Setup & Theme

Write-Host "🚀 Loading Enhanced PowerShell Profile..." -ForegroundColor Cyan

# --- Oh My Posh Theme Initialization ---
# To change themes, update the $env:THEME variable.
$env:POSH_THEMES_PATH = Join-Path -Path $env:USERPROFILE -ChildPath "Documents\PowerShell\powershell-themes"
$env:THEME = "neko" # You can change your theme here
$themePath = Join-Path -Path $env:POSH_THEMES_PATH -ChildPath "$($env:THEME).omp.json"
if (Test-Path $themePath) {
    oh-my-posh --init --shell pwsh --config $themePath | Invoke-Expression
}
else {
    Write-Warning "Oh My Posh theme '$($env:THEME)' not found at '$themePath'."
}

# --- Module Loading ---
# Ensures required modules are loaded for an interactive session.
if ($Host.Name -eq 'ConsoleHost' -or $host.Name -eq 'Visual Studio Code Host') {
    try {
        Write-Progress -Activity "Loading Modules" -Status "PSReadLine" -PercentComplete 33
        Import-Module PSReadLine -Force -ErrorAction SilentlyContinue

        Write-Progress -Activity "Loading Modules" -Status "Terminal-Icons" -PercentComplete 66
        Import-Module Terminal-Icons -Force -ErrorAction SilentlyContinue

        Write-Progress -Activity "Loading Modules" -Status "Posh-Git" -PercentComplete 90
        if (Get-Module -ListAvailable -Name posh-git) {
            Import-Module posh-git -Force -ErrorAction SilentlyContinue
        }
        Write-Progress -Activity "Loading Modules" -Status "Complete" -PercentComplete 100 -Completed
    }
    catch {
        Write-Warning "An error occurred while loading modules: $_"
    }
}

# --- Core Aliases ---
Set-Alias -Name ip       -Value Get-NetIPConfiguration -Force
Set-Alias -Name cls      -Value Clear-Host -Force
Set-Alias -Name grep     -Value Select-String -Force
Set-Alias -Name which    -Value Get-Command -Force
Set-Alias -Name code     -Value "code" -Option AllScope # Ensure 'code' works everywhere
Set-Alias -Name v        -Value nvim -Force
Set-Alias -Name o        -Value ollama -Force
Set-Alias -Name go       -Value Reload-Profile -Force # Reloads this profile
Set-Alias -Name commands -Value Get-CustomCommands -Force

# --- Quick Navigation Aliases ---
Set-Alias -Name ..  -Value Set-LocationParent -Force
Set-Alias -Name ... -Value Set-LocationGrandParent -Force

function Set-LocationParent { Set-Location .. }
function Set-LocationGrandParent { Set-Location ../.. }

#endregion

#region PSReadLine Configuration & Hotkeys

# --- PSReadLine General Options ---
Set-PSReadLineOption -EditMode Windows
Set-PSReadLineOption -PredictionSource History
Set-PSReadLineOption -PredictionViewStyle ListView
Set-PSReadLineOption -BellStyle None

# --- Custom Color Scheme ---
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
    "InlinePrediction" = '#70A99F' # Custom hex color for prediction
}

# --- Custom Key Bindings ---
# History Search & Suggestions
Set-PSReadLineKeyHandler -Key UpArrow -Function HistorySearchBackward
Set-PSReadLineKeyHandler -Key DownArrow -Function HistorySearchForward
Set-PSReadLineKeyHandler -Key 'Ctrl+Spacebar' -Function Complete # Changed to standard completion hotkey

# .NET Build & Test Hotkeys
Set-PSReadLineKeyHandler -Key 'Ctrl+Shift+b' -ScriptBlock {
    [Microsoft.PowerShell.PSConsoleReadLine]::RevertLine()
    [Microsoft.PowerShell.PSConsoleReadLine]::Insert("db") # Uses the 'db' function below
    [Microsoft.PowerShell.PSConsoleReadLine]::AcceptLine()
}
Set-PSReadLineKeyHandler -Key 'Ctrl+Shift+t' -ScriptBlock {
    [Microsoft.PowerShell.PSConsoleReadLine]::RevertLine()
    [Microsoft.PowerShell.PSConsoleReadLine]::Insert("dt") # Uses the 'dt' function below
    [Microsoft.PowerShell.PSConsoleReadLine]::AcceptLine()
}

# F7 for graphical history viewer
Set-PSReadLineKeyHandler -Key F7 -ScriptBlock {
    $command = Get-History | Out-GridView -Title 'Command History' -PassThru
    if ($command) {
        [Microsoft.PowerShell.PSConsoleReadLine]::RevertLine()
        [Microsoft.PowerShell.PSConsoleReadLine]::Insert($command.CommandLine)
    }
}

# Auto-pairing of brackets and quotes
Set-PSReadLineKeyHandler -Key '(', '{', '[', '"', "'" -ScriptBlock {
    param($key, $arg)
    $openChar = $key.KeyChar
    $closeChar = switch ($openChar) {
        '(' { ')' }
        '{' { '}' }
        '[' { ']' }
        '"' { '"' }
        "'" { "'" }
    }
    [Microsoft.PowerShell.PSConsoleReadLine]::Insert("$openChar$closeChar")
    [Microsoft.PowerShell.PSConsoleReadLine]::BackwardChar()
}

# Smart backspace to delete pairs
Set-PSReadLineKeyHandler -Key 'Backspace' -ScriptBlock {
    $line = ''
    $cursor = 0
    [Microsoft.PowerShell.PSConsoleReadLine]::GetBufferState([ref]$line, [ref]$cursor)
    if ($cursor -gt 0) {
        $charBehind = $line[$cursor - 1]
        $charAhead = if ($cursor -lt $line.Length) { $line[$cursor] } else { $null }
        $pairs = @{ '(' = ')'; '{' = '}'; '[' = ']'; '"' = '"'; "'" = "'" }

        if ($pairs.ContainsKey($charBehind) -and $pairs[$charBehind] -eq $charAhead) {
            [Microsoft.PowerShell.PSConsoleReadLine]::Delete($cursor - 1, 2)
        }
        else {
            [Microsoft.PowerShell.PSConsoleReadLine]::BackwardDeleteChar()
        }
    }
}

#endregion

#region .NET Development Commands

# --- Core Build Commands ---
function dr { Write-Host "🚀 Running project..." -ForegroundColor Green; dotnet run @args }
function dw { Write-Host "👀 Watching for changes..." -ForegroundColor Cyan; dotnet watch @args }
function db { Write-Host "🔨 Building project..." -ForegroundColor Blue; dotnet build @args }
function df { Write-Host "💅 Formatting code..." -ForegroundColor Magenta; dotnet format @args }
function dt { Write-Host "🧪 Running tests..." -ForegroundColor Yellow; dotnet test @args }

# --- Entity Framework Commands ---
function du {
    param([string]$Context)
    Write-Host "📈 Updating database..." -ForegroundColor Green
    $cmd = "dotnet ef database update"
    if ($Context) { $cmd += " --context $Context" }
    Invoke-Expression $cmd
}
function da {
    param([Parameter(Mandatory = $true)][string]$MigrationName, [string]$Context)
    Write-Host "➕ Adding migration: $MigrationName" -ForegroundColor Cyan
    $cmd = "dotnet ef migrations add $MigrationName"
    if ($Context) { $cmd += " --context $Context" }
    Invoke-Expression $cmd
}
function dd {
    param([string]$Context)
    Write-Host "🔥 Dropping database..." -ForegroundColor Red
    $confirmation = Read-Host "Are you sure you want to drop the database? (y/N)"
    if ($confirmation -eq 'y') {
        $cmd = "dotnet ef database drop --force"
        if ($Context) { $cmd += " --context $Context" }
        Invoke-Expression $cmd
        Write-Host "Database dropped." -ForegroundColor Green
    }
    else { Write-Host "Operation cancelled." -ForegroundColor Yellow }
}
function dremove {
    param([string]$Context)
    Write-Host "⏪ Removing last migration..." -ForegroundColor Yellow
    $cmd = "dotnet ef migrations remove"
    if ($Context) { $cmd += " --context $Context" }
    Invoke-Expression $cmd
}

# --- Project Scaffolding ---
function New-ConsoleProject {
    param(
        [Parameter(Mandatory = $true)] [string]$ProjectName,
        [switch]$SkipGit
    )
    Write-Host "Creating new Console project: $ProjectName" -ForegroundColor Green
    mkdir $ProjectName; cd $ProjectName
    dotnet new console -n $ProjectName
    dotnet new gitignore
    if (-not $SkipGit) {
        git init; git add .; git commit -m "Initial commit"
    }
    code .
    dotnet run
}
Set-Alias -Name console -Value New-ConsoleProject

function New-WebApiProject {
    param(
        [Parameter(Mandatory = $true)] [string]$ProjectName,
        [switch]$SkipGit
    )
    Write-Host "Creating new Web API project: $ProjectName" -ForegroundColor Green
    mkdir $ProjectName; cd $ProjectName
    dotnet new webapi -n $ProjectName
    dotnet new gitignore
    if (-not $SkipGit) {
        git init; git add .; git commit -m "Initial commit"
    }
    code .
    dotnet run
}
Set-Alias -Name webapi -Value New-WebApiProject

#endregion

#region Git Commands

function gs { Write-Host "🔍 Git Status" -ForegroundColor Yellow; git status @args }
function ga { Write-Host "✅ Staging all changes..." -ForegroundColor Green; git add . }
function gcmt {
    param([Parameter(Mandatory = $true, ValueFromRemainingArguments = $true)][string[]]$Message)
    $commitMessage = $Message -join ' '
    Write-Host "📝 Committing with message: `"$commitMessage`"" -ForegroundColor Cyan
    git commit -m "$commitMessage"
}
function gca { Write-Host "✍️ Amending previous commit..." -ForegroundColor Cyan; git commit --amend @args }
function co { param([string]$branchName); Write-Host "Check out branch: $branchName" -ForegroundColor Green; git checkout $branchName }
function cob { param([string]$branchName); Write-Host "Check out and create a new branch: $branchName" -ForegroundColor Green; git checkout -b $branchName }
function glg { git log --graph --oneline --decorate --all }
function gpu { Write-Host "⏬ Pulling changes from remote..." -ForegroundColor Blue; git pull @args }
function gus { Write-Host "⏫ Pushing changes to remote..." -ForegroundColor Blue; git push @args }
function guf { Write-Host "⏫ Force pushing changes..." -ForegroundColor Red; git push --force @args }
function gf { Write-Host "🔎 Fetching from all remotes..." -ForegroundColor Blue; git fetch --all --prune }
function gb { Write-Host "🌿 Branches:" -ForegroundColor Green; git branch @args }
function gbd { param([string]$branchName); git branch -d $branchName }
function gms {
    param([Parameter(Mandatory = $true)] [string]$BranchName)
    Write-Host "Squash merging branch: $BranchName" —ForegroundColor Yellow
    git merge --squash $BranchName
    Write-Host "Don't forget to commit the squashed changes! "—ForegroundColor Cyan
}

#endregion

#region Docker Commands

function dkcl {
    param([switch]$All)
    Write-Host "🐳 Docker Containers:" -ForegroundColor Blue
    if ($All) { docker container ls -a } else { docker container ls }
}
function dkrmac {
    Write-Host "🗑️ Removing ALL containers..." -ForegroundColor Red
    $confirmation = Read-Host "This will remove ALL containers. Are you sure? (y/N)"
    if ($confirmation -eq 'y') {
        $containers = docker container ls -aq
        if ($containers) {
            docker rm $containers
            Write-Host "All containers removed." -ForegroundColor Green
        }
        else { Write-Host "ℹ️ No containers to remove." -ForegroundColor Yellow }
    }
    else { Write-Host "Operation cancelled." -ForegroundColor Yellow }
}
function dkstac {
    Write-Host "⏹️ Stopping ALL running containers..." -ForegroundColor Yellow
    $containers = docker container ls -q
    if ($containers) {
        docker stop $containers
        Write-Host "All containers stopped." -ForegroundColor Green
    }
    else { Write-Host "ℹ️ No running containers to stop." -ForegroundColor Yellow }
}
function dkcpu { Write-Host "🚀 Starting Docker Compose..." -ForegroundColor Green; docker-compose up @args }
function dkcpub { Write-Host "🔨 Building and starting Docker Compose..." -ForegroundColor Blue; docker-compose up --build @args }
function dkcpd { Write-Host "🛑 Stopping Docker Compose..." -ForegroundColor Yellow; docker-compose down @args }
function fix-volume { Write-Host "🧹 Pruning Docker volumes..." -ForegroundColor Magenta; docker volume prune }
function fix-image { Write-Host "🧹 Pruning Docker images..." -ForegroundColor Magenta; docker image prune }

#endregion

#region AWS LocalStack Commands

$localStackUrl = "http://127.0.0.1:4566"

function list-queue { awslocal --endpoint-url=$localStackUrl sqs list-queues }
function create-queue { param([string]$QueueName); awslocal --endpoint-url=$localStackUrl sqs create-queue --queue-name=$QueueName }
function clear-queue { param([string]$QueueUrl); awslocal --endpoint-url=$localStackUrl sqs purge-queue --queue-url $QueueUrl }
function send-mess {
    param([string]$QueueUrl, [string]$MessageBody, [string]$GroupId = "default-group")
    awslocal --endpoint-url=$localStackUrl sqs send-message --queue-url $QueueUrl --message-body $MessageBody --message-group-id $GroupId
}
function receive-mess { param([string]$QueueUrl); awslocal --endpoint-url=$localStackUrl sqs receive-message --queue-url $QueueUrl }
function number-mes { param([string]$QueueUrl); awslocal --endpoint-url=$localStackUrl sqs get-queue-attributes --queue-url $QueueUrl --attribute-names All }

#endregion

#region System Utilities & Fun

# --- Utility Functions ---
function Reload-Profile { . $PROFILE; Write-Host "✅ Profile reloaded." -ForegroundColor Green }
function folder { Invoke-Item . }
function edit-profile { code $PROFILE }
function mkcd { param([string]$DirName); mkdir $DirName; cd $DirName }
function pw { cd "C:\Users\TruongNhon\Documents\PowerShell"; code . }

# --- Fun Commands ---
function fact { irm -Uri https://uselessfacts.jsph.pl/random.json?language=en | Select -ExpandProperty text }
function joke { irm https://icanhazdadjoke.com/ -Headers @{accept = 'application/json' } | select -ExpandProperty joke }

# --- History Management ---
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

#endregion

#region Help and Documentation

function Get-CustomCommands {
    Write-Host "
╔═════════════════════════════════════╗
║      Custom PowerShell Commands     ║
╚═════════════════════════════════════╝
" -ForegroundColor Cyan

    $commandHelp = @{
        ".NET"   = @(
            "db, dt, dr, dw, df     - Build, Test, Run, Watch, Format"
            "du, da, dd, dremove    - EF Core commands (Update, Add, Drop, Remove)"
            "console, webapi        - Create new .NET projects"
        )
        "Git"    = @(
            "gs, ga, gcmt, gca      - Status, Add, Commit, Amend"
            "co, cob, gb, gbd       - Branch operations (Checkout, Create, List, Delete)"
            "gpu, gus, guf, gf      - Pull, Push, Force Push, Fetch"
            "glg                    - Show pretty log graph"
        )
        "Docker" = @(
            "dkcl [-All]            - List containers"
            "dkstac, dkrmac         - Stop/Remove all containers (with confirmation)"
            "dkcpu, dkcpub, dkcpd   - Docker Compose Up, Up --build, Down"
            "fix-volume, fix-image  - Prune unused volumes or images"
        )
        "System" = @(
            "go                     - Reload this profile"
            "edit-profile           - Open this profile in VS Code"
            "folder                 - Open current directory in File Explorer"
            "mkcd <dir>             - Make a directory and enter it"
            "Clear-SavedHistory     - Wipes all command history"
            "ip, cls, grep, which   - Core aliases"
            ".., ...                - Navigate up one or two directories"
        )
    }

    foreach ($category in $commandHelp.Keys) {
        Write-Host "`n$category" -ForegroundColor Yellow
        Write-Host ('-' * $category.Length) -ForegroundColor Gray
        foreach ($command in $commandHelp[$category]) { Write-Host "  $command" -ForegroundColor White }
    }
    Write-Host "`nType 'commands' again to see this list. Use 'Get-PSReadlineKeyHandler' to see all hotkeys." -ForegroundColor Cyan
}

#endregion