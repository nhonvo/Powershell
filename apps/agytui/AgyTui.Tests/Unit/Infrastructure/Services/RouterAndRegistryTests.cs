using AgyTui.Infrastructure.Registries;
using AgyTui.Infrastructure.Services;
using Xunit;

namespace AgyTui.Tests.Unit.Infrastructure.Services;

public class RouterAndRegistryTests
{
    [Fact]
    public void WorkspaceRegistry_GetRootWorkspaces_ReturnsArray()
    {
        var roots = WorkspaceRegistry.GetRootWorkspaces();
        Assert.NotNull(roots);
    }

    [Fact]
    public void ResourceRegistry_LoadAll_ReturnsList()
    {
        var resources = ResourceRegistry.LoadAll();
        Assert.NotNull(resources);
    }
}
