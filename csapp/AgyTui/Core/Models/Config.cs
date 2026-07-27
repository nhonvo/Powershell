namespace AgyTui.Core.Models;

using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AgyTui.Infrastructure.Common;

public sealed class UiConfig
{
    public string Mode { get; set; } = "flat-tree";
    public string Density { get; set; } = "comfortable";
    public string ActiveTheme { get; set; } = "neko";
    public bool EnableMobile { get; set; } = false;
    public string[] FavoriteAliases { get; set; } = Config.DefaultFavoriteAliases;
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

public sealed class ObsidianConfig
{
    public string VaultPath { get; set; } = "";
}

public sealed class ProxyConfig
{
    public string HttpProxy { get; set; } = "";
    public string HttpsProxy { get; set; } = "";
    public string NoProxy { get; set; } = "";
}

public sealed class ConfigData
{
    public UiConfig Ui { get; set; } = new();
    public AiConfig Ai { get; set; } = new();
    public ProjectConfig Project { get; set; } = new();
    public SystemConfig System { get; set; } = new();
    public ObsidianConfig Obsidian { get; set; } = new();
    public ProxyConfig Proxy { get; set; } = new();

    // Flat getters and setters for backwards compatibility adapter pattern
    [JsonIgnore]
    public string UiMode { get => Ui.Mode; set { if (!string.IsNullOrEmpty(value)) Ui.Mode = value; } }

    [JsonIgnore]
    public string Density { get => Ui.Density; set { if (!string.IsNullOrEmpty(value)) Ui.Density = value; } }

    [JsonIgnore]
    public string AiMode { get => Ai.Mode; set { if (!string.IsNullOrEmpty(value)) Ai.Mode = value; } }

    [JsonIgnore]
    public string AiProviderMode { get => Ai.ProviderMode; set { if (!string.IsNullOrEmpty(value)) Ai.ProviderMode = value; } }

    [JsonIgnore]
    public bool EnableAiOllama { get => Ai.EnableOllama; set => Ai.EnableOllama = value; }

    [JsonIgnore]
    public bool EnableAgy { get => Ai.EnableAgy; set => Ai.EnableAgy = value; }

    [JsonIgnore]
    public bool VerboseStartup { get => System.VerboseStartup; set => System.VerboseStartup = value; }

    [JsonIgnore]
    public string StartupLogFile { get => System.StartupLogFile; set { if (!string.IsNullOrEmpty(value)) System.StartupLogFile = value; } }

    [JsonIgnore]
    public string PoshThemesPath { get => System.PoshThemesPath; set { if (!string.IsNullOrEmpty(value)) System.PoshThemesPath = value; } }

    [JsonIgnore]
    public string ProjectsBaseDir { get => Project.BaseDir; set { if (!string.IsNullOrEmpty(value)) Project.BaseDir = value; } }

    [JsonIgnore]
    public string AgySourceHome { get => System.AgySourceHome; set { if (!string.IsNullOrEmpty(value)) System.AgySourceHome = value; } }

    [JsonIgnore]
    public string GlobalBinDir { get => System.GlobalBinDir; set { if (!string.IsNullOrEmpty(value)) System.GlobalBinDir = value; } }

    [JsonIgnore]
    public string HttpProxy { get => Proxy.HttpProxy; set { if (value != null) Proxy.HttpProxy = value; } }

    [JsonIgnore]
    public string HttpsProxy { get => Proxy.HttpsProxy; set { if (value != null) Proxy.HttpsProxy = value; } }

    [JsonIgnore]
    public string NoProxy { get => Proxy.NoProxy; set { if (value != null) Proxy.NoProxy = value; } }

    [JsonIgnore]
    public string[] ProjectSearchPaths { get => Project.SearchPaths; set { if (value != null) Project.SearchPaths = value; } }

    [JsonIgnore]
    public string[] ProjectExcludeFolders { get => Project.ExcludeFolders; set { if (value != null) Project.ExcludeFolders = value; } }
}

public sealed class RuntimeState
{
    public string? ActiveThemeOverride { get; set; }
    public bool? MobileContextOverride { get; set; }
    public string? RuntimeDensity { get; set; }
    public DateTime SessionStartTime { get; } = DateTime.UtcNow;
}

public static class Config
{
    public static readonly string[] DefaultFavoriteAliases = ["proj", "agyswitch", "ide", "claude", "theme", "learn", "obsidian", "ssh-info"];

    public static string? OverrideConfigPath { get; set; }
    public static ConfigData Current { get; private set; } = new();
    public static RuntimeState Runtime { get; } = new();

    private static string ConfigPath => GetConfigFilePath();

    static Config()
    {
        bool fileExists = File.Exists(ConfigPath);
        Load();
        if (!fileExists)
        {
            AutoDetectDensity();
        }
    }

    public static string GetConfigFilePath()
    {
        if (!string.IsNullOrEmpty(OverrideConfigPath))
            return OverrideConfigPath;
        return AppPaths.ConfigFile;
    }

    public static string GetProfileRepoRoot() => AppPaths.RepoRoot;

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
            var dir = Path.GetDirectoryName(ConfigPath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(ConfigPath, content, Encoding.UTF8);
        }
        catch { }
    }

    public static string GetUiMode() => Current.Ui.Mode;
    public static string GetDensity() => Runtime.RuntimeDensity ?? Current.Ui.Density;

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

    public static bool IsMobileContext()
    {
        if (Runtime.MobileContextOverride.HasValue)
            return Runtime.MobileContextOverride.Value;

        try
        {
            if (string.Equals(GetDensity(), "compact", StringComparison.OrdinalIgnoreCase)) return true;
            if (Console.WindowWidth > 0 && Console.WindowWidth < 90) return true;
            var theme = Environment.GetEnvironmentVariable("THEME") ?? "";
            if (theme.EndsWith("-mobile", StringComparison.OrdinalIgnoreCase)) return true;
        }
        catch { }
        return false;
    }
}
