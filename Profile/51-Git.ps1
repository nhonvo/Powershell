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
function Get-GitStatus { 
    [CmdletBinding()] 
    param([Parameter(ValueFromRemainingArguments=$true)][string[]]$args) 
    Write-Host "🔍 Git Status" -ForegroundColor Yellow; git status @args 
}

<# 
.SYNOPSIS 
Soft resets the last commit (undo commit, keep changes). 
.CATEGORY
Git Commands
#>
function Invoke-GitUndo { 
    [CmdletBinding()] 
    param() 
    Write-Host "⏪ Undoing last commit (keeping changes)..." -ForegroundColor Yellow
    git reset --soft HEAD~1
}

<# 
.SYNOPSIS 
Unstages all files (git restore --staged .). 
.CATEGORY
Git Commands
#>
function Invoke-GitUnstage { 
    [CmdletBinding()] 
    param() 
    Write-Host "🔙 Unstaging changes..." -ForegroundColor Yellow
    git restore --staged .
}

<# 
.SYNOPSIS 
Creates a quickstash snapshot without clearing workspace. 
.CATEGORY
Git Commands
#>
function Invoke-GitStashSnapshot { 
    [CmdletBinding()] 
    param([string]$Message = "snapshot") 
    git stash push -m "$Message"
    git stash apply 0
    Write-Host "📸 Snapshot stashed: $Message" -ForegroundColor Green
}

<# 
.SYNOPSIS 
Shows git diff. 
.CATEGORY
Git Commands
#>
function Show-GitDiff { 
    [CmdletBinding()] 
    param() 
    git diff
}

<# 
.SYNOPSIS 
Stages all changes ('git add .'). 
.CATEGORY
Git Commands
#>
function Invoke-GitAddAll { 
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
function Invoke-GitCommit { 
    [CmdletBinding()] 
    param([Parameter(Mandatory=$true, ValueFromRemainingArguments=$true)][string[]]$Message) 
    $commitMessage = $Message -join ' '
    Write-Host "📝 Committing with message: `"$commitMessage`"" -ForegroundColor Cyan; git commit -m "$commitMessage" 
}

<# 
.SYNOPSIS 
Amends the previous commit ('git commit --amend'). 
.CATEGORY
Git Commands
#>
function Invoke-GitAmend { 
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
function Invoke-GitCheckout { 
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
function New-GitBranch { 
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
function Get-GitLogGraph { 
    [CmdletBinding()] 
    param() 
    git log --graph --oneline --decorate --all 
}

<# 
.SYNOPSIS 
Shows a one-line formatted git log. 
.CATEGORY
Git Commands
#>
function Get-GitLogPretty { 
    [CmdletBinding()] 
    param() 
    git log --pretty=format:"%C(yellow)%h%Creset -%C(red)%d%Creset %s %C(green)(%cr) %C(bold blue)<%an>%Creset" --abbrev-commit
}

function Get-GitLog { 
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
function Invoke-GitPull { 
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
function Invoke-GitPush { 
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
function Invoke-GitPushForce { 
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
function Invoke-GitFetch { 
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
function Get-GitBranches { 
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
function Remove-GitBranch { 
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
function Invoke-GitMergeSquash { 
    [CmdletBinding()] 
    param([Parameter(Mandatory=$true)][string]$BranchName) 
    Write-Host "Squash merging branch: $BranchName" -ForegroundColor Yellow
    git merge --squash $BranchName
    Write-Host "Commit the squashed changes!" -ForegroundColor Cyan 
}

<# 
.SYNOPSIS 
Resets the last commit ('git reset HEAD~'). 
.CATEGORY
Git Commands
#>
function Invoke-GitResetSoft { 
    [CmdletBinding()] 
    param() 
    git reset HEAD~ 
}

function Invoke-GitResetHard { 
    [CmdletBinding()] 
    param() 
    git reset --hard 
}

#endregion
