using System;
using System.IO;
using AgyTui.Helpers;

namespace AgyTui;

public sealed record IdeCommand(string Name, string ArgHint, string Description, string Category, Action<IdeContext, string[]> Run);

public sealed class IdeContext
{
    public string RootPath { get; }
    public string? CurrentFile { get; set; }

    public IdeContext(string rootPath, string? currentFile)
    {
        RootPath = rootPath;
        CurrentFile = currentFile;
    }
}

public static class IdeCommandRegistry
{
    public static readonly IdeCommand[] All = {
        new("open", "<path>", "Open a file", "Navigation", (ctx, a) => {
            if (a.Length > 0)
            {
                var full = Path.GetFullPath(Path.Combine(ctx.RootPath, a[0]));
                var rootFull = Path.GetFullPath(ctx.RootPath);
                if (full.StartsWith(rootFull, StringComparison.OrdinalIgnoreCase))
                {
                    ctx.CurrentFile = full;
                }
                else
                {
                    Components.SpectrePanel.Error("Access denied: path outside workspace root.");
                }
            }
        }),
        new("diff", "[path]", "Show git diff", "Git", (ctx, a) => {
            string file = a.Length > 0 ? a[0] : (ctx.CurrentFile ?? "");
            GitDiffViewer.ShowDiff(ctx.RootPath, file);
        }),
        new("symbols", "", "Browse symbols in current file", "Search", (ctx, _) => {
            if (ctx.CurrentFile != null) SymbolSearch.BrowseSymbols(ctx.CurrentFile);
        }),
        new("edit", "", "Open current file in $EDITOR", "Navigation", (ctx, _) => {
            if (ctx.CurrentFile != null) ProcessRunner.Run(EditorResolver.Resolve(), $"\"{ctx.CurrentFile}\"");
        }),
        new("ask", "[question]", "Ask AI about the current file", "AI", (ctx, a) => {
            if (ctx.CurrentFile != null && File.Exists(ctx.CurrentFile)) {
                string q = a.Length > 0 ? string.Join(' ', a) : "Explain this file";
                string content = File.ReadAllText(ctx.CurrentFile);
                if (content.Length > 8000) content = content[..8000] + "\n...(truncated)";
                AgyAiCore.AskAi($"Regarding the file '{ctx.CurrentFile}', question: {q}\n\nFile Content:\n{content}");
            }
        }),
    };
}
