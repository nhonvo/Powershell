using System;
using System.IO;
using System.Linq;
using AgyTui;
using AgyTui.Registry;
using Xunit;

namespace AgyTuiApp.Tests;

public class LearningDataTests
{
    [Fact]
    public void LearnDataPaths_DomainDirectories_AreNotNullOrEmpty()
    {
        Assert.False(string.IsNullOrWhiteSpace(LearnDataPaths.LearnRoot));
        Assert.False(string.IsNullOrWhiteSpace(LearnDataPaths.JapaneseDir));
        Assert.False(string.IsNullOrWhiteSpace(LearnDataPaths.EnglishDir));
        Assert.False(string.IsNullOrWhiteSpace(LearnDataPaths.CsharpDir));
        Assert.False(string.IsNullOrWhiteSpace(LearnDataPaths.DsaDir));
        Assert.False(string.IsNullOrWhiteSpace(LearnDataPaths.CareerDir));
        Assert.False(string.IsNullOrWhiteSpace(LearnDataPaths.CertificationsDir));
        Assert.False(string.IsNullOrWhiteSpace(LearnDataPaths.StatsDir));

        Assert.EndsWith("japanese", LearnDataPaths.JapaneseDir, StringComparison.OrdinalIgnoreCase);
        Assert.EndsWith("english", LearnDataPaths.EnglishDir, StringComparison.OrdinalIgnoreCase);
        Assert.EndsWith("csharp", LearnDataPaths.CsharpDir, StringComparison.OrdinalIgnoreCase);
        Assert.EndsWith("dsa", LearnDataPaths.DsaDir, StringComparison.OrdinalIgnoreCase);
        Assert.EndsWith("career", LearnDataPaths.CareerDir, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GrammarCard_RecordInstantiation_WorksCorrectly()
    {
        var sr = new SrState(2.5, 1, 1, DateTime.UtcNow, DateTime.UtcNow.AddDays(1), "review");
        var card = new GrammarCard("g_test_1", "N5", "～は～です", "A is B", "Noun + は + Noun + です", "わたしは がくせいです。", "I am a student.", new[] { "grammar", "n5" }, sr);

        Assert.Equal("g_test_1", card.Id);
        Assert.Equal("N5", card.Level);
        Assert.Equal("～は～です", card.Pattern);
        Assert.Equal("A is B", card.Meaning);
        Assert.Equal(2, card.Tags.Length);
    }

    [Fact]
    public void CommandRegistry_ContainsLearningAndVaultCommands()
    {
        var aliases = new[] { "obsidian", "refresh", "vault-open", "grammar", "jlpt", "vocab", "algo", "quiz", "interview", "star", "mock" };

        foreach (var alias in aliases)
        {
            var entry = CommandRegistry.All.FirstOrDefault(c => c.Alias.Equals(alias, StringComparison.OrdinalIgnoreCase));
            Assert.NotNull(entry);
            Assert.Equal("[Learn & Study]", entry.Category);
        }
    }

    [Fact]
    public void ObsidianBridge_LoadConfig_ReturnsFallbackOrConfiguredVault()
    {
        var config = ObsidianBridge.LoadConfig();
        if (config != null)
        {
            Assert.False(string.IsNullOrWhiteSpace(config.VaultPath));
        }
    }
}
