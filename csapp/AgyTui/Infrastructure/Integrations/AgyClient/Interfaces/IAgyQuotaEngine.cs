namespace AgyTui.Infrastructure.Integrations.AgyClient.Interfaces;

public interface IAgyQuotaEngine
{
    QuotaMetrics CalculateRollingQuotas(string accountName);
    QuotaMetrics CalculateRollingQuotasForAgent(string agentName);
    List<(DateTime Time, int ReqsReleased, double QuotaGained)> GetQuotaReleaseForecast(string accountName);
    void TriggerLowQuotaWebhook(string accountName, double remaining5H);
    long GetPrivateDirectorySize(string path);
    string GetJunctionStatus(string accountName);
    AccountStats GetAccountStats(string accountName);
    void ClearStatsCache();
}
