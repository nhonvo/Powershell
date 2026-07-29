namespace AgyTui.Tests;

using AgyTui.Core.Models;
using System.IO;
using System.Runtime.CompilerServices;

public static class TestInitializer
{
    [ModuleInitializer]
    public static void Init()
    {
        var tempConfigFile = Path.Combine(Path.GetTempPath(), "agy_test_profile_config.json");
        if (!File.Exists(tempConfigFile))
        {
            File.WriteAllText(tempConfigFile, "{\n  \"Ui\": { \"Mode\": \"flat-tree\" },\n  \"Ai\": { \"Mode\": \"auto\" }\n}");
        }
        Config.OverrideConfigPath = tempConfigFile;
        Config.Load();
    }
}
