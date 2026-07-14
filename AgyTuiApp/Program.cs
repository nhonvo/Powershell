
// ============================================================
// AGY TUI — Spectre.Console UI library for AGY CLI Profile
// Single-file C# class library · .NET 10
// ============================================================
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using Spectre.Console;

namespace AgyTui;

// ┌─────────────────────────────────────────────────────────┐
// │ SpectreMenu │
// │ Replaces: TerminalMenu::Show / ShowRobust / ShowDynamic│
// └─────────────────────────────────────────────────────────┘
public static class SpectreMenu
{
 public static int Show(string header, string[] items, int defaultIndex)
 => CoreShow([header], items, [], false, false);

 public static int Show(string header, string[] items, int defaultIndex, bool searchEnabled)
 => CoreShow([header], items, [], searchEnabled, false);

 public static int ShowRobust(
 string[] headerLines, string[] items,
 int defaultIndex, bool searchEnabled, bool fullScreen)
 => CoreShow(headerLines, items, [], searchEnabled, fullScreen);

 public static int Show(
 string[] headerLines, string[] items, string[] cmds,
 int defaultIndex, bool searchEnabled, bool fullScreen)
 {
 if (items.Length == 0) return -1;
 if (fullScreen) AnsiConsole.Clear();
 PrintHeader(headerLines);
 if (cmds.Length > defaultIndex && !string.IsNullOrWhiteSpace(cmds[defaultIndex]))
 AnsiConsole.Write(new Panel($"[dim]{cmds[defaultIndex].EscapeMarkup()}[/]")
 {
 Header = new PanelHeader("[grey]Command[/]"),
 Border = BoxBorder.Rounded
 });
 return PromptIndex(items, searchEnabled);
 }

 public static string? ShowDynamic(string header, Func<string, string[]> resolver, int defaultIndex)
 => ShowDynamic(header, resolver, defaultIndex, string.Empty);

 public static string? ShowDynamic(
 string header, Func<string, string[]> resolver,
 int defaultIndex, string initialFilter)
 {
 var items = resolver(initialFilter);
 if (items.Length == 0) return null;
 PrintHeader([header]);
 try { return AnsiConsole.Prompt(BuildPrompt(items, true)); }
 catch { return null; }
 }

 // kept for PowerShell call-site compatibility
 public static void InitializeTuiColors() { }

 private static int CoreShow(
 string[] headerLines, string[] items,
 string[] cmds, bool searchEnabled, bool fullScreen)
 {
 if (items.Length == 0) return -1;
 if (fullScreen) AnsiConsole.Clear();
 PrintHeader(headerLines);
 return PromptIndex(items, searchEnabled);
 }

 private static void PrintHeader(string[] lines)
 {
 foreach (var line in lines.Where(l => !string.IsNullOrWhiteSpace(l)))
 AnsiConsole.Write(new Rule($"[bold cyan]{line.EscapeMarkup()}[/]").RuleStyle("grey"));
 }

 private static int PromptIndex(string[] items, bool searchEnabled)
 {
 var prompt = BuildPrompt(items, searchEnabled);
 try { return Array.IndexOf(items, AnsiConsole.Prompt(prompt)); }
 catch { return -1; }
 }

 private static SelectionPrompt<string> BuildPrompt(string[] items, bool searchEnabled)
 {
 var pageSize = Math.Min(15, Math.Max(5, Console.WindowHeight - 8));
 var prompt = new SelectionPrompt<string>()
 .PageSize(pageSize)
 .HighlightStyle(new Style(Color.Green));
 if (searchEnabled) prompt.SearchEnabled = true;
 prompt.AddChoices(items);
 return prompt;
 }
}

