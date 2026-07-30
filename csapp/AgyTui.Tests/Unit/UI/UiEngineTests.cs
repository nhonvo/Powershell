using AgyTui.UI.Core.Commands;
using AgyTui.UI.Core.State;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AgyTui.Tests.Unit.UI;

public record SampleTestCommand(string Message);

public class SampleTestCommandHandler : ICommandHandler<SampleTestCommand>
{
    public static string? HandledMessage { get; private set; }

    public Task HandleAsync(SampleTestCommand command, CancellationToken ct = default)
    {
        HandledMessage = command.Message;
        return Task.CompletedTask;
    }
}

public class UiEngineTests
{
    [Fact]
    public void UiStateStore_UpdatesState_AndFiresEvent()
    {
        var store = new UiStateStore();
        bool fired = false;
        store.OnStateChanged += state => fired = true;

        store.Update(s => s with { ActiveAccount = "test_acc", IsCompactMode = true });

        Assert.Equal("test_acc", store.Current.ActiveAccount);
        Assert.True(store.Current.IsCompactMode);
        Assert.True(fired);
    }

    [Fact]
    public async Task UiCommandDispatcher_DispatchesToRegisteredHandler()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ICommandHandler<SampleTestCommand>, SampleTestCommandHandler>();
        services.AddSingleton<IUiCommandDispatcher, UiCommandDispatcher>();
        var provider = services.BuildServiceProvider();

        var dispatcher = provider.GetRequiredService<IUiCommandDispatcher>();
        await dispatcher.DispatchAsync(new SampleTestCommand("Hello World"));

        Assert.Equal("Hello World", SampleTestCommandHandler.HandledMessage);
    }
}
