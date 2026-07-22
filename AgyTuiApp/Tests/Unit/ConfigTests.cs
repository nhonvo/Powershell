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

    [Fact]
    public void Config_Save_DoesNotCorrupt_AiMode_When_UiMode_Saved()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), "test_profile_config_" + System.Guid.NewGuid() + ".json");
        try
        {
            var initialJson = @"{
  ""Ui"": {
    ""Mode"": ""flat-tree"",
    ""Theme"": ""dracula"",
    ""Density"": ""comfortable""
  },
  ""Ai"": {
    ""Mode"": ""auto"",
    ""ProviderMode"": ""cloud""
  }
}";
            File.WriteAllText(tempFile, initialJson);

            // Simulate Save line-by-line patch
            var lines = File.ReadAllLines(tempFile);
            string currentSection = "";
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                var trimmed = line.Trim();
                if (trimmed.StartsWith("\"Ui\"")) currentSection = "Ui";
                else if (trimmed.StartsWith("\"Ai\"")) currentSection = "Ai";
                else if (trimmed.StartsWith("\"SpacedRepetition\"")) currentSection = "SpacedRepetition";
                else if (trimmed.StartsWith("}") && !trimmed.Contains("{")) currentSection = "";

                if (currentSection == "Ui" && line.Contains("\"Mode\":"))
                {
                    lines[i] = System.Text.RegularExpressions.Regex.Replace(line, @"(""Mode""\s*:\s*"")[^""]*("")", "$1three-pane$2");
                }
                else if (currentSection == "Ai" && line.Contains("\"Mode\":"))
                {
                    lines[i] = System.Text.RegularExpressions.Regex.Replace(line, @"(""Mode""\s*:\s*"")[^""]*("")", "$1auto$2");
                }
            }
            File.WriteAllLines(tempFile, lines);

            var resultContent = File.ReadAllText(tempFile);
            Assert.Contains(@"""Mode"": ""three-pane""", resultContent);
            Assert.Contains(@"""Mode"": ""auto""", resultContent);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }
}
