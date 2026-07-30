namespace AgyTui.Domain.AccountContext;

public class AccountAggregate
{
    public string AccountName { get; private set; }
    public string? Email { get; private set; }
    public bool IsActive { get; private set; }
    public string QuotaStatus { get; private set; }
    public string LastUsed { get; private set; }
    public int UsageCount { get; private set; }
    public List<string> RequestHistory { get; private set; } = new();

    public AccountAggregate(string accountName, string? email = null, bool isActive = false, string quotaStatus = "OK")
    {
        AccountName = string.IsNullOrWhiteSpace(accountName) ? throw new ArgumentException("Account name required", nameof(accountName)) : accountName;
        Email = email;
        IsActive = isActive;
        QuotaStatus = quotaStatus;
        LastUsed = DateTimeOffset.Now.ToString("yyyy-MM-ddTHH:mm:sszzz");
        UsageCount = 0;
    }

    public void MarkActive() => IsActive = true;
    public void MarkInactive() => IsActive = false;
    public void SetQuotaExceeded(bool exceeded) => QuotaStatus = exceeded ? "Exceeded" : "OK";
    public void RecordUsage(string timestamp)
    {
        UsageCount++;
        LastUsed = timestamp;
        RequestHistory.Add(timestamp);
    }
}
