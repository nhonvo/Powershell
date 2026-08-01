# PowerShell script to extract all categories, groups, commands, and child actions from CommandRegistry.cs

$file = "$PSScriptRoot\..\csapp\AgyTui\UI\Core\Registries\CommandRegistry.cs"
$content = Get-Content $file -Raw

# Match new("alias", "name", "desc", "cat", "helpCat", ...)
$regex = 'new\("([^"]+)",\s*"([^"]+)",\s*"([^"]+)",\s*"([^"]+)"'
$matches = [regex]::Matches($content, $regex)

$commands = @()
foreach ($m in $matches) {
    $commands += [PSCustomObject]@{
        Alias       = $m.Groups[1].Value
        DisplayName = $m.Groups[2].Value
        Description = $m.Groups[3].Value
        Category    = $m.Groups[4].Value
    }
}

$grouped = $commands | Group-Object Category

foreach ($cat in $grouped) {
    Write-Host "## Category: $($cat.Name)" -ForegroundColor Cyan
    foreach ($cmd in $cat.Group) {
        Write-Host "  - [$($cmd.Alias)] $($cmd.DisplayName) — $($cmd.Description)"
    }
    Write-Host ""
}
