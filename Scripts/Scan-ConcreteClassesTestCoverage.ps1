# PowerShell script to scan all concrete C# classes and public methods in csapp/AgyTui and verify test coverage

$csFiles = Get-ChildItem -Path "$PSScriptRoot\..\csapp\AgyTui" -Filter "*.cs" -Recurse
$testFiles = Get-ChildItem -Path "$PSScriptRoot\..\csapp\AgyTui.Tests" -Filter "*.cs" -Recurse

$testCodeMap = @{}
foreach ($tf in $testFiles) {
    $testCodeMap[$tf.Name] = Get-Content $tf.FullName -Raw
}

$allPublicMethods = @()

foreach ($file in $csFiles) {
    if ($file.FullName -match '\\(obj|bin)\\') { continue }
    
    $lines = Get-Content $file.FullName
    $currentClass = $null
    
    for ($i = 0; $i -lt $lines.Count; $i++) {
        $line = $lines[$i]
        
        if ($line -match 'public\s+(sealed\s+)?(class|struct|record)\s+(\w+)') {
            $currentClass = $matches[3]
        }
        
        if ($currentClass -and $line -match '^\s*public\s+(static\s+)?(async\s+)?([\w\<\>\[\]\?]+)\s+(\w+)\s*\(') {
            $returnType = $matches[3]
            $methodName = $matches[4]
            
            if ($line -match '\{\s*get;' -or $methodName -eq "get" -or $methodName -eq "set" -or $methodName -eq "Main") { continue }

            $hasTest = $false
            foreach ($tfName in $testCodeMap.Keys) {
                if ($testCodeMap[$tfName].Contains($methodName)) {
                    $hasTest = $true
                    break
                }
            }

            $allPublicMethods += [PSCustomObject]@{
                ClassName  = $currentClass
                MethodName = $methodName
                HasTest    = $hasTest
                FilePath   = $file.FullName.Replace("$PSScriptRoot\..\", "")
            }
        }
    }
}

$total = $allPublicMethods.Count
$covered = $allPublicMethods.Where({$_.HasTest}).Count
$pending = $total - $covered
$pct = if ($total -gt 0) { [Math]::Round(($covered / $total) * 100, 1) } else { 0 }

Write-Host "=== Concrete Class Methods Scan Summary ===" -ForegroundColor Cyan
Write-Host "Total Public Methods Scanned: $total" -ForegroundColor Yellow
Write-Host "Covered by Unit Tests:       $covered ($pct%)" -ForegroundColor Green
Write-Host "Pending Tests:               $pending" -ForegroundColor Red
Write-Host ""

if ($pending -gt 0) {
    Write-Host "Sample Pending Methods:" -ForegroundColor Magenta
    $allPublicMethods.Where({-not $_.HasTest}) | Select-Object -First 15 | Format-Table ClassName, MethodName, FilePath
}
