namespace AgyTui;

public static class AgyServices
{
    public static readonly AwsService Aws = new();
    public static readonly DockerService Docker = new();
    public static readonly DotNetService DotNet = new();
    public static readonly GitService Git = new();

    public static readonly IAccountRepository Account = new JsonAccountRepository();
    public static readonly IStudyRepository Study = new JsonStudyRepository();
}
