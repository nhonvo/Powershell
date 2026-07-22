using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

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
    public string[] ProjectSearchPaths { get => Project.SearchPaths; set { if (value != null) Project.SearchPaths = value; } }

    [JsonIgnore]
    public string[] ProjectExcludeFolders { get => Project.ExcludeFolders; set { if (value != null) Project.ExcludeFolders = value; } }
}

public static class Config
{
    public static string? OverrideConfigPath { get; set; }

    public static string GetConfigFilePath()
    {
        if (!string.IsNullOrEmpty(OverrideConfigPath))
            return OverrideConfigPath;
        var envOverride = Environment.GetEnvironmentVariable("PROFILE_CONFIG_PATH");
        if (!string.IsNullOrEmpty(envOverride))
            return envOverride;
        var repoRoot = GetProfileRepoRoot();
        var csappCfg = Path.Combine(repoRoot, "csapp", "profile.config.json");
        if (File.Exists(csappCfg)) return csappCfg;
        return Path.Combine(repoRoot, "profile.config.json");
    }

    private static string ConfigPath => GetConfigFilePath();
    public static ConfigData Current { get; private set; } = new();

    static Config()
    {
        bool fileExists = File.Exists(ConfigPath);
        Load();
        if (!fileExists)
        {
            AutoDetectDensity();
        }
    }

