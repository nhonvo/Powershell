namespace AgyTui.Core.Models;

using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AgyTui.Infrastructure.Common;
using AgyTui.Infrastructure.Di;
using AgyTui.Infrastructure.Persistence;
using AgyTui.Infrastructure.Persistence.Interfaces;
using Microsoft.Extensions.DependencyInjection;

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

}

public sealed class RuntimeState
{
    public bool? MobileContextOverride { get; set; }
    public string? RuntimeDensity { get; set; }
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

    private static IConfigRepository Repository => Bootstrapper.ServiceProvider.GetRequiredService<IConfigRepository>();

    public static void Load()
    {
        try
        {
            Current = Repository.LoadConfig();
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
            Repository.SaveConfig(Current);
        }
        catch { }
    }

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
