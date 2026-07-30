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
