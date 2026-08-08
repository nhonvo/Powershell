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

        return _accountDirCache.GetOrAdd(accountName, name =>
        {
            var accDir = Path.Combine(GeminiHome, "accounts", name);
            try { Directory.CreateDirectory(accDir); } catch { }
            return accDir;
        });
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
        return AppPaths.GeminiHome;
    }

    private string ResolveAccountPrefix()
    {
        var accountsDir = Path.Combine(AppPaths.GeminiHome, "accounts");
        Directory.CreateDirectory(accountsDir);
        return Path.Combine(accountsDir, "");
    }
}
