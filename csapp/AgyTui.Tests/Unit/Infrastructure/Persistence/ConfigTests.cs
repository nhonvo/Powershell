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
            Assert.Contains(favNode.Children, c => c.Command?.Alias == "gs");
            Assert.Contains(favNode.Children, c => c.Command?.Alias == "ga");
        }
        finally
        {
            Config.Current.Ui.FavoriteAliases = originalFavs;
        }
    }

    [Fact]
    public void FavoriteAliases_Add_SavesAndPersistsNewAlias()
    {
        var originalFavs = Config.Current.Ui.FavoriteAliases;
        try
        {
            var favList = (Config.Current.Ui.FavoriteAliases ?? Config.DefaultFavoriteAliases).ToList();
            if (!favList.Contains("docker-health", StringComparer.OrdinalIgnoreCase))
            {
                favList.Add("docker-health");
            }
            Config.Current.Ui.FavoriteAliases = [.. favList];
            Config.Save();

            Config.Load();
            Assert.Contains("docker-health", Config.Current.Ui.FavoriteAliases);
        }
        finally
        {
            Config.Current.Ui.FavoriteAliases = originalFavs;
            Config.Save();
        }
    }

    [Fact]
    public void FavoriteAliases_Remove_RemovesAliasAndPersists()
    {
        var originalFavs = Config.Current.Ui.FavoriteAliases;
        try
        {
            var favList = (Config.Current.Ui.FavoriteAliases ?? Config.DefaultFavoriteAliases).ToList();
            favList.RemoveAll(a => string.Equals(a, "proj", StringComparison.OrdinalIgnoreCase));
            Config.Current.Ui.FavoriteAliases = [.. favList];
            Config.Save();

            Config.Load();
            Assert.DoesNotContain("proj", Config.Current.Ui.FavoriteAliases);
        }
        finally
        {
            Config.Current.Ui.FavoriteAliases = originalFavs;
            Config.Save();
        }
    }

    [Fact]
    public void FavoriteAliases_Reset_RestoresDefaultAliases()
    {
        var originalFavs = Config.Current.Ui.FavoriteAliases;
        try
        {
            Config.Current.Ui.FavoriteAliases = ["custom1", "custom2"];
            Config.Save();

            Config.Current.Ui.FavoriteAliases = [.. Config.DefaultFavoriteAliases];
            Config.Save();

            Config.Load();
            Assert.Equal(Config.DefaultFavoriteAliases.Length, Config.Current.Ui.FavoriteAliases.Length);
            Assert.Contains("proj", Config.Current.Ui.FavoriteAliases);
        }
        finally
        {
            Config.Current.Ui.FavoriteAliases = originalFavs;
            Config.Save();
        }
    }

    [Fact]
    public void FavoriteAliases_EmptyArray_PreservedOnReload()
    {
        var originalFavs = Config.Current.Ui.FavoriteAliases;
        try
        {
            Config.Current.Ui.FavoriteAliases = Array.Empty<string>();
            Config.Save();

            Config.Load();
            Assert.NotNull(Config.Current.Ui.FavoriteAliases);
            Assert.Empty(Config.Current.Ui.FavoriteAliases);
        }
        finally
        {
            Config.Current.Ui.FavoriteAliases = originalFavs;
            Config.Save();
        }
    }

    [Fact]
    public void FavoriteAliases_Edit_ReplacesAliasInSlotAndPersists()
    {
        var originalFavs = Config.Current.Ui.FavoriteAliases;
        try
        {
            var favList = (Config.Current.Ui.FavoriteAliases ?? Config.DefaultFavoriteAliases).ToList();
            int idx = favList.FindIndex(a => string.Equals(a, "proj", StringComparison.OrdinalIgnoreCase));
            if (idx >= 0)
            {
                favList[idx] = "gsu";
            }
            Config.Current.Ui.FavoriteAliases = [.. favList];
            Config.Save();

            Config.Load();
            Assert.Contains("gsu", Config.Current.Ui.FavoriteAliases);
            Assert.DoesNotContain("proj", Config.Current.Ui.FavoriteAliases);
        }
        finally
        {
            Config.Current.Ui.FavoriteAliases = originalFavs;
            Config.Save();
        }
    }
}
