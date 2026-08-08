namespace AgyTui.Infrastructure.Services;

public class PhysicalFileSystem : IFileSystem
{
    public bool Exists(string path) => File.Exists(path);
    public string ReadAllText(string path) => File.ReadAllText(path);
    public void WriteAllText(string path, string contents) => File.WriteAllText(path, contents);
    public void Delete(string path) => File.Delete(path);
    public bool DirectoryExists(string path) => Directory.Exists(path);
    public void CreateDirectory(string path) => Directory.CreateDirectory(path);
    public string[] GetFiles(string path) => Directory.GetFiles(path);
}
