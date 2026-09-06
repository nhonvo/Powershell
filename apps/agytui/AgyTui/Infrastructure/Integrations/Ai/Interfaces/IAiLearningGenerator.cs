namespace AgyTui.Infrastructure.Integrations.Ai.Abstractions;

public interface IAiLearningGenerator
{
    void RunGenerator(string domain = "");
}
