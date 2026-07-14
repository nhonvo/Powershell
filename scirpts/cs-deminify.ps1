#!/usr/bin/env pwsh
# cs-deminify.ps1 — Re-format a minified C# file produced by cs-minify.ps1
# Usage:
#   .\cs-deminify.ps1                        # auto: dist\*.min.cs
#   .\cs-deminify.ps1 -In .\dist\Prog.min.cs
#   .\cs-deminify.ps1 -IndentSize 2          # 2-space indent
#   .\cs-deminify.ps1 -DryRun               # stats only
#
# Input format expected (produced by cs-minify.ps1):
#   // CS-MINI part=001/003 src=Program chars=0-63876
#   <chunk1 data>
#   // CS-MINI part=002/003 src=Program chars=63877-127831
#   <chunk2 data>
#   ...
#
# Algorithm:
#   Strip    — remove all // CS-MINI header lines → pure minified code
#   Pass 1   — tokenize: split on { } ; respecting string/char literals
#   Pass 2   — re-indent: track brace depth, Allman { on own line
#   Pass 3   — restore readability: keyword( → keyword (, comma spacing, =>
#   Pass 4   — collapse excess blank lines
 
[CmdletBinding()]
param(
    [string] $In         = "",     # auto-detected if empty (see below)
    [string] $Out        = "",
    [int]    $IndentSize = 4,
    [switch] $DryRun
)
 
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
 
# ─────────────────────────────────────────────────────────────────────────────
# SECTION 1 — input resolver
# Reads a single .min.cs file, strips embedded // CS-MINI header lines,
# and returns (pureCode, resolvedPath, defaultOutputPath, partCount).
# ─────────────────────────────────────────────────────────────────────────────
 
function Resolve-Input([string]$inParam) {
    # Auto-detect: look for *.min.cs in .\dist\
    if (-not $inParam) {
        $candidates = @(Get-ChildItem -Path ".\dist" -Filter "*.min.cs" -ErrorAction SilentlyContinue |
                        Sort-Object Name)
        if ($candidates.Count -eq 0) { throw "No .min.cs found in .\dist\  Run cs-minify.ps1 first." }
        $inParam = $candidates[0].FullName
    }
 
    $resolved = (Resolve-Path $inParam).Path
    $dir      = [System.IO.Path]::GetDirectoryName($resolved)
    $baseName = [System.IO.Path]::GetFileNameWithoutExtension($resolved) -replace '\.min$', ''
 
    $raw      = [System.IO.File]::ReadAllText($resolved, [System.Text.Encoding]::UTF8)
 
    # Parse embedded // CS-MINI headers to report part info, then strip them
    $headerPattern = '^// CS-MINI\s'
    $lines         = $raw -split "`n"
    $headers       = @($lines | Where-Object { $_ -match $headerPattern })
    $partCount     = $headers.Count
 
    if ($partCount -gt 0) {
        Write-Host ("  Found {0} embedded part(s):" -f $partCount) -ForegroundColor DarkGray
        foreach ($h in $headers) {
            Write-Host ("    {0}" -f $h.Trim()) -ForegroundColor DarkGray
        }
        Write-Host ""
    }
 
    # Strip header lines → pure minified code
    $pure    = ($lines | Where-Object { $_ -notmatch $headerPattern }) -join "`n"
    $outDef  = Join-Path $dir "${baseName}.pretty.cs"
    return $pure, $resolved, $outDef
}
 
# ─────────────────────────────────────────────────────────────────────────────
# SECTION 2 — tokenizer (Pass 1)
# Split the assembled source into a flat token stream.
# Tokens are: '{', '}', ';'-terminated statements, preprocessor lines.
# String/char literal content is never split.
# ─────────────────────────────────────────────────────────────────────────────
 
