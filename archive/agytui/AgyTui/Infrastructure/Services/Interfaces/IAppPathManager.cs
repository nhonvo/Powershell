namespace AgyTui.Infrastructure.Services;

public interface IAppPathManager
{
    string GeminiHome { get; }
    string AccountPrefix { get; }
    string LogsDirectory { get; }
    string AssetDirectory { get; }
    string GetAccountDirectory(string accountName);
    void InvalidateAccountCache(string accountName);
    void ClearAllCache();
}
