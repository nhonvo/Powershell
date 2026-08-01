# PowerShell script to scan all C# interface methods and check their usage in CommandRegistry / CommandRouter

$csFiles = Get-ChildItem -Path "$PSScriptRoot\..\csapp\AgyTui" -Filter "*.cs" -Recurse

# Read CommandRegistry and CommandRouter content to check menu usage
$registryFile = "$PSScriptRoot\..\csapp\AgyTui\UI\Core\Registries\CommandRegistry.cs"
$routerFile   = "$PSScriptRoot\..\csapp\AgyTui\UI\Core\Navigation\CommandRouter.cs"
$menuCodeContent = (Get-Content $registryFile -Raw) + "`n" + (Get-Content $routerFile -Raw)

$interfaceMethods = @()

foreach ($file in $csFiles) {
    if ($file.FullName -match '\\(obj|bin)\\') { continue }
    
    $lines = Get-Content $file.FullName
    $currentInterface = $null
    
    for ($i = 0; $i -lt $lines.Count; $i++) {
        $line = $lines[$i]
        
        if ($line -match 'public\s+interface\s+(\w+)') {
            $currentInterface = $matches[1]
        }
        
        if ($currentInterface -and $line -match '^\s*([\w\<\>\[\]\?]+)\s+(\w+)\s*\(') {
            $returnType = $matches[1]
            $methodName = $matches[2]
            
            # Skip non-method lines (like properties/getters)
            if ($line -match '\{\s*get;' -or $methodName -eq "get" -or $methodName -eq "set") { continue }

            $isMenuUsed = $menuCodeContent.Contains($methodName)
            
            $interfaceMethods += [PSCustomObject]@{
                InterfaceName = $currentInterface
                MethodName    = $methodName
                ReturnType    = $returnType
                IsMenuUsed    = $isMenuUsed
                FilePath      = $file.FullName.Replace("$PSScriptRoot\..\", "")
                Line          = $i + 1
            }
        }
    }
}

Write-Host "=== Interface Methods Scan Summary ===" -ForegroundColor Cyan
Write-Host "Total Interface Methods Scanned: $($interfaceMethods.Count)" -ForegroundColor Yellow
Write-Host "Exposed in Menu Commands:        $($interfaceMethods.Where({$_.IsMenuUsed}).Count)" -ForegroundColor Green
Write-Host "Internal Architecture Logic:     $($interfaceMethods.Where({-not $_.IsMenuUsed}).Count)" -ForegroundColor Magenta
Write-Host ""
