using AgyTui.Infrastructure.Persistence.Interfaces;
using AgyTui.Infrastructure.Persistence.Repositories;
using Xunit;

namespace AgyTui.Tests.Unit.Infrastructure.Persistence;

public class TestModel
{
    public string Name { get; set; } = "test";
}

public class FileAndStudyRepositoryTests
{
    [Fact]
    public void JsonFileRepositoryBase_ReadFile_NonExistentFile_ReturnsNull()
    {
        IFileRepository<TestModel> repo = new JsonFileRepositoryBase<TestModel>();
        var content = repo.ReadFile("C:\\NonExistent_Path_XYZ_999.json");
        Assert.Null(content);

        repo.DeleteFile("C:\\NonExistent_Path_XYZ_999.json");
    }

    [Fact]
    public void JsonStudyRepository_EnsureDirectories_ExecutesWithoutError()
    {
        IStudyRepository repo = new JsonStudyRepository();
        repo.EnsureDirectories();

        var loaded = repo.LoadJson<TestModel>("C:\\NonExistent_Path_XYZ_999.json");
        Assert.Null(loaded);

        var deck = repo.LoadDeck("non_existent_topic");
        Assert.NotNull(deck);
    }
}