function Get-Tokens([string]$src) {
    $tokens = [System.Collections.Generic.List[string]]::new(8192)
    $buf    = [System.Text.StringBuilder]::new(256)
    $chars  = $src.ToCharArray()
    $n      = $chars.Length
    $i      = 0
 
    while ($i -lt $n) {
        $c    = $chars[$i]
        $next = if ($i+1 -lt $n) { $chars[$i+1] } else { [char]0 }
 
        # Preprocessor directive → atomic token
        if ($c -eq [char]'#') {
            $t = $buf.ToString().Trim(); if ($t) { $tokens.Add($t); [void]$buf.Clear() }
            [void]$buf.Append($c); $i++
            while ($i -lt $n -and $chars[$i] -ne [char]"`n") { [void]$buf.Append($chars[$i]); $i++ }
            $tokens.Add($buf.ToString().Trim()); [void]$buf.Clear()
            continue
        }
 
        # Verbatim string @"…" — copy into buffer verbatim
        if ($c -eq [char]'@' -and $next -eq [char]'"') {
            [void]$buf.Append($c); $i++
            [void]$buf.Append($chars[$i]); $i++
            while ($i -lt $n) {
                $vc = $chars[$i]; [void]$buf.Append($vc); $i++
                if ($vc -eq [char]'"') {
                    if ($i -lt $n -and $chars[$i] -eq [char]'"') { [void]$buf.Append($chars[$i]); $i++ }
                    else { break }
                }
            }
            continue
        }
 
        # Regular / interpolated string "…" (brace-depth for $"…{…}")
        if ($c -eq [char]'"') {
            [void]$buf.Append($c); $i++
            $depth = 0
            while ($i -lt $n) {
                $sc = $chars[$i]; [void]$buf.Append($sc); $i++
                if   ($sc -eq [char]'\')                       { if ($i -lt $n) { [void]$buf.Append($chars[$i]); $i++ } }
                elseif ($sc -eq [char]'"' -and $depth -eq 0)  { break }
                elseif ($sc -eq [char]'{')                     { $depth++ }
                elseif ($sc -eq [char]'}' -and $depth -gt 0)  { $depth-- }
            }
            continue
        }
 
        # Char literal '…'
        if ($c -eq [char]"'") {
            [void]$buf.Append($c); $i++
            while ($i -lt $n) {
                $cc = $chars[$i]; [void]$buf.Append($cc); $i++
                if ($cc -eq [char]'\') { if ($i -lt $n) { [void]$buf.Append($chars[$i]); $i++ } }
                elseif ($cc -eq [char]"'") { break }
            }
            continue
        }
 
        # Opening brace → flush, emit { as own token
        if ($c -eq [char]'{') {
            $t = $buf.ToString().Trim(); if ($t) { $tokens.Add($t) }
            $tokens.Add('{'); [void]$buf.Clear(); $i++; continue
        }
 
        # Closing brace → flush, emit } as own token
        if ($c -eq [char]'}') {
            $t = $buf.ToString().Trim(); if ($t) { $tokens.Add($t) }
            $tokens.Add('}'); [void]$buf.Clear(); $i++; continue
        }
 
        # Semicolon → flush with ; included
        if ($c -eq [char]';') {
            [void]$buf.Append($c); $i++
            $t = $buf.ToString().Trim(); if ($t) { $tokens.Add($t) }
            [void]$buf.Clear(); continue
        }
 
        [void]$buf.Append($c); $i++
    }
 
    $t = $buf.ToString().Trim(); if ($t) { $tokens.Add($t) }
    return $tokens
}
 
# ─────────────────────────────────────────────────────────────────────────────
# SECTION 3 — readability restore (Pass 3 helper)
# Applied to the CODE portions of a token only — string/char literals passed
# through verbatim to avoid mangling content like ',' or "a=>b".
# ─────────────────────────────────────────────────────────────────────────────
 
# C# keywords that precede a ( expression
$KW_PAREN = 'if|else if|for|foreach|while|switch|catch|lock|fixed|using|when'
$KW_ALONE = 'else|try|finally|do'
 
function Restore-Spacing([string]$line) {
    $sb    = [System.Text.StringBuilder]::new($line.Length + 24)
    $chars = $line.ToCharArray()
    $n     = $chars.Length
    $i     = 0
    $code  = [System.Text.StringBuilder]::new(64)
 
    $FlushCode = {
        $seg = $code.ToString()
        if ($seg -eq '') { return }
        $seg = [regex]::Replace($seg, "\b($KW_PAREN)\(",    '$1 (')
        $seg = [regex]::Replace($seg, '([)\]>A-Za-z0-9_])\{', '$1 {')
        $seg = [regex]::Replace($seg, '(?<![=!<>])=>(?![>=])', ' => ')
        $seg = [regex]::Replace($seg, ',([A-Za-z0-9_(\[!@])', ', $1')
        $seg = $seg -replace ' {2,}', ' '
        [void]$sb.Append($seg)
        [void]$code.Clear()
    }
 
    while ($i -lt $n) {
        $c = $chars[$i]
 
        # Verbatim @"…"
        if ($c -eq [char]'@' -and $i+1 -lt $n -and $chars[$i+1] -eq [char]'"') {
            & $FlushCode
            [void]$sb.Append($c); $i++; [void]$sb.Append($chars[$i]); $i++
            while ($i -lt $n) {
                $vc = $chars[$i]; [void]$sb.Append($vc); $i++
                if ($vc -eq [char]'"') {
                    if ($i -lt $n -and $chars[$i] -eq [char]'"') { [void]$sb.Append($chars[$i]); $i++ }
                    else { break }
                }
            }
            continue
        }
 
        # Regular / interpolated "…"
        if ($c -eq [char]'"') {
            & $FlushCode
            [void]$sb.Append($c); $i++
            while ($i -lt $n) {
                $sc = $chars[$i]; [void]$sb.Append($sc); $i++
                if ($sc -eq [char]'\') { if ($i -lt $n) { [void]$sb.Append($chars[$i]); $i++ } }
                elseif ($sc -eq [char]'"') { break }
            }
            continue
        }
 
        # Char literal '…'
        if ($c -eq [char]"'") {
            & $FlushCode
            [void]$sb.Append($c); $i++
            while ($i -lt $n) {
                $cc = $chars[$i]; [void]$sb.Append($cc); $i++
                if ($cc -eq [char]'\') { if ($i -lt $n) { [void]$sb.Append($chars[$i]); $i++ } }
                elseif ($cc -eq [char]"'") { break }
            }
            continue
        }
 
        [void]$code.Append($c); $i++
    }
 
    & $FlushCode
    return $sb.ToString().Trim()
}
 
# ─────────────────────────────────────────────────────────────────────────────
# SECTION 4 — formatter (Pass 2 + 3 combined)
# ─────────────────────────────────────────────────────────────────────────────
 
function Ind([int]$d) { ' ' * ($d * $IndentSize) }
 
function Format-Tokens([System.Collections.Generic.List[string]]$tokens) {
    $out       = [System.Collections.Generic.List[string]]::new($tokens.Count + 512)
    $depth     = 0
    $prevTok   = ''
 
    $blankBefore = @(
        '^(public|private|internal|protected)\s+(static\s+|abstract\s+|sealed\s+)?(class|record|interface|enum|struct)\b',
        '^namespace\b',
        '^using\b',
        '^\[(?!\[)',          # attribute (not double [[ which is array)
        "^($KW_ALONE)\b"
    )
    $methodPat = '^(public|private|protected|internal|static|override|virtual|abstract|sealed)\b.+\('
 
    foreach ($tok in $tokens) {
        $t = $tok.Trim()
        if ($t -eq '') { continue }
 
        # Preprocessor → column 0, flanked by blank lines
        if ($t -match '^#') {
            if ($prevTok -ne '') { $out.Add('') }
            $out.Add($t)
            $out.Add('')
            $prevTok = $t; continue
        }
 
        # } — dedent first
        if ($t -eq '}') {
            $depth = [math]::Max(0, $depth - 1)
            if ($depth -le 1 -and $prevTok -notin @('', '{')) { $out.Add('') }
            $out.Add((Ind $depth) + '}')
            $prevTok = '}'; continue
        }
 
        # { — Allman: own line, then indent
        if ($t -eq '{') {
            $out.Add((Ind $depth) + '{')
            $depth++
            $prevTok = '{'; continue
        }
 
        # Blank line before declarations
        if (($blankBefore | Where-Object { $t -match $_ }) -and
            $prevTok -ne '' -and $prevTok -notin @('{', '}')) {
            $out.Add('')
        }
 
        # Blank line before method/property signatures
        if ($prevTok -notin @('', '{') -and $t -match $methodPat) {
            $out.Add('')
        }
 
        $out.Add((Ind $depth) + (Restore-Spacing $t))
        $prevTok = $t
    }
    return $out
}
 
# ─────────────────────────────────────────────────────────────────────────────
# SECTION 5 — blank-line cleaner (Pass 4)
# ─────────────────────────────────────────────────────────────────────────────
 
function Clean-Blanks([System.Collections.Generic.List[string]]$lines) {
    $result = [System.Collections.Generic.List[string]]::new($lines.Count)
    $run    = 0
    foreach ($ln in $lines) {
        if ($ln.Trim() -eq '') { if (++$run -le 1) { $result.Add('') } }
        else                   { $run = 0; $result.Add($ln) }
    }
    return $result
}
 
# ─────────────────────────────────────────────────────────────────────────────
# SECTION 6 — main
# ─────────────────────────────────────────────────────────────────────────────
 
Write-Host "De-minifying…" -ForegroundColor Cyan
 
$pure, $resolvedIn, $defaultOut = Resolve-Input $In
 
$outPath = if ($Out) { $Out } else { $defaultOut }
Write-Host ("  Source   : {0}" -f $resolvedIn)  -ForegroundColor DarkCyan
Write-Host ("  Output   : {0}" -f $outPath)     -ForegroundColor DarkCyan
Write-Host ""
 
$tokens    = Get-Tokens    $pure
$formatted = Format-Tokens $tokens
$cleaned   = Clean-Blanks  $formatted
$final     = ($cleaned -join "`n").TrimEnd()
 
$inBytes  = [System.Text.Encoding]::UTF8.GetByteCount($pure)
$outBytes = [System.Text.Encoding]::UTF8.GetByteCount($final)
 
Write-Host ("  Minified : {0,7:N1} KB   {1,5} chars" -f ($inBytes/1KB),  $pure.Length)    -ForegroundColor DarkGray
Write-Host ("  Pretty   : {0,7:N1} KB   {1,5} lines" -f ($outBytes/1KB), $cleaned.Count) -ForegroundColor Green
 
if ($DryRun) {
    Write-Host "  [DryRun] Nothing written." -ForegroundColor Yellow
} else {
    $outDir = [System.IO.Path]::GetDirectoryName($outPath)
    if ($outDir) { [System.IO.Directory]::CreateDirectory($outDir) | Out-Null }
    [System.IO.File]::WriteAllText($outPath, $final, [System.Text.Encoding]::UTF8)
    Write-Host "  Written  : $outPath" -ForegroundColor Cyan
}
 