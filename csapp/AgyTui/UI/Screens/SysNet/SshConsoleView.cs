using QRCoder;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.AccessControl;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AgyTui.UI.Screens.SysNet;

public static class SshConsoleView
{
    private static readonly string AuthorizedKeysFile = AppPaths.UserSshKeysFile;

    public static void ShowSshInfo()
    {
        AnsiConsole.Write(new Rule("[bold cyan]SSH Info[/]").RuleStyle("grey"));
        if (File.Exists(AuthorizedKeysFile))
        {
            var keys = File.ReadAllLines(AuthorizedKeysFile).Where(l => !string.IsNullOrWhiteSpace(l) && !l.StartsWith('#')).ToArray();
            AnsiConsole.MarkupLine($" Authorized keys: [green]{keys.Length}[/]");
            foreach (var key in keys)
            {
                var parts = key.Split(' ');
                var comment = parts.Length >= 3 ? parts[^1] : "(no comment)";
                AnsiConsole.MarkupLine($" [dim]{parts[0].EscapeMarkup()}[/] [cyan]{comment.EscapeMarkup()}[/]");
            }
        }
        else
        {
            SpectrePanel.Warning("No authorized_keys file found.");
        }
        SystemHelper.ShowSshConnectionInfo();
    }

    public static void ShowTailscaleStatus()
    {
        if (!IsCommandAvailable("tailscale"))
        {
            SpectrePanel.Error("Tailscale CLI is not available on this machine.");
            Console.WriteLine("\nPress any key to return...");
            Console.ReadKey(true);
            return;
        }

        try
        {
            var statusJson = SystemHelper.RunProcess("tailscale", "status --json", capture: true);
            using var doc = JsonDocument.Parse(statusJson);
            var root = doc.RootElement;
            var self = root.GetProperty("Self");
            var dnsName = self.GetProperty("DNSName").GetString();
            var online = self.GetProperty("Online").GetBoolean();
            var tailIp = self.GetProperty("TailscaleIPs")[0].GetString();

            AnsiConsole.MarkupLine($"Tailscale Host: [green]{dnsName}[/]");
            AnsiConsole.MarkupLine($"IP Address: [green]{tailIp}[/]");
            AnsiConsole.MarkupLine($"Status: {(online ? "[green]Online[/]" : "[red]Offline[/]")}");

            if (root.TryGetProperty("Peer", out var peerProp))
            {
                var table = new Table().Border(TableBorder.Rounded);
                table.AddColumn("Peer");
                table.AddColumn("Tailscale IP");
                table.AddColumn("OS");
                table.AddColumn("Status");

                foreach (var peer in peerProp.EnumerateObject())
                {
                    var peerVal = peer.Value;
                    var name = peerVal.GetProperty("HostName").GetString() ?? "";
                    var ip = peerVal.GetProperty("TailscaleIPs")[0].GetString() ?? "";
                    var os = peerVal.GetProperty("OS").GetString() ?? "";
                    var peerOnline = peerVal.GetProperty("Online").GetBoolean();
                    table.AddRow(name, ip, os, peerOnline ? "[green]Active[/]" : "[dim]Idle[/]");
                }
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[bold cyan]Active Peers[/]");
                AnsiConsole.Write(table);
            }
        }
        catch (Exception ex)
        {
            SpectrePanel.Error($"Failed to query Tailscale status: {ex.Message}");
        }

        Console.WriteLine("\nPress any key to return...");
        Console.ReadKey(true);
    }

