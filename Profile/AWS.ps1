#region AWS LOCALSTACK COMMANDS
# ------------------------------------------------------------------------------
#  Commands for interacting with AWS services via LocalStack.
# ------------------------------------------------------------------------------

$localStackUrl = "http://127.0.0.1:4566"

<# 
.SYNOPSIS 
Lists SQS queues. 
.CATEGORY
AWS LocalStack Commands
#>
function list-queue { 
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
function create-queue { 
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
function clear-queue { 
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
function send-mess { 
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
function receive-mess { 
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
function number-mes { 
    [CmdletBinding()] 
    param([string]$QueueUrl) 
    awslocal --endpoint-url=$localStackUrl sqs get-queue-attributes --queue-url $QueueUrl --attribute-names All 
}

#endregion