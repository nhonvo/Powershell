using System;

using System.Buffers;

using System.Collections.Frozen;

using System.Collections.Generic;

using System.Diagnostics;

using System.IO;

using System.Linq;

using System.Net;

using System.Net.Http;

using System.Net.NetworkInformation;

using System.Net.Sockets;

using System.Runtime.InteropServices;

using System.Security.Cryptography;

using System.Text;

using System.Text.Json;

using System.Text.Json.Serialization;

using System.Text.RegularExpressions;

using System.Threading;

using Spectre.Console;

namespace AgyTui;

public static class SpectreMenu
{
    public static int Show(string header, string[]items, int defaultIndex) => CoreShow([header], items, [], false, false);

    public static int Show(string header, string[]items, int defaultIndex, bool searchEnabled) => CoreShow([header], items, [], searchEnabled, false);

    public static int ShowRobust(string[]headerLines, string[]items, int defaultIndex, bool searchEnabled, bool fullScreen) => CoreShow(headerLines, items, [], searchEnabled, fullScreen);

    public static int Show(string[]headerLines, string[]items, string[]cmds, int defaultIndex, bool searchEnabled, bool fullScreen)
    {
        if (items.Length==0)return-1;
        if (fullScreen)AnsiConsole.Clear();
        PrintHeader(headerLines);
        if (cmds.Length>defaultIndex&&!string.IsNullOrWhiteSpace(cmds[defaultIndex]))AnsiConsole.Write(new Panel($"[dim]{cmds[defaultIndex].EscapeMarkup()}[/]")
        {
            Header=new PanelHeader("[grey]Command[/]"), Border=BoxBorder.Rounded
        }
        );
        return PromptIndex(items, searchEnabled);

    }

    public static string?ShowDynamic(string header, Func<string, string[]>resolver, int defaultIndex) => ShowDynamic(header, resolver, defaultIndex, string.Empty);

    public static string?ShowDynamic(string header, Func<string, string[]>resolver, int defaultIndex, string initialFilter)
    {
        var items=resolver(initialFilter);
        if (items.Length==0)return null;
        PrintHeader([header]);

        try
        {
            return AnsiConsole.Prompt(BuildPrompt(items, true));
        }
        catch
        {
            return null;
        }

    }

    public static void InitializeTuiColors()
    {
    }

    private static int CoreShow(string[]headerLines, string[]items, string[]cmds, bool searchEnabled, bool fullScreen)
    {
        if (items.Length==0)return-1;
        if (fullScreen)AnsiConsole.Clear();
        PrintHeader(headerLines);
        return PromptIndex(items, searchEnabled);

    }

    private static void PrintHeader(string[]lines)
    {
        foreach (var line in lines.Where(l => !string.IsNullOrWhiteSpace(l)))AnsiConsole.Write(new Rule($"[bold cyan]{line.EscapeMarkup()}[/]").RuleStyle("grey"));

    }

    private static int PromptIndex(string[]items, bool searchEnabled)
    {
        var prompt=BuildPrompt(items, searchEnabled);

        try
        {
            return Array.IndexOf(items, AnsiConsole.Prompt(prompt));
        }
        catch
        {
            return-1;
        }

    }

    private static SelectionPrompt<string>BuildPrompt(string[]items, bool searchEnabled)
    {
        var pageSize=Math.Min(15, Math.Max(5, Console.WindowHeight-8));
        var prompt=new SelectionPrompt<string>().PageSize(pageSize).HighlightStyle(new Style(Color.Green));
        if (searchEnabled)prompt.SearchEnabled=true;
        prompt.AddChoices(items);
        return prompt;

    }

}
public static class SpectrePager
{
    public static void Show(string title, string[]lines)
    {
        var pageSize=Math.Max(5, Console.WindowHeight-8);
        var totalLines=lines.Length;
        var top=0;
        Console.CursorVisible=false;

        try
        {
            while (true)
            {
                AnsiConsole.Clear();
                AnsiConsole.Write(new Rule($"[bold cyan]{title.EscapeMarkup()}[/]").RuleStyle("grey"));
                for (var i=top;
                i<Math.Min(top+pageSize, totalLines);
                i++)AnsiConsole.MarkupLine(lines[i].EscapeMarkup());
                for (var p=Math.Min(top+pageSize, totalLines);
                p<top+pageSize;
                p++)AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine($"[dim] ↑↓/jk scroll  d/u page  g/G ends  / search  q quit"+$"  ({top + 1}–{Math.Min(top + pageSize, totalLines)} of {totalLines})[/]");
                var key=Console.ReadKey(true);
                switch (key.Key)
                {
                    case ConsoleKey.DownArrow:case ConsoleKey.J:if (top+pageSize<totalLines)top++;
                    break;
                    case ConsoleKey.UpArrow:case ConsoleKey.K:if (top>0)top--;
                    break;
                    case ConsoleKey.PageDown:case ConsoleKey.D:top=Math.Min(totalLines-pageSize, top+pageSize);
                    if (top<0)top=0;
                    break;
                    case ConsoleKey.PageUp:case ConsoleKey.U:top=Math.Max(0, top-pageSize);
                    break;
                    case ConsoleKey.Home:case ConsoleKey.G when key.Modifiers==ConsoleModifiers.Shift:top=0;
                    break;
                    case ConsoleKey.End:case ConsoleKey.G:top=Math.Max(0, totalLines-pageSize);
                    break;
                    case ConsoleKey.Oem2:case ConsoleKey.F:Console.CursorVisible=true;
                    AnsiConsole.Markup("[cyan]Search: [/]");
                    var q=Console.ReadLine()??"";
                    Console.CursorVisible=false;
                    if (!string.IsNullOrWhiteSpace(q))
                    {
                        var hit=Array.FindIndex(lines, top+1, l => l.Contains(q, StringComparison.OrdinalIgnoreCase));
                        if (hit>=0)top=Math.Max(0, hit-2);

                        else AnsiConsole.MarkupLine("[yellow]Not found[/]");
                    }
                    break;
                    case ConsoleKey.Escape:case ConsoleKey.Enter:case ConsoleKey.Q:return;
                }
            }
        }
        finally
        {
            Console.CursorVisible=true;
        }

    }

}
public static class SpectrePanel
{
    public static void Success(string message) => Render(message, Color.Green,"✓ Success");

    public static void Error(string message) => Render(message, Color.Red,"✗ Error");

    public static void Warning(string message) => Render(message, Color.Yellow,"⚠ Warning");

    public static void Info(string message) => Render(message, Color.Cyan1,"ℹ Info");

    private static void Render(string message, Color border, string header) => AnsiConsole.Write(new Panel(message.EscapeMarkup())
    {
        Header=new PanelHeader($"[bold]{header}[/]"), Border=BoxBorder.Rounded, BorderStyle=new Style(border), Padding=new Padding(1, 0)

    }
    );

}
public static class SpectreProgress
{
    public static void Spinner(string message, Action action) => AnsiConsole.Status().Spinner(Spectre.Console.Spinner.Known.Dots).SpinnerStyle(new Style(Color.Yellow)).Start(message, _ => action());

    public static void BulkProgress(string label, string[]items, Action<int, string>action) => AnsiConsole.Progress().AutoClear(false).Columns(new TaskDescriptionColumn(), new ProgressBarColumn(), new PercentageColumn(), new ElapsedTimeColumn()).Start(ctx =>
    {
        var task=ctx.AddTask($"[green]{label.EscapeMarkup()}[/]", maxValue:items.Length);
        for (var i=0;
        i<items.Length;
        i++)
        {
            task.Description=$"[green]{label.EscapeMarkup()}: {items[i].EscapeMarkup()}[/]";
            action(i, items[i]);
            task.Increment(1);
        }

    }
    );

}
public static class SpectreTable
{
    public static void Render(string[]columns, string[][]rows, bool markup=false)
    {
        var t=new Table
        {
            Border=TableBorder.Rounded
        }
        ;
        foreach (var col in columns)t.AddColumn(new TableColumn($"[bold]{col.EscapeMarkup()}[/]"));
        foreach (var row in rows)t.AddRow(markup?row:row.Select(c => c.EscapeMarkup()).ToArray());
        AnsiConsole.Write(t);

    }

    public static void Live(string[]columns, Func<string[][]>dataSource, int refreshMs=5000)
    {
        var t=new Table
        {
            Border=TableBorder.Rounded
        }
        ;
        foreach (var col in columns)t.AddColumn(new TableColumn($"[bold]{col.EscapeMarkup()}[/]"));
        AnsiConsole.Live(t).Start(ctx =>
        {
            while (true)
            {
                t.Rows.Clear();
                foreach (var row in dataSource())t.AddRow(row);
                ctx.Refresh();
                if (Console.KeyAvailable)break;
                Thread.Sleep(Math.Min(refreshMs, 500));
            }
            Console.ReadKey(true);
        }
        );

    }

    public static void ThreePane(string leftTitle, string[]leftItems, int leftSelected, string midTitle, string[]midItems, int midSelected, string rightTitle, string[]rightItems)
    {
        AnsiConsole.Write(new Columns(BuildPane(leftTitle, leftItems, leftSelected), BuildPane(midTitle, midItems, midSelected), BuildPane(rightTitle, rightItems,-1)));

    }

    private static Panel BuildPane(string title, string[]items, int selected)
    {
        var sb=new StringBuilder();
        for (var i=0;
        i<items.Length;
        i++)sb.AppendLine(i==selected?$"[green bold]> {items[i].EscapeMarkup()}[/]":$" {items[i].EscapeMarkup()}");
        return new Panel(sb.ToString())
        {
            Header=new PanelHeader($"[bold cyan]{title.EscapeMarkup()}[/]"), Border=BoxBorder.Rounded
        }
        ;

    }

}
public sealed class AccountMetadata
{
    [JsonPropertyName("LastUsed")]public string LastUsed
    {
        get;
        set;

    }
    ="Never";

    [JsonPropertyName("UsageCount")]public int UsageCount
    {
        get;
        set;

    }
    [JsonPropertyName("QuotaStatus")]public string QuotaStatus
    {
        get;
        set;

    }
    ="OK";

    [JsonPropertyName("RequestHistory")]public List<string>RequestHistory
    {
        get;
        set;

    }
    =[];

}

public sealed record QuotaMetrics(double RemainingWeekly, double Remaining5H, string TimeWeekly, string Time5H, int CountWeekly, int Count5H);

public sealed record AccountStats(string LastUsed, int UsageCount, string PrivateSize, string JunctionStatus, int SkillsCount, int ConversationsCount, string TokenStatus, string QuotaStatus, double GeminiWeekly, double GeminiFiveHour);

public static class AgyAccountCore
{
    public static TimeProvider Clock
    {
        get;
        set;

    }
    =TimeProvider.System;
    public static readonly string AgySourceHome=@"C:\Users\Public\.gemini";
    public static readonly string AgyAccountPrefix=@"C:\Users\Public\.gemini_";

    public static string AgyActiveAccountFile => Path.Combine(AgySourceHome,"active_account.txt");
    private static bool?_networkOnline;

    private static readonly Lock _networkLock=new();

    public static bool CheckNetworkStatus()
    {
        lock (_networkLock)
        {
            if (_networkOnline.HasValue)return _networkOnline.Value;

            try
            {
                if (!NetworkInterface.GetIsNetworkAvailable())
                {
                    _networkOnline=false;
                    return false;
                }
                using var ping=new Ping();
                _networkOnline=ping.Send("8.8.8.8", 200).Status==IPStatus.Success;
            }
            catch
            {
                _networkOnline=false;
            }
            return _networkOnline.Value;
        }

    }

    public static string[]GetAccounts()
    {
        var accounts=new List<string>
        {
            "default"
        }
        ;
        var scanPaths=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var userProfile=Environment.GetEnvironmentVariable("USERPROFILE")??"";
        if (Directory.Exists(userProfile))scanPaths.Add(userProfile);
        var prefixParent=Path.GetDirectoryName(AgyAccountPrefix);
        if (prefixParent!=null&&Directory.Exists(prefixParent))scanPaths.Add(prefixParent);
        foreach (var scanPath in scanPaths)
        {
            foreach (var dir in Directory.GetDirectories(scanPath,".gemini_*"))
            {
                var m=Regex.Match(Path.GetFileName(dir),@"^\.gemini_(.+)$");
                if (!m.Success)continue;
                var name=m.Groups[1].Value;
                if (!Regex.IsMatch(name,"backup|copy|temp", RegexOptions.IgnoreCase)&&!accounts.Contains(name, StringComparer.OrdinalIgnoreCase))accounts.Add(name);
            }
        }
        return[..accounts];

    }

    public static string GetActiveAccount()
    {
        var home=Environment.GetEnvironmentVariable("GEMINI_HOME")??"";
        var m=Regex.Match(home,@"\.gemini_(.+)$");
        return m.Success?m.Groups[1].Value:"default";

    }

    public static string GetAccountDirectory(string accountName)
    {
        if (string.Equals(accountName,"default", StringComparison.OrdinalIgnoreCase))return AgySourceHome;
        var target=AgyAccountPrefix+accountName;
        if (!Directory.Exists(target))
        {
            var alt=Path.Combine(Environment.GetEnvironmentVariable("USERPROFILE")??"",".gemini_"+accountName);
            if (Directory.Exists(alt))return alt;
        }
        return target;

    }

    public static AccountMetadata GetAccountMetadata(string accountName)
    {
        var file=Path.Combine(GetAccountDirectory(accountName),"account_metadata.json");
        if (File.Exists(file))
        {
            try
            {
                var raw=File.ReadAllText(file);
                if (!string.IsNullOrWhiteSpace(raw))
                {
                    var meta=JsonSerializer.Deserialize<AccountMetadata>(raw);
                    if (meta!=null)return meta;
                }
            }
            catch
            {
            }
        }
        return new AccountMetadata();

    }

    public static void UpdateAccountMetadata(string accountName)
    {
        var dir=GetAccountDirectory(accountName);

        try
        {
            Directory.CreateDirectory(dir);
        }
        catch
        {
            return;
        }
        var meta=GetAccountMetadata(accountName);
        meta.LastUsed=Clock.GetLocalNow().ToString("yyyy-MM-ddTHH:mm:sszzz");
        meta.UsageCount++;
        var now=Clock.GetUtcNow().UtcDateTime;
        var cutoff=now.AddDays(-7);
        meta.RequestHistory.Add(now.ToString("o"));
        meta.RequestHistory=meta.RequestHistory.Where(ts => DateTime.TryParse(ts, null, System.Globalization.DateTimeStyles.RoundtripKind, out var dt)&&dt>=cutoff).ToList();

        try
        {
            var json=JsonSerializer.Serialize(meta, new JsonSerializerOptions
            {
                WriteIndented=true
            }
            );
            File.WriteAllText(Path.Combine(dir,"account_metadata.json"), json, Encoding.UTF8);
        }
        catch
        {
        }

    }

    public static void SetAccountQuotaExceeded(string accountName, bool exceeded)
    {
        var dir=GetAccountDirectory(accountName);
        if (!Directory.Exists(dir))return;
        var meta=GetAccountMetadata(accountName);
        var newStatus=exceeded?"Exceeded":"OK";
        if (meta.QuotaStatus==newStatus)return;
        meta.QuotaStatus=newStatus;

        try
        {
            var json=JsonSerializer.Serialize(meta, new JsonSerializerOptions
            {
                WriteIndented=true
            }
            );
            File.WriteAllText(Path.Combine(dir,"account_metadata.json"), json, Encoding.UTF8);
        }
        catch
        {
        }

    }

    public static QuotaMetrics CalculateRollingQuotas(string accountName)
    {
        var history=GetAccountMetadata(accountName).RequestHistory;
        var now=Clock.GetUtcNow().UtcDateTime;
        var fiveHoursAgo=now.AddHours(-5);
        var sevenDaysAgo=now.AddDays(-7);
        int reqs5H=0, reqsWeekly=0;
        var oldest5H=now;
        var oldestWeekly=now;
        foreach (var ts in history)
        {
            if (!DateTime.TryParse(ts, null, System.Globalization.DateTimeStyles.RoundtripKind, out var dt))continue;
            if (dt>=fiveHoursAgo)
            {
                reqs5H++;
                if (dt<oldest5H)oldest5H=dt;
            }
            if (dt>=sevenDaysAgo)
            {
                reqsWeekly++;
                if (dt<oldestWeekly)oldestWeekly=dt;
            }
        }
        const int limit5H=50, limitWeekly=1000;
        var remaining5H=Math.Max(0.0, 100.0-Math.Round((reqs5H/(double)limit5H)*100.0, 2));
        var remainingWeekly=Math.Max(0.0, 100.0-Math.Round((reqsWeekly/(double)limitWeekly)*100.0, 2));
        var secs5H=Math.Max(0, (int)Math.Round((oldest5H.AddHours(5)-now).TotalSeconds));
        var secsWeekly=Math.Max(0, (int)Math.Round((oldestWeekly.AddDays(7)-now).TotalSeconds));

        static string Fmt(int s) => $"{s / 3600}h {(s % 3600) / 60}m";
        return new QuotaMetrics(remainingWeekly, remaining5H, Fmt(secsWeekly), Fmt(secs5H), reqsWeekly, reqs5H);

    }

    public static bool IsAutoSwitchEnabled()
    {
        var file=Path.Combine(AgySourceHome,"auto_switch_enabled.txt");
        if (!File.Exists(file))return true;

        try
        {
            return File.ReadAllText(file).Trim()!="False";
        }
        catch
        {
            return true;
        }

    }

    public static void ToggleAutoSwitch()
    {
        var current=IsAutoSwitchEnabled();

        try
        {
            Directory.CreateDirectory(AgySourceHome);
            File.WriteAllText(Path.Combine(AgySourceHome,"auto_switch_enabled.txt"), current?"False":"True", Encoding.UTF8);
            SpectrePanel.Info($"Auto-Switch is now: {(current ? "Disabled" : "Enabled")}");
        }
        catch
        {
            SpectrePanel.Error("Failed to update Auto-Switch setting.");
        }

    }

    public static string?FindAutoSwitchCandidate()
    {
        if (!IsAutoSwitchEnabled())return null;
        var active=GetActiveAccount();
        if (GetAccountMetadata(active).QuotaStatus!="Exceeded")return null;
        foreach (var acc in GetAccounts())
        {
            if (string.Equals(acc, active, StringComparison.OrdinalIgnoreCase))continue;
            var tokenFile=Path.Combine(GetAccountDirectory(acc),"keyring_token.txt");
            if (!File.Exists(tokenFile))continue;
            var quota=GetAccountMetadata(acc).QuotaStatus??"OK";
            if (quota=="OK")return acc;
        }
        return null;

    }

