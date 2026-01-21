#region AWS LOCALSTACK COMMANDS
# ------------------------------------------------------------------------------
#  Commands for interacting with AWS services via LocalStack.
# ------------------------------------------------------------------------------

$localStackUrl = "http://localhost:4566"

<# 
.SYNOPSIS 
Lists S3 buckets. 
.CATEGORY
AWS LocalStack Commands
#>
function Get-S3Buckets { 
    awslocal --endpoint-url=$localStackUrl s3 ls 
}

<# 
.SYNOPSIS 
Creates an S3 bucket. 
.CATEGORY
AWS LocalStack Commands
#>
function New-S3Bucket { 
    [CmdletBinding()] 
    param([string]$Name)
    awslocal --endpoint-url=$localStackUrl s3 mb s3://$Name
}

<# 
.SYNOPSIS 
Lists Lambda functions. 
.CATEGORY
AWS LocalStack Commands
#>
function Get-LambdaFunctions { 
    awslocal --endpoint-url=$localStackUrl lambda list-functions 
}

<# 
.SYNOPSIS 
Lists SQS queues. 
.CATEGORY
AWS LocalStack Commands
#>
function Get-LocalSQSQueues { 
    [CmdletBinding()] 
    param() 
    awslocal --endpoint-url=$localStackUrl sqs list-queues 
}

<# 
.SYNOPSIS 
Creates an SQS queue. 
.CATEGORY
AWS LocalStack Commands
#>
function New-LocalSQSQueue { 
    [CmdletBinding()] 
    param([string]$QueueName) 
    awslocal --endpoint-url=$localStackUrl sqs create-queue --queue-name=$QueueName 
}

<# 
.SYNOPSIS 
Purges all messages from an SQS queue. 
.CATEGORY
AWS LocalStack Commands
#>
function Clear-LocalSQSQueue { 
    [CmdletBinding()] 
    param([string]$QueueUrl) 
    awslocal --endpoint-url=$localStackUrl sqs purge-queue --queue-url $QueueUrl 
}

<# 
.SYNOPSIS 
Sends a message to an SQS queue. 
.CATEGORY
AWS LocalStack Commands
#>
function Send-LocalSQSMessage { 
    [CmdletBinding()] 
    param([string]$QueueUrl, [string]$MessageBody, [string]$GroupId = "default-group") 
    awslocal --endpoint-url=$localStackUrl sqs send-message --queue-url $QueueUrl --message-body $MessageBody --message-group-id $GroupId 
}

<# 
.SYNOPSIS 
Receives messages from an SQS queue. 
.CATEGORY
AWS LocalStack Commands
#>
function Get-LocalSQSMessage { 
    [CmdletBinding()] 
    param([string]$QueueUrl) 
    awslocal --endpoint-url=$localStackUrl sqs receive-message --queue-url $QueueUrl 
}

<# 
.SYNOPSIS 
Gets all attributes for an SQS queue. 
.CATEGORY
AWS LocalStack Commands
#>
function Get-LocalSQSAttributes { 
    [CmdletBinding()] 
    param([string]$QueueUrl) 
    awslocal --endpoint-url=$localStackUrl sqs get-queue-attributes --queue-url $QueueUrl --attribute-names All 
}

#endregion