using AgyTui.Infrastructure.Common;
using Xunit;

namespace AgyTui.Tests.Unit.Infrastructure.Common;

public class ProjectScaffolderTests
{
    [Fact]
    public void ProjectScaffolder_Instance_CanBeCreated()
    {
        IProjectScaffolder scaffolder = new ProjectScaffolder();
        Assert.NotNull(scaffolder);
    }
}
