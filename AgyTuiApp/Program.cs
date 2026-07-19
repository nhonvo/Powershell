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

using System.Security.AccessControl;

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

    public static int ShowWithEscape(string title, string[] items, int defaultIndex)
    {
        if (items.Length == 0) return -1;
        var selected = defaultIndex;
        Console.CursorVisible = false;

        while (true)
        {
            try
            {
                AnsiConsole.Clear();
            }
            catch {}

            AnsiConsole.Write(new Rule($"[bold cyan]{title.EscapeMarkup()}[/]").RuleStyle("grey"));
            AnsiConsole.WriteLine();

            for (var i = 0; i < items.Length; i++)
            {
                if (i == selected)
                {
                    AnsiConsole.MarkupLine($"[green bold]> {items[i].EscapeMarkup()}[/]");
                }
                else
                {
                    AnsiConsole.MarkupLine($"  {items[i].EscapeMarkup()}");
                }
            }

            AnsiConsole.WriteLine();
            AnsiConsole.Write(new Rule("[dim]↑/↓ Navigate  ·  Enter Select  ·  Esc/q Cancel[/]").RuleStyle("grey"));

            var key = Console.ReadKey(true);
            switch (key.Key)
            {
                case ConsoleKey.UpArrow:
                case ConsoleKey.K:
                    selected = (selected - 1 + items.Length) % items.Length;
                    break;
                case ConsoleKey.DownArrow:
                case ConsoleKey.J:
                    selected = (selected + 1) % items.Length;
                    break;
                case ConsoleKey.Enter:
                    return selected;
                case ConsoleKey.Escape:
                case ConsoleKey.Q:
                    return -1;
            }
        }
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
                AnsiConsole.MarkupLine($"[dim] ↑↓/jk scroll d/u page g/G ends / search q quit"+$" ({top + 1}–{Math.Min(top + pageSize, totalLines)} of {totalLines})[/]");
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
    public static void Spinner(string message, Action action) => AnsiConsole.Status().Spinner(Spectre.Console.Spinner.Known.Dots).SpinnerStyle(new Style(Color.Yellow)).Start(message.EscapeMarkup(), _ => action());

    public static object?SpinnerResult(string message, Func<object?>action)
    {
        object?result=null;
        AnsiConsole.Status().Spinner(Spectre.Console.Spinner.Known.Dots).SpinnerStyle(new Style(Color.Yellow)).Start(message.EscapeMarkup(), _ =>
        {
            result=action();
        }
        );
        return result;

    }

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
public static class LogHelper
{
    public static string GetLogFilePath()
    {
        var logDir=System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),".gemini","antigravity");
        Directory.CreateDirectory(logDir);
        return System.IO.Path.Combine(logDir,"profile.log");

    }

    public static void Log(string message, string level="INFO")
    {
        try
        {
            var line=$"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}";
            File.AppendAllText(GetLogFilePath(), line+Environment.NewLine);
        }
        catch
        {
        }

    }

    public static void LogError(string message, Exception?exception=null)
    {
        var text=exception!=null?$"{message} - Exception: {exception.Message}\n{exception.StackTrace}":message;
        Log(text,"ERROR");

    }

    public static void LogWarning(string message) => Log(message,"WARNING");

    public static void StreamLogs(string?logPath=null)
    {
        if (string.IsNullOrWhiteSpace(logPath))
        {
            var candidates=Directory.GetFiles(".","*.log").Select(f => new FileInfo(f)).OrderByDescending(f => f.LastWriteTime).ToArray();
            if (candidates.Length==0)candidates=Directory.GetFiles(System.IO.Path.GetTempPath(),"*.log").Select(f => new FileInfo(f)).OrderByDescending(f => f.LastWriteTime).ToArray();
            if (candidates.Length>0)logPath=candidates[0].FullName;
        }
        if (string.IsNullOrWhiteSpace(logPath)||!File.Exists(logPath))
        {
            SpectrePanel.Error("No log files found to stream.");
            return;
        }
        AnsiConsole.MarkupLine($"[cyan]Streaming logs from: {logPath.EscapeMarkup()}[/]");
        AnsiConsole.MarkupLine("[dim]Press Ctrl+C to stop streaming.[/]");

        using var stream=new FileStream(logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

        using var reader=new StreamReader(stream);
        stream.Seek(0, SeekOrigin.End);
        var cancelled=false;
        ConsoleCancelEventHandler handler=(_, e) =>
        {
            cancelled=true;
            e.Cancel=true;
        }
        ;
        Console.CancelKeyPress+=handler;

        try
        {
            while (!cancelled)
            {
                var line=reader.ReadLine();
                if (line==null)
                {
                    Thread.Sleep(300);
                    continue;
                }
                var color=Regex.IsMatch(line,@"error|fail|exception|err\b|critical", RegexOptions.IgnoreCase)?"red":Regex.IsMatch(line,"warn|warning", RegexOptions.IgnoreCase)?"yellow":Regex.IsMatch(line,@"success|ok\b|complete|done", RegexOptions.IgnoreCase)?"green":null;
                if (color!=null)AnsiConsole.MarkupLine($"[{color}]{line.EscapeMarkup()}[/]");

                else AnsiConsole.WriteLine(line);
            }
        }
        finally
        {
            Console.CancelKeyPress-=handler;
        }

    }

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

public static class AgyKeyringHelper
{
    [DllImport("advapi32.dll", EntryPoint="CredReadW", CharSet=CharSet.Unicode, SetLastError=true)]private static extern bool CredRead(string target, uint type, uint reserved, out IntPtr credentialPtr);

    [DllImport("advapi32.dll", EntryPoint="CredWriteW", CharSet=CharSet.Unicode, SetLastError=true)]private static extern bool CredWrite(ref CREDENTIAL credential, uint flags);

    [DllImport("advapi32.dll", EntryPoint="CredDeleteW", CharSet=CharSet.Unicode, SetLastError=true)]private static extern bool CredDelete(string target, uint type, uint flags);

    [DllImport("advapi32.dll", EntryPoint="CredFree", SetLastError=true)]private static extern void CredFree(IntPtr buffer);

    [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]private struct CREDENTIAL
    {
        public uint Flags;
        public uint Type;
        public string?TargetName;
        public string?Comment;
        public System.Runtime.InteropServices.ComTypes.FILETIME LastWritten;
        public uint CredentialBlobSize;
        public IntPtr CredentialBlob;
        public uint Persist;
        public uint AttributeCount;
        public IntPtr Attributes;
        public string?TargetAlias;
        public string?UserName;

    }

    public static string?ReadToken(string target)
    {
        if (!CredRead(target, 1, 0, out var credPtr))return null;

        try
        {
            var cred=Marshal.PtrToStructure<CREDENTIAL>(credPtr);
            if (cred.CredentialBlobSize>0&&cred.CredentialBlob!=IntPtr.Zero)
            {
                var blob=new byte[cred.CredentialBlobSize];
                Marshal.Copy(cred.CredentialBlob, blob, 0, (int)cred.CredentialBlobSize);
                return Encoding.UTF8.GetString(blob);
            }
        }
        finally
        {
            CredFree(credPtr);
        }
        return null;

    }

    public static bool WriteToken(string target, string username, string token)
    {
        var cred=new CREDENTIAL
        {
            Type=1, TargetName=target, UserName=username, Persist=2
        }
        ;
        var blob=Encoding.UTF8.GetBytes(token);
        cred.CredentialBlobSize=(uint)blob.Length;
        var blobPtr=Marshal.AllocHGlobal(blob.Length);

        try
        {
            Marshal.Copy(blob, 0, blobPtr, blob.Length);
            cred.CredentialBlob=blobPtr;
            return CredWrite(ref cred, 0);
        }
        finally
        {
            Marshal.FreeHGlobal(blobPtr);
        }

    }

    public static bool DeleteToken(string target) => CredDelete(target, 1, 0);

}
public static class AgySecretVault
{
    public static string GetSecretsFilePath()
    {
        var dir=@"C:\Users\Public\.gemini";
        Directory.CreateDirectory(dir);
        return System.IO.Path.Combine(dir,"secrets.json");

    }

    public static Dictionary<string, string>LoadSecrets()
    {
        var file=GetSecretsFilePath();
        if (!File.Exists(file))return new();

        try
        {
            var raw=File.ReadAllText(file);
            if (string.IsNullOrWhiteSpace(raw))return new();
            return JsonSerializer.Deserialize<Dictionary<string, string>>(raw)??new();
        }
        catch
        {
            return new();
        }

    }

    public static void SaveSecrets(Dictionary<string, string>secrets)
    {
        var file=GetSecretsFilePath();

        try
        {
            File.WriteAllText(file, JsonSerializer.Serialize(secrets));
        }
        catch (Exception ex)
        {
            SpectrePanel.Error($"Failed to save secrets: {ex.Message}");
        }

    }

    public static void SetSecret(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(key)||string.IsNullOrWhiteSpace(value))
        {
            SpectrePanel.Error("Key and Value cannot be empty.");
            return;
        }
        var secrets=LoadSecrets();

        try
        {
            var bytes=Encoding.Unicode.GetBytes(value);
            var protectedBytes=ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
            secrets[key]=Convert.ToHexString(protectedBytes).ToLowerInvariant();
            SaveSecrets(secrets);
            AnsiConsole.MarkupLine($"[green]Secret '{key.EscapeMarkup()}' saved and encrypted successfully.[/]");
        }
        catch (Exception ex)
        {
            SpectrePanel.Error($"Failed to encrypt/save secret: {ex.Message}");
        }

    }

    public static string GetSecret(string key)
    {
        if (string.IsNullOrWhiteSpace(key))return"";
        var secrets=LoadSecrets();
        if (!secrets.TryGetValue(key, out var encrypted))
        {
            SpectrePanel.Warning($"Secret '{key}' not found.");
            return"";
        }
        try
        {
            var bytes=Convert.FromHexString(encrypted);
            var plain=ProtectedData.Unprotect(bytes, null, DataProtectionScope.CurrentUser);
            return Encoding.Unicode.GetString(plain);
        }
        catch (Exception ex)
        {
            SpectrePanel.Error($"Failed to decrypt secret '{key}': {ex.Message}");
            return"";
        }

    }

    public static void RemoveSecret(string key)
    {
        if (string.IsNullOrWhiteSpace(key))return;
        var secrets=LoadSecrets();
        if (secrets.Remove(key))
        {
            SaveSecrets(secrets);
            AnsiConsole.MarkupLine($"[green]Secret '{key.EscapeMarkup()}' removed successfully.[/]");
        }
        else
        {
            SpectrePanel.Warning($"Secret '{key}' not found.");
        }

    }

    public static void ListSecrets()
    {
        var secrets=LoadSecrets();
        if (secrets.Count==0)
        {
            SpectrePanel.Warning("No secrets stored.");
            return;
        }
        AnsiConsole.MarkupLine("[cyan]Stored Secret Keys:[/]");
        foreach (var key in secrets.Keys)AnsiConsole.MarkupLine($" * {key.EscapeMarkup()}");

    }

}
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
        try
        {
            if (File.Exists(AgyActiveAccountFile))
            {
                var content = File.ReadAllText(AgyActiveAccountFile).Trim();
                if (!string.IsNullOrEmpty(content)) return content;
            }
        }
        catch {}
        var home=Environment.GetEnvironmentVariable("GEMINI_HOME")??"";
        var m=Regex.Match(home,@"\.gemini_(.+)$");
        return m.Success?m.Groups[1].Value:"default";

    }

    public static string? GetAccountEmail(string accountName)
    {
        var dir = GetAccountDirectory(accountName);
        var googleAccountsFile = Path.Combine(dir, "google_accounts.json");
        if (File.Exists(googleAccountsFile))
        {
            try
            {
                var content = File.ReadAllText(googleAccountsFile);
                using var doc = JsonDocument.Parse(content);
                if (doc.RootElement.ValueKind == JsonValueKind.Object)
                {
                    if (doc.RootElement.TryGetProperty("email", out var emailProp)) return emailProp.GetString();
                    if (doc.RootElement.TryGetProperty("account", out var accProp)) return accProp.GetString();
                }
                else if (doc.RootElement.ValueKind == JsonValueKind.Array && doc.RootElement.GetArrayLength() > 0)
                {
                    var first = doc.RootElement[0];
                    if (first.TryGetProperty("email", out var emailProp)) return emailProp.GetString();
                    if (first.TryGetProperty("account", out var accProp)) return accProp.GetString();
                }
            }
            catch {}
        }
        
        var tokenFile = Path.Combine(dir, "keyring_token.txt");
        if (File.Exists(tokenFile))
        {
            try
            {
                var encrypted = File.ReadAllText(tokenFile).Trim();
                var decrypted = DecryptToken(encrypted);
                if (!string.IsNullOrEmpty(decrypted))
                {
                    using var doc = JsonDocument.Parse(decrypted);
                    if (doc.RootElement.TryGetProperty("email", out var emailProp)) return emailProp.GetString();
                }
            }
            catch {}
        }
        return null;
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

    public static string GlobalBinDir => @"C:\ProgramData\agy\bin";
    public static string AgyBinaryPath => Path.Combine(GlobalBinDir, "agy.exe");

    public static void BackupActiveToken(string accountName)
    {
        try
        {
            var token = AgyKeyringHelper.ReadToken("gemini:antigravity");
            if (!string.IsNullOrEmpty(token))
            {
                var accDir = GetAccountDirectory(accountName);
                Directory.CreateDirectory(accDir);
                var tokenFile = Path.Combine(accDir, "keyring_token.txt");
                var encrypted = EncryptToken(token);
                File.WriteAllText(tokenFile, encrypted, Encoding.UTF8);
            }
        }
        catch {}
    }

    public static void RestoreActiveToken(string accountName)
    {
        try
        {
            var accDir = GetAccountDirectory(accountName);
            var tokenFile = Path.Combine(accDir, "keyring_token.txt");
            if (File.Exists(tokenFile))
            {
                var encrypted = File.ReadAllText(tokenFile).Trim();
                if (!string.IsNullOrEmpty(encrypted))
                {
                    var token = DecryptToken(encrypted);
                    if (!string.IsNullOrEmpty(token))
                    {
                        AgyKeyringHelper.WriteToken("gemini:antigravity", "antigravity", token);
                    }
                }
            }
            else
            {
                AgyKeyringHelper.DeleteToken("gemini:antigravity");
            }
        }
        catch {}
    }

    private static string EncryptToken(string token)
    {
        var data = Encoding.UTF8.GetBytes(token);
        var encrypted = ProtectedData.Protect(data, null, DataProtectionScope.CurrentUser);
        return Convert.ToBase64String(encrypted);
    }

    private static string? DecryptToken(string encrypted)
    {
        try
        {
            var data = Convert.FromBase64String(encrypted);
            var decrypted = ProtectedData.Unprotect(data, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(decrypted);
        }
        catch
        {
            return null;
        }
    }

    public static void SetActiveAccount(string accountName, bool temporary)
    {
        if (!string.Equals(accountName, "default", StringComparison.OrdinalIgnoreCase))
        {
            var targetDir = GetAccountDirectory(accountName);
            if (!Directory.Exists(targetDir))
            {
                throw new ArgumentException($"Account '{accountName}' does not exist.");
            }
        }

        ClearStatsCache();
        UpdateAccountMetadata(accountName);
        BackupActiveToken(GetActiveAccount());

        if (string.Equals(accountName, "default", StringComparison.OrdinalIgnoreCase))
        {
            Environment.SetEnvironmentVariable("GEMINI_HOME", AgySourceHome);
            if (!temporary)
            {
                try
                {
                    Directory.CreateDirectory(AgySourceHome);
                    File.WriteAllText(AgyActiveAccountFile, "default", Encoding.UTF8);
                }
                catch {}
            }
            RestoreActiveToken("default");
            SpectrePanel.Success("Switched to default Antigravity account (Primary).");
            return;
        }

        var targetDirLoc = GetAccountDirectory(accountName);
        var defaultIdFile = Path.Combine(AgySourceHome, "installation_id");
        var targetIdFile = Path.Combine(targetDirLoc, "installation_id");
        string defaultId = "";
        if (File.Exists(defaultIdFile)) defaultId = File.ReadAllText(defaultIdFile).Trim();
        string targetId = "";
        if (File.Exists(targetIdFile)) targetId = File.ReadAllText(targetIdFile).Trim();

        if (string.IsNullOrWhiteSpace(targetId) || targetId == defaultId)
        {
            try
            {
                Directory.CreateDirectory(targetDirLoc);
                var newId = Guid.NewGuid().ToString();
                File.WriteAllText(targetIdFile, newId);
                SpectrePanel.Warning($"Re-generated unique installation ID for '{accountName}' to separate credentials.");
            }
            catch {}
        }

        Environment.SetEnvironmentVariable("GEMINI_HOME", targetDirLoc);
        RestoreActiveToken(accountName);

        if (!temporary)
        {
            try
            {
                Directory.CreateDirectory(AgySourceHome);
                File.WriteAllText(AgyActiveAccountFile, accountName, Encoding.UTF8);
                SpectrePanel.Success($"Switched to account '{accountName}' (Persistent).");
            }
            catch
            {
                SpectrePanel.Error("Failed to update active account file.");
            }
        }
        else
        {
            SpectrePanel.Warning($"Switched to account '{accountName}' (Temporary - current session only).");
        }

        try
        {
            foreach (var name in new[] { "agy", "antigravity-hub", "openclaw" })
            {
                foreach (var p in Process.GetProcessesByName(name))
                {
                    try { p.Kill(true); } catch {}
                }
            }
        }
        catch {}
    }

    public static void AddAccount(string accountName)
    {
        if (string.IsNullOrWhiteSpace(accountName))
            throw new ArgumentException("Account name cannot be empty.");
            
        if (string.Equals(accountName, "default", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Cannot create an account named 'default'.");

        var destDir = GetAccountDirectory(accountName);
        if (Directory.Exists(destDir))
            throw new InvalidOperationException($"Account '{accountName}' already exists.");

        Directory.CreateDirectory(destDir);

        // Copy root files
        var credentialsFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "google_accounts.json", "oauth_creds.json", "state.json", "installation_id", "keyring_token.txt"
        };

        if (Directory.Exists(AgySourceHome))
        {
            foreach (var file in Directory.GetFiles(AgySourceHome))
            {
                var fileName = Path.GetFileName(file);
                if (!credentialsFiles.Contains(fileName))
                {
                    try
                    {
                        File.Copy(file, Path.Combine(destDir, fileName), true);
                    }
                    catch {}
                }
            }
        }

        // Generate installation ID
        var installationIdFile = Path.Combine(destDir, "installation_id");
        File.WriteAllText(installationIdFile, Guid.NewGuid().ToString());

        // Create independent subdirectories (no symlinks or junctions, to keep accounts isolated!)
        var subDirs = new[] { "antigravity", "antigravity-cli", "config", "history", "antigravity-ide", "wf", "learn" };
        foreach (var sub in subDirs)
        {
            Directory.CreateDirectory(Path.Combine(destDir, sub));
        }
        ClearStatsCache();
    }

    public static void DeleteAccount(string accountName)
    {
        if (string.IsNullOrWhiteSpace(accountName))
            throw new ArgumentException("Account name cannot be empty.");

        var targetDir = GetAccountDirectory(accountName);
        if (!Directory.Exists(targetDir))
            throw new DirectoryNotFoundException($"Account '{accountName}' does not exist.");

        // Recursively delete the directory
        Directory.Delete(targetDir, true);

        // If the active account was deleted, switch back to default
        if (string.Equals(GetActiveAccount(), accountName, StringComparison.OrdinalIgnoreCase))
        {
            SetActiveAccount("default", false);
        }
        ClearStatsCache();
    }

    public static void LogoutAccount(string accountName)
    {
        var dir = GetAccountDirectory(accountName);
        if (!Directory.Exists(dir)) return;

        var files = new[] { "google_accounts.json", "oauth_creds.json", "state.json", "keyring_token.txt" };
        foreach (var f in files)
        {
            var p = Path.Combine(dir, f);
            if (File.Exists(p))
            {
                try { File.Delete(p); } catch {}
            }
        }
    }

    public static void SyncActiveAccountWithKeyring(bool silent)
    {
        if (!IsAutoSwitchEnabled()) return;
        try
        {
            string savedAcc = GetActiveAccount();
            string? keyringToken = AgyKeyringHelper.ReadToken("gemini:antigravity");
            if (string.IsNullOrEmpty(keyringToken)) return;

            string? matchedAcc = null;
            var availableAccounts = GetAccounts();
            foreach (var acc in availableAccounts)
            {
                var accDir = GetAccountDirectory(acc);
                var tokenFile = Path.Combine(accDir, "keyring_token.txt");
                if (File.Exists(tokenFile))
                {
                    try
                    {
                        var encrypted = File.ReadAllText(tokenFile).Trim();
                        if (!string.IsNullOrEmpty(encrypted))
                        {
                            var savedToken = DecryptToken(encrypted);
                            if (savedToken == keyringToken)
                            {
                                matchedAcc = acc;
                                break;
                            }
                            else
                            {
                                try
                                {
                                    using var savedJson = JsonDocument.Parse(savedToken);
                                    using var currentJson = JsonDocument.Parse(keyringToken);
                                    if (savedJson.RootElement.TryGetProperty("token", out var sToken) &&
                                        currentJson.RootElement.TryGetProperty("token", out var cToken) &&
                                        sToken.TryGetProperty("refresh_token", out var sRefresh) &&
                                        cToken.TryGetProperty("refresh_token", out var cRefresh) &&
                                        sRefresh.GetString() == cRefresh.GetString())
                                    {
                                        matchedAcc = acc;
                                        break;
                                    }
                                }
                                catch {}
                            }
                        }
                    }
                    catch {}
                }
            }

            if (matchedAcc == null)
            {
                try
                {
                    if (CheckNetworkStatus())
                    {
                        using var json = JsonDocument.Parse(keyringToken);
                        if (json.RootElement.TryGetProperty("token", out var tokenObj) && tokenObj.TryGetProperty("access_token", out var accessTok))
                        {
                            var accessToken = accessTok.GetString();
                            if (!string.IsNullOrEmpty(accessToken))
                            {
                                using var client = new HttpClient();
                                client.Timeout = TimeSpan.FromSeconds(3);
                                var response = client.GetStringAsync($"https://oauth2.googleapis.com/tokeninfo?access_token={accessToken}").Result;
                                using var info = JsonDocument.Parse(response);
                                if (info.RootElement.TryGetProperty("email", out var emailProp))
                                {
                                    var email = emailProp.GetString()?.Trim().ToLower();
                                    if (email != null)
                                    {
                                        foreach (var acc in availableAccounts)
                                        {
                                            if (string.Equals(acc.Trim(), email, StringComparison.OrdinalIgnoreCase))
                                            {
                                                matchedAcc = acc;
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch {}
            }

            if (matchedAcc != null)
            {
                if (!string.Equals(matchedAcc, savedAcc, StringComparison.OrdinalIgnoreCase))
                {
                    if (!silent)
                    {
                        SpectrePanel.Warning($"Keyring matches account '{matchedAcc}'. Auto-switching active account.");
                    }
                    savedAcc = matchedAcc;
                    Directory.CreateDirectory(AgySourceHome);
                    File.WriteAllText(AgyActiveAccountFile, savedAcc, Encoding.UTF8);
                    Environment.SetEnvironmentVariable("GEMINI_HOME", GetAccountDirectory(savedAcc));
                }
                var accDir = GetAccountDirectory(savedAcc);
                Directory.CreateDirectory(accDir);
                var tokenFile = Path.Combine(accDir, "keyring_token.txt");
                var encryptedToken = EncryptToken(keyringToken);
                File.WriteAllText(tokenFile, encryptedToken, Encoding.UTF8);
            }
            else
            {
                var accDir = GetAccountDirectory(savedAcc);
                Directory.CreateDirectory(accDir);
                var tokenFile = Path.Combine(accDir, "keyring_token.txt");
                var encryptedToken = EncryptToken(keyringToken);
                File.WriteAllText(tokenFile, encryptedToken, Encoding.UTF8);
            }
        }
        catch {}
    }

    public static void AutoSwitchOnQuotaExceeded()
    {
        if (!IsAutoSwitchEnabled()) return;
        var active = GetActiveAccount();
        var activeMeta = GetAccountMetadata(active);
        if (string.Equals(activeMeta.QuotaStatus, "Exceeded", StringComparison.OrdinalIgnoreCase))
        {
            string? candidate = null;
            foreach (var acc in GetAccounts())
            {
                if (string.Equals(acc, active, StringComparison.OrdinalIgnoreCase)) continue;
                var accDir = GetAccountDirectory(acc);
                if (!File.Exists(Path.Combine(accDir, "keyring_token.txt"))) continue;
                var meta = GetAccountMetadata(acc);
                if (string.Equals(meta.QuotaStatus, "OK", StringComparison.OrdinalIgnoreCase))
                {
                    candidate = acc;
                    break;
                }
            }

            if (candidate != null)
            {
                SpectrePanel.Warning($"Active account '{active}' exceeded quota. Auto-switching to candidate account '{candidate}' with available quota.");
                SetActiveAccount(candidate, false);
            }
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

    private static readonly Dictionary<string, (long Size, DateTime CachedAt)> _sizeCache = new();

    public static long GetPrivateDirectorySize(string path)
    {
        if (!Directory.Exists(path))return 0;
        lock (_sizeCache)
        {
            if (_sizeCache.TryGetValue(path, out var cached) && (DateTime.UtcNow - cached.CachedAt).TotalSeconds < 15)
            {
                return cached.Size;
            }
        }
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
        lock (_sizeCache)
        {
            _sizeCache[path] = (total, DateTime.UtcNow);
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

    private static readonly Dictionary<string, (AccountStats Stats, DateTime CachedAt)> _statsCache = new();

    public static void ClearStatsCache()
    {
        lock (_statsCache)
        {
            _statsCache.Clear();
        }
    }

    public static AccountStats GetAccountStats(string accountName)
    {
        lock (_statsCache)
        {
            if (_statsCache.TryGetValue(accountName, out var cached) && (DateTime.UtcNow - cached.CachedAt).TotalSeconds < 3)
            {
                return cached.Stats;
            }
        }
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
        var stats = new AccountStats(meta.LastUsed, meta.UsageCount, sizeStr, junctionStatus, skillsCount, convCount, tokenStatus, meta.QuotaStatus, quota.RemainingWeekly, quota.Remaining5H);

        lock (_statsCache)
        {
            _statsCache[accountName] = (stats, DateTime.UtcNow);
        }
        return stats;

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
public static class AgyAccountMenu
{
    public enum MainMenuChoice
    {
        Exit, ManageAccount, AddAccount, ToggleAutoSwitch, ShowStats

    }

    public sealed record MainMenuResult(MainMenuChoice Choice, string?AccountName);

    public static MainMenuResult ShowManageMenu()
    {
        var accounts=AgyAccountCore.GetAccounts();
        var active=AgyAccountCore.GetActiveAccount();
        var menuItems=new List<string>();
        var defaultIdx=0;
        for (var i=0;
        i<accounts.Length;
        i++)
        {
            var status=File.Exists(System.IO.Path.Combine(AgyAccountCore.GetAccountDirectory(accounts[i]),"keyring_token.txt"))?"Logged In":"Not Logged In";
            if (accounts[i]==active)
            {
                menuItems.Add($"* {accounts[i]} (Active, {status})");
                defaultIdx=i;
            }
            else menuItems.Add($" {accounts[i]} ({status})");
        }
        menuItems.Add("[+] Add New Account");
        menuItems.Add($"[Settings] Toggle Auto-Switch (Currently: {(AgyAccountCore.IsAutoSwitchEnabled() ? "Enabled" : "Disabled")})");
        menuItems.Add("[Stats] Show All Accounts Summary");
        menuItems.Add("[x] Exit Dashboard");
        var selected=SpectreMenu.ShowRobust(["Antigravity Multi-Account Manager"], menuItems.ToArray(), defaultIdx, false, true);
        if (selected<0||selected==menuItems.Count-1)return new(MainMenuChoice.Exit, null);
        if (selected<accounts.Length)return new(MainMenuChoice.ManageAccount, accounts[selected]);
        if (selected==accounts.Length)return new(MainMenuChoice.AddAccount, null);
        if (selected==accounts.Length+1)return new(MainMenuChoice.ToggleAutoSwitch, null);
        return new(MainMenuChoice.ShowStats, null);

    }
    public enum AccountAction
    {
        Back, SetActivePersistent, SetActiveTemporary, ShowUsage, Login, Logout, Delete

    }

    public static void ShowAccountStatsCard(string accountName)
    {
        var stats=AgyAccountCore.GetAccountStats(accountName);
        AnsiConsole.MarkupLine("[cyan]=============================================[/]");
        AnsiConsole.MarkupLine($"[cyan] ACCOUNT STATS: {accountName.EscapeMarkup()}[/]");
        AnsiConsole.MarkupLine("[cyan]=============================================[/]");
        AnsiConsole.MarkupLine($" * Status: {stats.TokenStatus.EscapeMarkup()}");
        AnsiConsole.MarkupLine($" * Quota Status: {stats.QuotaStatus.EscapeMarkup()}");
        AnsiConsole.MarkupLine($" * Last Used: {stats.LastUsed.EscapeMarkup()}");
        AnsiConsole.MarkupLine($" * Usage Count: {stats.UsageCount} sessions/calls");
        AnsiConsole.MarkupLine($" * Private Size: {stats.PrivateSize.EscapeMarkup()} (excluding shared)");
        AnsiConsole.MarkupLine($" * Sync Health: {stats.JunctionStatus.EscapeMarkup()}");
        AnsiConsole.MarkupLine($" * Shared Skills: {stats.SkillsCount} skills");
        AnsiConsole.MarkupLine($" * Shared History: {stats.ConversationsCount} conversations");
        AnsiConsole.MarkupLine("[cyan]=============================================[/]");
        AnsiConsole.WriteLine();

    }

    public static AccountAction ShowAccountSubMenu(string accountName)
    {
        AnsiConsole.Clear();
        ShowAccountStatsCard(accountName);
        var status=File.Exists(System.IO.Path.Combine(AgyAccountCore.GetAccountDirectory(accountName),"keyring_token.txt"))?"Logged In":"Not Logged In";
        var subItems=new List<string>
        {
            "[Switch] Set as Active (Persistent)","[Switch] Set as Active (Temporary)","[Usage] Models & Quota","[Login] Sign In / Re-authenticate","[Logout] Sign Out / Reset Credentials"
        }
        ;
        if (!string.Equals(accountName,"default", StringComparison.OrdinalIgnoreCase))subItems.Add("[Delete] Remove Account");
        subItems.Add("[Back] Return to Main Menu");
        var subSel=SpectreMenu.ShowRobust([$"Manage Account: {accountName} ({status})"], subItems.ToArray(), 0, false, true);
        if (subSel<0)return AccountAction.Back;
        return subItems[subSel]switch
        {
            "[Switch] Set as Active (Persistent)" => AccountAction.SetActivePersistent,"[Switch] Set as Active (Temporary)" => AccountAction.SetActiveTemporary,"[Usage] Models & Quota" => AccountAction.ShowUsage,"[Login] Sign In / Re-authenticate" => AccountAction.Login,"[Logout] Sign Out / Reset Credentials" => AccountAction.Logout,"[Delete] Remove Account" => AccountAction.Delete, _ => AccountAction.Back
        }
        ;

    }
    public enum SelectChoice
    {
        Cancel, Selected, AddAccount, DeleteAccount

    }

    public sealed record SelectResult(SelectChoice Choice, string?AccountName);

    public static SelectResult ShowSelectAccountMenu()
    {
        var accounts=AgyAccountCore.GetAccounts();
        var active=AgyAccountCore.GetActiveAccount();
        var menuItems=new List<string>();
        var defaultIdx=0;
        for (var i=0;
        i<accounts.Length;
        i++)
        {
            if (accounts[i]==active)
            {
                menuItems.Add($"{accounts[i]} (Active)");
                defaultIdx=i;
            }
            else menuItems.Add(accounts[i]);
        }
        menuItems.Add("[+] Add New Account");
        menuItems.Add("[x] Delete Account");
        menuItems.Add("[exit] Cancel / Exit");
        var selected=SpectreMenu.ShowRobust(["Select Antigravity Account"], menuItems.ToArray(), defaultIdx, false, true);
        if (selected<0)return new(SelectChoice.Cancel, null);
        if (selected<accounts.Length)return new(SelectChoice.Selected, accounts[selected]);
        if (selected==accounts.Length)return new(SelectChoice.AddAccount, null);
        return new(SelectChoice.DeleteAccount, null);

    }

    public static string?ShowDeleteAccountMenu()
    {
        var deletable=AgyAccountCore.GetAccounts().Where(a => !string.Equals(a,"default", StringComparison.OrdinalIgnoreCase)).ToArray();
        if (deletable.Length==0)
        {
            SpectrePanel.Warning("No secondary accounts available to delete.");
            return null;
        }
        var idx=SpectreMenu.ShowRobust(["Delete Antigravity Account"], deletable, 0, false, true);
        return idx>=0?deletable[idx]:null;

    }

}
public static class Projects
{
    public static readonly string AgBaseDir=Directory.Exists(@"C:\Users\sshuser\project")?@"C:\Users\sshuser\project":System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),"Desktop","project");

    public static string?StartManager()
    {
        var projectDir=System.IO.Path.Combine(AgBaseDir,"AntigravityManager");
        if (!Directory.Exists(projectDir))
        {
            SpectrePanel.Error($"Project not found: {projectDir}");
            return null;
        }
        RunNpmSetupAndStart(projectDir,"Antigravity Manager", null);
        return projectDir;

    }

    public static string?StartProxy()
    {
        var projectDir=System.IO.Path.Combine(AgBaseDir,"antigravity-claude-proxy");
        if (!Directory.Exists(projectDir))
        {
            SpectrePanel.Error($"Project not found: {projectDir}");
            return null;
        }
        AnsiConsole.MarkupLine("[cyan]🛸 Proxy env set (BASE_URL=localhost:8080)[/]");
        var env=new Dictionary<string, string?>
        {
            ["ANTHROPIC_BASE_URL"]="http://localhost:8080", ["ANTHROPIC_AUTH_TOKEN"]="test"
        }
        ;
        RunNpmSetupAndStart(projectDir,"Antigravity Proxy", env);
        return projectDir;

    }

    private static void RunNpmSetupAndStart(string projectDir, string label, IDictionary<string, string?>?env)
    {
        AnsiConsole.MarkupLine("[cyan][[1/2]] 📦 Checking dependencies...[/]");
        if (!Directory.Exists(System.IO.Path.Combine(projectDir,"node_modules")))
        {
            AnsiConsole.MarkupLine("[yellow] -> Installing (npm install)...[/]");
            RunNpm("install", projectDir, env);
        }
        else
        {
            AnsiConsole.MarkupLine("[green] -> node_modules OK.[/]");
        }
        AnsiConsole.MarkupLine($"[green][[2/2]] 🚀 Launching {label.EscapeMarkup()}...[/]");
        RunNpm("start", projectDir, env);

    }

    private static void RunNpm(string args, string workingDir, IDictionary<string, string?>?env)
    {
        var npmPath=AgyAiCore.FindOnPath("npm.cmd")??"npm.cmd";
        var psi=new ProcessStartInfo(npmPath, args)
        {
            UseShellExecute=false, WorkingDirectory=workingDir
        }
        ;
        if (env!=null)
        {
            foreach (var kv in env)
            {
                if (kv.Value==null)psi.Environment.Remove(kv.Key);

                else psi.Environment[kv.Key]=kv.Value;
            }
        }
        try
        {
            using var p=Process.Start(psi);
            p?.WaitForExit();
        }
        catch (Exception ex)
        {
            SpectrePanel.Error($"Failed to run 'npm {args}': {ex.Message}");
        }

    }

}

public sealed record WorkspaceEntry(string Name, [property:JsonPropertyName("Path")]string WorkspacePath, string?AssociatedAccount, string[]?Tags);

public static class WorkspaceRegistry
{
    private static readonly string ConfigFile=System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".gemini", "antigravity", "priority_workspaces.json");

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
public static class OllamaHelper
{
    public static void ShowOllamaLogs()
    {
        var logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Ollama", "server.log");
        if (!File.Exists(logPath))
        {
            SpectrePanel.Error($"Ollama log file not found at: {logPath}");
            Console.WriteLine("Press any key to return...");
            Console.ReadKey(true);
            return;
        }
        
        AnsiConsole.MarkupLine($"[bold cyan]Showing last 50 lines of Ollama Server Logs...[/]");
        AnsiConsole.MarkupLine($"[dim]Log Path: {logPath}[/]\n");
        
        try
        {
            using var fs = new FileStream(logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var sr = new StreamReader(fs);
            var lines = new List<string>();
            string? line;
            while ((line = sr.ReadLine()) != null)
            {
                lines.Add(line);
            }
            
            var lastLines = lines.Skip(Math.Max(0, lines.Count - 50));
            foreach (var l in lastLines)
            {
                Console.WriteLine(l);
            }
        }
        catch (Exception ex)
        {
            SpectrePanel.Error($"Failed to read logs: {ex.Message}");
        }
        
        Console.WriteLine("\nPress any key to return...");
        Console.ReadKey(true);
    }

    public static void ManageOllamaModels()
    {
        if (!AgyAiCore.IsOllamaRunning())
        {
            SpectrePanel.Error("Ollama daemon is offline.");
            Thread.Sleep(1500);
            return;
        }
        
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
            var response = client.GetStringAsync("http://127.0.0.1:11434/api/tags").Result;
            using var doc = JsonDocument.Parse(response);
            if (!doc.RootElement.TryGetProperty("models", out var modelsProp) || modelsProp.ValueKind != JsonValueKind.Array)
            {
                SpectrePanel.Warning("No local models found.");
                Thread.Sleep(1500);
                return;
            }
            
            var models = new List<string>();
            foreach (var m in modelsProp.EnumerateArray())
            {
                if (m.TryGetProperty("name", out var nameProp))
                {
                    models.Add(nameProp.GetString() ?? "");
                }
            }
            
            if (models.Count == 0)
            {
                SpectrePanel.Warning("No local models found.");
                Thread.Sleep(1500);
                return;
            }
            
            var selection = SpectreMenu.ShowWithEscape("Manage Ollama Models", models.ToArray(), 0);
            if (selection >= 0)
            {
                var modelName = models[selection];
                var action = SpectreMenu.ShowWithEscape($"Model: {modelName}", ["Delete Model", "Show Info"], 0);
                if (action == 0)
                {
                    if (AnsiConsole.Confirm($"Are you sure you want to delete model '{modelName}'?"))
                    {
                        AnsiConsole.MarkupLine($"[yellow]Deleting {modelName}...[/]");
                        var request = new HttpRequestMessage(HttpMethod.Delete, "http://127.0.0.1:11434/api/delete");
                        request.Content = new StringContent($"{{\"name\":\"{modelName}\"}}", Encoding.UTF8, "application/json");
                        var delResp = client.SendAsync(request).Result;
                        if (delResp.IsSuccessStatusCode)
                        {
                            SpectrePanel.Success($"Model '{modelName}' deleted successfully.");
                        }
                        else
                        {
                            SpectrePanel.Error($"Failed to delete model: {delResp.StatusCode}");
                        }
                        Thread.Sleep(1500);
                    }
                }
                else if (action == 1)
                {
                    AnsiConsole.MarkupLine($"[cyan]Querying model info for {modelName}...[/]");
                    var requestBody = $"{{\"name\":\"{modelName}\"}}";
                    var infoResp = client.PostAsync("http://127.0.0.1:11434/api/show", new StringContent(requestBody, Encoding.UTF8, "application/json")).Result;
                    if (infoResp.IsSuccessStatusCode)
                    {
                        var infoJson = infoResp.Content.ReadAsStringAsync().Result;
                        AnsiConsole.Clear();
                        AnsiConsole.MarkupLine($"[bold white]Model Details: {modelName}[/]\n");
                        Console.WriteLine(infoJson);
                    }
                    else
                    {
                        SpectrePanel.Error("Failed to fetch model info.");
                    }
                    Console.WriteLine("\nPress any key to return...");
                    Console.ReadKey(true);
                }
            }
        }
        catch (Exception ex)
        {
            SpectrePanel.Error($"Error managing models: {ex.Message}");
            Thread.Sleep(1500);
        }
    }

    public static void PullOllamaModel()
    {
        if (!AgyAiCore.IsOllamaRunning())
        {
            SpectrePanel.Error("Ollama daemon is offline.");
            Thread.Sleep(1500);
            return;
        }
        
        var modelName = AnsiConsole.Ask<string>("Enter Ollama model name to pull (e.g. qwen2.5:coder, llama3):").Trim();
        if (string.IsNullOrEmpty(modelName)) return;
        
        AnsiConsole.MarkupLine($"[yellow]Starting pull command: ollama pull {modelName}[/]");
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "ollama",
                Arguments = $"pull {modelName}",
                UseShellExecute = false,
                CreateNoWindow = false
            };
            using var proc = Process.Start(psi);
            if (proc != null)
            {
                proc.WaitForExit();
                if (proc.ExitCode == 0)
                {
                    SpectrePanel.Success($"Model '{modelName}' pulled successfully.");
                }
                else
                {
                    SpectrePanel.Error($"Ollama pull exited with code {proc.ExitCode}");
                }
            }
        }
        catch (Exception ex)
        {
            SpectrePanel.Error($"Failed to run pull command: {ex.Message}");
        }
        Console.WriteLine("\nPress any key to return...");
        Console.ReadKey(true);
    }

    public static void StartOllamaDaemon()
    {
        if (AgyAiCore.IsOllamaRunning())
        {
            SpectrePanel.Success("Ollama daemon is already running!");
            Thread.Sleep(1500);
            return;
        }
        
        AnsiConsole.MarkupLine("[yellow]Starting Ollama daemon in background...[/]");
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "ollama",
                Arguments = "serve",
                UseShellExecute = true,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };
            Process.Start(psi);
            
            for (var i = 0; i < 10; i++)
            {
                Thread.Sleep(500);
                if (AgyAiCore.IsOllamaRunning())
                {
                    SpectrePanel.Success("Ollama daemon started successfully!");
                    Thread.Sleep(1500);
                    return;
                }
            }
            SpectrePanel.Warning("Ollama process started, but status check timed out. Verify manually.");
        }
        catch (Exception ex)
        {
            SpectrePanel.Error($"Failed to start Ollama: {ex.Message}");
        }
        Thread.Sleep(2000);
    }
}

public static class AntigravityDeckHelper
{
    private static readonly string DeckPath = @"C:\Users\TruongNhon\AppData\Local\AntigravityDeck";

    public static void Setup()
    {
        if (!Directory.Exists(DeckPath))
        {
            SpectrePanel.Error($"Antigravity Deck path not found at {DeckPath}. Please install it first.");
            Thread.Sleep(2000);
            return;
        }
        AnsiConsole.MarkupLine("[yellow]Running: npm run setup...[/]");
        RunNpmCommand("run", "setup");
    }

    public static void StartLocal()
    {
        if (!Directory.Exists(DeckPath))
        {
            SpectrePanel.Error($"Antigravity Deck path not found at {DeckPath}. Please install it first.");
            Thread.Sleep(2000);
            return;
        }
        AnsiConsole.MarkupLine("[yellow]Starting Antigravity Deck (Local dev server on port 3000)...[/]");
        AnsiConsole.MarkupLine("[dim]Press Ctrl+C to terminate the server.[/]");
        RunNpmCommand("run", "dev");
    }

    public static void StartOnline()
    {
        if (!Directory.Exists(DeckPath))
        {
            SpectrePanel.Error($"Antigravity Deck path not found at {DeckPath}. Please install it first.");
            Thread.Sleep(2000);
            return;
        }
        AnsiConsole.MarkupLine("[yellow]Starting Antigravity Deck (Cloudflare Tunnel)...[/]");
        AnsiConsole.MarkupLine("[dim]Press Ctrl+C to terminate the server.[/]");
        RunNpmCommand("run", "online");
    }

    private static void RunNpmCommand(string cmd, string arg)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "npm.cmd",
                Arguments = $"{cmd} {arg}",
                WorkingDirectory = DeckPath,
                UseShellExecute = false,
                CreateNoWindow = false
            };
            using var proc = Process.Start(psi);
            if (proc != null)
            {
                proc.WaitForExit();
            }
        }
        catch (Exception ex)
        {
            SpectrePanel.Error($"Failed to run npm command: {ex.Message}");
            Console.WriteLine("\nPress any key to return...");
            Console.ReadKey(true);
        }
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
        var killedAny=false;
        var seenPids=new HashSet<int>();
        foreach (var line in result.Split('\n'))
        {
            if (!line.Contains($":{port} ")&&!line.Contains($":{port}\t"))continue;
            var parts=line.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length<5)continue;
            if (!int.TryParse(parts[^1], out var pid))continue;
            if (!seenPids.Add(pid))continue;

            try
            {
                var proc=Process.GetProcessById(pid);
                var name=proc.ProcessName;
                proc.Kill(entireProcessTree:true);
                SpectrePanel.Success($"Killed process '{name}' (PID {pid}) listening on port {port}.");
                killedAny=true;
            }
            catch (Exception ex)
            {
                SpectrePanel.Error($"Failed to kill PID {pid}: {ex.Message}");
            }
        }
        if (!killedAny)SpectrePanel.Warning($"No process found listening on port {port}.");
        return killedAny;

    }

    public static void OpenExplorer(string?path=null) => Process.Start(new ProcessStartInfo("explorer.exe", path??Directory.GetCurrentDirectory())
    {
        UseShellExecute=true

    }
    );

    public static void StopProcessFriendly(string?name=null)
    {
        if (!string.IsNullOrWhiteSpace(name))
        {
            var named=Process.GetProcessesByName(name);
            if (named.Length==0)
            {
                SpectrePanel.Warning($"No process named '{name}' found.");
                return;
            }
            foreach (var p in named)
            {
                try
                {
                    p.Kill();
                    SpectrePanel.Success($"Stopped '{p.ProcessName}' (PID {p.Id}).");
                }
                catch (Exception ex)
                {
                    SpectrePanel.Error($"Failed to stop PID {p.Id}: {ex.Message}");
                }
            }
            return;
        }
        var all=Process.GetProcesses().OrderBy(p => p.ProcessName).ToArray();
        var labels=all.Select(p => $"{p.ProcessName,-30} PID {p.Id}").ToArray();
        var idx=SpectreMenu.Show("Select process to kill", labels, 0, true);
        if (idx<0)return;
        var target=all[idx];

        try
        {
            target.Kill();
            SpectrePanel.Success($"Stopped '{target.ProcessName}' (PID {target.Id}).");
        }
        catch (Exception ex)
        {
            SpectrePanel.Error($"Failed to stop PID {target.Id}: {ex.Message}");
        }

    }

    public static void SystemMonitor()
    {
        AnsiConsole.MarkupLine("[dim]Press Escape or Enter to exit System Monitor...[/]");
        PerformanceCounter?cpuCounter=null;
        PerformanceCounter?diskCounter=null;

        try
        {
            cpuCounter=new PerformanceCounter("Processor","% Processor Time","_Total");
            diskCounter=new PerformanceCounter("PhysicalDisk","% Disk Time","_Total");
            cpuCounter.NextValue();
            diskCounter.NextValue();
        }
        catch
        {
        }
        try
        {
            while (true)
            {
                var cpu=0.0;

                try
                {
                    cpu=cpuCounter?.NextValue()??0.0;
                }
                catch
                {
                }
                var disk=0.0;

                try
                {
                    disk=Math.Min(100.0, diskCounter?.NextValue()??0.0);
                }
                catch
                {
                }
                GetMemoryInfo(out var totalMb, out var availMb);
                var usedMb=totalMb-availMb;
                var ramPercent=totalMb>0?(usedMb/totalMb)*100.0:0.0;
                AnsiConsole.MarkupLine($" CPU Usage: {Bar(cpu)} {cpu:F1}%".PadRight(60));
                AnsiConsole.MarkupLine($" RAM Usage: {Bar(ramPercent)} {ramPercent:F1}% ({usedMb/1024.0:F2} GB / {totalMb/1024.0:F2} GB)".PadRight(60));
                AnsiConsole.MarkupLine($" Disk I/O: {Bar(disk)} {disk:F1}%".PadRight(60));
                var exit=false;
                for (var s=0;
                s<20;
                s++)
                {
                    if (Console.KeyAvailable)
                    {
                        var key=Console.ReadKey(true);
                        if (key.Key is ConsoleKey.Escape or ConsoleKey.Enter)
                        {
                            exit=true;
                            break;
                        }
                    }
                    Thread.Sleep(100);
                }
                if (exit)break;
                AnsiConsole.Cursor.MoveUp(3);
            }
        }
        finally
        {
            cpuCounter?.Dispose();
            diskCounter?.Dispose();
        }

    }

    private static string Bar(double percentage)
    {
        var filled=Math.Clamp((int)Math.Round(percentage/100.0*20), 0, 20);
        return"["+new string('█', filled)+new string('░', 20-filled)+"]";

    }
    [DllImport("kernel32.dll", SetLastError=true)]private static extern bool GlobalMemoryStatusEx(ref MemoryStatusEx buffer);

    [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Auto)]private struct MemoryStatusEx
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;

    }

    private static void GetMemoryInfo(out double totalMb, out double availMb)
    {
        var status=new MemoryStatusEx();
        status.dwLength=(uint)Marshal.SizeOf<MemoryStatusEx>();
        if (GlobalMemoryStatusEx(ref status))
        {
            totalMb=status.ullTotalPhys/1024.0/1024.0;
            availMb=status.ullAvailPhys/1024.0/1024.0;
        }
        else
        {
            totalMb=1.0;
            availMb=1.0;
        }

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

    public static void GetConnectionInfo()
    {
        AnsiConsole.MarkupLine("[bold cyan]🌐 Network Connection Status[/]");
        AnsiConsole.MarkupLine("[cyan]===========================[/]");
        string?tailscaleIp=null;
        if (IsCommandAvailable("tailscale"))
        {
            tailscaleIp=SystemHelper.RunProcess("tailscale","ip -4", capture:true).Trim();
            if (!string.IsNullOrWhiteSpace(tailscaleIp))AnsiConsole.MarkupLine($" Tailscale IPv4 Address: [green]{tailscaleIp.EscapeMarkup()}[/]");

            else AnsiConsole.MarkupLine(" [yellow][[WARN]] Tailscale is installed but may not be logged in or connected.[/]");
        }
        else
        {
            AnsiConsole.MarkupLine(" [dim]Tailscale is not installed on this machine.[/]");
        }
        var localIps=NetworkInterface.GetAllNetworkInterfaces().Where(n => n.OperationalStatus==OperationalStatus.Up&&(n.Name.Contains("Wi-Fi", StringComparison.OrdinalIgnoreCase)||n.Name.Contains("Ethernet", StringComparison.OrdinalIgnoreCase))).SelectMany(n => n.GetIPProperties().UnicastAddresses).Where(a => a.Address.AddressFamily==AddressFamily.InterNetwork).Select(a => a.Address.ToString()).ToArray();
        if (localIps.Length>0)AnsiConsole.MarkupLine($" Local IPv4 Address(es): [cyan]{string.Join(", ", localIps).EscapeMarkup()}[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold cyan]🔒 Active SSH Sessions[/]");
        AnsiConsole.MarkupLine("[cyan]====================[/]");
        var netstatOut=SystemHelper.RunProcess("netstat","-ano", capture:true);
        var sshConns=netstatOut.Split('\n').Select(l => l.Trim()).Where(l => l.StartsWith("TCP", StringComparison.OrdinalIgnoreCase)).Select(l => l.Split(' ', StringSplitOptions.RemoveEmptyEntries)).Where(parts => parts.Length>=5&&parts[1].EndsWith(":22")&&parts[3].Equals("ESTABLISHED", StringComparison.OrdinalIgnoreCase)).ToArray();
        if (sshConns.Length>0)
        {
            foreach (var parts in sshConns)
            {
                var procName="?";
                if (int.TryParse(parts[4], out var pid))
                {
                    try
                    {
                        procName=Process.GetProcessById(pid).ProcessName;
                    }
                    catch
                    {
                    }
                }
                AnsiConsole.MarkupLine($" Established connection from [green]{parts[2].EscapeMarkup()}[/] (Process: {procName.EscapeMarkup()}, PID: {parts[4]})");
            }
        }
        else
        {
            AnsiConsole.MarkupLine(" [dim]No active SSH connections on port 22.[/]");
        }
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold cyan]📱 Phone to PC Control Quick Guide[/]");
        AnsiConsole.MarkupLine("[cyan]================================[/]");
        AnsiConsole.MarkupLine(" 1. On your phone (Termux), run: ssh sshuser@<IP>");
        var displayIp=!string.IsNullOrWhiteSpace(tailscaleIp)?tailscaleIp:"100.x.y.z";
        AnsiConsole.MarkupLine($" 2. Use your Tailscale IP ({displayIp.EscapeMarkup()}) for secure access anywhere.");
        AnsiConsole.MarkupLine(" 3. To authorize a passwordless login key, run: ssh-addkey");

    }

    private static bool IsCommandAvailable(string exe)
    {
        try
        {
            using var p=Process.Start(new ProcessStartInfo("where", exe)
            {
                RedirectStandardOutput=true, RedirectStandardError=true, UseShellExecute=false, CreateNoWindow=true
            }
            );
            p?.WaitForExit();
            return p?.ExitCode==0;
        }
        catch
        {
            return false;
        }

    }

    public static void AddAuthorizedKey(string key, string?account=null)
    {
        var targetUser=string.IsNullOrWhiteSpace(account)?Environment.UserName:account;
        var userHome=Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (!string.Equals(targetUser, Environment.UserName, StringComparison.OrdinalIgnoreCase))
        {
            var usersRoot=Directory.GetParent(userHome)!.FullName;
            userHome=System.IO.Path.Combine(usersRoot, targetUser!);
        }
        if (!Directory.Exists(userHome))
        {
            SpectrePanel.Error($"Home directory for user '{targetUser}' not found at {userHome}.");
            return;
        }
        var sshDir=System.IO.Path.Combine(userHome,".ssh");
        var authFile=System.IO.Path.Combine(sshDir,"authorized_keys");
        if (!Directory.Exists(sshDir))
        {
            Directory.CreateDirectory(sshDir);
            AnsiConsole.MarkupLine($"[cyan]📂 Created directory: {sshDir.EscapeMarkup()}[/]");
        }
        if (!File.Exists(authFile))
        {
            File.Create(authFile).Dispose();
            AnsiConsole.MarkupLine($"[cyan]📄 Created file: {authFile.EscapeMarkup()}[/]");
        }
        var existingKeys=File.ReadAllLines(authFile);
        if (existingKeys.Contains(key))
        {
            AnsiConsole.MarkupLine("[yellow]ℹ️ SSH Key is already authorized.[/]");
            return;
        }
        File.AppendAllText(authFile, key+Environment.NewLine);
        SpectrePanel.Success($"SSH key successfully authorized for user '{targetUser}'.");
        AnsiConsole.MarkupLine("[cyan]🔒 Setting secure permissions on SSH files...[/]");
        const string systemUser="NT AUTHORITY\\SYSTEM";
        var targetIdentity=$"{Environment.UserDomainName}\\{targetUser}";
        const FileSystemRights fullControl=FileSystemRights.FullControl;
        const AccessControlType allow=AccessControlType.Allow;
        var dirInfo=new DirectoryInfo(sshDir);
        var dirSecurity=dirInfo.GetAccessControl();
        dirSecurity.SetAccessRuleProtection(true, false);
        foreach (FileSystemAccessRule rule in dirSecurity.GetAccessRules(true, true, typeof(System.Security.Principal.NTAccount)))dirSecurity.RemoveAccessRule(rule);
        dirSecurity.AddAccessRule(new FileSystemAccessRule(targetIdentity, fullControl, InheritanceFlags.None, PropagationFlags.None, allow));
        dirSecurity.AddAccessRule(new FileSystemAccessRule(systemUser, fullControl, InheritanceFlags.None, PropagationFlags.None, allow));
        dirInfo.SetAccessControl(dirSecurity);
        var fileInfo=new FileInfo(authFile);
        var fileSecurity=fileInfo.GetAccessControl();
        fileSecurity.SetAccessRuleProtection(true, false);
        foreach (FileSystemAccessRule rule in fileSecurity.GetAccessRules(true, true, typeof(System.Security.Principal.NTAccount)))fileSecurity.RemoveAccessRule(rule);
        fileSecurity.AddAccessRule(new FileSystemAccessRule(targetIdentity, fullControl, InheritanceFlags.None, PropagationFlags.None, allow));
        fileSecurity.AddAccessRule(new FileSystemAccessRule(systemUser, fullControl, InheritanceFlags.None, PropagationFlags.None, allow));
        fileInfo.SetAccessControl(fileSecurity);
        SpectrePanel.Success("Secure OpenSSH file permissions applied.");

    }

    public static void StartMobileSshKeyReceiver(int port=8999)
    {
        var tsIp=IsCommandAvailable("tailscale")?SystemHelper.RunProcess("tailscale","ip -4", capture:true).Trim():null;
        var localIps=NetworkInterface.GetAllNetworkInterfaces().Where(n => n.OperationalStatus==OperationalStatus.Up&&(n.Name.Contains("Wi-Fi", StringComparison.OrdinalIgnoreCase)||n.Name.Contains("Ethernet", StringComparison.OrdinalIgnoreCase))).SelectMany(n => n.GetIPProperties().UnicastAddresses).Where(a => a.Address.AddressFamily==AddressFamily.InterNetwork).Select(a => a.Address.ToString()).ToArray();
        var displayIp=!string.IsNullOrWhiteSpace(tsIp)?tsIp:localIps.Length>0?localIps[0]:"localhost";
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold cyan]📱 Mobile SSH Key Authorizer[/]");
        AnsiConsole.MarkupLine("[cyan]=============================[/]");
        AnsiConsole.MarkupLine("[dim]Starting temporary local server to receive your public SSH key...[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[cyan]👉 Link to open in your phone's browser:[/]");
        AnsiConsole.MarkupLine($" [green]http://{displayIp}:{port}/[/]");
        AnsiConsole.MarkupLine($" [dim](or http://localhost:{port}/ if local)[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[yellow]Waiting for connection… (Timeout in 2 minutes. Press Ctrl+C to cancel)[/]");
        AnsiConsole.WriteLine();
        var listener=new HttpListener();
        listener.Prefixes.Add($"http://*:{port}/");

        try
        {
            listener.Start();
        }
        catch (Exception ex)
        {
            SpectrePanel.Error($"Failed to start HTTP listener: {ex.Message}. Make sure port {port} is not in use and you have administrator permissions.");
            return;
        }
        var timeout=TimeSpan.FromMinutes(2);
        var start=DateTime.Now;
        var success=false;

        try
        {
            while (DateTime.Now-start<timeout)
            {
                var getContext=listener.BeginGetContext(null, null);
                if (!getContext.AsyncWaitHandle.WaitOne(timeout-(DateTime.Now-start)))break;
                var context=listener.EndGetContext(getContext);
                var request=context.Request;
                var response=context.Response;
                if (request.HttpMethod=="GET")
                {
                    WriteHtml(response, FormHtml);
                }
                else if (request.HttpMethod=="POST")
                {
                    using var reader=new StreamReader(request.InputStream, Encoding.UTF8);
                    var body=reader.ReadToEnd();
                    var decoded=WebUtility.UrlDecode(body);
                    var sshKey=decoded.StartsWith("key=")?decoded[4..]:decoded;
                    sshKey=sshKey.Trim();
                    var isValid=Regex.IsMatch(sshKey,@"^ssh-(ed25519|rsa|dss|ecdsa) [A-Za-z0-9+/=]+( .+)?$");
                    if (isValid)
                    {
                        AddAuthorizedKey(sshKey);
                        success=true;
                        WriteHtml(response, SuccessHtml);
                    }
                    else
                    {
                        WriteHtml(response, InvalidHtml);
                    }
                    if (success)break;
                }
            }
        }
        finally
        {
            listener.Stop();
            listener.Close();
            AnsiConsole.MarkupLine("[dim]🛑 Mobile Key Authorizer server stopped.[/]");
        }

    }

    private static void WriteHtml(HttpListenerResponse response, string html)
    {
        var buffer=Encoding.UTF8.GetBytes(html);
        response.ContentLength64=buffer.Length;
        response.ContentType="text/html";
        response.OutputStream.Write(buffer, 0, buffer.Length);
        response.OutputStream.Close();

    }

    private const string PageStyle="body{font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,Helvetica,Arial,sans-serif;background-color:#0f141c;color:#abb2bf;margin:0;padding:20px;display:flex;justify-content:center;align-items:center;min-height:90vh}.container,.card{background-color:#161b22;border-radius:12px;padding:24px;max-width:500px;width:100%;box-shadow:0 4px 12px rgba(0,0,0,.3);border:1px solid #30363d}h2{color:#56b6c2;margin-top:0;font-size:1.5rem;text-align:center}p{font-size:.95rem;line-height:1.5;color:#8b949e}textarea{width:100%;height:120px;box-sizing:border-box;background-color:#0d1117;color:#c9d1d9;border:1px solid #30363d;border-radius:6px;padding:10px;font-family:monospace;font-size:.85rem;resize:vertical;margin-top:10px;margin-bottom:20px}button{width:100%;background-color:#238636;color:#fff;border:none;border-radius:6px;padding:12px;font-size:1rem;font-weight:bold;cursor:pointer}";

    private static readonly string FormHtml=$"<!DOCTYPE html><html><head><meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\"><title>Mobile SSH Key Authorizer</title><style>{PageStyle}</style></head><body><div class=\"container\"><h2>📱 Add SSH Public Key</h2><p>Paste the public SSH key from your mobile phone (e.g. from Termux's <code>~/.ssh/id_ed25519.pub</code>) to authorize connection.</p><form method=\"POST\"><textarea name=\"key\" placeholder=\"ssh-ed25519 AAAAC3NzaC1lZDI1NTE5...\" required></textarea><button type=\"submit\">Authorize Key</button></form></div></body></html>";
    private static readonly string SuccessHtml=$"<!DOCTYPE html><html><head><meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\"><title>Success</title><style>{PageStyle}h2{{color:#2ea043}}</style></head><body><div class=\"card\"><h2>✅ Success!</h2><p>The SSH key has been added to authorized_keys and NTFS file permissions have been secured.</p><p>You can close this window now.</p></div></body></html>";
    private static readonly string InvalidHtml=$"<!DOCTYPE html><html><head><meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\"><title>Invalid Key</title><style>{PageStyle}h2{{color:#f85149}}a{{color:#58a6ff}}</style></head><body><div class=\"card\"><h2>❌ Invalid SSH Key Format</h2><p>The key provided does not match a valid public SSH key format.</p><p><a href=\"/\">Go back and try again</a></p></div></body></html>";

}
public static class ThemeHelper
{
    private sealed record ThemeConfig(string?active_theme, bool?enable_mobile);

    public static string?SelectThemeInteractive(string themesPath, string?currentTheme)
    {
        if (!Directory.Exists(themesPath))
        {
            SpectrePanel.Error($"Themes directory not found: {themesPath}");
            return null;
        }
        var files=Directory.GetFiles(themesPath,"*.omp.json").OrderBy(f => f, StringComparer.OrdinalIgnoreCase).ToArray();
        if (files.Length==0)
        {
            SpectrePanel.Error($"No Oh My Posh themes (.omp.json) found in {themesPath}.");
            return null;
        }
        var themeNames=files.Select(f => System.IO.Path.GetFileName(f).Replace(".omp.json","")).ToArray();
        var displayLabels=new string[files.Length];
        for (var i=0;
        i<files.Length;
        i++)
        {
            var preview=BuildPreview(files[i]);
            displayLabels[i]=$"{themeNames[i].PadRight(25)} │ {preview}";
        }
        var defaultIndex=currentTheme!=null?Array.IndexOf(themeNames, currentTheme):-1;
        if (defaultIndex<0)defaultIndex=0;
        var selectedIndex=SpectreMenu.Show("Select Oh My Posh Theme (Color segment preview)", displayLabels, defaultIndex);
        if (selectedIndex<0)return null;
        var selectedTheme=themeNames[selectedIndex];
        PersistConfig(themesPath, selectedTheme, selectedTheme.EndsWith("-mobile"));
        Environment.SetEnvironmentVariable("THEME", selectedTheme);
        var themePath=System.IO.Path.Combine(themesPath,$"{selectedTheme}.omp.json");
        if (!File.Exists(themePath))return null;
        AnsiConsole.MarkupLine($"[green][[Theme]] Oh My Posh theme switched to '{selectedTheme}' (Persistent).[/]");
        return themePath;

    }

    public static string?ToggleMobileMode(string themesPath) => ApplyMobileMode(themesPath, !ReadConfig(themesPath).IsMobile);

    public static string?SetMobileMode(string themesPath, bool enableMobile) => ApplyMobileMode(themesPath, enableMobile);

    private static string?ApplyMobileMode(string themesPath, bool enableMobile)
    {
        if (!Directory.Exists(themesPath))return null;
        var current=ReadConfig(themesPath);
        var baseTheme=Regex.Replace(current.ThemeName,"-mobile$","");
        var themeName=baseTheme;
        if (enableMobile)
        {
            var candidate=$"{baseTheme}-mobile";
            if (File.Exists(System.IO.Path.Combine(themesPath,$"{candidate}.omp.json")))themeName=candidate;
        }
        PersistConfig(themesPath, themeName, enableMobile);
        Environment.SetEnvironmentVariable("THEME", themeName);
        var themePath=System.IO.Path.Combine(themesPath,$"{themeName}.omp.json");
        if (!File.Exists(themePath))return null;
        AnsiConsole.MarkupLine(enableMobile?"[cyan][[Theme]] Mobile Prompt Theme activated (ASCII mode, stacked).[/]":"[green][[Theme]] Desktop Prompt Theme activated (Rich Unicode/Emoji mode).[/]");
        return themePath;

    }

    private static(string ThemeName, bool IsMobile)ReadConfig(string themesPath)
    {
        var configPath=System.IO.Path.Combine(themesPath,"config.json");
        var themeName="neko";
        var isMobile=false;
        if (File.Exists(configPath))
        {
            try
            {
                var cfg=JsonSerializer.Deserialize<ThemeConfig>(File.ReadAllText(configPath));
                if (!string.IsNullOrWhiteSpace(cfg?.active_theme))themeName=cfg!.active_theme!;
                if (cfg?.enable_mobile is bool b)isMobile=b;
            }
            catch
            {
            }
        }
        return(themeName, isMobile);

    }

    public static string ResolveStartupTheme(string themesPath)
    {
        var configPath=System.IO.Path.Combine(themesPath,"config.json");
        if (File.Exists(configPath))return ReadConfig(themesPath).ThemeName;
        var legacyFile=System.IO.Path.Combine(themesPath,"active_theme.txt");
        if (File.Exists(legacyFile))
        {
            var theme=File.ReadAllText(legacyFile).Trim();

            try
            {
                File.Delete(legacyFile);
            }
            catch
            {
            }
            PersistConfig(themesPath, theme, theme.EndsWith("-mobile"));
            return theme;
        }
        return"neko";

    }

    private static void PersistConfig(string themesPath, string themeName, bool enableMobile)
    {
        var configPath=System.IO.Path.Combine(themesPath,"config.json");

        try
        {
            File.WriteAllText(configPath, JsonSerializer.Serialize(new ThemeConfig(themeName, enableMobile)));
        }
        catch
        {
        }
        try
        {
            File.Delete(System.IO.Path.Combine(themesPath,"active_theme.txt"));
        }
        catch
        {
        }
        try
        {
            File.Delete(System.IO.Path.Combine(themesPath,"mobile_mode_active.txt"));
        }
        catch
        {
        }

    }

    private static string BuildPreview(string filePath)
    {
        try
        {
            using var doc=JsonDocument.Parse(File.ReadAllText(filePath));
            if (!doc.RootElement.TryGetProperty("blocks", out var blocks))return"";
            var parts=new List<string>();
            foreach (var block in blocks.EnumerateArray())
            {
                if (parts.Count>=3)break;
                if (!block.TryGetProperty("segments", out var segments))continue;
                foreach (var seg in segments.EnumerateArray())
                {
                    if (parts.Count>=3)break;
                    var color=seg.TryGetProperty("background", out var bg)?bg.GetString():seg.TryGetProperty("foreground", out var fg)?fg.GetString():null;
                    var type=seg.TryGetProperty("type", out var t)?t.GetString():"";
                    parts.Add($"{MapHexToEmoji(color)} {type}");
                }
            }
            return string.Join(" ", parts);
        }
        catch
        {
            return"";
        }

    }

    private static string MapHexToEmoji(string?hex)
    {
        var emoji="🔵";
        if (string.IsNullOrWhiteSpace(hex))return emoji;
        var m=Regex.Match(hex,@"^#?([0-9a-fA-F]{6})$");
        if (!m.Success)return emoji;
        var clean=m.Groups[1].Value;
        var r=Convert.ToInt32(clean[..2], 16);
        var g=Convert.ToInt32(clean.Substring(2, 2), 16);
        var b=Convert.ToInt32(clean.Substring(4, 2), 16);
        var max=Math.Max(r, Math.Max(g, b));
        var min=Math.Min(r, Math.Min(g, b));
        if (max-min<30)emoji=max<64?"⚫":max>192?"⚪":"🔘";

        else if (r>g&&r>b)emoji=(g-b)>40?"🟠":"🔴";

        else if (g>r&&g>b)emoji="🟢";

        else if (b>r&&b>g)emoji=(r-g)>40?"🟣":"🔵";

        else if (r>b&&g>b)emoji=Math.Abs(r-g)<40?"🟡":"🟠";
        return emoji;

    }

}
public static class AgyAiCore
{
    private static readonly string OllamaDefaultModelFile=System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),".ollama_default_model");

    private static string _ollamaDefaultModel=LoadDefaultModel();
    public static string OllamaDefaultModel => _ollamaDefaultModel;

    private static string LoadDefaultModel()
    {
        try
        {
            if (File.Exists(OllamaDefaultModelFile))
            {
                var saved=File.ReadAllText(OllamaDefaultModelFile).Trim();
                if (!string.IsNullOrWhiteSpace(saved))return saved;
            }
        }
        catch
        {
        }
        return"qwen3:1.7b";

    }

    private static void PersistDefaultModel(string model)
    {
        _ollamaDefaultModel=model;

        try
        {
            File.WriteAllText(OllamaDefaultModelFile, model);
        }
        catch
        {
        }

    }

    public static string GetProfileRepoRoot()
    {
        var asmPath = typeof(AgyAiCore).Assembly.Location;
        if (string.IsNullOrEmpty(asmPath)) return Directory.GetCurrentDirectory();
        var asmDir = Path.GetDirectoryName(asmPath);
        if (string.IsNullOrEmpty(asmDir)) return Directory.GetCurrentDirectory();
        var parent = Path.GetDirectoryName(asmDir);
        if (parent == null) return asmDir;
        var grandParent = Path.GetDirectoryName(parent);
        return grandParent ?? parent;
    }

    private static string GetConfigPath()
    {
        return Path.Combine(GetProfileRepoRoot(), "profile.config.json");
    }

    public static string GetAiProviderMode()
    {
        var path = GetConfigPath();
        if (!File.Exists(path)) return "cloud";
        try
        {
            var content = File.ReadAllText(path);
            using var doc = System.Text.Json.JsonDocument.Parse(content);
            if (doc.RootElement.TryGetProperty("AiProviderMode", out var prop))
            {
                return prop.GetString() ?? "cloud";
            }
        }
        catch {}
        return "cloud";
    }

    public static bool IsAiOllamaEnabled()
    {
        var path = GetConfigPath();
        if (!File.Exists(path)) return true;
        try
        {
            var content = File.ReadAllText(path);
            using var doc = System.Text.Json.JsonDocument.Parse(content);
            if (doc.RootElement.TryGetProperty("EnableAiOllama", out var prop))
            {
                return prop.ValueKind != System.Text.Json.JsonValueKind.False;
            }
        }
        catch {}
        return true;
    }

    public static bool IsAgyEnabled()
    {
        var path = GetConfigPath();
        if (!File.Exists(path)) return true;
        try
        {
            var content = File.ReadAllText(path);
            using var doc = System.Text.Json.JsonDocument.Parse(content);
            if (doc.RootElement.TryGetProperty("EnableAgy", out var prop))
            {
                return prop.ValueKind != System.Text.Json.JsonValueKind.False;
            }
        }
        catch {}
        return true;
    }

    public static string GetEffectiveProviderMode()
    {
        var mode = GetAiProviderMode();
        if (mode == "auto")
        {
            return IsOllamaRunning() ? "local" : "cloud";
        }
        return mode;
    }

    public static void SetAiProviderMode(string mode)
    {
        var path = GetConfigPath();
        if (!File.Exists(path)) return;
        try
        {
            var content = File.ReadAllText(path);
            var options = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };
            var dict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(content);
            if (dict != null)
            {
                dict["AiProviderMode"] = mode;
                var updated = System.Text.Json.JsonSerializer.Serialize(dict, options);
                File.WriteAllText(path, updated);
            }
        }
        catch {}
    }

    private static string ResolveProxyScriptPath()
    {
        var asmDir=System.IO.Path.GetDirectoryName(typeof(AgyAiCore).Assembly.Location)??Directory.GetCurrentDirectory();
        var dir=new DirectoryInfo(asmDir);
        for (var i=0;
        i<5&&dir!=null;
        i++, dir=dir.Parent)
        {
            var candidate1=System.IO.Path.Combine(dir.FullName,"Tests","Mocks","ollama-proxy.js");
            if (File.Exists(candidate1))return candidate1;
            var candidate2=System.IO.Path.Combine(dir.FullName,"Tests","ollama-proxy.js");
            if (File.Exists(candidate2))return candidate2;
        }
        return System.IO.Path.Combine(asmDir,"ollama-proxy.js");

    }

    private static bool IsPortListening(int port) => IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners().Any(e => e.Port==port);

    private static bool IsPortResponding(int port, string?pattern)
    {
        try
        {
            using var handler=new HttpClientHandler
            {
                UseProxy=false
            }
            ;

            using var client=new HttpClient(handler)
            {
                Timeout=TimeSpan.FromSeconds(2)
            }
            ;
            var resp=client.GetStringAsync($"http://127.0.0.1:{port}/").GetAwaiter().GetResult();
            return string.IsNullOrEmpty(pattern)||resp.Contains(pattern.Trim('*'), StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return IsPortListening(port);
        }

    }

    private static bool? _lastOllamaStatus;
    private static DateTime _ollamaStatusCachedAt = DateTime.MinValue;

    public static bool IsOllamaRunning()
    {
        if (_lastOllamaStatus.HasValue && (DateTime.UtcNow - _ollamaStatusCachedAt).TotalSeconds < 3)
        {
            return _lastOllamaStatus.Value;
        }
        var status = IsPortResponding(11434, "*Ollama is running*");
        _lastOllamaStatus = status;
        _ollamaStatusCachedAt = DateTime.UtcNow;
        return status;
    }

    public static void EnsureOllamaProxy()
    {
        const int proxyPort=11435;
        if (IsPortResponding(proxyPort, null))return;
        AnsiConsole.MarkupLine($"[yellow][[AI]] Ollama Proxy is not running on port {proxyPort}. Starting...[/]");
        var proxyScriptPath=ResolveProxyScriptPath();
        if (!File.Exists(proxyScriptPath))
        {
            SpectrePanel.Error($"Ollama proxy script not found at {proxyScriptPath}.");
            return;
        }
        var stdoutPath=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"ollama_proxy_out.log");
        var stderrPath=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"ollama_proxy_err.log");

        try
        {
            File.Delete(stdoutPath);
        }
        catch
        {
        }
        try
        {
            File.Delete(stderrPath);
        }
        catch
        {
        }
        try
        {
            var psi=new ProcessStartInfo("node",$"\"{proxyScriptPath}\"")
            {
                UseShellExecute=false, CreateNoWindow=true, WindowStyle=ProcessWindowStyle.Hidden, RedirectStandardOutput=true, RedirectStandardError=true
            }
            ;
            var proc=Process.Start(psi);
            if (proc!=null)
            {
                _=Task.Run(() =>
                {
                    try
                    {
                        using var f=File.Create(stdoutPath);
                        proc.StandardOutput.BaseStream.CopyTo(f);
                    }
                    catch
                    {
                    }
                }
                );
                _=Task.Run(() =>
                {
                    try
                    {
                        using var f=File.Create(stderrPath);
                        proc.StandardError.BaseStream.CopyTo(f);
                    }
                    catch
                    {
                    }
                }
                );
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red][[AI]] Failed to start Ollama Proxy: {ex.Message.EscapeMarkup()}[/]");
        }
        Thread.Sleep(1000);

    }

    public static void EnsureOllamaServer()
    {
        if (!IsOllamaRunning())InitializeOllamaServer();
        EnsureOllamaProxy();

    }

    public static void InvokeOllamaNative(string?model)
    {
        EnsureOllamaServer();
        var activeModel=string.IsNullOrWhiteSpace(model)?OllamaDefaultModel:model;
        AnsiConsole.MarkupLine($"[cyan]Starting native Ollama interactive session for '{activeModel.EscapeMarkup()}'...[/]");
        RunInteractive("ollama", ["run", activeModel]);

    }

    private static string AppendNodeOption(string?existing) => string.IsNullOrEmpty(existing)?"--dns-result-order=ipv4first":$"{existing} --dns-result-order=ipv4first";

    public static void InvokeClaude(string[]argsList, string? providerModeOverride = null)
    {
        var mode = providerModeOverride ?? GetEffectiveProviderMode();
        if (mode == "cloud")
        {
            RunInteractive("claude.cmd", argsList);
        }
        else
        {
            EnsureOllamaServer();
            var env=new Dictionary<string, string?>
            {
                ["OLLAMA_HOST"]="127.0.0.1:11434", ["ANTHROPIC_BASE_URL"]="http://127.0.0.1:11434", ["NODE_OPTIONS"]=AppendNodeOption(Environment.GetEnvironmentVariable("NODE_OPTIONS"))
            }
            ;
            var argList=new List<string>
            {
                "launch","claude"
            }
            ;
            if (!argsList.Contains("--model"))
            {
                argList.Add("--model");
                argList.Add(OllamaDefaultModel);
            }
            argList.AddRange(argsList);
            RunInteractive("ollama.exe", argList, env);
        }

    }

    public static void InvokeCodex(string[]argsList, string? providerModeOverride = null)
    {
        var mode = providerModeOverride ?? GetEffectiveProviderMode();
        if (mode == "cloud")
        {
            RunInteractive("codex.cmd", argsList);
        }
        else
        {
            EnsureOllamaServer();
            var model=OllamaDefaultModel;
            var newArgsList=new List<string>();
            for (var i=0;
            i<argsList.Length;
            i++)
            {
                if ((argsList[i]=="--model"||argsList[i]=="-m")&&i<argsList.Length-1)
                {
                    model=Regex.Replace(argsList[i+1],"^ollama_custom/","");
                    newArgsList.Add(argsList[i]);
                    newArgsList.Add(model);
                    i++;
                }
                else newArgsList.Add(argsList[i]);
            }
            var sandboxPath=System.IO.Path.Combine(System.IO.Path.GetTempPath(),".codex_local_ollama");
            Directory.CreateDirectory(sandboxPath);
            var emptySkillsDir=@"C:\Users\TruongNhon\.gemini\antigravity\scratch\empty_skills";

            try
            {
                Directory.CreateDirectory(emptySkillsDir);
            }
            catch
            {
            }
            var configToml=$"# Temp sandbox configuration generated at {sandboxPath}/config.toml\nmodel = \"{model}\"\n\n[codex]\nskills_directory = \"{emptySkillsDir}\"\n\n[mcp_servers]\n# Intentionally empty to disable external tool description loads\n".Replace('\\','/');
            File.WriteAllText(System.IO.Path.Combine(sandboxPath,"config.toml"), configToml);
            var env=new Dictionary<string, string?>
            {
                ["OLLAMA_HOST"]="127.0.0.1:11435", ["NODE_OPTIONS"]=AppendNodeOption(Environment.GetEnvironmentVariable("NODE_OPTIONS")), ["OPENAI_BASE_URL"]=null, ["OPENAI_API_KEY"]=null, ["CODEX_HOME"]=sandboxPath
            }
            ;
            var flags=new List<string>();
            if (!newArgsList.Contains("--model")&&!newArgsList.Contains("-m"))
            {
                flags.Add("--model");
                flags.Add(model);
            }
            flags.Add("--oss");
            flags.Add("--local-provider");
            flags.Add("ollama");
            var argList=new List<string>(flags);
            argList.AddRange(newArgsList);
            RunInteractive("codex.cmd", argList, env);
        }

    }

    public static void EnsureOpenClawGateway()
    {
        const int port=18789;
        if (IsPortListening(port))return;
        AnsiConsole.MarkupLine("[yellow][[AI]] OpenClaw Gateway is not running. Starting...[/]");

        try
        {
            Process.Start(new ProcessStartInfo("openclaw","gateway start")
            {
                UseShellExecute=false, CreateNoWindow=true, WindowStyle=ProcessWindowStyle.Hidden
            }
            );
        }
        catch
        {
        }
        Thread.Sleep(2000);

    }

    public static void InvokeOpenClaw(string[]argsList)
    {
        EnsureOllamaServer();
        EnsureOpenClawGateway();
        string?model=null;
        var cleanArgs=new List<string>();
        for (var i=0;
        i<argsList.Length;
        i++)
        {
            if (argsList[i]=="--model"&&i<argsList.Length-1)
            {
                model=argsList[i+1];
                i++;
            }
            else cleanArgs.Add(argsList[i]);
        }
        model??=OllamaDefaultModel;
        var cleanModel=Regex.Replace(model,"^ollama/","");
        RunInteractive("openclaw.cmd", ["config","set","agents.defaults.model.primary",$"ollama/{cleanModel}"]);
        var argList2=cleanArgs.Count==0?new List<string>
        {
            "chat"
        }
        :cleanArgs;
        var env=new Dictionary<string, string?>
        {
            ["OLLAMA_HOST"]="127.0.0.1:11434"
        }
        ;
        RunInteractive("openclaw.cmd", argList2, env);

    }

    public static void InvokeClawdbot(string[]argsList) => InvokeOpenClaw(argsList);

    public enum HermesResult
    {
        Launched, NotInstalled

    }

    public static HermesResult InvokeHermes(string[]argsList)
    {
        EnsureOllamaServer();
        var bin=FindHermesBinary("hermes", [System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),".hermes","bin","hermes.exe"), System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),".hermes","bin","hermes.cmd"), System.IO.Path.Combine(Environment.GetEnvironmentVariable("LOCALAPPDATA")??"","Programs","Hermes","bin","hermes.exe")]);
        if (bin==null)return HermesResult.NotInstalled;
        var configPath=System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),".hermes","config.toml");
        if (!File.Exists(configPath))
        {
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(configPath)!);
            File.WriteAllText(configPath,"");
        }
        var configContent=File.ReadAllText(configPath);
        if (!configContent.Contains("127.0.0.1:11434"))
        {
            AnsiConsole.MarkupLine("[yellow][[AI]] Configuring local Ollama endpoint in Hermes config.toml...[/]");
            File.AppendAllText(configPath,"\n[model_providers.ollama_custom]\nname = \"Ollama Custom\"\nbase_url = \"http://127.0.0.1:11434/v1\"\n");
        }
        var argList=new List<string>
        {
            "chat"
        }
        ;
        foreach (var a in argsList)if (a!="--model"&&a!=OllamaDefaultModel)argList.Add(a);
        AnsiConsole.MarkupLine("[cyan]Starting Hermes Agent TUI...[/]");
        RunInteractive(bin, argList);
        return HermesResult.Launched;

    }

    public static HermesResult InvokeHermesDesktop(string[]argsList)
    {
        EnsureOllamaServer();
        var bin=FindHermesBinary("hermes-desktop", [System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),".hermes","bin","hermes-desktop.exe"), System.IO.Path.Combine(Environment.GetEnvironmentVariable("LOCALAPPDATA")??"","Programs","Hermes","bin","hermes-desktop.exe")]);
        if (bin!=null)
        {
            AnsiConsole.MarkupLine("[cyan]Starting Hermes Desktop...[/]");
            RunInteractive(bin, []);
            return HermesResult.Launched;
        }
        var cliBin=FindOnPath("hermes");
        if (cliBin!=null)
        {
            AnsiConsole.MarkupLine("[cyan]Starting Hermes Desktop...[/]");
            RunInteractive(cliBin, ["desktop"]);
            return HermesResult.Launched;
        }
        return HermesResult.NotInstalled;

    }

    private static string?FindHermesBinary(string exeNameOnPath, string[]localPaths)
    {
        var onPath=FindOnPath(exeNameOnPath);
        if (onPath!=null)return onPath;
        foreach (var p in localPaths)if (File.Exists(p))return p;
        return null;

    }

    internal static string?FindOnPath(string exe)
    {
        try
        {
            var psi=new ProcessStartInfo("where", exe)
            {
                RedirectStandardOutput=true, UseShellExecute=false, CreateNoWindow=true
            }
            ;

            using var p=Process.Start(psi);
            var output=p?.StandardOutput.ReadToEnd().Trim();
            p?.WaitForExit();
            if (p?.ExitCode==0&&!string.IsNullOrWhiteSpace(output))return output.Split('\n')[0].Trim();
        }
        catch
        {
        }
        return null;

    }

    public static void InitializeOllamaServer()
    {
        const int port=11434;
        AnsiConsole.MarkupLine("[cyan][[Ollama]] Resetting port 11434...[/]");
        if (IsPortListening(port))
        {
            SystemHelper.KillPort(port);
            Thread.Sleep(1000);
        }
        else
        {
            AnsiConsole.MarkupLine("[green][[Ollama]] Port 11434 is free.[/]");
        }
        AnsiConsole.MarkupLine("[cyan][[Ollama]] Starting Ollama server...[/]");
        var logPath=System.IO.Path.Combine(Environment.GetEnvironmentVariable("LOCALAPPDATA")??System.IO.Path.GetTempPath(),"Ollama","server.log");
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(logPath)!);

        try
        {
            var psi=new ProcessStartInfo("ollama","serve")
            {
                UseShellExecute=false, CreateNoWindow=true, WindowStyle=ProcessWindowStyle.Hidden, RedirectStandardOutput=true, RedirectStandardError=true
            }
            ;
            psi.Environment["OLLAMA_HOST"]=$"127.0.0.1:{port}";
            var proc=Process.Start(psi);
            if (proc!=null)
            {
                _=Task.Run(() =>
                {
                    try
                    {
                        using var f=File.Create(logPath);
                        proc.StandardOutput.BaseStream.CopyTo(f);
                    }
                    catch
                    {
                    }
                }
                );
                _=Task.Run(() =>
                {
                    try
                    {
                        proc.StandardError.BaseStream.CopyTo(Stream.Null);
                    }
                    catch
                    {
                    }
                }
                );
            }
        }
        catch
        {
        }
        for (var retry=0;
        retry<10;
        retry++)
        {
            Thread.Sleep(1000);
            if (IsPortResponding(port,"*Ollama is running*"))
            {
                AnsiConsole.MarkupLine("[green][[Ollama]] Ollama server is running and ready![/]");
                return;
            }
        }
        AnsiConsole.MarkupLine("[yellow][[Ollama]] Failed to verify if Ollama started successfully after 10 seconds.[/]");

    }

    public static void InstallAIIntegrations()
    {
        InstallIfMissing("claude","@anthropic-ai/claude-code","Claude Code");
        InstallIfMissing("codex","@openai/codex","Codex CLI");
        InstallIfMissing("openclaw","openclaw","OpenClaw");

    }

    private static void InstallIfMissing(string command, string npmPackage, string label)
    {
        if (FindOnPath(command)!=null)
        {
            AnsiConsole.MarkupLine($"[green][[AI]] {label.EscapeMarkup()} is already installed.[/]");
            return;
        }
        AnsiConsole.MarkupLine($"[cyan][[AI]] Installing {label.EscapeMarkup()} via npm...[/]");
        RunInteractive(FindOnPath("npm.cmd")??"npm.cmd", ["install","-g", npmPackage]);

    }

    public static void SetOllamaModel(string?modelName)
    {
        if (FindOnPath("ollama")==null)
        {
            SpectrePanel.Error("Ollama is not installed or not in PATH.");
            return;
        }
        var listOutput=RunCapture("ollama","list");
        var localModels=listOutput.Split('\n').Skip(1).Select(l => Regex.Split(l.Trim(),@"\s+").FirstOrDefault()).Where(m => !string.IsNullOrWhiteSpace(m)).Select(m => m!).ToArray();
        if (localModels.Length==0)
        {
            SpectrePanel.Error("No local Ollama models found. Please download one using 'ollama pull'.");
            return;
        }
        if (!string.IsNullOrWhiteSpace(modelName))
        {
            if (localModels.Contains(modelName))
            {
                PersistDefaultModel(modelName);
                AnsiConsole.MarkupLine($"[green]🟢 Default Ollama model set to '{modelName.EscapeMarkup()}'.[/]");
            }
            else
            {
                SpectrePanel.Error($"Model '{modelName}' is not available locally. Available models: {string.Join(", ", localModels)}");
            }
            return;
        }
        var menuItems=new string[localModels.Length];
        var defaultIdx=0;
        for (var i=0;
        i<localModels.Length;
        i++)
        {
            menuItems[i]=localModels[i]==OllamaDefaultModel?$"{localModels[i]} (Active)":localModels[i];
            if (localModels[i]==OllamaDefaultModel)defaultIdx=i;
        }
        var selected=SpectreMenu.Show("Select Default Ollama Model", menuItems, defaultIdx);
        if (selected>=0)
        {
            PersistDefaultModel(localModels[selected]);
            AnsiConsole.MarkupLine($"[green]🟢 Default Ollama model set to '{localModels[selected].EscapeMarkup()}'.[/]");
        }
        else
        {
            AnsiConsole.MarkupLine("[yellow]Cancelled.[/]");
        }

    }

    public static void ShowOllamaLogs()
    {
        EnsureOllamaServer();
        var logPath=System.IO.Path.Combine(Environment.GetEnvironmentVariable("LOCALAPPDATA")??System.IO.Path.GetTempPath(),"Ollama","server.log");
        if (File.Exists(logPath))
        {
            AnsiConsole.MarkupLine("[cyan]--- Ollama Server Log (Last 50 lines) ---[/]");
            foreach (var line in File.ReadLines(logPath).TakeLast(50))Console.WriteLine(line);
            AnsiConsole.MarkupLine("[cyan]----------------------------------------[/]");
        }
        else
        {
            SpectrePanel.Warning($"Ollama server log not found at {logPath}.");
        }

    }

    public static void ShowAiDashboard()
    {
        while (true)
        {
            var statusInfo=(OllamaStatus)SpectreProgress.SpinnerResult("[AI] Loading Ollama server configuration...", () =>
            {
                var status="Offline";
                var models=new List<string>();

                try
                {
                    using var client=new HttpClient
                    {
                        Timeout=TimeSpan.FromSeconds(1)
                    }
                    ;
                    client.GetStringAsync("http://127.0.0.1:11434/").GetAwaiter().GetResult();
                    status="Running";
                }
                catch
                {
                }
                if (status=="Running")
                {
                    try
                    {
                        var list=RunCapture("ollama","list");
                        var lines=list.Split('\n');
                        for (var i=1;
                        i<lines.Length;
                        i++)
                        {
                            var parts=Regex.Split(lines[i].Trim(),@"\s+");
                            if (parts.Length>0&&!string.IsNullOrWhiteSpace(parts[0]))models.Add(parts[0]);
                        }
                    }
                    catch
                    {
                    }
                }
                return (object)new OllamaStatus(status, models);
            }
            )!;
        var cHalf=(char)0x2584;
        var cFull=(char)0x2588;
        var cTop=(char)0x2580;
        var aiHeaders=new[]
        {
            $" {cHalf}{cFull}{cFull}{cFull}{cFull}{cHalf} {cHalf}{cFull}{cFull}{cFull}{cFull}{cHalf} Powershell Profile CLI v2.0",$" {cFull}{cTop} {cTop} {cFull}{cTop} {cTop} Ollama Local AI Hub",$" {cFull} {cFull} Ollama Status: {statusInfo.Status}",$" {cFull}{cHalf} {cHalf} {cFull}{cHalf} {cHalf} Active Model: {OllamaDefaultModel}",$" {cTop}{cFull}{cFull}{cFull}{cFull}{cTop} {cTop}{cFull}{cFull}{cFull}{cFull}{cTop} Select an agent to run. Esc to back.","============================================="
        }
        ;
        var providerMode = GetAiProviderMode();
        var modeLabel = providerMode switch
        {
            "cloud" => "Cloud API (Normal)",
            "local" => "Local Ollama",
            "auto" => "Auto (Local if online, else Cloud)",
            _ => "Cloud API (Normal)"
        };
        var menuItems=new[]
        {
            "[Agent] Claude CLI (Interactive coding chat)","[Agent] Hermes TUI (Autonomous workspace assistant)","[Agent] Codex CLI (Natural language command tool)","[Agent] OpenClaw CLI (Local agent router)","[Agent] Clawdbot TUI (Interactive helper)",$"[Setting] Provider Mode: {modeLabel}","[Model] Select / Set Default Local Model","[Action] Auto-Install missing LLM CLI tools","[x] Return to Main Menu"
        }
        ;
        while (Console.KeyAvailable)Console.ReadKey(true);
        var selected=SpectreMenu.ShowRobust(aiHeaders, menuItems, 0, false, true);
        if (selected<0||selected==menuItems.Length-1)break;
        switch (selected)
        {
            case 0:InvokeClaude([]);
            break;
            case 1:InvokeHermes(OllamaDefaultModel is
            {
                Length:>0
            }
            ?["--model", OllamaDefaultModel]:[]);
            break;
            case 2:InvokeCodex(OllamaDefaultModel is
            {
                Length:>0
            }
            ?["--model", OllamaDefaultModel]:[]);
            break;
            case 3:InvokeOpenClaw([]);
            break;
            case 4:InvokeClawdbot(OllamaDefaultModel is
            {
                Length:>0
            }
            ?["--model", OllamaDefaultModel]:[]);
            break;
            case 5:
            var choices = new[] { "cloud", "local", "auto" };
            var labels = new[] { "Cloud API (Normal)", "Local Ollama", "Auto (Local if online, else Cloud)" };
            var defaultIdx = Array.IndexOf(choices, providerMode);
            if (defaultIdx < 0) defaultIdx = 0;
            var chosenIdx = SpectreMenu.Show("Select AI Provider Mode", labels, defaultIdx);
            if (chosenIdx >= 0)
            {
                var chosenMode = choices[chosenIdx];
                SetAiProviderMode(chosenMode);
                AnsiConsole.MarkupLine($"[green][[AI]] Switched Provider Mode to '{labels[chosenIdx]}'.[/]");
                Thread.Sleep(1000);
            }
            break;
            case 6:SetOllamaModel(null);
            break;
            case 7:InstallAIIntegrations();
            break;
        }

    }

}

