#region ALIASES
# ------------------------------------------------------------------------------
#  Shortcuts for frequently used commands.
#  All aliases for the profile are centralized here.
# ------------------------------------------------------------------------------

# --- Core Aliases ---
Set-Alias -Name ip -Value Get-NetIPConfiguration -Force
Set-Alias -Name cls -Value Clear-Host -Force
Set-Alias -Name which -Value Get-Command -Force
Set-Alias -Name v -Value nvim -Force

# --- System & Navigation (20-Navigation, 30-System) ---
Set-Alias -Name .. -Value Set-LocationParent -Force
Set-Alias -Name ... -Value Set-LocationGrandParent -Force
Set-Alias -Name prj -Value Enter-Project -Force
Set-Alias -Name go -Value Reload-Profile -Force
Set-Alias -Name folder -Value Invoke-OpenExplorer -Force
Set-Alias -Name edit-profile -Value Edit-Profile -Force
Set-Alias -Name mkcd -Value New-DirAndEnter -Force
Set-Alias -Name code -Value Invoke-VSCode -Force
Set-Alias -Name commands -Value Get-CustomCommands -Force

# --- Winget Shortcuts ---
function Find-WinGetPackage { winget search $args }
Set-Alias -Name wsearch -Value Find-WinGetPackage -Force

function Install-WinGetPackage { winget install $args }
Set-Alias -Name winstall -Value Install-WinGetPackage -Force

function Get-WinGetList { winget list $args }
Set-Alias -Name wlist -Value Get-WinGetList -Force

function Update-WinGetPackage { winget upgrade $args }
Set-Alias -Name wupdate -Value Update-WinGetPackage -Force

# --- .NET Development (50-DotNet) ---
Set-Alias -Name dr -Value Invoke-DotNetRun -Force
Set-Alias -Name dw -Value Invoke-DotNetWatch -Force
Set-Alias -Name db -Value Invoke-DotNetBuild -Force
Set-Alias -Name df -Value Invoke-DotNetFormat -Force
Set-Alias -Name dt -Value Invoke-DotNetTest -Force
Set-Alias -Name dcl -Value Invoke-DotNetClean -Force
Set-Alias -Name dres -Value Invoke-DotNetRestore -Force

# Entity Framework
Set-Alias -Name du -Value Update-Database -Force
Set-Alias -Name da -Value Add-Migration -Force
Set-Alias -Name dd -Value Remove-Database -Force
Set-Alias -Name dremove -Value Remove-Migration -Force

# Project Scaffolding
Set-Alias -Name console -Value New-ConsoleProject -Force
Set-Alias -Name webapi -Value New-WebApiProject -Force

# --- Git (51-Git) ---
Set-Alias -Name gs -Value Get-GitStatus -Force
Set-Alias -Name ga -Value Invoke-GitAddAll -Force
Set-Alias -Name gcmt -Value Invoke-GitCommit -Force
Set-Alias -Name gca -Value Invoke-GitAmend -Force
Set-Alias -Name co -Value Invoke-GitCheckout -Force
Set-Alias -Name cob -Value New-GitBranch -Force
Set-Alias -Name glg -Value Get-GitLogGraph -Force
Set-Alias -Name glog -Value Get-GitLogPretty -Force
Set-Alias -Name glo -Value Get-GitLog -Force
Set-Alias -Name gpu -Value Invoke-GitPull -Force
Set-Alias -Name gus -Value Invoke-GitPush -Force
Set-Alias -Name guf -Value Invoke-GitPushForce -Force
Set-Alias -Name gf -Value Invoke-GitFetch -Force
Set-Alias -Name gb -Value Get-GitBranches -Force
Set-Alias -Name gbd -Value Remove-GitBranch -Force
Set-Alias -Name gms -Value Invoke-GitMergeSquash -Force
Set-Alias -Name gr -Value Invoke-GitResetSoft -Force
Set-Alias -Name grh -Value Invoke-GitResetHard -Force

