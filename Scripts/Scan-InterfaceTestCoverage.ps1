# PowerShell script to scan all 155 interface methods and match them against unit/integration test cases in csapp/AgyTui.Tests

$csFiles = Get-ChildItem -Path "$PSScriptRoot\..\csapp\AgyTui" -Filter "*.cs" -Recurse
$testFiles = Get-ChildItem -Path "$PSScriptRoot\..\csapp\AgyTui.Tests" -Filter "*.cs" -Recurse

# Load all test file contents into memory
$testCodeMap = @{}
foreach ($tf in $testFiles) {
    $testCodeMap[$tf.Name] = Get-Content $tf.FullName -Raw
}

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
            
            if ($line -match '\{\s*get;' -or $methodName -eq "get" -or $methodName -eq "set") { continue }

            # Find matching test cases in csapp/AgyTui.Tests
            $matchingTests = @()
            foreach ($tfName in $testCodeMap.Keys) {
                $tContent = $testCodeMap[$tfName]
                
                # Check if test file calls or references methodName
                if ($tContent.Contains($methodName)) {
                    # Extract test method names ([Fact] / [Theory] public void TestName...)
                    $tLines = $tContent -split "`r?\n"
                    for ($j = 0; $j -lt $tLines.Count; $j++) {
                        if ($tLines[$j] -match '^\s*\[(Fact|Theory)\]') {
                            # Search next 5 lines for test method signature
                            for ($k = $j; $k -lt [Math]::Min($j + 6, $tLines.Count); $k++) {
                                if ($tLines[$k] -match 'public\s+(async\s+)?(Task|void)\s+(\w+)') {
                                    $testMethodName = $matches[3]
                                    # Inspect body of this test method up to next method
                                    $bodyEnd = [Math]::Min($k + 40, $tLines.Count)
                                    $body = ($tLines[$k..$bodyEnd] -join "`n")
                                    if ($body.Contains($methodName)) {
                                        $matchingTests += "$tfName::$testMethodName"
                                    }
                                    break
                                }
                            }
                        }
                    }
                }
            }

            $matchingTests = $matchingTests | Select-Object -Unique

            $interfaceMethods += [PSCustomObject]@{
                InterfaceName = $currentInterface
                MethodName    = $methodName
                ReturnType    = $returnType
                HasTest       = ($matchingTests.Count -gt 0)
                TestCases     = ($matchingTests -join ", ")
                TestCount     = $matchingTests.Count
            }
        }
    }
}

Write-Host "=== Interface Methods Test Coverage Summary ===" -ForegroundColor Cyan
Write-Host "Total Interface Methods: $($interfaceMethods.Count)" -ForegroundColor Yellow
Write-Host "Covered by Unit Tests:  $($interfaceMethods.Where({$_.HasTest}).Count)" -ForegroundColor Green
Write-Host "Pending Unit Tests:     $($interfaceMethods.Where({-not $_.HasTest}).Count)" -ForegroundColor Red
Write-Host ""
