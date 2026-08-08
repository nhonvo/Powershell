namespace AgyTui.Infrastructure.Logging;

public interface IErrorLogger
{
    void LogError(Exception ex, string context);
}
