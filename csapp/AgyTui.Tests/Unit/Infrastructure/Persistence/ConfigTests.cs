namespace AgyTui.Tests.Unit.Infrastructure.Persistence;

public class ConfigTests
{
    [Fact]
    public void Config_Defaults_AreValid()
    {
        Assert.NotNull(Config.Current);
        Assert.NotNull(Config.Current.Ui);
        Assert.NotNull(Config.Current.Ai);
    }

    [Fact]
    public void IsMobileContext_And_RendererDensityCheck_AlwaysAgree()
    {
        var originalDensity = Config.Current.Ui.Density;
        try
        {
            Config.Current.Ui.Density = "comfortable";
            Assert.False(Config.Current.Density == "compact" && !Config.IsMobileContext());

            Config.Current.Ui.Density = "compact";
            Assert.True(Config.IsMobileContext());
        }
        finally
        {
            Config.Current.Ui.Density = originalDensity;
        }
    }
}
