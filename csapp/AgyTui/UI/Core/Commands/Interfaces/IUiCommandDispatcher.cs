namespace AgyTui.UI.Core.Commands;

public interface IUiCommandDispatcher
{
    Task DispatchAsync<TCommand>(TCommand command, CancellationToken ct = default) where TCommand : class;
}
