#!/usr/bin/env pwsh
# cs-minify.ps1 — C# source minifier with part-splitting
# Usage:
#   .\cs-minify.ps1                          # minify .\Program.cs → dist\ parts
#   .\cs-minify.ps1 -In .\Foo.cs            # explicit input
#   .\cs-minify.ps1 -PartSize 32000         # custom chars-per-part
#   .\cs-minify.ps1 -DryRun                 # stats only, no write
#
# What it does (safe — no name mangling):
#   1. Strip //  ///  /* */  comments (string-literal-aware)
#   2. Strip ALL whitespace — indentation, operator spaces, newlines
#   3. Keep ONE space only when BOTH neighbours are identifier chars
#      (prevents keyword fusion: "public class" ≠ "publicclass")
#   4. Guard: prevent -- ++ // /* */ fusion across collapsed spaces
#   5. Preserve preprocessor directives (#if #pragma …) on own lines
#   6. Split the compressed body into ≤PartSize-char chunks, cutting only
#      at ; or } positions that fall outside any string/char literal
#   7. Write each chunk as {base}.min.part001.cs … in dist\ subfolder
 
[CmdletBinding()]
param(
    [string] $In       = ".\Program.cs",
    [string] $OutDir   = "",          # default: .\dist\
    [int]    $PartSize = 64000,       # max characters per part file
    [switch] $DryRun
)
 
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
 
# ─────────────────────────────────────────────────────────────────────────────
# SECTION 1 — character classification helpers
# ─────────────────────────────────────────────────────────────────────────────
 
function IsIdent([char]$c) {
    ($c -ge [char]'a' -and $c -le [char]'z') -or
    ($c -ge [char]'A' -and $c -le [char]'Z') -or
    ($c -ge [char]'0' -and $c -le [char]'9') -or
    $c -eq [char]'_'
}
 
# Returns $true when a pending space MUST be emitted between $p and $c.
#   • Both identifier chars  → fusion would merge keywords/names
#   • Identifier then @      → verbatim identifier prefix (int @class)
#   • -- ++ // /* */         → accidental multi-char operator creation
function NeedSpace([char]$p, [char]$c) {
    if ($p -eq [char]0)                                          { return $false }
    if ((IsIdent $p) -and (IsIdent $c))                          { return $true  }
    if ((IsIdent $p) -and $c -eq [char]'@')                      { return $true  }
    if ($p -eq [char]'-'  -and $c -eq [char]'-')                 { return $true  }
    if ($p -eq [char]'+'  -and $c -eq [char]'+')                 { return $true  }
    if ($p -eq [char]'/'  -and ($c -eq [char]'/' -or
                                 $c -eq [char]'*'))               { return $true  }
    if ($p -eq [char]'*'  -and $c -eq [char]'/')                 { return $true  }
    return $false
}
 
# ─────────────────────────────────────────────────────────────────────────────
# SECTION 2 — core compressor (single-pass char-by-char)
# ─────────────────────────────────────────────────────────────────────────────
 
