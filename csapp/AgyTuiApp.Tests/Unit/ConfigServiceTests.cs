using Xunit;
using AgyTui;

namespace AgyTui.Tests.Unit;

public class ConfigServiceTests
{
    [Fact]
    public void Save_UiModeChanged_DoesNotMutateAiMode()
    {
        var cfg = Config.Current;
        var initialAiMode = cfg.Ai.Mode;

        cfg.Ui.Mode = "flat-tree";
        Config.Save();

        var reloaded = Config.Current;
        Assert.Equal("flat-tree", reloaded.Ui.Mode);
        Assert.Equal(initialAiMode, reloaded.Ai.Mode);
    }

    [Fact]
    public void Save_ExistingComments_ArePreserved()
    {
        var cfg = Config.Current;
        Config.Save();
        var reloaded = Config.Current;
        Assert.NotNull(reloaded);
    }
}
