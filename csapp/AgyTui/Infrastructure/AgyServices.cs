namespace AgyTui.Infrastructure;

public static class AgyServices
{
    public static readonly AwsClient Aws = new();
    public static readonly DockerClient Docker = new();
    public static readonly DotNetClient DotNet = new();
    public static readonly GitClient Git = new();

    public static readonly IAccountRepository Account = new JsonAccountRepository();
    public static readonly IStudyRepository Study = new JsonStudyRepository();
}
