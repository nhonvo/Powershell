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
}
