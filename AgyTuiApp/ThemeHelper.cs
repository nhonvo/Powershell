using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Spectre.Console;
using AgyTui.Components;

namespace AgyTui;

public static class ThemeHelper
{
    private sealed record ThemeConfig(
        [property: JsonPropertyName("active_theme")] string? active_theme,
        [property: JsonPropertyName("enable_mobile")] bool? enable_mobile
    );

    public static string? SelectThemeInteractive(string themesPath, string? currentTheme)
    {
        if (!Directory.Exists(themesPath))
        {
            SpectrePanel.Error($"Themes directory not found: {themesPath}");
            return null;
        }
        var files = Directory.GetFiles(themesPath, "*.omp.json").OrderBy(f => f, StringComparer.OrdinalIgnoreCase).ToArray();
        if (files.Length == 0)
        {
            SpectrePanel.Error($"No Oh My Posh themes (.omp.json) found in {themesPath}.");
            return null;
        }
        var themeNames = files.Select(f => Path.GetFileName(f).Replace(".omp.json", "")).ToArray();
        var displayLabels = new string[files.Length];
        for (var i = 0; i < files.Length; i++)
        {
            var preview = BuildPreview(files[i]);
            displayLabels[i] = $"{themeNames[i].PadRight(25)} │ {preview}";
        }
        var defaultIndex = currentTheme != null ? Array.IndexOf(themeNames, currentTheme) : -1;
        if (defaultIndex < 0) defaultIndex = 0;
        var selectedIndex = SpectreMenu.Show("Select Oh My Posh Theme (Color segment preview)", displayLabels, defaultIndex);
        if (selectedIndex < 0) return null;
        var selectedTheme = themeNames[selectedIndex];
        PersistConfig(themesPath, selectedTheme, selectedTheme.EndsWith("-mobile"));
        Environment.SetEnvironmentVariable("THEME", selectedTheme);
        var themePath = Path.Combine(themesPath, $"{selectedTheme}.omp.json");
        if (!File.Exists(themePath)) return null;
        AnsiConsole.MarkupLine($"[green][[Theme]] Oh My Posh theme switched to '{selectedTheme}' (Persistent).[/]");
        return themePath;
    }

    public static bool IsMobileModeActive(string? themesPath = null)
    {
        var path = ResolveThemesPath(themesPath);
        return ReadConfig(path).IsMobile;
    }

    public static string? ToggleMobileMode(string? themesPath = null) => ApplyMobileMode(ResolveThemesPath(themesPath), !ReadConfig(ResolveThemesPath(themesPath)).IsMobile);

    public static string? SetMobileMode(string themesPath, bool enableMobile) => ApplyMobileMode(themesPath, enableMobile);

