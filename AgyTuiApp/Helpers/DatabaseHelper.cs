using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Spectre.Console;
using AgyTui.Components;

namespace AgyTui;

public static class DatabaseHelper
{
    public static void ShowDatabaseTui(string dbPath)
    {
        if (!File.Exists(dbPath))
        {
            SpectrePanel.Error($"Database not found: {dbPath}");
            return;
        }
        AnsiConsole.Write(new Rule($"[bold cyan]SQLite: {Path.GetFileName(dbPath).EscapeMarkup()}[/]").RuleStyle("grey"));
        var schemaOutput = Helpers.ProcessRunner.RunCapture("sqlite3", $"\"{dbPath}\" .schema");
        if (string.IsNullOrWhiteSpace(schemaOutput))
        {
            SpectrePanel.Warning("No schema found or sqlite3 CLI not available.");
            return;
        }
        var tables = Regex.Matches(schemaOutput, @"CREATE TABLE\s+(?:IF NOT EXISTS\s+)?[""']?(\w+)[""']?", RegexOptions.IgnoreCase)
            .Select(m => m.Groups[1].Value)
            .Distinct()
            .ToArray();

        if (tables.Length == 0)
        {
            SpectrePanel.Info("No tables found.");
            return;
        }
        var idx = SpectreMenu.Show("Select table to inspect", tables, 0, true);
        if (idx < 0) return;
        var tableData = Helpers.ProcessRunner.RunCapture("sqlite3", $"\"{dbPath}\" -header -column \"SELECT * FROM {tables[idx]} LIMIT 50;\"");
        var lines = tableData.Split('\n').Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();
        if (lines.Length == 0)
        {
            SpectrePanel.Info("Table is empty.");
            return;
        }
        var headers = lines[0].Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(h => h.Trim()).ToArray();
        // Simple heuristic: lines[1] is the separator like "----------"
        var dataLines = new List<string[]>();
        var startIdx = 1;
        if (lines.Length > 1 && lines[1].Contains("---"))
        {
            startIdx = 2;
        }
        for (var i = startIdx; i < lines.Length; i++)
        {
            var parts = lines[i].Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == headers.Length)
            {
                dataLines.Add(parts);
            }
            else
            {
                // Fallback: pad or slice if split is uneven
                var row = new string[headers.Length];
                for (var j = 0; j < headers.Length; j++)
                {
                    row[j] = j < parts.Length ? parts[j] : "";
                }
                dataLines.Add(row);
            }
        }
        SpectreTable.Render(headers, dataLines.ToArray());
    }
}
