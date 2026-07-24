using System;
using AgyTui.Components;

namespace AgyTui;

public static class CcNavigator
{
    public static void Run()
    {
        Config.Load();
        try { Console.Write("\x1b[?1049h\x1b[H"); } catch { }
        try
        {
            var root = MenuNodeBuilder.BuildTree();
            IMenuRenderer renderer = string.Equals(Config.Current.UiMode, "flat-tree", StringComparison.OrdinalIgnoreCase)
                ? new FlatTreeRenderer()
                : new ThreePaneRenderer();
            renderer.Run(root);
        }
        finally
        {
            try { Console.Write("\x1b[?1049l"); } catch { }
        }
    }
}
