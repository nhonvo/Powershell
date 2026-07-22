using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Spectre.Console;
using AgyTui.Components;

namespace AgyTui;

public static class FileExplorer
{
    public static string? Browse(string rootPath)
    {
        var current = Directory.Exists(rootPath) ? rootPath : Directory.GetCurrentDirectory();
        while (true)
        {
            AnsiConsole.Clear();
            AnsiConsole.Write(new Rule($"[bold cyan]IDE: {current.EscapeMarkup()}[/]").RuleStyle("grey"));
            var entries = new List<string> { ".. (go up)" };

            try
            {
                entries.AddRange(Directory.GetDirectories(current).Select(d => $"📁 {Path.GetFileName(d)}"));
                entries.AddRange(Directory.GetFiles(current).Select(f => $"{GetFileIcon(Path.GetExtension(f))} {Path.GetFileName(f)}"));
            }
            catch (Exception ex)
            {
                SpectrePanel.Error(ex.Message);
                return null;
            }
            entries.Add("← Exit Explorer");
            var idx = SpectreMenu.Show($"Explorer: {Path.GetFileName(current)}", [.. entries], 0, true);
            if (idx < 0 || idx == entries.Count - 1) return null;
            if (idx == 0)
            {
                var parent = Path.GetDirectoryName(current);
                if (parent != null) current = parent;
                continue;
            }
            var selectedLine = entries[idx];
            var spaceIdx = selectedLine.IndexOf(' ');
            var selected = spaceIdx >= 0 ? selectedLine[(spaceIdx + 1)..].Trim() : selectedLine;
            var fullPath = Path.Combine(current, selected);
            if (Directory.Exists(fullPath))
            {
                current = fullPath;
                continue;
            }
            if (File.Exists(fullPath)) return fullPath;
        }
    }

    private static string GetFileIcon(string ext) => ext.ToLower() switch
    {
        ".cs" => "⚙",
        ".json" => "📋",
        ".md" => "📝",
        ".txt" => "📄",
        ".ps1" => "⚡",
        ".sh" => "⚡",
        ".yaml" or ".yml" => "🔧",
        ".csproj" or ".sln" => "🏗",
        _ => "📄"
    };
}
