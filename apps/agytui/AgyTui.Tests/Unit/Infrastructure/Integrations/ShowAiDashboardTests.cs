namespace AgyTui.Tests.Unit.Infrastructure.Integrations;

[Collection("Sequential")]
public class ShowAiDashboardTests
{
    [Fact]
    public void Config_AiProviderMode_CanBeUpdatedAndRestored()
    {
        var orig = Config.Current.Ai.ProviderMode;
        try
        {
            Config.Current.Ai.ProviderMode = "cloud";
            Assert.Equal("cloud", Config.Current.Ai.ProviderMode);
        }
        finally
        {
            Config.Current.Ai.ProviderMode = orig;
        }
    }
}
