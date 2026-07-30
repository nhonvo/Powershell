# Detailed Plan - Step 2: Path Resolution & File I/O Optimization

## 1. Problem Statement
Properties such as `AgySourceHome` and `AgyAccountPrefix` perform repeated expensive filesystem operations on every access:
- Querying `Config.Current.System.AgySourceHome`
- Calling `Environment.GetEnvironmentVariable`
- Computing `Path.GetPathRoot(Environment.SystemDirectory)`
- Invoking `Directory.CreateDirectory(...)` synchronously during property evaluation

When rendering terminal UI layouts at 60 FPS or inside Spectre.Console render loops, evaluating these property getters hundreds of times per second causes micro-stutters and high disk I/O.

---

## 2. Proposed Architecture: `IAppPathManager` Service

```csharp
namespace AgyTui.Core.Services;

public interface IAppPathManager
{
    string GeminiHome { get; }
    string AccountPrefix { get; }
    string LogsDirectory { get; }
    string AssetDirectory { get; }
    string GetAccountDirectory(string accountName);
    void InvalidateCache();
}

public class AppPathManager : IAppPathManager
{
    private string? _cachedGeminiHome;
    private string? _cachedAccountPrefix;
    private readonly ConcurrentDictionary<string, string> _accountDirCache = new(StringComparer.OrdinalIgnoreCase);

    public string GeminiHome => _cachedGeminiHome ??= ResolveGeminiHome();
    public string AccountPrefix => _cachedAccountPrefix ??= ResolveAccountPrefix();

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
        _accountDirCache.Clear();
    }
}
```

---

## 3. Implementation Checklist

- [ ] Create `IAppPathManager.cs` interface in `AgyTui.Core.Services`.
- [ ] Implement `AppPathManager.cs` with thread-safe `ConcurrentDictionary` path caching.
- [ ] Register `IAppPathManager` as a Singleton in `Bootstrapper.cs`.
- [ ] Update `AgyAccountStore` to consume `IAppPathManager` instead of raw property getters.
- [ ] Add unit tests verifying cache invalidation when accounts switch.
