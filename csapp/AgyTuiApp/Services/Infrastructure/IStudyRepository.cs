namespace AgyTui;

public interface IStudyRepository
{
    void EnsureDirectories();
    T? LoadJson<T>(string path) where T : class;
    void SaveJson<T>(string path, T obj);
}
