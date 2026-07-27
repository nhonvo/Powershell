using AgyTui.Core.Models;
using Xunit;

namespace AgyTui.Tests.Unit.Infrastructure.Integrations;

public class AiModeCheckTests
{
    [Fact]
    public void Config_AiConfig_InitializesWithValidDefaults()
    {
        var ai = Config.Current.Ai;
        Assert.NotNull(ai);
        Assert.NotNull(ai.Mode);
        Assert.NotNull(ai.ProviderMode);
    }
}
