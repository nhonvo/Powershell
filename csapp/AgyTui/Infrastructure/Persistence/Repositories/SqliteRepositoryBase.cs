using System.Text.Json;
using AgyTui.Infrastructure.Persistence.DbContext;
using AgyTui.Infrastructure.Persistence.Interfaces;

namespace AgyTui.Infrastructure.Persistence.Repositories;

public abstract class SqliteRepositoryBase<TEntity, TKey> : IRepository<TEntity, TKey> where TEntity : class
{
    protected readonly ISqliteDatabase Database;
    protected readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    protected SqliteRepositoryBase(ISqliteDatabase database)
    {
        Database = database;
        Database.InitializeDatabase();
    }

    public abstract TEntity? GetById(TKey id);
    public abstract IEnumerable<TEntity> GetAll();
    public abstract void Save(TKey id, TEntity entity);
    public abstract void Delete(TKey id);
}
