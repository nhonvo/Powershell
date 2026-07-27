namespace AgyTui.Infrastructure.Integrations.Ai;

public interface IOpenClawClient
{
    void EnsureGateway();
    void InvokeOpenClaw(string[] argsList);
    void InvokeClawdbot(string[] argsList);
}