    public static void ShowSshQrCode()
    {
        string? tailscaleIp = null;
        if (IsCommandAvailable("tailscale"))
        {
            tailscaleIp = SystemHelper.RunProcess("tailscale", "ip -4", capture: true).Trim();
        }
        var ipAddress = !string.IsNullOrEmpty(tailscaleIp) ? tailscaleIp : "127.0.0.1";
        var sshUser = Environment.UserName;
        var sshCmd = $"ssh {sshUser}@{ipAddress}";

        AnsiConsole.Write(new Rule("[bold cyan]SSH Mobile Enrollment[/]").RuleStyle("grey"));
        AnsiConsole.MarkupLine($"Scan this QR code to connect from your phone or run: [yellow]{sshCmd}[/]\n");

        var qrLines = GenerateAsciiQrCode(sshCmd);
        foreach (var line in qrLines)
        {
            AnsiConsole.WriteLine(line);
        }
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"Connection command: [bold green]{sshCmd}[/]");
        Console.WriteLine("\nPress any key to return...");
        Console.ReadKey(true);
    }

    public static void ShowTailscaleServeInfo()
    {
        if (!IsCommandAvailable("tailscale"))
        {
            SpectrePanel.Warning("Tailscale CLI not found on PATH.");
            return;
        }
        var serveStatus = SystemHelper.RunProcess("tailscale", "serve status", capture: true);
        var funnelStatus = SystemHelper.RunProcess("tailscale", "funnel status", capture: true);

        AnsiConsole.Write(new Rule("[bold cyan]Tailscale Serve & Funnel Status[/]").RuleStyle("grey"));
        AnsiConsole.MarkupLine($"[cyan]Serve Status:[/]\n{(string.IsNullOrWhiteSpace(serveStatus) ? "No active Tailscale Serve endpoints." : serveStatus.EscapeMarkup())}\n");
        AnsiConsole.MarkupLine($"[cyan]Funnel Status:[/]\n{(string.IsNullOrWhiteSpace(funnelStatus) ? "No active Tailscale Funnel endpoints." : funnelStatus.EscapeMarkup())}");
    }

    public static string[] GenerateAsciiQrCode(string text)
    {
        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(text, QRCodeGenerator.ECCLevel.L);
        int size = qrCodeData.ModuleMatrix.Count;

        int pad = 2;
        int newSize = size + pad * 2;
        bool[,] grid = new bool[newSize, newSize];
        for (int r = 0; r < size; r++)
        {
            var row = qrCodeData.ModuleMatrix[r];
            for (int c = 0; c < size; c++)
            {
                grid[r + pad, c + pad] = row[c];
            }
        }

        var lines = new List<string>();
        for (int r = 0; r < newSize; r += 2)
        {
            var sb = new StringBuilder();
            sb.Append("  ");
            for (int c = 0; c < newSize; c++)
            {
                bool top = grid[r, c];
                bool bottom = r + 1 < newSize && grid[r + 1, c];
                if (top && bottom) sb.Append('█');
                else if (top) sb.Append('▀');
                else if (bottom) sb.Append('▄');
                else sb.Append(' ');
            }
            lines.Add(sb.ToString());
        }
        return lines.ToArray();
    }

    public static void StartKeyReceiver(int listenPort = 2222)
    {
        StartMobileSshKeyReceiver(listenPort);
    }

    public static void GetConnectionInfo()
    {
        AnsiConsole.MarkupLine("[bold cyan]🌐 Network Connection Status[/]");
        AnsiConsole.MarkupLine("[cyan]===========================[/]");
        string? tailscaleIp = null;
        if (IsCommandAvailable("tailscale"))
        {
            tailscaleIp = SystemHelper.RunProcess("tailscale", "ip -4", capture: true).Trim();
            if (!string.IsNullOrWhiteSpace(tailscaleIp)) AnsiConsole.MarkupLine($" Tailscale IPv4 Address: [green]{tailscaleIp.EscapeMarkup()}[/]");
            else AnsiConsole.MarkupLine(" [yellow][[WARN]] Tailscale is installed but may not be logged in or connected.[/]");
        }
        else
        {
            AnsiConsole.MarkupLine(" [dim]Tailscale is not installed on this machine.[/]");
        }
        var localIps = NetworkInterface.GetAllNetworkInterfaces().Where(n => n.OperationalStatus == OperationalStatus.Up && (n.Name.Contains("Wi-Fi", StringComparison.OrdinalIgnoreCase) || n.Name.Contains("Ethernet", StringComparison.OrdinalIgnoreCase))).SelectMany(n => n.GetIPProperties().UnicastAddresses).Where(a => a.Address.AddressFamily == AddressFamily.InterNetwork).Select(a => a.Address.ToString()).ToArray();
        if (localIps.Length > 0) AnsiConsole.MarkupLine($" Local IPv4 Address(es): [cyan]{string.Join(", ", localIps).EscapeMarkup()}[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold cyan]🔒 Active SSH Sessions[/]");
        AnsiConsole.MarkupLine("[cyan]====================[/]");
        var netstatOut = SystemHelper.RunProcess("netstat", "-ano", capture: true);
        var sshConns = netstatOut.Split('\n').Select(l => l.Trim()).Where(l => l.StartsWith("TCP", StringComparison.OrdinalIgnoreCase)).Select(l => l.Split(' ', StringSplitOptions.RemoveEmptyEntries)).Where(parts => parts.Length >= 5 && parts[1].EndsWith(":22") && parts[3].Equals("ESTABLISHED", StringComparison.OrdinalIgnoreCase)).ToArray();
        if (sshConns.Length > 0)
        {
            foreach (var parts in sshConns)
            {
                var procName = "?";
                if (int.TryParse(parts[4], out var pid))
                {
                    try
                    {
                        using var proc = Process.GetProcessById(pid);
                        procName = proc.ProcessName;
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
        AnsiConsole.MarkupLine($" 1. On your phone (Termux), run: ssh {Environment.UserName.EscapeMarkup()}@<IP>");
        var displayIp = !string.IsNullOrWhiteSpace(tailscaleIp) ? tailscaleIp : "100.x.y.z";
        AnsiConsole.MarkupLine($" 2. Use your Tailscale IP ({displayIp.EscapeMarkup()}) for secure access anywhere.");
        AnsiConsole.MarkupLine(" 3. To authorize a passwordless login key, run: ssh-addkey");
    }

    private static bool IsCommandAvailable(string exe)
    {
        try
        {
            using var p = Process.Start(new ProcessStartInfo("where", exe)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });
            p?.WaitForExit();
            return p?.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    public static void AddAuthorizedKey(string key, string? account = null)
    {
        var targetUser = string.IsNullOrWhiteSpace(account) ? Environment.UserName : account;
        var userHome = AppPaths.UserProfileDir;
        if (!string.Equals(targetUser, Environment.UserName, StringComparison.OrdinalIgnoreCase))
        {
            var usersRoot = Directory.GetParent(userHome)!.FullName;
            userHome = System.IO.Path.Combine(usersRoot, targetUser!);
        }
        if (!Directory.Exists(userHome))
        {
            SpectrePanel.Error($"Home directory for user '{targetUser}' not found at {userHome}.");
            return;
        }
        var sshDir = System.IO.Path.Combine(userHome, ".ssh");
        var authFile = System.IO.Path.Combine(sshDir, "authorized_keys");
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
        var existingKeys = File.ReadAllLines(authFile);
        if (existingKeys.Contains(key))
        {
            AnsiConsole.MarkupLine("[yellow]ℹ️ SSH Key is already authorized.[/]");
            return;
        }
        File.AppendAllText(authFile, key + Environment.NewLine);
        SpectrePanel.Success($"SSH key successfully authorized for user '{targetUser}'.");

        try
        {
            AnsiConsole.MarkupLine("[cyan]🔒 Setting secure permissions on SSH files...[/]");
            const string systemUser = "NT AUTHORITY\\SYSTEM";
            var targetIdentity = $"{Environment.UserDomainName}\\{targetUser}";
            const FileSystemRights fullControl = FileSystemRights.FullControl;
            const AccessControlType allow = AccessControlType.Allow;
            var dirInfo = new DirectoryInfo(sshDir);
            var dirSecurity = dirInfo.GetAccessControl();
            dirSecurity.SetAccessRuleProtection(true, false);
            foreach (FileSystemAccessRule rule in dirSecurity.GetAccessRules(true, true, typeof(System.Security.Principal.NTAccount))) dirSecurity.RemoveAccessRule(rule);
            dirSecurity.AddAccessRule(new FileSystemAccessRule(targetIdentity, fullControl, InheritanceFlags.None, PropagationFlags.None, allow));
            dirSecurity.AddAccessRule(new FileSystemAccessRule(systemUser, fullControl, InheritanceFlags.None, PropagationFlags.None, allow));
            dirInfo.SetAccessControl(dirSecurity);
            var fileInfo = new FileInfo(authFile);
            var fileSecurity = fileInfo.GetAccessControl();
            fileSecurity.SetAccessRuleProtection(true, false);
            foreach (FileSystemAccessRule rule in fileSecurity.GetAccessRules(true, true, typeof(System.Security.Principal.NTAccount))) fileSecurity.RemoveAccessRule(rule);
            fileSecurity.AddAccessRule(new FileSystemAccessRule(targetIdentity, fullControl, InheritanceFlags.None, PropagationFlags.None, allow));
            fileSecurity.AddAccessRule(new FileSystemAccessRule(systemUser, fullControl, InheritanceFlags.None, PropagationFlags.None, allow));
            fileInfo.SetAccessControl(fileSecurity);
            SpectrePanel.Success("Secure OpenSSH file permissions applied.");
        }
        catch (Exception ex)
        {
            SpectrePanel.Warning($"Failed to set secure ACL permissions: {ex.Message}");
        }
    }

    public static void StartMobileSshKeyReceiver(int port = 8999)
    {
        var tsIp = IsCommandAvailable("tailscale") ? SystemHelper.RunProcess("tailscale", "ip -4", capture: true).Trim() : null;
        var localIps = NetworkInterface.GetAllNetworkInterfaces().Where(n => n.OperationalStatus == OperationalStatus.Up && (n.Name.Contains("Wi-Fi", StringComparison.OrdinalIgnoreCase) || n.Name.Contains("Ethernet", StringComparison.OrdinalIgnoreCase))).SelectMany(n => n.GetIPProperties().UnicastAddresses).Where(a => a.Address.AddressFamily == AddressFamily.InterNetwork).Select(a => a.Address.ToString()).ToArray();
        var displayIp = !string.IsNullOrWhiteSpace(tsIp) ? tsIp : localIps.Length > 0 ? localIps[0] : "localhost";

        var oneTimeToken = Guid.NewGuid().ToString("N");
        var enrollmentUrl = $"http://{displayIp}:{port}/?token={oneTimeToken}";

        bool isTailscaleActive = false;
        if (IsCommandAvailable("tailscale"))
        {
            try
            {
                SystemHelper.RunProcess("tailscale", $"serve --bg https / http://localhost:{port}", capture: true);
                isTailscaleActive = true;
            }
            catch { }
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold cyan]📱 Mobile SSH Key Authorizer[/]");
        AnsiConsole.MarkupLine("[cyan]=============================[/]");
        AnsiConsole.MarkupLine("[dim]Starting temporary secure local server to receive your public SSH key...[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[cyan]👉 Scan this QR code or open the link on your mobile phone:[/]");

        var qrLines = GenerateAsciiQrCode(enrollmentUrl);
        foreach (var line in qrLines)
        {
            AnsiConsole.WriteLine(line);
        }
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine($" Link: [green]{enrollmentUrl}[/]");
        AnsiConsole.MarkupLine($" [dim](or http://localhost:{port}/?token={oneTimeToken} if local)[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[yellow]Waiting for connection… (Timeout in 2 minutes. Press Ctrl+C to cancel)[/]");
        AnsiConsole.WriteLine();

        var listener = new HttpListener();
        listener.Prefixes.Add($"http://localhost:{port}/");
        listener.Prefixes.Add($"http://127.0.0.1:{port}/");
        if (!string.IsNullOrWhiteSpace(tsIp))
        {
            try { listener.Prefixes.Add($"http://{tsIp}:{port}/"); } catch { }
        }

        try
        {
            listener.Start();
        }
        catch (Exception ex)
        {
            SpectrePanel.Error($"Failed to start HTTP listener: {ex.Message}. Make sure port {port} is not in use and you have administrator permissions.");
            return;
        }
        var timeout = TimeSpan.FromMinutes(2);
        var start = DateTime.Now;
        var success = false;

        try
        {
            while (DateTime.Now - start < timeout)
            {
                var getContext = listener.BeginGetContext(null, null);
                if (!getContext.AsyncWaitHandle.WaitOne(timeout - (DateTime.Now - start))) break;
                var context = listener.EndGetContext(getContext);
                var request = context.Request;
                var response = context.Response;

                var urlToken = request.QueryString["token"] ?? "";

                if (request.HttpMethod == "GET")
                {
                    WriteHtml(response, FormHtml.Replace("{{TOKEN_PLACEHOLDER}}", urlToken));
                }
                else if (request.HttpMethod == "POST")
                {
                    using var reader = new StreamReader(request.InputStream, Encoding.UTF8);
                    var body = reader.ReadToEnd();
                    var decoded = WebUtility.UrlDecode(body);

                    var parts = decoded.Split('&');
                    string sshKey = "";
                    string receivedToken = "";
                    foreach (var p in parts)
                    {
                        if (p.StartsWith("key=")) sshKey = p[4..];
                        if (p.StartsWith("token=")) receivedToken = p[6..];
                    }

                    sshKey = sshKey.Trim();
                    receivedToken = receivedToken.Trim();

                    var isTokenValid = (receivedToken == oneTimeToken) || (urlToken == oneTimeToken);
                    var isValid = isTokenValid && Regex.IsMatch(sshKey, @"^ssh-(ed25519|rsa|dss|ecdsa) [A-Za-z0-9+/=]+( .+)?$");
                    if (isValid)
                    {
                        AddAuthorizedKey(sshKey);
                        success = true;
                        WriteHtml(response, SuccessHtml);
                    }
                    else
                    {
                        WriteHtml(response, InvalidHtml);
                    }
                    if (success) break;
                }
            }
        }
        finally
        {
            if (isTailscaleActive)
            {
                try
                {
                    SystemHelper.RunProcess("tailscale", "serve https / off", capture: true);
                }
                catch { }
            }
            listener.Stop();
            listener.Close();
            AnsiConsole.MarkupLine("[dim]🛑 Mobile Key Authorizer server stopped.[/]");
        }
    }

    private static void WriteHtml(HttpListenerResponse response, string html)
    {
        var buffer = Encoding.UTF8.GetBytes(html);
        response.ContentLength64 = buffer.Length;
        response.ContentType = "text/html";
        response.OutputStream.Write(buffer, 0, buffer.Length);
        response.OutputStream.Close();
    }

    private const string PageStyle = "body{font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,Helvetica,Arial,sans-serif;background-color:#0f141c;color:#abb2bf;margin:0;padding:20px;display:flex;justify-content:center;align-items:center;min-height:90vh}.container,.card{background-color:#161b22;border-radius:12px;padding:24px;max-width:500px;width:100%;box-shadow:0 4px 12px rgba(0,0,0,.3);border:1px solid #30363d}h2{color:#56b6c2;margin-top:0;font-size:1.5rem;text-align:center}p{font-size:.95rem;line-height:1.5;color:#8b949e}textarea{width:100%;height:120px;box-sizing:border-box;background-color:#0d1117;color:#c9d1d9;border:1px solid #30363d;border-radius:6px;padding:10px;font-family:monospace;font-size:.85rem;resize:vertical;margin-top:10px;margin-bottom:20px}button{width:100%;background-color:#238636;color:#fff;border:none;border-radius:6px;padding:12px;font-size:1rem;font-weight:bold;cursor:pointer}";

    private static readonly string FormHtml = $"<!DOCTYPE html><html><head><meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\"><title>Mobile SSH Key Authorizer</title><style>{PageStyle}</style></head><body><div class=\"container\"><h2>📱 Add SSH Public Key</h2><p>Paste the public SSH key from your mobile phone (e.g. from Termux's <code>~/.ssh/id_ed25519.pub</code>) to authorize connection.</p><form method=\"POST\"><input type=\"hidden\" name=\"token\" value=\"{{TOKEN_PLACEHOLDER}}\"/><textarea name=\"key\" placeholder=\"ssh-ed25519 AAAAC3NzaC1lZDI1NTE5...\" required></textarea><button type=\"submit\">Authorize Key</button></form></div></body></html>";
    private static readonly string SuccessHtml = $"<!DOCTYPE html><html><head><meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\"><title>Success</title><style>{PageStyle}h2{{color:#2ea043}}</style></head><body><div class=\"card\"><h2>✅ Success!</h2><p>The SSH key has been added to authorized_keys and NTFS file permissions have been secured.</p><p>You can close this window now.</p></div></body></html>";
    private static readonly string InvalidHtml = $"<!DOCTYPE html><html><head><meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\"><title>Invalid Key</title><style>{PageStyle}h2{{color:#f85149}}a{{color:#58a6ff}}</style></head><body><div class=\"card\"><h2>❌ Invalid SSH Key Format</h2><p>The key provided does not match a valid public SSH key format.</p><p><a href=\"/\">Go back and try again</a></p></div></body></html>";
}
