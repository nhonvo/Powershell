using System;
using System.IO;
using System.Text.Json;

namespace AgyTui;

public sealed class UiConfig
{
    public string Mode { get; set; } = "flat-tree";
    public string Density { get; set; } = "comfortable";
}

public sealed class AiConfig
{
    public string Mode { get; set; } = "auto";
    public string ProviderMode { get; set; } = "cloud";
    public bool EnableOllama { get; set; } = true;
    public bool EnableAgy { get; set; } = true;
}

public sealed class ProjectConfig
{
    public string BaseDir { get; set; } = "";
    public string[] SearchPaths { get; set; } = Array.Empty<string>();
    public string[] ExcludeFolders { get; set; } = Array.Empty<string>();
}

public sealed class SystemConfig
{
    public bool VerboseStartup { get; set; } = false;
    public string StartupLogFile { get; set; } = "";
    public string PoshThemesPath { get; set; } = "";
    public string AgySourceHome { get; set; } = "";
    public string GlobalBinDir { get; set; } = "";
}

public sealed class ConfigData
{
    public UiConfig Ui { get; set; } = new();
    public AiConfig Ai { get; set; } = new();
    public ProjectConfig Project { get; set; } = new();
    public SystemConfig System { get; set; } = new();

    // Flat getters and setters for backwards compatibility
    public string UiMode { get => Ui.Mode; set { if (!string.IsNullOrEmpty(value)) Ui.Mode = value; } }
    public string Density { get => Ui.Density; set { if (!string.IsNullOrEmpty(value)) Ui.Density = value; } }
    public string AiMode { get => Ai.Mode; set { if (!string.IsNullOrEmpty(value)) Ai.Mode = value; } }
    public string AiProviderMode { get => Ai.ProviderMode; set { if (!string.IsNullOrEmpty(value)) Ai.ProviderMode = value; } }
    public bool EnableAiOllama { get => Ai.EnableOllama; set => Ai.EnableOllama = value; }
    public bool EnableAgy { get => Ai.EnableAgy; set => Ai.EnableAgy = value; }
    public bool VerboseStartup { get => System.VerboseStartup; set => System.VerboseStartup = value; }
    public string StartupLogFile { get => System.StartupLogFile; set { if (!string.IsNullOrEmpty(value)) System.StartupLogFile = value; } }
    public string PoshThemesPath { get => System.PoshThemesPath; set { if (!string.IsNullOrEmpty(value)) System.PoshThemesPath = value; } }
    public string ProjectsBaseDir { get => Project.BaseDir; set { if (!string.IsNullOrEmpty(value)) Project.BaseDir = value; } }
    public string AgySourceHome { get => System.AgySourceHome; set { if (!string.IsNullOrEmpty(value)) System.AgySourceHome = value; } }
    public string GlobalBinDir { get => System.GlobalBinDir; set { if (!string.IsNullOrEmpty(value)) System.GlobalBinDir = value; } }
    public string[] ProjectSearchPaths { get => Project.SearchPaths; set { if (value != null) Project.SearchPaths = value; } }
    public string[] ProjectExcludeFolders { get => Project.ExcludeFolders; set { if (value != null) Project.ExcludeFolders = value; } }
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
            var options = new JsonSerializerOptions
            {
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true,
                PropertyNameCaseInsensitive = true
            };
            var content = File.ReadAllText(ConfigPath);
            var data = JsonSerializer.Deserialize<ConfigData>(content, options);
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
        catch { }
    }

    public static string GetUiMode() => Current.Ui.Mode;
    public static string GetDensity() => Current.Ui.Density;

    public static void SetUiMode(string uiMode)
    {
        Current.Ui.Mode = uiMode;
        Save();
    }

    public static void SetDensity(string density)
    {
        Current.Ui.Density = density;
        Save();
    }

    private static void AutoDetectDensity()
    {
        try
        {
            if (Console.WindowWidth > 0 && Console.WindowWidth < 70)
            {
                Current.Ui.Density = "compact";
                Current.Ui.Mode = "flat-tree";
            }
        }
        catch { }
    }
}
