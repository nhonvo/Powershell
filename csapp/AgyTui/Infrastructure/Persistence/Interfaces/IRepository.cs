namespace AgyTui.Infrastructure.Persistence.Interfaces;

public interface IRepository<TEntity, TKey> where TEntity : class
{
    TEntity? GetById(TKey id);
    IEnumerable<TEntity> GetAll();
    void Save(TKey id, TEntity entity);
    void Delete(TKey id);
}
