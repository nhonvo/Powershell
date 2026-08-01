# PowerShell script to scan all C# static classes in csapp/AgyTui and classify them for DI refactoring

$csFiles = Get-ChildItem -Path "$PSScriptRoot\..\csapp\AgyTui" -Filter "*.cs" -Recurse

$staticClasses = @()

foreach ($file in $csFiles) {
    $lines = Get-Content $file.FullName
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i] -match '(public|internal|private)?\s+static\s+class\s+(\w+)') {
            $className = $matches[2]
            
            # Check if class contains state (static fields / properties with getters+setters)
            $hasState = $false
            $hasMethods = $false
            
            for ($j = $i + 1; $j -lt [Math]::Min($i + 50, $lines.Count); $j++) {
                if ($lines[$j] -match '^\s*\}') { break }
                if ($lines[$j] -match 'public\s+static\s+.*\s*\{ get;\s*set;') { $hasState = $true }
                if ($lines[$j] -match 'public\s+static\s+.*Cache\s*=') { $hasState = $true }
                if ($lines[$j] -match 'public\s+static\s+\w+\s+\w+\(') { $hasMethods = $true }
            }

            $type = if ($hasState) { "Stateful Service (Refactor to DI)" } elseif ($hasMethods) { "Method Helper (Evaluate DI)" } else { "Constants / Pure Data (Keep Static)" }

            $staticClasses += [PSCustomObject]@{
                ClassName = $className
                FilePath  = $file.FullName.Replace("$PSScriptRoot\..\", "")
                LineNumber = $i + 1
                Category   = $type
            }
        }
    }
}

Write-Host "=== C# Static Classes Audit Report ===" -ForegroundColor Cyan
$staticClasses | Format-Table -AutoSize
