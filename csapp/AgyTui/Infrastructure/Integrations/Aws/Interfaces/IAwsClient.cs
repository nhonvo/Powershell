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
    void CreateS3Bucket(string name);
    void CreateSQSQueue(string name);
    void PurgeSQSQueue(string url);
    void SendSQSMessage(string url, string body, string? groupId = null);
    void ReceiveSQSMessage(string url);
    void GetSQSAttributes(string url);
}
