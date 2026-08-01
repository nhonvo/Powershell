# PowerShell script to scan all interface files and their directory paths across csapp/AgyTui

$csFiles = Get-ChildItem -Path "$PSScriptRoot\..\csapp\AgyTui" -Filter "*.cs" -Recurse

$interfaces = @()

foreach ($file in $csFiles) {
    if ($file.FullName -match '\\(obj|bin)\\') { continue }
    
    $content = Get-Content $file.FullName -Raw
    $matches = [regex]::Matches($content, 'public\s+interface\s+(\w+)')
    
    foreach ($m in $matches) {
        $interfaceName = $m.Groups[1].Value
        $relPath = $file.FullName.Replace("$PSScriptRoot\..\", "")
        $isInInterfacesFolder = $file.DirectoryName -match '\\Interfaces$'
        
        $interfaces += [PSCustomObject]@{
            InterfaceName         = $interfaceName
            FileName              = $file.Name
            CurrentPath           = $relPath
            CurrentDirectory      = $file.DirectoryName.Replace("$PSScriptRoot\..\", "")
            IsInInterfacesFolder  = $isInInterfacesFolder
        }
    }
}

Write-Host "=== All Scanned Interfaces in csapp/AgyTui ===" -ForegroundColor Cyan
Write-Host "Total Interfaces Found: $($interfaces.Count)" -ForegroundColor Yellow
Write-Host "Interfaces in 'Interfaces/' Folder: $($interfaces.Where({$_.IsInInterfacesFolder}).Count)" -ForegroundColor Green
Write-Host "Interfaces Needing Relocation: $($interfaces.Where({-not $_.IsInInterfacesFolder}).Count)" -ForegroundColor Red
Write-Host ""

$interfaces | Format-Table -AutoSize