private sealed record OllamaStatus(string Status, List<string>Models);

public static void AskAi(string resolvedQueryOrError)
{
    if (string.IsNullOrWhiteSpace(resolvedQueryOrError))
    {
        AnsiConsole.MarkupLine("[yellow]No recent console errors found to explain.[/]");
        return;

    }
    AnsiConsole.MarkupLine("[cyan]🤖 Querying local AI for explanation/fix...[/]");
    var prompt=$"Analyze the following PowerShell error or question and provide a brief explanation and a clear, copy-pasteable fix:\n\n{resolvedQueryOrError}";
    var body=JsonSerializer.Serialize(new
    {
        model=OllamaDefaultModel, prompt, stream=false

    }
    );

    try
    {
        using var client=new HttpClient
        {
            Timeout=TimeSpan.FromSeconds(10)
        }
        ;
        var resp=client.PostAsync("http://127.0.0.1:11434/api/generate", new StringContent(body, Encoding.UTF8,"application/json")).GetAwaiter().GetResult();
        var text=resp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        var json=JsonDocument.Parse(text);
        if (json.RootElement.TryGetProperty("response", out var respProp))
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[green]🤖 AI Explanation:[/]");
            Console.WriteLine(respProp.GetString()?.Trim());
        }

    }
    catch
    {
        SpectrePanel.Error("Failed to connect to local Ollama. Ensure Ollama server is running.");

    }

}

