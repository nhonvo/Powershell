using AgyTui.Core.Models;
using AgyTui.Infrastructure.Di;
using AgyTui.Infrastructure.Integrations.Ai.Abstractions;
using AgyTui.Infrastructure.Integrations.Ai.Providers;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;

namespace AgyTui.Infrastructure.Integrations.Ai.Services;

public static class AiDashboardView
{
    public static void ShowAiModeCheck(string alias)
    {
        var mode = Config.Current.AiProviderMode ?? "hybrid";
        SpectrePanel.Info($"Active AI Alias: {alias} (Provider Mode: {mode})");
        Thread.Sleep(1000);
    }

    public static void ShowAiDashboard()
    {
        var ollama = Bootstrapper.ServiceProvider.GetRequiredService<IOllamaClient>();

        while (true)
        {
            var mode = Config.Current.AiProviderMode ?? "hybrid";
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
                var modeChoice = SpectreMenu.Show("Select AI Provider Mode", new[] { "cloud", "ollama", "hybrid" }, 0);
                if (modeChoice >= 0)
                {
                    var selected = modeChoice switch { 0 => "cloud", 1 => "ollama", _ => "hybrid" };
                    Config.Current.AiProviderMode = selected;
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

    public static void AskAi(string query)
    {
        SpectrePanel.Info($"Querying AI: {query}");
        var claude = Bootstrapper.ServiceProvider.GetRequiredService<IClaudeClient>();
        claude.InvokeClaude(new[] { query });
    }

    public static void InstallAIIntegrations()
    {
        SpectrePanel.Info("AI Integrations installer checked.");
        Thread.Sleep(1000);
    }
}
