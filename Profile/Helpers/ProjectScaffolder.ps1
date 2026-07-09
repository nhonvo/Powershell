#region PROJECT SCAFFOLDER
# ==============================================================================
#  Template-driven project scaffolder and registration wizard.
# ==============================================================================

class ProjectScaffolder {
    static [void] ScaffoldProject([string]$Template, [string]$Name, [int]$Port) {
        $validTemplates = @("dotnet-webapi", "dotnet-console", "react-vite", "node-console")
        
        # AI Mode check
        if ($Global:AiMode -and (-not $Template -or -not $Name)) {
            Write-Host "Usage: new-project -Template <TemplateName> -Name <ProjectName> [-Port <PortNumber>]"
            Write-Host "Templates: dotnet-webapi, dotnet-console, react-vite, node-console"
            return
        }

        # Interactive Mode
        if (-not $Template) {
            $options = @(
                "[1] .NET 8.0 Web API (C#)",
                "[2] .NET 8.0 Console (C#)",
                "[3] React + Vite (Typescript)",
                "[4] Node.js Console app (Javascript)"
            )
            $sel = ([type]"TerminalMenu")::Show("Select Project Template", $options, 0)
            if ($sel -lt 0) { return }
            
            $Template = switch ($sel) {
                0 { "dotnet-webapi" }
                1 { "dotnet-console" }
                2 { "react-vite" }
                3 { "node-console" }
            }
        }

        if (-not $Name) {
            $Name = Read-Host "Enter project name"
            if ([string]::IsNullOrWhiteSpace($Name)) {
                Write-Error "Project name cannot be empty."
                return
            }
        }

        if (-not $Port) {
            $defaultPort = if ($Template -eq "react-vite") { 3000 } else { 5000 }
            $portInput = Read-Host "Enter development port [default $defaultPort]"
            $Port = if ($portInput) { [int]$portInput } else { $defaultPort }
        }

        $baseDir = if ($null -ne $Global:ProfileWorkspaces -and $Global:ProfileWorkspaces.Count -gt 0) {
            Split-Path $Global:ProfileWorkspaces[0].Path -Parent
        } else {
            "$env:USERPROFILE\Documents"
        }
        # Fallback if path doesn't exist
        if (-not (Test-Path $baseDir)) {
            $baseDir = "$env:USERPROFILE\Documents"
        }
        $targetPath = Join-Path $baseDir $Name

        if (Test-Path $targetPath) {
            Write-Error "Directory already exists: $targetPath"
            return
        }

        Write-Host "Creating project '$Name' in $targetPath using template '$Template'..." -ForegroundColor Cyan
        $null = New-Item -ItemType Directory -Path $targetPath -Force

        # Run scaffold commands
        switch ($Template) {
            "dotnet-webapi" {
                Start-Process dotnet -ArgumentList "new webapi -n $Name -o `"$targetPath`"" -NoNewWindow -Wait
            }
            "dotnet-console" {
                Start-Process dotnet -ArgumentList "new console -n $Name -o `"$targetPath`"" -NoNewWindow -Wait
            }
            "react-vite" {
                Start-Process npm -ArgumentList "create vite@latest `"$targetPath`" -- --template react-ts" -NoNewWindow -Wait
            }
            "node-console" {
                $packageJson = @"
{
  "name": "$Name",
  "version": "1.0.0",
  "main": "index.js",
  "scripts": {
    "start": "node index.js"
  }
}
"@
                $packageJson | Out-File -FilePath (Join-Path $targetPath "package.json") -Force -Encoding utf8
                "console.log('Hello from $Name!');" | Out-File -FilePath (Join-Path $targetPath "index.js") -Force -Encoding utf8
            }
        }

        Write-Host "Project scaffolded successfully." -ForegroundColor Green

        # Write registration to Projects.ps1
        $projectsFile = "c:\Users\TruongNhon\Documents\Powershell\Profile\Core\Projects.ps1"
        if (Test-Path $projectsFile) {
            try {
                $raw = Get-Content -Raw -Path $projectsFile
                $pattern = '(?s)\$Global:ProfileWorkspaces = @\((.*?)\)'
                if ($raw -match $pattern) {
                    $inside = $Matches[1]
                    $shortAlias = $Name.Substring(0, [Math]::Min(4, $Name.Length)).ToLower()
                    $newEntry = "    @{ Name = `"$Name`";                  Short = `"$shortAlias`";   AssociatedAccount = `"default`" }"
                    $updatedInside = $inside.TrimEnd() + "`r`n" + $newEntry + "`r`n"
                    $updatedRaw = $raw -replace $pattern, "`$Global:ProfileWorkspaces = @($updatedInside)"
                    $updatedRaw | Out-File -FilePath $projectsFile -Force -Encoding utf8
                    
                    # Update active global list dynamically
                    $newWorkspace = @{ Name = $Name; Short = $shortAlias; AssociatedAccount = "default"; Path = $targetPath }
                    $Global:ProfileWorkspaces += $newWorkspace
                    Write-Host "Auto-registered workspace '$Name' persistently." -ForegroundColor Green
                }
            } catch {
                Write-Warning "Failed to auto-register project persistently: $_"
            }
        }
    }
}
#endregion
