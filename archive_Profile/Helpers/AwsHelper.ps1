#region AWS HELPER
# ==============================================================================
#  AWS LocalStack commands and S3/SQS utility wrappers.
# ==============================================================================

class AwsHelper {
    static [string]$LocalStackUrl = "http://localhost:4566"

    static [void] GetS3Buckets() {
        awslocal --endpoint-url=([AwsHelper]::LocalStackUrl) s3 ls | Out-Default
    }

    static [void] NewS3Bucket([string]$Name) {
        awslocal --endpoint-url=([AwsHelper]::LocalStackUrl) s3 mb s3://$Name | Out-Default
    }

    static [void] GetLambdaFunctions() {
        awslocal --endpoint-url=([AwsHelper]::LocalStackUrl) lambda list-functions | Out-Default
    }

    static [void] GetLocalSQSQueues() {
        awslocal --endpoint-url=([AwsHelper]::LocalStackUrl) sqs list-queues | Out-Default
    }

    static [void] NewLocalSQSQueue([string]$QueueName) {
        awslocal --endpoint-url=([AwsHelper]::LocalStackUrl) sqs create-queue --queue-name=$QueueName | Out-Default
    }

    static [void] ClearLocalSQSQueue([string]$QueueUrl) {
        awslocal --endpoint-url=([AwsHelper]::LocalStackUrl) sqs purge-queue --queue-url $QueueUrl | Out-Default
    }

    static [void] SendLocalSQSMessage([string]$QueueUrl, [string]$MessageBody, [string]$GroupId) {
        $gid = if ($GroupId) { $GroupId } else { "default-group" }
        awslocal --endpoint-url=([AwsHelper]::LocalStackUrl) sqs send-message --queue-url $QueueUrl --message-body $MessageBody --message-group-id $gid | Out-Default
    }

    static [void] GetLocalSQSMessage([string]$QueueUrl) {
        awslocal --endpoint-url=([AwsHelper]::LocalStackUrl) sqs receive-message --queue-url $QueueUrl | Out-Default
    }

    static [void] GetLocalSQSAttributes([string]$QueueUrl) {
        awslocal --endpoint-url=([AwsHelper]::LocalStackUrl) sqs get-queue-attributes --queue-url $QueueUrl --attribute-names All | Out-Default
    }
}
#endregion



