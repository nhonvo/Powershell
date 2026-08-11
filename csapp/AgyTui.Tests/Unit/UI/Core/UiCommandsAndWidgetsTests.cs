using AgyTui.UI.Core.Commands;
using AgyTui.UI.Core.Components;
using Spectre.Console;
using Spectre.Console.Rendering;
using Xunit;

namespace AgyTui.Tests.Unit.UI.Core;

public class TestCommand
{
    public string Name { get; set; } = "test";
}

public class TestUiCommandHandler : ICommandHandler<TestCommand>
{
    public bool Executed { get; private set; }

    public Task HandleAsync(TestCommand command, CancellationToken ct = default)
    {
        Executed = true;
        return Task.CompletedTask;
    }
}

public class TestStatusWidget : IStatusWidget
{
    public string Alias => "test";
    public IRenderable Render() => new Text("test");
}

public class UiCommandsAndWidgetsTests
{
    [Fact]
    public async Task UiCommandHandler_HandleAsync_ExecutesSuccessfully()
    {
        var handler = new TestUiCommandHandler();
        await handler.HandleAsync(new TestCommand());
        Assert.True(handler.Executed);
    }

    [Fact]
    public void StatusWidget_Render_ReturnsRenderable()
    {
        IStatusWidget widget = new TestStatusWidget();
        var renderable = widget.Render();
        Assert.NotNull(renderable);
        Assert.Equal("test", widget.Alias);
    }
}

