namespace AgyTui.Infrastructure.Integrations.Ai;

public enum HermesResult
{
    Success,
    NotInstalled,
    Error
}

public interface IHermesClient
{
    HermesResult InvokeHermes(string[]? argsList = null);
    HermesResult InvokeHermesDesktop(string[]? argsList = null);
}
