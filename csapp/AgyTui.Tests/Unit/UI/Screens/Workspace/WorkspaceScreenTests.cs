using AgyTui.Infrastructure.Di;
using AgyTui.Infrastructure.Registries;
using AgyTui.UI.Screens.Workspace;
using AgyTui.UI.Screens.Workspace.Helpers;
using AgyTui.UI.Screens.Workspace.Navigators;

namespace AgyTui.Tests.Unit.UI.Screens.Workspace;

public class WorkspaceScreenTests
{
    [Fact]
    public void ProjectScreen_CanBeInstantiated()
    {
        var screen = new ProjectScreen();
        Assert.NotNull(screen);

        int count = screen.GetItemCount("");
        Assert.True(count >= 0);
    }

    [Fact]
    public void WorkspaceRegistry_StaticType_Exists()
    {
        Assert.NotNull(typeof(WorkspaceRegistry));
    }

    [Fact]
    public void SubPageProjNavigator_StaticType_Exists()
    {
        Assert.NotNull(typeof(SubPageProjNavigator));
    }
}
