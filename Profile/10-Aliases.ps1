#region ALIASES
# ------------------------------------------------------------------------------
#  Shortcuts for frequently used commands.
#  All aliases for the profile are centralized here.
# ------------------------------------------------------------------------------

# --- Core ---
Set-Alias -Name ip  -Value Get-NetIPConfiguration -Force
Set-Item -Path Alias:\cls -Value Clear-Host -Force -Option AllScope

# --- System & Navigation (20-Navigation, 30-System) ---
Set-Alias -Name ..          -Value Set-LocationParent          -Force
Set-Alias -Name ...         -Value Set-LocationGrandParent     -Force
Set-Alias -Name f           -Value Invoke-OpenExplorer         -Force
Set-Alias -Name prj         -Value Enter-Project               -Force
Set-Alias -Name proj        -Value Enter-Project               -Force
Set-Alias -Name go          -Value Reload-Profile              -Force
Set-Alias -Name mkcd        -Value New-DirAndEnter             -Force
Set-Alias -Name edit-profile -Value Edit-Profile              -Force
Set-Alias -Name refresh-env -Value Update-EnvironmentVariables -Force
Set-Alias -Name usage       -Value Get-DiskSpace               -Force
Set-Alias -Name myip        -Value Get-PublicIP                -Force
Set-Alias -Name tree        -Value Get-FileTree                -Force
Set-Item -Path Alias:\kill -Value Stop-ProcessFriendly -Force -Option AllScope
Set-Alias -Name commands    -Value Get-CustomCommands          -Force

# --- Git (51-Git) ---
# Inspect
Set-Alias -Name gs   -Value Get-GitStatus    -Force   # status
Set-Alias -Name gd   -Value Show-GitDiff     -Force   # diff
Set-Alias -Name glo  -Value Get-GitLog       -Force   # log
Set-Alias -Name glg  -Value Get-GitLogGraph  -Force   # log --graph
Set-Alias -Name glog -Value Get-GitLogPretty -Force   # log --pretty

# Branch
Set-Alias -Name gb   -Value Get-GitBranches    -Force   # list branches
Set-Alias -Name co   -Value Invoke-GitCheckout -Force   # checkout
Set-Alias -Name cob  -Value New-GitBranch      -Force   # checkout -b
Set-Alias -Name gbd  -Value Remove-GitBranch   -Force   # branch -d

# Stage & Commit
Set-Alias -Name ga       -Value Invoke-GitAddAll       -Force   # add .
Set-Alias -Name gunstage -Value Invoke-GitUnstage      -Force   # restore --staged
Set-Alias -Name gcmt     -Value Invoke-GitCommit       -Force   # commit -m
Set-Alias -Name gca      -Value Invoke-GitAmend        -Force   # commit --amend
Set-Alias -Name gundo    -Value Invoke-GitUndo         -Force   # reset --soft HEAD~1
Set-Alias -Name gr       -Value Invoke-GitResetSoft    -Force   # reset HEAD~
Set-Alias -Name grh      -Value Invoke-GitResetHard    -Force   # reset --hard

# Sync
Set-Alias -Name gf  -Value Invoke-GitFetch     -Force   # fetch --all
Set-Alias -Name gpu -Value Invoke-GitPull      -Force   # pull
Set-Alias -Name gus -Value Invoke-GitPush      -Force   # push
Set-Alias -Name guf -Value Invoke-GitPushForce -Force   # push --force

# Merge & Stash
Set-Alias -Name gms   -Value Invoke-GitMergeSquash   -Force   # merge --squash
Set-Alias -Name gsnap -Value Invoke-GitStashSnapshot -Force   # stash push + apply

# --- .NET Development (50-DotNet) ---
Set-Alias -Name dr     -Value Invoke-DotNetRun       -Force
Set-Alias -Name dw     -Value Invoke-DotNetWatch     -Force
Set-Alias -Name db     -Value Invoke-DotNetBuild     -Force
Set-Alias -Name df     -Value Invoke-DotNetFormat    -Force
Set-Alias -Name dt     -Value Invoke-DotNetTest      -Force
Set-Alias -Name wt     -Value Invoke-DotNetWatchTest -Force
Set-Alias -Name dcl    -Value Invoke-DotNetClean     -Force
Set-Alias -Name dres   -Value Invoke-DotNetRestore   -Force
Set-Alias -Name dclean -Value Remove-BinObj          -Force

# Entity Framework
Set-Alias -Name du      -Value Update-Database   -Force
Set-Alias -Name da      -Value Add-Migration     -Force
Set-Alias -Name dd      -Value Remove-Database   -Force
Set-Alias -Name dremove -Value Remove-Migration  -Force

# Project Scaffolding
Set-Alias -Name sln     -Value New-Solution              -Force
Set-Alias -Name sln-add -Value Add-AllProjectsToSolution -Force
Set-Alias -Name console -Value New-ConsoleProject        -Force
Set-Alias -Name webapi  -Value New-WebApiProject         -Force

# --- Docker (52-Docker) ---
Set-Alias -Name dkcl       -Value Get-DockerContainers       -Force
Set-Alias -Name dkrmac     -Value Remove-AllDockerContainers -Force
Set-Alias -Name dkstac     -Value Stop-AllDockerContainers   -Force
Set-Alias -Name dkcpu      -Value Invoke-ComposeUp           -Force
Set-Alias -Name dkcpub     -Value Invoke-ComposeUpBuild      -Force
Set-Alias -Name dkcpd      -Value Invoke-ComposeDown         -Force
Set-Alias -Name fix-volume -Value Remove-UnusedDockerVolumes -Force
Set-Alias -Name fix-image  -Value Remove-UnusedDockerImages  -Force

# --- AI Tools (60-AI, 61-Antigravity) ---
Set-Alias -Name ai          -Value Invoke-MultiAgent        -Force
Set-Alias -Name codex       -Value Invoke-Codex-By-Ollama   -Force
Set-Alias -Name claude      -Value Invoke-Claude-By-Ollama  -Force
Set-Alias -Name agm         -Value Start-AntigravityManager -Force
Set-Alias -Name openclaw    -Value Invoke-OpenClaw-By-Ollama -Force
Set-Alias -Name clawdbot    -Value Invoke-Clawdbot-By-Ollama -Force
Set-Alias -Name hermes      -Value Invoke-Hermes-By-Ollama   -Force
Set-Alias -Name hermesd     -Value Invoke-HermesDesktop-By-Ollama -Force
Set-Alias -Name model       -Value Set-OllamaModel          -Force

# --- Help shortcuts ---
Set-Alias -Name cc -Value Get-CustomCommands -Force
function cg   { Get-CustomCommands "Git" }
function cnet { Get-CustomCommands "Net" }
function csys { Get-CustomCommands "Sys" }
function cdk  { Get-CustomCommands "Docker" }
function cai  { Get-CustomCommands "AI" }
function caws { Get-CustomCommands "AWS" }

# --- Conditional ---
$claudeExe = "$env:USERPROFILE\.local\bin\claude.exe"
if (Test-Path $claudeExe) {
    Set-Alias -Name claude-proxy -Value $claudeExe -Force
}

#endregion
