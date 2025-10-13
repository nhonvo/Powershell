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
function gs { 
    [CmdletBinding()] 
    param([Parameter(ValueFromRemainingArguments=$true)][string[]]$args) 
    Write-Host "🔍 Git Status" -ForegroundColor Yellow; git status @args 
}
<# 
.SYNOPSIS 
Stages all changes ('git add .'). 
.CATEGORY
Git Commands
#>
function ga { 
    [CmdletBinding()] 
    param() 
    Write-Host "✅ Staging all changes..." -ForegroundColor Green; git add . 
}
<# 
.SYNOPSIS 
Commits with a message ('git commit -m'). 
.CATEGORY
Git Commands
#>
function gcmt { 
    [CmdletBinding()] 
    param([Parameter(Mandatory=$true, ValueFromRemainingArguments=$true)][string[]]$Message) 
    $commitMessage = $Message -join ' '
    Write-Host "📝 Committing with message: `"$commitMessage`"" -ForegroundColor Cyan
    git commit -m "$commitMessage" 
}
<# 
.SYNOPSIS 
Amends the previous commit ('git commit --amend'). 
.CATEGORY
Git Commands
#>
function gca { 
    [CmdletBinding()] 
    param([Parameter(ValueFromRemainingArguments=$true)][string[]]$args) 
    Write-Host "✍️ Amending previous commit..." -ForegroundColor Cyan; git commit --amend @args 
}
<# 
.SYNOPSIS 
Checks out a branch ('git checkout'). 
.CATEGORY
Git Commands
#>
function co { 
    [CmdletBinding()] 
    param([string]$branchName) 
    Write-Host "Check out branch: $branchName" -ForegroundColor Green; git checkout $branchName 
}
<# 
.SYNOPSIS 
Creates and checks out a new branch ('git checkout -b'). 
.CATEGORY
Git Commands
#>
function cob { 
    [CmdletBinding()] 
    param([string]$branchName) 
    Write-Host "Check out and create a new branch: $branchName" -ForegroundColor Green; git checkout -b $branchName 
}
<# 
.SYNOPSIS 
Shows a compact, graphical log ('git log --graph...'). 
.CATEGORY
Git Commands
#>
function glg { 
    [CmdletBinding()] 
    param() 
    git log --graph --oneline --decorate --all 
}

function glo { 
    [CmdletBinding()] 
    param() 
    git log 
}
<# 
.SYNOPSIS 
Pulls from remote ('git pull'). 
.CATEGORY
Git Commands
#>
function gpu { 
    [CmdletBinding()] 
    param([Parameter(ValueFromRemainingArguments=$true)][string[]]$args) 
    Write-Host "⏬ Pulling changes from remote..." -ForegroundColor Blue; git pull @args 
}
<# 
.SYNOPSIS 
Pushes to remote ('git push'). 
.CATEGORY
Git Commands
#>
function gus { 
    [CmdletBinding()] 
    param([Parameter(ValueFromRemainingArguments=$true)][string[]]$args) 
    Write-Host "⏫ Pushing changes to remote..." -ForegroundColor Blue; git push @args 
}
<# 
.SYNOPSIS 
Force pushes to remote ('git push --force'). 
.CATEGORY
Git Commands
#>
function guf { 
    [CmdletBinding()] 
    param([Parameter(ValueFromRemainingArguments=$true)][string[]]$args) 
    Write-Host "⏫ Force pushing changes..." -ForegroundColor Red; git push --force @args 
}
<# 
.SYNOPSIS 
Fetches from all remotes ('git fetch --all --prune'). 
.CATEGORY
Git Commands
#>
function gf { 
    [CmdletBinding()] 
    param() 
    Write-Host "🔎 Fetching from all remotes..." -ForegroundColor Blue; git fetch --all --prune 
}
<# 
.SYNOPSIS 
Lists branches ('git branch'). 
.CATEGORY
Git Commands
#>
function gb { 
    [CmdletBinding()] 
    param([Parameter(ValueFromRemainingArguments=$true)][string[]]$args) 
    Write-Host "🌿 Branches:" -ForegroundColor Green; git branch @args 
}
<# 
.SYNOPSIS 
Deletes a local branch ('git branch -d'). 
.CATEGORY
Git Commands
#>
function gbd { 
    [CmdletBinding()] 
    param([string]$branchName) 
    git branch -d $branchName 
}
<# 
.SYNOPSIS 
Squash merges a branch ('git merge --squash'). 
.CATEGORY
Git Commands
#>
function gms { 
    [CmdletBinding()] 
    param([Parameter(Mandatory=$true)][string]$BranchName) 
    Write-Host "Squash merging branch: $BranchName" —ForegroundColor Yellow
    git merge --squash $BranchName
    Write-Host "Commit the squashed changes!" —ForegroundColor Cyan 
}
<# 
.SYNOPSIS 
Resets the last commit ('git reset HEAD~'). 
.CATEGORY
Git Commands
#>
function gr { 
    [CmdletBinding()] 
    param() 
    git reset HEAD~ 
}

function grh { 
    [CmdletBinding()] 
    param() 
    git reset --hard 
} 

#endregion