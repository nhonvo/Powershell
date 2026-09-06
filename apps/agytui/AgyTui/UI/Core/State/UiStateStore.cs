namespace AgyTui.UI.Core.State;

public record UiState(
    string ActiveAccount = "default",
    string FilterBuffer = "",
    int SelectedIndex = 0,
    bool IsGitQueryLoading = false,
    string? GitStatusSummary = null,
    bool IsCompactMode = false
);

public class UiStateStore : IUiStateStore
{
    private UiState _current = new();
    private readonly object _lock = new();

    public UiState Current
    {
        get { lock (_lock) return _current; }
    }

    public event Action<UiState>? OnStateChanged;

    public void Update(Func<UiState, UiState> updateFunc)
    {
        UiState updated;
        lock (_lock)
        {
            _current = updateFunc(_current);
            updated = _current;
        }
        OnStateChanged?.Invoke(updated);
    }
}
