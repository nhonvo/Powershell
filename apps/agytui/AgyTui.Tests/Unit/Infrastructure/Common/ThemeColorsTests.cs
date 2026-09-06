namespace AgyTui.Tests.Unit.Infrastructure.Common;

public class ThemeColorsTests
{
    [Fact]
    public void ThemeColors_ShouldHaveDefaultFallbackValues()
    {
        // Assert fallbacks are non-null and non-empty
        Assert.False(string.IsNullOrEmpty(AgyThemeColors.Accent));
        Assert.False(string.IsNullOrEmpty(AgyThemeColors.Secondary));
        Assert.False(string.IsNullOrEmpty(AgyThemeColors.Selected));
        Assert.False(string.IsNullOrEmpty(AgyThemeColors.Border));
    }

    [Fact]
    public void ThemeColors_GetColorHelpers_ShouldReturnValidSpectreColors()
    {
        var accent = AgyThemeColors.GetAccentColor();
        var secondary = AgyThemeColors.GetSecondaryColor();
        var selected = AgyThemeColors.GetSelectedColor();
        var border = AgyThemeColors.GetBorderColor();

        Assert.IsType<Color>(accent);
        Assert.IsType<Color>(secondary);
        Assert.IsType<Color>(selected);
        Assert.IsType<Color>(border);
    }
}
