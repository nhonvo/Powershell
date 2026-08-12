namespace AgyTui.UI.Core.Abstractions;

public interface ICommand
{
    string Alias { get; }
    Task<int> ExecuteAsync(string[] args);
}
