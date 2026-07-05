#region DOCKER HELPER
# ==============================================================================
#  Shortcuts and prune utility wrappers for Docker and Docker Compose.
# ==============================================================================

class DockerHelper {
    static [void] GetContainers([bool]$All) {
        Write-Host "🐳 Docker Containers:" -ForegroundColor Blue
        if ($All) { docker container ls -a | Out-Default } else { docker container ls | Out-Default }
    }

    static [void] RemoveAllContainers() {
        Write-Host "🗑️ Removing ALL containers..." -ForegroundColor Red
        if ((Read-Host "This will remove ALL containers. Are you sure? (y/N)") -eq 'y') {
            $c = docker ps -aq
            if ($c) { docker rm $c | Out-Default }
            Write-Host "All containers removed."
        } else {
            Write-Host "Cancelled."
        }
    }

    static [void] StopAllContainers() {
        Write-Host "⏹️ Stopping ALL running containers..." -ForegroundColor Yellow
        $c = docker ps -q
        if ($c) { docker stop $c | Out-Default }
        Write-Host "All containers stopped."
    }

    static [void] ComposeUp([string[]]$PassThruArgs) {
        Write-Host "🚀 Starting Docker Compose..." -ForegroundColor Green
        if ($PassThruArgs) { docker-compose up $PassThruArgs | Out-Default } else { docker-compose up | Out-Default }
    }

    static [void] ComposeUpBuild([string[]]$PassThruArgs) {
        Write-Host "🔨 Building and starting Docker Compose... " -ForegroundColor Blue
        if ($PassThruArgs) { docker-compose up --build $PassThruArgs | Out-Default } else { docker-compose up --build | Out-Default }
    }

    static [void] ComposeDown([string[]]$PassThruArgs) {
        Write-Host "🛑 Stopping Docker Compose..." -ForegroundColor Yellow
        if ($PassThruArgs) { docker-compose down $PassThruArgs | Out-Default } else { docker-compose down | Out-Default }
    }

    static [void] RemoveUnusedVolumes() {
        Write-Host "🧹 Pruning Docker volumes..." -ForegroundColor Magenta
        docker volume prune | Out-Default
    }

    static [void] RemoveUnusedImages() {
        Write-Host "🧹 Pruning Docker images..." -ForegroundColor Magenta
        docker image prune | Out-Default
    }
}
#endregion
