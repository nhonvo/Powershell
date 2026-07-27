using AgyTui.Core.Models;
using Xunit;

namespace AgyTui.Tests.Unit.Infrastructure.Integrations;

public class AiClientTests
{
    [Fact]
    public void Config_AiProviderMode_DefaultsToHybridOrConfiguredValue()
    {
        var mode = Config.Current.AiProviderMode;
        Assert.NotNull(mode);
    }

    [Fact]
    public void Config_EnableAiOllama_FlagCanBeToggled()
    {
        var orig = Config.Current.EnableAiOllama;
        try
        {
            Config.Current.EnableAiOllama = true;
            Assert.True(Config.Current.EnableAiOllama);
        }
        finally
        {
            Config.Current.EnableAiOllama = orig;
        }
    }
}