private static void RunInteractive(string exe, IEnumerable<string>args, IDictionary<string, string?>?env=null, string?workingDir=null)
{
    var resolvedExe=System.IO.Path.IsPathRooted(exe)?exe:FindOnPath(exe)??exe;
    var psi=new ProcessStartInfo(resolvedExe)
    {
        UseShellExecute=false, WorkingDirectory=workingDir??Directory.GetCurrentDirectory()

    }
    ;
    foreach (var a in args)psi.ArgumentList.Add(a);
    if (env!=null)
    {
        foreach (var kv in env)
        {
            if (kv.Value==null)psi.Environment.Remove(kv.Key);

            else psi.Environment[kv.Key]=kv.Value;
        }

    }
    try
    {
        using var p=Process.Start(psi);
        p?.WaitForExit();

    }
    catch (Exception ex)
    {
        SpectrePanel.Error($"Failed to launch '{exe}': {ex.Message}");

    }

}

private static string RunCapture(string exe, string args)
{
    var psi=new ProcessStartInfo(exe, args)
    {
        RedirectStandardOutput=true, UseShellExecute=false, CreateNoWindow=true

    }
    ;

    using var p=Process.Start(psi);
    if (p==null)return"";
    var output=p.StandardOutput.ReadToEnd();
    p.WaitForExit();
    return output;

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
        if (string.IsNullOrWhiteSpace(output))AnsiConsole.MarkupLine("[dim] (no results or LocalStack unavailable)[/]");

        else foreach (var line in output.Trim().Split('\n'))AnsiConsole.MarkupLine($" {line.EscapeMarkup()}");

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
                SystemHelper.RunProcess("npm.cmd",$"create vite@latest {name} -- --template react-ts");
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
            var num=$"{i + 1,4} ";
            var colored=ColorizeToken(l, ext);
            return highlightLines.Contains(i+1)?$"[yellow]{num}→[/] {colored}":$"[dim]{num}[/] {colored}";
        }
        ).ToArray();
        SpectrePager.Show(System.IO.Path.GetFileName(filePath), numbered);

    }

    private static string[]LoadWithLineNumbers(string filePath)
    {
        var ext=System.IO.Path.GetExtension(filePath).ToLower();
        var lines=File.ReadAllLines(filePath);
        return lines.Select((l, i) => $"[dim]{i + 1,4}[/] {ColorizeToken(l, ext)}").ToArray();

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
                    if (name.Length>2)symbols.Add($"{name,-30} ln {i + 1,4}");
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
    public static readonly PaletteCommand[]Commands=[new("proj","Navigate to a registered workspace","Navigation"), new("p","Alias for proj","Navigation"), new("gs","Git status summary","Git"), new("gcmt","Conventional commit wizard","Git"), new("git-undo","Soft-reset the last local commit","Git"), new("dbld","dotnet build in active workspace",".NET"), new("dtst","dotnet test in active workspace",".NET"), new("clean-build","Remove bin/ and obj/ recursively",".NET"), new("add-migration","EF Core: add migration",".NET"), new("update-db","EF Core: update database",".NET"), new("dkcl","Docker cleanup TUI dashboard","Docker"), new("dcup","docker compose up -d","Docker"), new("dcdown","docker compose down","Docker"), new("aws-local","LocalStack sandbox diagnostics","AWS"), new("claude","Launch Claude Code CLI","AI"), new("codex","Launch Codex CLI","AI"), new("openclaw","Launch OpenClaw via Ollama","AI"), new("hermes","Launch Hermes3 via Ollama","AI"), new("hermesd","Launch Hermes3 debug mode","AI"), new("ollama-status","Check local Ollama server status and pulled models","AI"), new("disk","Show disk usage and health","System"), new("public-ip","Resolve public IPv4 address","System"), new("kill-port","Kill process by port number","System"), new("ssh-info","SSH connection summary","System"), new("db-tui","SQLite schema and data viewer","Database"), new("agyswitch","Switch AGY account context","Accounts"), new("agyquota","Show quota usage for all accounts","Accounts"), new("scaffold","Create new project from template","Scaffold"), new("help","Open interactive help browser","Help"), new("cc","Open this Command Palette","Help"), new("learn","Start learning for a topic (auto-refresh)","Learn"), new("flashcard","Open flashcard deck browser","Learn"), new("vocab","English vocabulary drill","Learn"), new("kana","Hiragana / katakana quiz","Learn"), new("kanji","Kanji lookup / stroke detail","Learn"), new("jlpt","JLPT vocabulary drill","Learn"), new("algo","Algorithm visualizer (sort / search)","Learn"), new("complexity","Big-O complexity cheat-sheet","Learn"), new("problems","DSA problem tracker","Learn"), new("snippets","Code snippet library browser","Learn"), new("sheets","Cheat-sheet browser (.txt files)","Learn"), new("quiz","C# multiple-choice quiz","Learn"), new("interview","Interview question bank","Learn"), new("star","STAR answer builder","Learn"), new("mock","Mock interview timer","Learn"), new("word-of-day","Show today's word of the day","Learn"), new("session","Start a Pomodoro study session","Tracking"), new("stats","Study statistics and weekly chart","Tracking"), new("goals","Daily learning goals","Tracking"), new("streak","Study streak display","Tracking"), new("due","Show due spaced-repetition reviews","Tracking"), new("progress","Progress dashboard (bar chart + tree)","Tracking"), new("weak","Weak items queue (pre-session review)","Tracking"), new("obsidian","Configure / browse Obsidian vault","Obsidian"), new("obs-graph","Obsidian wikilink graph","Obsidian"), new("nexus","Git Nexus multi-repo dashboard","Git"), new("repo-graph","Repository dependency graph","Git"), new("nexus-stats","Git Nexus commit stats","Git"), new("ide","Terminal IDE (browse, view, diff)","IDE"), new("ide-diff","Git diff viewer for current dir","IDE"), new("ide-search","Search pattern across files","IDE"), new("refresh","Refresh learning data from vault","Resources"), new("add-resource","Add a file/URL to resource registry","Resources"),];

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
        ["Navigation"]=["proj <query> — Navigate to a workspace matching <query>."," If multiple matches are found an interactive selector opens."," If exactly one matches, jumps immediately.","Alias: p",], ["Git"]=["gs — Short git status (--short) with color coding.","gcmt — Conventional commit wizard. Prompts for:"," 1. Type: feat | fix | docs | style | refactor | test | chore | ci"," 2. Scope (optional)"," 3. Short description (5–72 chars)"," 4. Breaking changes / issues closed","git-undo — Soft-reset the last commit (keeps changes staged).",], [".NET"]=["dbld — dotnet build in the active workspace.","dtst — dotnet test in the active workspace.","clean-build — Recursively delete all bin/ and obj/ folders.","add-migration — dotnet ef migrations add <name>","update-db — dotnet ef database update",], ["Docker"]=["dkcl — Docker cleanup TUI dashboard. Options:"," • Stop & remove all containers"," • Prune images and dangling layers"," • Delete unused volumes / networks"," • Full cleanup (all of the above)","dcup — docker compose up -d","dcdown — docker compose down",], ["AWS / LocalStack"]=["aws-local — Query running LocalStack sandbox on http://localhost:4566."," Shows: S3 buckets, SQS queues, Lambda functions.",], ["AI / LLM"]=["claude — Launch Claude Code CLI.","codex — Launch Codex CLI.","openclaw — Launch OpenClaw model via local Ollama daemon.","hermes — Launch Hermes3 model via Ollama.","hermesd — Launch Hermes3 in debug mode."," Note: Ollama daemon on port 11434 is started automatically if offline.",], ["System"]=["disk — Disk partitions, free space ratios, health status.","public-ip — Resolve external IPv4 via REST fallback chain.","kill-port <n> — Terminate the process listening on TCP port <n>.","ssh-info — Local IPs, Tailscale address, active SSH connections.",], ["Database"]=["db-tui <path> — Open SQLite file in interactive schema/data viewer."," Requires sqlite3 CLI on PATH.",], ["Accounts"]=["agyswitch — Switch the active AGY/Gemini account context.","agyquota — Show quota usage summary for all accounts.",], ["Scaffold"]=["scaffold — Interactive project boilerplate creator."," Templates: webapi · console · react (Vite) · blazorwasm · classlib · worker",],

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

    public static Dictionary<string, Dictionary<string, CommandDoc[]>>GetCommands(string jsonPath)
    {
        if (!File.Exists(jsonPath))return new();

        try
        {
            var raw=File.ReadAllText(jsonPath);
            var opts=new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive=true
            }
            ;
            return JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, CommandDoc[]>>>(raw, opts)??new();
        }
        catch
        {
            return new();
        }

    }

    public static CommandDoc?ShowInteractive(string jsonPath, string initialFilter)
    {
        var cmdsNested=GetCommands(jsonPath);
        var cmds=new Dictionary<string, CommandDoc[]>();
        foreach (var(_, subDict)in cmdsNested)foreach (var(sub, docs)in subDict)cmds[sub]=docs;
        var categories=cmds.Keys.OrderBy(k => k, StringComparer.Ordinal).ToArray();
        var allCommands=categories.SelectMany(c => cmds[c]).ToArray();
        var categoryLookup=new Dictionary<string, string>();
        foreach (var c in categories)categoryLookup[$"{c} ({cmds[c].Length} commands)"]=c;
        var commandLookup=new Dictionary<string, CommandDoc>();
        foreach (var c in allCommands)commandLookup[$"{c.Alias,-10} - {c.Desc}"]=c;
        string[]TopResolver(string filter)
        {
            if (string.IsNullOrWhiteSpace(filter))return categories.Select(c => $"{c} ({cmds[c].Length} commands)").ToArray();
            return allCommands.Where(c => c.Alias.Contains(filter, StringComparison.OrdinalIgnoreCase)||c.Desc.Contains(filter, StringComparison.OrdinalIgnoreCase)||c.Command.Contains(filter, StringComparison.OrdinalIgnoreCase)).Select(c => $"{c.Alias,-10} - {c.Desc}").ToArray();
        }
        var filter=initialFilter;
        while (true)
        {
            var selectedLabel=SpectreMenu.ShowDynamic("Select Help Category", TopResolver, 0, filter);
            filter="";
            if (selectedLabel==null)return null;
            if (commandLookup.TryGetValue(selectedLabel, out var cmdObj))return cmdObj;
            if (categoryLookup.TryGetValue(selectedLabel, out var catName))
            {
                var catCmds=cmds[catName];
                var subLookup=new Dictionary<string, CommandDoc>();
                foreach (var c in catCmds)subLookup[$"{c.Alias,-10} - {c.Desc}"]=c;
                string[]SubResolver(string subFilter) => catCmds.Where(c => string.IsNullOrWhiteSpace(subFilter)||c.Alias.Contains(subFilter, StringComparison.OrdinalIgnoreCase)||c.Desc.Contains(subFilter, StringComparison.OrdinalIgnoreCase)||c.Command.Contains(subFilter, StringComparison.OrdinalIgnoreCase)).Select(c => $"{c.Alias,-10} - {c.Desc}").ToArray();
                while (true)
                {
                    var selectedSubLabel=SpectreMenu.ShowDynamic($"Category: {catName}", SubResolver, 0);
                    if (selectedSubLabel==null)break;
                    if (subLookup.TryGetValue(selectedSubLabel, out var subCmd))return subCmd;
                }
            }
        }

    }

}

