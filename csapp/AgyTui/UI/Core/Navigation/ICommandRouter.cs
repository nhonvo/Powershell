namespace AgyTui.UI.Core.Navigation;

public interface ICommandRouter
{
    int Execute(string alias, string[]? args = null);
}
