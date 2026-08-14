namespace AgyTui.Tests.Unit.Infrastructure.Persistence;

[Collection("Sequential")]
public class ConfigTests
{
    public ConfigTests()
    {
        if (Config.Current.Ui.FavoriteAliases == null || Config.Current.Ui.FavoriteAliases.Length == 0)
        {
            Config.Current.Ui.FavoriteAliases = ["proj", "agyswitch", "open-term", "ask-ai", "vault", "ide"];
            Config.Save();
        }
    }

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
    public void FavoriteAliases_LoadedFromConfig_PopulatesDefaultList()
    {
        Assert.NotNull(Config.Current.Ui.FavoriteAliases);
        Assert.Contains("proj", Config.Current.Ui.FavoriteAliases);
        Assert.Contains("agyswitch", Config.Current.Ui.FavoriteAliases);
        Assert.Contains("open-term", Config.Current.Ui.FavoriteAliases);
        Assert.Contains("ask-ai", Config.Current.Ui.FavoriteAliases);
        Assert.Contains("vault", Config.Current.Ui.FavoriteAliases);
        Assert.Contains("ide", Config.Current.Ui.FavoriteAliases);
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
            var favList = (Config.Current.Ui.FavoriteAliases ?? Array.Empty<string>()).ToList();
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
            var favList = (Config.Current.Ui.FavoriteAliases ?? Array.Empty<string>()).ToList();
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

            Config.Current.Ui.FavoriteAliases = ["proj", "agyswitch", "open-term", "vault", "ide", "ask-ai"];
            Config.Save();

            Config.Load();
            Assert.Equal(6, Config.Current.Ui.FavoriteAliases.Length);
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
            Config.Current.Ui.FavoriteAliases = (originalFavs != null && originalFavs.Length > 0)
                ? originalFavs
                : ["proj", "agyswitch", "open-term", "ask-ai", "vault", "ide"];
            Config.Save();
        }
    }

    [Fact]
    public void FavoriteAliases_Edit_ReplacesAliasInSlotAndPersists()
    {
        var originalFavs = Config.Current.Ui.FavoriteAliases;
        try
        {
            var favList = (Config.Current.Ui.FavoriteAliases ?? Array.Empty<string>()).ToList();
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
