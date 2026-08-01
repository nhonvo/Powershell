# PowerShell script to verify the exact rendered menu structure of MenuNodeBuilder

Add-Type -Path "$PSScriptRoot\..\csapp\AgyTui\bin\Debug\net9.0\AgyTui.dll"

$root = [AgyTui.UI.Core.Layouts.MenuNodeBuilder]::BuildTree()

Write-Host "=================== CONTROL CENTER TUI MENU HIERARCHY ===================" -ForegroundColor Cyan

foreach ($cat in $root.Children) {
    Write-Host "`n=== $($cat.Label) ===" -ForegroundColor Yellow
    foreach ($child in $cat.Children) {
        if ($child.Kind -eq [AgyTui.UI.Core.Layouts.MenuNodeKind]::Group) {
            Write-Host "  $($child.Label)" -ForegroundColor Green
            foreach ($cmd in $child.Children) {
                Write-Host "    • $($cmd.Label)" -ForegroundColor Gray
            }
        } else {
            Write-Host "  • $($child.Label)" -ForegroundColor White
        }
    }
}