// ┌─────────────────────────────────────────────────────────┐
// │ SpectrePager │
// │ Replaces: TerminalMenu::ShowScrollableContent │
// └─────────────────────────────────────────────────────────┘
public static class SpectrePager
{
 public static void Show(string title, string[] lines)
 {
 var pageSize = Math.Max(5, Console.WindowHeight - 8);
 var totalLines = lines.Length;
 var top = 0;
 Console.CursorVisible = false;
 try
 {
 while (true)
 {
 AnsiConsole.Clear();
 AnsiConsole.Write(new Rule($"[bold cyan]{title.EscapeMarkup()}[/]").RuleStyle("grey"));

 for (var i = top; i < Math.Min(top + pageSize, totalLines); i++)
 AnsiConsole.MarkupLine(lines[i].EscapeMarkup());

 for (var p = Math.Min(top + pageSize, totalLines); p < top + pageSize; p++)
 AnsiConsole.WriteLine();

 AnsiConsole.MarkupLine(
 $"[dim] ↑/↓ scroll PgUp/PgDn page Home/End jump Esc close" +
 $" ({top + 1}–{Math.Min(top + pageSize, totalLines)} of {totalLines})[/]");

 switch (Console.ReadKey(true).Key)
 {
 case ConsoleKey.DownArrow: if (top + pageSize < totalLines) top++; break;
 case ConsoleKey.UpArrow: if (top > 0) top--; break;
 case ConsoleKey.PageDown:
 top = Math.Min(totalLines - pageSize, top + pageSize);
 if (top < 0) top = 0;
 break;
 case ConsoleKey.PageUp: top = Math.Max(0, top - pageSize); break;
 case ConsoleKey.Home: top = 0; break;
 case ConsoleKey.End: top = Math.Max(0, totalLines - pageSize); break;
 case ConsoleKey.Escape:
 case ConsoleKey.Enter: return;
 }
 }
 }
 finally { Console.CursorVisible = true; }
 }
}

// ┌─────────────────────────────────────────────────────────┐
// │ SpectrePanel │
// │ Replaces: bare Write-Host status / error output │
// └─────────────────────────────────────────────────────────┘
public static class SpectrePanel
{
 public static void Success(string message) => Render(message, Color.Green, "✓ Success");
 public static void Error(string message) => Render(message, Color.Red, "✗ Error");
 public static void Warning(string message) => Render(message, Color.Yellow, "⚠ Warning");
 public static void Info(string message) => Render(message, Color.Cyan1, "ℹ Info");

 private static void Render(string message, Color border, string header)
 => AnsiConsole.Write(new Panel(message.EscapeMarkup())
 {
 Header = new PanelHeader($"[bold]{header}[/]"),
 Border = BoxBorder.Rounded,
 BorderStyle = new Style(border),
 Padding = new Padding(1, 0)
 });
}

// ┌─────────────────────────────────────────────────────────┐
// │ SpectreProgress │
// │ Replaces: LogHelper::InvokeWithSpinner + manual bars │
// └─────────────────────────────────────────────────────────┘
public static class SpectreProgress
{
 public static void Spinner(string message, Action action)
 => AnsiConsole.Status()
 .Spinner(Spectre.Console.Spinner.Known.Dots)
 .SpinnerStyle(new Style(Color.Yellow))
 .Start(message, _ => action());

 public static void BulkProgress(string label, string[] items, Action<int, string> action)
 => AnsiConsole.Progress()
 .AutoClear(false)
 .Columns(
 new TaskDescriptionColumn(),
 new ProgressBarColumn(),
 new PercentageColumn(),
 new ElapsedTimeColumn())
 .Start(ctx =>
 {
 var task = ctx.AddTask($"[green]{label.EscapeMarkup()}[/]", maxValue: items.Length);
 for (var i = 0; i < items.Length; i++)
 {
 task.Description = $"[green]{label.EscapeMarkup()}: {items[i].EscapeMarkup()}[/]";
 action(i, items[i]);
 task.Increment(1);
 }
 });
}

// ┌─────────────────────────────────────────────────────────┐
// │ SpectreTable │
// │ Replaces: manual Write-Host rows + cc dashboard panes │
// └─────────────────────────────────────────────────────────┘
public static class SpectreTable
{
 public static void Render(string[] columns, string[][] rows, bool markup = false)
 {
 var t = new Table { Border = TableBorder.Rounded };
 foreach (var col in columns)
 t.AddColumn(new TableColumn($"[bold]{col.EscapeMarkup()}[/]"));
 foreach (var row in rows)
 t.AddRow(markup ? row : row.Select(c => c.EscapeMarkup()).ToArray());
 AnsiConsole.Write(t);
 }