function Compress-CSharp([string]$src) {
    $sb      = [System.Text.StringBuilder]::new([int]($src.Length * 0.62))
    $chars   = $src.ToCharArray()
    $n       = $chars.Length
    $i       = 0
    $pending = $false      # whitespace seen but not yet emitted
    $prevOut = [char]0     # last char written to $sb
 
    while ($i -lt $n) {
        $c = $chars[$i]
 
        # ── Verbatim string  @"…"  or  @$"…"  ──────────────────────────────
        if ($c -eq [char]'@' -and $i+1 -lt $n) {
            $n1       = $chars[$i+1]
            $isDollar = ($n1 -eq [char]'$' -and $i+2 -lt $n -and $chars[$i+2] -eq [char]'"')
            $isVStr   = ($n1 -eq [char]'"')
            if ($isDollar -or $isVStr) {
                if ($pending -and (NeedSpace $prevOut $c)) { [void]$sb.Append(' ') }
                $pending = $false
                [void]$sb.Append($c); $i++
                if ($isDollar) { [void]$sb.Append($chars[$i]); $i++ }
                [void]$sb.Append($chars[$i]); $i++   # opening "
                while ($i -lt $n) {
                    $vc = $chars[$i]; $i++; [void]$sb.Append($vc)
                    if ($vc -eq [char]'"') {
                        if ($i -lt $n -and $chars[$i] -eq [char]'"') { [void]$sb.Append($chars[$i]); $i++ }
                        else { break }
                    }
                }
                $prevOut = [char]'"'; continue
            }
        }
 
        # ── Interpolated string  $"…"  or  $@"…"  ──────────────────────────
        if ($c -eq [char]'$' -and $i+1 -lt $n) {
            $n1  = $chars[$i+1]
            $n2  = if ($i+2 -lt $n) { $chars[$i+2] } else { [char]0 }
            $isIS = ($n1 -eq [char]'"') -or ($n1 -eq [char]'@' -and $n2 -eq [char]'"')
            if ($isIS) {
                if ($pending -and (NeedSpace $prevOut $c)) { [void]$sb.Append(' ') }
                $pending = $false
                [void]$sb.Append($c); $i++
                if ($chars[$i] -eq [char]'@') { [void]$sb.Append($chars[$i]); $i++ }
                [void]$sb.Append($chars[$i]); $i++   # opening "
                $depth = 0
                while ($i -lt $n) {
                    $ic = $chars[$i]; $i++; [void]$sb.Append($ic)
                    if ($depth -eq 0) {
                        if ($ic -eq [char]'"') { break }
                        if ($ic -eq [char]'\' -and $i -lt $n) { [void]$sb.Append($chars[$i]); $i++ }
                        elseif ($ic -eq [char]'{') {
                            if ($i -lt $n -and $chars[$i] -eq [char]'{') { [void]$sb.Append($chars[$i]); $i++ }
                            else { $depth++ }
                        } elseif ($ic -eq [char]'}') {
                            if ($i -lt $n -and $chars[$i] -eq [char]'}') { [void]$sb.Append($chars[$i]); $i++ }
                        }
                    } else {
                        if ($ic -eq [char]'{') { $depth++ }
                        elseif ($ic -eq [char]'}') { $depth-- }
                    }
                }
                $prevOut = [char]'"'; continue
            }
        }
 
        # ── Regular string  "…"  ────────────────────────────────────────────
        if ($c -eq [char]'"') {
            if ($pending -and (NeedSpace $prevOut $c)) { [void]$sb.Append(' ') }
            $pending = $false
            [void]$sb.Append($c); $i++
            while ($i -lt $n) {
                $sc = $chars[$i]; $i++; [void]$sb.Append($sc)
                if ($sc -eq [char]'\' -and $i -lt $n) { [void]$sb.Append($chars[$i]); $i++ }
                elseif ($sc -eq [char]'"') { break }
            }
            $prevOut = [char]'"'; continue
        }
 
        # ── Char literal  '…'  ──────────────────────────────────────────────
        if ($c -eq [char]"'") {
            if ($pending -and (NeedSpace $prevOut $c)) { [void]$sb.Append(' ') }
            $pending = $false
            [void]$sb.Append($c); $i++
            while ($i -lt $n) {
                $cc = $chars[$i]; $i++; [void]$sb.Append($cc)
                if ($cc -eq [char]'\' -and $i -lt $n) { [void]$sb.Append($chars[$i]); $i++ }
                elseif ($cc -eq [char]"'") { break }
            }
            $prevOut = [char]"'"; continue
        }
 
        # ── Line comment  //…  (incl. ///)  ─────────────────────────────────
        if ($c -eq [char]'/' -and $i+1 -lt $n -and $chars[$i+1] -eq [char]'/') {
            while ($i -lt $n -and $chars[$i] -ne [char]"`n") { $i++ }
            $pending = $true; continue
        }
 
        # ── Block comment  /* … */  ──────────────────────────────────────────
        if ($c -eq [char]'/' -and $i+1 -lt $n -and $chars[$i+1] -eq [char]'*') {
            $i += 2
            while ($i+1 -lt $n -and -not ($chars[$i] -eq [char]'*' -and $chars[$i+1] -eq [char]'/')) { $i++ }
            if ($i+1 -lt $n) { $i += 2 } else { $i++ }
            $pending = $true; continue
        }
 
        # ── Preprocessor directive  #…  (must stay on own line)  ────────────
        if ($c -eq [char]'#' -and ($prevOut -eq [char]"`n" -or $prevOut -eq [char]0)) {
            if ($sb.Length -gt 0 -and $prevOut -ne [char]"`n") { [void]$sb.Append("`n") }
            while ($i -lt $n -and $chars[$i] -ne [char]"`n") { [void]$sb.Append($chars[$i]); $i++ }
            [void]$sb.Append("`n")
            $prevOut = [char]"`n"; $pending = $false; continue
        }
 
        # ── Whitespace  ──────────────────────────────────────────────────────
        if ($c -eq [char]' ' -or $c -eq [char]"`t" -or
            $c -eq [char]"`r" -or $c -eq [char]"`n") {
            $pending = $true; $i++; continue
        }
 
        # ── Normal character  ────────────────────────────────────────────────
        if ($pending -and (NeedSpace $prevOut $c)) { [void]$sb.Append(' ') }
        [void]$sb.Append($c)
        $prevOut = $c
        $pending = $false
        $i++
    }
 
    return $sb.ToString()
}
 
# ─────────────────────────────────────────────────────────────────────────────
# SECTION 3 — string-aware split-point finder
#
# Returns an int[] of positions in $body (exclusive-end, i.e. index AFTER a
# ; or }) that are safe to cut at — meaning the ; or } is NOT inside any
# string or char literal.
# ─────────────────────────────────────────────────────────────────────────────
 
function Find-SafeSplitPoints([string]$body) {
    $pts   = [System.Collections.Generic.List[int]]::new(4096)
    $chars = $body.ToCharArray()
    $n     = $chars.Length
    $i     = 0
 
    while ($i -lt $n) {
        $c = $chars[$i]
 
        # Skip @"…" / @$"…"
        if ($c -eq [char]'@' -and $i+1 -lt $n) {
            $n1 = $chars[$i+1]
            $isDollar = ($n1 -eq [char]'$' -and $i+2 -lt $n -and $chars[$i+2] -eq [char]'"')
            if ($n1 -eq [char]'"' -or $isDollar) {
                $i++
                if ($isDollar) { $i++ }
                $i++                # opening "
                while ($i -lt $n) {
                    $vc = $chars[$i]; $i++
                    if ($vc -eq [char]'"') {
                        if ($i -lt $n -and $chars[$i] -eq [char]'"') { $i++ }
                        else { break }
                    }
                }
                continue
            }
        }
 
        # Skip $"…" / $@"…"
        if ($c -eq [char]'$' -and $i+1 -lt $n) {
            $n1 = $chars[$i+1]
            $n2 = if ($i+2 -lt $n) { $chars[$i+2] } else { [char]0 }
            if ($n1 -eq [char]'"' -or ($n1 -eq [char]'@' -and $n2 -eq [char]'"')) {
                $i++
                if ($chars[$i] -eq [char]'@') { $i++ }
                $i++                # opening "
                $depth = 0
                while ($i -lt $n) {
                    $ic = $chars[$i]; $i++
                    if ($depth -eq 0) {
                        if ($ic -eq [char]'"') { break }
                        if ($ic -eq [char]'\' -and $i -lt $n) { $i++ }
                        elseif ($ic -eq [char]'{') {
                            if ($i -lt $n -and $chars[$i] -eq [char]'{') { $i++ }
                            else { $depth++ }
                        } elseif ($ic -eq [char]'}') {
                            if ($i -lt $n -and $chars[$i] -eq [char]'}') { $i++ }
                        }
                    } else {
                        if ($ic -eq [char]'{') { $depth++ }
                        elseif ($ic -eq [char]'}') { $depth-- }
                    }
                }
                continue
            }
        }
 
        # Skip "…"
        if ($c -eq [char]'"') {
            $i++
            while ($i -lt $n) {
                $sc = $chars[$i]; $i++
                if ($sc -eq [char]'\' -and $i -lt $n) { $i++ }
                elseif ($sc -eq [char]'"') { break }
            }
            continue
        }
 
        # Skip '…'
        if ($c -eq [char]"'") {
            $i++
            while ($i -lt $n) {
                $cc = $chars[$i]; $i++
                if ($cc -eq [char]'\' -and $i -lt $n) { $i++ }
                elseif ($cc -eq [char]"'") { break }
            }
            continue
        }
 
        # Record safe split point AFTER this ; or }
        if ($c -eq [char]';' -or $c -eq [char]'}') {
            $pts.Add($i + 1)
        }
 
        $i++
    }
 
    return $pts
}
 
# ─────────────────────────────────────────────────────────────────────────────
# SECTION 4 — split body into ≤PartSize chunks at safe boundaries
# ─────────────────────────────────────────────────────────────────────────────
 
function Split-Body([string]$body, [int]$partSize) {
    $parts   = [System.Collections.Generic.List[string]]::new()
    # @() forces an array even when PS would otherwise unwrap a single-element List
    $ptArr   = @(Find-SafeSplitPoints $body)   # int[] of safe exclusive-end positions
    $bodyLen = $body.Length
    $start   = 0
 
    while ($start -lt $bodyLen) {
        $remaining = $bodyLen - $start
        if ($remaining -le $partSize) {
            $parts.Add($body.Substring($start))
            break
        }
 
        $limit = $start + $partSize   # first position we can NOT include
 
        # Binary search: find last safe split point that falls within [start+1 .. limit]
        $lo = 0; $hi = $ptArr.Length - 1; $best = -1
        while ($lo -le $hi) {
            $mid = ($lo + $hi) -shr 1
            $pt  = $ptArr[$mid]
            if ($pt -le $limit -and $pt -gt $start) {
                $best = $pt
                $lo   = $mid + 1     # try to find a later (larger) valid point
            } elseif ($pt -le $start) {
                $lo = $mid + 1
            } else {
                $hi = $mid - 1
            }
        }
 
        # Fallback: no safe point found in window — hard-split at limit
        $splitAt = if ($best -gt 0) { $best } else { $limit }
 
        $parts.Add($body.Substring($start, $splitAt - $start))
        $start = $splitAt
    }
 
    return $parts
}
 
# ─────────────────────────────────────────────────────────────────────────────
# SECTION 5 — main
# ─────────────────────────────────────────────────────────────────────────────
 
if (-not (Test-Path $In)) { Write-Error "File not found: $In"; exit 1 }
 
$inPath  = Resolve-Path $In
$srcBase = [System.IO.Path]::GetFileNameWithoutExtension($inPath)
$distDir = if ($OutDir) { $OutDir } else {
    Join-Path ([System.IO.Path]::GetDirectoryName($inPath)) "dist"
}
if (-not $DryRun) { [System.IO.Directory]::CreateDirectory($distDir) | Out-Null }
 
Write-Host "Minifying  : $inPath" -ForegroundColor Cyan
Write-Host "  OutDir   : $distDir" -ForegroundColor DarkCyan
 
# ── Step 1: compress
$src  = [System.IO.File]::ReadAllText($inPath, [System.Text.Encoding]::UTF8)
$body = Compress-CSharp $src
 
# ── Step 2: split into ≤PartSize char chunks
$chunks = @(Split-Body $body $PartSize)
$total  = $chunks.Count
 
# ── Step 3: stats
$origBytes = [System.Text.Encoding]::UTF8.GetByteCount($src)
$miniBytes = [System.Text.Encoding]::UTF8.GetByteCount($body)
$pct       = [math]::Round((1 - $miniBytes / $origBytes) * 100, 1)
 
Write-Host ""
Write-Host ("  Original : {0,7:N1} KB   {1,5} lines" -f ($origBytes/1KB), ($src -split "`n").Count) -ForegroundColor DarkGray
Write-Host ("  Minified : {0,7:N1} KB   {1,5} chars   [{2}% reduction]" -f ($miniBytes/1KB), $body.Length, $pct) -ForegroundColor Green
Write-Host ("  Parts    : {0}  x  ≤{1:N0} chars" -f $total, $PartSize) -ForegroundColor Yellow
Write-Host ""
 
# ── Step 4: build single combined file
#   Format:
#     // CS-MINI part=001/003 src=Program chars=0-63876
#     <chunk1 data>
#     // CS-MINI part=002/003 src=Program chars=63877-127831
#     <chunk2 data>
#     ...
$sb         = [System.Text.StringBuilder]::new($miniBytes + $total * 80)
$charOffset = 0
 
for ($p = 0; $p -lt $total; $p++) {
    $chunk     = $chunks[$p]
    $partNum   = $p + 1
    $partLabel = "{0:000}" -f $partNum
    $totalLabel= "{0:000}" -f $total
    $end       = $charOffset + $chunk.Length - 1
 
    $header = "// CS-MINI part=$partLabel/$totalLabel src=$srcBase chars=$charOffset-$end"
    Write-Host ("  [{0}/{1}]  chars={2}-{3}  ({4,6} chars)" -f $partNum, $total, $charOffset, $end, $chunk.Length) -ForegroundColor Cyan
 
    [void]$sb.AppendLine($header)
    # Ensure chunk data starts on its own line and is followed by a blank separator
    $trimmed = $chunk.TrimEnd()
    [void]$sb.AppendLine($trimmed)
 
    $charOffset += $chunk.Length
}
 
$outFile    = Join-Path $distDir "${srcBase}.min.cs"
$outContent = $sb.ToString()
$outBytes   = [System.Text.Encoding]::UTF8.GetByteCount($outContent)
 
Write-Host ""
Write-Host ("  Output   : {0,7:N1} KB  → {1}" -f ($outBytes/1KB), $outFile) -ForegroundColor Green
 
if ($DryRun) {
    Write-Host "  [DryRun] Nothing written." -ForegroundColor Yellow
} else {
    [System.IO.File]::WriteAllText($outFile, $outContent, [System.Text.Encoding]::UTF8)
    Write-Host "  De-minify with:" -ForegroundColor DarkGray
    Write-Host "    .\cs-deminify.ps1 -In `"$outFile`"" -ForegroundColor DarkGray
}
 

