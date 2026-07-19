using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace AgyTui;

public interface IStatusWidget
{
    string Alias { get; }
    IRenderable Render();
}

public static class OllamaStatusWidgetCache
{
    public static Table? CachedOllamaWidget = null;
    public static DateTime OllamaWidgetCachedAt = DateTime.MinValue;

    public static void Invalidate()
    {
        CachedOllamaWidget = null;
        OllamaWidgetCachedAt = DateTime.MinValue;
    }
}

public sealed class DiskSpaceWidget : IStatusWidget
{
    public string Alias => "disk";

    public IRenderable Render()
    {
        var table = new Table().Border(TableBorder.Rounded).BorderColor(Color.Cyan1);
        table.AddColumn("[bold cyan]Drive[/]");
        table.AddColumn("[bold cyan]Type[/]");
        table.AddColumn("[bold cyan]TotalSize[/]");
        table.AddColumn("[bold cyan]FreeSpace[/]");
        table.AddColumn("[bold cyan]Used%[/]");
        table.AddColumn("[bold cyan]Health[/]");

        foreach (var d in DriveInfo.GetDrives().Where(d => d.IsReady))
        {
            var usedPct = d.TotalSize > 0 ? Math.Round((1.0 - (double)d.AvailableFreeSpace / d.TotalSize) * 100.0, 1) : 0.0;
            var health = usedPct >= 90 ? "[red]Critical[/]" : usedPct >= 75 ? "[yellow]Warning[/]" : "[green]Healthy[/]";

            static string Fmt(long b) => b > 1_073_741_824 ? $"{Math.Round(b / 1_073_741_824.0, 2)} GB" : $"{Math.Round(b / 1_048_576.0, 2)} MB";
            table.AddRow(d.Name.EscapeMarkup(), d.DriveType.ToString().EscapeMarkup(), Fmt(d.TotalSize), Fmt(d.AvailableFreeSpace), $"{usedPct}%", health);
        }
        return table;
    }
}

public sealed class PublicIpWidget : IStatusWidget
{
    public string Alias => "public-ip";

    private static string _cachedIp = null;
    private static DateTime _lastIpFetch = DateTime.MinValue;

    public IRenderable Render()
    {
        if (_cachedIp == null || (DateTime.Now - _lastIpFetch).TotalMinutes > 5)
        {
            _cachedIp = "Fetching...";
            Task.Run(() => {
                try {
                    _cachedIp = SystemHelper.GetPublicIP();
                } catch {
                    _cachedIp = "Error fetching IP";
                }
                _lastIpFetch = DateTime.Now;
            });
        }
        return new Panel(new Markup($"\n[bold cyan]Public IP Address:[/] [green]{_cachedIp.EscapeMarkup()}[/]\n\n[dim](Refreshes every 5 mins)[/]"))
        {
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Cyan1)
        };
    }
}

public sealed class SshInfoWidget : IStatusWidget
{
    public string Alias => "ssh-info";

    public IRenderable Render()
    {
        var localIp = "127.0.0.1";
        try
        {
            var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    localIp = ip.ToString();
                    break;
                }
            }
        }
        catch {}

        var user = Environment.UserName;
        var hostName = Environment.MachineName;

        var rightSb = new StringBuilder();
        rightSb.AppendLine("[bold white]SSH Connection Info[/]");
        rightSb.AppendLine();
        rightSb.AppendLine("[dim]Local Network Address:[/]");
        rightSb.AppendLine($"  [yellow]ssh {user.ToLowerInvariant()}@{localIp}[/]");
        rightSb.AppendLine();
        rightSb.AppendLine("[dim]Bonjour Hostname Address:[/]");
        rightSb.AppendLine($"  [yellow]ssh {user.ToLowerInvariant()}@{hostName.ToLowerInvariant()}.local[/]");
        rightSb.AppendLine();
        rightSb.AppendLine("[dim]Ensure the Windows SSH service is running.[/]");
        return new Panel(new Markup(rightSb.ToString()))
        {
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Cyan1)
        };
    }
}

public sealed class AccountTreeWidget : IStatusWidget
{
    public string Alias => "account-tree";

    public IRenderable Render()
    {
        var accounts = AgyAccountCore.GetAccounts();
        var active = AgyAccountCore.GetActiveAccount();
        var tree = new Tree("[bold cyan]Account Tree[/]");
        foreach (var acc in accounts)
        {
            var stats = AgyAccountCore.GetAccountStats(acc);
            var displayName = acc;
            if (string.Equals(acc, "default", StringComparison.OrdinalIgnoreCase))
            {
                var email = AgyAccountCore.GetAccountEmail("default");
                if (!string.IsNullOrEmpty(email)) displayName = $"default ({email})";
            }
            var label = acc == active ? $"[green bold]★ {displayName.EscapeMarkup()} (Active)[/]" : displayName.EscapeMarkup();
            var node = tree.AddNode(label);
            node.AddNode($"[dim]Login:[/] {(stats.TokenStatus == "Logged In" ? "[green]Logged In[/]" : "[red]Not Logged In[/]")}");
            node.AddNode($"[dim]Convos:[/] {stats.ConversationsCount} [dim]Skills:[/] {stats.SkillsCount}");
            node.AddNode($"[dim]Weekly:[/] {(int)Math.Round(stats.GeminiWeekly)}% [dim]5h:[/] {(int)Math.Round(stats.GeminiFiveHour)}%");
            node.AddNode($"[dim]Size:[/] {stats.PrivateSize}");
        }
        return tree;
    }
}