 public static void Live(string[] columns, Func<string[][]> dataSource, int refreshMs = 5000)
 {
 var t = new Table { Border = TableBorder.Rounded };
 foreach (var col in columns)
 t.AddColumn(new TableColumn($"[bold]{col.EscapeMarkup()}[/]"));

 AnsiConsole.Live(t).Start(ctx =>
 {
 while (!Console.KeyAvailable)
 {
 t.Rows.Clear();
 foreach (var row in dataSource()) t.AddRow(row);
 ctx.Refresh();
 Thread.Sleep(refreshMs);
 }
 Console.ReadKey(true);
 });
 }

 public static void ThreePane(
 string leftTitle, string[] leftItems, int leftSelected,
 string midTitle, string[] midItems, int midSelected,
 string rightTitle, string[] rightItems)
 {
 AnsiConsole.Write(new Columns(
 BuildPane(leftTitle, leftItems, leftSelected),
 BuildPane(midTitle, midItems, midSelected),
 BuildPane(rightTitle, rightItems, -1)));
 }

 private static Panel BuildPane(string title, string[] items, int selected)
 {
 var sb = new StringBuilder();
 for (var i = 0; i < items.Length; i++)
 sb.AppendLine(i == selected
 ? $"[green bold]> {items[i].EscapeMarkup()}[/]"
 : $" {items[i].EscapeMarkup()}");
 return new Panel(sb.ToString())
 {
 Header = new PanelHeader($"[bold cyan]{title.EscapeMarkup()}[/]"),
 Border = BoxBorder.Rounded
 };
 }
}

// ┌─────────────────────────────────────────────────────────┐
// │ AgyAccountCore — Data layer │
// │ Port of AgyAccountManager.ps1 pure-logic methods. │
// │ No DPAPI, no junctions, no process invocation. │
// │ Those stay in PowerShell. │
// └─────────────────────────────────────────────────────────┘

public sealed class AccountMetadata
{
 [JsonPropertyName("LastUsed")] public string LastUsed { get; set; } = "Never";
 [JsonPropertyName("UsageCount")] public int UsageCount { get; set; }
 [JsonPropertyName("QuotaStatus")] public string QuotaStatus { get; set; } = "OK";
 [JsonPropertyName("RequestHistory")] public List<string> RequestHistory { get; set; } = [];
}

public sealed record QuotaMetrics(
 double RemainingWeekly, double Remaining5H,
 string TimeWeekly, string Time5H,
 int CountWeekly, int Count5H);

public sealed record AccountStats(
 string LastUsed, int UsageCount, string PrivateSize,
 string JunctionStatus, int SkillsCount, int ConversationsCount,
 string TokenStatus, string QuotaStatus,
 double GeminiWeekly, double GeminiFiveHour);

public static class AgyAccountCore
{
 public static readonly string AgySourceHome = @"C:\Users\Public\.gemini";
 public static readonly string AgyAccountPrefix = @"C:\Users\Public\.gemini_";
 public static string AgyActiveAccountFile => Path.Combine(AgySourceHome, "active_account.txt");

 private static bool? _networkOnline;

 // ── Network ───────────────────────────────────────────────
 public static bool CheckNetworkStatus()
 {
 if (_networkOnline.HasValue) return _networkOnline.Value;
 try
 {
 if (!NetworkInterface.GetIsNetworkAvailable()) { _networkOnline = false; return false; }
 using var ping = new Ping();
 _networkOnline = ping.Send("8.8.8.8", 200).Status == IPStatus.Success;
 }
 catch { _networkOnline = false; }
 return _networkOnline.Value;
 }

