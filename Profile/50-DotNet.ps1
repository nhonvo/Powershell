#region .NET DEVELOPMENT COMMANDS
# ------------------------------------------------------------------------------
#  Shortcuts for dotnet CLI commands with Verb-Noun naming.
# ------------------------------------------------------------------------------

<# 
.SYNOPSIS 
Runs 'dotnet run'. 
.CATEGORY
.NET Development Commands
#>
function Invoke-DotNetRun { 
    [CmdletBinding()] 
    param([Parameter(ValueFromRemainingArguments=$true)][string[]]$args) 
    Write-Host "🚀 Running project..." -ForegroundColor Green; dotnet run @args 
}

<# 
.SYNOPSIS 
Recursively deletes bin and obj folders. 
.CATEGORY
.NET Development Commands
#>
function Remove-BinObj { 
    [CmdletBinding()] 
    param() 
    Write-Host "💥 Destroying bin/ and obj/ folders..." -ForegroundColor Red
    Get-ChildItem -Inc -Include bin,obj -Recurse | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
    Write-Host "✅ Clean complete." -ForegroundColor Green
}

<# 
.SYNOPSIS 
Creates a new solution file. 
.CATEGORY
.NET Development Commands
#>
function New-Solution { 
    [CmdletBinding()] 
    param([string]$Name)
    dotnet new sln -n $Name
}

<# 
.SYNOPSIS 
Adds all projects in subfolders to the solution. 
.CATEGORY
.NET Development Commands
#>
function Add-AllProjectsToSolution { 
    [CmdletBinding()] 
    param() 
    $projects = Get-ChildItem -Recurse -Filter "*.csproj"
    foreach ($p in $projects) {
        dotnet sln add $p.FullName
    }
}

<# 
.SYNOPSIS 
Runs 'dotnet watch test'. 
.CATEGORY
.NET Development Commands
#>
function Invoke-DotNetWatchTest { 
    [CmdletBinding()] 
    param([Parameter(ValueFromRemainingArguments=$true)][string[]]$args) 
    Write-Host "👀 Watching Tests..." -ForegroundColor Yellow; dotnet watch test @args 
}

<# 
.SYNOPSIS 
Runs 'dotnet watch'. 
.CATEGORY
.NET Development Commands
#>
function Invoke-DotNetWatch { 
    [CmdletBinding()] 
    param([Parameter(ValueFromRemainingArguments=$true)][string[]]$args) 
    Write-Host "👀 Watching for changes..." -ForegroundColor Cyan; dotnet watch @args 
}

<# 
.SYNOPSIS 
Runs 'dotnet build'. 
.CATEGORY
.NET Development Commands
#>
function Invoke-DotNetBuild { 
    [CmdletBinding()] 
    param([Parameter(ValueFromRemainingArguments=$true)][string[]]$args) 
    Write-Host "🔨 Building project..." -ForegroundColor Blue; dotnet build @args 
}

<# 
.SYNOPSIS 
Runs 'dotnet format'. 
.CATEGORY
.NET Development Commands
#>
function Invoke-DotNetFormat { 
    [CmdletBinding()] 
    param([Parameter(ValueFromRemainingArguments=$true)][string[]]$args) 
    Write-Host "💅 Formatting code..." -ForegroundColor Magenta; dotnet format @args 
}

<# 
.SYNOPSIS 
Runs 'dotnet test'. 
.CATEGORY
.NET Development Commands
#>
function Invoke-DotNetTest { 
    [CmdletBinding()] 
    param([Parameter(ValueFromRemainingArguments=$true)][string[]]$args) 
    Write-Host "🧪 Running tests..." -ForegroundColor Yellow; dotnet test @args 
}

<# 
.SYNOPSIS 
Runs 'dotnet clean'. 
.CATEGORY
.NET Development Commands
#>
function Invoke-DotNetClean { 
    [CmdletBinding()] 
    param([Parameter(ValueFromRemainingArguments=$true)][string[]]$args) 
    Write-Host "🧹 Cleaning project..." -ForegroundColor Yellow; dotnet clean @args 
}

