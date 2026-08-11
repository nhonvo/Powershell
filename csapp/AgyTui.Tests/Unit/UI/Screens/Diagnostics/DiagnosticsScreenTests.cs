using AgyTui.Infrastructure.Integrations.Docker;
using AgyTui.Infrastructure.Logging;

namespace AgyTui.Tests.Unit.UI.Screens.Diagnostics;

public class DiagnosticsScreenTests
{
    [Fact]
    public void LogHelper_StaticType_Exists()
    {
        Assert.NotNull(typeof(LogHelper));
    }

    [Fact]
    public void DockerClient_StaticType_Exists()
    {
        Assert.NotNull(typeof(DockerClient));
    }
}
