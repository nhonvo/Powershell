using AgyTui.UI.Screens.Learn;

namespace AgyTui.UI.Core.Navigation.Routers;

public class LearnCommandRouter
{
    public bool TryHandle(string alias, string[] args, out int exitCode)
    {
        exitCode = 0;
        switch (alias.ToLowerInvariant())
        {
            case "learn":
            case "study":
                LearnRouter.LaunchMasterHub();
                return true;
            default:
                return false;
        }
    }
}
