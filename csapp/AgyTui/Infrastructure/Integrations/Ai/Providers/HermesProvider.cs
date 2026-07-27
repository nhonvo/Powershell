using AgyTui.Infrastructure.Integrations.Ai.Services;

namespace AgyTui.Infrastructure.Integrations.Ai.Providers;

public enum HermesResult
{
    Success,
    NotInstalled,
    Error
}

public static class HermesProvider
{
    public static string? FindOnPath(string exe)
    {
        var pathEnv = Environment.GetEnvironmentVariable("PATH") ?? "";
        foreach (var dir in pathEnv.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            try
            {
                var full = Path.Combine(dir.Trim(), exe);
                if (File.Exists(full)) return full;
                if (!exe.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) && File.Exists(full + ".exe")) return full + ".exe";
                if (!exe.EndsWith(".cmd", StringComparison.OrdinalIgnoreCase) && File.Exists(full + ".cmd")) return full + ".cmd";
            }
            catch { }
        }
        return null;
    }

    private static string? FindHermesBinary(string exeNameOnPath, string[] localPaths)
    {
        var found = FindOnPath(exeNameOnPath);
        if (found != null) return found;

        foreach (var p in localPaths)
        {
            if (File.Exists(p)) return p;
        }
        return null;
    }

    public static HermesResult InvokeHermes(string[]? argsList = null)
    {
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var exe = FindHermesBinary("hermes", new[] {
            Path.Combine(userProfile, ".cargo", "bin", "hermes.exe"),
            Path.Combine(userProfile, "AppData", "Local", "Programs", "hermes", "hermes.exe")
        });

        if (exe == null) return HermesResult.NotInstalled;

        try
        {
            var args = argsList != null && argsList.Length > 0 ? argsList : new[] { "chat" };
            AiProcessRunner.RunInteractive(exe, args);
            return HermesResult.Success;
        }
        catch
        {
            return HermesResult.Error;
        }
    }

    public static HermesResult InvokeHermesDesktop(string[]? argsList = null)
    {
        return InvokeHermes(argsList);
    }
}
