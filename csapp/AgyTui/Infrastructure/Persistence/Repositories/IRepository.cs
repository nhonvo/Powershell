namespace AgyTui.Infrastructure.Persistence.Repositories;

public interface IRepository<T> where T : class
{
    T? GetById(string id);
    IEnumerable<T> GetAll();
    void Save(T entity);
    void Delete(string id);
}
