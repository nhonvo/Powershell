#region DOCKER HELPER
# ==============================================================================
#  Shortcuts and prune utility wrappers for Docker and Docker Compose.
# ==============================================================================

class DockerHelper {
    static [void] GetContainers([bool]$All) {
        Write-Host "🐳 Docker Containers:" -ForegroundColor Blue
        if ($All) { docker container ls -a } else { docker container ls }
    }

    static [void] RemoveAllContainers() {
        Write-Host "🗑️ Removing ALL containers..." -ForegroundColor Red
        if ((Read-Host "This will remove ALL containers. Are you sure? (y/N)") -eq 'y') {
            $c = docker ps -aq
            if ($c) { docker rm $c }
            Write-Host "All containers removed."
        } else {
            Write-Host "Cancelled."
        }
    }

    static [void] StopAllContainers() {
        Write-Host "⏹️ Stopping ALL running containers..." -ForegroundColor Yellow
        $c = docker ps -q
        if ($c) { docker stop $c }
        Write-Host "All containers stopped."
    }

    static [void] ComposeUp([string[]]$PassThruArgs) {
        Write-Host "🚀 Starting Docker Compose..." -ForegroundColor Green
        if ($PassThruArgs) { docker-compose up @PassThruArgs } else { docker-compose up }
    }

    static [void] ComposeUpBuild([string[]]$PassThruArgs) {
        Write-Host "🔨 Building and starting Docker Compose... " -ForegroundColor Blue
        if ($PassThruArgs) { docker-compose up --build @PassThruArgs } else { docker-compose up --build }
    }

    static [void] ComposeDown([string[]]$PassThruArgs) {
        Write-Host "🛑 Stopping Docker Compose..." -ForegroundColor Yellow
        if ($PassThruArgs) { docker-compose down @PassThruArgs } else { docker-compose down }
    }

    static [void] RemoveUnusedVolumes() {
        Write-Host "🧹 Pruning Docker volumes..." -ForegroundColor Magenta
        docker volume prune
    }

    static [void] RemoveUnusedImages() {
        Write-Host "🧹 Pruning Docker images..." -ForegroundColor Magenta
        docker image prune
    }
}
#endregion
