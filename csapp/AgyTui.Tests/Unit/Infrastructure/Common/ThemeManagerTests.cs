using AgyTui.Infrastructure.Common;
using Xunit;

namespace AgyTui.Tests.Unit.Infrastructure.Common;

public class ThemeManagerTests
{
    [Fact]
    public void SetMobileMode_ValidMode_ReturnsModeString()
    {
        IThemeManager manager = new ThemeManager();
        var tempDir = Path.Combine(Path.GetTempPath(), "agy_test_themes_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            var res = manager.SetMobileMode(tempDir, "desktop");
            Assert.NotNull(res);
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void ResolveStartupTheme_NonExistentDir_ReturnsDefaultTheme()
    {
        IThemeManager manager = new ThemeManager();
        var themeName = manager.ResolveStartupTheme("C:\\NonExistent_Directory_12345");
        Assert.NotNull(themeName);
        Assert.False(string.IsNullOrWhiteSpace(themeName));
    }

    [Fact]
    public void SetTheme_NonExistentTheme_ReturnsNull()
    {
        // Failure case: theme file does not exist
        IThemeManager manager = new ThemeManager();
        var tempDir = Path.Combine(Path.GetTempPath(), "agy_test_themes_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            var result = manager.SetTheme(tempDir, "non_existent_theme_9999");
            Assert.Null(result);
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void SelectThemeInteractive_NonExistentPath_ReturnsNull()
    {
        // Zero/Failure case: non-existent directory path
        IThemeManager manager = new ThemeManager();
        var result = manager.SelectThemeInteractive("C:\\NonExistent_Themes_Folder_XYZ", "neko");
        Assert.Null(result);
    }

    [Fact]
    public void SelectThemeInteractive_ZeroThemeFiles_ReturnsNull()
    {
        // Zero case: empty folder with zero theme files (.omp.json)
        IThemeManager manager = new ThemeManager();
        var tempDir = Path.Combine(Path.GetTempPath(), "agy_empty_themes_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            var result = manager.SelectThemeInteractive(tempDir, "neko");
            Assert.Null(result);
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }
    }
}
