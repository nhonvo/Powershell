namespace AgyTui.Infrastructure.Logging;

public class FileErrorLogger : IErrorLogger
{
    private readonly string _logPath;

    public FileErrorLogger()
    {
        _logPath = Path.Combine(AppPaths.LogsDir, "error.log");
    }

    public void LogError(Exception ex, string context)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_logPath)!);
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var content = $"[{timestamp}] [ERROR] Context: {context}\nType: {ex.GetType().FullName}\nMessage: {ex.Message}\nStackTrace:\n{ex.StackTrace}\n----------------------------------------\n";
            File.AppendAllText(_logPath, content);
        }
        catch { }
    }
}
