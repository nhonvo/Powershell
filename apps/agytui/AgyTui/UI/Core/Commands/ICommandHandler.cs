namespace AgyTui.UI.Core.Commands;

public interface ICommandHandler<in TCommand> where TCommand : class
{
    Task HandleAsync(TCommand command, CancellationToken ct = default);
}
