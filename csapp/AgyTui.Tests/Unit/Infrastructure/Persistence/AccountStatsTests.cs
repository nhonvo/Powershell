namespace AgyTui.Tests.Unit.Infrastructure.Persistence;

[Collection("Sequential")]
public class AccountStatsTests
{
    [Fact]
    public void GetAccountStats_ReadsSkillsAndBrainFromAccountSpecificDir()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "AgyStatsTest_" + Path.GetRandomFileName());
        var originalHome = Config.Current.System.AgySourceHome;
        try
        {
            Config.Current.System.AgySourceHome = tempDir;
            var accDir = AgyAccountCore.GetAccountDirectory("testacc");
            Directory.CreateDirectory(Path.Combine(accDir, "skills", "skill1"));
            Directory.CreateDirectory(Path.Combine(accDir, "skills", "skill2"));
            Directory.CreateDirectory(Path.Combine(accDir, "brain", "conv1"));
            AgyAccountCore.ClearStatsCache();

            var stats = AgyAccountCore.GetAccountStats("testacc");

            Assert.Equal(2, stats.SkillsCount);
            Assert.Equal(1, stats.ConversationsCount);
        }
        finally
        {
            Config.Current.System.AgySourceHome = originalHome;
            if (Directory.Exists(tempDir))
            {
                try { Directory.Delete(tempDir, true); } catch { }
            }
        }
    }
}
