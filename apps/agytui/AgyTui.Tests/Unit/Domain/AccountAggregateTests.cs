using Xunit;
using AgyTui.Domain.AccountContext;

namespace AgyTui.Tests.Unit.Domain;

public class AccountAggregateTests
{
    [Fact]
    public void Constructor_WithValidAccountName_InitializesCorrectly()
    {
        // Arrange & Act
        var account = new AccountAggregate("dev-account-1", "dev@example.com");

        // Assert
        Assert.Equal("dev-account-1", account.AccountName);
        Assert.Equal("dev@example.com", account.Email);
        Assert.False(account.IsActive);
        Assert.Equal("OK", account.QuotaStatus);
        Assert.Equal(0, account.UsageCount);
        Assert.Empty(account.RequestHistory);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyOrWhitespaceAccountName_ThrowsArgumentException(string invalidName)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new AccountAggregate(invalidName));
    }

    [Fact]
    public void MarkActive_And_MarkInactive_UpdatesActiveState()
    {
        // Arrange
        var account = new AccountAggregate("dev-account-2");

        // Act & Assert
        account.MarkActive();
        Assert.True(account.IsActive);

        account.MarkInactive();
        Assert.False(account.IsActive);
    }

    [Fact]
    public void SetQuotaExceeded_TogglesQuotaStatusString()
    {
        // Arrange
        var account = new AccountAggregate("dev-account-3");

        // Act & Assert
        account.SetQuotaExceeded(true);
        Assert.Equal("Exceeded", account.QuotaStatus);

        account.SetQuotaExceeded(false);
        Assert.Equal("OK", account.QuotaStatus);
    }

    [Fact]
    public void RecordUsage_IncrementsUsageCountAndTracksHistory()
    {
        // Arrange
        var account = new AccountAggregate("dev-account-4");
        var timestamp = "2026-08-08T23:55:00+07:00";

        // Act
        account.RecordUsage(timestamp);

        // Assert
        Assert.Equal(1, account.UsageCount);
        Assert.Equal(timestamp, account.LastUsed);
        Assert.Single(account.RequestHistory);
        Assert.Contains(timestamp, account.RequestHistory);
    }

    [Fact]
    public void ToMetadata_And_FromMetadata_RoundtripsState()
    {
        // Arrange
        var original = new AccountAggregate("dev-account-5", "test@test.com", true, "OK", "2026-08-08T12:00:00Z", 5, new[] { "ts1", "ts2" });

        // Act
        var metadata = original.ToMetadata();
        var reconstructed = AccountAggregate.FromMetadata("dev-account-5", metadata, "test@test.com", true);

        // Assert
        Assert.Equal(original.AccountName, reconstructed.AccountName);
        Assert.Equal(original.Email, reconstructed.Email);
        Assert.Equal(original.IsActive, reconstructed.IsActive);
        Assert.Equal(original.QuotaStatus, reconstructed.QuotaStatus);
        Assert.Equal(original.UsageCount, reconstructed.UsageCount);
        Assert.Equal(2, reconstructed.RequestHistory.Count);
    }
}
