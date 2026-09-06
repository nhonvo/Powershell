using Spectre.Console.Rendering;

namespace AgyTui.UI.Core.Abstractions;

public interface ILayoutRenderer
{
    IRenderable Render(IScreenView view, ScreenState state);
}
