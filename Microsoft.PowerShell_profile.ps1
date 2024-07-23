# INIT_COMMANDS
# CUSTOM_HOTKEYS 
# DOTNET_COMMANDS 
# GIT_COMMANDS 
# DOCKER_COMMANDS 
# AWS_COMMANDS 

# ============================INIT_COMMANDS============================ 

$env:POSH_THEMES_PATH = "$env:USERPROFILE\Documents\PowerShell\powershell-themes"
oh-my-posh --init --shell pwsh --config "$env:POSH_THEMES_PATH\neko.omp.json" | Invoke-Expression

# ALIAS
Set-Alias -Name ip -Value ipconfig
Set-Alias -Name np -Value notepad
Set-Alias -Name v -Value nvim
Set-Alias -Name o -Value ollama

# Import the PSReadline module
Import-Module PSReadline 
Import-Module -Name Terminal-Icons

# Set PSReadline options
Set-PSReadLineOption -EditMode Windows
Set-PSReadLineOption -PredictionSource History
Set-PSReadLineOption -PredictionViewStyle ListView 
    
# Define custom key bindings for PSReadline
Set-PSReadLineKeyHandler -Function AcceptSuggestion -Key 'Ctrl+Spacebar'
Set-PSReadLineKeyHandler -Key UpArrow -Function HistorySearchBackward
Set-PSReadLineKeyHandler -Key DownArrow -Function HistorySearchForward 


