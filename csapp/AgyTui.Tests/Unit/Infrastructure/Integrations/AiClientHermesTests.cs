using AgyTui.Infrastructure.Integrations.Ai.Providers;
using Xunit;

namespace AgyTui.Tests.Unit.Infrastructure.Integrations;

public class AiClientHermesTests
{
    [Fact]
    public void HermesProvider_FindOnPath_ReturnsNullForNonExistentBinary()
    {
        var result = HermesProvider.FindOnPath("non_existent_binary_xyz_123");
        Assert.Null(result);
    }
}
