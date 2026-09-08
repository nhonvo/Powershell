namespace AgyTui.Domain.AiAgentContext;

public class AgentInvocationLog
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Alias { get; private set; }
    public DateTime TimestampUtc { get; private set; } = DateTime.UtcNow;
    public long DurationMs { get; private set; }
    public bool Success { get; private set; }
    public string ActiveAccount { get; private set; }
    public ProviderMode Mode { get; private set; }

    public AgentInvocationLog(string alias, long durationMs, bool success, string activeAccount, ProviderMode mode = ProviderMode.Auto)
    {
        Alias = alias;
        DurationMs = durationMs;
        Success = success;
        ActiveAccount = activeAccount;
        Mode = mode;
    }
}
