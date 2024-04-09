# Powershell config

## Setup powershell environment variables

Set the POSH_THEMES_PATH environment variable to the chosen directory. You can do this using the following command:

```powershell
$env:POSH_THEMES_PATH = "$env:USERPROFILE\PowerShell\Themes"
Verify that the POSH_THEMES_PATH environment variable has been set correctly by running:
```

```powershell
echo $env:POSH_THEMES_PATH
```
