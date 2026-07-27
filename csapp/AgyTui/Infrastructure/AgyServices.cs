using AgyTui.Infrastructure.Integrations.Ai;
using AgyTui.Infrastructure.Integrations.Ai.Providers;
using AgyTui.Infrastructure.Integrations.Ai.Services;

namespace AgyTui.Infrastructure;

public static class AgyServices
{
    public static readonly AwsClient Aws = new();
    public static readonly DockerClient Docker = new();
    public static readonly DotNetClient DotNet = new();
    public static readonly GitClient Git = new();

    public static readonly IOllamaClient Ollama = new OllamaClient();
    public static readonly IClaudeClient Claude = new ClaudeProvider();
    public static readonly IHermesClient Hermes = new HermesProvider();
    public static readonly IOpenClawClient OpenClaw = new OpenClawProvider();
    public static readonly IAiProjectScanner ProjectScanner = new AiProjectScanner();
    public static readonly IAiCommitGenerator CommitGenerator = new AiCommitGenerator();
    public static readonly IAiProcessRunner ProcessRunner = new AiProcessRunner();
    public static readonly IAiLearningGenerator LearningGenerator = new AiLearningGenerator();

    public static readonly IAccountRepository Account = new JsonAccountRepository();
    public static readonly IStudyRepository Study = new JsonStudyRepository();
}
