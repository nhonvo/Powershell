using Xunit;
using AgyTui.UI.Core.Navigation;
using AgyTui.UI.Core.Navigation.Routers;
using AgyTui.Infrastructure.Integrations.Git;
using AgyTui.Infrastructure.Integrations.AgyClient;
using AgyTui.Infrastructure.Integrations.Ai.Services;
using AgyTui.Infrastructure.Services;
using Moq;
using AgyTui.Infrastructure.Integrations.AgyClient.Interfaces;
using AgyTui.Infrastructure.Integrations.Ai.Abstractions;
using AgyTui.Infrastructure.Persistence.Repositories;
using AgyTui.Infrastructure.Persistence.DbContext;

namespace AgyTui.Tests.Integration;

public class CommandRouterIntegrationTests
{
    private readonly GitCommandRouter _gitRouter;
    private readonly SystemCommandRouter _systemRouter;
    private readonly LearnCommandRouter _learnRouter;

    public CommandRouterIntegrationTests()
    {
        var mockGit = new Mock<IGitClient>();
        var mockAccountStore = new Mock<IAgyAccountStore>();
        var mockThemeManager = new Mock<IThemeManager>();

        _gitRouter = new GitCommandRouter(mockGit.Object);
        _systemRouter = new SystemCommandRouter(mockAccountStore.Object, mockThemeManager.Object);
        _learnRouter = new LearnCommandRouter();
    }

    [Theory]
    [InlineData("git-status")]
    [InlineData("git-branches")]
    [InlineData("gcmt")]
    public void GitCommandRouter_HandlesValidGitAliases(string alias)
    {
        var handled = _gitRouter.TryHandle(alias, Array.Empty<string>(), out var exitCode);
        Assert.True(handled);
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public void SystemCommandRouter_HandlesSysInfoAlias()
    {
        var handled = _systemRouter.TryHandle("sysinfo", Array.Empty<string>(), out var exitCode);
        Assert.True(handled);
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public void LearnCommandRouter_HandlesLearnAlias()
    {
        var handled = _learnRouter.TryHandle("learn", Array.Empty<string>(), out var exitCode);
        Assert.True(handled);
        Assert.Equal(0, exitCode);
    }
}
