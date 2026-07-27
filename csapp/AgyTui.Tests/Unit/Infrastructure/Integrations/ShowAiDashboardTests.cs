using AgyTui.Core.Models;
using Xunit;

namespace AgyTui.Tests.Unit.Infrastructure.Integrations;

public class ShowAiDashboardTests
{
    [Fact]
    public void Config_AiProviderMode_CanBeUpdatedAndRestored()
    {
        var orig = Config.Current.AiProviderMode;
        try
        {
            Config.Current.AiProviderMode = "cloud";
            Assert.Equal("cloud", Config.Current.AiProviderMode);
        }
        finally
        {
            Config.Current.AiProviderMode = orig;
        }
    }
}