public sealed class QuotaChartWidget : IStatusWidget
{
    public string Alias => "quota-chart";

    public IRenderable Render()
    {
        var accountName = AgyAccountCore.GetActiveAccount();
        var quota = AgyAccountCore.CalculateRollingQuotas(accountName);
        var chartLabel = accountName;
        if (string.Equals(accountName, "default", StringComparison.OrdinalIgnoreCase))
        {
            var email = AgyAccountCore.GetAccountEmail("default");
            if (!string.IsNullOrEmpty(email)) chartLabel = $"default ({email})";
        }
        var chart = new BarChart().Width(28).Label($"[bold cyan]{chartLabel.EscapeMarkup()} Quota Remaining %[/]").CenterLabel()
            .AddItem("Gemini W", quota.RemainingWeekly, Color.Cyan1)
            .AddItem("Gemini 5H", quota.Remaining5H, Color.Yellow)
            .AddItem("Claude W", 100.0, Color.Green)
            .AddItem("Claude 5H", 100.0, Color.Blue);

        var lines = new List<IRenderable>
        {
            chart,
            new Markup("\n"),
            new Markup($"[dim]Weekly: {quota.CountWeekly,4}/1000 reqs[/]"),
            new Markup($"[dim]5-Hour: {quota.Count5H,4}/50 reqs[/]"),
            new Markup($"[dim]Refreshes in {quota.TimeWeekly}[/]")
        };
        return new Rows(lines);
    }
}

public sealed class LiveDashboardWidget : IStatusWidget
{
    public string Alias => "live-dashboard";

    public IRenderable Render()
    {
        var table = new Table().Border(TableBorder.Rounded).BorderColor(Color.Grey);
        table.AddColumn("[bold cyan]Account[/]");
        table.AddColumn("[bold cyan]L[/]"); 
        table.AddColumn("[bold cyan]W[/]"); 
        table.AddColumn("[bold cyan]5h[/]"); 
        
        foreach (var a in AgyAccountCore.GetAccounts())
        {
            var s = AgyAccountCore.GetAccountStats(a);
            var act = AgyAccountCore.GetActiveAccount();
            var displayName = a;
            if (string.Equals(a, "default", StringComparison.OrdinalIgnoreCase))
            {
                var email = AgyAccountCore.GetAccountEmail("default");
                if (!string.IsNullOrEmpty(email)) displayName = $"default ({email})";
            }
            var n = a == act ? $"[green bold]* {displayName.EscapeMarkup()}[/]" : displayName.EscapeMarkup();
            var st = s.TokenStatus == "Logged In" ? "[green]●[/]" : "[red]○[/]";
            table.AddRow(n, st, $"{(int)Math.Round(s.GeminiWeekly)}%", $"{(int)Math.Round(s.GeminiFiveHour)}%");
        }
        return table;
    }
}

public sealed class OllamaStatusWidget : IStatusWidget
{
    public string Alias => "ollama-status";

    public IRenderable Render()
    {
        if (OllamaStatusWidgetCache.CachedOllamaWidget != null && (DateTime.UtcNow - OllamaStatusWidgetCache.OllamaWidgetCachedAt).TotalSeconds < 3)
        {
            return OllamaStatusWidgetCache.CachedOllamaWidget;
        }

        var isRunning = AgyAiCore.IsOllamaRunning();
        var table = new Table().Border(TableBorder.Rounded).BorderColor(isRunning ? Color.Green : Color.Red);
        table.AddColumn("[bold cyan]Ollama Daemon[/]");
        table.AddColumn("[bold cyan]Value[/]");
        
        table.AddRow("Status", isRunning ? "[green bold]● Active (Running)[/]" : "[red bold]○ Offline (Stopped)[/]");
        table.AddRow("Port", "11434");
        
        if (isRunning)
        {
            try
            {
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(1) };
                var response = client.GetStringAsync("http://127.0.0.1:11434/api/tags").Result;
                using var doc = JsonDocument.Parse(response);
                if (doc.RootElement.TryGetProperty("models", out var modelsProp) && modelsProp.ValueKind == JsonValueKind.Array)
                {
                    var modelNames = new List<string>();
                    foreach (var model in modelsProp.EnumerateArray())
                    {
                        if (model.TryGetProperty("name", out var nameProp))
                        {
                            modelNames.Add(nameProp.GetString() ?? "");
                        }
                    }
                    if (modelNames.Count > 0)
                    {
                        table.AddRow("Local Models", string.Join(", ", modelNames));
                    }
                    else
                    {
                        table.AddRow("Local Models", "[yellow]None pulled yet[/]");
                    }
                }
            }
            catch
            {
                table.AddRow("Local Models", "[red]Error listing models[/]");
            }
        }
        OllamaStatusWidgetCache.CachedOllamaWidget = table;
        OllamaStatusWidgetCache.OllamaWidgetCachedAt = DateTime.UtcNow;
        return table;
    }
}

public static class StatusWidgetRegistry
{
    private static readonly List<IStatusWidget> _widgets = new()
    {
        new DiskSpaceWidget(),
        new PublicIpWidget(),
        new SshInfoWidget(),
        new AccountTreeWidget(),
        new QuotaChartWidget(),
        new LiveDashboardWidget(),
        new OllamaStatusWidget()
    };

    public static IStatusWidget? GetByAlias(string alias)
    {
        return _widgets.FirstOrDefault(w => string.Equals(w.Alias, alias, StringComparison.OrdinalIgnoreCase));
    }
}
