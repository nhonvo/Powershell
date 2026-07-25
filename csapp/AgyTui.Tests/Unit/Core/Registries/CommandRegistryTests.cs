namespace AgyTui.Tests.Unit.Core.Registries;

using System.Linq;
using AgyTui.Core.Registries;
using AgyTui.UI.Core.Layouts;
using Xunit;

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
            "[AGY Account Switch]",
            "[AI Agent & Ollama]",
            "[Appearance & Layout]",
            "[Learn & Study]",
            "[Obsidian & Resources]",
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
