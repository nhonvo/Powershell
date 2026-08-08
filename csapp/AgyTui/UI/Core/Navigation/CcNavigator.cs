using AgyTui.Infrastructure.Di;
using AgyTui.UI.Core.Layouts;
using AgyTui.UI.Core.Layouts.Interfaces;
using AgyTui.UI.Core.Navigation.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace AgyTui.UI.Core.Navigation;

public class CcNavigatorService : ICcNavigator
{
    private readonly FlatTreeRenderer _flatTreeRenderer;
    private readonly ThreePaneRenderer _threePaneRenderer;

    public CcNavigatorService(FlatTreeRenderer flatTreeRenderer, ThreePaneRenderer threePaneRenderer)
    {
        _flatTreeRenderer = flatTreeRenderer;
        _threePaneRenderer = threePaneRenderer;
    }

    public void Run()
    {
        Config.Load();
        try { Console.Write("\x1b[?1049h\x1b[H"); } catch { }
        try
        {
            var root = MenuNodeBuilder.BuildTree();
            IMenuRenderer renderer = string.Equals(Config.Current.Ui.Mode, "flat-tree", StringComparison.OrdinalIgnoreCase)
                ? (IMenuRenderer)_flatTreeRenderer
                : (IMenuRenderer)_threePaneRenderer;
            renderer.Run(root);
        }
        finally
        {
            try { Console.Write("\x1b[?1049l"); } catch { }
        }
    }
}

public static class CcNavigator
{
    private static readonly ICcNavigator _service = new CcNavigatorService(new FlatTreeRenderer(), new ThreePaneRenderer());
    public static ICcNavigator Instance => _service;

    public static void Run() => _service.Run();
}
