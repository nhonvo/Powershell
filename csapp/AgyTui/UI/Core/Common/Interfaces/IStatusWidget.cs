using Spectre.Console.Rendering;

namespace AgyTui.UI.Core.Common;

public interface IStatusWidget
{
    string Alias { get; }
    IRenderable Render();
}
