using System.IO;
using AgyTui;
using Xunit;

namespace AgyTuiApp.Tests;

public class ConfigTests
{
    [Fact]
    public void Config_Defaults_AreValid()
    {
        Assert.NotNull(Config.Current);
        Assert.NotNull(Config.Current.Ui);
        Assert.NotNull(Config.Current.Ai);
    }
}
