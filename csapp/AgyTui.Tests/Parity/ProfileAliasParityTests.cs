using AgyTui.Core.Registries;
using AgyTui.UI.Core.Navigation;
using Xunit;

namespace AgyTui.Tests.Parity;

public class ProfileAliasParityTests
{
    [Fact]
    public void All_Registered_Aliases_Are_Valid_And_Have_Entries()
    {
        var allEntries = CommandRegistry.All;
        Assert.NotEmpty(allEntries);

        foreach (var entry in allEntries)
        {
            Assert.False(string.IsNullOrWhiteSpace(entry.Alias), "Command entry alias should not be null or empty");
            Assert.False(string.IsNullOrWhiteSpace(entry.DisplayName), $"DisplayName for {entry.Alias} should not be empty");
            Assert.False(string.IsNullOrWhiteSpace(entry.Description), $"Description for {entry.Alias} should not be empty");

            var resolved = CommandRegistry.GetByAlias(entry.Alias);
            Assert.NotNull(resolved);
        }
    }

    [Theory]
    [InlineData("gs")]
    [InlineData("gbr")]
    [InlineData("gcmt")]
    [InlineData("dbld")]
    [InlineData("dtst")]
    [InlineData("docker-health")]
    [InlineData("agyswitch")]
    [InlineData("reset-agy")]
    [InlineData("cnav")]
    [InlineData("dotnet-info")]
    [InlineData("purge-accounts")]
    public void Key_Profile_Aliases_Are_Registered_In_CommandRegistry(string alias)
    {
        var entry = CommandRegistry.GetByAlias(alias);
        Assert.NotNull(entry);
    }

    [Fact]
    public void Profile_Script_Contains_Key_Aliases()
    {
        var rootDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var profilePath = Path.Combine(rootDir, "Microsoft.PowerShell_profile.ps1");
        if (File.Exists(profilePath))
        {
            var content = File.ReadAllText(profilePath);
            Assert.Contains("Set-Alias -Name cnav", content);
            Assert.Contains("Set-Alias -Name reset-agy", content);
            Assert.Contains("Set-Alias -Name purge-accounts", content);
            Assert.Contains("Set-Alias -Name dotnet-info", content);
        }
    }
}
