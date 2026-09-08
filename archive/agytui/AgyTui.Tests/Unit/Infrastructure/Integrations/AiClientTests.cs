namespace AgyTui.Tests.Unit.Infrastructure.Integrations;

[Collection("Sequential")]
public class AiClientTests
{
    [Fact]
    public void Config_AiProviderMode_DefaultsToHybridOrConfiguredValue()
    {
        var mode = Config.Current.Ai.ProviderMode;
        Assert.NotNull(mode);
    }

    [Fact]
    public void Config_EnableAiOllama_FlagCanBeToggled()
    {
        var orig = Config.Current.Ai.EnableOllama;
        try
        {
            Config.Current.Ai.EnableOllama = true;
            Assert.True(Config.Current.Ai.EnableOllama);
        }
        finally
        {
            Config.Current.Ai.EnableOllama = orig;
        }
    }
}
