namespace AgyTui.Infrastructure.Integrations.Aws;

public interface IAwsClient
{
    void ShowLocalStackInfo();
    void ShowCallerIdentity();
    void ShowS3Buckets();
    void ShowSQSQueues();
    void ShowSsmParameters();
    void ShowSnsTopics();
    void ShowDynamoDbTables();
    void ShowLambdaFunctions();
}
