# scripts/dotnet_new_solution.ps1
param (
    [string]$SolutionName
)

if (-not $SolutionName) {
    Write-Error "Please provide a SolutionName."
    exit 1
}

# Create Solution
dotnet new sln -n $SolutionName
mkdir src

# Create Projects
dotnet new classlib -n "$SolutionName.Domain" -o "src/$SolutionName.Domain"
dotnet new classlib -n "$SolutionName.Application" -o "src/$SolutionName.Application"
dotnet new classlib -n "$SolutionName.Infrastructure" -o "src/$SolutionName.Infrastructure"
dotnet new webapi -n "$SolutionName.Api" -o "src/$SolutionName.Api"

# Add projects to solution
dotnet sln add "src/$SolutionName.Domain/$SolutionName.Domain.csproj"
dotnet sln add "src/$SolutionName.Application/$SolutionName.Application.csproj"
dotnet sln add "src/$SolutionName.Infrastructure/$SolutionName.Infrastructure.csproj"
dotnet sln add "src/$SolutionName.Api/$SolutionName.Api.csproj"

# Add Project References
# Application -> Domain
dotnet add "src/$SolutionName.Application/$SolutionName.Application.csproj" reference "src/$SolutionName.Domain/$SolutionName.Domain.csproj"

# Infrastructure -> Application
dotnet add "src/$SolutionName.Infrastructure/$SolutionName.Infrastructure.csproj" reference "src/$SolutionName.Application/$SolutionName.Application.csproj"

# Api -> Application, Infrastructure
dotnet add "src/$SolutionName.Api/$SolutionName.Api.csproj" reference "src/$SolutionName.Application/$SolutionName.Application.csproj"
dotnet add "src/$SolutionName.Api/$SolutionName.Api.csproj" reference "src/$SolutionName.Infrastructure/$SolutionName.Infrastructure.csproj"

Write-Host "Clean Architecture Solution '$SolutionName' created successfully."
