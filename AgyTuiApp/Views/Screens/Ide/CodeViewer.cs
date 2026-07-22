using System;
using System.Buffers;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Spectre.Console;
using AgyTui.Components;

namespace AgyTui;

public static class CodeViewer
{
    private static readonly SearchValues<char> StringDelimiters = SearchValues.Create(['"', '\'', '`']);

    public static void Show(string filePath)
    {
        if (!File.Exists(filePath))
        {
            SpectrePanel.Error($"File not found: {filePath}");
            return;
        }
        var lines = LoadWithLineNumbers(filePath);
        SpectrePager.Show(Path.GetFileName(filePath), lines);
    }

    public static void ShowWithHighlight(string filePath, int[] highlightLines)
    {
        if (!File.Exists(filePath))
        {
            SpectrePanel.Error($"File not found: {filePath}");
            return;
        }
        var ext = Path.GetExtension(filePath).ToLower();
        var rawLines = File.ReadAllLines(filePath);
        var numbered = rawLines.Select((l, i) =>
        {
            var num = $"{i + 1,4} ";
            var colored = ColorizeToken(l, ext);
            return highlightLines.Contains(i + 1) ? $"[yellow]{num}→[/] {colored}" : $"[dim]{num}[/] {colored}";
        }).ToArray();
        SpectrePager.Show(Path.GetFileName(filePath), numbered);
    }

    private static string[] LoadWithLineNumbers(string filePath)
    {
        var ext = Path.GetExtension(filePath).ToLower();
        var lines = File.ReadAllLines(filePath);
        return lines.Select((l, i) => $"[dim]{i + 1,4}[/] {ColorizeToken(l, ext)}").ToArray();
    }

    internal static string ColorizeToken(string line, string ext)
    {
        if (string.IsNullOrWhiteSpace(line)) return string.Empty;
        var escaped = line.EscapeMarkup();
        if (ext == ".cs")
        {
            var keywords = new[] { "public", "private", "protected", "internal", "class", "void", "string", "int", "var", "return", "if", "else", "foreach", "using", "namespace", "static", "new" };
            foreach (var kw in keywords)
            {
                escaped = Regex.Replace(escaped, $@"\b{kw}\b", $"[blue]{kw}[/]");
            }
            escaped = Regex.Replace(escaped, @"//.*$", m => $"[green]{m.Value}[/]");
        }
        else if (ext == ".ps1")
        {
            var keywords = new[] { "function", "param", "if", "else", "foreach", "return", "process" };
            foreach (var kw in keywords)
            {
                escaped = Regex.Replace(escaped, $@"\b{kw}\b", $"[blue]{kw}[/]");
            }
            escaped = Regex.Replace(escaped, @"#.*$", m => $"[green]{m.Value}[/]");
        }
        return escaped;
    }
}