 // ── Account discovery ─────────────────────────────────────
 public static string[] GetAccounts()
 {
 var accounts = new List<string> { "default" };
 var scanPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

 var userProfile = Environment.GetEnvironmentVariable("USERPROFILE") ?? "";
 if (Directory.Exists(userProfile)) scanPaths.Add(userProfile);

 var prefixParent = Path.GetDirectoryName(AgyAccountPrefix);
 if (prefixParent != null && Directory.Exists(prefixParent)) scanPaths.Add(prefixParent);

 foreach (var scanPath in scanPaths)
 {
 foreach (var dir in Directory.GetDirectories(scanPath, ".gemini_*"))
 {
 var m = Regex.Match(Path.GetFileName(dir), @"^\.gemini_(.+)$");
 if (!m.Success) continue;
 var name = m.Groups[1].Value;
 if (!Regex.IsMatch(name, "backup|copy|temp", RegexOptions.IgnoreCase)
 && !accounts.Contains(name, StringComparer.OrdinalIgnoreCase))
 accounts.Add(name);
 }
 }
 return [.. accounts];
 }

 public static string GetActiveAccount()
 {
 var home = Environment.GetEnvironmentVariable("GEMINI_HOME") ?? "";
 var m = Regex.Match(home, @"\.gemini_(.+)$");
 return m.Success ? m.Groups[1].Value : "default";
 }

 public static string GetAccountDirectory(string accountName)
 {
 if (string.Equals(accountName, "default", StringComparison.OrdinalIgnoreCase))
 return AgySourceHome;
 var target = AgyAccountPrefix + accountName;
 if (!Directory.Exists(target))
 {
 var alt = Path.Combine(Environment.GetEnvironmentVariable("USERPROFILE") ?? "", ".gemini_" + accountName);
 if (Directory.Exists(alt)) return alt;
 }
 return target;
 }

 // ── Metadata ─────────────────────────────────────────────
 public static AccountMetadata GetAccountMetadata(string accountName)
 {
 var file = Path.Combine(GetAccountDirectory(accountName), "account_metadata.json");
 if (File.Exists(file))
 {
 try
 {
 var raw = File.ReadAllText(file);
 if (!string.IsNullOrWhiteSpace(raw))
 {
 var meta = JsonSerializer.Deserialize<AccountMetadata>(raw);
 if (meta != null) return meta;
 }
 }
 catch { }
 }
 return new AccountMetadata();
 }

 public static void UpdateAccountMetadata(string accountName)
 {
 var dir = GetAccountDirectory(accountName);
 try { Directory.CreateDirectory(dir); } catch { return; }

 var meta = GetAccountMetadata(accountName);
 meta.LastUsed = DateTimeOffset.Now.ToString("yyyy-MM-ddTHH:mm:sszzz");
 meta.UsageCount++;

 var now = DateTime.UtcNow;
 var cutoff = now.AddDays(-7);
 meta.RequestHistory.Add(now.ToString("o"));
 meta.RequestHistory = meta.RequestHistory
 .Where(ts => DateTime.TryParse(ts, null,
 System.Globalization.DateTimeStyles.RoundtripKind, out var dt) && dt >= cutoff)
 .ToList();

 try
 {
 var json = JsonSerializer.Serialize(meta, new JsonSerializerOptions { WriteIndented = true });
 File.WriteAllText(Path.Combine(dir, "account_metadata.json"), json, Encoding.UTF8);
 }
 catch { }
 }

 public static void SetAccountQuotaExceeded(string accountName, bool exceeded)
 {
 var dir = GetAccountDirectory(accountName);
 if (!Directory.Exists(dir)) return;
 var meta = GetAccountMetadata(accountName);
 var newStatus = exceeded ? "Exceeded" : "OK";
 if (meta.QuotaStatus == newStatus) return;
 meta.QuotaStatus = newStatus;
 try
 {
 var json = JsonSerializer.Serialize(meta, new JsonSerializerOptions { WriteIndented = true });
 File.WriteAllText(Path.Combine(dir, "account_metadata.json"), json, Encoding.UTF8);
 }
 catch { }
 }

