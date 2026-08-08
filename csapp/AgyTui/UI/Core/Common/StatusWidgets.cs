using System.Text.Json;
using AgyTui.Infrastructure.Di;
using AgyTui.Infrastructure.Integrations.Ai.Abstractions;
using AgyTui.UI.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Rendering;

namespace AgyTui.UI.Core.Common;

public static class OllamaStatusWidgetCache
{
    private static readonly TtlCache<string, Table> _cache = new(TimeSpan.FromSeconds(30));

    public static Table? CachedOllamaWidget
    {
        get => _cache.Get("ollama_widget");
        set => _cache.Set("ollama_widget", value!);
    }

    public static void Invalidate()
    {
        _cache.Clear();
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
    private static readonly TtlCache<string, string> _ipCache = new(TimeSpan.FromMinutes(5));

    public IRenderable Render()
    {
        var cached = _ipCache.Get("ip");
        if (cached == null)
        {
            _ipCache.Set("ip", "Fetching...");
            Task.Run(async () =>
            {
                try
                {
                    var ip = await HttpClientProvider.Instance.Client.GetStringAsync("https://api.ipify.org");
                    _ipCache.Set("ip", ip.Trim());
                }
                catch
                {
                    _ipCache.Set("ip", "Unavailable");
                }
            });
            cached = "Fetching...";
        }
        return new Panel($"[bold green]🌐 Public IP:[/] [yellow]{cached.EscapeMarkup()}[/]")
        {
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Grey)
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
        catch { }

        var panel = new Panel(
            $"[bold green]🔑 SSH Connection Info[/]\n" +
            $"[dim]Host:[/] [yellow]{Environment.MachineName}[/]\n" +
            $"[dim]IP:[/] [yellow]{localIp}[/]\n" +
            $"[dim]User:[/] [yellow]{Environment.UserName}[/]\n" +
            $"[dim]Command:[/] [cyan]ssh {Environment.UserName}@{localIp}[/]"
        )
        {
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Grey)
        };

        return panel;
    }
}

public sealed class AccountTreeWidget : IStatusWidget
{
    private readonly IAgyAccountRepository? _accountRepo;

    public AccountTreeWidget(IAgyAccountRepository? accountRepo = null)
    {
        _accountRepo = accountRepo;
    }

    public string Alias => "account-tree";

    public IRenderable Render()
    {
        var tree = new Tree("[bold cyan]👤 Developer Accounts[/]");
        try
        {
            var accountRepo = _accountRepo;
            if (accountRepo != null)
            {
                var accounts = accountRepo.GetAccounts();
                var active = accountRepo.GetActiveAccount();

                foreach (var acc in accounts)
                {
                    var isAct = string.Equals(acc, active, StringComparison.OrdinalIgnoreCase);
                    var label = isAct ? $"[bold green]❯ {acc.EscapeMarkup()} (Active)[/]" : $"  [dim]{acc.EscapeMarkup()}[/]";
                    var node = tree.AddNode(label);
                    var meta = accountRepo.GetAccountMetadata(acc);
                    node.AddNode($"[dim]Quota Status: {meta.QuotaStatus}[/]");
                }
            }
        }
        catch
        {
            tree.AddNode("[dim]No account data available[/]");
        }
        return tree;
    }
}

public sealed class QuotaChartWidget : IStatusWidget
{
    private readonly IAgyAccountRepository? _accountRepo;

    public QuotaChartWidget(IAgyAccountRepository? accountRepo = null)
    {
        _accountRepo = accountRepo;
    }

    public string Alias => "quota-chart";

    public IRenderable Render()
    {
        var chart = new BreakdownChart().Width(60);
        try
        {
            var accountRepo = _accountRepo;
            if (accountRepo != null)
            {
                var accounts = accountRepo.GetAccounts();
                foreach (var acc in accounts)
                {
                    chart.AddItem(acc, 100, Color.Cyan1);
                }
            }
        }
        catch { }
        return chart;
    }
}

public sealed class LiveDashboardWidget : IStatusWidget
{
    public string Alias => "live-dashboard";

    public IRenderable Render()
    {
        var grid = new Grid();
        grid.AddColumn();
        grid.AddColumn();
        grid.AddRow(new DiskSpaceWidget().Render(), new PublicIpWidget().Render());
        grid.AddRow(new SshInfoWidget().Render(), new AccountTreeWidget().Render());
        return grid;
    }
}

public sealed class OllamaStatusWidget : IStatusWidget
{
    public string Alias => "ollama";

    public IRenderable Render()
    {
        var cached = OllamaStatusWidgetCache.CachedOllamaWidget;
        if (cached != null) return cached;

        var table = new Table().Border(TableBorder.Rounded).BorderColor(Color.Cyan1);
        table.AddColumn("[bold cyan]Model[/]");
        table.AddColumn("[bold cyan]Size[/]");
        table.AddColumn("[bold cyan]Family[/]");
        table.AddColumn("[bold cyan]Status[/]");

        try
        {
            var client = HttpClientProvider.Instance.Client;
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            var response = client.GetStringAsync("http://127.0.0.1:11434/api/tags", cts.Token).GetAwaiter().GetResult();
            using var doc = JsonDocument.Parse(response);
            if (doc.RootElement.TryGetProperty("models", out var modelsProp) && modelsProp.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in modelsProp.EnumerateArray())
                {
                    var name = item.GetProperty("name").GetString() ?? "";
                    var size = item.TryGetProperty("size", out var s) ? $"{Math.Round(s.GetInt64() / 1_073_741_824.0, 1)} GB" : "N/A";
                    var details = item.TryGetProperty("details", out var d) ? d : default;
                    var family = details.ValueKind != JsonValueKind.Undefined && details.TryGetProperty("family", out var f) ? f.GetString() ?? "N/A" : "N/A";

                    table.AddRow(name.EscapeMarkup(), size, family.EscapeMarkup(), "[green]Ready[/]");
                }
            }
        }
        catch
        {
            table.AddRow("[dim]Ollama Server[/]", "-", "-", "[red]Offline[/]");
        }

        OllamaStatusWidgetCache.CachedOllamaWidget = table;
        return table;
    }
}

public class StatusWidgetRegistryService : IStatusWidgetRegistry
{
    private readonly List<IStatusWidget> _widgets = new()
    {
        new DiskSpaceWidget(),
        new PublicIpWidget(),
        new SshInfoWidget(),
        new AccountTreeWidget(),
        new QuotaChartWidget(),
        new LiveDashboardWidget(),
        new OllamaStatusWidget()
    };

    public IEnumerable<IStatusWidget> GetAll() => _widgets;

    public IStatusWidget? GetByAlias(string alias)
    {
        return _widgets.FirstOrDefault(w => string.Equals(w.Alias, alias, StringComparison.OrdinalIgnoreCase));
    }
}

public static class StatusWidgetRegistry
{
    private static readonly IStatusWidgetRegistry _service = new StatusWidgetRegistryService();
    public static IStatusWidgetRegistry Instance => _service;

    public static IEnumerable<IStatusWidget> GetAll() => _service.GetAll();
    public static IStatusWidget? GetByAlias(string alias) => _service.GetByAlias(alias);
}
