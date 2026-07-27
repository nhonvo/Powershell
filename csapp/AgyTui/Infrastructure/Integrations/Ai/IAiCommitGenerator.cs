namespace AgyTui.Infrastructure.Integrations.Ai;

public interface IAiCommitGenerator
{
    string GenerateDraftDescription(string diff);
}