 // ── Quota calculation ─────────────────────────────────────
 public static QuotaMetrics CalculateRollingQuotas(string accountName)
 {
 var history = GetAccountMetadata(accountName).RequestHistory;
 var now = DateTime.UtcNow;
 var fiveHoursAgo = now.AddHours(-5);
 var sevenDaysAgo = now.AddDays(-7);

 int reqs5H = 0, reqsWeekly = 0;
 var oldest5H = now;
 var oldestWeekly = now;

 foreach (var ts in history)
 {
 if (!DateTime.TryParse(ts, null,
 System.Globalization.DateTimeStyles.RoundtripKind, out var dt)) continue;
 if (dt >= fiveHoursAgo) { reqs5H++; if (dt < oldest5H) oldest5H = dt; }
 if (dt >= sevenDaysAgo) { reqsWeekly++; if (dt < oldestWeekly) oldestWeekly = dt; }
 }

 const int limit5H = 50, limitWeekly = 1000;
 var remaining5H = Math.Max(0.0, 100.0 - Math.Round((reqs5H / (double)limit5H) * 100.0, 2));
 var remainingWeekly = Math.Max(0.0, 100.0 - Math.Round((reqsWeekly / (double)limitWeekly) * 100.0, 2));

 var secs5H = Math.Max(0, (int)Math.Round((oldest5H.AddHours(5) - now).TotalSeconds));
 var secsWeekly = Math.Max(0, (int)Math.Round((oldestWeekly.AddDays(7) - now).TotalSeconds));

 static string Fmt(int s) => $"{s / 3600}h {(s % 3600) / 60}m";
 return new QuotaMetrics(remainingWeekly, remaining5H, Fmt(secsWeekly), Fmt(secs5H), reqsWeekly, reqs5H);
 }

 // ── Auto-switch ───────────────────────────────────────────
 public static bool IsAutoSwitchEnabled()
 {
 var file = Path.Combine(AgySourceHome, "auto_switch_enabled.txt");
 if (!File.Exists(file)) return true;
 try { return File.ReadAllText(file).Trim() != "False"; }
 catch { return true; }
 }

 public static void ToggleAutoSwitch()
 {
 var current = IsAutoSwitchEnabled();
 try
 {
 Directory.CreateDirectory(AgySourceHome);
 File.WriteAllText(Path.Combine(AgySourceHome, "auto_switch_enabled.txt"),
 current ? "False" : "True", Encoding.UTF8);
 SpectrePanel.Info($"Auto-Switch is now: {(current ? "Disabled" : "Enabled")}");
 }
 catch { SpectrePanel.Error("Failed to update Auto-Switch setting."); }
 }

 // Returns the account to switch to, or null if no switch needed.
 // Caller (PowerShell) is responsible for executing SetActiveAccount.
 public static string? FindAutoSwitchCandidate()
 {
 if (!IsAutoSwitchEnabled()) return null;
 var active = GetActiveAccount();
 if (GetAccountMetadata(active).QuotaStatus != "Exceeded") return null;

 foreach (var acc in GetAccounts())
 {
 if (string.Equals(acc, active, StringComparison.OrdinalIgnoreCase)) continue;
 var tokenFile = Path.Combine(GetAccountDirectory(acc), "keyring_token.txt");
 if (!File.Exists(tokenFile)) continue;
 var quota = GetAccountMetadata(acc).QuotaStatus ?? "OK";
 if (quota == "OK") return acc;
 }
 return null;
 }

