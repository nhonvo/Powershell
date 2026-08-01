# PowerShell script to scan all C# interfaces in csapp/AgyTui and check for multi-type co-locations

$csFiles = Get-ChildItem -Path "$PSScriptRoot\..\csapp\AgyTui" -Filter "*.cs" -Recurse

$colocated = @()

foreach ($file in $csFiles) {
    if ($file.FullName -match '\\(obj|bin)\\') { continue }
    
    $content = Get-Content $file.FullName -Raw
    
    # Match interface declarations
    $interfaceMatches = [regex]::Matches($content, 'public\s+interface\s+(\w+)')
    
    if ($interfaceMatches.Count -gt 0) {
        # Check if file also defines class, struct, record, or another interface
        $classMatches = [regex]::Matches($content, 'public\s+(static\s+)?(class|struct|record)\s+(\w+)')
        
        $typesInFile = @()
        foreach ($m in $interfaceMatches) { $typesInFile += "Interface: $($m.Groups[1].Value)" }
        foreach ($m in $classMatches) { $typesInFile += "$($m.Groups[2].Value): $($m.Groups[3].Value)" }

        if ($typesInFile.Count -gt 1) {
            $colocated += [PSCustomObject]@{
                FileName = $file.Name
                Path     = $file.FullName.Replace("$PSScriptRoot\..\", "")
                Types    = ($typesInFile -join ", ")
            }
        }
    }
}

Write-Host "=== Co-located Interfaces (Declared in same file as another type) ===" -ForegroundColor Cyan
Write-Host "Total Co-located Files Found: $($colocated.Count)" -ForegroundColor Yellow
Write-Host ""

foreach ($item in $colocated) {
    Write-Host "File: $($item.FileName)" -ForegroundColor Green
    Write-Host "   Path:  $($item.Path)"
    Write-Host "   Types: $($item.Types)"
    Write-Host ""
}
