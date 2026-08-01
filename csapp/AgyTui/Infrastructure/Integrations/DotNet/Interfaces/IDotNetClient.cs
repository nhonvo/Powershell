namespace AgyTui.Infrastructure.Integrations.DotNet;

public interface IDotNetClient
{
    void RemoveBinObj(string rootPath);
    int Build(string? projectPath = null);
    int Run(string? projectPath = null);
    int Test(string? projectPath = null);
    int Format(string? projectPath = null);
    int Clean(string? projectPath = null);
    int Restore(string? projectPath = null);
    int Publish(string? projectPath = null);
    int Pack(string? projectPath = null, string outputDir = "nupkg");
    int PublishPackage(string? nupkgPath = null, string? apiKey = null, string source = "https://api.nuget.org/v3/index.json");
    int Watch(string? projectPath = null);
    int AddMigration(string migrationName, string? project = null, string? context = null);
    int UpdateDatabase(string? project = null, string? context = null);
}