    public static bool CheckQuotaAfterRun(string accountName)
    {
        try
        {
            var brainDir=Path.Combine(AgySourceHome,"antigravity","brain");
            if (!Directory.Exists(brainDir))return false;
            var latest=new DirectoryInfo(brainDir).EnumerateFiles("transcript.jsonl", SearchOption.AllDirectories).OrderByDescending(f => f.LastWriteTime).FirstOrDefault();
            if (latest==null)return false;
            if ((DateTime.Now-latest.LastWriteTime).TotalSeconds>60)return false;
            var tail=File.ReadLines(latest.FullName).TakeLast(15);
            var quotaErr=tail.Any(line => Regex.IsMatch(line,@"RESOURCE_EXHAUSTED|quota exceeded|quotaExceeded|ResourceExhausted|quota limit")&&Regex.IsMatch(line,@"""status""\s*:\s*""ERROR""|""code""\s*:\s*429"));
            SetAccountQuotaExceeded(accountName, quotaErr);
            return quotaErr;
        }
        catch
        {
            return false;
        }

    }

    public static long GetPrivateDirectorySize(string path)
    {
        if (!Directory.Exists(path))return 0;
        long total=0;

        try
        {
            foreach (var file in Directory.EnumerateFiles(path,"*", SearchOption.AllDirectories))
            {
                bool inJunction=false;
                var parent=Path.GetDirectoryName(file);
                while (parent!=null&&parent.Length>=path.Length)
                {
                    var di=new DirectoryInfo(parent);
                    if (di.Exists&&di.LinkTarget!=null)
                    {
                        inJunction=true;
                        break;
                    }
                    parent=Path.GetDirectoryName(parent);
                }
                if (!inJunction)total+=new FileInfo(file).Length;
            }
        }
        catch
        {
        }
        return total;

    }

    public static string GetJunctionStatus(string accountName)
    {
        if (string.Equals(accountName,"default", StringComparison.OrdinalIgnoreCase))return"Healthy (Primary)";
        var destDir=GetAccountDirectory(accountName);
        if (!Directory.Exists(destDir))return"Uninitialized";
        var shared=new[]
        {
            "antigravity","antigravity-cli","config","history","antigravity-ide","wf"
        }
        ;
        foreach (var sub in shared)
        {
            var subPath=Path.Combine(destDir, sub);
            if (!Directory.Exists(subPath))return"Needs Repair";
            if (new DirectoryInfo(subPath).LinkTarget==null)return"Needs Repair";
        }
        return"Healthy";

    }

    public static AccountStats GetAccountStats(string accountName)
    {
        var meta=GetAccountMetadata(accountName);
        var dir=GetAccountDirectory(accountName);
        var privateSize=GetPrivateDirectorySize(dir);
        var junctionStatus=GetJunctionStatus(accountName);
        int skillsCount=0, convCount=0;
        var skillsPath=Path.Combine(AgySourceHome,"config","skills");
        if (Directory.Exists(skillsPath))skillsCount=Directory.GetDirectories(skillsPath).Length;
        var convPath=Path.Combine(AgySourceHome,"antigravity","brain");
        if (Directory.Exists(convPath))convCount=Directory.GetDirectories(convPath).Length;
        var tokenStatus=File.Exists(Path.Combine(dir,"keyring_token.txt"))?"Logged In":"Not Logged In";
        string sizeStr;
        if (privateSize>1_048_576)sizeStr=$"{Math.Round(privateSize / 1_048_576.0, 2)} MB";

        else if (privateSize>1_024)sizeStr=$"{Math.Round(privateSize / 1_024.0, 2)} KB";

        else sizeStr=$"{privateSize} B";
        var quota=CalculateRollingQuotas(accountName);
        return new AccountStats(meta.LastUsed, meta.UsageCount, sizeStr, junctionStatus, skillsCount, convCount, tokenStatus, meta.QuotaStatus, quota.RemainingWeekly, quota.Remaining5H);

    }

    public static string GetProgressBar(double percentage)
    {
        const int total=50;
        int filled=Math.Min(total, Math.Max(0, (int)Math.Round((percentage/100.0)*total)));
        var bar=new string('█', filled)+new string('░', total-filled);
        return$" [{bar}] {Math.Round(percentage, 2):F2}%";

    }

    public static string[]GetUsageLines(string accountName)
    {
        var meta=GetAccountMetadata(accountName);
        var quota=CalculateRollingQuotas(accountName);
        double geminiWeekly=quota.RemainingWeekly;
        double geminiFiveHour=quota.Remaining5H;
        double claudeWeekly=100.0;
        double claudeFiveHour=100.0;
        var bar=new string('─', 140);
        var lines=new List<string>
        {
            bar,">", bar,"└ Models & Quota","",$" Account: {accountName}","","GEMINI MODELS"," Models within this group: Gemini Flash, Gemini Pro",""," Weekly Limit", GetProgressBar(geminiWeekly),$" {(int)Math.Round(geminiWeekly)}% remaining · Refreshes in {quota.TimeWeekly}",""," Five Hour Limit", GetProgressBar(geminiFiveHour), geminiFiveHour>=100.0?" Quota available":$" {(int)Math.Round(geminiFiveHour)}% remaining · Refreshes in {quota.Time5H}","","","CLAUDE AND GPT MODELS"," Models within this group: Claude Opus, Claude Sonnet, GPT-OSS",""," Weekly Limit", GetProgressBar(claudeWeekly), claudeWeekly>=100.0?" Quota available":$" {(int)Math.Round(claudeWeekly)}% remaining",""," Five Hour Limit", GetProgressBar(claudeFiveHour), claudeFiveHour>=100.0?" Quota available":$" {(int)Math.Round(claudeFiveHour)}% remaining","",""," │ Within each group, models share a weekly limit and a 5-hour limit. Quota is"," │ consumed proportionally to the cost of the tokens. The 5-hour limit smooths"," │ out aggregate demand to fairly distribute global capacity across all users.",""," Weekly Request Distribution (Last 7 Days)"," ==========================================="
        }
        ;
        var now=Clock.GetLocalNow().DateTime;
        var dayData=new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i=0;
        i<7;
        i++)dayData[now.Date.AddDays(-i).ToString("ddd")]=0;
        foreach (var ts in meta.RequestHistory)
        {
            if (!DateTime.TryParse(ts, null, System.Globalization.DateTimeStyles.RoundtripKind, out var dt))continue;
            var key=dt.ToLocalTime().ToString("ddd");
            if (dayData.ContainsKey(key))dayData[key]++;
        }
        for (int i=6;
        i>=0;
        i--)
        {
            var date=now.Date.AddDays(-i);
            var day=date.ToString("ddd");
            var count=dayData.GetValueOrDefault(day);
            var bar2=new string('█', Math.Min(10, count))+new string('░', Math.Max(0, 10-count));
            lines.Add($" {day} [{bar2}] {count} requests");
        }
        lines.Add(" -------------------------------------------");
        lines.Add($" Total Weekly Requests: {quota.CountWeekly} / 1000 limit");
        return[..lines];

    }

    public static void ShowAccounts()
    {
        var accounts=GetAccounts();
        var active=GetActiveAccount();
        var rows=accounts.Select(a => new[]
        {
            a==active?"[green bold]*[/]":" ", a.EscapeMarkup(), GetAccountDirectory(a).EscapeMarkup(), File.Exists(Path.Combine(GetAccountDirectory(a),"keyring_token.txt"))?"[green]Logged In[/]":"[dim]Not Logged In[/]"
        }
        ).ToArray();
        SpectreTable.Render(["","Account","Directory","Status"], rows, markup:true);

    }

    public static void ShowAllAccountsSummary()
    {
        var accounts=GetAccounts();
        var active=GetActiveAccount();
        var rows=accounts.Select(acc =>
        {
            var stats=GetAccountStats(acc);
            var nameCell=(acc==active?"[green bold]* ":" ")+acc.EscapeMarkup()+(acc==active?"[/]":"");
            var quotaStr=stats.TokenStatus=="Logged In"?(stats.QuotaStatus=="Exceeded"?"[red]Exceeded[/]":$"[cyan]{(int)Math.Round(stats.GeminiWeekly)}%[/] / [cyan]{(int)Math.Round(stats.GeminiFiveHour)}%[/]"):"[dim]--[/]";
            var lastUsed=stats.LastUsed.Length>=10&&stats.LastUsed!="Never"?stats.LastUsed[..10]:"Never";
            return new[]
            {
                nameCell, stats.TokenStatus=="Logged In"?"[green]Logged In[/]":"[dim]Not Logged In[/]", quotaStr, stats.UsageCount.ToString(), lastUsed, stats.PrivateSize
            }
            ;
        }
        ).ToArray();
        SpectreTable.Render(["Account","Status","Quota W / 5h","Uses","Last Used","Size"], rows, markup:true);

    }

}

public sealed record WorkspaceEntry(string Name, [property:JsonPropertyName("Path")]string WorkspacePath, string?AssociatedAccount, string[]?Tags);

public static class WorkspaceRegistry
{
    private static readonly string ConfigFile=System.IO.Path.Combine(AgyAccountCore.AgySourceHome,"workspaces.json");

    public static WorkspaceEntry[]GetWorkspaces()
    {
        if (!File.Exists(ConfigFile))return[];

        try
        {
            var raw=File.ReadAllText(ConfigFile);
            return JsonSerializer.Deserialize<WorkspaceEntry[]>(raw)??[];
        }
        catch
        {
            return[];
        }

    }

    public static void SaveWorkspaces(WorkspaceEntry[]entries)
    {
        try
        {
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(ConfigFile)!);
            File.WriteAllText(ConfigFile, JsonSerializer.Serialize(entries, new JsonSerializerOptions
            {
                WriteIndented=true
            }
            ), Encoding.UTF8);
        }
        catch (Exception ex)
        {
            SpectrePanel.Error($"Failed to save workspaces: {ex.Message}");
        }

    }

    public static WorkspaceEntry[]FindByQuery(string query)
    {
        var all=GetWorkspaces();
        if (string.IsNullOrWhiteSpace(query))return all;

        try
        {
            return all.Where(w => Regex.IsMatch(w.Name, query, RegexOptions.IgnoreCase)||Regex.IsMatch(w.WorkspacePath, query, RegexOptions.IgnoreCase)).ToArray();
        }
        catch
        {
            return[];
        }

    }

    public static WorkspaceEntry[]GetByAccount(string accountName) => GetWorkspaces().Where(w => string.Equals(w.AssociatedAccount, accountName, StringComparison.OrdinalIgnoreCase)).ToArray();

}
public static class ProfileNavigator
{
    public static string?Navigate(string query) => Navigate(query, WorkspaceRegistry.GetWorkspaces());

    public static string?Navigate(string query, WorkspaceEntry[]workspaces)
    {
        if (workspaces.Length==0)
        {
            SpectrePanel.Warning("No workspaces registered.");
            return null;
        }
        WorkspaceEntry[]matches;
        if (string.IsNullOrWhiteSpace(query))matches=workspaces;

        else
        {
            matches=WorkspaceRegistry.FindByQuery(query);
            if (matches.Length==0)
            {
                SpectrePanel.Warning($"No workspace matched '{query}'.");
                return null;
            }
        }
        if (matches.Length==1)return matches[0].WorkspacePath;
        var idx=SpectreMenu.Show("Navigate to Workspace", matches.Select(m => m.Name).ToArray(), 0, true);
        return idx>=0?matches[idx].WorkspacePath:null;

    }

}
public static class SystemHelper
{
    public static void ShowDiskSpace()
    {
        var rows=DriveInfo.GetDrives().Where(d => d.IsReady).Select(d =>
        {
            var usedPct=d.TotalSize>0?Math.Round((1.0-(double)d.AvailableFreeSpace/d.TotalSize)*100.0, 1):0.0;
            var health=usedPct>=90?"[red]Critical[/]":usedPct>=75?"[yellow]Warning[/]":"[green]Healthy[/]";

            static string Fmt(long b) => b>1_073_741_824?$"{Math.Round(b / 1_073_741_824.0, 2)} GB":$"{Math.Round(b / 1_048_576.0, 2)} MB";
            return new[]
            {
                d.Name.EscapeMarkup(), d.DriveType.ToString().EscapeMarkup(), Fmt(d.TotalSize), Fmt(d.AvailableFreeSpace),$"{usedPct}%", health
            }
            ;
        }
        ).ToArray();
        SpectreTable.Render(["Drive","Type","Total","Free","Used%","Health"], rows, markup:true);

    }

    public static string GetPublicIP()
    {
        var endpoints=new[]
        {
            "https://api.ipify.org","https://icanhazip.com","https://ifconfig.me/ip"
        }
        ;

        using var client=new HttpClient
        {
            Timeout=TimeSpan.FromSeconds(5)
        }
        ;
        foreach (var url in endpoints)
        {
            try
            {
                return client.GetStringAsync(url).GetAwaiter().GetResult().Trim();
            }
            catch
            {
            }
        }
        return"Unavailable";

    }

    public static bool KillPort(int port)
    {
        var result=RunProcess("netstat",$"-ano", capture:true);
        foreach (var line in result.Split('\n'))
        {
            if (!line.Contains($":{port} ")&&!line.Contains($":{port}\t"))continue;
            var parts=line.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length<5)continue;
            if (!int.TryParse(parts[^1], out var pid))continue;

            try
            {
                var proc=Process.GetProcessById(pid);
                proc.Kill(entireProcessTree:true);
                SpectrePanel.Success($"Killed PID {pid} on port {port}.");
                return true;
            }
            catch (Exception ex)
            {
                SpectrePanel.Error($"Failed to kill PID {pid}: {ex.Message}");
                return false;
            }
        }
        SpectrePanel.Warning($"No process found listening on port {port}.");
        return false;

    }

    public static void ShowSshConnectionInfo()
    {
        var localIPs=NetworkInterface.GetAllNetworkInterfaces().Where(n => n.OperationalStatus==OperationalStatus.Up).SelectMany(n => n.GetIPProperties().UnicastAddresses).Where(a => a.Address.AddressFamily==AddressFamily.InterNetwork&&!IPAddress.IsLoopback(a.Address)).Select(a => a.Address.ToString()).ToArray();
        AnsiConsole.Write(new Rule("[bold cyan]SSH Connection Info[/]").RuleStyle("grey"));
        var ipRows=localIPs.Select(ip => new[]
        {
            ip
        }
        ).ToArray();
        SpectreTable.Render(["Local IPv4"], ipRows);
        var tailscaleIP=Environment.GetEnvironmentVariable("TAILSCALE_IP")??"Not configured";
        AnsiConsole.MarkupLine($" Tailscale: [cyan]{tailscaleIP.EscapeMarkup()}[/]");
        var netstatOut=RunProcess("netstat","-an", capture:true);
        var sshConns=netstatOut.Split('\n').Where(l => l.Contains(":22 ")||l.Contains(":22\t")).Where(l => l.Contains("ESTABLISHED")).ToArray();
        AnsiConsole.MarkupLine($" Active SSH connections: [yellow]{sshConns.Length}[/]");
        foreach (var c in sshConns)AnsiConsole.MarkupLine($" [dim]{c.Trim().EscapeMarkup()}[/]");

    }

    internal static string RunProcess(string exe, string args, bool capture=false)
    {
        var psi=new ProcessStartInfo(exe, args)
        {
            RedirectStandardOutput=capture, UseShellExecute=false, CreateNoWindow=true
        }
        ;

        using var p=Process.Start(psi);
        if (p==null)return string.Empty;
        var output=capture?p.StandardOutput.ReadToEnd():string.Empty;
        p.WaitForExit();
        return output;

    }

}
public static class SshHelper
{
    private static readonly string AuthorizedKeysFile=System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),".ssh","authorized_keys");

    public static void ShowSshInfo()
    {
        AnsiConsole.Write(new Rule("[bold cyan]SSH Info[/]").RuleStyle("grey"));
        if (File.Exists(AuthorizedKeysFile))
        {
            var keys=File.ReadAllLines(AuthorizedKeysFile).Where(l => !string.IsNullOrWhiteSpace(l)&&!l.StartsWith('#')).ToArray();
            AnsiConsole.MarkupLine($" Authorized keys: [green]{keys.Length}[/]");
            foreach (var key in keys)
            {
                var parts=key.Split(' ');
                var comment=parts.Length>=3?parts[^1]:"(no comment)";
                AnsiConsole.MarkupLine($" [dim]{parts[0].EscapeMarkup()}[/] [cyan]{comment.EscapeMarkup()}[/]");
            }
        }
        else
        {
            SpectrePanel.Warning("No authorized_keys file found.");
        }
        SystemHelper.ShowSshConnectionInfo();

    }

    public static void StartKeyReceiver(int listenPort=2222)
    {
        AnsiConsole.MarkupLine($"[yellow]Listening on port {listenPort} for public key…[/]");
        AnsiConsole.MarkupLine("[dim]Send key from remote: ssh-copy-id or: cat ~/.ssh/id_rsa.pub | nc <this-ip> {listenPort}[/]");

        try
        {
            var listener=new TcpListener(IPAddress.Any, listenPort);
            listener.Start();

            using var client=listener.AcceptTcpClient();
            listener.Stop();

            using var reader=new StreamReader(client.GetStream(), Encoding.UTF8);
            var keyLine=reader.ReadLine()?.Trim();
            if (string.IsNullOrWhiteSpace(keyLine))
            {
                SpectrePanel.Error("Received empty key.");
                return;
            }
            var sshDir=System.IO.Path.GetDirectoryName(AuthorizedKeysFile)!;
            Directory.CreateDirectory(sshDir);
            File.AppendAllText(AuthorizedKeysFile, keyLine+Environment.NewLine, Encoding.UTF8);
            SpectrePanel.Success($"Public key added: {keyLine[..Math.Min(48, keyLine.Length)]}…");
        }
        catch (Exception ex)
        {
            SpectrePanel.Error($"Key receiver failed: {ex.Message}");
        }

    }

}
public static class DotNetHelper
{
    public static void RemoveBinObj(string rootPath)
    {
        var targets=new[]
        {
            "bin","obj"
        }
        ;
        var deleted=new List<string>();
        var failed=new List<string>();
        foreach (var dir in Directory.EnumerateDirectories(rootPath,"*", SearchOption.AllDirectories))
        {
            if (!targets.Contains(System.IO.Path.GetFileName(dir), StringComparer.OrdinalIgnoreCase))continue;

            try
            {
                Directory.Delete(dir, recursive:true);
                deleted.Add(dir);
            }
            catch
            {
                failed.Add(dir);
            }
        }
        SpectreTable.Render(["Status","Path"], [..deleted.Select(d => new[]
        {
            "[green]Deleted[/]", d.EscapeMarkup()
        }
        ).Concat(failed.Select(f => new[]
        {
            "[red]Failed[/]", f.EscapeMarkup()
        }
        ))], markup:true);

    }

    public static int Build(string?projectPath=null) => RunDotnet("build", projectPath);

    public static int Test(string?projectPath=null) => RunDotnet("test", projectPath);

    public static int AddMigration(string migrationName, string?project=null) => RunDotnet($"ef migrations add {migrationName}", project);

    public static int UpdateDatabase(string?project=null) => RunDotnet("ef database update", project);

    private static int RunDotnet(string args, string?workingDir)
    {
        var psi=new ProcessStartInfo("dotnet", args)
        {
            UseShellExecute=false, WorkingDirectory=workingDir??Directory.GetCurrentDirectory()
        }
        ;

        using var p=Process.Start(psi);
        p?.WaitForExit();
        return p?.ExitCode??-1;

    }

}
public static class GitHelper
{
    private static readonly string[]CommitTypes=["feat","fix","docs","style","refactor","test","chore","ci"];

    public static void ShowStatus()
    {
        var output=RunGit("status --short");
        if (string.IsNullOrWhiteSpace(output))
        {
            SpectrePanel.Info("Working tree clean.");
            return;
        }
        AnsiConsole.Write(new Rule("[bold cyan]Git Status[/]").RuleStyle("grey"));
        foreach (var line in output.Split('\n').Where(l => !string.IsNullOrWhiteSpace(l)))
        {
            var status=line.Length>=2?line[..2]:"??";
            var color=status.Trim()switch
            {
                "M" => "yellow","A" => "green","D" => "red","??" => "dim","R" => "cyan", _ => "white"
            }
            ;
            AnsiConsole.MarkupLine($"[{color}]{line.EscapeMarkup()}[/]");
        }

    }

    public static void ConventionalCommitWizard()
    {
        AnsiConsole.Write(new Rule("[bold cyan]Conventional Commit Wizard[/]").RuleStyle("grey"));
        var typeIdx=SpectreMenu.Show("Commit Type", CommitTypes, 0, false);
        if (typeIdx<0)return;
        var commitType=CommitTypes[typeIdx];
        var scope=AnsiConsole.Ask<string>("[dim]Scope[/] (optional, press Enter to skip):", string.Empty).Trim();
        var scopePart=string.IsNullOrWhiteSpace(scope)?string.Empty:$"({scope})";
        string description;
        while (true)
        {
            description=AnsiConsole.Ask<string>("[cyan]Short description[/]:").Trim();
            if (description.Length is>=5 and<=72)break;
            SpectrePanel.Warning("Description must be 5–72 characters.");
        }
        var breaking=AnsiConsole.Ask<string>("[dim]Breaking changes[/] (optional):", string.Empty).Trim();
        var issues=AnsiConsole.Ask<string>("[dim]Issues closed[/] (e.g. #42, optional):", string.Empty).Trim();
        var sb=new StringBuilder($"{commitType}{scopePart}: {description}");
        if (!string.IsNullOrWhiteSpace(breaking))sb.Append($"\n\nBREAKING CHANGE: {breaking}");
        if (!string.IsNullOrWhiteSpace(issues))sb.Append($"\n\nCloses {issues}");
        var message=sb.ToString();
        AnsiConsole.Write(new Panel(message.EscapeMarkup())
        {
            Header=new PanelHeader("[bold]Commit Message Preview[/]"), Border=BoxBorder.Rounded
        }
        );
        if (!AnsiConsole.Confirm("Commit now?"))return;
        var exitCode=RunGitDirect($"commit -m \"{message.Replace("\"", "\\\"")}\"");
        if (exitCode==0)SpectrePanel.Success("Committed successfully.");

        else SpectrePanel.Error($"git commit failed (exit {exitCode}).");

    }

    public static void InvokeGitUndo()
    {
        var lastLog=RunGit("log --oneline -1").Trim();
        AnsiConsole.MarkupLine($"[yellow]Last commit:[/] {lastLog.EscapeMarkup()}");
        if (!AnsiConsole.Confirm("Soft-reset (keep changes staged)?"))return;
        var exit=RunGitDirect("reset HEAD~1 --soft");
        if (exit==0)SpectrePanel.Success("Last commit undone. Changes kept in working directory.");

        else SpectrePanel.Error($"git reset failed (exit {exit}).");

    }

    private static string RunGit(string args) => SystemHelper.RunProcess("git", args, capture:true);

    private static int RunGitDirect(string args)
    {
        using var p=Process.Start(new ProcessStartInfo("git", args)
        {
            UseShellExecute=false
        }
        );
        p?.WaitForExit();
        return p?.ExitCode??-1;

    }

}
public static class DockerHelper
{
    private static readonly string[]CleanupOptions=["Stop & remove all running containers","Prune unused images and dangling layers","Delete unused volumes","Delete unused networks","Full cleanup (all of the above)","Cancel"];

    public static void ShowCleanupDashboard()
    {
        AnsiConsole.Write(new Rule("[bold cyan]Docker Cleanup Dashboard[/]").RuleStyle("grey"));
        var idx=SpectreMenu.Show("Select cleanup action", CleanupOptions, 0, false);
        switch (idx)
        {
            case 0:RunDocker("stop $(docker ps -q)");
            RunDocker("rm $(docker ps -aq)");
            break;
            case 1:RunDocker("image prune -af");
            break;
            case 2:RunDocker("volume prune -f");
            break;
            case 3:RunDocker("network prune -f");
            break;
            case 4:SpectreProgress.BulkProgress("Docker cleanup", CleanupOptions[..4], (i, step) =>
            {
                switch (i)
                {
                    case 0:RunDocker("container prune -f");
                    break;
                    case 1:RunDocker("image prune -af");
                    break;
                    case 2:RunDocker("volume prune -f");
                    break;
                    case 3:RunDocker("network prune -f");
                    break;
                }
            }
            );
            break;
            default:return;
        }
        SpectrePanel.Success("Docker cleanup completed.");

    }

    public static int ComposeUp(string?composeFile=null)
    {
        var args=composeFile!=null?$"-f {composeFile} up -d":"up -d";
        return RunDockerCompose(args);

    }

    public static int ComposeDown(string?composeFile=null)
    {
        var args=composeFile!=null?$"-f {composeFile} down":"down";
        return RunDockerCompose(args);

    }

    private static void RunDocker(string args)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            using var p=Process.Start(new ProcessStartInfo("cmd",$"/c docker {args}")
            {
                UseShellExecute=false
            }
            );
            p?.WaitForExit();
        }
        else
        {
            using var p=Process.Start(new ProcessStartInfo("sh",$"-c \"docker {args}\"")
            {
                UseShellExecute=false
            }
            );
            p?.WaitForExit();
        }

    }

    private static int RunDockerCompose(string args)
    {
        using var p=Process.Start(new ProcessStartInfo("docker",$"compose {args}")
        {
            UseShellExecute=false
        }
        );
        p?.WaitForExit();
        return p?.ExitCode??-1;

    }

}
public static class AwsHelper
{
    private const string LocalStackEndpoint="http://localhost:4566";

    public static void ShowLocalStackInfo()
    {
        AnsiConsole.Write(new Rule("[bold cyan]LocalStack Sandbox[/]").RuleStyle("grey"));
        SpectreProgress.Spinner("Querying LocalStack…", () =>
        {
            RunLocalAwsCli("s3 ls","S3 Buckets");
            RunLocalAwsCli("sqs list-queues","SQS Queues");
            RunLocalAwsCli("lambda list-functions --query 'Functions[*].FunctionName'","Lambda Functions");
        }
        );

    }

    private static void RunLocalAwsCli(string args, string section)
    {
        AnsiConsole.MarkupLine($"\n[bold cyan]{section.EscapeMarkup()}[/]");
        var output=SystemHelper.RunProcess("aws",$"--endpoint-url {LocalStackEndpoint} {args}", capture:true);
        if (string.IsNullOrWhiteSpace(output))AnsiConsole.MarkupLine("[dim]  (no results or LocalStack unavailable)[/]");

        else foreach (var line in output.Trim().Split('\n'))AnsiConsole.MarkupLine($"  {line.EscapeMarkup()}");

    }

}
public static class AiHelper
{
    private const int OllamaPort=11434;

    public static bool EnsureOllamaDaemon()
    {
        if (IsPortListening(OllamaPort))return true;
        AnsiConsole.MarkupLine("[yellow]Ollama not running — starting daemon in background…[/]");

        try
        {
            Process.Start(new ProcessStartInfo("ollama","serve")
            {
                UseShellExecute=false, CreateNoWindow=true
            }
            );
            Thread.Sleep(1500);
            return IsPortListening(OllamaPort);
        }
        catch
        {
            SpectrePanel.Error("Failed to start Ollama daemon.");
            return false;
        }

    }

    public static void LaunchClaude()
    {
        Environment.SetEnvironmentVariable("NODE_NO_WARNINGS","1");
        Environment.SetEnvironmentVariable("ANTHROPIC_MODEL","claude-sonnet-4-6");
        RunInteractive("claude", string.Empty);

    }

    public static void LaunchCodex()
    {
        Environment.SetEnvironmentVariable("NODE_NO_WARNINGS","1");
        RunInteractive("codex", string.Empty);

    }

    public static void LaunchOpenClaw()
    {
        if (!EnsureOllamaDaemon())return;
        RunInteractive("ollama","run openclaw");

    }

    public static void LaunchHermes(bool debug=false)
    {
        if (!EnsureOllamaDaemon())return;
        var model=debug?"hermes3:debug":"hermes3";
        RunInteractive("ollama",$"run {model}");

    }

    private static bool IsPortListening(int port)
    {
        try
        {
            using var tcp=new TcpClient();
            return tcp.ConnectAsync(IPAddress.Loopback, port).Wait(300);
        }
        catch
        {
            return false;
        }

    }

    private static void RunInteractive(string exe, string args)
    {
        using var p=Process.Start(new ProcessStartInfo(exe, args)
        {
            UseShellExecute=false
        }
        );
        p?.WaitForExit();

    }

}
public static class AgySecretVault
{
    private static readonly string VaultDir=System.IO.Path.Combine(AgyAccountCore.AgySourceHome,"vault");

    private static string KeyFile(string key) => System.IO.Path.Combine(VaultDir, Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(key.ToLowerInvariant())))+".enc");

    public static void SetSecret(string key, string value)
    {
        try
        {
            Directory.CreateDirectory(VaultDir);
            byte[]plain=Encoding.UTF8.GetBytes(value);
            byte[]cipher=RuntimeInformation.IsOSPlatform(OSPlatform.Windows)?ProtectedData.Protect(plain, null, DataProtectionScope.CurrentUser):Convert.FromBase64String(Convert.ToBase64String(plain));
            File.WriteAllBytes(KeyFile(key), cipher);
            SpectrePanel.Success($"Secret '{key}' stored.");
        }
        catch (Exception ex)
        {
            SpectrePanel.Error($"Failed to store secret: {ex.Message}");
        }

    }

    public static string?GetSecret(string key)
    {
        var file=KeyFile(key);
        if (!File.Exists(file))return null;

        try
        {
            byte[]cipher=File.ReadAllBytes(file);
            byte[]plain=RuntimeInformation.IsOSPlatform(OSPlatform.Windows)?ProtectedData.Unprotect(cipher, null, DataProtectionScope.CurrentUser):cipher;
            return Encoding.UTF8.GetString(plain);
        }
        catch
        {
            return null;
        }

    }

    public static void RemoveSecret(string key)
    {
        var file=KeyFile(key);
        if (!File.Exists(file))
        {
            SpectrePanel.Warning($"Secret '{key}' not found.");
            return;
        }
        File.Delete(file);
        SpectrePanel.Success($"Secret '{key}' removed.");

    }

}
public static class DatabaseHelper
{
    public static void ShowDatabaseTui(string dbPath)
    {
        if (!File.Exists(dbPath))
        {
            SpectrePanel.Error($"Database not found: {dbPath}");
            return;
        }
        AnsiConsole.Write(new Rule($"[bold cyan]SQLite: {System.IO.Path.GetFileName(dbPath).EscapeMarkup()}[/]").RuleStyle("grey"));
        var schemaOutput=SystemHelper.RunProcess("sqlite3",$"\"{dbPath}\" .schema", capture:true);
        if (string.IsNullOrWhiteSpace(schemaOutput))
        {
            SpectrePanel.Warning("No schema found or sqlite3 CLI not available.");
            return;
        }
        var tables=Regex.Matches(schemaOutput,@"CREATE TABLE\s+(?:IF NOT EXISTS\s+)?[""']?(\w+)[""']?", RegexOptions.IgnoreCase).Select(m => m.Groups[1].Value).Distinct().ToArray();
        if (tables.Length==0)
        {
            SpectrePanel.Info("No tables found.");
            return;
        }
        var idx=SpectreMenu.Show("Select table to inspect", tables, 0, true);
        if (idx<0)return;
        var tableData=SystemHelper.RunProcess("sqlite3",$"\"{dbPath}\" -header -column \"SELECT * FROM {tables[idx]} LIMIT 50;\"", capture:true);
        var lines=tableData.Split('\n').Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();
        SpectrePager.Show($"Table: {tables[idx]}", lines);

    }

}
public static class ProjectScaffolder
{
    private static readonly string[]Templates=["webapi","console","react (Vite)","blazorwasm","classlib","worker"];

    public static void Scaffold()
    {
        AnsiConsole.Write(new Rule("[bold cyan]Project Scaffolder[/]").RuleStyle("grey"));
        var idx=SpectreMenu.Show("Select template", Templates, 0, false);
        if (idx<0)return;
        var template=Templates[idx];
        var name=AnsiConsole.Ask<string>("[cyan]Project name:[/]").Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            SpectrePanel.Warning("Project name cannot be empty.");
            return;
        }
        var outputDir=AnsiConsole.Ask<string>("[dim]Output directory[/] (Enter for current):", Directory.GetCurrentDirectory()).Trim();
        SpectreProgress.Spinner($"Scaffolding {template} project '{name}'…", () =>
        {
            if (template=="react (Vite)")
            {
                Directory.CreateDirectory(System.IO.Path.Combine(outputDir, name));
                SystemHelper.RunProcess("npm",$"create vite@latest {name} -- --template react-ts");
            }
            else
            {
                var psi=new ProcessStartInfo("dotnet",$"new {template} -n {name} -o \"{System.IO.Path.Combine(outputDir, name)}\"")
                {
                    UseShellExecute=false
                }
                ;

                using var p=Process.Start(psi);
                p?.WaitForExit();
            }
        }
        );
        SpectrePanel.Success($"Project '{name}' created at {System.IO.Path.Combine(outputDir, name)}");

    }

}
public static class FileExplorer
{
    public static string?Browse(string rootPath)
    {
        var current=Directory.Exists(rootPath)?rootPath:Directory.GetCurrentDirectory();
        while (true)
        {
            AnsiConsole.Clear();
            AnsiConsole.Write(new Rule($"[bold cyan]IDE: {current.EscapeMarkup()}[/]").RuleStyle("grey"));
            var entries=new List<string>
            {
                ".. (go up)"
            }
            ;

            try
            {
                entries.AddRange(Directory.GetDirectories(current).Select(d => $"📁 {System.IO.Path.GetFileName(d)}"));
                entries.AddRange(Directory.GetFiles(current).Select(f => $"{GetFileIcon(System.IO.Path.GetExtension(f))} {System.IO.Path.GetFileName(f)}"));
            }
            catch (Exception ex)
            {
                SpectrePanel.Error(ex.Message);
                return null;
            }
            entries.Add("← Exit Explorer");
            var limit=Math.Min(entries.Count, Math.Max(5, Console.WindowHeight-6));
            var idx=SpectreMenu.Show($"Explorer: {System.IO.Path.GetFileName(current)}", [..entries], 0, true);
            if (idx<0||idx==entries.Count-1)return null;
            if (idx==0)
            {
                var parent=System.IO.Path.GetDirectoryName(current);
                if (parent!=null)current=parent;
                continue;
            }
            var selected=entries[idx][3..].Trim();
            var fullPath=System.IO.Path.Combine(current, selected);
            if (Directory.Exists(fullPath))
            {
                current=fullPath;
                continue;
            }
            if (File.Exists(fullPath))return fullPath;
        }

    }

    private static string GetFileIcon(string ext) => ext.ToLower()switch
    {
        ".cs" => "⚙",".json" => "📋",".md" => "📝",".txt" => "📄",".ps1" => "⚡",".sh" => "⚡",".yaml"or".yml" => "🔧",".csproj"or".sln" => "🏗", _ => "📄"

    }
    ;

}
public static class CodeViewer
{
    private static readonly SearchValues<char>StringDelimiters=SearchValues.Create(['"','\'','`']);

    public static void Show(string filePath)
    {
        if (!File.Exists(filePath))
        {
            SpectrePanel.Error($"File not found: {filePath}");
            return;
        }
        var lines=LoadWithLineNumbers(filePath);
        SpectrePager.Show(System.IO.Path.GetFileName(filePath), lines);

    }

    public static void ShowWithHighlight(string filePath, int[]highlightLines)
    {
        if (!File.Exists(filePath))
        {
            SpectrePanel.Error($"File not found: {filePath}");
            return;
        }
        var ext=System.IO.Path.GetExtension(filePath).ToLower();
        var rawLines=File.ReadAllLines(filePath);
        var numbered=rawLines.Select((l, i) =>
        {
            var num=$"{i + 1,4}  ";
            var colored=ColorizeToken(l, ext);
            return highlightLines.Contains(i+1)?$"[yellow]{num}→[/] {colored}":$"[dim]{num}[/]  {colored}";
        }
        ).ToArray();
        SpectrePager.Show(System.IO.Path.GetFileName(filePath), numbered);

    }

    private static string[]LoadWithLineNumbers(string filePath)
    {
        var ext=System.IO.Path.GetExtension(filePath).ToLower();
        var lines=File.ReadAllLines(filePath);
        return lines.Select((l, i) => $"[dim]{i + 1,4}[/]  {ColorizeToken(l, ext)}").ToArray();

    }

    internal static string ColorizeToken(string line, string ext)
    {
        var escaped=line.EscapeMarkup();

        try
        {
            return ext switch
            {
                ".cs" => ColorizeCsharp(escaped),".ps1" => ColorizePowershell(escaped),".json" => ColorizeJson(escaped),".md" => ColorizeMarkdown(escaped),".ts"or".tsx"or".js" => ColorizeTypeScript(escaped),".sql" => ColorizeSql(escaped),".sh"or".bash" => ColorizeBash(escaped),".yaml"or".yml" => ColorizeYaml(escaped), _ => escaped
            }
            ;
        }
        catch
        {
            return escaped;
        }

    }

    private static string ColorizeCsharp(string l)
    {
        if (Regex.IsMatch(l,@"^\s*//"))return$"[dim]{l}[/]";
        l=Regex.Replace(l,@"\b(public|private|static|void|class|using|return|new|var|if|else|for|foreach|switch|case|break|readonly|sealed|record|namespace|async|await|override|partial|internal|protected|abstract|virtual)\b","[cyan]$0[/]");
        if (l.AsSpan().IndexOfAny(StringDelimiters)>=0)l=Regex.Replace(l,@"""[^""]*""", m => $"[yellow]{m.Value}[/]");
        return l;

    }

    private static string ColorizePowershell(string l)
    {
        if (l.TrimStart().StartsWith("#"))return$"[dim]{l}[/]";
        l=Regex.Replace(l,@"\b(function|param|if|else|foreach|return|Write-Host|Write-Output|Set-Location|Get-ChildItem)\b","[cyan]$0[/]");
        l=Regex.Replace(l,@"\$[\w:]+", m => $"[green]{m.Value}[/]");
        l=Regex.Replace(l,@"""[^""]*""", m => $"[yellow]{m.Value}[/]");
        return l;

    }

    private static string ColorizeJson(string l)
    {
        l=Regex.Replace(l,@"""([^""]+)""\s*:", m => $"[cyan]{m.Value}[/]");
        l=Regex.Replace(l,@":\s*""([^""]*)""", m => $": [yellow]\"{Regex.Match(m.Value, @"""([^""]*)""$").Groups[1].Value}\"[/]");
        l=Regex.Replace(l,@"\b(true|false|null)\b","[green]$0[/]");
        return l;

    }

    private static string ColorizeMarkdown(string l)
    {
        if (Regex.IsMatch(l,@"^#{1,6} "))return$"[bold cyan]{l}[/]";
        l=Regex.Replace(l,@"\*\*[^*]+\*\*", m => $"[bold]{m.Value}[/]");
        l=Regex.Replace(l,@"`[^`]+`", m => $"[yellow]{m.Value}[/]");
        return l;

    }

    private static string ColorizeTypeScript(string l)
    {
        if (Regex.IsMatch(l,@"^\s*//"))return$"[dim]{l}[/]";
        l=Regex.Replace(l,@"\b(const|let|var|function|class|import|export|return|if|else|async|await|interface|type)\b","[cyan]$0[/]");
        l=Regex.Replace(l,@"""[^""]*""|'[^']*'|`[^`]*`", m => $"[yellow]{m.Value}[/]");
        return l;

    }

    private static string ColorizeSql(string l)
    {
        l=Regex.Replace(l,@"\b(SELECT|FROM|WHERE|JOIN|ON|INSERT|UPDATE|DELETE|CREATE|TABLE|ALTER|DROP|AND|OR|AS|INTO|VALUES|SET|LEFT|RIGHT|INNER|OUTER|GROUP|ORDER|BY|HAVING|DISTINCT|TOP|LIMIT)\b","[cyan]$0[/]", RegexOptions.IgnoreCase);
        l=Regex.Replace(l,@"'[^']*'", m => $"[yellow]{m.Value}[/]");
        return l;

    }

    private static string ColorizeBash(string l)
    {
        if (l.TrimStart().StartsWith("#"))return$"[dim]{l}[/]";
        l=Regex.Replace(l,@"\b(if|then|else|fi|for|do|done|function|echo|export)\b","[cyan]$0[/]");
        l=Regex.Replace(l,@"\$\{?[\w]+\}?|\$\([^)]+\)", m => $"[green]{m.Value}[/]");
        return l;

    }

    private static string ColorizeYaml(string l)
    {
        if (l.TrimStart().StartsWith("#"))return$"[dim]{l}[/]";
        l=Regex.Replace(l,@"^(\s*[\w-]+):", m => $"[cyan]{m.Value}[/]");
        l=Regex.Replace(l,@"\b(true|false|null)\b","[green]$0[/]");
        return l;

    }

}
public static class SymbolSearch
{
    public static void BrowseSymbols(string filePath)
    {
        if (!File.Exists(filePath))
        {
            SpectrePanel.Error($"File not found: {filePath}");
            return;
        }
        var ext=System.IO.Path.GetExtension(filePath).ToLower();
        var lines=File.ReadAllLines(filePath);
        var symbols=ExtractSymbols(lines, ext);
        if (symbols.Length==0)
        {
            SpectrePanel.Info("No symbols found.");
            return;
        }
        var idx=SpectreMenu.Show($"Symbols: {System.IO.Path.GetFileName(filePath)}", symbols, 0, true);
        if (idx<0)return;
        var lineNum=FindSymbolLine(filePath, symbols[idx].Split(' ')[0]);
        if (lineNum.HasValue)CodeViewer.ShowWithHighlight(filePath, [lineNum.Value]);

    }

    public static int?FindSymbolLine(string filePath, string symbol)
    {
        var lines=File.ReadAllLines(filePath);
        for (int i=0;
        i<lines.Length;
        i++)if (lines[i].Contains(symbol, StringComparison.OrdinalIgnoreCase))return i+1;
        return null;

    }

    private static string[]ExtractSymbols(string[]lines, string ext)
    {
        var symbols=new List<string>();
        var patterns=ext switch
        {
            ".cs" => new[]
            {
                @"(public|private|internal|protected).*\s(class|interface|record|enum)\s+(\w+)",@"(public|private|static).*\s(\w+)\s*\("
            }
            ,".ts"or".js" => new[]
            {
                @"(export\s+)?(class|function|interface|type)\s+(\w+)",@"const\s+(\w+)\s*="
            }
            ,".ps1" => new[]
            {
                @"function\s+([\w-]+)"
            }
            , _ => new[]
            {
                @"\b(\w+)\s*[({]"
            }
        }
        ;
        for (int i=0;
        i<lines.Length;
        i++)
        {
            foreach (var pat in patterns)
            {
                var m=Regex.Match(lines[i], pat);
                if (m.Success)
                {
                    var name=m.Groups[m.Groups.Count-1].Value;
                    if (name.Length>2)symbols.Add($"{name,-30}  ln {i + 1,4}");
                }
            }
        }
        return[..symbols.Distinct()];

    }

}
public static class GitDiffViewer
{
    public static void ShowDiff(string workspacePath, string?filePath=null)
    {
        var args=filePath!=null?$"diff {filePath}":"diff";
        var output=RunGit(workspacePath, args);
        if (string.IsNullOrWhiteSpace(output))
        {
            SpectrePanel.Info("No changes to show.");
            return;
        }
        var lines=ColorizeHunk(output.Split('\n'));
        SpectrePager.Show($"Diff: {System.IO.Path.GetFileName(workspacePath)}", lines);

    }

    public static void ShowCommitDiff(string workspacePath, string commitHash)
    {
        var output=RunGit(workspacePath,$"show {commitHash}");
        if (string.IsNullOrWhiteSpace(output))
        {
            SpectrePanel.Info("No diff for that commit.");
            return;
        }
        var lines=ColorizeHunk(output.Split('\n'));
        SpectrePager.Show($"Commit: {commitHash[..Math.Min(7, commitHash.Length)]}", lines);

    }

    private static string[]ColorizeHunk(string[]diffLines) => diffLines.Select(l => l switch
    {
        _ when l.StartsWith("+")&&!l.StartsWith("+++") => $"[green]{l.EscapeMarkup()}[/]", _ when l.StartsWith("-")&&!l.StartsWith("---") => $"[red]{l.EscapeMarkup()}[/]", _ when l.StartsWith("@@") => $"[cyan]{l.EscapeMarkup()}[/]", _ when l.StartsWith("diff ")||l.StartsWith("index ")||l.StartsWith("--- ")||l.StartsWith("+++ ") => $"[dim]{l.EscapeMarkup()}[/]", _ => l.EscapeMarkup()

    }
    ).ToArray();

    private static string RunGit(string workingDir, string args)
    {
        try
        {
            var psi=new ProcessStartInfo("git", args)
            {
                WorkingDirectory=workingDir, RedirectStandardOutput=true, UseShellExecute=false, CreateNoWindow=true
            }
            ;

            using var p=Process.Start(psi)!;
            var output=p.StandardOutput.ReadToEnd();
            p.WaitForExit();
            return output;
        }
        catch
        {
            return string.Empty;
        }

    }

}
public static class TerminalIde
{
    public static void Open(string?path=null)
    {
        var root=path??Directory.GetCurrentDirectory();
        while (true)
        {
            AnsiConsole.Clear();
            AnsiConsole.Write(new Rule($"[bold cyan]AGY — IDE: {root.EscapeMarkup()}[/]").RuleStyle("grey"));
            var actions=new[]
            {
                "Browse files","Search in files","View git diff","Open file by path","← Exit IDE"
            }
            ;
            var idx=SpectreMenu.Show("Terminal IDE", actions, 0, false);
            switch (idx)
            {
                case 0:var file=FileExplorer.Browse(root);
                if (file!=null)OpenFile(file);
                break;
                case 1:SearchAcrossFiles(root, AnsiConsole.Ask<string>("[cyan]Search pattern:[/]").Trim());
                break;
                case 2:GitDiffViewer.ShowDiff(root);
                break;
                case 3:var fp=AnsiConsole.Ask<string>("[cyan]File path:[/]").Trim();
                if (File.Exists(fp))OpenFile(fp);

                else SpectrePanel.Error($"File not found: {fp}");
                break;
                default:return;
            }
        }

    }

    public static void OpenFile(string filePath)
    {
        var ext=System.IO.Path.GetExtension(filePath).ToLower();
        while (true)
        {
            AnsiConsole.Clear();
            AnsiConsole.Write(new Rule($"[bold cyan]IDE: {System.IO.Path.GetFileName(filePath).EscapeMarkup()}[/]").RuleStyle("grey"));
            var actions=new[]
            {
                "View file","Symbol search","View diff (this file)",$"Edit ({(RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "notepad" : "nano")})","← Back"
            }
            ;
            var idx=SpectreMenu.Show("File actions", actions, 0, false);
            switch (idx)
            {
                case 0:CodeViewer.Show(filePath);
                break;
                case 1:SymbolSearch.BrowseSymbols(filePath);
                break;
                case 2:GitDiffViewer.ShowDiff(System.IO.Path.GetDirectoryName(filePath)??".", filePath);
                break;
                case 3:LaunchEditor(filePath);
                break;
                default:return;
            }
        }

    }

    public static void SearchInFile(string filePath)
    {
        if (!File.Exists(filePath))return;
        var pattern=AnsiConsole.Ask<string>("[cyan]Search pattern:[/]").Trim();
        var lines=File.ReadAllLines(filePath);
        var matches=lines.Select((l, i) => (line:l, num:i+1)).Where(x => Regex.IsMatch(x.line, pattern, RegexOptions.IgnoreCase)).ToArray();
        if (matches.Length==0)
        {
            SpectrePanel.Info($"No matches for '{pattern}'.");
            return;
        }
        CodeViewer.ShowWithHighlight(filePath, matches.Select(m => m.num).ToArray());

    }

    public static void SearchAcrossFiles(string rootPath, string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))return;
        var results=new List<string>();
        foreach (var f in Directory.EnumerateFiles(rootPath,"*.*", SearchOption.AllDirectories).Where(f => !f.Contains("bin")&&!f.Contains("obj")&&!f.Contains(".git")))
        {
            try
            {
                var lines=File.ReadAllLines(f);
                for (int i=0;
                i<lines.Length;
                i++)if (Regex.IsMatch(lines[i], pattern, RegexOptions.IgnoreCase))results.Add($"{System.IO.Path.GetRelativePath(rootPath, f)}:{i + 1}: {lines[i].Trim()}");
            }
            catch
            {
            }
            if (results.Count>=100)break;
        }
        if (results.Count==0)
        {
            SpectrePanel.Info($"No matches for '{pattern}'.");
            return;
        }
        SpectrePager.Show($"Search results: {pattern}", [..results]);

    }

    private static void LaunchEditor(string filePath)
    {
        var editor=RuntimeInformation.IsOSPlatform(OSPlatform.Windows)?"notepad":"nano";

        try
        {
            using var p=Process.Start(new ProcessStartInfo(editor,$"\"{filePath}\"")
            {
                UseShellExecute=false
            }
            );
            p?.WaitForExit();
        }
        catch (Exception ex)
        {
            SpectrePanel.Error($"Editor launch failed: {ex.Message}");
        }

    }

}

public sealed record ExtractedItem(string SourceId, string SourcePath, string Format, string Topic, string SubTopic, string Language, string ItemType, string Front, string Back, string?Hint, string?Mnemonic, string?ExampleSentence, string?CodeSnippetText, string[]Tags, int Difficulty);

public sealed record ResourceEntry(string Id, string Path, string Format, string Title, string[]Tags, string[]Topics, string Language, string SourceType, string?Checksum, long SizeBytes, string AddedAt, string?LastExtractedAt, string Status, string?ErrorMessage, int ExtractedItemCount, string[]LearnFiles, bool AutoDiscovered, bool Enabled);

public sealed record ExtractionConfig(string LearnPath, string VaultPath, string ResourcesIndexPath, bool DryRun, bool ForceReExtract);

public sealed record ResourceIndex(int Version, string UpdatedAt, ResourceEntry[]Resources);

public static class ResourceRegistry
{
    public static ResourceEntry[]LoadAll()
    {
        var idx=LearnDataPaths.LoadJson<ResourceIndex>(LearnDataPaths.ResourcesIndex);
        return idx?.Resources??[];

    }

    public static void Save(ResourceEntry[]entries)
    {
        var idx=new ResourceIndex(1, DateTimeOffset.Now.ToString("o"), entries);
        LearnDataPaths.SaveJson(LearnDataPaths.ResourcesIndex, idx);

    }

    public static string AddResource(string path, string[]tags)
    {
        var entries=LoadAll().ToList();
        var id=$"res_{entries.Count + 1:000}";
        var format=DetectFormat(path);
        var checksum=File.Exists(path)?ComputeChecksum(path):null;
        var size=File.Exists(path)?new FileInfo(path).Length:0;
        entries.Add(new ResourceEntry(id, path, format, System.IO.Path.GetFileNameWithoutExtension(path), tags, [],"auto","local_file", checksum, size, DateTimeOffset.Now.ToString("o"), null,"pending", null, 0, [], false, true));
        Save([..entries]);
        return id;

    }

    public static void UpdateStatus(string id, string status, string?error=null)
    {
        var entries=LoadAll().ToList();
        var idx=entries.FindIndex(e => e.Id==id);
        if (idx<0)return;
        entries[idx]=entries[idx]with
        {
            Status=status, ErrorMessage=error
        }
        ;
        Save([..entries]);

    }

    public static string ComputeChecksum(string filePath)
    {
        using var sha=System.Security.Cryptography.SHA256.Create();

        using var stream=File.OpenRead(filePath);
        return"sha256:"+Convert.ToHexString(sha.ComputeHash(stream));

    }

    private static string DetectFormat(string path)
    {
        if (path.StartsWith("http"))return"url";
        return System.IO.Path.GetExtension(path).ToLower()switch
        {
            ".md"or".txt" => "md",".pdf" => "pdf",".docx" => "docx",".csv" => "csv",".epub" => "epub",".cs"or".py"or".ts"or".js"or".go" => "code",".png"or".jpg"or".jpeg" => "image", _ => "md"
        }
        ;

    }

}
public static class MdExtractor
{
    public static ExtractedItem[]Extract(string path, ResourceEntry entry)
    {
        if (!File.Exists(path))return[];
        var text=File.ReadAllText(path);
        var items=new List<ExtractedItem>();
        items.AddRange(ExtractTables(text, entry));
        items.AddRange(ExtractBoldColon(text, entry));
        items.AddRange(ExtractCodeBlocks(text, entry));
        return[..items];

    }

    private static ExtractedItem[]ExtractTables(string text, ResourceEntry entry)
    {
        var items=new List<ExtractedItem>();
        var tablePattern=new Regex(@"^\|.+\|$", RegexOptions.Multiline);
        var tableBlocks=Regex.Split(text,@"\n\n+");
        foreach (var block in tableBlocks)
        {
            var rows=block.Split('\n').Where(l => l.TrimStart().StartsWith("|")&&!l.Contains("---")).ToArray();
            if (rows.Length<2)continue;
            var headers=rows[0].Split('|').Select(h => h.Trim()).Where(h => h.Length>0).ToArray();
            for (int i=1;
            i<rows.Length;
            i++)
            {
                var cells=rows[i].Split('|').Select(c => c.Trim()).Where(c => c.Length>0).ToArray();
                if (cells.Length<2)continue;
                items.Add(new ExtractedItem(entry.Id, entry.Path,"md", entry.Topics.FirstOrDefault()??"general","table", entry.Language,"flashcard", cells[0], cells.Length>1?cells[1]:"", null, null, cells.Length>2?cells[2]:null, null, entry.Tags, 3));
            }
        }
        return[..items];

    }

    private static ExtractedItem[]ExtractBoldColon(string text, ResourceEntry entry)
    {
        var pattern=new Regex(@"\*\*([^*]+)\*\*\s*:\s*(.+)");
        return pattern.Matches(text).Select(m => new ExtractedItem(entry.Id, entry.Path,"md", entry.Topics.FirstOrDefault()??"general","bold-colon", entry.Language,"flashcard", m.Groups[1].Value.Trim(), m.Groups[2].Value.Trim(), null, null, null, null, entry.Tags, 3)).ToArray();

    }

    private static ExtractedItem[]ExtractCodeBlocks(string text, ResourceEntry entry)
    {
        var pattern=new Regex(@"```(\w+)?\n([\s\S]*?)```");
        return pattern.Matches(text).Select(m => new ExtractedItem(entry.Id, entry.Path,"md", entry.Topics.FirstOrDefault()??"csharp","snippet","code","snippet",$"Snippet", m.Groups[2].Value.Trim(), null, null, null, m.Groups[2].Value.Trim(), entry.Tags, 3)).ToArray();

    }

}
public static class CsvExtractor
{
    public static ExtractedItem[]Extract(string path, ResourceEntry entry)
    {
        if (!File.Exists(path))return[];
        var lines=File.ReadAllLines(path);
        if (lines.Length<2)return[];
        var headers=lines[0].Split(',').Select(h => h.Trim().Trim('"').ToLower()).ToArray();
        int frontIdx=Array.FindIndex(headers, h => h is"word"or"front"or"term"or"question");
        int backIdx=Array.FindIndex(headers, h => h is"definition"or"back"or"meaning"or"answer");
        if (frontIdx<0)frontIdx=0;
        if (backIdx<0)backIdx=1;
        var items=new List<ExtractedItem>();
        for (int i=1;
        i<lines.Length;
        i++)
        {
            var cells=lines[i].Split(',').Select(c => c.Trim().Trim('"')).ToArray();
            if (cells.Length<=Math.Max(frontIdx, backIdx))continue;
            items.Add(new ExtractedItem(entry.Id, path,"csv", entry.Topics.FirstOrDefault()??"general","csv-row", entry.Language,"flashcard", cells[frontIdx], cells[backIdx], null, null, null, null, entry.Tags, 3));
        }
        return[..items];

    }

}
public static class ExtractorRouter
{
    public static ExtractedItem[]Route(ResourceEntry entry)
    {
        try
        {
            return entry.Format switch
            {
                "md"or"txt" => MdExtractor.Extract(entry.Path, entry),"csv" => CsvExtractor.Extract(entry.Path, entry),"code" => ExtractCode(entry),"url" => ExtractUrl(entry), _ => []
            }
            ;
        }
        catch (Exception ex)
        {
            ResourceRegistry.UpdateStatus(entry.Id,"error", ex.Message);
            return[];
        }

    }

    private static ExtractedItem[]ExtractCode(ResourceEntry entry)
    {
        if (!File.Exists(entry.Path))return[];
        var content=File.ReadAllText(entry.Path);
        var comments=Regex.Matches(content,@"///\s*<summary>([\s\S]*?)</summary>|/\*\*([\s\S]*?)\*/|#\s+(.+)").Select(m => (m.Groups[1].Value+m.Groups[2].Value+m.Groups[3].Value).Trim()).Where(c => c.Length>10).Select(c => new ExtractedItem(entry.Id, entry.Path,"code", entry.Topics.FirstOrDefault()??"csharp","comment","code","flashcard", c,"", null, null, null, null, entry.Tags, 3)).ToArray();
        return comments;

    }

    private static ExtractedItem[]ExtractUrl(ResourceEntry entry)
    {
        try
        {
            using var client=new HttpClient
            {
                Timeout=TimeSpan.FromSeconds(10)
            }
            ;
            var html=client.GetStringAsync(entry.Path).GetAwaiter().GetResult();
            var text=Regex.Replace(html,@"<[^>]+>"," ");
            text=Regex.Replace(text,@"\s+"," ").Trim();
            var tempEntry=entry with
            {
                Format="md"
            }
            ;
            var fakeEntry=new ResourceEntry(entry.Id, entry.Path,"md", entry.Title, entry.Tags, entry.Topics, entry.Language, entry.SourceType, null, 0, entry.AddedAt, null,"pending", null, 0, [], false, true);
            var tempFile=System.IO.Path.GetTempFileName()+".md";
            File.WriteAllText(tempFile, text);
            var items=MdExtractor.Extract(tempFile, fakeEntry);
            File.Delete(tempFile);
            return items;
        }
        catch
        {
            return[];
        }

    }

}
public static class ResourceScanner
{
    public static string[]FindNotesByTag(string vaultPath, string[]tags)
    {
        if (!Directory.Exists(vaultPath))return[];
        return Directory.GetFiles(vaultPath,"*.md", SearchOption.AllDirectories).Where(f =>
        {
            var fm=ObsidianBridge.ParseFrontmatter(f);
            return fm!=null&&fm.Tags.Any(t => tags.Any(needle => t.Contains(needle, StringComparison.OrdinalIgnoreCase)));
        }
        ).ToArray();

    }

    public static string[]FindNotesByTopic(string vaultPath, string topic)
    {
        if (!Directory.Exists(vaultPath))return[];
        return Directory.GetFiles(vaultPath,"*.md", SearchOption.AllDirectories).Where(f =>
        {
            var fm=ObsidianBridge.ParseFrontmatter(f);
            return fm!=null&&fm.Topic.Equals(topic, StringComparison.OrdinalIgnoreCase);
        }
        ).ToArray();

    }

    public static string[]ListAllTags(string vaultPath)
    {
        if (!Directory.Exists(vaultPath))return[];
        return Directory.GetFiles(vaultPath,"*.md", SearchOption.AllDirectories).SelectMany(f => ObsidianBridge.ParseFrontmatter(f)?.Tags??[]).Distinct().OrderBy(t => t).ToArray();

    }

}
public static class ContentExtractor
{
    public static string[][]ExtractVocabTable(string notePath)
    {
        if (!File.Exists(notePath))return[];
        var lines=File.ReadAllLines(notePath);
        var results=new List<string[]>();
        foreach (var line in lines)
        {
            if (!line.TrimStart().StartsWith("|")||line.Contains("---"))continue;
            var cells=line.Split('|').Select(c => c.Trim()).Where(c => c.Length>0).ToArray();
            if (cells.Length>=2)results.Add(cells);
        }
        return results.Count>1?[..results.Skip(1)]:[];

    }

    public static(string Front, string Back)[]ExtractBoldColonPairs(string notePath)
    {
        if (!File.Exists(notePath))return[];
        var content=File.ReadAllText(notePath);
        return Regex.Matches(content,@"\*\*([^*]+)\*\*\s*:\s*(.+)").Select(m => (m.Groups[1].Value.Trim(), m.Groups[2].Value.Trim())).ToArray();

    }

    public static string[]ExtractBulletPoints(string notePath)
    {
        if (!File.Exists(notePath))return[];
        return File.ReadAllLines(notePath).Where(l => l.TrimStart().StartsWith("- ")||l.TrimStart().StartsWith("* ")).Select(l => l.TrimStart('-','*',' ')).ToArray();

    }

    public static string[][]ExtractQuizBlocks(string notePath)
    {
        if (!File.Exists(notePath))return[];
        var content=File.ReadAllText(notePath);
        var results=new List<string[]>();
        var blocks=Regex.Matches(content,@"### Q: (.+?)\n((?:- \[[ x]\] .+\n)+)", RegexOptions.Singleline);
        foreach (Match b in blocks)
        {
            var question=b.Groups[1].Value.Trim();
            var options=Regex.Matches(b.Groups[2].Value,@"- \[([ x])\] (.+)").Select(m => m.Groups[2].Value.Trim()).ToArray();
            var correct=Regex.Matches(b.Groups[2].Value,@"- \[([ x])\] (.+)").Select((m, i) => (m.Groups[1].Value=="x", i)).FirstOrDefault(t => t.Item1).i.ToString();
            results.Add([question,..options, correct]);
        }
        return[..results];

    }

}
public static class TemplateGenerator
{
    public static void RouteItemsToFiles(ExtractedItem[]items)
    {
        LearnDataPaths.EnsureDirectories();
        foreach (var g in items.GroupBy(i => i.ItemType))
        {
            switch (g.Key)
            {
                case"vocab":GenerateVocabFile(g.ToArray());
                break;
                case"flashcard":GenerateDeckFile(g.ToArray());
                break;
                case"snippet":GenerateSnippetFile(g.ToArray());
                break;
            }
        }

    }

    private static void GenerateDeckFile(ExtractedItem[]items)
    {
        var byTopic=items.GroupBy(i => i.Topic);
        foreach (var g in byTopic)
        {
            var path=System.IO.Path.Combine(LearnDataPaths.DecksDir,$"{g.Key}.json");
            var existing=LearnDataPaths.LoadJson<DeckFile>(path);
            var existingCards=existing?.Cards.ToList()??[];
            var newCards=g.Where(i => !existingCards.Any(c => c.Front.Equals(i.Front, StringComparison.OrdinalIgnoreCase))).Select((i, idx) => new FlashCard($"card_{existingCards.Count + idx + 1:000}", i.Front, i.Back, i.Hint, i.Mnemonic, i.ExampleSentence, i.Tags, i.Difficulty, SpacedRepetitionEngine.NewCard())).ToList();
            existingCards.AddRange(newCards);
            var meta=existing?.Meta??new DeckMeta(g.Key, g.Key,"mixed", g.Key,"intermediate", [], DateTimeOffset.Now.ToString("o"), 1);
            LearnDataPaths.SaveJson(path, new DeckFile(meta with
            {
                Version=meta.Version+1
            }
            , [..existingCards]));
        }

    }

    private static void GenerateVocabFile(ExtractedItem[]items)
    {
        var byTopic=items.GroupBy(i => i.SubTopic.Contains("beginner")?"beginner":i.SubTopic.Contains("advanced")?"advanced":"intermediate");
        foreach (var g in byTopic)
        {
            var path=System.IO.Path.Combine(LearnDataPaths.VocabDir,$"{g.Key}.json");
            var existing=LearnDataPaths.LoadJson<VocabFile>(path);
            var words=existing?.Words.ToList()??[];
            foreach (var item in g)
            {
                if (words.Any(w => w.Word.Equals(item.Front, StringComparison.OrdinalIgnoreCase)))continue;
                words.Add(new VocabWord($"word_{words.Count + 1:000}", item.Front,"","noun", item.Back, item.ExampleSentence??"", [], [], item.Difficulty, item.Tags, SpacedRepetitionEngine.NewCard()));
            }
            LearnDataPaths.SaveJson(path, new VocabFile(g.Key, [..words]));
        }

    }

    private static void GenerateSnippetFile(ExtractedItem[]items)
    {
        var byLang=items.GroupBy(i => i.Language);
        foreach (var g in byLang)
        {
            var path=System.IO.Path.Combine(LearnDataPaths.SnippetsDir,$"{g.Key}.json");
            var existing=LearnDataPaths.LoadJson<SnippetsFile>(path);
            var snippets=existing?.Snippets.ToList()??[];
            foreach (var item in g.Where(i => i.CodeSnippetText!=null))snippets.Add(new CodeSnippet($"cs_{snippets.Count + 1:000}", item.Front,"general", item.CodeSnippetText!, item.Back,"", item.Tags, item.Difficulty));
            LearnDataPaths.SaveJson(path, new SnippetsFile(g.Key, [..snippets]));
        }

    }

}
public static class LearnRouter
{
    public static void StartLearning(string topic)
    {
        RefreshData(topic);
        LaunchTool(topic,"auto");

    }

    public static void RefreshData(string topic)
    {
        var cfg=ObsidianBridge.LoadConfig();
        if (cfg==null||!Directory.Exists(cfg.VaultPath))
        {
            SpectrePanel.Warning("No Obsidian vault configured. Run: obsidian");
            return;
        }
        var tagMap=new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["jp"]=["japanese","jp","jlpt"], ["japanese"]=["japanese","jp","jlpt"], ["en"]=["english","vocab","idiom","grammar"], ["english"]=["english","vocab","idiom"], ["cs"]=["csharp","dotnet","cs","snippet"], ["csharp"]=["csharp","dotnet","cs"], ["dsa"]=["dsa","algorithm","leetcode","problem"], ["interview"]=["interview","behavioral","system-design"],
        }
        ;
        var tags=tagMap.GetValueOrDefault(topic, [topic]);
        var notes=ResourceScanner.FindNotesByTag(cfg.VaultPath, tags);
        if (notes.Length==0)
        {
            SpectrePanel.Info($"No notes tagged for topic '{topic}'.");
            return;
        }
        var items=new List<ExtractedItem>();
        SpectreProgress.BulkProgress($"Extracting {notes.Length} notes", notes, (_, note) =>
        {
            var fakeEntry=new ResourceEntry("tmp", note,"md", System.IO.Path.GetFileNameWithoutExtension(note), tags, [topic],"auto","obsidian_note", null, 0, DateTimeOffset.Now.ToString("o"), null,"pending", null, 0, [], true, true);
            items.AddRange(MdExtractor.Extract(note, fakeEntry));
        }
        );
        TemplateGenerator.RouteItemsToFiles([..items]);
        SpectrePanel.Success($"Generated {items.Count} items from {notes.Length} notes → learn/");

    }

    private static void LaunchTool(string topic, string level)
    {
        switch (topic.ToLower())
        {
            case"jp"or"japanese":JlptVocabDrill.Run("N5");
            break;
            case"en"or"english":VocabDrill.Run("Intermediate");
            break;
            case"cs"or"csharp":CsharpQuiz.Run();
            break;
            case"dsa":AlgoVisualizer.PickAndRun();
            break;
            case"interview":InterviewBank.RunRandom();
            break;
            default:FlashcardEngine.PickAndRun(LearnDataPaths.DecksDir);
            break;
        }

    }

}

public sealed record PaletteCommand(string Alias, string Description, string Category);

public static class CommandPalette
{
    public static readonly PaletteCommand[]Commands=[new("proj","Navigate to a registered workspace","Navigation"), new("p","Alias for proj","Navigation"), new("gs","Git status summary","Git"), new("gcmt","Conventional commit wizard","Git"), new("git-undo","Soft-reset the last local commit","Git"), new("dbld","dotnet build in active workspace",".NET"), new("dtst","dotnet test in active workspace",".NET"), new("clean-build","Remove bin/ and obj/ recursively",".NET"), new("add-migration","EF Core: add migration",".NET"), new("update-db","EF Core: update database",".NET"), new("dkcl","Docker cleanup TUI dashboard","Docker"), new("dcup","docker compose up -d","Docker"), new("dcdown","docker compose down","Docker"), new("aws-local","LocalStack sandbox diagnostics","AWS"), new("claude","Launch Claude Code CLI","AI"), new("codex","Launch Codex CLI","AI"), new("openclaw","Launch OpenClaw via Ollama","AI"), new("hermes","Launch Hermes3 via Ollama","AI"), new("hermesd","Launch Hermes3 debug mode","AI"), new("disk","Show disk usage and health","System"), new("public-ip","Resolve public IPv4 address","System"), new("kill-port","Kill process by port number","System"), new("ssh-info","SSH connection summary","System"), new("db-tui","SQLite schema and data viewer","Database"), new("agyswitch","Switch AGY account context","Accounts"), new("agyquota","Show quota usage for all accounts","Accounts"), new("scaffold","Create new project from template","Scaffold"), new("help","Open interactive help browser","Help"), new("cc","Open this Command Palette","Help"), new("learn","Start learning for a topic (auto-refresh)","Learn"), new("flashcard","Open flashcard deck browser","Learn"), new("vocab","English vocabulary drill","Learn"), new("kana","Hiragana / katakana quiz","Learn"), new("kanji","Kanji lookup / stroke detail","Learn"), new("jlpt","JLPT vocabulary drill","Learn"), new("algo","Algorithm visualizer (sort / search)","Learn"), new("complexity","Big-O complexity cheat-sheet","Learn"), new("problems","DSA problem tracker","Learn"), new("snippets","Code snippet library browser","Learn"), new("sheets","Cheat-sheet browser (.txt files)","Learn"), new("quiz","C# multiple-choice quiz","Learn"), new("interview","Interview question bank","Learn"), new("star","STAR answer builder","Learn"), new("mock","Mock interview timer","Learn"), new("word-of-day","Show today's word of the day","Learn"), new("session","Start a Pomodoro study session","Tracking"), new("stats","Study statistics and weekly chart","Tracking"), new("goals","Daily learning goals","Tracking"), new("streak","Study streak display","Tracking"), new("due","Show due spaced-repetition reviews","Tracking"), new("progress","Progress dashboard (bar chart + tree)","Tracking"), new("weak","Weak items queue (pre-session review)","Tracking"), new("obsidian","Configure / browse Obsidian vault","Obsidian"), new("obs-graph","Obsidian wikilink graph","Obsidian"), new("nexus","Git Nexus multi-repo dashboard","Git"), new("repo-graph","Repository dependency graph","Git"), new("nexus-stats","Git Nexus commit stats","Git"), new("ide","Terminal IDE (browse, view, diff)","IDE"), new("ide-diff","Git diff viewer for current dir","IDE"), new("ide-search","Search pattern across files","IDE"), new("refresh","Refresh learning data from vault","Resources"), new("add-resource","Add a file/URL to resource registry","Resources"),];

    public static void Show()
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new Rule("[bold cyan]Command Palette[/]").RuleStyle("grey"));
        var categories=Commands.Select(c => c.Category).Distinct().ToArray();
        var catIdx=SpectreMenu.Show("Category", ["All",..categories], 0, true);
        IEnumerable<PaletteCommand>filtered=catIdx<=0?Commands:Commands.Where(c => c.Category==categories[catIdx-1]);
        var items=filtered.Select(c => $"{c.Alias,-20} {c.Description}").ToArray();
        var cmds=filtered.Select(c => c.Alias).ToArray();
        var selected=SpectreMenu.Show(["Command Palette","Select a command to view details"], items, cmds, 0, true, false);
        if (selected>=0)
        {
            var cmd=filtered.ElementAt(selected);
            AnsiConsole.Write(new Panel($"[bold]Alias:[/] {cmd.Alias.EscapeMarkup()}\n"+$"[bold]Category:[/] {cmd.Category.EscapeMarkup()}\n"+$"[bold]Description:[/] {cmd.Description.EscapeMarkup()}")
            {
                Header=new PanelHeader("[bold cyan]Command Details[/]"), Border=BoxBorder.Rounded
            }
            );
        }

    }

}
public static class ProfileHelp
{
    private static readonly FrozenDictionary<string, string[]>HelpTopics=new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
    {
        ["Navigation"]=["proj <query>  — Navigate to a workspace matching <query>."," If multiple matches are found an interactive selector opens."," If exactly one matches, jumps immediately.","Alias: p",], ["Git"]=["gs            — Short git status (--short) with color coding.","gcmt          — Conventional commit wizard. Prompts for:"," 1. Type: feat | fix | docs | style | refactor | test | chore | ci"," 2. Scope (optional)"," 3. Short description (5–72 chars)"," 4. Breaking changes / issues closed","git-undo      — Soft-reset the last commit (keeps changes staged).",], [".NET"]=["dbld          — dotnet build in the active workspace.","dtst          — dotnet test in the active workspace.","clean-build   — Recursively delete all bin/ and obj/ folders.","add-migration — dotnet ef migrations add <name>","update-db     — dotnet ef database update",], ["Docker"]=["dkcl          — Docker cleanup TUI dashboard. Options:"," • Stop & remove all containers"," • Prune images and dangling layers"," • Delete unused volumes / networks"," • Full cleanup (all of the above)","dcup          — docker compose up -d","dcdown        — docker compose down",], ["AWS / LocalStack"]=["aws-local     — Query running LocalStack sandbox on http://localhost:4566."," Shows: S3 buckets, SQS queues, Lambda functions.",], ["AI / LLM"]=["claude        — Launch Claude Code CLI.","codex         — Launch Codex CLI.","openclaw      — Launch OpenClaw model via local Ollama daemon.","hermes        — Launch Hermes3 model via Ollama.","hermesd       — Launch Hermes3 in debug mode."," Note: Ollama daemon on port 11434 is started automatically if offline.",], ["System"]=["disk          — Disk partitions, free space ratios, health status.","public-ip     — Resolve external IPv4 via REST fallback chain.","kill-port <n> — Terminate the process listening on TCP port <n>.","ssh-info      — Local IPs, Tailscale address, active SSH connections.",], ["Database"]=["db-tui <path> — Open SQLite file in interactive schema/data viewer."," Requires sqlite3 CLI on PATH.",], ["Accounts"]=["agyswitch     — Switch the active AGY/Gemini account context.","agyquota      — Show quota usage summary for all accounts.",], ["Scaffold"]=["scaffold      — Interactive project boilerplate creator."," Templates: webapi · console · react (Vite) · blazorwasm · classlib · worker",],

    }
    .ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    public static void Show()
    {
        AnsiConsole.Write(new Rule("[bold cyan]Help Browser[/]").RuleStyle("grey"));
        var topics=HelpTopics.Keys.ToArray();
        var idx=SpectreMenu.Show("Help Topics", topics, 0, true);
        if (idx<0)return;
        var topic=topics[idx];
        SpectrePager.Show($"Help: {topic}", HelpTopics[topic]);

    }

}
public static class AgyHeader
{
    public static void ShowSplash()
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new FigletText("AGY").Centered().Color(Color.Green));
        AnsiConsole.Write(new Rule("[bold green]Antigravity Account Manager  v2.0[/]").RuleStyle("grey"));
        AnsiConsole.WriteLine();
        var active=AgyAccountCore.GetActiveAccount();
        var stats=AgyAccountCore.GetAccountStats(active);
        var quota=AgyAccountCore.CalculateRollingQuotas(active);
        var grid=new Grid();
        grid.AddColumn(new GridColumn().PadLeft(4));
        grid.AddRow($"[cyan]Active account[/]  : [green bold]{active.EscapeMarkup()}[/]");
        grid.AddRow($"[cyan]Login status[/]    : {(stats.TokenStatus == "Logged In" ? "[green]● Logged In[/]" : "[red]○ Not Logged In[/]")}");
        grid.AddRow($"[cyan]Weekly quota[/]    : {AgyAccountCore.GetProgressBar(quota.RemainingWeekly)}");
        AnsiConsole.Write(grid);
        AnsiConsole.WriteLine();

        try
        {
            var w=WordOfDay.Pick();
            if (w!=null)WordOfDay.Render(w);
        }
        catch
        {
        }
        try
        {
            StudyStreak.ShowPanel();
        }
        catch
        {
        }
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[dim]               Press Enter to continue[/]");
        Console.ReadKey(true);
        AnsiConsole.Clear();

    }

}
public static class AgyAccountDisplay
{
    public static void ShowQuotaChart(string accountName)
    {
        var quota=AgyAccountCore.CalculateRollingQuotas(accountName);
        AnsiConsole.Write(new Rule($"[bold cyan]Quota: {accountName.EscapeMarkup()}[/]").RuleStyle("grey"));
        var chart=new BarChart().Width(60).Label($"[bold]Remaining Quota % — {accountName.EscapeMarkup()}[/]").CenterLabel().AddItem("Gemini Weekly", quota.RemainingWeekly, Color.Cyan1).AddItem("Gemini 5-Hour", quota.Remaining5H, Color.Yellow).AddItem("Claude Weekly", 100.0, Color.Green).AddItem("Claude 5-Hour", 100.0, Color.Blue);
        AnsiConsole.Write(chart);
        AnsiConsole.MarkupLine($"[dim] Weekly : {quota.CountWeekly,4} / 1000 requests · Refreshes in {quota.TimeWeekly}[/]");
        AnsiConsole.MarkupLine($"[dim] 5-Hour : {quota.Count5H,4} / 50   requests · Refreshes in {quota.Time5H}[/]");

    }

    public static void ShowAccountTree()
    {
        var accounts=AgyAccountCore.GetAccounts();
        var active=AgyAccountCore.GetActiveAccount();
        var tree=new Tree("[bold cyan]AGY Accounts[/]");
        foreach (var acc in accounts)
        {
            var stats=AgyAccountCore.GetAccountStats(acc);
            var label=acc==active?$"[green bold]★ {acc.EscapeMarkup()} (Active)[/]":acc.EscapeMarkup();
            var node=tree.AddNode(label);
            node.AddNode($"[dim]Login:[/]  {(stats.TokenStatus == "Logged In" ? "[green]Logged In[/]" : "[red]Not Logged In[/]")}");
            node.AddNode($"[dim]Convos:[/] {stats.ConversationsCount}  [dim]Skills:[/] {stats.SkillsCount}");
            node.AddNode($"[dim]Weekly:[/] {(int)Math.Round(stats.GeminiWeekly)}%  [dim]5h:[/] {(int)Math.Round(stats.GeminiFiveHour)}%");
            node.AddNode($"[dim]Size:[/]   {stats.PrivateSize}  [dim]Junctions:[/] {stats.JunctionStatus.EscapeMarkup()}");
        }
        AnsiConsole.Write(tree);

    }

    public static string[]MultiSelectAccounts(string prompt="Select accounts:")
    {
        var accounts=AgyAccountCore.GetAccounts();
        if (accounts.Length==0)
        {
            SpectrePanel.Warning("No accounts found.");
            return[];
        }
        try
        {
            return[..AnsiConsole.Prompt(new MultiSelectionPrompt<string>().Title(prompt).PageSize(12).HighlightStyle(new Style(Color.Green)).InstructionsText("[grey](Space to select · Enter to confirm)[/]").AddChoices(accounts))];
        }
        catch
        {
            return[];
        }

    }

    public static void BulkAccountOperation(string label, string selectPrompt, Action<string>action)
    {
        var selected=MultiSelectAccounts(selectPrompt);
        if (selected.Length==0)
        {
            SpectrePanel.Info("Nothing selected.");
            return;
        }
        SpectreProgress.BulkProgress(label, selected, (_, acc) => action(acc));

    }

}
public static class LearnDataPaths
{
    public static string LearnRoot => System.IO.Path.Combine(AgyAccountCore.AgySourceHome,"learn");

    public static string DecksDir => System.IO.Path.Combine(LearnRoot,"decks");

    public static string VocabDir => System.IO.Path.Combine(LearnRoot,"vocab");

    public static string JlptDir => System.IO.Path.Combine(LearnRoot,"jlpt");

    public static string SnippetsDir => System.IO.Path.Combine(LearnRoot,"snippets");

    public static string SheetsDir => System.IO.Path.Combine(LearnRoot,"cheatsheets");

    public static string WordBankFile => System.IO.Path.Combine(LearnRoot,"word_bank.json");

    public static string KanaFile => System.IO.Path.Combine(LearnRoot,"kana.json");

    public static string KanjiFile => System.IO.Path.Combine(LearnRoot,"kanji.json");

    public static string ComplexityFile => System.IO.Path.Combine(LearnRoot,"complexity.json");

    public static string QuizFile => System.IO.Path.Combine(LearnRoot,"csharp_quiz.json");

    public static string InterviewFile => System.IO.Path.Combine(LearnRoot,"interview_questions.json");

    public static string StarFile => System.IO.Path.Combine(LearnRoot,"star_answers.json");

    public static string ProblemsFile => System.IO.Path.Combine(LearnRoot,"problems.json");

    public static string StudyLogFile => System.IO.Path.Combine(LearnRoot,"study_log.json");

    public static string ObsidianCfgFile => System.IO.Path.Combine(AgyAccountCore.AgySourceHome,"obsidian_config.json");

    public static string ResourcesIndex => System.IO.Path.Combine(AgyAccountCore.AgySourceHome,"resources","index.json");

    public static void EnsureDirectories()
    {
        foreach (var d in new[]
        {
            LearnRoot, DecksDir, VocabDir, JlptDir, SnippetsDir, SheetsDir
        }
        )Directory.CreateDirectory(d);

    }

    private static readonly JsonSerializerOptions _js=new()
    {
        PropertyNameCaseInsensitive=true, WriteIndented=true

    }
    ;

    public static T?LoadJson<T>(string path)where T:class
    {
        if (!File.Exists(path))return null;

        try
        {
            return JsonSerializer.Deserialize<T>(File.ReadAllText(path), _js);
        }
        catch
        {
            return null;
        }

    }

    public static void SaveJson<T>(string path, T obj)
    {
        try
        {
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
            File.WriteAllText(path, JsonSerializer.Serialize(obj, _js), Encoding.UTF8);
        }
        catch
        {
        }

    }

}

public sealed record SrState(double EaseFactor, int IntervalDays, int Repetitions, DateTime?LastReviewed, DateTime?NextReview, string Status);

public sealed record SrResult(SrState Updated, bool Passed, int NextIntervalDays);

public static class SpacedRepetitionEngine
{
    public static SrState NewCard() => new(2.5, 0, 0, null, null,"new");

    public static bool IsDueToday(SrState sr) => sr.NextReview==null||sr.NextReview.Value.Date<=DateTime.Today;

    public static SrResult UpdateCard(SrState current, int quality)
    {
        bool passed=quality>=3;
        double ef=Math.Max(1.3, current.EaseFactor+0.1-(5-quality)*(0.08+(5-quality)*0.02));
        int reps=passed?current.Repetitions+1:0;
        int interval=reps switch
        {
            0 => 1, 1 => 1, 2 => 6, _ => (int)Math.Round(current.IntervalDays*ef)
        }
        ;
        if (!passed)
        {
            interval=1;
            ef=current.EaseFactor;
        }
        string status=!passed?"learning":interval>21?"mastered":"review";
        var updated=new SrState(ef, interval, reps, DateTime.Now, DateTime.Today.AddDays(interval), status);
        return new SrResult(updated, passed, interval);

    }

    public static int CardsRemaining(SrState[]states) => states.Count(IsDueToday);

}

public sealed record FlashCard(string Id, string Front, string Back, string?Hint, string?Mnemonic, string?ExampleSentence, string[]Tags, int Difficulty, SrState Sr);

public sealed record DeckMeta(string Id, string Title, string Language, string Topic, string Level, string[]SourceNotes, string GeneratedAt, int Version);

public sealed record DeckFile(DeckMeta Meta, FlashCard[]Cards);

public static class FlashcardEngine
{
    public static void Run(string deckPath)
    {
        var deck=LearnDataPaths.LoadJson<DeckFile>(deckPath);
        if (deck==null||deck.Cards.Length==0)
        {
            SpectrePanel.Warning("Deck not found or empty.");
            return;
        }
        Run(deck.Cards, deck.Meta.Title);

    }

    public static void Run(FlashCard[]cards, string deckName)
    {
        if (cards.Length==0)
        {
            SpectrePanel.Info("No cards in deck.");
            return;
        }
        var queue=cards.Where(c => SpacedRepetitionEngine.IsDueToday(c.Sr)).ToList();
        if (queue.Count==0)
        {
            SpectrePanel.Success($"All {cards.Length} cards in '{deckName}' are up to date!");
            return;
        }
        int known=0, again=0;
        foreach (var card in queue)
        {
            AnsiConsole.Clear();
            AnsiConsole.Write(new Rule($"[bold cyan]Flashcard: {deckName.EscapeMarkup()}[/]").RuleStyle("grey"));
            AnsiConsole.MarkupLine($"[dim]Card {known + again + 1} / {queue.Count}  ·  ✓ {known} known  ·  ✗ {again} again[/]");
            AnsiConsole.WriteLine();
            AnsiConsole.Write(new Panel($"[bold]{card.Front.EscapeMarkup()}[/]"+(card.Hint!=null?$"\n[dim]{card.Hint.EscapeMarkup()}[/]":""))
            {
                Header=new PanelHeader("[dim]Front[/]"), Border=BoxBorder.Rounded, BorderStyle=new Style(Color.Cyan1), Padding=new Padding(1, 1)
            }
            );
            AnsiConsole.MarkupLine("[dim]  Press Enter to reveal · Esc to exit[/]");
            var key=Console.ReadKey(true);
            if (key.Key==ConsoleKey.Escape)break;
            AnsiConsole.Clear();
            AnsiConsole.Write(new Rule($"[bold cyan]Flashcard: {deckName.EscapeMarkup()}[/]").RuleStyle("grey"));
            var backContent=card.Back.EscapeMarkup()+(card.ExampleSentence!=null?$"\n\n[dim]\"{card.ExampleSentence.EscapeMarkup()}\"[/]":"")+(card.Mnemonic!=null?$"\n[yellow]💡 {card.Mnemonic.EscapeMarkup()}[/]":"");
            AnsiConsole.Write(new Panel(backContent)
            {
                Header=new PanelHeader("[green bold]✓ Back[/]"), Border=BoxBorder.Rounded, BorderStyle=new Style(Color.Green), Padding=new Padding(1, 1)
            }
            );
            if (AnsiConsole.Confirm("[bold]Did you know it?[/]", defaultValue:false))known++;

            else again++;
        }
        AnsiConsole.Clear();
        SpectrePanel.Success($"Session complete — ✓ {known} known  ✗ {again} missed  ({queue.Count} cards reviewed)");

    }

    public static DeckFile[]GetDecks(string decksDir)
    {
        if (!Directory.Exists(decksDir))return[];
        return Directory.GetFiles(decksDir,"*.json").Select(f => LearnDataPaths.LoadJson<DeckFile>(f)).OfType<DeckFile>().ToArray();

    }

    public static void PickAndRun(string decksDir)
    {
        var decks=GetDecks(decksDir);
        if (decks.Length==0)
        {
            SpectrePanel.Warning($"No decks found in {decksDir}");
            return;
        }
        var names=decks.Select(d => d.Meta.Title).ToArray();
        var idx=SpectreMenu.Show("Select Flashcard Deck", names, 0, true);
        if (idx>=0)Run(decks[idx].Cards, decks[idx].Meta.Title);

    }

}

public sealed record KanaEntry(string Char, string Romaji, string Row, string Type, SrState Sr);

public sealed record KanaFile(KanaEntry[]Hiragana, KanaEntry[]Katakana);

public static class KanaQuiz
{
    public static void Run(string type="hiragana")
    {
        var kana=LearnDataPaths.LoadJson<KanaFile>(LearnDataPaths.KanaFile);
        if (kana==null)
        {
            SpectrePanel.Warning("kana.json not found. Run: learn jp");
            return;
        }
        KanaEntry[]pool=type switch
        {
            "katakana" => kana.Katakana,"both" => [..kana.Hiragana,..kana.Katakana], _ => kana.Hiragana
        }
        ;
        var due=pool.Where(k => SpacedRepetitionEngine.IsDueToday(k.Sr)).ToArray();
        if (due.Length==0)
        {
            SpectrePanel.Success("All kana are up to date!");
            return;
        }
        var rowStats=new Dictionary<string, (int c, int t)>(StringComparer.OrdinalIgnoreCase);
        int correct=0;
        foreach (var entry in due)
        {
            AnsiConsole.Clear();
            AnsiConsole.Write(new Rule($"[bold cyan]Kana Quiz — {type}[/]").RuleStyle("grey"));
            AnsiConsole.MarkupLine($"[dim]Score: {correct}/{due.IndexOf(entry)} · Due: {due.Length}[/]");
            AnsiConsole.WriteLine();
            AnsiConsole.Write(new FigletText(entry.Char).Centered().Color(Color.Green));
            AnsiConsole.WriteLine();
            var answer=AnsiConsole.Ask<string>("[cyan]Romaji:[/]").Trim().ToLower();
            bool ok=answer==entry.Romaji.ToLower();
            AnsiConsole.MarkupLine(ok?$"[green]✓ Correct!  {entry.Char} = {entry.Romaji}[/]":$"[red]✗ Wrong — {entry.Char} = {entry.Romaji}  (you typed: {answer.EscapeMarkup()})[/]");
            if (ok)correct++;
            rowStats.TryGetValue(entry.Row, out var stat);
            rowStats[entry.Row]=(stat.c+(ok?1:0), stat.t+1);
            Thread.Sleep(600);
        }
        ShowAccuracyChart(rowStats);

    }

    public static void ShowAccuracyChart(Dictionary<string, (int c, int t)>rowStats)
    {
        if (rowStats.Count==0)return;
        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule("[bold cyan]Row Accuracy[/]").RuleStyle("grey"));
        var chart=new BarChart().Width(50).Label("[bold]Accuracy %[/]").CenterLabel();
        foreach (var(row, (c, t))in rowStats.OrderBy(x => x.Key))
        {
            double pct=t>0?Math.Round(c*100.0/t, 0):0;
            chart.AddItem($"{row}-row", pct, pct>=80?Color.Green:pct>=50?Color.Yellow:Color.Red);
        }
        AnsiConsole.Write(chart);

    }

}
static class ListExtensions
{
    public static int IndexOf<T>(this T[]arr, T item) => Array.IndexOf(arr, item);

}

public sealed record ExampleWord(string Word, string Reading, string Meaning);

public sealed record KanjiEntry(string Char, string[]Onyomi, string[]Kunyomi, string Meaning, string JlptLevel, int StrokeCount, string[]Radicals, ExampleWord[]ExampleWords, string?Mnemonic, string[]Tags, SrState Sr);

public sealed record KanjiFile(KanjiEntry[]Kanji);

public static class KanjiLookup
{
    public static void Run()
    {
        var file=LearnDataPaths.LoadJson<KanjiFile>(LearnDataPaths.KanjiFile);
        if (file==null||file.Kanji.Length==0)
        {
            SpectrePanel.Warning("kanji.json not found. Run: learn jp");
            return;
        }
        var all=file.Kanji;
        while (true)
        {
            AnsiConsole.Clear();
            AnsiConsole.Write(new Rule("[bold cyan]Kanji Lookup[/]").RuleStyle("grey"));
            var query=AnsiConsole.Ask<string>("[cyan]Search[/] (meaning/kana, Enter=quit):").Trim();
            if (string.IsNullOrWhiteSpace(query))return;
            var results=Search(all, query);
            if (results.Length==0)
            {
                SpectrePanel.Warning($"No kanji matched '{query}'");
                continue;
            }
            var items=results.Select(k => $"{k.Char}  {k.Meaning,-20}  {k.JlptLevel,-3}  {string.Join("、", k.Kunyomi)}").ToArray();
            var idx=SpectreMenu.Show($"Results for '{query}'", items, 0, false);
            if (idx>=0)ShowDetail(results[idx]);
        }

    }

    public static KanjiEntry[]Search(KanjiEntry[]all, string query) => all.Where(k => k.Meaning.Contains(query, StringComparison.OrdinalIgnoreCase)||k.Char.Contains(query)||k.Onyomi.Any(o => o.Contains(query, StringComparison.OrdinalIgnoreCase))||k.Kunyomi.Any(u => u.Contains(query, StringComparison.OrdinalIgnoreCase))).ToArray();

    public static void ShowDetail(KanjiEntry k)
    {
        var lines=new List<string>
        {
            $"Meaning   : {k.Meaning}",$"On-yomi   : {string.Join("、", k.Onyomi)}",$"Kun-yomi  : {string.Join("、", k.Kunyomi)}",$"JLPT      : {k.JlptLevel}",$"Strokes   : {k.StrokeCount}",$"Radicals  : {string.Join(" ", k.Radicals)}","","Example words", new string('─', 40)
        }
        ;
        foreach (var ex in k.ExampleWords)lines.Add($"  {ex.Word}  {ex.Reading,-10}  {ex.Meaning}");
        if (k.Mnemonic!=null)
        {
            lines.Add("");
            lines.Add($"💡 {k.Mnemonic}");
        }
        SpectrePager.Show($"Kanji: {k.Char}", [..lines]);

    }

}

public sealed record JlptWord(string Id, string Word, string Reading, string Romaji, string Meaning, string PartOfSpeech, string JlptLevel, string ExampleJp, string ExampleEn, string[]Tags, SrState Sr);

public sealed record JlptFile(string JlptLevel, JlptWord[]Words);

public static class JlptVocabDrill
{
    public static void Run(string level="N5")
    {
        var path=System.IO.Path.Combine(LearnDataPaths.JlptDir,$"{level}.json");
        var data=LearnDataPaths.LoadJson<JlptFile>(path);
        if (data==null||data.Words.Length==0)
        {
            SpectrePanel.Warning($"No JLPT {level} data found. Run: learn jp");
            return;
        }
        var cards=data.Words.Where(w => SpacedRepetitionEngine.IsDueToday(w.Sr)).Select(w => new FlashCard(w.Id, w.Word,$"{w.Reading}  {w.Meaning}", w.Romaji, null, w.ExampleJp+" / "+w.ExampleEn, w.Tags, 3, w.Sr)).ToArray();
        FlashcardEngine.Run(cards,$"JLPT {level}");

    }

}
public static class AlgoVisualizer
{
    public static void PickAndRun()
    {
        var algos=new[]
        {
            "Bubble Sort","Binary Search","Merge Sort"
        }
        ;
        var idx=SpectreMenu.Show("Algorithm Visualizer", algos, 0, false);
        var arr=GenerateArray(8);
        switch (idx)
        {
            case 0:RunBubbleSort([..arr]);
            break;
            case 1:RunBinarySearch([..arr.OrderBy(x => x)], arr[0]);
            break;
            case 2:RunMergeSort([..arr]);
            break;
        }

    }

    public static void RunBubbleSort(int[]input)
    {
        var a=(int[])input.Clone();
        int step=0, comps=0, swaps=0;
        for (int i=0;
        i<a.Length-1;
        i++)for (int j=0;
        j<a.Length-i-1;
        j++)
        {
            RenderArray(a, j, j+1,++step, comps, swaps,"Bubble Sort");
            comps++;
            if (a[j]>a[j+1])
            {
                (a[j], a[j+1])=(a[j+1], a[j]);
                swaps++;
            }
            if (Console.ReadKey(true).Key==ConsoleKey.Escape)return;
        }
        RenderArray(a,-1,-1, step, comps, swaps,"Bubble Sort — Done");
        Console.ReadKey(true);

    }

    public static void RunBinarySearch(int[]sorted, int target)
    {
        int lo=0, hi=sorted.Length-1, step=0;
        while (lo<=hi)
        {
            int mid=(lo+hi)/2;
            RenderArray(sorted, lo, hi,++step, 0, 0,$"Binary Search: target={target} mid={sorted[mid]}");
            if (sorted[mid]==target)
            {
                SpectrePanel.Success($"Found {target} at index {mid}!");
                return;
            }
            if (sorted[mid]<target)lo=mid+1;

            else hi=mid-1;
            if (Console.ReadKey(true).Key==ConsoleKey.Escape)return;
        }
        SpectrePanel.Warning($"{target} not found.");

    }

    public static void RunMergeSort(int[]input)
    {
        var a=(int[])input.Clone();
        int step=0;
        MergeSortHelper(a, 0, a.Length-1, ref step);
        RenderArray(a,-1,-1, step, 0, 0,"Merge Sort — Done");
        Console.ReadKey(true);

    }

    private static void MergeSortHelper(int[]a, int lo, int hi, ref int step)
    {
        if (lo>=hi)return;
        int mid=(lo+hi)/2;
        MergeSortHelper(a, lo, mid, ref step);
        MergeSortHelper(a, mid+1, hi, ref step);
        int[]merged=new int[hi-lo+1];
        int l=lo, r=mid+1, k=0;
        while (l<=mid&&r<=hi)merged[k++]=a[l]<=a[r]?a[l++]:a[r++];
        while (l<=mid)merged[k++]=a[l++];
        while (r<=hi)merged[k++]=a[r++];
        for (int i=0;
        i<merged.Length;
        i++)a[lo+i]=merged[i];
        RenderArray(a, lo, hi,++step, 0, 0,$"Merge Sort — merged [{lo}..{hi}]");
        Console.ReadKey(true);

    }

    private static void RenderArray(int[]a, int lo, int hi, int step, int comps, int swaps, string label)
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new Rule($"[bold cyan]AGY — Algo: {label.EscapeMarkup()}[/]").RuleStyle("grey"));
        AnsiConsole.MarkupLine($"[dim]Step {step}  · Comparisons: {comps}  · Swaps: {swaps}[/]");
        AnsiConsole.WriteLine();
        var t=new Table
        {
            Border=TableBorder.Rounded
        }
        ;
        for (int i=0;
        i<a.Length;
        i++)t.AddColumn(new TableColumn("").Centered());
        t.AddRow(a.Select((v, i) =>
        {
            bool hl=i>=lo&&i<=hi;
            return hl?$"[green bold]{v}[/]":v.ToString();
        }
        ).ToArray());
        AnsiConsole.Write(t);
        if (lo>=0&&hi>=0&&lo<a.Length)AnsiConsole.MarkupLine($"[dim]  comparing indices {lo}–{hi}[/]");
        AnsiConsole.MarkupLine("[dim]  Enter next step · Esc exit[/]");

    }

    private static int[]GenerateArray(int size)
    {
        var rng=new Random();
        return Enumerable.Range(0, size).Select(_ => rng.Next(1, 20)).ToArray();

    }

}

public sealed record ComplexityEntry(string Name, string Access, string Search, string Insert, string Delete, string Space, string Notes, string[]Tags);

public sealed record AlgoEntry(string Name, string Best, string Average, string Worst, string Space, string Category, string Notes, string[]Tags);

public sealed record ComplexityFile(ComplexityEntry[]DataStructures, AlgoEntry[]Algorithms);

public static class ComplexitySheet
{
    public static void Run()
    {
        var categories=new[]
        {
            "Data Structures","Sorting Algorithms","Search Algorithms"
        }
        ;
        while (true)
        {
            var idx=SpectreMenu.Show("Big-O Complexity Sheet", [..categories,"← Back"], 0, false);
            if (idx<0||idx>=categories.Length)return;
            ShowCategory(categories[idx]);
        }

    }

    public static void ShowCategory(string category)
    {
        var data=LearnDataPaths.LoadJson<ComplexityFile>(LearnDataPaths.ComplexityFile);
        if (category=="Data Structures")
        {
            var rows=(data?.DataStructures??GetDefaultStructures()).Select(e => new[]
            {
                e.Name, e.Access, e.Search, e.Insert, e.Delete, e.Space, e.Notes
            }
            ).ToArray();
            SpectreTable.Render(["Structure","Access","Search","Insert","Delete","Space","Notes"], rows);
        }
        else
        {
            var rows=(data?.Algorithms??GetDefaultAlgorithms()).Where(a => category=="Sorting Algorithms"?a.Category=="sort":a.Category=="search").Select(a => new[]
            {
                a.Name, a.Best, a.Average, a.Worst, a.Space, a.Notes
            }
            ).ToArray();
            SpectreTable.Render(["Algorithm","Best","Average","Worst","Space","Notes"], rows);
        }
        AnsiConsole.MarkupLine("[dim]  Press any key...[/]");
        Console.ReadKey(true);

    }

    private static ComplexityEntry[]GetDefaultStructures() => [new("Array","O(1)","O(n)","O(n)","O(n)","O(n)","random access O(1)", []), new("Linked List","O(n)","O(n)","O(1)","O(1)","O(n)","prepend O(1)", []), new("Hash Table","N/A","O(1)","O(1)","O(1)","O(n)","worst O(n) collision", []), new("BST","O(log n)","O(log n)","O(log n)","O(log n)","O(n)","balanced only", []), new("Heap","O(1)*","O(n)","O(log n)","O(log n)","O(n)","*min/max only", []), new("Stack/Queue","O(n)","O(n)","O(1)","O(1)","O(n)","push/pop O(1)", []),];

    private static AlgoEntry[]GetDefaultAlgorithms() => [new("Merge Sort","O(n log n)","O(n log n)","O(n log n)","O(n)","sort","stable", []), new("Quick Sort","O(n log n)","O(n log n)","O(n²)","O(log n)","sort","in-place", []), new("Heap Sort","O(n log n)","O(n log n)","O(n log n)","O(1)","sort","in-place", []), new("Bubble Sort","O(n)","O(n²)","O(n²)","O(1)","sort","simple", []), new("Binary Search","O(1)","O(log n)","O(log n)","O(1)","search","sorted array", []), new("BFS/DFS","O(V+E)","O(V+E)","O(V+E)","O(V)","search","graph traversal", []),];

}

public sealed record Problem(string Id, string Title, string Source, string Url, string Difficulty, string[]Topics, string Status, string TimeComplexity, string SpaceComplexity, string ApproachNotes, int Attempts, string?FirstSolvedAt, string?LastReviewedAt, string[]Tags);

public sealed record ProblemsFile(Problem[]Problems);

public static class ProblemTracker
{
    public static void Run()
    {
        while (true)
        {
            var data=Load();
            AnsiConsole.Clear();
            AnsiConsole.Write(new Rule("[bold cyan]Problem Tracker[/]").RuleStyle("grey"));
            var rows=data.Select(p => new[]
            {
                p.Title, p.Difficulty, string.Join(", ", p.Topics), p.Status=="solved"?"[green]✓ Solved[/]":p.Status=="review"?"[yellow]↺ Review[/]":"[dim]○ Todo[/]"
            }
            ).ToArray();
            SpectreTable.Render(["Title","Diff","Topics","Status"], rows, markup:true);
            var actions=new[]
            {
                "[n] Add problem","[f] Filter by topic","← Back"
            }
            ;
            var idx=SpectreMenu.Show("Problem Tracker", actions, 0, false);
            if (idx==0)Add();

            else return;
        }

    }

    public static void Add()
    {
        var title=AnsiConsole.Ask<string>("[cyan]Title:[/]").Trim();
        var source=AnsiConsole.Ask<string>("[dim]Source[/] (e.g. LeetCode #1):","").Trim();
        var diff=AnsiConsole.Prompt(new SelectionPrompt<string>().Title("Difficulty").AddChoices("easy","medium","hard"));
        var topics=AnsiConsole.Ask<string>("[dim]Topics[/] (comma-separated):","").Split(',').Select(t => t.Trim()).Where(t => t.Length>0).ToArray();
        var data=Load().ToList();
        var id=$"p_{(data.Count + 1):000}";
        data.Add(new Problem(id, title, source,"", diff, topics,"todo","?","?","", 0, null, null, []));
        Save([..data]);
        SpectrePanel.Success($"Problem '{title}' added.");

    }

    public static Problem[]Filter(Problem[]all, string?topic, string?status)
    {
        IEnumerable<Problem>q=all;
        if (!string.IsNullOrEmpty(topic))q=q.Where(p => p.Topics.Any(t => t.Contains(topic, StringComparison.OrdinalIgnoreCase)));
        if (!string.IsNullOrEmpty(status))q=q.Where(p => p.Status==status);
        return[..q];

    }

    private static Problem[]Load()
    {
        var f=LearnDataPaths.LoadJson<ProblemsFile>(LearnDataPaths.ProblemsFile);
        return f?.Problems??[];

    }

    private static void Save(Problem[]problems) => LearnDataPaths.SaveJson(LearnDataPaths.ProblemsFile, new ProblemsFile(problems));

}

public sealed record CodeSnippet(string Id, string Title, string Category, string Code, string Explanation, string UseCase, string[]Tags, int Difficulty);

public sealed record SnippetsFile(string Language, CodeSnippet[]Snippets);

public static class SnippetLibrary
{
    public static void Run()
    {
        var langs=Directory.Exists(LearnDataPaths.SnippetsDir)?Directory.GetFiles(LearnDataPaths.SnippetsDir,"*.json").Select(f => System.IO.Path.GetFileNameWithoutExtension(f)).ToArray():new[]
        {
            "csharp","powershell","sql"
        }
        ;
        if (langs.Length==0)
        {
            SpectrePanel.Warning("No snippet files found.");
            return;
        }
        var langIdx=SpectreMenu.Show("Snippet Library", [..langs,"← Back"], 0, false);
        if (langIdx<0||langIdx>=langs.Length)return;
        var lang=langs[langIdx];
        var path=System.IO.Path.Combine(LearnDataPaths.SnippetsDir,$"{lang}.json");
        var file=LearnDataPaths.LoadJson<SnippetsFile>(path);
        if (file==null||file.Snippets.Length==0)
        {
            SpectrePanel.Warning($"No {lang} snippets found.");
            return;
        }
        var titles=file.Snippets.Select(s => s.Title).ToArray();
        var idx=SpectreMenu.Show($"{lang} Snippets", [..titles,"← Back"], 0, true);
        if (idx<0||idx>=file.Snippets.Length)return;
        var snip=file.Snippets[idx];
        var lines=new List<string>
        {
            $"[bold]{snip.Title.EscapeMarkup()}[/]",$"[dim]{snip.Category.EscapeMarkup()} · Difficulty {snip.Difficulty}[/]","", snip.Code.EscapeMarkup(),"",$"[cyan]{snip.Explanation.EscapeMarkup()}[/]","",$"[dim]Use case: {snip.UseCase.EscapeMarkup()}[/]"
        }
        ;
        SpectrePager.Show($"{lang}: {snip.Title}", [..lines]);
        if (AnsiConsole.Confirm("Copy to clipboard?", defaultValue:false))CopyToClipboard(snip.Code);

    }

    public static void CopyToClipboard(string text)
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var psi=new ProcessStartInfo("clip")
                {
                    UseShellExecute=false, RedirectStandardInput=true
                }
                ;

                using var p=Process.Start(psi)!;
                p.StandardInput.Write(text);
                p.StandardInput.Close();
                p.WaitForExit();
                SpectrePanel.Success("Copied to clipboard.");
            }
            else SpectrePanel.Warning("Clipboard only supported on Windows.");
        }
        catch (Exception ex)
        {
            SpectrePanel.Error($"Clipboard error: {ex.Message}");
        }

    }

}
public static class CheatSheetBrowser
{
    public static void Run()
    {
        var sheets=Directory.Exists(LearnDataPaths.SheetsDir)?Directory.GetFiles(LearnDataPaths.SheetsDir,"*.txt").Select(f => System.IO.Path.GetFileNameWithoutExtension(f)).ToArray():new[]
        {
            "csharp","powershell","sql","bash","regex","git","docker"
        }
        ;
        var idx=SpectreMenu.Show("Cheat Sheets", [..sheets,"← Back"], 0, false);
        if (idx<0||idx>=sheets.Length)return;
        Show(System.IO.Path.Combine(LearnDataPaths.SheetsDir,$"{sheets[idx]}.txt"), sheets[idx]);

    }

    public static void Show(string filePath, string title)
    {
        if (!File.Exists(filePath))
        {
            SpectrePanel.Warning($"Cheat sheet not found: {filePath}");
            return;
        }
        var lines=File.ReadAllLines(filePath);
        SpectrePager.Show($"Cheat Sheet: {title}", lines);

    }

}

public sealed record QuizQuestion(string Id, string Topic, int Difficulty, string Question, string[]Options, int CorrectAnswer, string Explanation, string?CodeSnippet, string[]Tags);

public sealed record QuizFile(QuizQuestion[]Questions);

public static class CsharpQuiz
{
    public static void Run(string?topic=null)
    {
        var file=LearnDataPaths.LoadJson<QuizFile>(LearnDataPaths.QuizFile);
        if (file==null||file.Questions.Length==0)
        {
            SpectrePanel.Warning("No quiz data. Run: learn cs");
            return;
        }
        var questions=topic!=null?file.Questions.Where(q => q.Topic.Equals(topic, StringComparison.OrdinalIgnoreCase)).ToArray():file.Questions;
        if (questions.Length==0)
        {
            SpectrePanel.Warning($"No questions for topic '{topic}'");
            return;
        }
        var scores=new Dictionary<string, (int c, int t)>(StringComparer.OrdinalIgnoreCase);
        foreach (var q in questions.OrderBy(_ => Guid.NewGuid()).Take(10))
        {
            AnsiConsole.Clear();
            AnsiConsole.Write(new Rule($"[bold cyan]C# Quiz — {q.Topic.EscapeMarkup()}[/]").RuleStyle("grey"));
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine($"[bold]{q.Question.EscapeMarkup()}[/]");
            if (q.CodeSnippet!=null)
            {
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine($"[dim]{q.CodeSnippet.EscapeMarkup()}[/]");
            }
            AnsiConsole.WriteLine();
            var chosen=SpectreMenu.Show("Select answer", q.Options, 0, false);
            bool correct=chosen==q.CorrectAnswer;
            scores.TryGetValue(q.Topic, out var s);
            scores[q.Topic]=(s.c+(correct?1:0), s.t+1);
            AnsiConsole.Write(new Panel((correct?"[green]✓ Correct![/]":$"[red]✗ Wrong — answer: {q.Options[q.CorrectAnswer].EscapeMarkup()}[/]")+$"\n\n{q.Explanation.EscapeMarkup()}")
            {
                Border=BoxBorder.Rounded, BorderStyle=new Style(correct?Color.Green:Color.Red), Padding=new Padding(1, 0)
            }
            );
            Console.ReadKey(true);
        }
        ShowResults(scores);

    }

    public static void ShowResults(Dictionary<string, (int c, int t)>scores)
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new Rule("[bold cyan]Quiz Results[/]").RuleStyle("grey"));
        var rows=scores.Select(kv => new[]
        {
            kv.Key, kv.Value.c.ToString(), kv.Value.t.ToString(),$"{kv.Value.c * 100 / Math.Max(1, kv.Value.t)}%"
        }
        ).ToArray();
        SpectreTable.Render(["Topic","Correct","Total","Score%"], rows);

    }

}

public sealed record InterviewQuestion(string Id, string Type, string Category, string Difficulty, string Question, string Format, string[]Hints, string[]Companies, string[]Tags);

public sealed record InterviewFile(InterviewQuestion[]Questions);

public static class InterviewBank
{
    public static void Run()
    {
        var file=LearnDataPaths.LoadJson<InterviewFile>(LearnDataPaths.InterviewFile);
        if (file==null||file.Questions.Length==0)
        {
            SpectrePanel.Warning("No interview data. Run: learn interview");
            return;
        }
        var all=file.Questions;
        while (true)
        {
            AnsiConsole.Clear();
            AnsiConsole.Write(new Rule($"[bold cyan]Interview Bank — {all.Length} questions[/]").RuleStyle("grey"));
            var items=all.Select(q => $"{q.Question,-55}  [dim]{q.Type}[/]").ToArray();
            var actions=new[]
            {
                "[r] Random question","[f] Filter by type","← Back"
            }
            ;
            var topIdx=SpectreMenu.Show("Options", actions, 0, false);
            if (topIdx==0)
            {
                ShowQuestion(all[new Random().Next(all.Length)]);
                continue;
            }
            if (topIdx==1)
            {
                var types=all.Select(q => q.Type).Distinct().ToArray();
                var tIdx=SpectreMenu.Show("Filter by type", types, 0, false);
                if (tIdx>=0)
                {
                    var filtered=Filter(all, types[tIdx], null, null);
                    var qIdx=SpectreMenu.Show($"Type: {types[tIdx]}", filtered.Select(q => q.Question).ToArray(), 0, true);
                    if (qIdx>=0)ShowQuestion(filtered[qIdx]);
                }
                continue;
            }
            return;
        }

    }

    public static void RunRandom()
    {
        var file=LearnDataPaths.LoadJson<InterviewFile>(LearnDataPaths.InterviewFile);
        if (file==null||file.Questions.Length==0)
        {
            SpectrePanel.Warning("No interview data.");
            return;
        }
        ShowQuestion(file.Questions[new Random().Next(file.Questions.Length)]);

    }

    public static void ShowQuestion(InterviewQuestion q)
    {
        var lines=new List<string>
        {
            $"[bold cyan]{q.Type.EscapeMarkup()}  ·  {q.Category.EscapeMarkup()}  ·  {q.Difficulty.EscapeMarkup()}[/]", new string('─', 50),"",$"[bold]{q.Question.EscapeMarkup()}[/]","",$"[dim]Format: {q.Format.EscapeMarkup()}[/]",
        }
        ;
        if (q.Hints.Length>0)
        {
            lines.Add("");
            lines.Add("[cyan]Hints:[/]");
            foreach (var h in q.Hints)lines.Add($"  • {h.EscapeMarkup()}");
        }
        if (q.Companies.Length>0)lines.Add($"\n[dim]Companies: {string.Join(", ", q.Companies).EscapeMarkup()}[/]");
        SpectrePager.Show($"Interview: {q.Type}", [..lines]);

    }

    public static InterviewQuestion[]Filter(InterviewQuestion[]all, string?type, string?difficulty, string?tag)
    {
        IEnumerable<InterviewQuestion>q=all;
        if (!string.IsNullOrEmpty(type))q=q.Where(x => x.Type.Equals(type, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrEmpty(difficulty))q=q.Where(x => x.Difficulty.Equals(difficulty, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrEmpty(tag))q=q.Where(x => x.Tags.Any(t => t.Contains(tag, StringComparison.OrdinalIgnoreCase)));
        return[..q];

    }

}

public sealed record StarAnswer(string Id, string QuestionId, string QuestionText, string Situation, string Task, string Action, string Result, string OutcomeMetric, string CreatedAt, string UpdatedAt, string[]Tags, int Rating);

public sealed record StarFile(StarAnswer[]Answers);

public static class StarBuilder
{
    public static void Run()
    {
        AnsiConsole.Write(new Rule("[bold cyan]STAR Answer Builder[/]").RuleStyle("grey"));
        var question=AnsiConsole.Ask<string>("[bold]Interview question:[/]").Trim();
        if (string.IsNullOrWhiteSpace(question))return;
        AnsiConsole.MarkupLine("[dim]Answer each section. Press Enter when done.[/]\n");
        var situation=AnsiConsole.Ask<string>("[cyan]Situation[/] (set the context):").Trim();
        var task=AnsiConsole.Ask<string>("[cyan]Task[/] (your responsibility):").Trim();
        var action=AnsiConsole.Ask<string>("[cyan]Action[/] (what you did):").Trim();
        var result=AnsiConsole.Ask<string>("[cyan]Result[/] (outcome):").Trim();
        var metric=AnsiConsole.Ask<string>("[dim]Outcome metric[/] (optional):","").Trim();
        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Panel($"[bold]S:[/] {situation.EscapeMarkup()}\n"+$"[bold]T:[/] {task.EscapeMarkup()}\n"+$"[bold]A:[/] {action.EscapeMarkup()}\n"+$"[bold]R:[/] {result.EscapeMarkup()}"+(metric.Length>0?$"\n[dim]{metric.EscapeMarkup()}[/]":""))
        {
            Header=new PanelHeader("[bold]✓ STAR Answer[/]"), Border=BoxBorder.Rounded, BorderStyle=new Style(Color.Green), Padding=new Padding(1, 1)
        }
        );
        if (!AnsiConsole.Confirm("Save this answer?", defaultValue:false))return;
        var file=LearnDataPaths.LoadJson<StarFile>(LearnDataPaths.StarFile)??new StarFile([]);
        var answers=file.Answers.ToList();
        var now=DateTimeOffset.Now.ToString("o");
        answers.Add(new StarAnswer($"star_{answers.Count + 1:000}","", question, situation, task, action, result, metric, now, now, [], 3));
        LearnDataPaths.SaveJson(LearnDataPaths.StarFile, new StarFile([..answers]));
        SpectrePanel.Success("STAR answer saved.");

    }

    public static void Review()
    {
        var file=LearnDataPaths.LoadJson<StarFile>(LearnDataPaths.StarFile);
        if (file==null||file.Answers.Length==0)
        {
            SpectrePanel.Info("No saved STAR answers.");
            return;
        }
        var items=file.Answers.Select(a => a.QuestionText).ToArray();
        var idx=SpectreMenu.Show("Saved STAR Answers", items, 0, true);
        if (idx<0)return;
        var a=file.Answers[idx];
        SpectrePager.Show($"STAR: {a.QuestionText[..Math.Min(40, a.QuestionText.Length)]}", [$"[bold]Question:[/] {a.QuestionText.EscapeMarkup()}","",$"[bold]S:[/] {a.Situation.EscapeMarkup()}","",$"[bold]T:[/] {a.Task.EscapeMarkup()}","",$"[bold]A:[/] {a.Action.EscapeMarkup()}","",$"[bold]R:[/] {a.Result.EscapeMarkup()}", a.OutcomeMetric.Length>0?$"\n[dim]{a.OutcomeMetric.EscapeMarkup()}[/]":"",$"\n[dim]Created: {a.CreatedAt}[/]"]);

    }

}
public static class MockInterviewTimer
{
    public static void Run(int timeLimitSeconds=300)
    {
        var file=LearnDataPaths.LoadJson<InterviewFile>(LearnDataPaths.InterviewFile);
        InterviewQuestion[]questions=file?.Questions??[];
        if (questions.Length==0)
        {
            SpectrePanel.Warning("No interview data.");
            return;
        }
        RunSession(questions.OrderBy(_ => Guid.NewGuid()).Take(3).ToArray(), timeLimitSeconds);

    }

    public static void RunSession(InterviewQuestion[]questions, int timeLimitSeconds)
    {
        foreach (var q in questions)
        {
            var start=DateTime.Now;
            AnsiConsole.Live(new Table
            {
                Border=TableBorder.None
            }
            ).Start(ctx =>
            {
                while ((DateTime.Now-start).TotalSeconds<timeLimitSeconds&&!Console.KeyAvailable)
                {
                    var elapsed=DateTime.Now-start;
                    var pct=Math.Min(100.0, elapsed.TotalSeconds/timeLimitSeconds*100.0);
                    AnsiConsole.Clear();
                    AnsiConsole.Write(new Rule($"[bold cyan]Mock Interview[/] [dim]{elapsed:mm\\:ss} / {TimeSpan.FromSeconds(timeLimitSeconds):mm\\:ss}[/]").RuleStyle("grey"));
                    AnsiConsole.Write(new Panel($"[bold]{q.Type.EscapeMarkup()}[/]\n\n[bold white]{q.Question.EscapeMarkup()}[/]"+(q.Hints.Length>0?$"\n\n[dim]Hint: {q.Hints[0].EscapeMarkup()}[/]":""))
                    {
                        Border=BoxBorder.Rounded, BorderStyle=new Style(Color.Cyan1), Padding=new Padding(1, 1)
                    }
                    );
                    int bars=(int)(pct/100.0*40);
                    AnsiConsole.MarkupLine($"[cyan]{'█'.ToString().PadRight(bars, '█').PadRight(40, '░')}[/]  {pct:F0}%");
                    AnsiConsole.MarkupLine("[dim]  Esc stop early · Enter mark done & next[/]");
                    Thread.Sleep(500);
                }
                if (Console.KeyAvailable)Console.ReadKey(true);
            }
            );
            if (!AnsiConsole.Confirm("Continue to next question?", defaultValue:true))break;
        }
        SpectrePanel.Success("Mock interview session complete.");

    }

}

public sealed record VocabWord(string Id, string Word, string Pronunciation, string PartOfSpeech, string Definition, string ExampleSentence, string[]Synonyms, string[]Antonyms, int Difficulty, string[]Tags, SrState Sr);

public sealed record VocabFile(string Level, VocabWord[]Words);

public static class VocabDrill
{
    public static void Run(string difficulty="Intermediate")
    {
        var file=System.IO.Path.Combine(LearnDataPaths.VocabDir,$"{difficulty.ToLower()}.json");
        var vocab=LearnDataPaths.LoadJson<VocabFile>(file);
        if (vocab==null||vocab.Words.Length==0)
        {
            SpectrePanel.Warning($"No vocabulary data for level '{difficulty}'. Run refresh-data first.");
            return;
        }
        var due=vocab.Words.Where(w => SpacedRepetitionEngine.IsDueToday(w.Sr)).ToArray();
        if (due.Length==0)
        {
            SpectrePanel.Success($"All {difficulty} vocabulary is up to date!");
            return;
        }
        int correct=0, total=0;
        foreach (var word in due)
        {
            AnsiConsole.Clear();
            AnsiConsole.Write(new Rule($"[bold cyan]{difficulty} Vocab[/]").RuleStyle("grey"));
            AnsiConsole.MarkupLine($"[dim]Word {total + 1} / {due.Length}  ·  Weak queue: {due.Length - total}[/]");
            AnsiConsole.WriteLine();
            AnsiConsole.Write(new Panel($"[bold]{word.Word.EscapeMarkup()}[/]\n[dim]{word.Pronunciation.EscapeMarkup()}[/]")
            {
                Header=new PanelHeader("[cyan]ℹ[/]"), Border=BoxBorder.Rounded, BorderStyle=new Style(Color.Cyan1), Padding=new Padding(1, 1)
            }
            );
            AnsiConsole.MarkupLine("[dim]  Press Enter to reveal definition[/]");
            if (Console.ReadKey(true).Key==ConsoleKey.Escape)break;
            AnsiConsole.Clear();
            AnsiConsole.Write(new Rule($"[bold cyan]{difficulty} Vocab[/]").RuleStyle("grey"));
            var detail=$"[bold]{word.Word.EscapeMarkup()}[/]  [dim]{word.PartOfSpeech.EscapeMarkup()}[/]\n\n"+$"{word.Definition.EscapeMarkup()}\n\n"+$"[italic dim]\"{word.ExampleSentence.EscapeMarkup()}\"[/]";
            if (word.Synonyms.Length>0)detail+=$"\n[dim]Synonyms: {string.Join(", ", word.Synonyms).EscapeMarkup()}[/]";
            AnsiConsole.Write(new Panel(detail)
            {
                Header=new PanelHeader("[green]Definition[/]"), Border=BoxBorder.Rounded, BorderStyle=new Style(Color.Green), Padding=new Padding(1, 1)
            }
            );
            if (AnsiConsole.Confirm("Did you know it?", defaultValue:false))correct++;
            total++;
        }
        SpectrePanel.Success($"Vocab drill done — {correct}/{total} correct");

    }

}

public sealed record StudyScore(int Correct, int Total, double Percentage);

public sealed record StudyLogEntry(string Id, string Date, string StartTime, string EndTime, int DurationMinutes, string Topic, string SubTopic, string Activity, StudyScore Score, string[]WeakItems, int PomodoroCount, string Notes, string[]Tags);

public sealed record GoalTarget(string Topic, string Activity, int Count, int Completed);

public sealed record DailyGoalData(string Date, GoalTarget[]Targets);

public sealed record StreakData(int Current, int Best, string LastActive, int DaysThisWeek);

public sealed record DueItem(string Topic, string ItemId, string Front, DateTime NextReview, bool Overdue);

public sealed record MasteryData(string Topic, int Total, int Mastered, int Learning, int NewItems);

public sealed record WeakItem(string Topic, string ItemId, string FrontText, int FailCount);

public sealed record StudySummary(string Topic, int Score, int Total, string[]WeakItems, int DurationMinutes);

public sealed record StudyLogFile(DailyGoalData?DailyGoals, StudyLogEntry[]Sessions);

public static class StudySession
{
    public static void Run(string topic, int workMin=25, int breakMin=5)
    {
        LearnDataPaths.EnsureDirectories();
        int cycle=0;
        var start=DateTime.Now;
        var allWeak=new List<string>();
        while (true)
        {
            cycle++;
            AnsiConsole.Clear();
            AnsiConsole.Write(new Rule($"[bold cyan]Study Session — {topic.EscapeMarkup()}[/]").RuleStyle("grey"));
            AnsiConsole.MarkupLine($"[dim]Mode: Work · Cycle {cycle}[/]");
            RunTimer($"Work: {topic}", workMin*60, Color.Green);
            AnsiConsole.Write(new Panel("[green]Work block complete! Take a break.[/]")
            {
                Header=new PanelHeader("[green bold]✓[/]"), Border=BoxBorder.Rounded, BorderStyle=new Style(Color.Green)
            }
            );
            Thread.Sleep(500);
            if (!AnsiConsole.Confirm($"Continue to break ({breakMin} min)?", defaultValue:true))break;
            RunTimer("Break", breakMin*60, Color.Yellow);
            if (!AnsiConsole.Confirm("Continue next cycle?", defaultValue:true))break;
        }
        var duration=(int)(DateTime.Now-start).TotalMinutes;
        var notes=AnsiConsole.Ask<string>("[dim]Session notes[/] (optional):","").Trim();
        Record(topic,"general","pomodoro", new StudyScore(0, 0, 0), [..allWeak], cycle, duration, notes);
        SpectrePanel.Success($"Session complete — {cycle} cycles · {duration} min");

    }

    private static void RunTimer(string label, int totalSecs, Color barColor)
    {
        var start=DateTime.Now;
        while (true)
        {
            if (Console.KeyAvailable&&Console.ReadKey(true).Key==ConsoleKey.Escape)break;
            var elapsed=(int)(DateTime.Now-start).TotalSeconds;
            if (elapsed>=totalSecs)break;
            var pct=elapsed*100.0/totalSecs;
            var remain=TimeSpan.FromSeconds(totalSecs-elapsed);
            AnsiConsole.Clear();
            AnsiConsole.Write(new Rule($"[bold]{label.EscapeMarkup()}[/]").RuleStyle("grey"));
            int bars=(int)(pct/100.0*40);
            AnsiConsole.MarkupLine($"[{(barColor == Color.Green ? "green" : "yellow")}]{'█'.ToString().PadRight(bars, '█').PadRight(40, '░')}[/]  {pct:F0}%");
            AnsiConsole.MarkupLine($"[dim]{elapsed / 60:00}:{elapsed % 60:00} elapsed · {remain:mm\\:ss} remaining[/]");
            AnsiConsole.MarkupLine("[dim]Esc to end early[/]");
            Thread.Sleep(1000);
        }

    }

    public static void Record(string topic, string subTopic, string activity, StudyScore score, string[]weakItems, int pomodoros, int durationMin, string notes)
    {
        var log=LearnDataPaths.LoadJson<StudyLogFile>(LearnDataPaths.StudyLogFile)??new StudyLogFile(null, []);
        var sessions=log.Sessions.ToList();
        var now=DateTime.Now;
        var id=$"s_{sessions.Count + 1:000}";
        sessions.Add(new StudyLogEntry(id, now.ToString("yyyy-MM-dd"), now.ToString("HH:mm"), now.ToString("HH:mm"), durationMin, topic, subTopic, activity, score, weakItems, pomodoros, notes, []));
        LearnDataPaths.SaveJson(LearnDataPaths.StudyLogFile, new StudyLogFile(log.DailyGoals, [..sessions]));

    }

}
public static class StudyStats
{
    public static void Run()
    {
        var log=LearnDataPaths.LoadJson<StudyLogFile>(LearnDataPaths.StudyLogFile);
        if (log==null||log.Sessions.Length==0)
        {
            SpectrePanel.Info("No study sessions recorded yet.");
            return;
        }
        ShowWeeklyChart(log.Sessions);
        AnsiConsole.WriteLine();
        ShowRecentTable(log.Sessions, 10);
        AnsiConsole.MarkupLine($"\n[bold]Current streak:[/] [yellow]{GetCurrentStreak(log.Sessions)} days 🔥[/]");
        AnsiConsole.MarkupLine("[dim]Press any key...[/]");
        Console.ReadKey(true);

    }

    public static void ShowWeeklyChart(StudyLogEntry[]logs)
    {
        AnsiConsole.Write(new Rule("[bold cyan]Minutes studied (last 7 days)[/]").RuleStyle("grey"));
        var cutoff=DateTime.Today.AddDays(-7);
        var byTopic=logs.Where(s => DateTime.TryParse(s.Date, out var d)&&d>=cutoff).GroupBy(s => s.Topic).ToDictionary(g => g.Key, g => g.Sum(s => s.DurationMinutes));
        var chart=new BarChart().Width(50).Label("[bold]Minutes[/]").CenterLabel();
        foreach (var(topic, mins)in byTopic.OrderByDescending(x => x.Value))chart.AddItem(topic, mins, Color.Cyan1);
        AnsiConsole.Write(chart);

    }

    public static void ShowRecentTable(StudyLogEntry[]logs, int days)
    {
        AnsiConsole.Write(new Rule("[bold cyan]Recent Sessions[/]").RuleStyle("grey"));
        var recent=logs.TakeLast(days).Reverse().ToArray();
        var rows=recent.Select(s => new[]
        {
            s.Date+" "+s.StartTime, s.Topic, s.Activity, s.Score.Total>0?$"{s.Score.Correct}/{s.Score.Total} ({s.Score.Percentage:F0}%)":$"{s.DurationMinutes}min"
        }
        ).ToArray();
        SpectreTable.Render(["Date","Topic","Activity","Score/Duration"], rows);

    }

    public static int GetCurrentStreak(StudyLogEntry[]logs)
    {
        var dates=logs.Select(s => s.Date).Distinct().OrderByDescending(d => d).ToArray();
        int streak=0;
        var check=DateTime.Today;
        foreach (var d in dates)
        {
            if (!DateTime.TryParse(d, out var dt))continue;
            if (dt.Date==check.Date)
            {
                streak++;
                check=check.AddDays(-1);
            }
            else break;
        }
        return streak;

    }

}
public static class DailyGoals
{
    public static void Show()
    {
        var data=LoadToday();
        AnsiConsole.Write(new Rule($"[bold cyan]Daily Goals: {data.Date}[/]").RuleStyle("grey"));
        if (data.Targets.Length==0)
        {
            AnsiConsole.MarkupLine("[dim]  No goals set today. Press n to add.[/]");
        }
        else
        {
            var sb=new StringBuilder();
            foreach (var t in data.Targets)
            {
                bool done=t.Completed>=t.Count;
                int bars=t.Count>0?(int)(t.Completed*16.0/t.Count):0;
                var bar=new string('█', Math.Min(16, bars))+new string('░', Math.Max(0, 16-bars));
                sb.AppendLine($"  {(done ? "[green]✓[/]" : "[red]✗[/]")}  {t.Topic,-12} {t.Activity,-12}  [{bar}] {t.Completed}/{t.Count}");
            }
            int complete=data.Targets.Count(t => t.Completed>=t.Count);
            AnsiConsole.Write(new Panel(sb.ToString().TrimEnd())
            {
                Border=BoxBorder.Rounded, BorderStyle=new Style(Color.Cyan1), Padding=new Padding(1, 0)
            }
            );
            AnsiConsole.MarkupLine($"[dim]  {complete} / {data.Targets.Length} goals complete[/]");
        }

    }

    public static void SetGoal(string topic, string activity, int count)
    {
        var data=LoadToday();
        var targets=data.Targets.ToList();
        var existing=targets.FindIndex(t => t.Topic==topic&&t.Activity==activity);
        if (existing>=0)targets[existing]=targets[existing]with
        {
            Count=count
        }
        ;

        else targets.Add(new GoalTarget(topic, activity, count, 0));
        SaveToday(data with
        {
            Targets=[..targets]
        }
        );
        SpectrePanel.Success($"Goal set: {topic}/{activity} = {count}");

    }

    public static void UpdateProgress(string topic, string activity, int completedCount)
    {
        var data=LoadToday();
        var targets=data.Targets.ToList();
        var idx=targets.FindIndex(t => t.Topic==topic&&t.Activity==activity);
        if (idx>=0)targets[idx]=targets[idx]with
        {
            Completed=completedCount
        }
        ;
        SaveToday(data with
        {
            Targets=[..targets]
        }
        );

    }

    public static bool AllComplete() => LoadToday().Targets.All(t => t.Completed>=t.Count);

    private static DailyGoalData LoadToday()
    {
        var log=LearnDataPaths.LoadJson<StudyLogFile>(LearnDataPaths.StudyLogFile);
        var today=DateTime.Today.ToString("yyyy-MM-dd");
        var goals=log?.DailyGoals;
        if (goals!=null&&goals.Date==today)return goals;
        return new DailyGoalData(today, []);

    }

    private static void SaveToday(DailyGoalData data)
    {
        var log=LearnDataPaths.LoadJson<StudyLogFile>(LearnDataPaths.StudyLogFile)??new StudyLogFile(null, []);
        LearnDataPaths.SaveJson(LearnDataPaths.StudyLogFile, log with
        {
            DailyGoals=data
        }
        );

    }

}
public static class StudyStreak
{
    public static StreakData Calculate()
    {
        var log=LearnDataPaths.LoadJson<StudyLogFile>(LearnDataPaths.StudyLogFile);
        var sessions=log?.Sessions??[];
        var dates=sessions.Select(s => s.Date).Distinct().Where(d => DateTime.TryParse(d, out _)).OrderByDescending(d => d).ToArray();
        int current=StudyStats.GetCurrentStreak(sessions);
        int best=0, run=0;
        for (int i=0;
        i<dates.Length;
        i++)
        {
            if (i==0||(DateTime.Parse(dates[i-1])-DateTime.Parse(dates[i])).TotalDays==1)run++;

            else run=1;
            if (run>best)best=run;
        }
        var lastActive=dates.Length>0?dates[0]:"Never";
        var weekAgo=DateTime.Today.AddDays(-7).ToString("yyyy-MM-dd");
        int daysThisWeek=dates.Count(d => string.Compare(d, weekAgo, StringComparison.Ordinal)>=0);
        return new StreakData(current, best, lastActive, daysThisWeek);

    }

    public static void ShowPanel()
    {
        var s=Calculate();
        AnsiConsole.Write(new Panel($"🔥 Current streak : [bold yellow]{s.Current} days[/]\n"+$"🏆 Best streak    : [bold green]{s.Best} days[/]\n"+$"📅 Last active    : [cyan]{s.LastActive}[/]\n"+$"📊 This week      : [dim]{s.DaysThisWeek} / 7 days active[/]")
        {
            Header=new PanelHeader("[bold cyan]Study Streak[/]"), Border=BoxBorder.Rounded, BorderStyle=new Style(Color.Yellow), Padding=new Padding(1, 1)
        }
        );

    }

    public static bool StudiedToday()
    {
        var log=LearnDataPaths.LoadJson<StudyLogFile>(LearnDataPaths.StudyLogFile);
        var today=DateTime.Today.ToString("yyyy-MM-dd");
        return log?.Sessions.Any(s => s.Date==today)??false;

    }

}
public static class DueReview
{
    public static void Show()
    {
        AnsiConsole.Write(new Rule("[bold cyan]Due for Review[/]").RuleStyle("grey"));
        var groups=GetAllDue().GroupBy(d => d.Topic).ToArray();
        if (groups.Length==0)
        {
            SpectrePanel.Success("Nothing due for review today!");
            return;
        }
        var rows=groups.Select(g =>
        {
            int due=g.Count(d => !d.Overdue);
            int over=g.Count(d => d.Overdue);
            var next=g.Where(d => !d.Overdue).OrderBy(d => d.NextReview).FirstOrDefault();
            return new[]
            {
                g.Key, due.ToString(), over.ToString(), next?.NextReview.ToString("yyyy-MM-dd")??"today"
            }
            ;
        }
        ).ToArray();
        SpectreTable.Render(["Topic","Due Today","Overdue","Next Due"], rows);
        AnsiConsole.MarkupLine($"\n[dim]Total: {groups.Sum(g => g.Count())} items due  ·  {groups.Sum(g => g.Count(d => d.Overdue))} overdue[/]");

    }

    public static DueItem[]GetAllDue()
    {
        var all=new List<DueItem>();
        ScanDecks(all);
        ScanJlpt(all);
        return[..all];

    }

    public static DueItem[]GetDueByTopic(string topic) => GetAllDue().Where(d => d.Topic.Equals(topic, StringComparison.OrdinalIgnoreCase)).ToArray();

    private static void ScanDecks(List<DueItem>all)
    {
        if (!Directory.Exists(LearnDataPaths.DecksDir))return;
        foreach (var f in Directory.GetFiles(LearnDataPaths.DecksDir,"*.json"))
        {
            var deck=LearnDataPaths.LoadJson<DeckFile>(f);
            if (deck==null)continue;
            foreach (var card in deck.Cards.Where(c => SpacedRepetitionEngine.IsDueToday(c.Sr)))all.Add(new DueItem(deck.Meta.Topic, card.Id, card.Front, card.Sr.NextReview??DateTime.Today, card.Sr.NextReview!=null&&card.Sr.NextReview.Value.Date<DateTime.Today));
        }

    }

    private static void ScanJlpt(List<DueItem>all)
    {
        if (!Directory.Exists(LearnDataPaths.JlptDir))return;
        foreach (var f in Directory.GetFiles(LearnDataPaths.JlptDir,"*.json"))
        {
            var jlpt=LearnDataPaths.LoadJson<JlptFile>(f);
            if (jlpt==null)continue;
            foreach (var w in jlpt.Words.Where(x => SpacedRepetitionEngine.IsDueToday(x.Sr)))all.Add(new DueItem($"JLPT {jlpt.JlptLevel}", w.Id, w.Word, w.Sr.NextReview??DateTime.Today, w.Sr.NextReview!=null&&w.Sr.NextReview.Value.Date<DateTime.Today));
        }

    }

}
public static class ProgressDashboard
{
    public static void Show()
    {
        AnsiConsole.Clear();
        var log=LearnDataPaths.LoadJson<StudyLogFile>(LearnDataPaths.StudyLogFile);
        var sessions=log?.Sessions??[];
        StudyStats.ShowWeeklyChart(sessions);
        AnsiConsole.WriteLine();
        ShowMasteryTree(sessions);
        AnsiConsole.WriteLine();
        StudyStats.ShowRecentTable(sessions, 5);
        AnsiConsole.MarkupLine("[dim]  Press any key...[/]");
        Console.ReadKey(true);

    }

    public static void ShowMasteryTree(StudyLogEntry[]sessions)
    {
        AnsiConsole.Write(new Rule("[bold cyan]Mastery Tree[/]").RuleStyle("grey"));
        var tree=new Tree("[bold cyan]Topics[/]");
        foreach (var topic in sessions.Select(s => s.Topic).Distinct())
        {
            var m=GetMastery(topic);
            var node=tree.AddNode($"[bold]{topic.EscapeMarkup()}[/]");
            node.AddNode($"[dim]{m.Total} total · [green]{m.Mastered} mastered[/] · [yellow]{m.Learning} learning[/] · [dim]{m.NewItems} new[/]");
        }
        AnsiConsole.Write(tree);

    }

    private static MasteryData GetMastery(string topic)
    {
        int total=0, mastered=0, learning=0, newItems=0;
        if (!Directory.Exists(LearnDataPaths.DecksDir))return new(topic, 0, 0, 0, 0);
        foreach (var f in Directory.GetFiles(LearnDataPaths.DecksDir,"*.json"))
        {
            var deck=LearnDataPaths.LoadJson<DeckFile>(f);
            if (deck==null||!deck.Meta.Topic.Equals(topic, StringComparison.OrdinalIgnoreCase))continue;
            foreach (var c in deck.Cards)
            {
                total++;
                if (c.Sr.Status=="mastered")mastered++;

                else if (c.Sr.Status=="learning"||c.Sr.Status=="review")learning++;

                else newItems++;
            }
        }
        return new(topic, total, mastered, learning, newItems);

    }

}
public static class WeakItemsQueue
{
    public static void ShowPreSessionReview(string topic)
    {
        var items=GetWeakItems(topic);
        if (items.Length==0)return;
        AnsiConsole.Write(new Panel($"You have [yellow]{items.Length}[/] weak items from your last session:\n\n"+string.Join("\n", items.Take(5).Select((w, i) => $"  {i + 1}. {w.FrontText.EscapeMarkup()}  [dim]({w.Topic} — failed {w.FailCount}x)[/]"))+(items.Length>5?$"\n  [dim]... and {items.Length - 5} more[/]":"")+"\n\n[dim]These will be shown first in your session.[/]")
        {
            Header=new PanelHeader("[yellow]⚠ Review Needed[/]"), Border=BoxBorder.Rounded, BorderStyle=new Style(Color.Yellow), Padding=new Padding(1, 1)
        }
        );
        if (!AnsiConsole.Confirm("Start session with weak items first?", defaultValue:true))ClearWeakItems(topic);

    }

    public static WeakItem[]GetWeakItems(string topic)
    {
        var log=LearnDataPaths.LoadJson<StudyLogFile>(LearnDataPaths.StudyLogFile);
        return log?.Sessions.Where(s => s.Topic.Equals(topic, StringComparison.OrdinalIgnoreCase)&&s.WeakItems.Length>0).SelectMany(s => s.WeakItems).GroupBy(w => w).Select(g => new WeakItem(topic, g.Key, g.Key, g.Count())).OrderByDescending(w => w.FailCount).ToArray()??[];

    }

    public static void AddWeakItem(string topic, string itemId)
    {
    }

    public static void ClearWeakItems(string topic)
    {
    }

}

public sealed record ObsidianConfig(string VaultPath);

public sealed record NoteMatch(string Title, string RelativePath, string FullPath);

public sealed record NoteNode(string Title, string NodePath, string[]OutLinks);

public sealed record NoteFrontmatter(string[]Tags, string Topic, string Level, string Type, string Source, string Difficulty);

public static class ObsidianBridge
{
    public static void Configure()
    {
        var vaultPath=AnsiConsole.Ask<string>("[cyan]Obsidian vault path:[/]").Trim();
        if (!Directory.Exists(vaultPath))
        {
            SpectrePanel.Error($"Directory not found: {vaultPath}");
            return;
        }
        LearnDataPaths.SaveJson(LearnDataPaths.ObsidianCfgFile, new ObsidianConfig(vaultPath));
        SpectrePanel.Success($"Vault configured: {vaultPath}");

    }

    public static ObsidianConfig?LoadConfig() => LearnDataPaths.LoadJson<ObsidianConfig>(LearnDataPaths.ObsidianCfgFile);

    public static void Run()
    {
        var cfg=LoadConfig();
        if (cfg==null)
        {
            SpectrePanel.Warning("Obsidian vault not configured. Run: obsidian → Configure.");
        }
        var vaultPath=cfg?.VaultPath??"";
        while (true)
        {
            var actions=new[]
            {
                "Search notes","Browse by tag","Open today's daily note","Obsidian graph","Configure vault path","← Back"
            }
            ;
            var idx=SpectreMenu.Show("Obsidian", actions, 0, false);
            switch (idx)
            {
                case 0:if (Directory.Exists(vaultPath))SearchNotes(vaultPath,"");
                break;
                case 1:if (Directory.Exists(vaultPath))ListByTag(vaultPath);
                break;
                case 2:if (Directory.Exists(vaultPath))ShowDailyNote(vaultPath);
                break;
                case 3:if (Directory.Exists(vaultPath))ObsidianGraph.Run(vaultPath);
                break;
                case 4:Configure();
                cfg=LoadConfig();
                vaultPath=cfg?.VaultPath??"";
                break;
                default:return;
            }
            if (!Directory.Exists(vaultPath)&&idx<4)SpectrePanel.Warning("Configure vault path first.");
        }

    }

    public static void SearchNotes(string vaultPath, string query)
    {
        while (true)
        {
            query=AnsiConsole.Ask<string>("[cyan]Search notes:[/]", query).Trim();
            if (string.IsNullOrWhiteSpace(query))return;
            var matches=SearchFiles(vaultPath, query);
            if (matches.Length==0)
            {
                SpectrePanel.Info($"No notes matched '{query}'.");
                continue;
            }
            var items=matches.Select(m => $"{m.Title,-40}  [dim]{m.RelativePath}[/]").ToArray();
            var idx=SpectreMenu.Show($"Results for \"{query}\"", items, 0, false);
            if (idx>=0)ShowNote(matches[idx].FullPath, matches[idx].Title);
            return;
        }

    }

    public static NoteMatch[]FindByTag(string vaultPath, string tag)
    {
        if (!Directory.Exists(vaultPath))return[];
        return Directory.GetFiles(vaultPath,"*.md", SearchOption.AllDirectories).Select(f => (path:f, fm:ParseFrontmatter(f))).Where(x => x.fm!=null&&x.fm.Tags.Any(t => t.Contains(tag, StringComparison.OrdinalIgnoreCase))).Select(x => new NoteMatch(System.IO.Path.GetFileNameWithoutExtension(x.path), System.IO.Path.GetRelativePath(vaultPath, x.path), x.path)).ToArray();

    }

    public static void ShowDailyNote(string vaultPath)
    {
        var noteFile=System.IO.Path.Combine(vaultPath,$"{DateTime.Today:yyyy-MM-dd}.md");
        if (!File.Exists(noteFile))
        {
            File.WriteAllText(noteFile,$"---\ntags: [daily]\ncreated: {DateTime.Today:yyyy-MM-dd}\n---\n\n# {DateTime.Today:yyyy-MM-dd}\n\n", Encoding.UTF8);
            SpectrePanel.Info($"Created daily note: {noteFile}");
        }
        ShowNote(noteFile, DateTime.Today.ToString("yyyy-MM-dd"));

    }

    public static void ListByTag(string vaultPath)
    {
        var allTags=Directory.GetFiles(vaultPath,"*.md", SearchOption.AllDirectories).SelectMany(f => ParseFrontmatter(f)?.Tags??[]).Distinct().OrderBy(t => t).ToArray();
        if (allTags.Length==0)
        {
            SpectrePanel.Info("No tags found.");
            return;
        }
        var idx=SpectreMenu.Show("Browse by Tag", allTags, 0, true);
        if (idx<0)return;
        var matches=FindByTag(vaultPath, allTags[idx]);
        if (matches.Length==0)
        {
            SpectrePanel.Info($"No notes tagged #{allTags[idx]}");
            return;
        }
        var noteIdx=SpectreMenu.Show($"#{allTags[idx]} ({matches.Length} notes)", matches.Select(m => m.Title).ToArray(), 0, true);
        if (noteIdx>=0)ShowNote(matches[noteIdx].FullPath, matches[noteIdx].Title);

    }

    public static void AppendStudySummary(string vaultPath, string topic, string summary)
    {
        ShowDailyNote(vaultPath);
        var noteFile=System.IO.Path.Combine(vaultPath,$"{DateTime.Today:yyyy-MM-dd}.md");
        var block=$"\n## AGY Study — {DateTime.Now:HH:mm}\n{summary}\n";
        File.AppendAllText(noteFile, block, Encoding.UTF8);
        SpectrePanel.Success("Summary appended to daily note.");

    }

    public static NoteFrontmatter?ParseFrontmatter(string notePath)
    {
        try
        {
            var lines=File.ReadLines(notePath).Take(20).ToArray();
            if (lines.Length==0||lines[0]!="---")return null;
            var fm=lines.Skip(1).TakeWhile(l => l!="---").ToArray();
            string GetField(string key) => fm.FirstOrDefault(l => l.StartsWith(key+":"))?.Split(':', 2)[1].Trim()??"";
            string[]GetTags()
            {
                var tagLine=GetField("tags");
                return Regex.Matches(tagLine,@"[\w-]+").Select(m => m.Value).ToArray();
            }
            return new NoteFrontmatter(GetTags(), GetField("topic"), GetField("level"), GetField("type"), GetField("source"), GetField("difficulty"));
        }
        catch
        {
            return null;
        }

    }

    private static NoteMatch[]SearchFiles(string vaultPath, string query)
    {
        return Directory.GetFiles(vaultPath,"*.md", SearchOption.AllDirectories).Where(f => System.IO.Path.GetFileNameWithoutExtension(f).Contains(query, StringComparison.OrdinalIgnoreCase)||File.ReadLines(f).Take(5).Any(l => l.Contains(query, StringComparison.OrdinalIgnoreCase))).Select(f => new NoteMatch(System.IO.Path.GetFileNameWithoutExtension(f), System.IO.Path.GetRelativePath(vaultPath, f), f)).ToArray();

    }

    private static void ShowNote(string path, string title)
    {
        if (!File.Exists(path))
        {
            SpectrePanel.Error($"File not found: {path}");
            return;
        }
        var lines=File.ReadAllLines(path);
        SpectrePager.Show(title, lines);

    }

}
public static class ObsidianGraph
{
    public static void Run(string vaultPath)
    {
        var graph=BuildGraph(vaultPath);
        var titles=graph.Select(n => n.Title).ToArray();
        var idx=SpectreMenu.Show("Obsidian Graph — Select root note", titles, 0, true);
        if (idx<0)return;
        ShowFromRoot(graph, graph[idx].Title, 2);
        AnsiConsole.WriteLine();
        if (AnsiConsole.Confirm("Show orphan notes?", defaultValue:false))ShowOrphans(graph);

    }

    public static NoteNode[]BuildGraph(string vaultPath)
    {
        if (!Directory.Exists(vaultPath))return[];
        return Directory.GetFiles(vaultPath,"*.md", SearchOption.AllDirectories).Select(f =>
        {
            var content=File.ReadAllText(f);
            var links=Regex.Matches(content,@"\[\[([^\]|]+)(?:\|[^\]]+)?\]\]").Select(m => m.Groups[1].Value.Trim()).ToArray();
            return new NoteNode(System.IO.Path.GetFileNameWithoutExtension(f), f, links);
        }
        ).ToArray();

    }

    public static NoteNode[]FindOrphans(NoteNode[]graph)
    {
        var allTargets=new HashSet<string>(graph.SelectMany(n => n.OutLinks), StringComparer.OrdinalIgnoreCase);
        return graph.Where(n => n.OutLinks.Length==0&&!allTargets.Contains(n.Title)).ToArray();

    }

    public static void ShowFromRoot(NoteNode[]graph, string rootTitle, int depth=2)
    {
        AnsiConsole.Write(new Rule($"[bold cyan]Obsidian Graph — root: {rootTitle.EscapeMarkup()}  depth: {depth}[/]").RuleStyle("grey"));
        var root=graph.FirstOrDefault(n => n.Title.Equals(rootTitle, StringComparison.OrdinalIgnoreCase));
        if (root==null)
        {
            SpectrePanel.Warning($"Note '{rootTitle}' not found.");
            return;
        }
        var tree=new Tree($"[bold]{root.Title.EscapeMarkup()}[/]");
        AddChildren(tree, graph, root, depth, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
        AnsiConsole.Write(tree);

    }

    public static void ShowOrphans(NoteNode[]graph)
    {
        var orphans=FindOrphans(graph);
        if (orphans.Length==0)
        {
            SpectrePanel.Success("No orphan notes found.");
            return;
        }
        AnsiConsole.Write(new Rule("[bold cyan]Orphan Notes[/]").RuleStyle("grey"));
        var rows=orphans.Select(n => new[]
        {
            n.Title, n.NodePath
        }
        ).ToArray();
        SpectreTable.Render(["Note","Path"], rows);

    }

    private static void AddChildren(IHasTreeNodes parent, NoteNode[]graph, NoteNode node, int depth, HashSet<string>visited)
    {
        if (depth<=0||!visited.Add(node.Title))return;
        foreach (var link in node.OutLinks)
        {
            var child=graph.FirstOrDefault(n => n.Title.Equals(link, StringComparison.OrdinalIgnoreCase));
            var childNode=parent.AddNode(child!=null?link.EscapeMarkup():$"[dim]{link.EscapeMarkup()} (not found)[/]");
            if (child!=null)AddChildren(childNode, graph, child, depth-1, visited);
        }

    }

}
public static class ObsidianStudySync
{
    public static void OfferSync(StudySummary summary)
    {
        var cfg=ObsidianBridge.LoadConfig();
        if (cfg==null||!Directory.Exists(cfg.VaultPath))return;
        AnsiConsole.Write(new Panel($"[bold]Append session summary to {DateTime.Today:yyyy-MM-dd}.md?[/]")
        {
            Header=new PanelHeader("[cyan]ℹ Sync to Obsidian?[/]"), Border=BoxBorder.Rounded, BorderStyle=new Style(Color.Cyan1)
        }
        );
        if (!AnsiConsole.Confirm("Sync?", defaultValue:false))return;
        var block=FormatSummaryBlock(summary);
        ObsidianBridge.AppendStudySummary(cfg.VaultPath, summary.Topic, block);

    }

    public static string FormatSummaryBlock(StudySummary s) => $"- **Topic:** {s.Topic}\n"+$"- **Score:** {s.Score} / {s.Total} ({(s.Total > 0 ? s.Score * 100 / s.Total : 0)}%)\n"+(s.WeakItems.Length>0?$"- **Weak words:** {string.Join(", ", s.WeakItems)}\n":"")+$"- **Duration:** {s.DurationMinutes} min";

}

public sealed record RepoStatus(string Name, string RepoPath, string Branch, int AheadBy, int BehindBy, int DirtyFiles, string LastCommit);

public sealed record RepoNode(string Name, string NodePath, string Kind, string[]DependsOn);

public static class GitNexus
{
    public static RepoStatus[]FetchAllStatuses()
    {
        var workspaces=WorkspaceRegistry.GetWorkspaces();
        return workspaces.AsParallel().Select(w => FetchStatus(w.Name, w.WorkspacePath)).OfType<RepoStatus>().ToArray();

    }

    public static void ShowDashboard()
    {
        var cols=new[]
        {
            "Repo","Branch","↑↓","Dirty","Last Commit"
        }
        ;
        AnsiConsole.Write(new Rule("[bold cyan]AGY — Git Nexus[/]").RuleStyle("grey"));
        AnsiConsole.MarkupLine("[dim]  Auto-refreshes · Press any key to exit[/]");
        var t=new Table
        {
            Border=TableBorder.Rounded
        }
        ;
        foreach (var col in cols)t.AddColumn(new TableColumn($"[bold]{col.EscapeMarkup()}[/]"));
        AnsiConsole.Live(t).Start(ctx =>
        {
            int ticks=0;
            while (!Console.KeyAvailable&&ticks<20)
            {
                t.Rows.Clear();
                var statuses=FetchAllStatuses();
                foreach (var s in statuses)
                {
                    var sync=s.AheadBy>0?$"[yellow]↑{s.AheadBy}[/]":s.BehindBy>0?$"[cyan]↓{s.BehindBy}[/]":"[green]sync[/]";
                    t.AddRow(s.Name.EscapeMarkup(), s.Branch.EscapeMarkup(), sync, s.DirtyFiles>0?$"[yellow]{s.DirtyFiles}[/]":"[dim]0[/]", s.LastCommit.EscapeMarkup());
                }
                ctx.Refresh();
                Thread.Sleep(30_000);
                ticks++;
            }
            if (Console.KeyAvailable)Console.ReadKey(true);
        }
        );

    }

    public static void ShowCommitGraph(string workspacePath, int count=20)
    {
        var output=Git(workspacePath,$"log --graph --oneline --decorate -n {count}");
        if (string.IsNullOrWhiteSpace(output))
        {
            SpectrePanel.Info("No commits found.");
            return;
        }
        SpectrePager.Show($"Commit Graph: {System.IO.Path.GetFileName(workspacePath)}", output.Split('\n'));

    }

    private static RepoStatus?FetchStatus(string name, string path)
    {
        if (!Directory.Exists(path))return null;

        try
        {
            var branch=Git(path,"rev-parse --abbrev-ref HEAD").Trim();
            var dirty=Git(path,"status --short").Split('\n').Count(l => !string.IsNullOrWhiteSpace(l));
            var ahead=0;
            var behind=0;
            var ab=Git(path,"rev-list --left-right --count HEAD...@{upstream}").Trim().Split('\t');
            if (ab.Length==2)
            {
                int.TryParse(ab[0], out ahead);
                int.TryParse(ab[1], out behind);
            }
            var last=Git(path,"log --oneline -1").Trim();
            return new RepoStatus(name, path, branch, ahead, behind, dirty, last);
        }
        catch
        {
            return null;
        }

    }

    private static string Git(string workingDir, string args)
    {
        try
        {
            var psi=new ProcessStartInfo("git", args)
            {
                WorkingDirectory=workingDir, RedirectStandardOutput=true, UseShellExecute=false, CreateNoWindow=true
            }
            ;

            using var p=Process.Start(psi)!;
            var output=p.StandardOutput.ReadToEnd();
            p.WaitForExit();
            return output;
        }
        catch
        {
            return string.Empty;
        }

    }

}
public static class RepoGraph
{
    public static RepoNode[]Build()
    {
        var workspaces=WorkspaceRegistry.GetWorkspaces();
        return workspaces.SelectMany(w => ParseWorkspace(w.Name, w.WorkspacePath)).ToArray();

    }

    public static void Show(RepoNode[]graph)
    {
        AnsiConsole.Write(new Rule("[bold cyan]Repo Dependency Graph[/]").RuleStyle("grey"));
        var tree=new Tree("[bold cyan]Workspaces[/]");
        foreach (var node in graph)
        {
            var n=tree.AddNode($"[bold]{node.Name.EscapeMarkup()}[/]  [dim]({node.Kind})[/]");
            foreach (var dep in node.DependsOn)n.AddNode($"→ {dep.EscapeMarkup()}");
            if (node.DependsOn.Length==0)n.AddNode("[dim](no dependencies)[/]");
        }
        AnsiConsole.Write(tree);
        AnsiConsole.MarkupLine("[dim]  Press any key...[/]");
        Console.ReadKey(true);

    }

    public static RepoNode[]ParseCsproj(string path)
    {
        if (!File.Exists(path))return[];
        var content=File.ReadAllText(path);
        var refs=Regex.Matches(content,@"<ProjectReference\s+Include=""([^""]+)""").Select(m => System.IO.Path.GetFileNameWithoutExtension(m.Groups[1].Value)).ToArray();
        return[new RepoNode(System.IO.Path.GetFileNameWithoutExtension(path), path,"csproj", refs)];

    }

    public static RepoNode ParseNpm(string path)
    {
        if (!File.Exists(path))return new("", path,"npm", []);

        try
        {
            var doc=JsonDocument.Parse(File.ReadAllText(path));
            var name=doc.RootElement.TryGetProperty("name", out var n)?n.GetString()??"":"";
            var deps=doc.RootElement.TryGetProperty("dependencies", out var d)?d.EnumerateObject().Select(p => p.Name).ToArray():Array.Empty<string>();
            return new RepoNode(name, path,"npm", deps);
        }
        catch
        {
            return new("", path,"npm", []);
        }

    }

    private static RepoNode[]ParseWorkspace(string name, string path)
    {
        if (!Directory.Exists(path))return[];
        var results=new List<RepoNode>();
        foreach (var csproj in Directory.GetFiles(path,"*.csproj", SearchOption.AllDirectories))results.AddRange(ParseCsproj(csproj));
        foreach (var pkg in Directory.GetFiles(path,"package.json", SearchOption.AllDirectories))results.Add(ParseNpm(pkg));
        if (results.Count==0)results.Add(new RepoNode(name, path,"unknown", []));
        return[..results];

    }

}
public static class GitNexusStats
{
    public static void Run()
    {
        AnsiConsole.Clear();
        var workspaces=WorkspaceRegistry.GetWorkspaces();
        var commitsByRepo=workspaces.ToDictionary(w => w.Name, w => CountCommitsSince(w.WorkspacePath, 7));
        ShowCommitBarChart(commitsByRepo);
        AnsiConsole.WriteLine();
        ShowBranchTree(workspaces);
        AnsiConsole.MarkupLine("[dim]  Press any key...[/]");
        Console.ReadKey(true);

    }

    public static void ShowCommitBarChart(Dictionary<string, int>commitsByRepo)
    {
        AnsiConsole.Write(new Rule("[bold cyan]Commits this week by repo[/]").RuleStyle("grey"));
        var chart=new BarChart().Width(50).Label("[bold]Commits[/]").CenterLabel();
        foreach (var(repo, count)in commitsByRepo.OrderByDescending(x => x.Value))chart.AddItem(repo, count, Color.Cyan1);
        AnsiConsole.Write(chart);

    }

    public static void ShowBranchTree(WorkspaceEntry[]workspaces)
    {
        AnsiConsole.Write(new Rule("[bold cyan]Branch Structure[/]").RuleStyle("grey"));
        var tree=new Tree("[bold cyan]Repos[/]");
        foreach (var ws in workspaces)
        {
            var node=tree.AddNode($"[bold]{ws.Name.EscapeMarkup()}[/]");
            foreach (var branch in GetBranches(ws.WorkspacePath))node.AddNode(branch.StartsWith("*")?$"[green]{branch.EscapeMarkup()}[/]":branch.EscapeMarkup());
        }
        AnsiConsole.Write(tree);

    }

    private static int CountCommitsSince(string path, int days)
    {
        if (!Directory.Exists(path))return 0;

        try
        {
            var psi=new ProcessStartInfo("git",$"log --since={days}.days --oneline")
            {
                WorkingDirectory=path, RedirectStandardOutput=true, UseShellExecute=false, CreateNoWindow=true
            }
            ;

            using var p=Process.Start(psi)!;
            var lines=p.StandardOutput.ReadToEnd().Split('\n').Count(l => !string.IsNullOrWhiteSpace(l));
            p.WaitForExit();
            return lines;
        }
        catch
        {
            return 0;
        }

    }

    private static string[]GetBranches(string path)
    {
        if (!Directory.Exists(path))return[];

        try
        {
            var psi=new ProcessStartInfo("git","branch")
            {
                WorkingDirectory=path, RedirectStandardOutput=true, UseShellExecute=false, CreateNoWindow=true
            }
            ;

            using var p=Process.Start(psi)!;
            var output=p.StandardOutput.ReadToEnd();
            p.WaitForExit();
            return output.Split('\n').Where(l => !string.IsNullOrWhiteSpace(l)).Select(l => l.Trim()).ToArray();
        }
        catch
        {
            return[];
        }

    }

}

public sealed record WordEntry(string Date, string Word, string Pronunciation, string PartOfSpeech, string Definition, string Example, string[]Tags);

public sealed record WordBankFile(WordEntry[]Words);

public static class WordOfDay
{
    public static WordEntry?Pick()
    {
        var bank=LearnDataPaths.LoadJson<WordBankFile>(LearnDataPaths.WordBankFile);
        if (bank==null||bank.Words.Length==0)return null;
        var idx=DateTime.Today.DayOfYear%bank.Words.Length;
        return bank.Words[idx];

    }

    public static void Render(WordEntry word)
    {
        AnsiConsole.Write(new Rule("[bold cyan]📖 Word of the Day[/]").RuleStyle("grey"));
        AnsiConsole.MarkupLine($"\n   [bold green]{word.Word.EscapeMarkup()}[/]  [dim]{word.Pronunciation.EscapeMarkup()}[/]  [yellow]{word.PartOfSpeech.EscapeMarkup()}[/]");
        AnsiConsole.MarkupLine($"   {word.Definition.EscapeMarkup()}");
        AnsiConsole.MarkupLine($"   [italic dim]\"{word.Example.EscapeMarkup()}\"[/]");
        AnsiConsole.WriteLine();

    }

}