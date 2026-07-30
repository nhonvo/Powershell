using System.Collections.Concurrent;
using AgyTui.Core.Models;
using AgyTui.Infrastructure.Common;

namespace AgyTui.Core.Services;

public class AppPathManager : IAppPathManager
{
    private string? _cachedGeminiHome;
    private string? _cachedAccountPrefix;
    private string? _cachedLogsDirectory;
    private string? _cachedAssetDirectory;
    private readonly ConcurrentDictionary<string, string> _accountDirCache = new(StringComparer.OrdinalIgnoreCase);

    public string GeminiHome => _cachedGeminiHome ??= ResolveGeminiHome();
    public string AccountPrefix => _cachedAccountPrefix ??= ResolveAccountPrefix();
    public string LogsDirectory => _cachedLogsDirectory ??= AppPaths.LogsDir;
    public string AssetDirectory => _cachedAssetDirectory ??= AppPaths.DataDir;

    public string GetAccountDirectory(string accountName)
    {
        if (string.Equals(accountName, "default", StringComparison.OrdinalIgnoreCase))
            return GeminiHome;

        return _accountDirCache.GetOrAdd(accountName, name => $"{AccountPrefix}{name}");
    }

    private string ResolveGeminiHome()
    {
        var cfgHome = Config.Current.System.AgySourceHome;
        if (!string.IsNullOrEmpty(cfgHome)) return cfgHome;

        var envGemini = Environment.GetEnvironmentVariable("GEMINI_HOME");
        if (!string.IsNullOrEmpty(envGemini) && Directory.Exists(envGemini))
            return envGemini;

        return AppPaths.GeminiHome;
    }

    private string ResolveAccountPrefix()
    {
        var publicDir = Path.Combine(Path.GetPathRoot(Environment.SystemDirectory) ?? @"C:\", "Users", "Public");
        if (GeminiHome.StartsWith(publicDir, StringComparison.OrdinalIgnoreCase))
        {
            return Path.Combine(publicDir, ".gemini_");
        }

        var accountsDir = Path.Combine(AppPaths.DataDir, ".gemini_");
        Directory.CreateDirectory(Path.GetDirectoryName(accountsDir)!);
        return accountsDir;
    }

    public void InvalidateCache()
    {
        _cachedGeminiHome = null;
        _cachedAccountPrefix = null;
        _cachedLogsDirectory = null;
        _cachedAssetDirectory = null;
        _accountDirCache.Clear();
    }
}
