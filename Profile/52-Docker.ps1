#region DOCKER COMMANDS
# ------------------------------------------------------------------------------
#  Shortcuts for Docker and Docker Compose.
# ------------------------------------------------------------------------------

<# 
.SYNOPSIS 
Lists Docker containers ('docker container ls'). Use -All to see stopped containers. 
.CATEGORY
Docker Commands
#>
function dkcl { 
    [CmdletBinding()] 
    param([switch]$All) 
    Write-Host "🐳 Docker Containers:" -ForegroundColor Blue
    if ($All) { docker container ls -a } else { docker container ls } 
}
<# 
.SYNOPSIS 
Removes all Docker containers with confirmation. 
.CATEGORY
Docker Commands
#>
function dkrmac { 
    [CmdletBinding()] 
    param() 
    Write-Host "🗑️ Removing ALL containers..." -ForegroundColor Red
    if ((Read-Host "This will remove ALL containers. Are you sure? (y/N)") -eq 'y') { 
        $c = docker ps -aq
        if ($c) { docker rm $c }
        Write-Host "All containers removed." 
    } else { 
        Write-Host "Cancelled." 
    } 
}
<# 
.SYNOPSIS 
Stops all running Docker containers. 
.CATEGORY
Docker Commands
#>
function dkstac { 
    [CmdletBinding()] 
    param() 
    Write-Host "⏹️ Stopping ALL running containers..." -ForegroundColor Yellow
    $c = docker ps -q
    if ($c) { docker stop $c }
    Write-Host "All containers stopped." 
}
<# 
.SYNOPSIS 
Runs 'docker-compose up'. 
.CATEGORY
Docker Commands
#>
function dkcpu { 
    [CmdletBinding()] 
    param([Parameter(ValueFromRemainingArguments=$true)][string[]]$args) 
    Write-Host "🚀 Starting Docker Compose..." -ForegroundColor Green; docker-compose up @args 
}
<# 
.SYNOPSIS 
Runs 'docker-compose up --build'. 
.CATEGORY
Docker Commands
#>
function dkcpub { 
    [CmdletBinding()] 
    param([Parameter(ValueFromRemainingArguments=$true)][string[]]$args) 
    Write-Host "🔨 Building and starting Docker Compose... " -ForegroundColor Blue; docker-compose up --build @args 
}
<# 
.SYNOPSIS 
Runs 'docker-compose down'. 
.CATEGORY
Docker Commands
#>
function dkcpd { 
    [CmdletBinding()] 
    param([Parameter(ValueFromRemainingArguments=$true)][string[]]$args) 
    Write-Host "🛑 Stopping Docker Compose..." -ForegroundColor Yellow; docker-compose down @args 
}
<# 
.SYNOPSIS 
Prunes unused Docker volumes ('docker volume prune'). 
.CATEGORY
Docker Commands
#>
function fix-volume { 
    [CmdletBinding()] 
    param() 
    Write-Host "🧹 Pruning Docker volumes..." -ForegroundColor Magenta; docker volume prune 
}
<# 
.SYNOPSIS 
Prunes unused Docker images ('docker image prune'). 
.CATEGORY
Docker Commands
#>
function fix-image { 
    [CmdletBinding()] 
    param() 
    Write-Host "🧹 Pruning Docker images..." -ForegroundColor Magenta; docker image prune 
}

#endregion