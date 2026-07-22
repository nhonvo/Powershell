using System.IO;
using Xunit;
using AgyTui;

namespace AgyTui.Tests.Integration;

public class ResourceDiscoveryTests
{
    [Fact]
    public void ScanDirectory_ValidDirectory_DiscoversResources()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "agy_res_test_" + System.Guid.NewGuid().ToString("N")[..6]);
        Directory.CreateDirectory(tempDir);
        try
        {
            var sampleFile = Path.Combine(tempDir, "vocab.tsv");
            File.WriteAllText(sampleFile, "word\treading\tmeaning\ncat\tねこ\tneko\n");

            var format = ResourceRegistry.DetectFormat(sampleFile);
            Assert.Equal("tsv", format);
        }
        finally
        {
            try { Directory.Delete(tempDir, true); } catch { }
        }
    }
}