 // ── Quota-after-run check ─────────────────────────────────
 public static bool CheckQuotaAfterRun(string accountName)
 {
 try
 {
 var brainDir = Path.Combine(AgySourceHome, "antigravity", "brain");
 if (!Directory.Exists(brainDir)) return false;

 var latest = new DirectoryInfo(brainDir)
 .EnumerateFiles("transcript.jsonl", SearchOption.AllDirectories)
 .OrderByDescending(f => f.LastWriteTime)
 .FirstOrDefault();
 if (latest == null) return false;
 if ((DateTime.Now - latest.LastWriteTime).TotalSeconds > 60) return false;

 var tail = File.ReadLines(latest.FullName).TakeLast(15);
 var quotaErr = tail.Any(line =>
 Regex.IsMatch(line, @"RESOURCE_EXHAUSTED|quota exceeded|quotaExceeded|ResourceExhausted|quota limit") &&
 Regex.IsMatch(line, @"""status""\s*:\s*""ERROR""|""code""\s*:\s*429"));

 SetAccountQuotaExceeded(accountName, quotaErr);
 return quotaErr;
 }
 catch { return false; }
 }

 // ── Filesystem helpers ────────────────────────────────────
 public static long GetPrivateDirectorySize(string path)
 {
 if (!Directory.Exists(path)) return 0;
 long total = 0;
 try
 {
 foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
 {
 bool inJunction = false;
 var parent = Path.GetDirectoryName(file);
 while (parent != null && parent.Length >= path.Length)
 {
 var di = new DirectoryInfo(parent);
 if (di.Exists && di.LinkTarget != null) { inJunction = true; break; }
 parent = Path.GetDirectoryName(parent);
 }
 if (!inJunction) total += new FileInfo(file).Length;
 }
 }
 catch { }
 return total;
 }

 public static string GetJunctionStatus(string accountName)
 {
 if (string.Equals(accountName, "default", StringComparison.OrdinalIgnoreCase))
 return "Healthy (Primary)";
 var destDir = GetAccountDirectory(accountName);
 if (!Directory.Exists(destDir)) return "Uninitialized";
 var shared = new[] { "antigravity", "antigravity-cli", "config", "history", "antigravity-ide", "wf" };
 foreach (var sub in shared)
 {
 var subPath = Path.Combine(destDir, sub);
 if (!Directory.Exists(subPath)) return "Needs Repair";
 if (new DirectoryInfo(subPath).LinkTarget == null) return "Needs Repair";
 }
 return "Healthy";
 }

 // ── Aggregated stats ──────────────────────────────────────
 public static AccountStats GetAccountStats(string accountName)
 {
 var meta = GetAccountMetadata(accountName);
 var dir = GetAccountDirectory(accountName);
 var privateSize = GetPrivateDirectorySize(dir);
 var junctionStatus = GetJunctionStatus(accountName);

 int skillsCount = 0, convCount = 0;
 var skillsPath = Path.Combine(AgySourceHome, "config", "skills");
 if (Directory.Exists(skillsPath))
 skillsCount = Directory.GetDirectories(skillsPath).Length;
 var convPath = Path.Combine(AgySourceHome, "antigravity", "brain");
 if (Directory.Exists(convPath))
 convCount = Directory.GetDirectories(convPath).Length;

 var tokenStatus = File.Exists(Path.Combine(dir, "keyring_token.txt")) ? "Logged In" : "Not Logged In";

 string sizeStr;
 if (privateSize > 1_048_576) sizeStr = $"{Math.Round(privateSize / 1_048_576.0, 2)} MB";
 else if (privateSize > 1_024) sizeStr = $"{Math.Round(privateSize / 1_024.0, 2)} KB";
 else sizeStr = $"{privateSize} B";

 var quota = CalculateRollingQuotas(accountName);
 return new AccountStats(
 meta.LastUsed, meta.UsageCount, sizeStr, junctionStatus,
 skillsCount, convCount, tokenStatus, meta.QuotaStatus,
 quota.RemainingWeekly, quota.Remaining5H);
 }

 // ── Display helpers ───────────────────────────────────────
 public static string GetProgressBar(double percentage)
 {
 const int total = 50;
 int filled = Math.Min(total, Math.Max(0, (int)Math.Round((percentage / 100.0) * total)));
 var bar = new string('█', filled) + new string('░', total - filled);
 return $" [{bar}] {Math.Round(percentage, 2):F2}%";
 }

 public static string[] GetUsageLines(string accountName)
 {
 var meta = GetAccountMetadata(accountName);
 var quota = CalculateRollingQuotas(accountName);
 double geminiWeekly = quota.RemainingWeekly;
 double geminiFiveHour = quota.Remaining5H;
 double claudeWeekly = 100.0; // not yet tracked
 double claudeFiveHour = 100.0;

 var bar = new string('─', 140);
 var lines = new List<string>
 {
 bar, ">", bar,
 "└ Models & Quota", "",
 $" Account: {accountName}", "",
 "GEMINI MODELS",
 " Models within this group: Gemini Flash, Gemini Pro", "",
 " Weekly Limit",
 GetProgressBar(geminiWeekly),
 $" {(int)Math.Round(geminiWeekly)}% remaining · Refreshes in {quota.TimeWeekly}", "",
 " Five Hour Limit",
 GetProgressBar(geminiFiveHour),
 geminiFiveHour >= 100.0
 ? " Quota available"
 : $" {(int)Math.Round(geminiFiveHour)}% remaining · Refreshes in {quota.Time5H}",
 "", "",
 "CLAUDE AND GPT MODELS",
 " Models within this group: Claude Opus, Claude Sonnet, GPT-OSS", "",
 " Weekly Limit",
 GetProgressBar(claudeWeekly),
 claudeWeekly >= 100.0 ? " Quota available" : $" {(int)Math.Round(claudeWeekly)}% remaining",
 "", " Five Hour Limit",
 GetProgressBar(claudeFiveHour),
 claudeFiveHour >= 100.0 ? " Quota available" : $" {(int)Math.Round(claudeFiveHour)}% remaining",
 "", "",
 " │ Within each group, models share a weekly limit and a 5-hour limit. Quota is",
 " │ consumed proportionally to the cost of the tokens. The 5-hour limit smooths",
 " │ out aggregate demand to fairly distribute global capacity across all users.",
 "", " Weekly Request Distribution (Last 7 Days)",
 " ==========================================="
 };

 var now = DateTime.Now;
 var dayData = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
 for (int i = 0; i < 7; i++) dayData[now.Date.AddDays(-i).ToString("ddd")] = 0;

 foreach (var ts in meta.RequestHistory)
 {
 if (!DateTime.TryParse(ts, null,
 System.Globalization.DateTimeStyles.RoundtripKind, out var dt)) continue;
 var key = dt.ToLocalTime().ToString("ddd");
 if (dayData.ContainsKey(key)) dayData[key]++;
 }

 for (int i = 6; i >= 0; i--)
 {
 var date = now.Date.AddDays(-i);
 var day = date.ToString("ddd");
 var count = dayData.GetValueOrDefault(day);
 var bar2 = new string('█', Math.Min(10, count)) + new string('░', Math.Max(0, 10 - count));
 lines.Add($" {day} [{bar2}] {count} requests");
 }

 lines.Add(" -------------------------------------------");
 lines.Add($" Total Weekly Requests: {quota.CountWeekly} / 1000 limit");
 return [.. lines];
 }

 // ── Spectre display methods ───────────────────────────────
 public static void ShowAccounts()
 {
 var accounts = GetAccounts();
 var active = GetActiveAccount();
 var rows = accounts
 .Select(a => new[] {
 a == active ? "[green bold]*[/]" : " ",
 a.EscapeMarkup(),
 GetAccountDirectory(a).EscapeMarkup(),
 File.Exists(Path.Combine(GetAccountDirectory(a), "keyring_token.txt"))
 ? "[green]Logged In[/]" : "[dim]Not Logged In[/]"
 })
 .ToArray();
 SpectreTable.Render(["", "Account", "Directory", "Status"], rows, markup: true);
 }

 public static void ShowAllAccountsSummary()
 {
 var accounts = GetAccounts();
 var active = GetActiveAccount();
 var rows = accounts.Select(acc =>
 {
 var stats = GetAccountStats(acc);
 var nameCell = (acc == active ? "[green bold]* " : " ") + acc.EscapeMarkup() + (acc == active ? "[/]" : "");
 var quotaStr = stats.TokenStatus == "Logged In"
 ? (stats.QuotaStatus == "Exceeded"
 ? "[red]Exceeded[/]"
 : $"[cyan]{(int)Math.Round(stats.GeminiWeekly)}%[/] / [cyan]{(int)Math.Round(stats.GeminiFiveHour)}%[/]")
 : "[dim]--[/]";
 var lastUsed = stats.LastUsed.Length >= 10 && stats.LastUsed != "Never"
 ? stats.LastUsed[..10] : "Never";
 return new[] { nameCell, stats.TokenStatus == "Logged In" ? "[green]Logged In[/]" : "[dim]Not Logged In[/]",
 quotaStr, stats.UsageCount.ToString(), lastUsed, stats.PrivateSize };
 }).ToArray();
 SpectreTable.Render(["Account", "Status", "Quota W / 5h", "Uses", "Last Used", "Size"], rows, markup: true);
 }
}
