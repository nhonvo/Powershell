using Spectre.Console;

namespace AgyTui;

public static class HotkeysGuide
{
    public static void Show()
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
        table.AddRow("", "[green]git-undo[/]", "/git-undo — Soft-reset last local commit");
        table.AddRow("", "[green]nexus[/]", "/nexus — Git Nexus multi-repo dashboard");

        table.AddRow("🐳 Docker & DB", "[bold green]cdk[/]", "/dkcl — Docker cleanup TUI dashboard");
        table.AddRow("", "[green]dcup[/]", "/dcup — docker compose up -d");
        table.AddRow("", "[green]dcdown[/]", "/dcdown — docker compose down");
        table.AddRow("", "[green]db-tui[/]", "/db-tui — SQLite database browser");

        table.AddRow("☁ AWS & Cloud", "[bold green]caws[/]", "/aws-local — LocalStack sandbox diagnostics");

        table.AddRow("🌐 System & Network", "[bold green]cnet[/]", "/public-ip — Public IP address & network status");
        table.AddRow("", "[bold green]csys[/]", "/disk — Disk space & system health");
        table.AddRow("", "[bold green]cssh[/]", "/ssh-info — SSH connection info & QR generator");
        table.AddRow("", "[green]kill-port[/]", "/kill-port — Kill process by port number");

        table.AddRow("🤖 AI Assistants", "[bold green]cai[/]", "/claude — Launch Claude Code CLI");
        table.AddRow("", "[green]codex[/]", "/codex — Launch Codex CLI");
        table.AddRow("", "[green]openclaw[/]", "/openclaw — Launch OpenClaw via Ollama");
        table.AddRow("", "[green]ollama-status[/]", "/ollama-status — Check local Ollama daemon");

        table.AddRow("👤 AGY Accounts", "[green]agyswitch[/]", "/agyswitch — Switch active account context");
        table.AddRow("", "[green]agyquota[/]", "/agyquota — View quota usage across accounts");
        table.AddRow("", "[green]autoswitch[/]", "/autoswitch — Toggle automatic workspace account switch");

        table.AddRow("📚 Learn & Study", "[green]learn[/]", "/learn — Interactive learning topic browser");
        table.AddRow("", "[green]vocab[/]", "/vocab — English vocabulary drill");
        table.AddRow("", "[green]kana[/]", "/kana — Japanese Kana quiz");
        table.AddRow("", "[green]kanji[/]", "/kanji — Kanji lookup & stroke detail");
        table.AddRow("", "[green]algo[/]", "/algo — Algorithm visualizer");

        table.AddRow("📈 Track & Progress", "[green]session[/]", "/session — Start Pomodoro study session");
        table.AddRow("", "[green]stats[/]", "/stats — Weekly study statistics");
        table.AddRow("", "[green]streak[/]", "/streak — Study streak display");

        table.AddRow("🎨 Theme & Settings", "[green]cc[/]", "/cc — Open Command Palette");
        table.AddRow("", "[green]theme[/]", "/theme — Select Shell theme");
        table.AddRow("", "[green]ui-mode[/]", "/ui-mode — Toggle three-pane / flat-tree layout");
        table.AddRow("", "[green]mobile-setup[/]", "/mobile-setup — Toggle mobile setup mode");

        AnsiConsole.Write(table);
    }
}
