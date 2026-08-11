using AgyTui.UI.Screens.Scaffolder;

namespace AgyTui.Tests.Unit.UI.Screens.Scaffolder;

public class ScaffolderScreenTests
{
    [Fact]
    public void ProjectScaffolder_Instance_CanBeCreated()
    {
        var scaffolder = new ProjectScaffolder();
        Assert.NotNull(scaffolder);
    }
}
