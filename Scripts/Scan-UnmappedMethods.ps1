# PowerShell script to scan all C# public methods in csapp/AgyTui that are NOT exposed in CommandRegistry or UI menus

$csFiles = Get-ChildItem -Path "$PSScriptRoot\..\csapp\AgyTui" -Filter "*.cs" -Recurse

# 1. Read CommandRegistry.cs to find all registered command aliases and method invocations
$registryFile = "$PSScriptRoot\..\csapp\AgyTui\UI\Core\Registries\CommandRegistry.cs"
$routerFile   = "$PSScriptRoot\..\csapp\AgyTui\UI\Core\Navigation\CommandRouter.cs"
$registryContent = (Get-Content $registryFile -Raw) + (Get-Content $routerFile -Raw)

$allMethods = @()

foreach ($file in $csFiles) {
    # Skip generated/obj/bin files
    if ($file.FullName -match '\\(obj|bin)\\') { continue }
    
    $className = $file.BaseName
    $lines = Get-Content $file.FullName

    for ($i = 0; $i -lt $lines.Count; $i++) {
        $line = $lines[$i]
        
        # Regex to match public methods: public [static] [async] ReturnType MethodName(...)
        if ($line -match 'public\s+(static\s+)?(async\s+)?([\w\<\>\[\]\?]+)\s+(\w+)\s*\(') {
            $methodName = $matches[4]
            $returnType = $matches[3]
            $isStatic   = [bool]$matches[1]
            
            # Filter out constructors, property getters/setters, lifecycle methods
            if ($methodName -eq $className -or $methodName -match '^(get_|set_|Main|Build|Create|Execute|Render|Handle|Run|ToString|Equals|GetHashCode)') {
                continue
            }

            # Check if methodName appears in CommandRouter or CommandRegistry
            $isMapped = $registryContent.Contains($methodName)

            $allMethods += [PSCustomObject]@{
                ClassName  = $className
                MethodName = $methodName
                ReturnType = $returnType
                IsStatic   = $isStatic
                IsMapped   = $isMapped
                FilePath   = $file.FullName.Replace("$PSScriptRoot\..\", "")
                Line       = $i + 1
            }
        }
    }
}

$unmapped = $allMethods | Where-Object { -not $_.IsMapped } | Group-Object ClassName

Write-Host "=== Unmapped C# Public Methods (Not Exposed in Menu) ===" -ForegroundColor Cyan
Write-Host "Total Unmapped Methods Found: $($allMethods.Where({ -not $_.IsMapped }).Count)" -ForegroundColor Yellow
Write-Host ""

foreach ($group in $unmapped) {
    Write-Host "Class: $($group.Name)" -ForegroundColor Green
    foreach ($m in $group.Group) {
        Write-Host "   • $($m.MethodName)($($m.ReturnType)) [Line $($m.Line)]"
    }
    Write-Host ""
}
