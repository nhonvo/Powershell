using System.Text.Json;

namespace AgyTui.Infrastructure.Persistence.Seeding;

public class WorkspaceSeeder : ISeeder
{
    private readonly IWorkspaceRepository _workspaceRepo;
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public int Order => 2;

    public WorkspaceSeeder(IWorkspaceRepository workspaceRepo)
    {
        _workspaceRepo = workspaceRepo;
    }

    public void Seed()
    {
        try
        {
            var jsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "priority_workspaces.json");
            if (!File.Exists(jsonPath))
            {
                jsonPath = Path.Combine(Directory.GetCurrentDirectory(), "csapp", "AgyTui", "priority_workspaces.json");
            }
            if (!File.Exists(jsonPath)) return;

            var json = File.ReadAllText(jsonPath);
            var entries = JsonSerializer.Deserialize<WorkspaceEntry[]>(json, JsonOpts);
            if (entries == null) return;

            foreach (var entry in entries)
            {
                var existing = _workspaceRepo.GetWorkspace(entry.Name);
                if (existing == null)
                {
                    var agg = WorkspaceAggregate.FromEntry(entry);
                    _workspaceRepo.SaveWorkspace(agg);
                }
            }
        }
        catch { }
    }
}
