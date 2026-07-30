namespace AgyTui.Domain.WorkspaceContext;

public sealed record ProjectPath
{
    public string Value { get; }

    public ProjectPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path cannot be empty", nameof(path));
        Value = Path.GetFullPath(path);
    }

    public bool Exists => Directory.Exists(Value) || File.Exists(Value);
    public override string ToString() => Value;
}
