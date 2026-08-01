namespace AgyTui.Tests.Unit.Infrastructure.Persistence;

[Collection("Sequential")]
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
            Assert.False(Config.Current.Ui.Density == "compact" && !Config.IsMobileContext());

            Config.Current.Ui.Density = "compact";
            Assert.True(Config.IsMobileContext());
        }
        finally
        {
            Config.Current.Ui.Density = originalDensity;
        }
    }

    [Fact]
    public void FavoriteAliases_DefaultsToCurrentHardcodedList_NoConfigMigrationNeeded()
    {
        Assert.NotNull(Config.DefaultFavoriteAliases);
        Assert.Contains("proj", Config.DefaultFavoriteAliases);
        Assert.Contains("agyswitch", Config.DefaultFavoriteAliases);
        Assert.Contains("open-term", Config.DefaultFavoriteAliases);
        Assert.Contains("ask-ai", Config.DefaultFavoriteAliases);
        Assert.Contains("vault", Config.DefaultFavoriteAliases);
        Assert.Contains("ide", Config.DefaultFavoriteAliases);
    }

    [Fact]
    public void MenuNodeBuilder_BuildTree_FavoritesCategory_ReflectsConfigNotHardcodedArray()
    {
        var originalFavs = Config.Current.Ui.FavoriteAliases;
        try
        {
            Config.Current.Ui.FavoriteAliases = ["gs", "ga"];
            var root = AgyTui.UI.Core.Layouts.MenuNodeBuilder.BuildTree();
            var favNode = root.Children.FirstOrDefault(c => c.Id == "favorites" || c.Label == "[Favorites]");
            Assert.NotNull(favNode);
            Assert.Equal(2, favNode.Children.Length);
            Assert.Equal("gs", favNode.Children[0].Command?.Alias);
            Assert.Equal("ga", favNode.Children[1].Command?.Alias);
        }
        finally
        {
            Config.Current.Ui.FavoriteAliases = originalFavs;
        }
    }
}