<# 
.SYNOPSIS 
Runs 'dotnet restore'. 
.CATEGORY
.NET Development Commands
#>
function Invoke-DotNetRestore { 
    [CmdletBinding()] 
    param([Parameter(ValueFromRemainingArguments=$true)][string[]]$args) 
    Write-Host "📦 Restoring packages..." -ForegroundColor Magenta; dotnet restore @args 
}


# --- Entity Framework ---
<# 
.SYNOPSIS 
Runs 'dotnet ef database update'. 
.CATEGORY
.NET Development Commands
#>
function Update-Database { 
    [CmdletBinding()] 
    param([string]$Context) 
    Write-Host "📈 Updating database..." -ForegroundColor Green
    $params = "ef", "database", "update"
    if ($Context) { $params += "--context", "$Context" }
    dotnet @params 
}

<# 
.SYNOPSIS 
Runs 'dotnet ef migrations add'. 
.CATEGORY
.NET Development Commands
#>
function Add-Migration { 
    [CmdletBinding()] 
    param([Parameter(Mandatory=$true)][string]$MigrationName, [string]$Context) 
    Write-Host "➕ Adding migration: $MigrationName" -ForegroundColor Cyan
    $params = "ef", "migrations", "add", "$MigrationName"
    if ($Context) { $params += "--context", "$Context" }
    dotnet @params
}

<# 
.SYNOPSIS 
Runs 'dotnet ef database drop'. 
.CATEGORY
.NET Development Commands
#>
function Remove-Database { 
    [CmdletBinding()] 
    param([string]$Context) 
    Write-Host "🔥 Dropping database..." -ForegroundColor Red
    if ((Read-Host "Are you sure? (y/N)") -eq 'y') { 
        $params = "ef", "database", "drop", "--force"
        if ($Context) { $params += "--context", "$Context" }
        dotnet @params
        Write-Host "Database dropped." 
    } else { 
        Write-Host "Cancelled." 
    } 
}

<# 
.SYNOPSIS 
Runs 'dotnet ef migrations remove'. 
.CATEGORY
.NET Development Commands
#>
function Remove-Migration { 
    [CmdletBinding()] 
    param([string]$Context) 
    Write-Host "⏪ Removing last migration..." -ForegroundColor Yellow
    $params = "ef", "migrations", "remove"
    if ($Context) { $params += "--context", "$Context" }
    dotnet @params
}

# --- Project Scaffolding ---
function New-ConsoleProject { 
    [CmdletBinding()] 
    param([Parameter(Mandatory=$true)][string]$ProjectName, [switch]$SkipGit) 
    Write-Host "🚀 Creating new Console project: $ProjectName" -ForegroundColor Green
    
    dotnet new console -n $ProjectName
    Set-Location $ProjectName
    
    dotnet new gitignore
    if (-not $SkipGit) { 
        git init
        git add .
        git commit -m "Initial commit" 
    }
    
    if (Get-Command code -ErrorAction SilentlyContinue) { code . }
    dotnet run 
}

<# 
.SYNOPSIS 
Creates a new .NET Web API project with Git initialized. 
.CATEGORY
.NET Development Commands
#>
function New-WebApiProject { 
    [CmdletBinding()] 
    param([Parameter(Mandatory=$true)][string]$ProjectName, [switch]$SkipGit) 
    Write-Host "🚀 Creating new Web API project: $ProjectName" -ForegroundColor Green
    
    dotnet new webapi -n $ProjectName
    Set-Location $ProjectName
    
    dotnet new gitignore
    if (-not $SkipGit) { 
        git init
        git add .
        git commit -m "Initial commit" 
    }
    
    if (Get-Command code -ErrorAction SilentlyContinue) { code . }
}

#endregion