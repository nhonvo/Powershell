using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Text.Json;
using AgyTui.Infrastructure.Integrations.Ai.Abstractions;

namespace AgyTui.Infrastructure.Integrations.Ai.Providers;

public class OllamaClient : IOllamaClient
{
    private static readonly string OllamaDefaultModelFile = Path.Combine(AppPaths.OllamaDataDir, "default_model.txt");
    private readonly IAiProcessRunner _processRunner;
    private string _defaultModel = LoadDefaultModel();

    public OllamaClient(IAiProcessRunner processRunner)
    {
        _processRunner = processRunner;
    }

    public OllamaClient() : this(new Services.AiProcessRunner()) { }

    public string DefaultModel => _defaultModel;

    public bool IsRunning => IsPortListening(11434);

    private static string LoadDefaultModel()
    {
        try
        {
            if (File.Exists(OllamaDefaultModelFile))
            {
                var saved = File.ReadAllText(OllamaDefaultModelFile).Trim();
                if (!string.IsNullOrWhiteSpace(saved)) return saved;
            }
        }
        catch { }
        return "qwen3:1.7b";
    }

    public void SetModel(string? modelName)
    {
        if (!string.IsNullOrWhiteSpace(modelName))
        {
            _defaultModel = modelName.Trim();
            try
            {
                File.WriteAllText(OllamaDefaultModelFile, _defaultModel);
            }
            catch { }
        }
    }

    public bool IsPortListening(int port) => IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners().Any(e => e.Port == port);

    public void EnsureServer()
    {
        if (!IsRunning)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "ollama",
                    Arguments = "serve",
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
            }
            catch { }
        }
    }

    public void InvokeNative(string? model = null)
    {
        EnsureServer();
        var selectedModel = !string.IsNullOrEmpty(model) ? model : DefaultModel;
        _processRunner.RunInteractive("ollama", new[] { "run", selectedModel });
    }

    public void ShowLogs()
    {
        var logPath = Path.Combine(AppPaths.LocalAppDataDir, "Ollama", "server.log");
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

    public void ManageModels()
    {
        if (!IsRunning)
        {
            SpectrePanel.Error("Ollama daemon is offline.");
            Thread.Sleep(1500);
            return;
        }

        try
        {
            var client = HttpClientProvider.Instance.Client;
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
                        var jsonPayload = JsonSerializer.Serialize(new { name = modelName });
                        request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
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
                    var requestBody = JsonSerializer.Serialize(new { name = modelName });
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

    public void BenchmarkModels()
    {
        if (!IsRunning)
        {
            SpectrePanel.Error("Ollama daemon is offline.");
            Thread.Sleep(1500);
            return;
        }

        try
        {
            var response = HttpClientProvider.Instance.Client.GetStringAsync("http://127.0.0.1:11434/api/tags").Result;
            using var doc = JsonDocument.Parse(response);
            if (!doc.RootElement.TryGetProperty("models", out var modelsProp) || modelsProp.ValueKind != JsonValueKind.Array || modelsProp.GetArrayLength() == 0)
            {
                SpectrePanel.Warning("No local models found to benchmark.");
                Thread.Sleep(1500);
                return;
            }

            AnsiConsole.Clear();
            AnsiConsole.MarkupLine("[cyan bold]Ollama Model Benchmark[/]\n");

            var table = new Table().Border(TableBorder.Rounded);
            table.AddColumn("[bold]Model[/]");
            table.AddColumn("[bold]Size (GB)[/]");
            table.AddColumn("[bold]Latency (s)[/]");
            table.AddColumn("[bold]Status[/]");

            AnsiConsole.MarkupLine("[dim]Starting benchmark run... this sends a short prompt to each model to measure latency.[/]\n");

            foreach (var m in modelsProp.EnumerateArray())
            {
                var name = m.GetProperty("name").GetString() ?? "";
                long sizeBytes = 0;
                if (m.TryGetProperty("size", out var sizeProp)) sizeBytes = sizeProp.GetInt64();
                var sizeGb = Math.Round(sizeBytes / (1024.0 * 1024.0 * 1024.0), 2);

                AnsiConsole.Markup($"Testing [yellow]{name}[/]... ");

                var startTime = DateTime.UtcNow;
                var requestBody = JsonSerializer.Serialize(new
                {
                    model = name,
                    prompt = "Explain gravity in 5 words.",
                    stream = false
                });

                try
                {
                    var postTask = HttpClientProvider.Instance.Client.PostAsync(
                        "http://127.0.0.1:11434/api/generate",
                        new StringContent(requestBody, Encoding.UTF8, "application/json")
                    );

                    if (postTask.Wait(TimeSpan.FromSeconds(10)))
                    {
                        var res = postTask.Result;
                        if (res.IsSuccessStatusCode)
                        {
                            var elapsed = (DateTime.UtcNow - startTime).TotalSeconds;
                            table.AddRow(name, sizeGb.ToString("F2"), elapsed.ToString("F2"), "[green]Success[/]");
                            AnsiConsole.MarkupLine($"[green]Done ({elapsed:F2}s)[/]");
                        }
                        else
                        {
                            table.AddRow(name, sizeGb.ToString("F2"), "--", $"[red]HTTP {res.StatusCode}[/]");
                            AnsiConsole.MarkupLine($"[red]Failed ({res.StatusCode})[/]");
                        }
                    }
                    else
                    {
                        table.AddRow(name, sizeGb.ToString("F2"), "--", "[red]Timeout[/]");
                        AnsiConsole.MarkupLine("[red]Timeout (10s)[/]");
                    }
                }
                catch (Exception ex)
                {
                    table.AddRow(name, sizeGb.ToString("F2"), "--", $"[red]Error[/]");
                    AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
                }
            }

            AnsiConsole.WriteLine();
            AnsiConsole.Write(table);
        }
        catch (Exception ex)
        {
            SpectrePanel.Error($"Benchmark failed: {ex.Message}");
        }

        Console.WriteLine("\nPress any key to return...");
        Console.ReadKey(true);
    }

    public void PullModel()
    {
        if (!IsRunning)
        {
            SpectrePanel.Error("Ollama daemon is offline.");
            Thread.Sleep(1500);
            return;
        }

        var modelName = AnsiConsole.Ask<string>("Enter Ollama model name to pull (e.g. qwen2.5:coder, llama3):").Trim();
        if (string.IsNullOrEmpty(modelName)) return;

        AnsiConsole.MarkupLine($"[yellow]Starting pull command: ollama pull {modelName.EscapeMarkup()}[/]");
        try
        {
            ProcessRunner.Instance.RunInteractive("ollama", new[] { "pull", modelName });
            SpectrePanel.Success($"Model '{modelName}' pull completed.");
        }
        catch (Exception ex)
        {
            SpectrePanel.Error($"Failed to run pull command: {ex.Message}");
        }
        Console.WriteLine("\nPress any key to return...");
        Console.ReadKey(true);
    }

    public void StartDaemon()
    {
        if (IsRunning)
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
                if (IsRunning)
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
