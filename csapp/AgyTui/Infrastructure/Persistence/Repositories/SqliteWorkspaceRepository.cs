namespace AgyTui.Infrastructure.Persistence.Repositories;

public class SqliteWorkspaceRepository : SqliteRepositoryBase<WorkspaceAggregate, string>, IWorkspaceRepository
{
    public SqliteWorkspaceRepository(ISqliteDatabase db) : base(db) { }

    public override WorkspaceAggregate? GetById(string id) => GetWorkspace(id);
    public override IEnumerable<WorkspaceAggregate> GetAll() => GetAllWorkspaces();
    public override void Save(string id, WorkspaceAggregate entity) => SaveWorkspace(entity);
    public override void Delete(string id) => DeleteWorkspace(id);

    public IEnumerable<WorkspaceAggregate> GetAllWorkspaces()
    {
        var list = new List<WorkspaceAggregate>();
        try
        {
            using var conn = Database.CreateConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT name, workspace_path, associated_account, tags_csv, alias FROM workspaces ORDER BY name ASC;";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var name = reader.GetString(0);
                var path = reader.GetString(1);
                var account = reader.IsDBNull(2) ? "default" : reader.GetString(2);
                var tagsCsv = reader.IsDBNull(3) ? null : reader.GetString(3);
                var alias = reader.IsDBNull(4) ? null : reader.GetString(4);

                var tags = !string.IsNullOrEmpty(tagsCsv) ? tagsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) : null;
                list.Add(new WorkspaceAggregate(name, path, account, false, null, alias, tags));
            }
        }
        catch { }
        return list;
    }

    public WorkspaceAggregate? GetWorkspace(string name)
    {
        try
        {
            using var conn = Database.CreateConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT name, workspace_path, associated_account, tags_csv, alias FROM workspaces WHERE name = @name;";
            cmd.Parameters.AddWithValue("@name", name);
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                var wName = reader.GetString(0);
                var path = reader.GetString(1);
                var account = reader.IsDBNull(2) ? "default" : reader.GetString(2);
                var tagsCsv = reader.IsDBNull(3) ? null : reader.GetString(3);
                var alias = reader.IsDBNull(4) ? null : reader.GetString(4);

                var tags = !string.IsNullOrEmpty(tagsCsv) ? tagsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) : null;
                return new WorkspaceAggregate(wName, path, account, false, null, alias, tags);
            }
        }
        catch { }
        return null;
    }

    public void SaveWorkspace(WorkspaceAggregate workspace)
    {
        var now = DateTime.UtcNow.ToString("o");
        var tagsCsv = workspace.Tags != null && workspace.Tags.Length > 0 ? string.Join(",", workspace.Tags) : (object)DBNull.Value;
        try
        {
            using var conn = Database.CreateConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                INSERT INTO workspaces (name, workspace_path, associated_account, tags_csv, alias, updated_at)
                VALUES (@name, @path, @account, @tags, @alias, @now)
                ON CONFLICT(name) DO UPDATE SET
                    workspace_path = @path,
                    associated_account = @account,
                    tags_csv = @tags,
                    alias = @alias,
                    updated_at = @now;
                """;
            cmd.Parameters.AddWithValue("@name", workspace.Name);
            cmd.Parameters.AddWithValue("@path", workspace.WorkspacePath.Value);
            cmd.Parameters.AddWithValue("@account", (object?)workspace.CorpusName ?? "default");
            cmd.Parameters.AddWithValue("@tags", tagsCsv);
            cmd.Parameters.AddWithValue("@alias", (object?)workspace.Alias ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@now", now);
            cmd.ExecuteNonQuery();
        }
        catch { }
    }

    public void DeleteWorkspace(string name)
    {
        try
        {
            using var conn = Database.CreateConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM workspaces WHERE name = @name;";
            cmd.Parameters.AddWithValue("@name", name);
            cmd.ExecuteNonQuery();
        }
        catch { }
    }
}
