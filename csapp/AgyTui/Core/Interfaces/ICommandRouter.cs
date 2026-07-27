namespace AgyTui.Core.Interfaces;

public interface ICommandRouter
{
    int Execute(string alias, string[]? args = null);
}
