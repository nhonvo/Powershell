using AgyTui.UI.Screens.GitNexus;
using AgyTui.UI.Screens.GitNexus.Helpers;

namespace AgyTui.Tests.Unit.UI.Screens.GitNexus;

public class GitNexusScreenTests
{
    [Fact]
    public void GitNexusStats_StaticType_Exists()
    {
        Assert.NotNull(typeof(GitNexusStats));
    }

    [Fact]
    public void RepoGraph_StaticType_Exists()
    {
        Assert.NotNull(typeof(RepoGraph));
    }
}
