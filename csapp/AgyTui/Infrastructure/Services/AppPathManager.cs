using System.Collections.Concurrent;
using AgyTui.Infrastructure.Common;
using AgyTui.Infrastructure.Configuration;

namespace AgyTui.Infrastructure.Services;

public class AppPathManager : IAppPathManager
{
    private string? _cachedAccountPrefix;
    private string? _cachedLogsDirectory;
    private string? _cachedAssetDirectory;
    private readonly ConcurrentDictionary<string, string> _accountDirCache = new(StringComparer.OrdinalIgnoreCase);

    public string GeminiHome => ResolveGeminiHome();
    public string AccountPrefix => _cachedAccountPrefix ??= ResolveAccountPrefix();
    public string LogsDirectory => _cachedLogsDirectory ??= AppPaths.LogsDir;
    public string AssetDirectory => _cachedAssetDirectory ??= AppPaths.DataDir;

    public string GetAccountDirectory(string accountName)
    {
        if (string.Equals(accountName, "default", StringComparison.OrdinalIgnoreCase))
            return GeminiHome;

        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (!string.IsNullOrEmpty(userProfile))
        {
            var userAccDir = Path.Combine(userProfile, $".gemini_{accountName}");
            if (Directory.Exists(userAccDir))
                return userAccDir;
        }

        return _accountDirCache.GetOrAdd(accountName, name => $"{AccountPrefix}{name}");
    }

    public void InvalidateAccountCache(string accountName)
    {
        _accountDirCache.TryRemove(accountName, out _);
    }

    public void ClearAllCache()
    {
        _cachedAccountPrefix = null;
        _cachedLogsDirectory = null;
        _cachedAssetDirectory = null;
        _accountDirCache.Clear();
    }

    public void InvalidateCache() => ClearAllCache();

    private string ResolveGeminiHome()
    {
        var cfgHome = Config.Current.System.AgySourceHome;
        if (!string.IsNullOrEmpty(cfgHome)) return cfgHome;

        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (!string.IsNullOrEmpty(userProfile))
        {
            return Path.Combine(userProfile, ".gemini");
        }

        return AppPaths.GeminiHome;
    }

    private string ResolveAccountPrefix()
    {
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (!string.IsNullOrEmpty(userProfile))
        {
            return Path.Combine(userProfile, ".gemini_");
        }

        var publicDir = Path.Combine(Path.GetPathRoot(Environment.SystemDirectory) ?? @"C:\", "Users", "Public");
        if (GeminiHome.StartsWith(publicDir, StringComparison.OrdinalIgnoreCase))
        {
            return Path.Combine(publicDir, ".gemini_");
        }

        var accountsDir = Path.Combine(AppPaths.DataDir, ".gemini_");
        Directory.CreateDirectory(Path.GetDirectoryName(accountsDir)!);
        return accountsDir;
    }
}
