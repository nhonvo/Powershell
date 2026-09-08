namespace AgyTui.UI.Core.Commands;

public class UiCommandDispatcher : IUiCommandDispatcher
{
    private readonly IServiceProvider _serviceProvider;

    public UiCommandDispatcher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task DispatchAsync<TCommand>(TCommand command, CancellationToken ct = default) where TCommand : class
    {
        var handler = _serviceProvider.GetService(typeof(ICommandHandler<TCommand>)) as ICommandHandler<TCommand>;
        if (handler != null)
        {
            await handler.HandleAsync(command, ct);
        }
    }
}
