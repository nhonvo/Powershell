using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
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

        while (true)
        {
            AnsiConsole.Clear();
            AnsiConsole.Write(new Rule($"[bold cyan]SQLite: {Path.GetFileName(dbPath).EscapeMarkup()}[/]").RuleStyle("grey"));

            var options = new[] { "Inspect Tables", "Execute SQL Query (Backup-before-write)", "Exit" };
            var choice = SpectreMenu.Show("Database Options", options, 0);

            if (choice == 0)
            {
                var schemaOutput = Helpers.ProcessRunner.RunCapture("sqlite3", $"\"{dbPath}\" .schema");
                if (string.IsNullOrWhiteSpace(schemaOutput))
                {
                    SpectrePanel.Warning("No schema found or sqlite3 CLI not available.");
                    Thread.Sleep(1500);
                    continue;
                }
                var tables = Regex.Matches(schemaOutput, @"CREATE TABLE\s+(?:IF NOT EXISTS\s+)?[""']?(\w+)[""']?", RegexOptions.IgnoreCase)
                    .Select(m => m.Groups[1].Value)
                    .Distinct()
                    .ToArray();

                if (tables.Length == 0)
                {
                    SpectrePanel.Info("No tables found.");
                    Thread.Sleep(1500);
                    continue;
                }
                var idx = SpectreMenu.Show("Select table to inspect", tables, 0, true);
                if (idx < 0) continue;

                var tableData = Helpers.ProcessRunner.RunCapture("sqlite3", $"\"{dbPath}\" -header -column \"SELECT * FROM {tables[idx]} LIMIT 50;\"");
                var lines = tableData.Split('\n').Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();
                if (lines.Length == 0)
                {
                    SpectrePanel.Info("Table is empty.");
                    Thread.Sleep(1500);
                    continue;
                }
                var headers = lines[0].Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(h => h.Trim()).ToArray();
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
                        var row = new string[headers.Length];
                        for (var j = 0; j < headers.Length; j++)
                        {
                            row[j] = j < parts.Length ? parts[j] : "";
                        }
                        dataLines.Add(row);
                    }
                }
                AnsiConsole.Clear();
                AnsiConsole.Write(new Rule($"[bold white]Table: {tables[idx]}[/]").RuleStyle("grey"));
                SpectreTable.Render(headers, dataLines.ToArray());
                Console.WriteLine("\nPress any key to return...");
                Console.ReadKey(true);
            }
            else if (choice == 1)
            {
                ExecuteSqlQuery(dbPath);
            }
            else
            {
                break;
            }
        }
    }

    public static void ExecuteSqlQuery(string dbPath)
    {
        if (!File.Exists(dbPath))
        {
            SpectrePanel.Error($"Database not found: {dbPath}");
            return;
        }

        var sql = AnsiConsole.Ask<string>("Enter SQL Query to execute:").Trim();
        if (string.IsNullOrEmpty(sql)) return;

        bool isWrite = Regex.IsMatch(sql, @"\b(insert|update|delete|drop|create|alter|replace)\b", RegexOptions.IgnoreCase);
        if (isWrite)
        {
            var backupPath = dbPath + ".bak";
            try
            {
                File.Copy(dbPath, backupPath, true);
                if (File.Exists(dbPath + "-wal")) File.Copy(dbPath + "-wal", backupPath + "-wal", true);
                if (File.Exists(dbPath + "-shm")) File.Copy(dbPath + "-shm", backupPath + "-shm", true);
                AnsiConsole.MarkupLine($"[green][[DB]] Backup created successfully at '{Path.GetFileName(backupPath)}' (including WAL/SHM files) before executing write operation.[/]");
            }
            catch (Exception ex)
            {
                SpectrePanel.Error($"Failed to create database backup: {ex.Message}");
                if (!AnsiConsole.Confirm("Do you want to proceed without a backup?"))
                {
                    return;
                }
            }
        }

        try
        {
            AnsiConsole.MarkupLine($"[yellow]Executing query: {sql.EscapeMarkup()}[/]");
            var output = Helpers.ProcessRunner.RunCapture("sqlite3", $"\"{dbPath}\" \"{sql}\"");
            if (string.IsNullOrWhiteSpace(output))
            {
                AnsiConsole.MarkupLine("[green]Query executed successfully with no output returned.[/]");
            }
            else
            {
                AnsiConsole.WriteLine(output);
            }
        }
        catch (Exception ex)
        {
            SpectrePanel.Error($"Query execution failed: {ex.Message}");
        }

        Console.WriteLine("\nPress any key to return...");
        Console.ReadKey(true);
    }
}
