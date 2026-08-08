namespace AgyTui.UI.Core.Commands;

public interface ICommand
{
    string Alias { get; }
    Task<int> ExecuteAsync(string[] args);
}