    private static string ResolveThemesPath(string? themesPath)
    {
        if (!string.IsNullOrEmpty(themesPath) && Directory.Exists(themesPath)) return themesPath;
        var env = Environment.GetEnvironmentVariable("POSH_THEMES_PATH");
        if (!string.IsNullOrEmpty(env) && Directory.Exists(env)) return env;
        var defaultPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "asset", "powershell-themes");
        if (Directory.Exists(defaultPath)) return defaultPath;
        return Directory.GetCurrentDirectory();
    }

    private static string? ApplyMobileMode(string themesPath, bool enableMobile)
    {
        if (!Directory.Exists(themesPath)) return null;
        var current = ReadConfig(themesPath);
        var baseTheme = Regex.Replace(current.ThemeName, "-mobile$", "");
        var themeName = baseTheme;
        if (enableMobile)
        {
            var candidate = $"{baseTheme}-mobile";
            if (File.Exists(Path.Combine(themesPath, $"{candidate}.omp.json"))) themeName = candidate;
        }
        PersistConfig(themesPath, themeName, enableMobile);
        Environment.SetEnvironmentVariable("THEME", themeName);
        var themePath = Path.Combine(themesPath, $"{themeName}.omp.json");
        if (!File.Exists(themePath)) return null;
        AnsiConsole.MarkupLine(enableMobile ? "[cyan][[Theme]] Mobile Prompt Theme activated (ASCII mode, stacked).[/]" : "[green][[Theme]] Desktop Prompt Theme activated (Rich Unicode/Emoji mode).[/]");
        return themePath;
    }

    private static (string ThemeName, bool IsMobile) ReadConfig(string themesPath)
    {
        var configPath = Path.Combine(themesPath, "config.json");
        var themeName = "neko";
        var isMobile = false;
        if (File.Exists(configPath))
        {
            try
            {
                var cfg = JsonSerializer.Deserialize<ThemeConfig>(File.ReadAllText(configPath));
                if (!string.IsNullOrWhiteSpace(cfg?.active_theme)) themeName = cfg.active_theme!;
                if (cfg?.enable_mobile is bool b) isMobile = b;
            }
            catch
            {
            }
        }
        return (themeName, isMobile);
    }

    public static string ResolveStartupTheme(string themesPath)
    {
        var configPath = Path.Combine(themesPath, "config.json");
        if (File.Exists(configPath)) return ReadConfig(themesPath).ThemeName;
        var legacyFile = Path.Combine(themesPath, "active_theme.txt");
        if (File.Exists(legacyFile))
        {
            var theme = File.ReadAllText(legacyFile).Trim();
            try
            {
                File.Delete(legacyFile);
            }
            catch
            {
            }
            PersistConfig(themesPath, theme, theme.EndsWith("-mobile"));
            return theme;
        }
        return "neko";
    }

    private static void PersistConfig(string themesPath, string themeName, bool enableMobile)
    {
        var configPath = Path.Combine(themesPath, "config.json");
        try
        {
            File.WriteAllText(configPath, JsonSerializer.Serialize(new ThemeConfig(themeName, enableMobile)));
        }
        catch
        {
        }
        try
        {
            File.Delete(Path.Combine(themesPath, "active_theme.txt"));
        }
        catch
        {
        }
        try
        {
            File.Delete(Path.Combine(themesPath, "mobile_mode_active.txt"));
        }
        catch
        {
        }
    }

    private static string BuildPreview(string filePath)
    {
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(filePath));
            if (!doc.RootElement.TryGetProperty("blocks", out var blocks)) return "";
            var parts = new List<string>();
            foreach (var block in blocks.EnumerateArray())
            {
                if (parts.Count >= 3) break;
                if (!block.TryGetProperty("segments", out var segments)) continue;
                foreach (var seg in segments.EnumerateArray())
                {
                    if (parts.Count >= 3) break;
                    var color = seg.TryGetProperty("background", out var bg) ? bg.GetString() : seg.TryGetProperty("foreground", out var fg) ? fg.GetString() : null;
                    var type = seg.TryGetProperty("type", out var t) ? t.GetString() : "";
                    parts.Add($"{MapHexToEmoji(color)} {type}");
                }
            }
            return string.Join(" ", parts);
        }
        catch
        {
            return "";
        }
    }

    private static string MapHexToEmoji(string? hex)
    {
        var emoji = "🔵";
        if (string.IsNullOrWhiteSpace(hex)) return emoji;
        var m = Regex.Match(hex, @"^#?([0-9a-fA-F]{6})$");
        if (!m.Success) return emoji;
        var clean = m.Groups[1].Value;
        var r = Convert.ToInt32(clean[..2], 16);
        var g = Convert.ToInt32(clean.Substring(2, 2), 16);
        var b = Convert.ToInt32(clean.Substring(4, 2), 16);
        var max = Math.Max(r, Math.Max(g, b));
        var min = Math.Min(r, Math.Min(g, b));
        if (max - min < 30) emoji = max < 64 ? "⚫" : max > 192 ? "⚪" : "🔘";
        else if (r > g && r > b) emoji = (g - b) > 40 ? "🟠" : "🔴";
        else if (g > r && g > b) emoji = "🟢";
        else if (b > r && b > g) emoji = (r - g) > 40 ? "🟣" : "🔵";
        else if (r > b && g > b) emoji = Math.Abs(r - g) < 40 ? "🟡" : "🟠";
        return emoji;
    }
}
