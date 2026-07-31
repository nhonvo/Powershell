using Xunit;
using AgyTui.Infrastructure.Middleware;
using AgyTui.Domain.Common;

namespace AgyTui.Tests.Unit;

public class ExceptionMiddlewareTests
{
    [Fact]
    public void ExceptionMiddleware_Execute_ReturnsFallbackOnException()
    {
        var result = ExceptionMiddleware.Execute<string>(() =>
        {
            throw new InvalidOperationException("Test exception");
        }, ErrorConstants.System.GeneralError, "fallback");

        Assert.Equal("fallback", result);
    }

    [Fact]
    public void ExceptionMiddleware_Execute_ReturnsValueOnSuccess()
    {
        var result = ExceptionMiddleware.Execute<string>(() => "success", ErrorConstants.System.GeneralError, "fallback");

        Assert.Equal("success", result);
    }
}
