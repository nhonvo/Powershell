namespace AgyTui.Infrastructure.Integrations.Ai.Abstractions;

public interface IAiCommitGenerator
{
    string GenerateDraftDescription(string diff);
}
