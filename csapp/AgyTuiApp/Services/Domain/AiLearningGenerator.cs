using System;
using System.Diagnostics;
using System.IO;
using AgyTui.Components;
using Spectre.Console;

namespace AgyTui;

public static class AiLearningGenerator
{
    public static void RunGenerator(string domain = "")
    {
        AnsiConsole.Clear();
        SpectrePanel.Info("🤖 AI Deep Learning Content Generator");

        var domains = new[]
        {
            "🎴 Certifications (Flashcard Deck)",
            "🎌 Japanese (JLPT & Grammar)",
            "💻 C# & .NET 9 (Quiz Questions)",
            "💼 Career (STAR Interview Answers)"
        };

        if (string.IsNullOrWhiteSpace(domain))
        {
            int idx = SpectreMenu.ShowWithEscape("Select Domain to Generate Content For", domains, 0);
            if (idx < 0) return;
            domain = domains[idx];
        }

        var providers = new[]
        {
            "🛸 Google Antigravity CLI (agy)",
            "🧠 Claude Code CLI (claude)"
        };

        int pIdx = SpectreMenu.ShowWithEscape("Select AI Generation Engine", providers, 0);
        if (pIdx < 0) return;
        string provider = providers[pIdx];

        string topic = AnsiConsole.Ask<string>("Enter specific [cyan]topic/subject[/] (e.g. AWS SAA-C03, C# Async/Await, JLPT N3 Passive):");
        if (string.IsNullOrWhiteSpace(topic)) return;

        SpectrePanel.Info($"Generating content for '{topic.EscapeMarkup()}' using {provider.EscapeMarkup()} in automated mode...");

        string isAgy = provider.Contains("Antigravity") ? "agy" : "claude";
        string targetFile = GetTargetFilePath(domain, topic);

        string promptText = $"Generate deep learning content for topic: {topic}. Output clean JSON format suitable for flashcards/quizzes.";

        bool success = ExecuteCliGenerator(isAgy, promptText, targetFile);

        if (success)
        {
            SpectrePanel.Success($"AI generation complete! Data saved to: {targetFile.EscapeMarkup()}");
            SpectrePanel.Info("Auto-refreshing local Learning Suite indices...");
            LearnRouter.RefreshData("all");
        }
        else
        {
            SpectrePanel.Warning($"AI content generation finished with fallbacks. Check {targetFile.EscapeMarkup()}");
        }
    }

    private static string GetTargetFilePath(string domain, string topic)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        string cleanTopic = string.Concat(topic.Where(c => !invalidChars.Contains(c))).Trim();
        string safeTopic = cleanTopic.ToLowerInvariant().Replace(" ", "_").Replace("-", "_");
        if (string.IsNullOrEmpty(safeTopic)) safeTopic = "topic_" + Guid.NewGuid().ToString("N")[..6];

        LearnDataPaths.EnsureDirectories();

        if (domain.Contains("Certifications"))
            return Path.Combine(LearnDataPaths.DecksDir, $"deck_{safeTopic}.json");
        if (domain.Contains("Japanese"))
            return Path.Combine(LearnDataPaths.JapaneseDir, $"grammar_{safeTopic}.json");
        if (domain.Contains("C#"))
            return Path.Combine(LearnDataPaths.CsharpDir, $"quiz_{safeTopic}.json");

        return Path.Combine(LearnDataPaths.CareerDir, $"interview_{safeTopic}.json");
    }

    private static bool ExecuteCliGenerator(string cliName, string promptText, string targetFile)
    {
        try
        {
            var exeName = cliName == "agy" ? "agy" : (OperatingSystem.IsWindows() ? "claude.cmd" : "claude");

            var psi = new ProcessStartInfo
            {
                FileName = exeName,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            psi.ArgumentList.Add("--prompt");
            psi.ArgumentList.Add(promptText);

            using var proc = Process.Start(psi);
            if (proc == null) return false;

            string output = proc.StandardOutput.ReadToEnd();
            proc.WaitForExit();

            if (!string.IsNullOrWhiteSpace(output))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(targetFile)!);
                File.WriteAllText(targetFile, output);
                return true;
            }
        }
        catch (Exception ex)
        {
            SpectrePanel.Error($"CLI execution warning: {ex.Message.EscapeMarkup()}");
        }
        return false;
    }
}
