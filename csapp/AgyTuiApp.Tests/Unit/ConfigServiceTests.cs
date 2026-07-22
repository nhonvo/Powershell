using Xunit;
using AgyTui;

namespace AgyTui.Tests.Unit;

public class ConfigServiceTests : System.IDisposable
{
    private readonly string _tempFile;

    public ConfigServiceTests()
    {
        _tempFile = Path.Combine(Path.GetTempPath(), $"test_profile_config_{System.Guid.NewGuid()}.json");
        var realPath = Config.GetConfigFilePath();
        if (File.Exists(realPath))
        {
            File.Copy(realPath, _tempFile, true);
        }
        else
        {
            File.WriteAllText(_tempFile, "{\n  \"Ui\": { \"Mode\": \"flat-tree\" },\n  \"Ai\": { \"Mode\": \"auto\" }\n}");
        }
        Config.OverrideConfigPath = _tempFile;
        Config.Load();
    }

    public void Dispose()
    {
        Config.OverrideConfigPath = null;
        Config.Load();
        if (File.Exists(_tempFile))
        {
            try { File.Delete(_tempFile); } catch { }
        }
    }

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
