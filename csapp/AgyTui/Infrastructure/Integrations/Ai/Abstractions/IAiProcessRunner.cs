namespace AgyTui.Infrastructure.Integrations.Ai.Abstractions;

public interface IAiProcessRunner
{
    string ResolveProxyScriptPath();
    void RunInteractive(string exe, IEnumerable<string> args, IDictionary<string, string?>? env = null, string? workingDir = null);
    string RunCapture(string exe, string args);
}
