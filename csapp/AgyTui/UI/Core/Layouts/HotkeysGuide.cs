using AgyTui.UI.Core.Layouts.Interfaces;
using Spectre.Console;

namespace AgyTui.UI.Core.Layouts;

public class HotkeysGuideService : IHotkeysGuide
{
    public void Show()
    {
        var table = new Table().Border(TableBorder.Rounded).BorderColor(Color.Cyan1);
        table.Title("[bold cyan]🛸 PowerShell Profile Hotkeys Guide[/]");
        table.AddColumn(new TableColumn("[bold yellow]Domain / Category[/]"));
        table.AddColumn(new TableColumn("[bold green]Shortcut[/]"));
        table.AddColumn(new TableColumn("[bold]Command & Description[/]"));

        table.AddRow("📁 Workspace & Dev", "[bold green]cnav[/]", "/proj — Navigate registered workspace");
        table.AddRow("", "[green]ide[/]", "/ide — Launch terminal IDE session");
        table.AddRow("", "[green]ide-diff[/]", "/ide-diff — Git diff viewer");
        table.AddRow("", "[green]dbld[/]", "/dbld — [[.NET]] Build project");
        table.AddRow("", "[green]dtst[/]", "/dtst — [[.NET]] Test project");

        table.AddRow("🌿 Git Operations", "[bold green]cg[/]", "/gs — Git status summary & conventional commit");
        table.AddRow("", "[green]gcmt[/]", "/gcmt — Conventional commit wizard");
        table.AddRow("", "[green]gbr[/]", "/gbr — Interactive branch selector");
        table.AddRow("", "[green]glog[/]", "/glog — Interactive commit graph log");
        table.AddRow("", "[green]nexus[/]", "/nexus — Repository graph & stats dashboard");

        table.AddRow("👤 Account & Auth", "[bold green]agyswitch[/]", "/agyswitch — Switch active developer account");
        table.AddRow("", "[green]agyquota[/]", "/agyquota — View token quota & usage stats");

        table.AddRow("🐳 Docker & System", "[bold green]cdk[/]", "/docker-health — Docker system health check");
        table.AddRow("", "[green]dkcl[/]", "/dkcl — Interactive container logs viewer");
        table.AddRow("", "[green]csys[/]", "/disk — System disk space analyzer");

        table.AddRow("📚 Learning & Study", "[bold green]learn[/]", "/stats — Study stats & progress dashboard");
        table.AddRow("", "[green]due[/]", "/due — Review due cards & items");
        table.AddRow("", "[green]weak[/]", "/weak — Practice weak items queue");
        table.AddRow("", "[green]streak[/]", "/streak — Study streak counter");

        table.AddRow("💎 Resources & Notes", "[bold green]obsidian[/]", "/obsidian — Open Obsidian markdown browser");

        table.AddRow("🎨 Theme & Config", "[bold green]theme[/]", "/theme — Interactive shell theme selector");
        table.AddRow("", "[green]help[/]", "/help — Open comprehensive profile help docs");

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[dim grey]Press any key to return to main menu...[/]");
        Console.ReadKey(true);
    }
}

public static class HotkeysGuide
{
    private static readonly IHotkeysGuide _service = new HotkeysGuideService();
    public static IHotkeysGuide Instance => _service;

    public static void Show() => _service.Show();
}