public sealed record CommandDoc(string Alias, string FullName, string Desc, string Command);

public static class AgyHeader
{
    public static void ShowSplash()
    {
        AnsiConsole.Clear();
        var splashW=Math.Min(65, Math.Max(50, Console.WindowWidth-2));
        var sep=new string('=', splashW);
        AnsiConsole.MarkupLine($"[cyan]{sep.EscapeMarkup()}[/]");
        AnsiConsole.Write(new FigletText("AGY TUI").Centered().Color(Color.Green));
        AnsiConsole.Write(new Rule("[bold green]🛸 Powershell Profile Control Center v3.0 🛸[/]").RuleStyle("grey"));
        AnsiConsole.MarkupLine($"[cyan]{sep.EscapeMarkup()}[/]");
        AnsiConsole.WriteLine();
        var active=AgyAccountCore.GetActiveAccount();
        var stats=AgyAccountCore.GetAccountStats(active);
        var quota=AgyAccountCore.CalculateRollingQuotas(active);
        var grid=new Grid();
        grid.AddColumn(new GridColumn().PadLeft(4));
        grid.AddRow($"[cyan]Active account[/] : [green bold]{active.EscapeMarkup()}[/]");
        grid.AddRow($"[cyan]Login status[/] : {(stats.TokenStatus == "Logged In" ? "[green]● Logged In[/]" : "[red]○ Not Logged In[/]")}");
        grid.AddRow($"[cyan]Weekly quota[/] : {AgyAccountCore.GetProgressBar(quota.RemainingWeekly).EscapeMarkup()}");
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
        AnsiConsole.MarkupLine("[dim] Press Enter to continue[/]");
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
        AnsiConsole.MarkupLine($"[dim] 5-Hour : {quota.Count5H,4} / 50 requests · Refreshes in {quota.Time5H}[/]");

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
            node.AddNode($"[dim]Login:[/] {(stats.TokenStatus == "Logged In" ? "[green]Logged In[/]" : "[red]Not Logged In[/]")}");
            node.AddNode($"[dim]Convos:[/] {stats.ConversationsCount} [dim]Skills:[/] {stats.SkillsCount}");
            node.AddNode($"[dim]Weekly:[/] {(int)Math.Round(stats.GeminiWeekly)}% [dim]5h:[/] {(int)Math.Round(stats.GeminiFiveHour)}%");
            node.AddNode($"[dim]Size:[/] {stats.PrivateSize} [dim]Junctions:[/] {stats.JunctionStatus.EscapeMarkup()}");
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
    public static string LearnRoot => System.IO.Path.Combine(AgyAccountCore.GetAccountDirectory(AgyAccountCore.GetActiveAccount()),"learn");

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

    public static string ObsidianCfgFile => System.IO.Path.Combine(AgyAccountCore.GetAccountDirectory(AgyAccountCore.GetActiveAccount()),"obsidian_config.json");

    public static string ResourcesIndex => System.IO.Path.Combine(AgyAccountCore.GetAccountDirectory(AgyAccountCore.GetActiveAccount()),"resources","index.json");

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
            AnsiConsole.MarkupLine($"[dim]Card {known + again + 1} / {queue.Count} · ✓ {known} known · ✗ {again} again[/]");
            AnsiConsole.WriteLine();
            AnsiConsole.Write(new Panel($"[bold]{card.Front.EscapeMarkup()}[/]"+(card.Hint!=null?$"\n[dim]{card.Hint.EscapeMarkup()}[/]":""))
            {
                Header=new PanelHeader("[dim]Front[/]"), Border=BoxBorder.Rounded, BorderStyle=new Style(Color.Cyan1), Padding=new Padding(1, 1)
            }
            );
            AnsiConsole.MarkupLine("[dim] Press Enter to reveal · Esc to exit[/]");
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
        SpectrePanel.Success($"Session complete — ✓ {known} known ✗ {again} missed ({queue.Count} cards reviewed)");

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
            AnsiConsole.MarkupLine(ok?$"[green]✓ Correct! {entry.Char} = {entry.Romaji}[/]":$"[red]✗ Wrong — {entry.Char} = {entry.Romaji} (you typed: {answer.EscapeMarkup()})[/]");
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
            var items=results.Select(k => $"{k.Char} {k.Meaning,-20} {k.JlptLevel,-3} {string.Join("、", k.Kunyomi)}").ToArray();
            var idx=SpectreMenu.Show($"Results for '{query}'", items, 0, false);
            if (idx>=0)ShowDetail(results[idx]);
        }

    }

    public static KanjiEntry[]Search(KanjiEntry[]all, string query) => all.Where(k => k.Meaning.Contains(query, StringComparison.OrdinalIgnoreCase)||k.Char.Contains(query)||k.Onyomi.Any(o => o.Contains(query, StringComparison.OrdinalIgnoreCase))||k.Kunyomi.Any(u => u.Contains(query, StringComparison.OrdinalIgnoreCase))).ToArray();

    public static void ShowDetail(KanjiEntry k)
    {
        var lines=new List<string>
        {
            $"Meaning : {k.Meaning}",$"On-yomi : {string.Join("、", k.Onyomi)}",$"Kun-yomi : {string.Join("、", k.Kunyomi)}",$"JLPT : {k.JlptLevel}",$"Strokes : {k.StrokeCount}",$"Radicals : {string.Join(" ", k.Radicals)}","","Example words", new string('─', 40)
        }
        ;
        foreach (var ex in k.ExampleWords)lines.Add($" {ex.Word} {ex.Reading,-10} {ex.Meaning}");
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
        var cards=data.Words.Where(w => SpacedRepetitionEngine.IsDueToday(w.Sr)).Select(w => new FlashCard(w.Id, w.Word,$"{w.Reading} {w.Meaning}", w.Romaji, null, w.ExampleJp+" / "+w.ExampleEn, w.Tags, 3, w.Sr)).ToArray();
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
        AnsiConsole.MarkupLine($"[dim]Step {step} · Comparisons: {comps} · Swaps: {swaps}[/]");
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
        if (lo>=0&&hi>=0&&lo<a.Length)AnsiConsole.MarkupLine($"[dim] comparing indices {lo}–{hi}[/]");
        AnsiConsole.MarkupLine("[dim] Enter next step · Esc exit[/]");

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
        AnsiConsole.MarkupLine("[dim] Press any key...[/]");
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
            var items=all.Select(q => $"{q.Question,-55} [dim]{q.Type}[/]").ToArray();
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
            $"[bold cyan]{q.Type.EscapeMarkup()} · {q.Category.EscapeMarkup()} · {q.Difficulty.EscapeMarkup()}[/]", new string('─', 50),"",$"[bold]{q.Question.EscapeMarkup()}[/]","",$"[dim]Format: {q.Format.EscapeMarkup()}[/]",
        }
        ;
        if (q.Hints.Length>0)
        {
            lines.Add("");
            lines.Add("[cyan]Hints:[/]");
            foreach (var h in q.Hints)lines.Add($" • {h.EscapeMarkup()}");
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
                    AnsiConsole.MarkupLine($"[cyan]{'█'.ToString().PadRight(bars, '█').PadRight(40, '░')}[/] {pct:F0}%");
                    AnsiConsole.MarkupLine("[dim] Esc stop early · Enter mark done & next[/]");
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
            AnsiConsole.MarkupLine($"[dim]Word {total + 1} / {due.Length} · Weak queue: {due.Length - total}[/]");
            AnsiConsole.WriteLine();
            AnsiConsole.Write(new Panel($"[bold]{word.Word.EscapeMarkup()}[/]\n[dim]{word.Pronunciation.EscapeMarkup()}[/]")
            {
                Header=new PanelHeader("[cyan]ℹ[/]"), Border=BoxBorder.Rounded, BorderStyle=new Style(Color.Cyan1), Padding=new Padding(1, 1)
            }
            );
            AnsiConsole.MarkupLine("[dim] Press Enter to reveal definition[/]");
            if (Console.ReadKey(true).Key==ConsoleKey.Escape)break;
            AnsiConsole.Clear();
            AnsiConsole.Write(new Rule($"[bold cyan]{difficulty} Vocab[/]").RuleStyle("grey"));
            var detail=$"[bold]{word.Word.EscapeMarkup()}[/] [dim]{word.PartOfSpeech.EscapeMarkup()}[/]\n\n"+$"{word.Definition.EscapeMarkup()}\n\n"+$"[italic dim]\"{word.ExampleSentence.EscapeMarkup()}\"[/]";
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
            AnsiConsole.MarkupLine($"[{(barColor == Color.Green ? "green" : "yellow")}]{'█'.ToString().PadRight(bars, '█').PadRight(40, '░')}[/] {pct:F0}%");
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
        if (byTopic.Count == 0)
        {
            AnsiConsole.MarkupLine("[dim]  No study data recorded in the last 7 days.[/]");
            return;
        }
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
            AnsiConsole.MarkupLine("[dim] No goals set today. Press n to add.[/]");
        }
        else
        {
            var sb=new StringBuilder();
            foreach (var t in data.Targets)
            {
                bool done=t.Completed>=t.Count;
                int bars=t.Count>0?(int)(t.Completed*16.0/t.Count):0;
                var bar=new string('█', Math.Min(16, bars))+new string('░', Math.Max(0, 16-bars));
                sb.AppendLine($" {(done ? "[green]✓[/]" : "[red]✗[/]")} {t.Topic,-12} {t.Activity,-12} [{bar}] {t.Completed}/{t.Count}");
            }
            int complete=data.Targets.Count(t => t.Completed>=t.Count);
            AnsiConsole.Write(new Panel(sb.ToString().TrimEnd())
            {
                Border=BoxBorder.Rounded, BorderStyle=new Style(Color.Cyan1), Padding=new Padding(1, 0)
            }
            );
            AnsiConsole.MarkupLine($"[dim] {complete} / {data.Targets.Length} goals complete[/]");
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
        AnsiConsole.Write(new Panel($"🔥 Current streak : [bold yellow]{s.Current} days[/]\n"+$"🏆 Best streak : [bold green]{s.Best} days[/]\n"+$"📅 Last active : [cyan]{s.LastActive}[/]\n"+$"📊 This week : [dim]{s.DaysThisWeek} / 7 days active[/]")
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
        AnsiConsole.MarkupLine($"\n[dim]Total: {groups.Sum(g => g.Count())} items due · {groups.Sum(g => g.Count(d => d.Overdue))} overdue[/]");

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
        AnsiConsole.MarkupLine("[dim] Press any key...[/]");
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
        AnsiConsole.Write(new Panel($"You have [yellow]{items.Length}[/] weak items from your last session:\n\n"+string.Join("\n", items.Take(5).Select((w, i) => $" {i + 1}. {w.FrontText.EscapeMarkup()} [dim]({w.Topic} — failed {w.FailCount}x)[/]"))+(items.Length>5?$"\n [dim]... and {items.Length - 5} more[/]":"")+"\n\n[dim]These will be shown first in your session.[/]")
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
            var items=matches.Select(m => $"{m.Title,-40} [dim]{m.RelativePath}[/]").ToArray();
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
        AnsiConsole.Write(new Rule($"[bold cyan]Obsidian Graph — root: {rootTitle.EscapeMarkup()} depth: {depth}[/]").RuleStyle("grey"));
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
        AnsiConsole.MarkupLine("[dim] Auto-refreshes · Press any key to exit[/]");
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
            var n=tree.AddNode($"[bold]{node.Name.EscapeMarkup()}[/] [dim]({node.Kind})[/]");
            foreach (var dep in node.DependsOn)n.AddNode($"→ {dep.EscapeMarkup()}");
            if (node.DependsOn.Length==0)n.AddNode("[dim](no dependencies)[/]");
        }
        AnsiConsole.Write(tree);
        AnsiConsole.MarkupLine("[dim] Press any key...[/]");
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
        AnsiConsole.MarkupLine("[dim] Press any key...[/]");
        Console.ReadKey(true);

    }

    public static void ShowCommitBarChart(Dictionary<string, int>commitsByRepo)
    {
        AnsiConsole.Write(new Rule("[bold cyan]Commits this week by repo[/]").RuleStyle("grey"));
        if (commitsByRepo.Count == 0)
        {
            AnsiConsole.MarkupLine("[dim]  No commit data recorded this week.[/]");
            return;
        }
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
        AnsiConsole.MarkupLine($"\n [bold green]{word.Word.EscapeMarkup()}[/] [dim]{word.Pronunciation.EscapeMarkup()}[/] [yellow]{word.PartOfSpeech.EscapeMarkup()}[/]");
        AnsiConsole.MarkupLine($" {word.Definition.EscapeMarkup()}");
        AnsiConsole.MarkupLine($" [italic dim]\"{word.Example.EscapeMarkup()}\"[/]");
        AnsiConsole.WriteLine();

    }

}
public static class CcBanner
{
    public static void Print()
    {
        var acc=AgyAccountCore.GetActiveAccount();
        var displayAcc = acc;
        if (string.Equals(acc, "default", StringComparison.OrdinalIgnoreCase))
        {
            var email = AgyAccountCore.GetAccountEmail("default");
            if (!string.IsNullOrEmpty(email)) displayAcc = $"default ({email})";
        }
        var now=DateTime.Now;
        var w=Math.Min(65, Math.Max(50, Console.WindowWidth-2));
        var sep=new string('=', w);
        AnsiConsole.Clear();
        AnsiConsole.MarkupLine($"[cyan]{sep.EscapeMarkup()}[/]");
        AnsiConsole.MarkupLine("[cyan] ▄████▄ ▄████▄[/] [bold green]🛸 Powershell Profile Control Center v3.0 🛸[/]");
        AnsiConsole.MarkupLine("[cyan] █▀ ▀ █▀ ▀[/] [dim]System dashboard and control suite.[/]");
        AnsiConsole.MarkupLine("[cyan] █ █[/]");
        AnsiConsole.MarkupLine($"[cyan] █▄ ▄ █▄ ▄[/] [dim]Active Account:[/] [green bold]{displayAcc.EscapeMarkup()}[/]");
        AnsiConsole.MarkupLine($"[cyan] ▀████▀ ▀████▀[/] [dim]Time:[/] [yellow]{now:yyyy-MM-dd HH:mm}[/]");
        AnsiConsole.MarkupLine($"[cyan]{sep.EscapeMarkup()}[/]");
        AnsiConsole.MarkupLine("[dim] [[Tab/→]] Navigate Panes | [[←/Esc]] Go Back | [[Enter]] Select & Run[/]");
        AnsiConsole.MarkupLine($"[cyan]{sep.EscapeMarkup()}[/]");
        AnsiConsole.WriteLine();

    }

}
public static class CcNavigator
{
    private sealed record Section(string Label, (string Display, string Alias)[]Items, string Desc);

