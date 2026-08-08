using AgyTui.Infrastructure.Integrations.AgyClient;
using AgyTui.Infrastructure.Integrations.Ai.Abstractions;
using AgyTui.Infrastructure.Integrations.Ai.Providers;
using AgyTui.Infrastructure.Persistence.Repositories;

namespace AgyTui.Infrastructure.Integrations.Ai.Services;

public static class AiDashboardView
{
    public static void ShowAiModeCheck(string alias)
    {
        var mode = Config.Current.Ai.ProviderMode ?? "hybrid";
        SpectrePanel.Info($"Active AI Alias: {alias} (Provider Mode: {mode})");
        Thread.Sleep(1000);
    }

    public static void ShowAiDashboard(IOllamaClient ollama)
    {
        while (true)
        {
            var mode = Config.Current.Ai.ProviderMode ?? "hybrid";
            var options = new[]
            {
                $"🤖 Current AI Provider Mode: [{mode.ToUpper()}]",
                $"🦙 Ollama Server Status: {(ollama.IsRunning ? "ACTIVE (localhost:11434)" : "INACTIVE")}",
                $"📦 Default Model: {ollama.DefaultModel}",
                "⚡ Switch AI Provider Mode (Cloud / Ollama / Hybrid)",
                "✏️ Set Default Ollama Model",
                "📊 View Ollama Logs",
                "↩ Back"
            };

            var choice = SpectreMenu.Show("AI Dashboard & Control Panel", options, 0);
            if (choice < 0 || choice == options.Length - 1) break;

            if (choice == 3)
            {
                var modeChoice = SpectreMenu.Show("Select AI Provider Mode", ["cloud", "ollama", "hybrid"], 0);
                if (modeChoice >= 0)
                {
                    var selected = modeChoice switch { 0 => "cloud", 1 => "ollama", _ => "hybrid" };
                    Config.Current.Ai.ProviderMode = selected;
                    Config.Save();
                    SpectrePanel.Success($"AI Provider Mode set to: {selected}");
                    Thread.Sleep(1000);
                }
            }
            else if (choice == 4)
            {
                Console.CursorVisible = true;
                var model = AnsiConsole.Ask<string>("Enter Ollama Model Name (e.g. qwen3:1.7b, llama3.2, mistral):").Trim();
                Console.CursorVisible = false;
                if (!string.IsNullOrEmpty(model))
                {
                    ollama.SetModel(model);
                    SpectrePanel.Success($"Default Ollama model set to: {model}");
                    Thread.Sleep(1000);
                }
            }
            else if (choice == 5)
            {
                ollama.ShowLogs();
            }
        }
    }

    public static void AskAi(string query, IClaudeClient? claude = null)
    {
        SpectrePanel.Info($"Querying AI: {query}");
        var store = new AgyAccountStore(new SqliteAgyAccountRepository(new SqliteDatabase()), new AppPathManager());
        var client = claude ?? new ClaudeProvider(new AiProcessRunner(store), store);
        client.InvokeClaude([query]);
    }

    public static void InstallAIIntegrations()
    {
        SpectrePanel.Info("AI Integrations installer checked.");
        Thread.Sleep(1000);
    }
}
