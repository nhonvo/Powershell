namespace AgyTui.Tests.Unit.UI.Navigation;

using System.Linq;
using AgyTui.UI.Core.Navigation;
using Xunit;

public class CommandPaletteTests
{
    private static readonly PaletteCommand[] SampleCommands = new[]
    {
        new PaletteCommand("proj", "Workspace", "Navigation"),
        new PaletteCommand("claude", "Claude AI", "AI")
    };

    private static readonly string[] SampleCategories = new[] { "Navigation", "AI" };

    [Fact]
    public void CategoryPicker_EscapePressed_ReturnsNullToCancel()
    {
        var filtered = CommandPalette.GetFilteredCommands(-1, SampleCategories, SampleCommands);
        Assert.Null(filtered);
    }

    [Fact]
    public void CategoryPicker_AllSelected_ReturnsAllCommands()
    {
        var filtered = CommandPalette.GetFilteredCommands(0, SampleCategories, SampleCommands);
        Assert.NotNull(filtered);
        Assert.Equal(2, filtered.Count());
    }

    [Fact]
    public void CategoryPicker_SpecificCategorySelected_ReturnsFilteredCommands()
    {
        var filtered = CommandPalette.GetFilteredCommands(1, SampleCategories, SampleCommands);
        Assert.NotNull(filtered);
        Assert.Single(filtered);
        Assert.Equal("proj", filtered.First().Alias);
    }
}
