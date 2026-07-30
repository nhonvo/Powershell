namespace AgyTui.Infrastructure.Persistence.Interfaces;

public interface IFileRepository<TEntity> where TEntity : class
{
    TEntity? ReadFile(string filePath);
    bool WriteFile(string filePath, TEntity content);
    bool DeleteFile(string filePath);
}