# --- Docker (52-Docker) ---
Set-Alias -Name dkcl -Value Get-DockerContainers -Force
Set-Alias -Name dkrmac -Value Remove-AllDockerContainers -Force
Set-Alias -Name dkstac -Value Stop-AllDockerContainers -Force
Set-Alias -Name dkcpu -Value Invoke-ComposeUp -Force
Set-Alias -Name dkcpub -Value Invoke-ComposeUpBuild -Force
Set-Alias -Name dkcpd -Value Invoke-ComposeDown -Force
Set-Alias -Name fix-volume -Value Remove-UnusedDockerVolumes -Force
Set-Alias -Name fix-image -Value Remove-UnusedDockerImages -Force

# --- AWS LocalStack (53-AWS) ---
Set-Alias -Name lsq -Value Get-LocalSQSQueues -Force
Set-Alias -Name csq -Value New-LocalSQSQueue -Force
Set-Alias -Name clsq -Value Clear-LocalSQSQueue -Force
Set-Alias -Name ssm -Value Send-LocalSQSMessage -Force
Set-Alias -Name rsm -Value Get-LocalSQSMessage -Force
Set-Alias -Name gsqa -Value Get-LocalSQSAttributes -Force

# --- AI Tools (60-AI) ---
Set-Alias -Name ox -Value Get-OllamaModels -Force
Set-Alias -Name '??' -Value Invoke-CopilotSuggest -Force
Set-Alias -Name 'what?' -Value Invoke-CopilotExplain -Force

# --- New System Aliases ---
Set-Alias -Name refresh-env -Value Update-EnvironmentVariables -Force
Set-Alias -Name usage -Value Get-DiskSpace -Force
Set-Alias -Name myip -Value Get-PublicIP -Force
Set-Alias -Name tree -Value Get-FileTree -Force
Set-Alias -Name kill -Value Stop-ProcessFriendly -Force

# --- New .NET Aliases ---
Set-Alias -Name dclean -Value Remove-BinObj -Force
Set-Alias -Name sln -Value New-Solution -Force
Set-Alias -Name sln-add -Value Add-AllProjectsToSolution -Force
Set-Alias -Name wt -Value Invoke-DotNetWatchTest -Force

# --- New Git Aliases ---
Set-Alias -Name gundo -Value Invoke-GitUndo -Force
Set-Alias -Name gunstage -Value Invoke-GitUnstage -Force
Set-Alias -Name gsnap -Value Invoke-GitStashSnapshot -Force
Set-Alias -Name gd -Value Show-GitDiff -Force

# --- New AWS Aliases ---
Set-Alias -Name s3ls -Value Get-S3Buckets -Force
Set-Alias -Name s3mb -Value New-S3Bucket -Force
Set-Alias -Name lls -Value Get-LambdaFunctions -Force

# --- New AI Aliases ---
Set-Alias -Name venv -Value New-PythonVenv -Force
Set-Alias -Name activate -Value Invoke-VenvActivate -Force
Set-Alias -Name chat -Value Invoke-GeminiChat -Force
Set-Alias -Name prompt-save -Value Save-Prompt -Force
Set-Alias -Name prompt-load -Value Get-Prompt -Force

# --- HELPER SHORTCUTS ---
# Fast access to help menu
function c    { Get-CustomCommands }
function cg   { Get-CustomCommands "Git" }
function cnet { Get-CustomCommands "Net" }
function csys { Get-CustomCommands "Sys" }
function cdk  { Get-CustomCommands "Docker" }
function cai  { Get-CustomCommands "AI" }
function caws { Get-CustomCommands "AWS" }

# Conditional Aliases
if (Test-Path "C:\Users\TruongNhon\.local\bin\claude.exe") {
    Set-Alias -Name claude -Value "C:\Users\TruongNhon\.local\bin\claude.exe" -Force
}

#endregion