namespace AgyTui.Tests.Unit.UI.Common;

using AgyTui.UI.Core.Common;
using Xunit;

public class IconsTests
{
    [Fact]
    public void GetGlyphDisplayWidth_KnownEmojiAndScrollIndicators_Returns2()
    {
        Assert.Equal(2, Icons.GetGlyphDisplayWidth("📁"));
        Assert.Equal(2, Icons.GetGlyphDisplayWidth("🤖"));
        Assert.Equal(2, Icons.GetGlyphDisplayWidth("👤"));
        Assert.Equal(2, Icons.GetGlyphDisplayWidth("🐳"));
        Assert.Equal(2, Icons.GetGlyphDisplayWidth("▲"));
        Assert.Equal(2, Icons.GetGlyphDisplayWidth("▼"));

        Assert.Equal(1, Icons.GetGlyphDisplayWidth("a"));
        Assert.Equal(1, Icons.GetGlyphDisplayWidth("1"));
        Assert.Equal(1, Icons.GetGlyphDisplayWidth("+"));
    }
}
