using Spectre.Console.Rendering;

namespace AgyTui.UI.Core.Navigation;

public class SubPageNavigatorService : ISubPageNavigator
{
    private readonly IAgyAccountStore _accountStore;

    public SubPageNavigatorService(IAgyAccountStore? accountStore = null)
    {
        _accountStore = accountStore ?? new AgyAccountStore();
    }

    private static string _detailsSearchBuffer = "";
    private static bool _isSearchActive = false;

    public string ProcessSearchKey(ConsoleKeyInfo key, string currentBuffer)
    {
        if (key.Key == ConsoleKey.Backspace)
        {
            return currentBuffer.Length > 0 ? currentBuffer[..^1] : "";
        }
        if (!char.IsControl(key.KeyChar) && key.KeyChar != '\0')
        {
            return currentBuffer + key.KeyChar;
        }
        return currentBuffer;
    }

    public void RunScreen(IScreenView screenView, string initialQuery = "")
    {
        if (screenView == null) return;
        string searchBuffer = initialQuery;
        int selectedIndex = 0;

        Console.CursorVisible = false;
        try { Console.Write("\x1b[?1049h\x1b[H"); } catch { }

        try
        {
            while (true)
            {
                int itemCount = screenView.GetItemCount(searchBuffer);
                if (selectedIndex < 0) selectedIndex = 0;
                if (itemCount > 0 && selectedIndex >= itemCount) selectedIndex = itemCount - 1;

                var grid = new Grid().AddColumn(new GridColumn().NoWrap());
                var state = new ScreenState(searchBuffer, selectedIndex);
                var renderable = screenView.Render(grid, state);

                AnsiConsole.Clear();
                AnsiConsole.Write(renderable);

                var key = Console.ReadKey(true);
                switch (key.Key)
                {
                    case ConsoleKey.UpArrow:
                    case ConsoleKey.K:
                        if (itemCount > 0) selectedIndex = (selectedIndex - 1 + itemCount) % itemCount;
                        continue;

                    case ConsoleKey.DownArrow:
                    case ConsoleKey.J:
                        if (itemCount > 0) selectedIndex = (selectedIndex + 1) % itemCount;
                        continue;

                    case ConsoleKey.Backspace:
                        if (searchBuffer.Length > 0)
                        {
                            searchBuffer = searchBuffer[..^1];
                            selectedIndex = 0;
                        }
                        continue;
                }

                var result = screenView.HandleInput(key, state);
                if (result.Action == NavigationAction.Exit)
                {
                    break;
                }
                if (result.Action == NavigationAction.Handled)
                {
                    continue;
                }

                if (!char.IsControl(key.KeyChar) && key.KeyChar != '\0')
                {
                    searchBuffer += key.KeyChar;
                    selectedIndex = 0;
                    continue;
                }
            }
        }
        finally
        {
            try { Console.Write("\x1b[?1049l"); } catch { }
            Console.CursorVisible = true;
        }
    }

