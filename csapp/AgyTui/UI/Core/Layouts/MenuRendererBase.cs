namespace AgyTui.UI.Core.Layouts;

public abstract class MenuRendererBase : IMenuRenderer
{
    public abstract void Run(MenuNode root);

    private static readonly TtlCache<string, (bool Ai, bool Agy)> _enabledCache = new(TimeSpan.FromSeconds(2));

    protected static MenuNode[] GetActiveChildren(MenuNode parent)
    {
        var (enableAi, enableAgy) = _enabledCache.GetOrCompute("flags", () => (Config.Current.Ai.EnableOllama, Config.Current.Ai.EnableAgy));

        var list = new List<MenuNode>();
        foreach (var child in parent.Children)
        {
            if (child.Kind == MenuNodeKind.Category)
            {
                if (child.Label.Contains("AI Agent & Ollama") && !enableAi) continue;
                if (child.Label.Contains("AGY Account Switch") && !enableAgy) continue;
            }
            else if (child.Kind == MenuNodeKind.Command && child.Command != null)
            {
                if (child.Command.RequiresAiOllama && !enableAi) continue;
                if (child.Command.RequiresAgy && !enableAgy) continue;
            }

            if (child.Id == "agy-cli" && !enableAgy)
            {
                var originalCmd = child.Command!;
                var rewrittenCmd = originalCmd with { DisplayName = "Launch Claude Code CLI (claude)" };
                list.Add(child with { Label = "Launch Claude Code CLI (claude)", Command = rewrittenCmd });
                continue;
            }

            list.Add(child);
        }
        return list.ToArray();
    }

    protected static string[] GetThemeNames()
    {
        var themesPath = Environment.GetEnvironmentVariable("POSH_THEMES_PATH");
        if (string.IsNullOrEmpty(themesPath) || !Directory.Exists(themesPath))
        {
            themesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "asset", "powershell-themes");
            if (!Directory.Exists(themesPath))
            {
                themesPath = Path.Combine(Directory.GetCurrentDirectory(), "asset", "powershell-themes");
            }
        }
        if (!Directory.Exists(themesPath)) return Array.Empty<string>();
        return Directory.GetFiles(themesPath, "*.omp.json").Select(f => Path.GetFileName(f).Replace(".omp.json", "")).OrderBy(f => f).ToArray();
    }

    protected static string DeletePreviousWord(string text)
    {
        if (string.IsNullOrEmpty(text)) return "";
        var trimmed = text.TrimEnd();
        if (string.IsNullOrEmpty(trimmed)) return "";
        int lastSpace = trimmed.LastIndexOf(' ');
        if (lastSpace < 0) return "";
        return trimmed[..lastSpace].TrimEnd();
    }

    public static (int startIdx, int endIdx) ComputeViewport(int totalItems, int selectedIndex, int maxVisibleItems)
    {
        if (totalItems <= 0) return (0, 0);
        if (maxVisibleItems <= 0) maxVisibleItems = 10;

        selectedIndex = Math.Clamp(selectedIndex, 0, totalItems - 1);

        int startIdx = Math.Max(0, selectedIndex - (maxVisibleItems / 2));
        int endIdx = startIdx + maxVisibleItems;

        if (endIdx > totalItems)
        {
            endIdx = totalItems;
            startIdx = Math.Max(0, endIdx - maxVisibleItems);
        }

        return (startIdx, endIdx);
    }
}
