namespace AgyTui.Infrastructure.Integrations.Ai.Abstractions;

public interface IOpenClawClient
{
    void EnsureGateway();
    void InvokeOpenClaw(string[] argsList);
    void InvokeClawdbot(string[] argsList);
}