    public void Run(string mode, string initialQuery = "")
    {
        mode = mode.ToLowerInvariant();
        int detailsSel = 0;

        if (mode == "agyswitch")
        {
            var accs = _accountStore?.GetAccounts() ?? [];
            var activeAcc = _accountStore?.GetActiveAccount();
            detailsSel = Array.IndexOf(accs, activeAcc);
            if (detailsSel < 0) detailsSel = 0;
        }
        else if (mode == "theme")
        {
            var themeFiles = SubPageThemeNavigator.GetThemeNames();
            var currentTheme = Environment.GetEnvironmentVariable("THEME");
            detailsSel = Array.IndexOf(themeFiles, currentTheme);
            if (detailsSel < 0) detailsSel = 0;
        }
        else if (mode == "proj")
        {
            SubPageProjNavigator.SelectedWorkspaceIndex = 0;
            SubPageProjNavigator.SelectedActionIndex = -1;
            SubPageProjNavigator.ExpandedWorkspaceIndex = -1;
        }

        _detailsSearchBuffer = initialQuery;

        while (true)
        {
            int itemsCount = 0;
            var flatList = new List<SubPageProjNavigator.FlatItem>();
            var workspaces = Array.Empty<WorkspaceEntry>();

            if (mode == "agyswitch")
            {
                var accs = _accountStore?.GetAccounts() ?? [];
                if (!string.IsNullOrEmpty(_detailsSearchBuffer))
                {
                    accs = accs.Where(a => a.Contains(_detailsSearchBuffer, StringComparison.OrdinalIgnoreCase)).ToArray();
                }
                itemsCount = accs.Length;
            }
            else if (mode == "theme")
            {
                var themes = SubPageThemeNavigator.GetThemeNames();
                if (!string.IsNullOrEmpty(_detailsSearchBuffer))
                {
                    themes = themes.Where(t => t.Contains(_detailsSearchBuffer, StringComparison.OrdinalIgnoreCase)).ToArray();
                }
                itemsCount = themes.Length;
            }
            else if (mode == "learn" || mode == "session" || mode == "weak")
            {
                var topics = new[] { "jp", "en", "cs", "dsa", "interview", "[Type Custom Topic...]" };
                if (!string.IsNullOrEmpty(_detailsSearchBuffer))
                {
                    topics = topics.Where(t => t.Contains(_detailsSearchBuffer, StringComparison.OrdinalIgnoreCase)).ToArray();
                }
                itemsCount = topics.Length;
            }
            else if (mode == "favorite")
            {
                var favItems = SubPageFavoriteNavigator.GetFavoriteItems(_detailsSearchBuffer);
                itemsCount = favItems.Count;
            }
            else if (mode == "proj")
            {
                workspaces = WorkspaceRegistry.GetWorkspaces();
                if (!string.IsNullOrEmpty(_detailsSearchBuffer))
                {
                    workspaces = workspaces.Where(w => w != null &&
                        ((w.Name != null && SystemHelper.Instance.IsFuzzyMatch(w.Name, _detailsSearchBuffer)) ||
                         (w.WorkspacePath != null && SystemHelper.Instance.IsFuzzyMatch(w.WorkspacePath, _detailsSearchBuffer)))).ToArray();
                }

                flatList = SubPageProjNavigator.GetFlatList(workspaces, _detailsSearchBuffer);
                itemsCount = flatList.Count;

                if (detailsSel < 0) detailsSel = 0;
                if (itemsCount > 0 && detailsSel >= itemsCount) detailsSel = itemsCount - 1;
            }

            ScreenChrome.RenderFrame(() =>
            {
                RenderSubPageSelection(mode, detailsSel, workspaces, flatList);
            }, forceClear: false);

            ScreenChrome.EnableMouseTracking();
            var (key, isScrollUp, isScrollDown) = ScreenChrome.ReadKeyWithMouse();

            if (isScrollUp && itemsCount > 0)
            {
                detailsSel = Math.Max(0, detailsSel - 3);
                continue;
            }
            if (isScrollDown && itemsCount > 0)
            {
                detailsSel = Math.Min(itemsCount - 1, detailsSel + 3);
                continue;
            }

            switch (key.Key)
            {
                case ConsoleKey.UpArrow:
                    if (itemsCount > 0) detailsSel = (detailsSel - 1 + itemsCount) % itemsCount;
                    break;
                case ConsoleKey.K:
                    if (string.IsNullOrEmpty(_detailsSearchBuffer))
                    {
                        if (itemsCount > 0) detailsSel = (detailsSel - 1 + itemsCount) % itemsCount;
                    }
                    else
                    {
                        _detailsSearchBuffer += key.KeyChar;
                        detailsSel = 0;
                        if (mode == "proj")
                        {
                            SubPageProjNavigator.SelectedWorkspaceIndex = 0;
                            SubPageProjNavigator.SelectedActionIndex = -1;
                            SubPageProjNavigator.ExpandedWorkspaceIndex = -1;
                        }
                    }
                    break;
                case ConsoleKey.DownArrow:
                    if (itemsCount > 0) detailsSel = (detailsSel + 1) % itemsCount;
                    break;
                case ConsoleKey.J:
                    if (string.IsNullOrEmpty(_detailsSearchBuffer))
                    {
                        if (itemsCount > 0) detailsSel = (detailsSel + 1) % itemsCount;
                    }
                    else
                    {
                        _detailsSearchBuffer += key.KeyChar;
                        detailsSel = 0;
                        if (mode == "proj")
                        {
                            SubPageProjNavigator.SelectedWorkspaceIndex = 0;
                            SubPageProjNavigator.SelectedActionIndex = -1;
                            SubPageProjNavigator.ExpandedWorkspaceIndex = -1;
                        }
                    }
                    break;
                case ConsoleKey.PageUp:
                    if (itemsCount > 0) detailsSel = Math.Max(0, detailsSel - ScrollableListView.GetPageStep(Math.Max(3, Console.WindowHeight - 19)));
                    break;
                case ConsoleKey.PageDown:
                    if (itemsCount > 0) detailsSel = Math.Min(itemsCount - 1, detailsSel + ScrollableListView.GetPageStep(Math.Max(3, Console.WindowHeight - 19)));
                    break;
                case ConsoleKey.Home:
                    detailsSel = 0;
                    break;
                case ConsoleKey.End:
                    if (itemsCount > 0) detailsSel = Math.Max(0, itemsCount - 1);
                    break;
                case ConsoleKey.Backspace:
                    if (!string.IsNullOrEmpty(_detailsSearchBuffer))
                    {
                        if (key.Modifiers.HasFlag(ConsoleModifiers.Control))
                        {
                            _detailsSearchBuffer = DeletePreviousWord(_detailsSearchBuffer);
                        }
                        else
                        {
                            _detailsSearchBuffer = _detailsSearchBuffer[..^1];
                        }
                        detailsSel = 0;
                        if (mode == "proj")
                        {
                            SubPageProjNavigator.SelectedWorkspaceIndex = 0;
                            SubPageProjNavigator.SelectedActionIndex = -1;
                            SubPageProjNavigator.ExpandedWorkspaceIndex = -1;
                        }
                    }
                    break;
                case ConsoleKey.Oem2:
                case ConsoleKey.Divide:
                    if (key.KeyChar == '/')
                    {
                        _isSearchActive = !_isSearchActive;
                        break;
                    }
                    break;
                case ConsoleKey.Enter:
                    if (_isSearchActive)
                    {
                        _isSearchActive = false;
                    }
                    if (mode == "proj")
                    {
                        bool shouldExit = SubPageProjNavigator.HandleEnter(workspaces, flatList, detailsSel, _detailsSearchBuffer);
                        if (shouldExit) return;
                    }
                    else if (mode == "theme")
                    {
                        bool shouldExit = SubPageThemeNavigator.HandleSelection(_detailsSearchBuffer, detailsSel);
                        if (shouldExit) return;
                    }
                    else if (mode == "agyswitch")
                    {
                        bool shouldExit = SubPageAccountNavigator.HandleSelection(_detailsSearchBuffer, detailsSel);
                        if (shouldExit) return;
                    }
                    else if (mode == "learn" || mode == "session" || mode == "weak")
                    {
                        bool shouldExit = SubPageTopicNavigator.HandleSelection(mode, _detailsSearchBuffer, detailsSel);
                        if (shouldExit) return;
                    }
                    else if (mode == "favorite")
                    {
                        bool shouldExit = SubPageFavoriteNavigator.HandleSelection(_detailsSearchBuffer, detailsSel);
                        if (shouldExit) return;
                    }
                    break;

                case ConsoleKey.Escape:
                case ConsoleKey.Q:
                    if (_isSearchActive || !string.IsNullOrEmpty(_detailsSearchBuffer))
                    {
                        _isSearchActive = false;
                        _detailsSearchBuffer = "";
                        detailsSel = 0;
                        if (mode == "proj")
                        {
                            SubPageProjNavigator.SelectedWorkspaceIndex = 0;
                            SubPageProjNavigator.SelectedActionIndex = -1;
                            SubPageProjNavigator.ExpandedWorkspaceIndex = -1;
                        }
                    }
                    else
                    {
                        return;
                    }
                    break;

                case ConsoleKey.A:
                    if (mode == "agyswitch" && !_isSearchActive)
                    {
                        SubPageAccountNavigator.CreateAccount(_accountStore);
                        detailsSel = 0;
                        break;
                    }
                    if (_isSearchActive || !string.IsNullOrEmpty(_detailsSearchBuffer))
                    {
                        _detailsSearchBuffer += key.KeyChar;
                        detailsSel = 0;
                    }
                    break;

                case ConsoleKey.D:
                    if (mode == "agyswitch" && !_isSearchActive)
                    {
                        SubPageAccountNavigator.DeleteAccount(_detailsSearchBuffer, detailsSel, _accountStore);
                        detailsSel = Math.Max(0, detailsSel - 1);
                        break;
                    }
                    if (_isSearchActive || !string.IsNullOrEmpty(_detailsSearchBuffer))
                    {
                        _detailsSearchBuffer += key.KeyChar;
                        detailsSel = 0;
                    }
                    break;

                case ConsoleKey.L:
                    if (mode == "agyswitch" && !_isSearchActive)
                    {
                        SubPageAccountNavigator.LoginAccount(_detailsSearchBuffer, detailsSel, _accountStore);
                        break;
                    }
                    if (_isSearchActive || !string.IsNullOrEmpty(_detailsSearchBuffer))
                    {
                        _detailsSearchBuffer += key.KeyChar;
                        detailsSel = 0;
                    }
                    break;

                case ConsoleKey.O:
                    if (mode == "agyswitch" && !_isSearchActive)
                    {
                        SubPageAccountNavigator.LogoutAccount(_detailsSearchBuffer, detailsSel, _accountStore);
                        break;
                    }
                    if (_isSearchActive || !string.IsNullOrEmpty(_detailsSearchBuffer))
                    {
                        _detailsSearchBuffer += key.KeyChar;
                        detailsSel = 0;
                    }
                    break;

                case ConsoleKey.R:
                    if (mode == "agyswitch" && !_isSearchActive)
                    {
                        SubPageAccountNavigator.PurgeAccounts(_accountStore);
                        detailsSel = 0;
                        break;
                    }
                    if (_isSearchActive || !string.IsNullOrEmpty(_detailsSearchBuffer))
                    {
                        _detailsSearchBuffer += key.KeyChar;
                        detailsSel = 0;
                    }
                    break;

                default:
                    if (key.KeyChar >= 32 && key.KeyChar <= 126)
                    {
                        if (!_isSearchActive && mode == "proj" && key.KeyChar >= '1' && key.KeyChar <= '9')
                        {
                            bool shouldExit = SubPageProjNavigator.HandleKeyInput(key, workspaces, flatList, detailsSel, _detailsSearchBuffer);
                            if (shouldExit) return;
                            break;
                        }
                        if (_isSearchActive || !string.IsNullOrEmpty(_detailsSearchBuffer))
                        {
                            _detailsSearchBuffer += key.KeyChar;
                            detailsSel = 0;
                            if (mode == "proj")
                            {
                                SubPageProjNavigator.SelectedWorkspaceIndex = 0;
                                SubPageProjNavigator.SelectedActionIndex = -1;
                                SubPageProjNavigator.ExpandedWorkspaceIndex = -1;
                            }
                        }
                    }
                    break;
            }

            if (mode == "proj" && itemsCount > 0)
            {
                if (detailsSel < 0) detailsSel = 0;
                if (detailsSel >= flatList.Count) detailsSel = flatList.Count - 1;

                if (flatList.Count > 0)
                {
                    var selectedItem = flatList[detailsSel];
                    SubPageProjNavigator.SelectedWorkspaceIndex = selectedItem.WorkspaceIndex;
                    SubPageProjNavigator.SelectedActionIndex = selectedItem.ActionIndex;
                }
            }
        }
    }