    private static readonly Section[]AllSections=[
        new("[Workspace & Dev]", [
            ("Navigate Workspace","proj"),
            ("Terminal IDE","ide"),
            ("Diff Viewer","ide-diff"),
            ("Search Across Files","ide-search"),
            ("[.NET] Build Project","dbld"),
            ("[.NET] Test Project","dtst"),
            ("[.NET] Clean Build Artifacts","clean-build"),
            ("[.NET] Add EF Migration","add-migration"),
            ("[.NET] Update EF Database","update-db"),
            ("Scaffold New Project","scaffold"),
            ("Git Status","gs"),
            ("Conventional Commit","gcmt"),
            ("Git Undo Last Commit","git-undo"),
            ("Repo Nexus Graph","nexus")
        ],"Project navigation, terminal IDE, build/test tools, and git."),
        
        new("[AI Agent & Ollama]", [
            ("Claude Code (Cloud)","claude-cloud"),
            ("Claude Code (Ollama)","claude-ollama"),
            ("Codex (Cloud)","codex-cloud"),
            ("Codex (Ollama)","codex-ollama"),
            ("OpenClaw (Ollama)","openclaw"),
            ("Hermes3 (Ollama)","hermes"),
            ("Ollama: Check Daemon Status","ollama-status"),
            ("Ollama: Manage Models","ollama-models"),
            ("Ollama: Pull New Model","ollama-pull"),
            ("Ollama: Start Daemon","ollama-start"),
            ("Ollama: View Server Logs","ollama-logs"),
            ("Antigravity Deck: Setup/Initialize","deck-setup"),
            ("Antigravity Deck: Start Local","deck-start"),
            ("Antigravity Deck: Go Online (Tunnel)","deck-online"),
            ("Launch Antigravity CLI (agy)","agy-cli")
        ],"Launch AI coding agents, local/cloud models, Antigravity Deck, and agy CLI."),
        
        new("[AGY Account Switch]", [
            ("Select Active Account","agyswitch"),
            ("View All Accounts","agyquota"),
            ("Account Tree","account-tree"),
            ("Quota Bar Chart","quota-chart"),
            ("Live Dashboard","live-dashboard"),
            ("Toggle Auto-Switch","autoswitch")
        ],"AGY account switch, tree, rolling quotas, and live status."),
        
        new("[Docker & Databases]", [
            ("Docker Cleanup","dkcl"),
            ("Docker Compose Up","dcup"),
            ("Docker Compose Down","dcdown"),
            ("LocalStack Info","aws-local"),
            ("SQLite Browser","db-tui")
        ],"Manage docker containers, LocalStack cloud sandbox, and SQLite."),
        
        new("[System & Network]", [
            ("Disk Usage","disk"),
            ("Public IP Address","public-ip"),
            ("SSH Connection Info","ssh-info"),
            ("Kill Port","kill-port")
        ],"System health, external IP, SSH sessions, and port mapping."),
        
        new("[Learn & Study]", [
            ("Start Learning (auto)","learn"),
            ("Flashcard Deck Browser","flashcard"),
            ("English Vocab Drill","vocab"),
            ("Kana Quiz","kana"),
            ("Kanji Lookup","kanji"),
            ("JLPT Vocab Drill","jlpt"),
            ("Algorithm Visualizer","algo"),
            ("Big-O Complexity Sheet","complexity"),
            ("DSA Problem Tracker","problems"),
            ("Code Snippet Library","snippets"),
            ("Cheat Sheet Browser","sheets"),
            ("C# Quiz","quiz"),
            ("Interview Question Bank","interview"),
            ("STAR Answer Builder","star"),
            ("Mock Interview Timer","mock"),
            ("Word of the Day","word-of-day")
        ],"Coding quiz, flashcards, language drills, algorithms, and interview prep."),
        
        new("[Track & Progress]", [
            ("Start Pomodoro Session","session"),
            ("Study Statistics","stats"),
            ("Daily Goals","goals"),
            ("Study Streak","streak"),
            ("Due Reviews","due"),
            ("Progress Dashboard","progress"),
            ("Weak Items Queue","weak")
        ],"Streak tracker, daily goals, Pomodoro focus session, and progress stats."),
        
        new("[Obsidian & Resources]", [
            ("Obsidian Vault Config","obsidian"),
            ("Obsidian Graph View","obs-graph"),
            ("Refresh Learning Data","refresh"),
            ("Add Resource","add-resource")
        ],"Obsidian markdown vault integration, wikilink graph, and registries."),
        
        new("[Theme & Settings]", [
            ("Command Palette","cc"),
            ("Help Browser","help"),
            ("Select Shell Theme","theme")
        ],"Profile themes, Command Palette helper, and browser documentation."),
        
        new("────────────────────────────", [],""),
        new("[Exit] Exit Control Center", [],"Exit the Powershell Profile Control Center.")
    ];

