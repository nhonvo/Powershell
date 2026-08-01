using AgyTui.UI.Core.Navigation;

namespace AgyTui.Tests.Unit.UI.Navigation;

public class SubPageNavigatorTests
{
    [Fact]
    public void HKey_WhileSearchBufferActive_AppendsToBuffer_DoesNotClearOrExit()
    {
        var key = new ConsoleKeyInfo('h', ConsoleKey.H, false, false, false);
        var result = SubPageNavigator.ProcessSearchKey(key, "hea");
        Assert.Equal("heah", result);
    }

    [Fact]
    public void LKey_WhileSearchBufferActive_AppendsToBuffer_IsNotSwallowed()
    {
        var key = new ConsoleKeyInfo('l', ConsoleKey.L, false, false, false);
        var result = SubPageNavigator.ProcessSearchKey(key, "htm");
        Assert.Equal("html", result);
    }

    [Fact]
    public void GetFlatList_SearchBufferMatchingChild_ExpandsOnlyMatchingChild_SuppressesSiblings()
    {
        var roots = AgyTui.Infrastructure.Registries.WorkspaceRegistry.GetRootWorkspaces();
        if (roots.Length == 0) return;

        var list = SubPageProjNavigator.GetFlatList(roots, "assets");
        // Only items where name matches "assets" or root parents with matching children should be present
        foreach (var item in list)
        {
            if (item.IsChildWorkspace)
            {
                Assert.Contains("assets", item.Workspace.Name, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    [Fact]
    public void GetFlatList_SearchBufferMatchingRootOnly_DoesNotForceExpandRootChildren()
    {
        var mockRoot = new AgyTui.Domain.WorkspaceContext.WorkspaceEntry("UniqueTestRootNameXYZ", @"C:\TestPathXYZ", "default", ["test"], null, null, null, true);
        var mockRoots = new[] { mockRoot };

        // Search query matching root workspace name but no child name
        var list = SubPageProjNavigator.GetFlatList(mockRoots, "UniqueTestRootNameXYZ");
        var childCountInList = list.Count(i => i.IsChildWorkspace);
        Assert.Equal(0, childCountInList);
    }

    [Fact]
    public void GetFlatList_ZeroWorkspaces_ReturnsEmptyList()
    {
        // Zero case: empty workspace collection
        var list = SubPageProjNavigator.GetFlatList(Array.Empty<AgyTui.Domain.WorkspaceContext.WorkspaceEntry>(), "query");
        Assert.Empty(list);
    }

    [Fact]
    public void GetFlatList_NonMatchingSearchQuery_ReturnsEmptyList()
    {
        // Failure/Zero match case: query matches nothing
        var mockRoot = new AgyTui.Domain.WorkspaceContext.WorkspaceEntry("ProjectAlpha", @"C:\ProjectAlpha", "default", ["test"], null, null, null, true);
        var list = SubPageProjNavigator.GetFlatList(new[] { mockRoot }, "NON_EXISTENT_QUERY_999");
        Assert.Empty(list);
    }

    [Fact]
    public void ProcessSearchKey_BackspaceKey_RemovesLastCharacter()
    {
        // Zero/Edge case: backspace on buffer
        var key = new ConsoleKeyInfo('\b', ConsoleKey.Backspace, false, false, false);
        var result = SubPageNavigator.ProcessSearchKey(key, "test");
        Assert.Equal("tes", result);
    }
}
