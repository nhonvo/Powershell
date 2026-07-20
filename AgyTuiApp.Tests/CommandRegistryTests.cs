using System.Linq;
using AgyTui.Registry;
using Xunit;

namespace AgyTuiApp.Tests;

public class CommandRegistryTests
{
    [Fact]
    public void CommandRegistry_ContainsAllExpectedAliases()
    {
        Assert.NotEmpty(CommandRegistry.All);
        Assert.True(CommandRegistry.All.Length >= 80);
    }

    [Fact]
    public void CommandRegistry_Lookup_ReturnsValidCommandEntry()
    {
        var cmd = CommandRegistry.All.FirstOrDefault(c => c.Alias == "proj");
        Assert.NotNull(cmd);
        Assert.Equal("proj", cmd.Alias);
    }

    [Fact]
    public void AssertSwitchCases_DoesNotThrow_WhenAllAliasesAreMapped()
    {
        var exception = Record.Exception(() => CommandRegistry.AssertSwitchCases());
        Assert.Null(exception);
    }
}
