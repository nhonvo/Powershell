namespace AgyTui.Infrastructure.Integrations.Ai.Services;

public static class AiCommitGenerator
{
    public static string GenerateDraftDescription(string diff)
    {
        if (string.IsNullOrWhiteSpace(diff)) return "Automated commit: minor updates.";

        try
        {
            var summary = $"Updated {diff.Split('\n').Length} lines of code changes.";
            return summary;
        }
        catch
        {
            return "Automated commit: code updates.";
        }
    }
}