# ============================CUSTOM_HOTKEYS============================ 
if ($host.Name -eq 'ConsoleHost' -or $host.Name -eq 'Visual Studio Code Host') {

    # Configure PSReadline colors for syntax highlighting
    Set-PSReadlineOption -Color @{
        "Command"          = [ConsoleColor]::Green
        "Parameter"        = [ConsoleColor]::Gray
        "Operator"         = [ConsoleColor]::Magenta
        "Variable"         = [ConsoleColor]::Yellow
        "String"           = [ConsoleColor]::Yellow
        "Number"           = [ConsoleColor]::Yellow
        "Type"             = [ConsoleColor]::Cyan
        "Comment"          = [ConsoleColor]::DarkCyan
        "InlinePrediction" = '#70A99F'
    }

    # Ctrl+Shift+b -> dotnet build
    # Define a custom key binding for building the current directory with dotnet
    Set-PSReadLineKeyHandler -Key Ctrl+Shift+b `
        -BriefDescription BuildCurrentDirectory `
        -LongDescription "Build the current directory" `
        -ScriptBlock {
        [Microsoft.PowerShell.PSConsoleReadLine]::RevertLine()
        [Microsoft.PowerShell.PSConsoleReadLine]::Insert("dotnet build")
        [Microsoft.PowerShell.PSConsoleReadLine]::AcceptLine()
    }
    # Ctrl+Shift+t -> dotnet test
    Set-PSReadLineKeyHandler -Key Ctrl+Shift+t `
        -BriefDescription BuildCurrentDirectory `
        -LongDescription "Build the current directory" `
        -ScriptBlock {
        [Microsoft.PowerShell.PSConsoleReadLine]::RevertLine()
        [Microsoft.PowerShell.PSConsoleReadLine]::Insert("dotnet test")
        [Microsoft.PowerShell.PSConsoleReadLine]::AcceptLine()
    }
    # Auto filled {} () []
    Set-PSReadLineKeyHandler -Key '(', '{', '[' `
        -BriefDescription InsertPairedBraces `
        -LongDescription "Insert matching braces" `
        -ScriptBlock {
        param($key, $arg)

        $closeChar = switch ($key.KeyChar) {
            <#case#> '(' { [char]')'; break }
            <#case#> '{' { [char]'}'; break }
            <#case#> '[' { [char]']'; break }
        }

        $selectionStart = $null
        $selectionLength = $null
        [Microsoft.PowerShell.PSConsoleReadLine]::GetSelectionState([ref]$selectionStart, [ref]$selectionLength)

        $line = $null
        $cursor = $null
        [Microsoft.PowerShell.PSConsoleReadLine]::GetBufferState([ref]$line, [ref]$cursor)
        
        if ($selectionStart -ne -1) {
            # Text is selected, wrap it in brackets
            [Microsoft.PowerShell.PSConsoleReadLine]::Replace($selectionStart, $selectionLength, $key.KeyChar + $line.SubString($selectionStart, $selectionLength) + $closeChar)
            [Microsoft.PowerShell.PSConsoleReadLine]::SetCursorPosition($selectionStart + $selectionLength + 2)
        }
        else {
            # No text is selected, insert a pair
            [Microsoft.PowerShell.PSConsoleReadLine]::Insert("$($key.KeyChar)$closeChar")
            [Microsoft.PowerShell.PSConsoleReadLine]::SetCursorPosition($cursor + 1)
        }
    }
    # Auto escape )]}
    Set-PSReadLineKeyHandler -Key ')', ']', '}' `
        -BriefDescription SmartCloseBraces `
        -LongDescription "Insert closing brace or skip" `
        -ScriptBlock {
        param($key, $arg)

        $line = $null
        $cursor = $null
        [Microsoft.PowerShell.PSConsoleReadLine]::GetBufferState([ref]$line, [ref]$cursor)

        if ($line[$cursor] -eq $key.KeyChar) {
            [Microsoft.PowerShell.PSConsoleReadLine]::SetCursorPosition($cursor + 1)
        }
        else {
            [Microsoft.PowerShell.PSConsoleReadLine]::Insert("$($key.KeyChar)")
        }
    }
    # delete match })]
    Set-PSReadLineKeyHandler -Key Backspace `
        -BriefDescription SmartBackspace `
        -LongDescription "Delete previous character or matching quotes/parens/braces" `
        -ScriptBlock {
        param($key, $arg)

        $line = $null
        $cursor = $null
        [Microsoft.PowerShell.PSConsoleReadLine]::GetBufferState([ref]$line, [ref]$cursor)

        if ($cursor -gt 0) {
            $toMatch = $null
            if ($cursor -lt $line.Length) {
                switch ($line[$cursor]) {
                    <#case#> '"' { $toMatch = '"'; break }
                    <#case#> "'" { $toMatch = "'"; break }
                    <#case#> ')' { $toMatch = '('; break }
                    <#case#> ']' { $toMatch = '['; break }
                    <#case#> '}' { $toMatch = '{'; break }
                }
            }

            if ($toMatch -ne $null -and $line[$cursor - 1] -eq $toMatch) {
                [Microsoft.PowerShell.PSConsoleReadLine]::Delete($cursor - 1, 2)
            }
            else {
                [Microsoft.PowerShell.PSConsoleReadLine]::BackwardDeleteChar($key, $arg)
            }
        }
    }

    # F7 -> open history form
    Set-PSReadLineKeyHandler -Key F7 `
        -BriefDescription History `
        -LongDescription 'Show command history' `
        -ScriptBlock {
        $pattern = $null
        [Microsoft.PowerShell.PSConsoleReadLine]::GetBufferState([ref]$pattern, [ref]$null)
        if ($pattern) {
            $pattern = [regex]::Escape($pattern)
        }

        $history = [System.Collections.ArrayList]@(
            $last = ''
            $lines = ''
            foreach ($line in [System.IO.File]::ReadLines((Get-PSReadLineOption).HistorySavePath)) {
                if ($line.EndsWith('`')) {
                    $line = $line.Substring(0, $line.Length - 1)
                    $lines = if ($lines) {
                        "$lines`n$line"
                    }
                    else {
                        $line
                    }
                    continue
                }

                if ($lines) {
                    $line = "$lines`n$line"
                    $lines = ''
                }

                if (($line -cne $last) -and (!$pattern -or ($line -match $pattern))) {
                    $last = $line
                    $line
                }
            }
        )
        $history.Reverse()

        $command = $history | Out-GridView -Title History -PassThru
        if ($command) {
            [Microsoft.PowerShell.PSConsoleReadLine]::RevertLine()
            [Microsoft.PowerShell.PSConsoleReadLine]::Insert(($command -join "`n"))
        }
    }

    <#
    # .SYNOPSIS
    #  Clears the command history, including the saved-to-file history, if applicable.
    #>
    function Clear-SavedHistory {
        [CmdletBinding(ConfirmImpact = 'High', SupportsShouldProcess)]
        param(    
        )

        # Debugging: For testing you can simulate not having PSReadline loaded with
        #            Remove-Module PSReadline -Force
        $havePSReadline = ($null -ne (Get-Module -EA SilentlyContinue PSReadline))

        Write-Verbose "PSReadline present: $havePSReadline"

        $target = if ($havePSReadline) { "entire command history, including from previous sessions" } else { "command history" } 

        if (-not $pscmdlet.ShouldProcess($target)) {
            return
        }

        if ($havePSReadline) {
        
            Clear-Host

            # Remove PSReadline's saved-history file.
            if (Test-Path (Get-PSReadlineOption).HistorySavePath) { 
                # Abort, if the file for some reason cannot be removed.
                Remove-Item -EA Stop (Get-PSReadlineOption).HistorySavePath 
                # To be safe, we recreate the file (empty). 
                $null = New-Item -Type File -Path (Get-PSReadlineOption).HistorySavePath
            }

            # Clear PowerShell's own history 
            Clear-History

            # Clear PSReadline's *session* history.
            # General caveat (doesn't apply here, because we're removing the saved-history file):
            #   * By default (-HistorySaveStyle SaveIncrementally), if you use
            #    [Microsoft.PowerShell.PSConsoleReadLine]::ClearHistory(), any sensitive
            #    commands *have already been saved to the history*, so they'll *reappear in the next session*. 
            #   * Placing `Set-PSReadlineOption -HistorySaveStyle SaveAtExit` in your profile 
            #     SHOULD help that, but as of PSReadline v1.2, this option is BROKEN (saves nothing). 
            [Microsoft.PowerShell.PSConsoleReadLine]::ClearHistory()

        }
        else {
            # Without PSReadline, we only have a *session* history.

            Clear-Host
        
            # Clear the doskey library's buffer, used pre-PSReadline. 
            # !! Unfortunately, this requires sending key combination Alt+F7.
            # Thanks, https://stackoverflow.com/a/13257933/45375
            $null = [system.reflection.assembly]::loadwithpartialname("System.Windows.Forms")
            [System.Windows.Forms.SendKeys]::Sendwait('%{F7 2}')

            # Clear PowerShell's own history 
            Clear-History

        }
    }

    <#
    # <randome-theme> random theme each create new terminal or enter "go" command
    #>
    function Set-RandomOhMyPoshTheme {
        $themesPath = $env:POSH_THEMES_PATH
        $themeFiles = Get-ChildItem -Path $themesPath -Filter *.omp.json -File
        $randomThemeFile = $themeFiles | Get-Random -Count 1
        $fullThemePath = Join-Path -Path $themesPath -ChildPath $randomThemeFile.Name

        oh-my-posh --init --shell pwsh --config $fullThemePath | Invoke-Expression
        "$fullThemePath"
    }
    # Set-RandomOhMyPoshTheme

    function fact {
        irm -Uri https://uselessfacts.jsph.pl/random.json?language=en | Select -ExpandProperty text
    }

    function joke {
        irm https://icanhazdadjoke.com/ -Headers @{accept = 'application/json' } | select -ExpandProperty joke
    }
}