    private static Section[] GetActiveSections()
    {
        var enableAi = AgyAiCore.IsAiOllamaEnabled();
        var enableAgy = AgyAiCore.IsAgyEnabled();

        var list = new List<Section>();
        foreach (var s in AllSections)
        {
            if (s.Label.Contains("AI Agent & Ollama") && !enableAi) continue;
            if (s.Label.Contains("AGY Account Switch") && !enableAgy) continue;

            list.Add(s);
        }
        return list.ToArray();
    }

    public static void Run()
    {
        try
        {
            var leftSel=0;
            var midSel=0;
            var midActive=false;
            var detailsActive=false;
            var detailsMode="";
            var detailsSel=0;
            try { Console.CursorVisible=false; } catch {}
            while (true)
            {
                var sections = GetActiveSections();
                if (leftSel >= sections.Length) leftSel = Math.Max(0, sections.Length - 1);

                CcBanner.Print();
                RenderPanes(sections, leftSel, midSel, midActive, detailsActive, detailsSel, detailsMode);
                var key=Console.ReadKey(true);
                var section=sections[leftSel];

                if (detailsActive)
                {
                    int itemsCount = 0;
                    if (detailsMode == "agyswitch")
                    {
                        itemsCount = AgyAccountCore.GetAccounts().Length;
                    }
                    else if (detailsMode == "theme")
                    {
                        itemsCount = GetThemeNames().Length;
                    }
                    else if (detailsMode == "learn" || detailsMode == "session" || detailsMode == "weak")
                    {
                        itemsCount = 6;
                    }
                    else if (detailsMode == "proj")
                    {
                        itemsCount = WorkspaceRegistry.GetWorkspaces().Length;
                    }

                    if (itemsCount == 0)
                    {
                        detailsActive = false;
                        continue;
                    }

                    switch (key.Key)
                    {
                        case ConsoleKey.UpArrow:
                        case ConsoleKey.K:
                            detailsSel = (detailsSel - 1 + itemsCount) % itemsCount;
                            break;
                        case ConsoleKey.DownArrow:
                        case ConsoleKey.J:
                            detailsSel = (detailsSel + 1) % itemsCount;
                            break;
                        case ConsoleKey.Enter:
                            if (detailsSel >= 0 && detailsSel < itemsCount)
                            {
                                if (detailsMode == "agyswitch")
                                {
                                    var accs = AgyAccountCore.GetAccounts();
                                    var targetAcc = accs[detailsSel];
                                    Console.CursorVisible = true;
                                    AgyAccountCore.SetActiveAccount(targetAcc, false);
                                    Console.CursorVisible = false;
                                }
                                else if (detailsMode == "theme")
                                {
                                    var themeNames = GetThemeNames();
                                    var selectedTheme = themeNames[detailsSel];
                                    var themesPath = Environment.GetEnvironmentVariable("POSH_THEMES_PATH");
                                    if (string.IsNullOrEmpty(themesPath))
                                    {
                                        themesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "asset", "powershell-themes");
                                        if (!Directory.Exists(themesPath))
                                        {
                                            themesPath = Path.Combine(Directory.GetCurrentDirectory(), "asset", "powershell-themes");
                                        }
                                    }
                                    var configPath = Path.Combine(themesPath, "config.json");
                                    try
                                    {
                                        File.WriteAllText(configPath, JsonSerializer.Serialize(new { active_theme = selectedTheme, enable_mobile = selectedTheme.EndsWith("-mobile") }));
                                    }
                                    catch {}
                                    Environment.SetEnvironmentVariable("THEME", selectedTheme);
                                    var themePath = Path.Combine(themesPath, $"{selectedTheme}.omp.json");
                                    var selectedThemeFile = Path.Combine(AgyAccountCore.AgySourceHome, "selected_theme.txt");
                                    File.WriteAllText(selectedThemeFile, themePath);
                                }
                                else if (detailsMode == "learn" || detailsMode == "session" || detailsMode == "weak")
                                {
                                    var topics = new[] { "jp", "en", "cs", "dsa", "interview", "[Type Custom Topic...]" };
                                    var selectedTopic = topics[detailsSel];
                                    if (selectedTopic == "[Type Custom Topic...]")
                                    {
                                        Console.CursorVisible = true;
                                        selectedTopic = AnsiConsole.Ask<string>("Enter custom topic name:").Trim();
                                        Console.CursorVisible = false;
                                    }
                                    if (!string.IsNullOrEmpty(selectedTopic))
                                    {
                                        Console.CursorVisible = true;
                                        if (detailsMode == "learn") LearnRouter.StartLearning(selectedTopic);
                                        else if (detailsMode == "session") StudySession.Run(selectedTopic);
                                        else if (detailsMode == "weak") WeakItemsQueue.ShowPreSessionReview(selectedTopic);
                                        Console.CursorVisible = false;
                                    }
                                }
                                else if (detailsMode == "proj")
                                {
                                    var workspaces = WorkspaceRegistry.GetWorkspaces();
                                    var selectedProj = workspaces[detailsSel].WorkspacePath;
                                    var selectedProjFile = Path.Combine(AgyAccountCore.AgySourceHome, "selected_project.txt");
                                    File.WriteAllText(selectedProjFile, selectedProj);
                                    AnsiConsole.MarkupLine($"[green][[Workspace]] Selected workspace '{workspaces[detailsSel].Name}'. Switch will apply on exit.[/]");
                                    Thread.Sleep(1000);
                                }
                                detailsActive = false;
                            }
                            break;
                        case ConsoleKey.A:
                            if (detailsMode == "agyswitch")
                            {
                                var accs = AgyAccountCore.GetAccounts();
                                Console.CursorVisible = true;
                                AnsiConsole.Clear();
                                var newName = AnsiConsole.Ask<string>("Enter new account name:").Trim();
                                if (!string.IsNullOrEmpty(newName))
                                {
                                    try
                                    {
                                        AgyAccountCore.AddAccount(newName);
                                        SpectrePanel.Success($"Account '{newName}' created successfully!");
                                        Thread.Sleep(1500);
                                    }
                                    catch (Exception ex)
                                    {
                                        SpectrePanel.Error($"Failed to create account: {ex.Message}");
                                        Thread.Sleep(2000);
                                    }
                                }
                                Console.CursorVisible = false;
                                detailsSel = 0;
                            }
                            break;
                        case ConsoleKey.D:
                            if (detailsMode == "agyswitch")
                            {
                                var accs = AgyAccountCore.GetAccounts();
                                if (detailsSel >= 0 && detailsSel < accs.Length)
                                {
                                    var targetAcc = accs[detailsSel];
                                    var activeAcc = AgyAccountCore.GetActiveAccount();
                                    if (string.Equals(targetAcc, activeAcc, StringComparison.OrdinalIgnoreCase))
                                    {
                                        Console.CursorVisible = true;
                                        SpectrePanel.Error($"Cannot delete '{targetAcc}' because it is the current active account.");
                                        Thread.Sleep(1500);
                                        Console.CursorVisible = false;
                                        break;
                                    }
                                    Console.CursorVisible = true;
                                    AnsiConsole.Clear();
                                    var confirm = AnsiConsole.Confirm($"Are you sure you want to delete account '{targetAcc}'?");
                                    if (confirm)
                                    {
                                        try
                                        {
                                            AgyAccountCore.DeleteAccount(targetAcc);
                                            SpectrePanel.Success($"Account '{targetAcc}' deleted successfully!");
                                            Thread.Sleep(1500);
                                        }
                                        catch (Exception ex)
                                        {
                                            SpectrePanel.Error($"Failed to delete account: {ex.Message}");
                                            Thread.Sleep(2000);
                                        }
                                    }
                                    Console.CursorVisible = false;
                                    detailsSel = 0;
                                }
                            }
                            break;
                        case ConsoleKey.O:
                            if (detailsMode == "agyswitch")
                            {
                                var accs = AgyAccountCore.GetAccounts();
                                if (detailsSel >= 0 && detailsSel < accs.Length)
                                {
                                    var targetAcc = accs[detailsSel];
                                    Console.CursorVisible = true;
                                    AnsiConsole.Clear();
                                    var confirm = AnsiConsole.Confirm($"Are you sure you want to log out of '{targetAcc}'?");
                                    if (confirm)
                                    {
                                        AgyAccountCore.LogoutAccount(targetAcc);
                                        SpectrePanel.Success($"Logged out of '{targetAcc}' successfully!");
                                        Thread.Sleep(1500);
                                    }
                                    Console.CursorVisible = false;
                                }
                            }
                            break;
                        case ConsoleKey.LeftArrow:
                        case ConsoleKey.Escape:
                        case ConsoleKey.Q:
                            detailsActive = false;
                            break;
                    }
                    continue;
                }

                if (!midActive)
                {
                    switch (key.Key)
                    {
                        case ConsoleKey.UpArrow:
                        {
                            var next=leftSel;

                            do
                            {
                                next=Math.Max(0, next-1);
                            }
                            while (next>0&&IsSep(sections, next));
                            if (!IsSep(sections, next))
                            {
                                leftSel=next;
                                midSel=0;
                            }
                            break;
                        }
                        case ConsoleKey.DownArrow:
                        {
                            var next=leftSel;

                            do
                            {
                                next=Math.Min(sections.Length-1, next+1);
                            }
                            while (next<sections.Length-1&&IsSep(sections, next));
                            if (!IsSep(sections, next))
                            {
                                leftSel=next;
                                midSel=0;
                            }
                            break;
                        }
                        case ConsoleKey.Enter:case ConsoleKey.RightArrow:case ConsoleKey.Tab:if (section.Label.Contains("Exit"))return;
                        if (section.Items.Length>0)midActive=true;
                        break;
                        case ConsoleKey.Escape:case ConsoleKey.Q when key.Modifiers==0:return;
                    }
                }
                else
                {
                    switch (key.Key)
                    {
                        case ConsoleKey.UpArrow:midSel=Math.Max(0, midSel-1);
                        break;
                        case ConsoleKey.DownArrow:midSel=Math.Min(section.Items.Length-1, midSel+1);
                        break;
                        case ConsoleKey.Enter:
                        case ConsoleKey.RightArrow:
                        case ConsoleKey.Tab:
                        if (midSel<section.Items.Length)
                        {
                            var alias = section.Items[midSel].Alias;
                            if (string.Equals(alias, "agyswitch", StringComparison.OrdinalIgnoreCase))
                            {
                                detailsActive = true;
                                detailsMode = "agyswitch";
                                var accs = AgyAccountCore.GetAccounts();
                                var activeAcc = AgyAccountCore.GetActiveAccount();
                                detailsSel = Array.IndexOf(accs, activeAcc);
                                if (detailsSel < 0) detailsSel = 0;
                            }
                            else if (string.Equals(alias, "theme", StringComparison.OrdinalIgnoreCase))
                            {
                                detailsActive = true;
                                detailsMode = "theme";
                                var themeFiles = GetThemeNames();
                                var currentTheme = Environment.GetEnvironmentVariable("THEME");
                                detailsSel = Array.IndexOf(themeFiles, currentTheme);
                                if (detailsSel < 0) detailsSel = 0;
                            }
                            else if (string.Equals(alias, "learn", StringComparison.OrdinalIgnoreCase) ||
                                     string.Equals(alias, "session", StringComparison.OrdinalIgnoreCase) ||
                                     string.Equals(alias, "weak", StringComparison.OrdinalIgnoreCase) ||
                                     string.Equals(alias, "proj", StringComparison.OrdinalIgnoreCase))
                            {
                                detailsActive = true;
                                detailsMode = alias.ToLowerInvariant();
                                detailsSel = 0;
                            }
                            else if (string.Equals(alias, "account-tree", StringComparison.OrdinalIgnoreCase) ||
                                     string.Equals(alias, "quota-chart", StringComparison.OrdinalIgnoreCase) ||
                                     string.Equals(alias, "live-dashboard", StringComparison.OrdinalIgnoreCase) ||
                                     string.Equals(alias, "disk", StringComparison.OrdinalIgnoreCase) ||
                                     string.Equals(alias, "public-ip", StringComparison.OrdinalIgnoreCase) ||
                                     string.Equals(alias, "ssh-info", StringComparison.OrdinalIgnoreCase) ||
                                     string.Equals(alias, "proj", StringComparison.OrdinalIgnoreCase))
                            {
                                // Rendered directly in details, no execution needed on Enter/Right/Tab
                            }
                            else
                            {
                                Console.CursorVisible=true;
                                Program.RunCommand(alias);
                                AnsiConsole.WriteLine();
                                AnsiConsole.MarkupLine("[dim]Press any key to return to Control Center...[/]");
                                Console.ReadKey(true);
                                Console.CursorVisible=false;
                            }
                        }
                        break;
                        case ConsoleKey.LeftArrow:case ConsoleKey.Escape:midActive=false;
                        break;
                        case ConsoleKey.Q when key.Modifiers==0:return;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            try
            {
                File.WriteAllText(@"C:\Users\TruongNhon\Documents\Powershell\tui_error.txt", ex.ToString());
            }
            catch {}
            throw;
        }
        finally
        {
            Console.CursorVisible=true;
        }

    }

    private static bool IsSep(Section[] sections, int idx) => sections[idx].Label.Length>0&&sections[idx].Label[0]=='─';

    private static string[] GetThemeNames()
    {
        var themesPath = Environment.GetEnvironmentVariable("POSH_THEMES_PATH");
        if (string.IsNullOrEmpty(themesPath) || !Directory.Exists(themesPath))
        {
            themesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "asset", "powershell-themes");
            if (!Directory.Exists(themesPath))
            {
                themesPath = Path.Combine(Directory.GetCurrentDirectory(), "asset", "powershell-themes");
            }
        }
        if (!Directory.Exists(themesPath)) return Array.Empty<string>();
        return Directory.GetFiles(themesPath, "*.omp.json").Select(f => Path.GetFileName(f).Replace(".omp.json", "")).OrderBy(f => f).ToArray();
    }

    private static Spectre.Console.Rendering.IRenderable GetDiskSpaceWidget()
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

    private static string _cachedIp = null;
    private static DateTime _lastIpFetch = DateTime.MinValue;
    private static Spectre.Console.Rendering.IRenderable GetPublicIpWidget()
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

    private static Spectre.Console.Rendering.IRenderable GetSshInfoWidget()
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

    private static Spectre.Console.Rendering.IRenderable GetAccountTreeWidget()
    {
        var accounts=AgyAccountCore.GetAccounts();
        var active=AgyAccountCore.GetActiveAccount();
        var tree=new Tree("[bold cyan]Account Tree[/]");
        foreach (var acc in accounts)
        {
            var stats=AgyAccountCore.GetAccountStats(acc);
            var displayName = acc;
            if (string.Equals(acc, "default", StringComparison.OrdinalIgnoreCase))
            {
                var email = AgyAccountCore.GetAccountEmail("default");
                if (!string.IsNullOrEmpty(email)) displayName = $"default ({email})";
            }
            var label=acc==active?$"[green bold]★ {displayName.EscapeMarkup()} (Active)[/]":displayName.EscapeMarkup();
            var node=tree.AddNode(label);
            node.AddNode($"[dim]Login:[/] {(stats.TokenStatus == "Logged In" ? "[green]Logged In[/]" : "[red]Not Logged In[/]")}");
            node.AddNode($"[dim]Convos:[/] {stats.ConversationsCount} [dim]Skills:[/] {stats.SkillsCount}");
            node.AddNode($"[dim]Weekly:[/] {(int)Math.Round(stats.GeminiWeekly)}% [dim]5h:[/] {(int)Math.Round(stats.GeminiFiveHour)}%");
            node.AddNode($"[dim]Size:[/] {stats.PrivateSize}");
        }
        return tree;
    }

    private static Spectre.Console.Rendering.IRenderable GetQuotaChartWidget(string accountName)
    {
        var quota=AgyAccountCore.CalculateRollingQuotas(accountName);
        var chartLabel = accountName;
        if (string.Equals(accountName, "default", StringComparison.OrdinalIgnoreCase))
        {
            var email = AgyAccountCore.GetAccountEmail("default");
            if (!string.IsNullOrEmpty(email)) chartLabel = $"default ({email})";
        }
        var chart=new BarChart().Width(28).Label($"[bold cyan]{chartLabel.EscapeMarkup()} Quota Remaining %[/]").CenterLabel()
            .AddItem("Gemini W", quota.RemainingWeekly, Color.Cyan1)
            .AddItem("Gemini 5H", quota.Remaining5H, Color.Yellow)
            .AddItem("Claude W", 100.0, Color.Green)
            .AddItem("Claude 5H", 100.0, Color.Blue);

        var lines = new List<Spectre.Console.Rendering.IRenderable>
        {
            chart,
            new Markup("\n"),
            new Markup($"[dim]Weekly: {quota.CountWeekly,4}/1000 reqs[/]"),
            new Markup($"[dim]5-Hour: {quota.Count5H,4}/50 reqs[/]"),
            new Markup($"[dim]Refreshes in {quota.TimeWeekly}[/]")
        };
        return new Rows(lines);
    }

    private static Spectre.Console.Rendering.IRenderable GetLiveDashboardWidget()
    {
        var table = new Table().Border(TableBorder.Rounded).BorderColor(Color.Grey);
        table.AddColumn("[bold cyan]Account[/]");
        table.AddColumn("[bold cyan]L[/]"); 
        table.AddColumn("[bold cyan]W[/]"); 
        table.AddColumn("[bold cyan]5h[/]"); 
        
        foreach (var a in AgyAccountCore.GetAccounts())
        {
            var s=AgyAccountCore.GetAccountStats(a);
            var act=AgyAccountCore.GetActiveAccount();
            var displayName = a;
            if (string.Equals(a, "default", StringComparison.OrdinalIgnoreCase))
            {
                var email = AgyAccountCore.GetAccountEmail("default");
                if (!string.IsNullOrEmpty(email)) displayName = $"default ({email})";
            }
            var n=a==act?$"[green bold]* {displayName.EscapeMarkup()}[/]":displayName.EscapeMarkup();
            var st=s.TokenStatus=="Logged In"?"[green]●[/]":"[red]○[/]";
            table.AddRow(n, st, $"{(int)Math.Round(s.GeminiWeekly)}%", $"{(int)Math.Round(s.GeminiFiveHour)}%");
        }
        return table;
    }

    public static Table? _cachedOllamaWidget = null;
    public static DateTime _ollamaWidgetCachedAt = DateTime.MinValue;

    private static Spectre.Console.Rendering.IRenderable GetOllamaStatusWidget()
    {
        if (_cachedOllamaWidget != null && (DateTime.UtcNow - _ollamaWidgetCachedAt).TotalSeconds < 3)
        {
            return _cachedOllamaWidget;
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
        _cachedOllamaWidget = table;
        _ollamaWidgetCachedAt = DateTime.UtcNow;
        return table;
    }

    private static void RenderPanes(Section[] sections, int leftSel, int midSel, bool midActive, bool detailsActive, int detailsSel, string detailsMode)
    {
        var leftSb=new StringBuilder();
        for (var i=0;
        i<sections.Length;
        i++)
        {
            var s=sections[i];
            if (s.Label.Length>0&&s.Label[0]=='─')
            {
                leftSb.AppendLine("[dim]────────────────────────────[/]");
                continue;
            }
            if (i==leftSel)leftSb.AppendLine(midActive?$"[cyan bold]> {s.Label.EscapeMarkup()}[/]":$"[green bold]> {s.Label.EscapeMarkup()}[/]");

            else leftSb.AppendLine($" {s.Label.EscapeMarkup()}");
        }
        var section=sections[leftSel];
        var midSb=new StringBuilder();
        for (var i=0;
        i<section.Items.Length;
        i++)
        {
            var(display, _)=section.Items[i];
            midSb.AppendLine(midActive&&i==midSel?$"[green bold]> {display.EscapeMarkup()}[/]":$" {display.EscapeMarkup()}");
        }
        if (section.Items.Length==0)midSb.AppendLine("[dim] (press Enter to select)[/]");
        
        var sectionTitle=section.Label.TrimStart('>',' ');
        Spectre.Console.Rendering.IRenderable detailsContent;

        if (midActive&&midSel<section.Items.Length)
        {
            var(display, alias)=section.Items[midSel];
            
            if (string.Equals(alias, "account-tree", StringComparison.OrdinalIgnoreCase))
            {
                detailsContent = GetAccountTreeWidget();
            }
            else if (string.Equals(alias, "quota-chart", StringComparison.OrdinalIgnoreCase))
            {
                detailsContent = GetQuotaChartWidget(AgyAccountCore.GetActiveAccount());
            }
            else if (string.Equals(alias, "live-dashboard", StringComparison.OrdinalIgnoreCase))
            {
                detailsContent = GetLiveDashboardWidget();
            }
            else if (string.Equals(alias, "disk", StringComparison.OrdinalIgnoreCase))
            {
                detailsContent = GetDiskSpaceWidget();
            }
            else if (string.Equals(alias, "public-ip", StringComparison.OrdinalIgnoreCase))
            {
                detailsContent = GetPublicIpWidget();
            }
            else if (string.Equals(alias, "ssh-info", StringComparison.OrdinalIgnoreCase))
            {
                detailsContent = GetSshInfoWidget();
            }
            else if (string.Equals(alias, "agyswitch", StringComparison.OrdinalIgnoreCase) && detailsActive && detailsMode == "agyswitch")
            {
                var rightSb = new StringBuilder();
                rightSb.AppendLine($"[bold white]{display.EscapeMarkup()}[/]");
                rightSb.AppendLine($"[dim]alias:[/] [yellow]{alias.EscapeMarkup()}[/]");
                rightSb.AppendLine();
                rightSb.AppendLine("[cyan bold]Select Account to Switch:[/]");
                rightSb.AppendLine();
                var accs = AgyAccountCore.GetAccounts();
                var activeAcc = AgyAccountCore.GetActiveAccount();
                for (var i = 0; i < accs.Length; i++)
                {
                    var isSelected = (i == detailsSel);
                    var isActive = (accs[i] == activeAcc);
                    var prefix = isSelected ? "[green bold]> [/]" : "  ";
                    var suffix = isActive ? " [green](Active)[/]" : "";
                    var displayName = accs[i];
                    if (string.Equals(accs[i], "default", StringComparison.OrdinalIgnoreCase))
                    {
                        var email = AgyAccountCore.GetAccountEmail("default");
                        if (!string.IsNullOrEmpty(email)) displayName = $"default ({email})";
                    }
                    var stats = AgyAccountCore.GetAccountStats(accs[i]);
                    var loginStatus = stats.TokenStatus == "Logged In" ? "[green]✔[/]" : "[red]✘[/]";
                    rightSb.AppendLine($"{prefix}{displayName.EscapeMarkup()} [dim]({loginStatus})[/]{suffix}");
                }
                rightSb.AppendLine();
                rightSb.AppendLine("[dim]↑/↓ Navigate  ·  Enter Select  ·  Esc Cancel[/]");
                rightSb.AppendLine("[dim]a Create Account  ·  d Delete  ·  o Log Out[/]");
                detailsContent = new Markup(rightSb.ToString());
            }
            else if (string.Equals(alias, "theme", StringComparison.OrdinalIgnoreCase) && detailsActive && detailsMode == "theme")
            {
                var rightSb = new StringBuilder();
                rightSb.AppendLine($"[bold white]{display.EscapeMarkup()}[/]");
                rightSb.AppendLine($"[dim]alias:[/] [yellow]{alias.EscapeMarkup()}[/]");
                rightSb.AppendLine();
                rightSb.AppendLine("[cyan bold]Select Oh My Posh Theme (Color segment preview):[/]");
                rightSb.AppendLine();
                var themeNames = GetThemeNames();
                var currentTheme = Environment.GetEnvironmentVariable("THEME");
                for (var i = 0; i < themeNames.Length; i++)
                {
                    var isSelected = (i == detailsSel);
                    var isActive = (themeNames[i] == currentTheme);
                    var prefix = isSelected ? "[green bold]> [/]" : "  ";
                    var suffix = isActive ? " [green](Active)[/]" : "";
                    rightSb.AppendLine($"{prefix}{themeNames[i].EscapeMarkup()}{suffix}");
                }
                rightSb.AppendLine();
                rightSb.AppendLine("[dim]↑/↓ Navigate  ·  Enter Select  ·  Esc Cancel[/]");
                detailsContent = new Markup(rightSb.ToString());
            }
            else if ((string.Equals(alias, "learn", StringComparison.OrdinalIgnoreCase) ||
                      string.Equals(alias, "session", StringComparison.OrdinalIgnoreCase) ||
                      string.Equals(alias, "weak", StringComparison.OrdinalIgnoreCase)) && detailsActive && detailsMode == alias.ToLowerInvariant())
            {
                var rightSb = new StringBuilder();
                rightSb.AppendLine($"[bold white]{display.EscapeMarkup()}[/]");
                rightSb.AppendLine($"[dim]alias:[/] [yellow]{alias.EscapeMarkup()}[/]");
                rightSb.AppendLine();
                rightSb.AppendLine($"[cyan bold]Select Topic for {alias.EscapeMarkup()}:[/]");
                rightSb.AppendLine();
                var topics = new[] { "jp (Japanese / Language)", "en (English Vocabulary)", "cs (C# Quiz)", "dsa (Data Structures & Algorithms)", "interview (Question Bank & STAR)", "[Type Custom Topic...]" };
                for (var i = 0; i < topics.Length; i++)
                {
                    var isSelected = (i == detailsSel);
                    var prefix = isSelected ? "[green bold]> [/]" : "  ";
                    rightSb.AppendLine($"{prefix}{topics[i].EscapeMarkup()}");
                }
                rightSb.AppendLine();
                rightSb.AppendLine("[dim]↑/↓ Navigate  ·  Enter Select  ·  Esc Cancel[/]");
                detailsContent = new Markup(rightSb.ToString());
            }
            else if (string.Equals(alias, "proj", StringComparison.OrdinalIgnoreCase) && detailsActive && detailsMode == "proj")
            {
                var rightSb = new StringBuilder();
                rightSb.AppendLine($"[bold white]{display.EscapeMarkup()}[/]");
                rightSb.AppendLine($"[dim]alias:[/] [yellow]{alias.EscapeMarkup()}[/]");
                rightSb.AppendLine();
                rightSb.AppendLine("[cyan bold]Select Workspace directory to switch to on exit:[/]");
                rightSb.AppendLine();
                var workspaces = WorkspaceRegistry.GetWorkspaces();
                for (var i = 0; i < workspaces.Length; i++)
                {
                    var isSelected = (i == detailsSel);
                    var prefix = isSelected ? "[green bold]> [/]" : "  ";
                    rightSb.AppendLine($"{prefix}{workspaces[i].Name.EscapeMarkup()} [dim]({workspaces[i].WorkspacePath.EscapeMarkup()})[/]");
                }
                rightSb.AppendLine();
                rightSb.AppendLine("[dim]↑/↓ Navigate  ·  Enter Select  ·  Esc Cancel[/]");
                detailsContent = new Markup(rightSb.ToString());
            }
            else
            {
                var rightSb = new StringBuilder();
                rightSb.AppendLine($"[bold white]{display.EscapeMarkup()}[/]");
                rightSb.AppendLine($"[dim]alias:[/] [yellow]{alias.EscapeMarkup()}[/]");
                
                var cmd=CommandPalette.Commands.FirstOrDefault(c => string.Equals(c.Alias, alias, StringComparison.OrdinalIgnoreCase));
                if (cmd!=null)
                {
                    rightSb.AppendLine();
                    rightSb.AppendLine($"[dim]{cmd.Description.EscapeMarkup()}[/]");
                    rightSb.AppendLine();
                    rightSb.AppendLine($"[dim]Category: {cmd.Category.EscapeMarkup()}[/]");
                }
                if (alias == "ollama-status")
                {
                    detailsContent = new Rows(new Markup(rightSb.ToString()), new Markup("\n"), GetOllamaStatusWidget());
                }
                else
                {
                    detailsContent = new Markup(rightSb.ToString());
                }
            }
        }
        else
        {
            var rightSb = new StringBuilder();
            rightSb.AppendLine($"[bold cyan]{sectionTitle.EscapeMarkup()}[/]");
            rightSb.AppendLine();
            if (!string.IsNullOrWhiteSpace(section.Desc))rightSb.AppendLine($"[dim]{section.Desc.EscapeMarkup()}[/]");
            rightSb.AppendLine();
            rightSb.AppendLine("[dim]Press → or Enter to browse options[/]");
            detailsContent = new Markup(rightSb.ToString());
        }

        var leftPanel=new Panel(leftSb.ToString())
        {
            Header=new PanelHeader("[bold cyan]Menu[/]"), Border=BoxBorder.Rounded, BorderStyle=new Style(!midActive?Color.Cyan1:Color.Grey)
        };
        var midPanel=new Panel(midSb.ToString())
        {
            Header=new PanelHeader("[bold cyan]Options[/]"), Border=BoxBorder.Rounded, BorderStyle=new Style(midActive && !detailsActive?Color.Cyan1:Color.Grey)
        };
        var rightPanel=new Panel(detailsContent)
        {
            Header=new PanelHeader("[bold cyan]Details[/]"), Border=BoxBorder.Rounded, BorderStyle=new Style(detailsActive?Color.Cyan1:Color.Grey)
        };

        var table = new Table().NoBorder().HideHeaders();
        table.AddColumn(new TableColumn("").Width(30));
        table.AddColumn(new TableColumn("").Width(30));
        table.AddColumn(new TableColumn(""));
        table.AddRow(leftPanel, midPanel, rightPanel);
        AnsiConsole.Write(table);

    }

}
public static class Program
{
    public static void Main(string[]args)
    {
        if (args.Length>0)
        {
            RunCommand(args[0]);
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

    public static string? SelectTopicInteractive(string promptTitle)
    {
        var topics = new[] { "jp (Japanese / Language)", "en (English Vocabulary)", "cs (C# Quiz)", "dsa (Data Structures & Algorithms)", "interview (Question Bank & STAR)", "[Type Custom Topic...]" };
        var index = SpectreMenu.ShowWithEscape(promptTitle, topics, 0);
        if (index < 0) return null;
        if (index == topics.Length - 1)
        {
            Console.CursorVisible = true;
            var custom = AnsiConsole.Ask<string>("Enter custom topic name:").Trim();
            Console.CursorVisible = false;
            return string.IsNullOrEmpty(custom) ? null : custom;
        }
        return topics[index].Split(' ')[0];
    }

    public static void RunCommand(string alias)
    {
        try
        {
            AnsiConsole.Clear();
        }
        catch
        {
        }
        var lAlias = alias.ToLowerInvariant();
        if ((lAlias == "claude" || lAlias == "codex" || lAlias == "openclaw" || lAlias == "hermes" || lAlias == "hermesd" || lAlias == "claude-cloud" || lAlias == "claude-ollama" || lAlias == "codex-cloud" || lAlias == "codex-ollama") && !AgyAiCore.IsAiOllamaEnabled())
        {
            SpectrePanel.Error("AI/Ollama features are disabled in config.");
            Thread.Sleep(1500);
            return;
        }
        if ((lAlias == "agyswitch" || lAlias == "agyquota" || lAlias == "account-tree" || lAlias == "quota-chart" || lAlias == "live-dashboard" || lAlias == "autoswitch") && !AgyAiCore.IsAgyEnabled())
        {
            SpectrePanel.Error("AGY Account features are disabled in config.");
            Thread.Sleep(1500);
            return;
        }

        try
        {
            switch (alias.ToLowerInvariant())
            {
                case"proj":case"prj":var projPath=ProfileNavigator.Navigate("");
                if (!string.IsNullOrEmpty(projPath))
                {
                    AnsiConsole.MarkupLine($"Navigate target: [green]{projPath}[/]");
                }
                break;
                case"gs":GitHelper.ShowStatus();
                break;
                case"gcmt":GitHelper.ConventionalCommitWizard();
                break;
                case"git-undo":GitHelper.InvokeGitUndo();
                break;
                case"dbld":DotNetHelper.Build();
                break;
                case"dtst":DotNetHelper.Test();
                break;
                case"clean-build":DotNetHelper.RemoveBinObj(Directory.GetCurrentDirectory());
                break;
                case"add-migration":var migName=AnsiConsole.Ask<string>("Migration name:");
                DotNetHelper.AddMigration(migName);
                break;
                case"update-db":DotNetHelper.UpdateDatabase();
                break;
                case"dkcl":DockerHelper.ShowCleanupDashboard();
                break;
                case"dcup":DockerHelper.ComposeUp();
                break;
                case"dcdown":DockerHelper.ComposeDown();
                break;
                case"aws-local":AwsHelper.ShowLocalStackInfo();
                break;
                case"claude":AgyAiCore.InvokeClaude([]);
                break;
                case"claude-cloud":AgyAiCore.InvokeClaude([], "cloud");
                break;
                case"claude-ollama":AgyAiCore.InvokeClaude([], "local");
                break;
                case"codex":AgyAiCore.InvokeCodex([]);
                break;
                case"codex-cloud":AgyAiCore.InvokeCodex([], "cloud");
                break;
                case"codex-ollama":AgyAiCore.InvokeCodex([], "local");
                break;
                case"openclaw":AgyAiCore.InvokeOpenClaw([]);
                 break;
                 case"ollama-models":OllamaHelper.ManageOllamaModels();
                 break;
                 case"ollama-pull":OllamaHelper.PullOllamaModel();
                 break;
                 case"ollama-start":OllamaHelper.StartOllamaDaemon();
                 break;
                 case"ollama-logs":OllamaHelper.ShowOllamaLogs();
                break;
                case"ollama-status":
                    CcNavigator._cachedOllamaWidget = null;
                    break;
                case"deck-setup":AntigravityDeckHelper.Setup();
                break;
                case"deck-start":AntigravityDeckHelper.StartLocal();
                break;
                case"deck-online":AntigravityDeckHelper.StartOnline();
                break;
                case"agy-cli":
                    try
                    {
                        var targetDirLoc = AgyAccountCore.GetAccountDirectory(AgyAccountCore.GetActiveAccount());
                        var psi = new ProcessStartInfo
                        {
                            FileName = "cmd.exe",
                            Arguments = "/c agy",
                            UseShellExecute = false
                        };
                        psi.EnvironmentVariables["GEMINI_HOME"] = targetDirLoc;
                        psi.EnvironmentVariables["HOME"] = targetDirLoc;
                        psi.EnvironmentVariables["USERPROFILE"] = targetDirLoc;

                        using var p = Process.Start(psi);
                        p?.WaitForExit();
                    }
                    catch (Exception ex)
                    {
                        SpectrePanel.Error($"Failed to run agy CLI: {ex.Message}");
                    }
                    break;
                case"hermes":if (AgyAiCore.InvokeHermes([])==AgyAiCore.HermesResult.NotInstalled)SpectrePanel.Warning("Hermes is not installed. Run 'hermes' from PowerShell for install instructions.");
                break;
                case"hermesd":if (AgyAiCore.InvokeHermesDesktop([])==AgyAiCore.HermesResult.NotInstalled)SpectrePanel.Warning("Hermes Desktop is not installed. Run 'hermesd' from PowerShell for install instructions.");
                break;
                case"disk":SystemHelper.ShowDiskSpace();
                break;
                case"public-ip":AnsiConsole.MarkupLine($"Public IP: [green]{SystemHelper.GetPublicIP()}[/]");
                break;
                case"kill-port":var portStr=AnsiConsole.Ask<string>("Port number:");
                if (int.TryParse(portStr, out var port))SystemHelper.KillPort(port);
                break;
                case"ssh-info":SshHelper.ShowSshInfo();
                break;
                case"db-tui":var dbPath=AnsiConsole.Ask<string>("SQLite DB path:");
                DatabaseHelper.ShowDatabaseTui(dbPath);
                break;
                case"agyswitch":var accs=AgyAccountCore.GetAccounts();
                var activeAcc=AgyAccountCore.GetActiveAccount();
                var accItems=accs.Select(a => a==activeAcc?$"{a} (Active)":a).ToArray();
                var defaultIdx=Array.IndexOf(accs, activeAcc);
                if (defaultIdx<0)defaultIdx=0;
                var accIdx=SpectreMenu.ShowWithEscape("Select Account to Switch", accItems, defaultIdx);
                if (accIdx>=0)
                {
                    var targetAcc=accs[accIdx];
                    AgyAccountCore.SetActiveAccount(targetAcc, false);
                    Thread.Sleep(1000);
                }
                break;
                case"agyquota":AgyAccountCore.ShowAllAccountsSummary();
                break;
                case"account-tree":AgyAccountDisplay.ShowAccountTree();
                break;
                case"quota-chart":AgyAccountDisplay.ShowQuotaChart(AgyAccountCore.GetActiveAccount());
                break;
                case"live-dashboard":SpectreTable.Live(["Account","Login","Quota W","Quota 5h","Last Used"], () => AgyAccountCore.GetAccounts().Select(a =>
                {
                    var s=AgyAccountCore.GetAccountStats(a);
                    var act=AgyAccountCore.GetActiveAccount();
                    var n=a==act?$"[green bold]* {a}[/]":a;
                    var st=s.TokenStatus=="Logged In"?"[green]●[/]":"[red]○[/]";
                    var lu=s.LastUsed.Length>=10&&s.LastUsed!="Never"?s.LastUsed[..10]:"Never";
                    return new[]
                    {
                        n, st,$"{(int)Math.Round(s.GeminiWeekly)}%",$"{(int)Math.Round(s.GeminiFiveHour)}%", lu
                    }
                    ;
                }
                ).ToArray(), 5000);
                break;
                case"autoswitch":AgyAccountCore.ToggleAutoSwitch();
                break;
                case"scaffold":ProjectScaffolder.Scaffold();
                break;
                case"help":ProfileHelp.Show();
                break;
                case"theme":
                {
                    var tPath = Environment.GetEnvironmentVariable("POSH_THEMES_PATH");
                    if (string.IsNullOrEmpty(tPath))
                    {
                        tPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "asset", "powershell-themes");
                        if (!Directory.Exists(tPath))
                        {
                            tPath = Path.Combine(Directory.GetCurrentDirectory(), "asset", "powershell-themes");
                        }
                    }
                    var currTheme = Environment.GetEnvironmentVariable("THEME");
                    var newThemePath = ThemeHelper.SelectThemeInteractive(tPath, currTheme);
                    if (!string.IsNullOrEmpty(newThemePath))
                    {
                        var selThemeFile = Path.Combine(AgyAccountCore.AgySourceHome, "selected_theme.txt");
                        File.WriteAllText(selThemeFile, newThemePath);
                    }
                }
                break;
                case"cc":CommandPalette.Show();
                break;
                case"learn":var learnTopic=SelectTopicInteractive("Select Topic to Learn");
                if (!string.IsNullOrEmpty(learnTopic)) LearnRouter.StartLearning(learnTopic);
                break;
                case"flashcard":FlashcardEngine.PickAndRun(LearnDataPaths.DecksDir);
                break;
                case"vocab":VocabDrill.Run("Intermediate");
                break;
                case"kana":KanaQuiz.Run("hiragana");
                break;
                case"kanji":KanjiLookup.Run();
                break;
                case"jlpt":JlptVocabDrill.Run("N5");
                break;
                case"algo":AlgoVisualizer.PickAndRun();
                break;
                case"complexity":ComplexitySheet.Run();
                break;
                case"problems":ProblemTracker.Run();
                break;
                case"snippets":SnippetLibrary.Run();
                break;
                case"sheets":CheatSheetBrowser.Run();
                break;
                case"quiz":CsharpQuiz.Run();
                break;
                case"interview":InterviewBank.Run();
                break;
                case"star":StarBuilder.Run();
                break;
                case"mock":MockInterviewTimer.Run();
                break;
                case"word-of-day":var word=WordOfDay.Pick();
                if (word!=null)WordOfDay.Render(word);

                else SpectrePanel.Warning("No word of the day available.");
                break;
                case"session":var sessionTopic=SelectTopicInteractive("Select Topic for Study Session");
                if (!string.IsNullOrEmpty(sessionTopic)) StudySession.Run(sessionTopic);
                break;
                case"stats":StudyStats.Run();
                break;
                case"goals":DailyGoals.Show();
                break;
                case"streak":StudyStreak.ShowPanel();
                break;
                case"due":LearnDataPaths.EnsureDirectories();
                int dueCount=0;
                if (Directory.Exists(LearnDataPaths.DecksDir))
                {
                    foreach (var deckFile in Directory.GetFiles(LearnDataPaths.DecksDir,"*.json"))
                    {
                        var deck=LearnDataPaths.LoadJson<DeckFile>(deckFile);
                        if (deck!=null)
                        {
                            dueCount+=deck.Cards.Count(c => SpacedRepetitionEngine.IsDueToday(c.Sr));
                        }
                    }
                }
                AnsiConsole.MarkupLine($"Due spaced repetition reviews today: [yellow]{dueCount}[/]");
                break;
                case"progress":ProgressDashboard.Show();
                break;
                case"weak":var weakTopic=SelectTopicInteractive("Select Topic for Weak Items");
                if (!string.IsNullOrEmpty(weakTopic)) WeakItemsQueue.ShowPreSessionReview(weakTopic);
                break;
                case"obsidian":ObsidianBridge.Run();
                break;
                case"obs-graph":var cfg=ObsidianBridge.LoadConfig();
                if (cfg!=null&&Directory.Exists(cfg.VaultPath))ObsidianGraph.Run(cfg.VaultPath);

                else SpectrePanel.Warning("Obsidian vault path not configured. Run 'obsidian' first.");
                break;
                case"nexus":case"repo-graph":RepoGraph.Show(RepoGraph.Build());
                break;
                case"nexus-stats":GitNexusStats.Run();
                break;
                case"ide":TerminalIde.Open();
                break;
                case"ide-diff":GitDiffViewer.ShowDiff(Directory.GetCurrentDirectory());
                break;
                case"ide-search":AnsiConsole.MarkupLine("IDE Search: Browse symbols for current directory files.");
                break;
                case"refresh":LearnRouter.RefreshData("all");
                break;
                case"add-resource":var path=AnsiConsole.Ask<string>("Resource path/URL:");
                var tags=AnsiConsole.Ask<string>("Tags (comma separated):").Split(',').Select(t => t.Trim()).ToArray();
                ResourceRegistry.AddResource(path, tags);
                SpectrePanel.Success("Resource registered.");
                break;
                default:SpectrePanel.Warning($"Command alias '{alias}' is not implemented for direct TUI routing.");
                break;
            }
        }
        catch (Exception ex)
        {
            SpectrePanel.Error($"Error running command: {ex.Message}");
        }
        AnsiConsole.WriteLine();
        if (!Console.IsInputRedirected)
        {
            AnsiConsole.MarkupLine("[dim]Press any key to return to menu...[/]");

            try
            {
                Console.ReadKey(true);
            }
            catch
            {
            }
        }

    }

}