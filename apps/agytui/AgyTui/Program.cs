using AgyTui.Infrastructure.Di;
using Microsoft.Extensions.DependencyInjection;

namespace AgyTui;

public static class Program
{
    public static int Main(string[] args)
    {
        int exitCode = RunApp(args);
        Environment.ExitCode = exitCode;
        return exitCode;
    }

    public static int RunApp(string[] args)
    {
        try
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.InputEncoding = Encoding.UTF8;
        }
        catch { }

        try
        {
            CommandRegistry.AssertSwitchCases();
            CommandRegistry.AssertAllAliasesReachable(MenuNodeBuilder.BuildTree());
        }
        catch (Exception ex)
        {
            LogHelper.LogError("CommandRegistry assertion failed", ex);
            AnsiConsole.WriteException(ex);
            return 1;
        }

        try
        {
            if (args.Length > 0)
            {
                LogHelper.Log($"[Program] RunApp starting with command: '{args[0]}'");
                return RunCommand(args[0], args.Skip(1).ToArray());
            }

            LogHelper.Log("[Program] RunApp starting CcNavigator.Run()");
            CcNavigator.Run();
            LogHelper.Log("[Program] CcNavigator.Run() completed cleanly.");
        }
        catch (Exception ex)
        {
            LogHelper.LogError("Unhandled exception in RunApp", ex);
            AnsiConsole.WriteException(ex);
            return 1;
        }

        try
        {
            AnsiConsole.Clear();
        }
        catch { }
        AnsiConsole.MarkupLine("[dim]Goodbye.[/]");
        return 0;
    }

    public static int RunCommand(string alias, string[]? args = null)
    {
        var router = Bootstrapper.ServiceProvider.GetRequiredService<ICommandRouter>();
        return router.Execute(alias, args);
    }
}
