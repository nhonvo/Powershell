namespace AgyTui.Tests.Unit.Architecture;

using AgyTui.Core.Registries;
using Xunit;

public class IdeCommandRegistryTests
{
    [Fact]
    public void ExecuteCommand_KnownCommand_InvokesActionAndReturnsTrue()
    {
        var ctx = new IdeContext(Directory.GetCurrentDirectory(), null);
        bool executed = IdeCommandRegistry.ExecuteCommand(ctx, "open Program.cs");
        Assert.True(executed);
        Assert.NotNull(ctx.CurrentFile);
    }
}
