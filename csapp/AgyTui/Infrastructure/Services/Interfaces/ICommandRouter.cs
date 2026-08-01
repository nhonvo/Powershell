namespace AgyTui.Infrastructure.Services;

public interface ICommandRouter
{
    int Execute(string alias, string[]? args = null);
}
