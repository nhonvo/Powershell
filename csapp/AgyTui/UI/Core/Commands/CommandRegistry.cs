using System.Collections.Concurrent;

namespace AgyTui.UI.Core.Commands;

public class CommandRegistry
{
    private readonly ConcurrentDictionary<string, ICommand> _commands = new(StringComparer.OrdinalIgnoreCase);

    public void Register(ICommand command)
    {
        _commands[command.Alias] = command;
    }

    public void Register(IEnumerable<ICommand> commands)
    {
        foreach (var cmd in commands)
        {
            Register(cmd);
        }
    }

    public ICommand? Get(string alias)
    {
        _commands.TryGetValue(alias, out var cmd);
        return cmd;
    }

    public IEnumerable<ICommand> GetAll() => _commands.Values;
}
