#region DOTNET HELPER
# ==============================================================================
#  Shortcuts and migrations tool wrappers for the .NET SDK.
# ==============================================================================

class DotNetHelper {
    static [void] Run([string[]]$PassThruArgs) {
        Write-Host "🚀 Running project..." -ForegroundColor Green
        if ($PassThruArgs) { dotnet run @PassThruArgs } else { dotnet run }
    }

    static [void] CleanBinObj() {
        Write-Host "💥 Destroying bin/ and obj/ folders..." -ForegroundColor Red
        Get-ChildItem -Path . -Recurse -Directory -Include bin,obj -Force -ErrorAction SilentlyContinue |
            Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
        Write-Host "✅ Clean complete." -ForegroundColor Green
    }

    static [void] NewSolution([string]$Name) {
        dotnet new sln -n $Name
    }

    static [void] AddAllProjectsToSolution() {
        $projects = Get-ChildItem -Recurse -Filter "*.csproj"
        foreach ($p in $projects) {
            dotnet sln add $p.FullName
        }
    }

    static [void] WatchTest([string[]]$PassThruArgs) {
        Write-Host "👀 Watching Tests..." -ForegroundColor Yellow
        if ($PassThruArgs) { dotnet watch test @PassThruArgs } else { dotnet watch test }
    }

    static [void] Watch([string[]]$PassThruArgs) {
        Write-Host "👀 Watching for changes..." -ForegroundColor Cyan
        if ($PassThruArgs) { dotnet watch @PassThruArgs } else { dotnet watch }
    }

    static [void] Build([string[]]$PassThruArgs) {
        Write-Host "🔨 Building project..." -ForegroundColor Blue
        if ($PassThruArgs) { dotnet build @PassThruArgs } else { dotnet build }
    }

    static [void] Format([string[]]$PassThruArgs) {
        Write-Host "💅 Formatting code..." -ForegroundColor Magenta
        if ($PassThruArgs) { dotnet format @PassThruArgs } else { dotnet format }
    }

    static [void] Test([string[]]$PassThruArgs) {
        Write-Host "🧪 Running tests..." -ForegroundColor Yellow
        if ($PassThruArgs) { dotnet test @PassThruArgs } else { dotnet test }
    }

    static [void] Clean([string[]]$PassThruArgs) {
        Write-Host "🧹 Cleaning project..." -ForegroundColor Yellow
        if ($PassThruArgs) { dotnet clean @PassThruArgs } else { dotnet clean }
    }

    static [void] Restore([string[]]$PassThruArgs) {
        Write-Host "📦 Restoring packages..." -ForegroundColor Magenta
        if ($PassThruArgs) { dotnet restore @PassThruArgs } else { dotnet restore }
    }

    static [void] UpdateDatabase([string]$Context) {
        Write-Host "📈 Updating database..." -ForegroundColor Green
        $params = @("ef", "database", "update")
        if ($Context) { $params += @("--context", $Context) }
        dotnet $params
    }

    static [void] AddMigration([string]$MigrationName, [string]$Context) {
        Write-Host "➕ Adding migration: $MigrationName" -ForegroundColor Cyan
        $params = @("ef", "migrations", "add", $MigrationName)
        if ($Context) { $params += @("--context", $Context) }
        dotnet $params
    }

    static [void] RemoveDatabase([string]$Context) {
        Write-Host "🔥 Dropping database..." -ForegroundColor Red
        if ((Read-Host "Are you sure? (y/N)") -eq "y") {
            $params = @("ef", "database", "drop", "--force")
            if ($Context) { $params += @("--context", $Context) }
            dotnet $params
            Write-Host "Database dropped."
        } else {
            Write-Host "Cancelled."
        }
    }

    static [void] RemoveMigration([string]$Context) {
        Write-Host "⏪ Removing last migration..." -ForegroundColor Yellow
        $params = @("ef", "migrations", "remove")
        if ($Context) { $params += @("--context", $Context) }
        dotnet $params
    }

    static [void] NewConsole([string]$Name) {
        dotnet new console -n $Name
    }

    static [void] NewWebApi([string]$Name) {
        dotnet new webapi -n $Name
    }
}
#endregion
