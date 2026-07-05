#region AWS HELPER
# ==============================================================================
#  AWS LocalStack commands and S3/SQS utility wrappers.
# ==============================================================================

class AwsHelper {
    static [string]$LocalStackUrl = "http://localhost:4566"

    static [void] GetS3Buckets() {
        awslocal --endpoint-url=([AwsHelper]::LocalStackUrl) s3 ls
    }

    static [void] NewS3Bucket([string]$Name) {
        awslocal --endpoint-url=([AwsHelper]::LocalStackUrl) s3 mb s3://$Name
    }

    static [void] GetLambdaFunctions() {
        awslocal --endpoint-url=([AwsHelper]::LocalStackUrl) lambda list-functions
    }

    static [void] GetLocalSQSQueues() {
        awslocal --endpoint-url=([AwsHelper]::LocalStackUrl) sqs list-queues
    }

    static [void] NewLocalSQSQueue([string]$QueueName) {
        awslocal --endpoint-url=([AwsHelper]::LocalStackUrl) sqs create-queue --queue-name=$QueueName
    }

    static [void] ClearLocalSQSQueue([string]$QueueUrl) {
        awslocal --endpoint-url=([AwsHelper]::LocalStackUrl) sqs purge-queue --queue-url $QueueUrl
    }

    static [void] SendLocalSQSMessage([string]$QueueUrl, [string]$MessageBody, [string]$GroupId) {
        $gid = if ($GroupId) { $GroupId } else { "default-group" }
        awslocal --endpoint-url=([AwsHelper]::LocalStackUrl) sqs send-message --queue-url $QueueUrl --message-body $MessageBody --message-group-id $gid
    }

    static [void] GetLocalSQSMessage([string]$QueueUrl) {
        awslocal --endpoint-url=([AwsHelper]::LocalStackUrl) sqs receive-message --queue-url $QueueUrl
    }

    static [void] GetLocalSQSAttributes([string]$QueueUrl) {
        awslocal --endpoint-url=([AwsHelper]::LocalStackUrl) sqs get-queue-attributes --queue-url $QueueUrl --attribute-names All
    }
}
#endregion
