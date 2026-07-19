using System;
using System.IO;
using System.Text.Json;

namespace AgyTui;

public sealed class ConfigData
{
    public string AiMode { get; set; } = "auto";
    public string AiProviderMode { get; set; } = "cloud";
    public bool VerboseStartup { get; set; } = false;
    public string StartupLogFile { get; set; } = "";
    public string PoshThemesPath { get; set; } = "";
    public string ProjectsBaseDir { get; set; } = "";
    public string AgySourceHome { get; set; } = "";
    public string GlobalBinDir { get; set; } = "";
    public bool EnableAiOllama { get; set; } = true;
    public bool EnableAgy { get; set; } = true;
    public string[] ProjectSearchPaths { get; set; } = Array.Empty<string>();
    public string[] ProjectExcludeFolders { get; set; } = Array.Empty<string>();
    
    // New settings
    public string UiMode { get; set; } = "flat-tree";
    public string Density { get; set; } = "comfortable";
}

public static class Config
{
    private static readonly string ConfigPath = Path.Combine(GetProfileRepoRoot(), "profile.config.json");
    public static ConfigData Current { get; private set; } = new();

    static Config()
    {
        Load();
        AutoDetectDensity();
    }

    public static string GetProfileRepoRoot()
    {
        var asmPath = typeof(Config).Assembly.Location;
        if (string.IsNullOrEmpty(asmPath)) return Directory.GetCurrentDirectory();
        var asmDir = Path.GetDirectoryName(asmPath);
        if (string.IsNullOrEmpty(asmDir)) return Directory.GetCurrentDirectory();
        var parent = Path.GetDirectoryName(asmDir);
        if (parent == null) return asmDir;
        var grandParent = Path.GetDirectoryName(parent);
        return grandParent ?? parent;
    }

    public static void Load()
    {
        if (!File.Exists(ConfigPath))
        {
            Current = new ConfigData();
            return;
        }

        try
        {
            var content = File.ReadAllText(ConfigPath);
            var data = JsonSerializer.Deserialize<ConfigData>(content);
            if (data != null)
            {
                Current = data;
            }
        }
        catch
        {
            Current = new ConfigData();
        }
    }

    public static void Save()
    {
        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var content = JsonSerializer.Serialize(Current, options);
            File.WriteAllText(ConfigPath, content);
        }
        catch {}
    }

    public static void SetUiMode(string uiMode)
    {
        Current.UiMode = uiMode;
        Save();
    }

    public static void SetDensity(string density)
    {
        Current.Density = density;
        Save();
    }

    private static void AutoDetectDensity()
    {
        try
        {
            // Auto-detect based on Console.WindowWidth
            if (Console.WindowWidth > 0 && Console.WindowWidth < 70)
            {
                Current.Density = "compact";
                Current.UiMode = "flat-tree";
            }
        }
        catch {}
    }
}
