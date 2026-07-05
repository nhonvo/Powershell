#region DOTNET HELPER
# ==============================================================================
#  Shortcuts and migrations tool wrappers for the .NET SDK.
# ==============================================================================

class DotNetHelper {
    static [void] Run([string[]]$PassThruArgs) {
        Write-Host "🚀 Running project..." -ForegroundColor Green
        if ($PassThruArgs) { dotnet run $PassThruArgs | Out-Default } else { dotnet run | Out-Default }
    }

    static [void] CleanBinObj() {
        Write-Host "💥 Destroying bin/ and obj/ folders..." -ForegroundColor Red
        Get-ChildItem -Path . -Recurse -Directory -Include bin,obj -Force -ErrorAction SilentlyContinue |
            Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
        Write-Host "✅ Clean complete." -ForegroundColor Green
    }

    static [void] NewSolution([string]$Name) {
        dotnet new sln -n $Name | Out-Default
    }

    static [void] AddAllProjectsToSolution() {
        $projects = Get-ChildItem -Recurse -Filter "*.csproj"
        foreach ($p in $projects) {
            dotnet sln add $p.FullName | Out-Default
        }
    }

    static [void] WatchTest([string[]]$PassThruArgs) {
        Write-Host "👀 Watching Tests..." -ForegroundColor Yellow
        if ($PassThruArgs) { dotnet watch test $PassThruArgs | Out-Default } else { dotnet watch test | Out-Default }
    }

    static [void] Watch([string[]]$PassThruArgs) {
        Write-Host "👀 Watching for changes..." -ForegroundColor Cyan
        if ($PassThruArgs) { dotnet watch $PassThruArgs | Out-Default } else { dotnet watch | Out-Default }
    }

    static [void] Build([string[]]$PassThruArgs) {
        Write-Host "🔨 Building project..." -ForegroundColor Blue
        if ($PassThruArgs) { dotnet build $PassThruArgs | Out-Default } else { dotnet build | Out-Default }
    }

    static [void] Format([string[]]$PassThruArgs) {
        Write-Host "💅 Formatting code..." -ForegroundColor Magenta
        if ($PassThruArgs) { dotnet format $PassThruArgs | Out-Default } else { dotnet format | Out-Default }
    }

    static [void] Test([string[]]$PassThruArgs) {
        Write-Host "🧪 Running tests..." -ForegroundColor Yellow
        if ($PassThruArgs) { dotnet test $PassThruArgs | Out-Default } else { dotnet test | Out-Default }
    }

    static [void] Clean([string[]]$PassThruArgs) {
        Write-Host "🧹 Cleaning project..." -ForegroundColor Yellow
        if ($PassThruArgs) { dotnet clean $PassThruArgs | Out-Default } else { dotnet clean | Out-Default }
    }

    static [void] Restore([string[]]$PassThruArgs) {
        Write-Host "📦 Restoring packages..." -ForegroundColor Magenta
        if ($PassThruArgs) { dotnet restore $PassThruArgs | Out-Default } else { dotnet restore | Out-Default }
    }

    static [void] UpdateDatabase([string]$Context) {
        Write-Host "📈 Updating database..." -ForegroundColor Green
        $params = @("ef", "database", "update")
        if ($Context) { $params += @("--context", $Context) }
        dotnet $params | Out-Default
    }

    static [void] AddMigration([string]$MigrationName, [string]$Context) {
        Write-Host "➕ Adding migration: $MigrationName" -ForegroundColor Cyan
        $params = @("ef", "migrations", "add", $MigrationName)
        if ($Context) { $params += @("--context", $Context) }
        dotnet $params | Out-Default
    }

    static [void] RemoveDatabase([string]$Context) {
        Write-Host "🔥 Dropping database..." -ForegroundColor Red
        if ((Read-Host "Are you sure? (y/N)") -eq "y") {
            $params = @("ef", "database", "drop", "--force")
            if ($Context) { $params += @("--context", $Context) }
            dotnet $params | Out-Default
            Write-Host "Database dropped."
        } else {
            Write-Host "Cancelled."
        }
    }

    static [void] RemoveMigration([string]$Context) {
        Write-Host "⏪ Removing last migration..." -ForegroundColor Yellow
        $params = @("ef", "migrations", "remove")
        if ($Context) { $params += @("--context", $Context) }
        dotnet $params | Out-Default
    }

    static [void] NewConsole([string]$Name) {
        dotnet new console -n $Name | Out-Default
    }

    static [void] NewWebApi([string]$Name) {
        dotnet new webapi -n $Name | Out-Default
    }
}
#endregion
