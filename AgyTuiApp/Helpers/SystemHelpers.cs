using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using Spectre.Console;
using AgyTui.Components;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.AccessControl;

namespace AgyTui;

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
        if (capture)
        {
            return Helpers.ProcessRunner.RunCapture(exe, args);
        }
        else
        {
            Helpers.ProcessRunner.Run(exe, args);
            return string.Empty;
        }
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
