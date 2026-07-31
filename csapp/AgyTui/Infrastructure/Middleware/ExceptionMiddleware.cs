using System;
using AgyTui.Domain.Common;
using AgyTui.UI.Core.Common;

namespace AgyTui.Infrastructure.Middleware;

public static class ExceptionMiddleware
{
    public static void Handle(Exception ex, string errorCodeMessage, string userTitle = "Error")
    {
        LogHelper.LogError($"[{errorCodeMessage}] {ex.Message}", ex);
    }

    public static T Execute<T>(Func<T> action, string errorCodeMessage, T fallbackValue = default!)
    {
        try
        {
            return action();
        }
        catch (Exception ex)
        {
            Handle(ex, errorCodeMessage);
            return fallbackValue;
        }
    }

    public static void Execute(Action action, string errorCodeMessage)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            Handle(ex, errorCodeMessage);
        }
    }
}
