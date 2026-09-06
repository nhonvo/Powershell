using AgyTui.Infrastructure.Integrations.Ai.Providers;

namespace AgyTui.Tests.Unit.UI.Screens.Ollama;

public class OllamaScreenTests
{
    [Fact]
    public void OllamaClient_StaticType_Exists()
    {
        Assert.NotNull(typeof(OllamaClient));
    }
}
