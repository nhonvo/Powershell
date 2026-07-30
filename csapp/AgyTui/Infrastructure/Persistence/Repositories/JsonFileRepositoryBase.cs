using System.Text.Json;
using AgyTui.Infrastructure.Persistence.Interfaces;

namespace AgyTui.Infrastructure.Persistence.Repositories;

public class JsonFileRepositoryBase<TEntity> : IFileRepository<TEntity> where TEntity : class
{
    protected readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public virtual TEntity? ReadFile(string filePath)
    {
        if (!File.Exists(filePath)) return null;
        try
        {
            var text = File.ReadAllText(filePath, Encoding.UTF8);
            return JsonSerializer.Deserialize<TEntity>(text, JsonOptions);
        }
        catch (Exception ex)
        {
            LogHelper.Log($"[JsonFileRepositoryBase] Error reading '{filePath}': {ex.Message}", "ERROR");
            return null;
        }
    }

    public virtual bool WriteFile(string filePath, TEntity content)
    {
        try
        {
            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            var tempPath = filePath + ".tmp." + Guid.NewGuid().ToString("N");
            var json = JsonSerializer.Serialize(content, JsonOptions);
            File.WriteAllText(tempPath, json, Encoding.UTF8);
            File.Move(tempPath, filePath, overwrite: true);
            return true;
        }
        catch (Exception ex)
        {
            LogHelper.Log($"[JsonFileRepositoryBase] Error writing '{filePath}': {ex.Message}", "ERROR");
            return false;
        }
    }

    public virtual bool DeleteFile(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            LogHelper.Log($"[JsonFileRepositoryBase] Error deleting '{filePath}': {ex.Message}", "ERROR");
            return false;
        }
    }
}
