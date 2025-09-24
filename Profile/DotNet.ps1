#region .NET DEVELOPMENT COMMANDS
# ------------------------------------------------------------------------------
#  Shortcuts for dotnet CLI commands.
# ------------------------------------------------------------------------------

<# 
.SYNOPSIS 
Runs 'dotnet run'. 
.CATEGORY
.NET Development Commands
#>
function dr { 
    [CmdletBinding()] 
    param([Parameter(ValueFromRemainingArguments=$true)][string[]]$args) 
    Write-Host "🚀 Running project..." -ForegroundColor Green; dotnet run @args 
}
<# 
.SYNOPSIS 
Runs 'dotnet watch'. 
.CATEGORY
.NET Development Commands
#>
function dw { 
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
function db { 
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
function df { 
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
function dt { 
    [CmdletBinding()] 
    param([Parameter(ValueFromRemainingArguments=$true)][string[]]$args) 
    Write-Host "🧪 Running tests..." -ForegroundColor Yellow; dotnet test @args 
}

# --- Entity Framework ---
<# 
.SYNOPSIS 
Runs 'dotnet ef database update'. 
.CATEGORY
.NET Development Commands
#>
function du { 
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
function da { 
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
function dd { 
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
function dremove { 
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
    Write-Host "Creating new Console project: $ProjectName" -ForegroundColor Green
    mkdir $ProjectName; cd $ProjectName
    dotnet new console -n $ProjectName
    dotnet new gitignore
    if (-not $SkipGit) { 
        git init
        git add .
        git commit -m "Initial commit" 
    }
    Open-Code .
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
    Write-Host "Creating new Web API project: $ProjectName" -ForegroundColor Green
    mkdir $ProjectName; cd $ProjectName
    dotnet new webapi -n $ProjectName
    dotnet new gitignore
    if (-not $SkipGit) { 
        git init
        git add .
        git commit -m "Initial commit" 
    }
    Open-Code .
    dotnet run 
}

#endregion