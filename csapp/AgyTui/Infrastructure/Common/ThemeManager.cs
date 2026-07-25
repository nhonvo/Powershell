using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace AgyTui.Infrastructure.Common;

public static class ThemeManager
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
        var themePath = SetTheme(themesPath, selectedTheme);
        if (themePath == null) return null;
        AnsiConsole.MarkupLine($"[green][[Theme]] Oh My Posh theme switched to '{selectedTheme}' (Persistent).[/]");
        return themePath;
    }

    public static string? SetTheme(string themesPath, string selectedTheme)
    {
        PersistConfig(themesPath, selectedTheme, selectedTheme.EndsWith("-mobile"));
        Environment.SetEnvironmentVariable("THEME", selectedTheme);
        var themePath = Path.Combine(themesPath, $"{selectedTheme}.omp.json");
        return File.Exists(themePath) ? themePath : null;
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
        var themeName = string.IsNullOrWhiteSpace(Config.Current.Ui.ActiveTheme) ? "neko" : Config.Current.Ui.ActiveTheme;
        var isMobile = Config.Current.Ui.EnableMobile;
        return (themeName, isMobile);
    }

    public static string ResolveStartupTheme(string themesPath)
    {
        var theme = ReadConfig(themesPath).ThemeName;
        var legacyFile = Path.Combine(Config.GetProfileRepoRoot(), "active_theme.txt");
        if (File.Exists(legacyFile))
        {
            theme = File.ReadAllText(legacyFile).Trim();
            try
            {
                File.Delete(legacyFile);
            }
            catch
            {
            }
            PersistConfig(themesPath, theme, theme.EndsWith("-mobile"));
        }
        return theme;
    }

    private static void PersistConfig(string themesPath, string themeName, bool enableMobile)
    {
        try
        {
            Config.Current.Ui.ActiveTheme = themeName;
            Config.Current.Ui.EnableMobile = enableMobile;
            Config.Save();
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

public static class AgyThemeColors
{
    public static string Accent { get; private set; } = "cyan";
    public static string Secondary { get; private set; } = "yellow";
    public static string Selected { get; private set; } = "green";
    public static string Border { get; private set; } = "grey";

    public static Color GetAccentColor() => Accent.StartsWith('#') ? Color.FromHex(Accent) : Color.Cyan;
    public static Color GetSecondaryColor() => Secondary.StartsWith('#') ? Color.FromHex(Secondary) : Color.Yellow;
    public static Color GetSelectedColor() => Selected.StartsWith('#') ? Color.FromHex(Selected) : Color.Green;
    public static Color GetBorderColor() => Border.StartsWith('#') ? Color.FromHex(Border) : Color.Grey;

    static AgyThemeColors()
    {
        Initialize();
    }

    public static void Initialize()
    {
        try
        {
            var repoRoot = Config.GetProfileRepoRoot();
            var themesPath = Path.Combine(repoRoot, "psapp", "asset", "powershell-themes");
            if (!Directory.Exists(themesPath))
            {
                themesPath = Path.Combine(repoRoot, "asset", "powershell-themes");
            }

            var themeName = ThemeManager.ResolveStartupTheme(themesPath);
            var themePath = Path.Combine(themesPath, $"{themeName}.omp.json");
            if (!File.Exists(themePath)) return;

            using var doc = JsonDocument.Parse(File.ReadAllText(themePath));
            if (!doc.RootElement.TryGetProperty("blocks", out var blocks)) return;

            var colors = new List<string>();
            foreach (var block in blocks.EnumerateArray())
            {
                if (!block.TryGetProperty("segments", out var segments)) continue;
                foreach (var seg in segments.EnumerateArray())
                {
                    if (seg.TryGetProperty("foreground", out var fgProp))
                    {
                        var fg = fgProp.GetString();
                        if (!string.IsNullOrEmpty(fg) && fg.StartsWith('#')) colors.Add(fg);
                    }
                    if (seg.TryGetProperty("background", out var bgProp))
                    {
                        var bg = bgProp.GetString();
                        if (!string.IsNullOrEmpty(bg) && bg.StartsWith('#')) colors.Add(bg);
                    }
                }
            }

            var filteredColors = colors
                .Distinct()
                .Where(c => IsGoodColor(c))
                .ToList();

            if (filteredColors.Count > 0)
            {
                Accent = filteredColors[0];
            }
            if (filteredColors.Count > 1)
            {
                Secondary = filteredColors[1];
            }
        }
        catch
        {
            // Fallback to default colors
        }
    }

    private static bool IsGoodColor(string hex)
    {
        try
        {
            var m = Regex.Match(hex, @"^#?([0-9a-fA-F]{6})$");
            if (!m.Success) return false;
            var clean = m.Groups[1].Value;
            var r = Convert.ToInt32(clean[..2], 16);
            var g = Convert.ToInt32(clean.Substring(2, 2), 16);
            var b = Convert.ToInt32(clean.Substring(4, 2), 16);

            var max = Math.Max(r, Math.Max(g, b));
            var min = Math.Min(r, Math.Min(g, b));

            if (max < 60) return false;
            if (max - min < 25) return false;

            return true;
        }
        catch
        {
            return false;
        }
    }
}
