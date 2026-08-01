using AgyTui.UI.Core.Registries;
namespace AgyTui.Tests.Unit.UI.Registries;

public class CommandRegistryTests
{
    [Fact]
    public void Categories_SortOrder_MatchesProposedSequence()
    {
        var root = MenuNodeBuilder.BuildTree();

        var categoryNames = root.Children
            .Where(c => c.Kind == MenuNodeKind.Category)
            .Select(c => c.Label)
            .ToList();

        var expectedSequence = new[]
        {
            "[Favorites]",
            "[Workspace & Dev]",
            "[AI Agent & Ollama]",
            "[AGY Account Switch]",
            "[Learn & Study]",
            "[Obsidian & Resources]",
            "[Appearance & Layout]",
            "[System & Network]",
            "[Help & Docs]"
        };

        Assert.Equal(expectedSequence, categoryNames);
    }

    [Fact]
    public void AssertAllAliasesReachable_DoesNotThrow()
    {
        var root = MenuNodeBuilder.BuildTree();
        var ex = Record.Exception(() => CommandRegistry.AssertAllAliasesReachable(root));
        Assert.Null(ex);
    }

    [Fact]
    public void AssertSwitchCases_WorksFromPublishedOutputLayout()
    {
        var ex = Record.Exception(() => CommandRegistry.AssertSwitchCases());
        Assert.Null(ex);
    }
}