# ============================DOTNET_COMMANDS============================ 
function dr { dotnet run } 
function dw { dotnet watch } 
function db { dotnet build } 
function df { dotnet format } 

# DOTNET entity framework 
function du { dotnet ef database update } 
function da { dotnet ef migrations add $1 } 
function dd { dotnet ef database drop } 
function dremove { dotnet ef migrations remove } 

function console {
    param( [string]$projectName ) 
    mkdir $projectName
    Set-Location $projectName
    dotnet new console -n $projectName
    Set-Location $projectName
    code .
    dotnet new gitignore
    git init
    git add .
    git commit -m "init"
    dotnet run
}

function webapi {
    param( [string]$projectName )
    mkdir $projectName
    Set-Location $projectName
    dotnet new webapi -n $projectName
    Set-Location $projectName
    code .
    dotnet new gitignore
    git init
    git add .
    git commit -m "Initial commit"
    dotnet run
}
# ============================GIT_COMMANDS============================ 
function gc { 
    param( [string]$commitMessage ) 
    git commit -m $commitMessage 
} 
function co { param( [string]$branchName ) git checkout $branchName } 
function cob { param( [string]$branchName ) git checkout -b $branchName } 
function gca { git commit --amend } 
function ga { git add . } 
function glo { git log --graph --oneline --decorate --all } 
function glg { git log } 
function gs { git status } 
function gpu { git pull } 
function gf { git fetch --all } 
function gus { git push } 
function guf { git push -f } 
function gme { param( [string]$branchName ) git merge $branchName } 
function gms { param( [string]$branchName ) git merge --squash $branchName } 
function glc { git show --stat HEAD } 
function grh { git reset --hard } 
function gr { git reset HEAD~ } 
function gb { git branch } 
function gbd { param( [string]$branchName ) git branch -d $branchName } 

