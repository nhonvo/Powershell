# ☁️ AWS Command Architecture & Dual-Tier Enhancement Pattern

## 1. Design Blueprint & Dual-Tier Standard

AWS cloud and LocalStack tools are split into:
1. **Native CLI Tier**: Direct execution of standard `aws` CLI commands (`aws-whoami`, `aws-s3`, `aws-sqs`, `aws-ssm`, `aws-sns`, `aws-dynamodb`, `aws-lambda`).
2. **Custom TUI Tier (`✨`)**: Interactive Spectre.Console dashboards, LocalStack mock service detectors, S3 bucket explorers, and SQS queue monitors (`aws-whoamiu`, `aws-s3u`, `aws-local`).

---

## 2. Naming & Routing Conventions

- **Native CLI Commands**:
  - `aws-whoami` $\rightarrow$ Standard native `aws sts get-caller-identity @args`.
  - `aws-s3` $\rightarrow$ Standard native `aws s3 ls @args`.
  - `aws-sqs` $\rightarrow$ Standard native `aws sqs list-queues @args`.
  - `aws-ssm` $\rightarrow$ Standard native `aws ssm describe-parameters @args`.
  - `aws-sns` $\rightarrow$ Standard native `aws sns list-topics @args`.
  - `aws-dynamodb` $\rightarrow$ Standard native `aws dynamodb list-tables @args`.
  - `aws-lambda` $\rightarrow$ Standard native `aws lambda list-functions @args`.

- **Custom TUI Commands (`✨`)**:
  - **`aws-whoamiu`** $\rightarrow$ `✨ AWS Identity & Credentials Inspector (Custom Spectre Table)`
  - **`aws-s3u`** $\rightarrow$ `✨ AWS S3 Bucket Explorer (Interactive TUI Bucket Navigator)`
  - **`aws-local`** $\rightarrow$ `✨ LocalStack Service Check (Health & Mock Endpoint Inspector)`
  - **`aws-sqsu`** $\rightarrow$ `✨ AWS SQS Queue Dashboard (Message Volume & Queue Inspector)`

---

## 3. AWS Command Alignment Matrix

| Native Command (CLI) | Execution Action | Custom TUI Command (`✨`) | TUI Feature & Behavior |
| :--- | :--- | :--- | :--- |
| **`aws-whoami`** | `aws sts get-caller-identity` | **`aws-whoamiu`** | **`✨ AWS Identity Inspector`**: Formatted Spectre card showing Account ID, IAM Arn, and region. |
| **`aws-s3`** | `aws s3 ls` | **`aws-s3u`** | **`✨ AWS S3 Bucket Explorer`**: Color-coded Spectre table listing buckets, creation dates, and object counts. |
| **`aws-local`** | Native CLI | **`aws-local`** | **`✨ LocalStack Service Check`**: Diagnostics verifying LocalStack Docker container (`localhost:4566`). |
| **`aws-sqs`** | `aws sqs list-queues` | **`aws-sqsu`** | **`✨ AWS SQS Queue Dashboard`**: Table rendering active queues and visible message counts. |
| **`aws-ssm`** | `aws ssm describe-parameters` | **`aws-ssmu`** | **`✨ AWS SSM Parameter Store`**: Interactive parameter key viewer with masked secret display. |
| **`aws-sns`** | `aws sns list-topics` | **`aws-snsu`** | **`✨ AWS SNS Topic Explorer`**: Topic ARN list and subscriber endpoint table. |
| **`aws-dynamodb`** | `aws dynamodb list-tables` | **`aws-dynamodbu`** | **`✨ AWS DynamoDB Explorer`**: Table browser with item count and partition key definitions. |
| **`aws-lambda`** | `aws lambda list-functions` | **`aws-lambdau`** | **`✨ AWS Lambda Function List`**: Function catalog rendering runtimes, memory, and timeout configs. |

---

## 4. TUI Menu Tree Folder Mapping

All AWS cloud tools are grouped under **`📂 AWS Tools`** in [CommandRegistry.cs](file:///C:/Users/TruongNhon/Documents/Powershell/csapp/AgyTui/UI/Core/Registries/CommandRegistry.cs):

```text
─ [-] 📂 AWS Tools
     ├── ☁️ /aws-whoami — AWS Identity Info (Native)
     ├── ☁️ /aws-whoamiu — ✨ AWS Identity Inspector (Custom TUI)
     ├── ☁️ /aws-s3 — AWS S3 Bucket Explorer (Native)
     ├── ☁️ /aws-s3u — ✨ AWS S3 Bucket Explorer (Custom TUI Table)
     ├── ☁️ /aws-local — ✨ LocalStack Service Check
     ├── ☁️ /aws-sqs — AWS SQS Queue List (Native)
     ├── ☁️ /aws-ssm — AWS SSM Parameters (Native)
     ├── ☁️ /aws-sns — AWS SNS Topics (Native)
     ├── ☁️ /aws-dynamodb — AWS DynamoDB Tables (Native)
     └── ☁️ /aws-lambda — AWS Lambda Functions (Native)
```
