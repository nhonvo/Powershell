using AgyTui.Infrastructure.Di;
using Microsoft.Extensions.DependencyInjection;

namespace AgyTui.UI.Core.Navigation;

public static class CcNavigator
{
    public static void Run()
    {
        Config.Load();
        try { Console.Write("\x1b[?1049h\x1b[H"); } catch { }
        try
        {
            var root = MenuNodeBuilder.BuildTree();
            IMenuRenderer renderer = string.Equals(Config.Current.Ui.Mode, "flat-tree", StringComparison.OrdinalIgnoreCase)
                ? Bootstrapper.ServiceProvider.GetRequiredService<FlatTreeRenderer>()
                : Bootstrapper.ServiceProvider.GetRequiredService<ThreePaneRenderer>();
            renderer.Run(root);
        }
        finally
        {
            try { Console.Write("\x1b[?1049l"); } catch { }
        }
    }
}
