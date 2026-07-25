namespace AgyTuiApp.Tests.Integration;

public class TsvExtractorTests
{
    [Fact]
    public void DetectFormat_TsvExtension_ReturnsTsv()
    {
        var format = ResourceRegistry.DetectFormat("custom_vocab.tsv");
        Assert.Equal("tsv", format);
    }
}
