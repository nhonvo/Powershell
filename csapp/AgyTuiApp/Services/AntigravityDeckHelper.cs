using System;
using System.IO;
using System.Threading;
using Spectre.Console;
using AgyTui.Components;

namespace AgyTui;

public static class AntigravityDeckHelper
{
    private static readonly string DeckPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AntigravityDeck");

    private static bool EnsureDeckPathExists()
    {
        if (!Directory.Exists(DeckPath))
        {
            SpectrePanel.Error($"Antigravity Deck path not found at {DeckPath}. Please install it first.");
            Thread.Sleep(2000);
            return false;
        }
        return true;
    }

    public static void Setup()
    {
        if (!EnsureDeckPathExists()) return;
        AnsiConsole.MarkupLine("[yellow]Running: npm run setup...[/]");
        RunNpmCommand("run", "setup");
    }

    public static void StartLocal()
    {
        if (!EnsureDeckPathExists()) return;
        AnsiConsole.MarkupLine("[yellow]Starting Antigravity Deck (Local dev server on port 3000)...[/]");
        AnsiConsole.MarkupLine("[dim]Press Ctrl+C to terminate the server.[/]");
        RunNpmCommand("run", "dev");
    }

    public static void StartOnline()
    {
        if (!EnsureDeckPathExists()) return;
        AnsiConsole.MarkupLine("[yellow]Starting Antigravity Deck (Cloudflare Tunnel)...[/]");
        AnsiConsole.MarkupLine("[dim]Press Ctrl+C to terminate the server.[/]");
        RunNpmCommand("run", "online");
    }

    private static void RunNpmCommand(string cmd, string arg)
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                Helpers.ProcessRunner.Run("cmd.exe", $"/c npm {cmd} {arg}", DeckPath);
            }
            else
            {
                Helpers.ProcessRunner.Run("npm", $"{cmd} {arg}", DeckPath);
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
