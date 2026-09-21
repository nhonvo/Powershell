# ☁️ AWS & LocalStack Developer Cheat Sheet
**Antigravity Developer Suite — Cloud CLI & Local Dev Recipes**

---

## 🧭 Document References
- **VS Code Clickable (Recommended):** [04_aws_localstack_cheatsheet.md](./docs/04_command_enhancements/04_aws_localstack_cheatsheet.md)
- **Windows UNC Path:** `\\wsl.localhost\Ubuntu\home\truongnhon\projects\powershell-profile\docs\04_command_enhancements\04_aws_localstack_cheatsheet.md`
- **AGYX Suite Proxy:** [apps/agyx/](./apps/agyx/)

---

## 1. Quick AGYX Commands

| Command | Action |
| :--- | :--- |
| `agyx aws` / `agyx aws sheet` | Open interactive developer cheat sheet with live STS & LocalStack pills |
| `agyx aws whoami` | Check active IAM caller identity (`aws sts get-caller-identity`) |
| `agyx aws s3` | List S3 buckets (`aws s3 ls`) |
| `agyx aws sqs` | List active SQS queues (`aws sqs list-queues`) |
| `agyx aws local` | Health check LocalStack mock cloud on `http://localhost:4566` |
| `agyx aws <any>` | Direct transparent proxy pass-through to official `aws` CLI |

---

## 2. 🔐 IAM, SSO & Profile Management

```bash
# Check active IAM user, account ID, and assumed role
aws sts get-caller-identity

# Inspect current configuration, active profile, region & credentials source
aws configure list

# Single Sign-On (SSO) browser login
aws sso login --profile <profile-name>

# Switch active AWS profile for current shell session
export AWS_PROFILE=staging
# On Windows PowerShell:
$env:AWS_PROFILE = "staging"

# View all configured profiles
aws configure list-profiles
```

---

## 3. 🧪 LocalStack (Port 4566 — Zero Cloud Cost)

LocalStack emulates AWS services (S3, SQS, SNS, DynamoDB, Lambda) locally on port `4566`.

```bash
# 1. Start LocalStack container in Docker
docker run -d --name localstack -p 4566:4566 -e SERVICES=s3,sqs,dynamodb,sns localstack/localstack

# 2. Check service health status
curl -s http://localhost:4566/_localstack/health | jq .

# 3. Create persistent alias in your shell profile (~/.zshrc or posh-profile.zsh)
alias awslocal="aws --endpoint-url=http://localhost:4566"

# 4. Use awslocal exactly like aws CLI
awslocal s3 mb s3://dev-bucket
awslocal sqs create-queue --queue-name dev-queue
awslocal dynamodb list-tables
```

---

## 4. 🪣 Amazon S3 Storage Recipes

```bash
# List all buckets
aws s3 ls

# List objects inside a bucket prefix
aws s3 ls s3://my-bucket/uploads/ --human-readable --summarize

# Create a new bucket
aws s3 mb s3://my-new-bucket --region ap-southeast-1

# Upload file
aws s3 cp document.pdf s3://my-bucket/docs/

# Sync directory (only upload new/modified files) and purge removed files
aws s3 sync ./dist/ s3://my-frontend-bucket/ --delete

# Sync with Intelligent-Tiering storage class (save up to 40% on storage costs)
aws s3 sync ./backups/ s3://my-backup-bucket/ --storage-class INTELLIGENT_TIERING

# Generate a temporary pre-signed URL (e.g. 1 hour = 3600 seconds)
aws s3 presign s3://my-bucket/export.zip --expires-in 3600

# Force delete bucket and ALL objects inside it
aws s3 rb s3://my-bucket --force
```

---

## 5. 📨 Amazon SQS & SNS Messaging

```bash
# List all active SQS queues
aws sqs list-queues

# Get queue URL by name
aws sqs get-queue-url --queue-name orders-queue

# Send a JSON message to queue
aws sqs send-message \
  --queue-url https://sqs.ap-southeast-1.amazonaws.com/123456789012/orders-queue \
  --message-body '{"orderId":"1001","status":"PAID"}'

# Receive messages from queue (read up to 10)
aws sqs receive-message \
  --queue-url https://sqs.ap-southeast-1.amazonaws.com/123456789012/orders-queue \
  --max-number-of-messages 10

# Purge ALL messages from queue (clears queue instantly without deleting queue)
aws sqs purge-queue --queue-url https://sqs.ap-southeast-1.amazonaws.com/123456789012/orders-queue

# SNS: Publish a topic notification
aws sns publish \
  --topic-arn arn:aws:sns:ap-southeast-1:123456789012:alerts \
  --message "Deployment completed successfully" \
  --subject "Production Alert"
```

---

## 6. ⚡ DynamoDB & Serverless

```bash
# List all DynamoDB tables
aws dynamodb list-tables

# Describe table schema and capacity
aws dynamodb describe-table --table-name Users

# Quick scan (sample 5 items)
aws dynamodb scan --table-name Users --max-items 5

# Query item by partition key
aws dynamodb get-item \
  --table-name Users \
  --key '{"UserId":{"S":"usr_4582"}}'

# Delete item
aws dynamodb delete-item \
  --table-name Users \
  --key '{"UserId":{"S":"usr_4582"}}'
```

---

## 7. 🪵 CloudWatch & Lambda Logs

```bash
# Live tail Lambda execution logs in terminal (like tail -f)
aws logs tail /aws/lambda/ProcessOrderFunction --follow

# Tail logs with filtering
aws logs tail /aws/lambda/ProcessOrderFunction --follow --filter-pattern "ERROR"

# List Lambda functions
aws lambda list-functions --query 'Functions[].FunctionName'

# Manually invoke a Lambda function and inspect response payload
aws lambda invoke --function-name ProcessOrderFunction response.json
cat response.json
```

---

## 8. 🚢 Amazon ECR (Container Registry)

```bash
# Authenticate local Docker daemon to AWS ECR
aws ecr get-login-password --region ap-southeast-1 | docker login --username AWS --password-stdin 123456789012.dkr.ecr.ap-southeast-1.amazonaws.com

# Create repository
aws ecr create-repository --repository-name my-app-service

# Tag and push Docker image
docker tag my-app:latest 123456789012.dkr.ecr.ap-southeast-1.amazonaws.com/my-app-service:latest
docker push 123456789012.dkr.ecr.ap-southeast-1.amazonaws.com/my-app-service:latest
```

---
*Maintained as part of the Antigravity Developer Suite.*
