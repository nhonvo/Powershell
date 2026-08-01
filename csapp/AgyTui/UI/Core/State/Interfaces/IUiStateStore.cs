namespace AgyTui.UI.Core.State;

public interface IUiStateStore
{
    UiState Current { get; }
    void Update(Func<UiState, UiState> updateFunc);
    event Action<UiState>? OnStateChanged;
}
