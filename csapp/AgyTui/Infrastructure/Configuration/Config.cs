using AgyTui.Infrastructure.Di;
using Microsoft.Extensions.DependencyInjection;

namespace AgyTui.Infrastructure.Configuration;

public sealed class UiConfig
{
    public string Mode { get; set; } = "flat-tree";
    public string Density { get; set; } = "comfortable";
    public string ActiveTheme { get; set; } = "neko";
    public bool EnableMobile { get; set; }
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

public sealed class EnvironmentConfig
{
    public string PoshThemesPath { get; set; } = "psapp/asset/powershell-themes";
    public string PsModulePath { get; set; } = "psapp/Modules";
    public bool EnableFastStartup { get; set; } = false;
    public bool ForceLoadRedirected { get; set; } = false;
    public string Theme { get; set; } = "neko";
}

public sealed class ConfigData
{
    public UiConfig Ui { get; set; } = new();
    public AiConfig Ai { get; set; } = new();
    public ProjectConfig Project { get; set; } = new();
    public SystemConfig System { get; set; } = new();
    public ObsidianConfig Obsidian { get; set; } = new();
    public ProxyConfig Proxy { get; set; } = new();
    public EnvironmentConfig Environment { get; set; } = new();
}

public sealed class RuntimeState
{
    public bool? MobileContextOverride { get; set; }
    public string? RuntimeDensity { get; set; }
}

public static class Config
{
    public static readonly string[] DefaultFavoriteAliases = ["proj", "agyswitch", "open-term", "vault", "ide", "ask-ai"];

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

    private static IConfigRepository? _repository;
    public static IConfigRepository Repository
    {
        get => _repository ??= new SqliteConfigRepository(new SqliteDatabase());
        set => _repository = value;
    }

    private static readonly object _configLock = new();

    public static void Load()
    {
        lock (_configLock)
        {
            try
            {
                Current = Repository.LoadConfig();
                if (Current.Ui.FavoriteAliases == null)
                {
                    Current.Ui.FavoriteAliases = DefaultFavoriteAliases;
                }
            }
            catch (Exception ex)
            {
                LogHelper.Log($"[Config] Load fallback: {ex.Message}", "DEBUG");
                Current = new ConfigData();
                Current.Ui.FavoriteAliases = DefaultFavoriteAliases;
            }
        }
    }

    public static void Save()
    {
        lock (_configLock)
        {
            try
            {
                Repository.SaveConfig(Current);
            }
            catch (Exception ex)
            {
                LogHelper.LogError("Config Save failed", ex);
            }
        }
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

    public static void SetRuntimeDensity(string? density)
    {
        Runtime.RuntimeDensity = density;
    }

    public static void SetMobileOverride(bool? overrideValue)
    {
        Runtime.MobileContextOverride = overrideValue;
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
        catch (Exception ex)
        {
            LogHelper.Log($"[Config] AutoDetectDensity non-fatal: {ex.Message}", "DEBUG");
        }
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
        catch (Exception ex)
        {
            LogHelper.Log($"[Config] IsMobileContext non-fatal: {ex.Message}", "DEBUG");
        }
        return false;
    }
}