    public static string GetProfileRepoRoot()
    {
        var envRoot = Environment.GetEnvironmentVariable("PROFILE_REPO_ROOT");
        if (!string.IsNullOrEmpty(envRoot) && File.Exists(Path.Combine(envRoot, "csapp", "profile.config.json")))
            return envRoot;

        var startDir = Directory.GetCurrentDirectory();
        try
        {
            var asmPath = typeof(Config).Assembly.Location;
            if (!string.IsNullOrEmpty(asmPath))
            {
                var dir = Path.GetDirectoryName(asmPath);
                if (!string.IsNullOrEmpty(dir)) startDir = dir;
            }
        }
        catch { }

        var curr = new DirectoryInfo(startDir);
        while (curr != null)
        {
            if (File.Exists(Path.Combine(curr.FullName, "csapp", "profile.config.json")) || File.Exists(Path.Combine(curr.FullName, "profile.config.json")))
                return curr.FullName;
            curr = curr.Parent;
        }

        return Directory.GetCurrentDirectory();
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
            if (File.Exists(ConfigPath))
            {
                var lines = File.ReadAllLines(ConfigPath);
                string currentSection = "";
                for (int i = 0; i < lines.Length; i++)
                {
                    var line = lines[i];
                    var trimmed = line.Trim();
                    if (trimmed.StartsWith("\"Ui\"")) currentSection = "Ui";
                    else if (trimmed.StartsWith("\"Ai\"")) currentSection = "Ai";
                    else if (trimmed.StartsWith("\"Project\"")) currentSection = "Project";
                    else if (trimmed.StartsWith("\"System\"")) currentSection = "System";
                    else if (trimmed.StartsWith("}") && !trimmed.Contains("{")) currentSection = "";

                    if (currentSection == "Ui" && line.Contains("\"Mode\":"))
                    {
                        lines[i] = Regex.Replace(line, @"(""Mode""\s*:\s*"")[^""]*("")", $"$1{Current.Ui.Mode}$2");
                    }
                    else if (currentSection == "Ui" && line.Contains("\"Density\":"))
                    {
                        lines[i] = Regex.Replace(line, @"(""Density""\s*:\s*"")[^""]*("")", $"$1{Current.Ui.Density}$2");
                    }
                    else if (currentSection == "Ai" && line.Contains("\"Mode\":"))
                    {
                        lines[i] = Regex.Replace(line, @"(""Mode""\s*:\s*"")[^""]*("")", $"$1{Current.Ai.Mode}$2");
                    }
                    else if (currentSection == "Ai" && line.Contains("\"ProviderMode\":"))
                    {
                        lines[i] = Regex.Replace(line, @"(""ProviderMode""\s*:\s*"")[^""]*("")", $"$1{Current.Ai.ProviderMode}$2");
                    }
                    else if (currentSection == "Ai" && line.Contains("\"EnableOllama\":"))
                    {
                        lines[i] = Regex.Replace(line, @"(""EnableOllama""\s*:\s*)(true|false)", $"$1{Current.Ai.EnableOllama.ToString().ToLowerInvariant()}");
                    }
                    else if (currentSection == "Ai" && line.Contains("\"EnableAgy\":"))
                    {
                        lines[i] = Regex.Replace(line, @"(""EnableAgy""\s*:\s*)(true|false)", $"$1{Current.Ai.EnableAgy.ToString().ToLowerInvariant()}");
                    }
                    else if (currentSection == "Project" && line.Contains("\"BaseDir\":"))
                    {
                        lines[i] = Regex.Replace(line, @"(""BaseDir""\s*:\s*"")[^""]*("")", $"$1{Current.Project.BaseDir.Replace("\\", "\\\\")}$2");
                    }
                    else if (currentSection == "Project" && line.Contains("\"SearchPaths\":"))
                    {
                        var jsonArr = JsonSerializer.Serialize(Current.Project.SearchPaths);
                        lines[i] = Regex.Replace(line, @"(""SearchPaths""\s*:\s*)\[[^\]]*\]", $"$1{jsonArr}");
                    }
                    else if (currentSection == "Project" && line.Contains("\"ExcludeFolders\":"))
                    {
                        var jsonArr = JsonSerializer.Serialize(Current.Project.ExcludeFolders);
                        lines[i] = Regex.Replace(line, @"(""ExcludeFolders""\s*:\s*)\[[^\]]*\]", $"$1{jsonArr}");
                    }
                    else if (currentSection == "System" && line.Contains("\"VerboseStartup\":"))
                    {
                        lines[i] = Regex.Replace(line, @"(""VerboseStartup""\s*:\s*)(true|false)", $"$1{Current.System.VerboseStartup.ToString().ToLowerInvariant()}");
                    }
                    else if (currentSection == "System" && line.Contains("\"StartupLogFile\":"))
                    {
                        lines[i] = Regex.Replace(line, @"(""StartupLogFile""\s*:\s*"")[^""]*("")", $"$1{Current.System.StartupLogFile.Replace("\\", "\\\\")}$2");
                    }
                    else if (currentSection == "System" && line.Contains("\"PoshThemesPath\":"))
                    {
                        lines[i] = Regex.Replace(line, @"(""PoshThemesPath""\s*:\s*"")[^""]*("")", $"$1{Current.System.PoshThemesPath.Replace("\\", "\\\\")}$2");
                    }
                    else if (currentSection == "System" && line.Contains("\"AgySourceHome\":"))
                    {
                        lines[i] = Regex.Replace(line, @"(""AgySourceHome""\s*:\s*"")[^""]*("")", $"$1{Current.System.AgySourceHome.Replace("\\", "\\\\")}$2");
                    }
                    else if (currentSection == "System" && line.Contains("\"GlobalBinDir\":"))
                    {
                        lines[i] = Regex.Replace(line, @"(""GlobalBinDir""\s*:\s*"")[^""]*("")", $"$1{Current.System.GlobalBinDir.Replace("\\", "\\\\")}$2");
                    }
                }
                File.WriteAllLines(ConfigPath, lines, Encoding.UTF8);
                return;
            }

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

    public static bool IsMobileContext()
    {
        try
        {
            if (string.Equals(Current.Ui.Density, "compact", StringComparison.OrdinalIgnoreCase)) return true;
            if (Console.WindowWidth > 0 && Console.WindowWidth < 90) return true;
            var theme = Environment.GetEnvironmentVariable("THEME") ?? "";
            if (theme.EndsWith("-mobile", StringComparison.OrdinalIgnoreCase)) return true;
        }
        catch { }
        return false;
    }
}
