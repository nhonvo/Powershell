# PowerShell script to move all interface files into dedicated Interfaces/ subfolders

$csappRoot = "$PSScriptRoot\..\csapp\AgyTui"
$csFiles = Get-ChildItem -Path $csappRoot -Filter "*.cs" -Recurse

$movedCount = 0

foreach ($file in $csFiles) {
    if ($file.FullName -match '\\(obj|bin)\\') { continue }
    
    # Check if file name starts with 'I' followed by uppercase letter (Standard C# interface naming)
    if ($file.Name -match '^I[A-Z]\w+\.cs$') {
        # Check if already inside an 'Interfaces' folder
        if ($file.DirectoryName -notmatch '\\Interfaces$') {
            $targetDir = Join-Path $file.DirectoryName "Interfaces"
            if (-not (Test-Path $targetDir)) {
                New-Item -ItemType Directory -Path $targetDir -Force | Out-Null
            }
            
            $targetPath = Join-Path $targetDir $file.Name
            Move-Item -Path $file.FullName -Destination $targetPath -Force
            
            Write-Host "Moved: $($file.Name) -> $targetPath" -ForegroundColor Green
            $movedCount++
        }
    }
}

Write-Host ""
Write-Host "Total Interface Files Moved: $movedCount" -ForegroundColor Yellow
