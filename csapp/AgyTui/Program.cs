using System.Buffers;

namespace AgyTui;

public static class Program
{
    public static void Main(string[] args)
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
            Environment.Exit(1);
        }

        if (args.Length > 0)
        {
            RunCommand(args[0], args.Skip(1).ToArray());
            return;
        }
        CcNavigator.Run();

        try
        {
            AnsiConsole.Clear();
        }
        catch
        {
        }
        AnsiConsole.MarkupLine("[dim]Goodbye.[/]");
    }

    public static void RunCommand(string alias, string[]? args = null)
    {
        CommandRouter.Execute(alias, args);
    }
}