# ============================DOCKER_COMMANDS============================ 
function dkcl { docker container ls } 
function dkrmac { docker rm $(docker container ls -aq) } 
function dkstac { docker stop $(docker container ls -aq) } 
function dkcpu { docker-compose up } 
function dkcpub { docker-compose up --build } 
function fix-volume { docker volume prune } 
function fix-image { docker image prune } 

# ============================AWS_COMMANDS============================ 
function clear-queue { awslocal --endpoint-url=http://127.0.0.1:4566 sqs purge-queue --queue-url http://127.0.0.1:4566/000000000000/MessageQueue.fifo } 
function list-queue { awslocal --endpoint-url=http://127.0.0.1:4566 sqs list-queues } 
# eg: http://127.0.0.1:4566/000000000000/MessageQueue.fifo 
function del-queue { awslocal --endpoint-url=http://localhost:24870 sqs delete-queue --queue-name=AppEventQueue } 
function create-queue { awslocal --endpoint-url=http://localhost:4566 sqs create-queue --queue-name=AppEventQueue } 
function number-mes { awslocal sqs get-queue-attributes --queue-url http://127.0.0.1:4566/000000000000/MessageQueue.fifo --attribute-names All } 
function send-mess { awslocal --endpoint-url=http://127.0.0.1:4566 sqs send-message --message-group-id 098 --queue-url http://127.0.0.1:4566/000000000000/MessageQueue.fifo --message-body "test msq" } 
function receive-mess { awslocal --endpoint-url=http://127.0.0.1:4566/_aws/sqs/messages sqs receive-message --queue-url http://127.0.0.1:4566/000000000000/AppEventQueue } 

# ============================OLLAMA_COMMANDS============================ 
function ol { ollama list } 
function or { param([string]$model)ollama run $model } 
function orv { param([string]$model)ollama run $model --verbose }

# ============================OTHER_COMMANDS============================
function go { . $PROFILE } 
function folder { start . } 
function read { param( [string]$filePath ) code -r $filePath } 
# Alias quick access folder
function clean() { cd "D:\1.Project\1.clean-architech-template-c#\api-clean-architecture-net-8.0" } 
function pws() { cd "C:\Users\TruongNhon\Documents\PowerShell" } 
