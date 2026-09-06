using Spectre.Console.Rendering;

namespace AgyTui.UI.Core.Navigation;

public class ScreenNavigator
{
    public static void RunScreen(IScreenView screen)
    {
        if (screen == null) return;
        var state = new ScreenState("", 0, 0);

        try { Console.Write("\x1b[?1049h\x1b[H"); } catch { }

        try
        {
            while (true)
            {
                int itemCount = screen.GetItemCount(state.SearchFilter);
                if (itemCount > 0 && state.SelectedIndex >= itemCount)
                {
                    state = state with { SelectedIndex = itemCount - 1 };
                }
                if (state.SelectedIndex < 0) state = state with { SelectedIndex = 0 };

                var grid = new Grid().AddColumn(new GridColumn().NoWrap());
                IRenderable content = screen.Render(grid, state);
                ScreenChrome.WriteSmooth(content);

                if (!Console.KeyAvailable)
                {
                    Thread.Sleep(20);
                }

                var key = Console.ReadKey(true);
                var result = screen.HandleInput(key, state);

                if (result.Action == NavigationAction.Exit)
                {
                    break;
                }

                if (key.Key == ConsoleKey.UpArrow)
                {
                    if (state.SelectedIndex > 0)
                    {
                        state = state with { SelectedIndex = state.SelectedIndex - 1 };
                    }
                }
                else if (key.Key == ConsoleKey.DownArrow)
                {
                    if (state.SelectedIndex < itemCount - 1)
                    {
                        state = state with { SelectedIndex = state.SelectedIndex + 1 };
                    }
                }
            }
        }
        finally
        {
            try { Console.Write("\x1b[?1049l"); } catch { }
        }
    }
}