    private static string DeletePreviousWord(string buffer)
    {
        if (string.IsNullOrEmpty(buffer)) return "";
        int i = buffer.Length - 1;
        while (i >= 0 && char.IsWhiteSpace(buffer[i])) i--;
        while (i >= 0 && !char.IsWhiteSpace(buffer[i])) i--;
        return buffer[..(i + 1)];
    }

    private static void RenderSubPageSelection(string mode, int selIdx, WorkspaceEntry[] workspaces, List<SubPageProjNavigator.FlatItem> flatList)
    {
        var grid = new Grid();
        grid.AddColumn(new GridColumn().NoWrap());

        IRenderable content = grid;

        if (mode == "agyswitch")
        {
            content = SubPageAccountNavigator.Render(grid, _detailsSearchBuffer, selIdx);
        }
        else if (mode == "theme")
        {
            content = SubPageThemeNavigator.Render(grid, _detailsSearchBuffer, selIdx);
        }
        else if (mode == "learn" || mode == "session" || mode == "weak")
        {
            content = SubPageTopicNavigator.Render(grid, mode, _detailsSearchBuffer, selIdx);
        }
        else if (mode == "favorite")
        {
            content = SubPageFavoriteNavigator.Render(grid, _detailsSearchBuffer, selIdx);
        }
        else if (mode == "proj")
        {
            var currentDir = Directory.GetCurrentDirectory();
            content = SubPageProjNavigator.Render(grid, _detailsSearchBuffer, workspaces, flatList, selIdx, currentDir);
            ScreenChrome.WriteSmooth(content);
            return;
        }

        ScreenChrome.WriteSmooth(content);
    }
}

public static class SubPageNavigator
{
    private static readonly ISubPageNavigator _service = new SubPageNavigatorService();
    public static ISubPageNavigator Instance => _service;

    public static void Run(string mode, string initialQuery = "") => _service.Run(mode, initialQuery);
    public static void RunScreen(IScreenView screenView, string initialQuery = "") => _service.RunScreen(screenView, initialQuery);
    public static string ProcessSearchKey(ConsoleKeyInfo key, string currentBuffer) => _service.ProcessSearchKey(key, currentBuffer);
}

