using Spectre.Console.Rendering;

namespace AgyTui.UI.Core.Components.Abstractions;

public interface IStatusWidget
{
    string Alias { get; }
    IRenderable Render();
}
