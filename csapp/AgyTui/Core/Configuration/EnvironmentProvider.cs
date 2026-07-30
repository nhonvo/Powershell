namespace AgyTui.Core.Configuration;

public static class EnvironmentProvider
{
    public static bool IsDevelopment => string.Equals(
        Environment.GetEnvironmentVariable("ENVIRONMENT") ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT"),
        "Development",
        StringComparison.OrdinalIgnoreCase);

    public static string DatabaseFileName => IsDevelopment ? "agytui.dev.db" : "agytui.db";
}
