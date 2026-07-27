using AgyTui.Core.Interfaces;

namespace AgyTui.Infrastructure.Logging;

public class CommandLoggingMiddleware : ICommandRouter
{
    private readonly ICommandRouter _inner;

    public CommandLoggingMiddleware(ICommandRouter inner)
    {
        _inner = inner;
    }

    public int Execute(string alias, string[]? args = null)
    {
        var logDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".gemini", "antigravity");
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

            try
            {
                File.AppendAllText(logFile, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [END] Command: '{alias}' ExitCode: {exitCode} Elapsed: {sw.ElapsedMilliseconds}ms\n");
            }
            catch { }

            return exitCode;
        }
        catch (Exception ex)
        {
            sw.Stop();
            try
            {
                File.AppendAllText(logFile, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [FAIL] Command: '{alias}' Error: {ex.Message} Elapsed: {sw.ElapsedMilliseconds}ms\n");
            }
            catch { }
            throw;
        }
    }
}
