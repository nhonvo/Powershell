using System.Text.Json.Serialization;

namespace AgyTui.Domain.WorkspaceContext;

public sealed record WorkspaceLink(string Label, string Url);

public sealed record WorkspaceEntry(
    string Name,
    [property: JsonPropertyName("Path")] string WorkspacePath,
    string? AssociatedAccount,
    string[]? Tags,
    WorkspaceLink[]? Links = null,
    string? Alias = null
);
