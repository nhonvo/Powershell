using AgyTui.Domain.AiAgentContext;

namespace AgyTui.Infrastructure.Logging;

public class CommandLoggingMiddleware : ICommandRouter
{
    private readonly ICommandRouter _inner;
    private readonly IErrorLogger _errorLogger;

    public CommandLoggingMiddleware(ICommandRouter inner, IErrorLogger errorLogger)
    {
        _inner = inner;
        _errorLogger = errorLogger;
    }

    public CommandLoggingMiddleware(ICommandRouter inner) : this(inner, new FileErrorLogger()) { }

    public int Execute(string alias, string[]? args = null)
    {
        var logDir = AppPaths.LogsDir;
        var logFile = Path.Combine(logDir, "tui_execution.log");

        try
        {
            Directory.CreateDirectory(logDir);
        }
        catch { }

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var startTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var argStr = args != null && args.Length > 0 ? string.Join(" ", args) : "none";

        try
        {
            File.AppendAllText(logFile, $"[{startTime}] [START] Command: '{alias}' Args: {argStr}\n");
        }
        catch { }

        try
        {
            int exitCode = _inner.Execute(alias, args);
            sw.Stop();

            var agentLog = new AgentInvocationLog(alias, sw.ElapsedMilliseconds, exitCode == 0, "default", ProviderMode.Auto);

            try
            {
                File.AppendAllText(logFile, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [END] InvocationId: {agentLog.Id} Command: '{agentLog.Alias}' ExitCode: {exitCode} Elapsed: {agentLog.DurationMs}ms\n");
            }
            catch { }

            return exitCode;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _errorLogger.LogError(ex, $"CommandRouter.Execute({alias})");
            try
            {
                File.AppendAllText(logFile, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [FAIL] Command: '{alias}' Error: {ex.Message} Elapsed: {sw.ElapsedMilliseconds}ms\n");
            }
            catch { }
            throw;
        }
    }
}
