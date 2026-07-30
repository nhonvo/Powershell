using AgyTui.Domain.AccountContext;

namespace AgyTui.Infrastructure.Integrations.AgyClient.Interfaces;

public sealed record AccountStats(string LastUsed, int UsageCount, string PrivateSize, string JunctionStatus, int SkillsCount, int ConversationsCount, string TokenStatus, string QuotaStatus, double GeminiWeekly, double GeminiFiveHour);

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
