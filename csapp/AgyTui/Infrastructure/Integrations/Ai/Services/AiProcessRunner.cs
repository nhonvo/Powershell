using AgyTui.Infrastructure.Di;
using AgyTui.Infrastructure.Integrations.AgyClient.Interfaces;
using AgyTui.Infrastructure.Integrations.Ai.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace AgyTui.Infrastructure.Integrations.Ai.Services;

public class AiProcessRunner : IAiProcessRunner
{
    private readonly Func<IAgyAccountStore> _accountStoreFactory;

    public AiProcessRunner(Func<IAgyAccountStore>? accountStoreFactory = null)
    {
        _accountStoreFactory = accountStoreFactory ?? (() => Bootstrapper.ServiceProvider.GetRequiredService<IAgyAccountStore>());
    }

    public string ResolveProxyScriptPath()
    {
        var repoRoot = Config.GetProfileRepoRoot();
        var candidates = new[]
        {
            Path.Combine(repoRoot, "psapp", "script", "Start-ClaudeProxy.ps1"),
            Path.Combine(repoRoot, "script", "Start-ClaudeProxy.ps1"),
            Path.Combine(AppPaths.GeminiHome, "antigravity", "script", "Start-ClaudeProxy.ps1")
        };
        return candidates.FirstOrDefault(File.Exists) ?? candidates[0];
    }

    public void RunInteractive(string exe, IEnumerable<string> args, IDictionary<string, string?>? env = null, string? workingDir = null)
    {
        var store = _accountStoreFactory();
        var activeAccount = store.GetActiveAccount();
        var accountDir = store.GetAccountDirectory(activeAccount);
        var fullEnv = env != null ? new Dictionary<string, string?>(env) : new Dictionary<string, string?>();
        if (!fullEnv.ContainsKey("GEMINI_HOME"))
        {
            fullEnv["GEMINI_HOME"] = accountDir;
        }
        if (store.IsNoAutoCommitEnabled() && !fullEnv.ContainsKey("AGY_AUTO_COMMIT"))
        {
            fullEnv["AGY_AUTO_COMMIT"] = "false";
        }

        var httpProxy = Config.Current.Proxy?.HttpProxy;
        if (string.IsNullOrEmpty(httpProxy)) httpProxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
        if (!string.IsNullOrEmpty(httpProxy) && !fullEnv.ContainsKey("HTTP_PROXY"))
        {
            fullEnv["HTTP_PROXY"] = httpProxy;
        }

        var httpsProxy = Config.Current.Proxy?.HttpsProxy;
        if (string.IsNullOrEmpty(httpsProxy)) httpsProxy = Environment.GetEnvironmentVariable("HTTPS_PROXY");
        if (!string.IsNullOrEmpty(httpsProxy) && !fullEnv.ContainsKey("HTTPS_PROXY"))
        {
            fullEnv["HTTPS_PROXY"] = httpsProxy;
        }

        var noProxy = Config.Current.Proxy?.NoProxy;
        if (string.IsNullOrEmpty(noProxy)) noProxy = Environment.GetEnvironmentVariable("NO_PROXY");
        if (!string.IsNullOrEmpty(noProxy) && !fullEnv.ContainsKey("NO_PROXY"))
        {
            fullEnv["NO_PROXY"] = noProxy;
        }

        var argList = new List<string>(args);
        bool targetsClaudeOrCodexOrAgy = exe.Contains("agy") || exe.Contains("claude") || exe.Contains("codex") || args.Any(a => a is "claude" or "codex" or "agy" || a.Contains("claude", StringComparison.OrdinalIgnoreCase));
        if (store.IsNoAutoCommitEnabled() && targetsClaudeOrCodexOrAgy)
        {
            if (!argList.Contains("--no-auto-commit") && !argList.Contains("--no-commit"))
            {
                argList.Add("--no-auto-commit");
            }
        }
        Helpers.ProcessRunner.RunInteractive(exe, argList, fullEnv, workingDir);
    }

    public static void RunInteractiveStatic(string exe, IEnumerable<string> args, IDictionary<string, string?>? env = null, string? workingDir = null)
    {
        new AiProcessRunner().RunInteractive(exe, args, env, workingDir);
    }

    public string RunCapture(string exe, string args)
    {
        return Helpers.ProcessRunner.RunCapture(exe, args);
    }
}
