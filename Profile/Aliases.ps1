#region ALIASES
# ------------------------------------------------------------------------------
#  Shortcuts for frequently used commands.
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
# Set-Alias -Name code -Value Open-Code -Force

# --- Quick Navigation ---
Set-Alias -Name .. -Value Set-LocationParent -Force
Set-Alias -Name ... -Value Set-LocationGrandParent -Force

# --- Project Scaffolding ---
Set-Alias -Name console -Value New-ConsoleProject
Set-Alias -Name webapi -Value New-WebApiProject

#endregion