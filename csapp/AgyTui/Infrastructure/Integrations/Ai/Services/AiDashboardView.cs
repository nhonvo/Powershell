using AgyTui.Infrastructure.Integrations.Ai.Providers;
using Spectre.Console;

namespace AgyTui.Infrastructure.Integrations.Ai.Services;

public static class AiDashboardView
{
    public static void ShowAiModeCheck(string alias)
    {
        SpectrePanel.Info($"Active AI Alias: {alias} (Provider Mode: {AgyAiCore.GetEffectiveProviderMode()})");
        Thread.Sleep(1000);
    }

    public static void ShowAiDashboard()
    {
        while (true)
        {
            var options = new[]
            {
                $"🤖 Current AI Provider Mode: [{AgyAiCore.GetEffectiveProviderMode().ToUpper()}]",
                $"🦙 Ollama Server Status: {(OllamaProvider.IsOllamaRunning() ? "ACTIVE (localhost:11434)" : "INACTIVE")}",
                $"📦 Default Model: {OllamaProvider.OllamaDefaultModel}",
                "⚡ Switch AI Provider Mode (Cloud / Ollama / Hybrid)",
                "✏️ Set Default Ollama Model",
                "📊 View Ollama Logs",
                "↩ Back"
            };

            var choice = SpectreMenu.Show("AI Dashboard & Control Panel", options, 0);
            if (choice < 0 || choice == options.Length - 1) break;

            if (choice == 3)
            {
                var modeChoice = SpectreMenu.Show("Select AI Provider Mode", new[] { "cloud", "ollama", "hybrid" }, 0);
                if (modeChoice >= 0)
                {
                    var selected = modeChoice switch { 0 => "cloud", 1 => "ollama", _ => "hybrid" };
                    AgyAiCore.SetAiProviderMode(selected);
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
                    OllamaProvider.SetOllamaModel(model);
                    SpectrePanel.Success($"Default Ollama model set to: {model}");
                    Thread.Sleep(1000);
                }
            }
            else if (choice == 5)
            {
                OllamaProvider.ShowOllamaLogs();
            }
        }
    }

    public static void AskAi(string query)
    {
        SpectrePanel.Info($"Querying AI: {query}");
        AgyServices.Claude.InvokeClaude(new[] { query });
    }

    public static void InstallAIIntegrations()
    {
        SpectrePanel.Info("AI Integrations installer checked.");
        Thread.Sleep(1000);
    }
}
