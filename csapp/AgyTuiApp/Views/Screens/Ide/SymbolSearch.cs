using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Spectre.Console;
using AgyTui.Components;

namespace AgyTui;

public static class SymbolSearch
{
    public static void BrowseSymbols(string filePath)
    {
        if (!File.Exists(filePath))
        {
            SpectrePanel.Error($"File not found: {filePath}");
            return;
        }
        var ext = Path.GetExtension(filePath).ToLower();
        var lines = File.ReadAllLines(filePath);
        var symbols = ExtractSymbols(lines, ext);
        if (symbols.Length == 0)
        {
            SpectrePanel.Info("No symbols found.");
            return;
        }
        var idx = SpectreMenu.Show($"Symbols: {Path.GetFileName(filePath)}", symbols, 0, true);
        if (idx < 0) return;
        var lineNum = FindSymbolLine(filePath, symbols[idx].Split(' ')[0]);
        if (lineNum.HasValue) CodeViewer.ShowWithHighlight(filePath, [lineNum.Value]);
    }

    public static void BrowseWorkspaceSymbols(string dirPath)
    {
        var query = AnsiConsole.Ask<string>("[cyan]Filter workspace symbols/files (or Enter for all):[/]", "").Trim();
        var files = Directory.GetFiles(dirPath, "*.*", SearchOption.AllDirectories)
            .Where(f => f.EndsWith(".cs") || f.EndsWith(".ps1") || f.EndsWith(".ts") || f.EndsWith(".js") || f.EndsWith(".py"))
            .Where(f => !f.Contains("\\bin\\") && !f.Contains("\\obj\\") && !f.Contains("\\.git\\"))
            .ToArray();
        if (files.Length == 0)
        {
            SpectrePanel.Info("No code files found in workspace.");
            return;
        }

        var scored = files.Select(f => {
            var relPath = Path.GetRelativePath(dirPath, f);
            var score = string.IsNullOrEmpty(query) ? 100 : TerminalIde.FuzzyScore(query, relPath);
            return (File: f, RelPath: relPath, Score: score);
        })
        .Where(x => x.Score >= 0)
        .OrderByDescending(x => x.Score)
        .Take(50)
        .ToArray();

        if (scored.Length == 0)
        {
            SpectrePanel.Info($"No files matching '{query}'.");
            return;
        }

        var selectedIdx = SpectreMenu.Show("Select File to Browse Symbols", scored.Select(x => x.RelPath).ToArray(), 0, true);
        if (selectedIdx >= 0)
        {
            BrowseSymbols(scored[selectedIdx].File);
        }
    }

    public static int? FindSymbolLine(string filePath, string symbol)
    {
        var lines = File.ReadAllLines(filePath);
        for (int i = 0; i < lines.Length; i++)
            if (lines[i].Contains(symbol, StringComparison.OrdinalIgnoreCase)) return i + 1;
        return null;
    }

    private static string[] ExtractSymbols(string[] lines, string ext)
    {
        var symbols = new List<string>();
        string[] patterns = ext switch
        {
            ".cs" => [
                @"(public|private|internal|protected).*\s(class|interface|record|enum)\s+(\w+)",
                @"(public|private|static).*\s(\w+)\s*\("
            ],
            ".ts" or ".js" => [
                @"(export\s+)?(class|function|interface|type)\s+(\w+)",
                @"const\s+(\w+)\s*="
            ],
            ".ps1" => [
                @"function\s+([\w-]+)"
            ],
            _ => [
                @"\b(\w+)\s*[({]"
            ]
        };
        for (int i = 0; i < lines.Length; i++)
        {
            foreach (var pat in patterns)
            {
                var m = Regex.Match(lines[i], pat);
                if (m.Success)
                {
                    var name = m.Groups[m.Groups.Count - 1].Value;
                    if (name.Length > 2) symbols.Add($"{name,-30} ln {i + 1,4}");
                }
            }
        }
        return [.. symbols.Distinct()];
    }
}
