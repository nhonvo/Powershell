Describe "Publish Release Script Tests" {
    Context "Project File Paths" {
        It "build-release.ps1 references only project files that exist on disk" {
            $repoRoot = (Get-Item (Join-Path $PSScriptRoot "..\..\..\")).FullName
            $scriptPath = Join-Path $repoRoot "build-release.ps1"
            Test-Path $scriptPath | Should Be $true

            $content = Get-Content $scriptPath -Raw
            $regex = [regex]'(csapp/[^\s"]+\.csproj)'
            $matches = $regex.Matches($content)

            $matches.Count | Should BeGreaterThan 0

            foreach ($m in $matches) {
                $relPath = $m.Groups[1].Value
                $fullPath = Join-Path $repoRoot ($relPath -replace '/', '\')
                Test-Path $fullPath | Should Be $true
            }
        }
    }
}
