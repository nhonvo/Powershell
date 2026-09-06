namespace AgyTui.UI.Core.Abstractions;

public interface ICommandRouter
{
    int Execute(string alias, string[]? args = null);
}
