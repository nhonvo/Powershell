using AgyTui.Infrastructure.Services;
using AgyTui.Infrastructure.Logging;
using AgyTui.UI.Core.Navigation.Interfaces;

namespace AgyTui.Tests.Unit.Infrastructure.Logging;

public class CommandLoggingMiddlewareTests
{
    private class DummyCommandRouter : ICommandRouter
    {
        public int ExpectedResult { get; set; } = 0;
        public string? LastAlias { get; private set; }
        public string[]? LastArgs { get; private set; }

        public int Execute(string alias, string[]? args = null)
        {
            LastAlias = alias;
            LastArgs = args;
            return ExpectedResult;
        }
    }

    [Fact]
    public void Execute_DelegatesToInnerRouterAndReturnsExitCode()
    {
        var dummy = new DummyCommandRouter { ExpectedResult = 42 };
        var middleware = new CommandLoggingMiddleware(dummy);

        var result = middleware.Execute("test-alias", new[] { "arg1", "arg2" });

        Assert.Equal(42, result);
        Assert.Equal("test-alias", dummy.LastAlias);
        Assert.Equal(new[] { "arg1", "arg2" }, dummy.LastArgs);
    }
}

