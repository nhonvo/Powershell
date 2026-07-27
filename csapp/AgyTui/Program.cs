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
            AnsiConsole.WriteException(ex);
            return 1;
        }

        if (args.Length > 0)
        {
            return RunCommand(args[0], args.Skip(1).ToArray());
        }
        CcNavigator.Run();

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