using System;
using System.Collections.Generic;
using System.Linq;
using Spectre.Console;
using AgyTui.Components;

namespace AgyTui;

public static class AwsHelper
{
    private const string LocalStackEndpoint = "http://localhost:4566";

    public static void ShowLocalStackInfo()
    {
        AnsiConsole.Write(new Rule("[bold cyan]LocalStack Sandbox[/]").RuleStyle("grey"));
        SpectreProgress.Spinner("Querying LocalStack…", () =>
        {
            RunLocalAwsCli(new[] { "s3", "ls" }, "S3 Buckets");
            RunLocalAwsCli(new[] { "sqs", "list-queues" }, "SQS Queues");
            RunLocalAwsCli(new[] { "lambda", "list-functions", "--query", "Functions[*].FunctionName" }, "Lambda Functions");
        });
    }

    public static void ShowCallerIdentity()
    {
        AnsiConsole.Write(new Rule("[bold cyan]AWS Identity & Region[/]").RuleStyle("grey"));
        var output = Helpers.ProcessRunner.RunCapture("aws", "sts get-caller-identity");
        if (string.IsNullOrWhiteSpace(output))
        {
            SpectrePanel.Warning("AWS CLI returned no response or credentials not configured.");
            return;
        }
        SpectrePanel.Info(output);
    }

    public static void ShowS3Buckets()
    {
        AnsiConsole.Write(new Rule("[bold cyan]AWS S3 Buckets[/]").RuleStyle("grey"));
        var output = Helpers.ProcessRunner.RunCapture("aws", "s3 ls");
        if (string.IsNullOrWhiteSpace(output)) output = Helpers.ProcessRunner.RunCapture("aws", $"--endpoint-url {LocalStackEndpoint} s3 ls");
        if (string.IsNullOrWhiteSpace(output)) SpectrePanel.Warning("No S3 buckets found or AWS/LocalStack offline.");
        else SpectrePager.Show("S3 Buckets", output);
    }

    public static void ShowSQSQueues()
    {
        AnsiConsole.Write(new Rule("[bold cyan]AWS SQS Queues[/]").RuleStyle("grey"));
        var output = Helpers.ProcessRunner.RunCapture("aws", "sqs list-queues");
        if (string.IsNullOrWhiteSpace(output)) output = Helpers.ProcessRunner.RunCapture("aws", $"--endpoint-url {LocalStackEndpoint} sqs list-queues");
        if (string.IsNullOrWhiteSpace(output)) SpectrePanel.Warning("No SQS queues found or AWS/LocalStack offline.");
        else SpectrePager.Show("SQS Queues", output);
    }

    public static void ShowSsmParameters()
    {
        AnsiConsole.Write(new Rule("[bold cyan]AWS SSM Parameter Store[/]").RuleStyle("grey"));
        var output = Helpers.ProcessRunner.RunCapture("aws", "ssm describe-parameters");
        if (string.IsNullOrWhiteSpace(output)) output = Helpers.ProcessRunner.RunCapture("aws", $"--endpoint-url {LocalStackEndpoint} ssm describe-parameters");
        if (string.IsNullOrWhiteSpace(output)) SpectrePanel.Warning("No SSM parameters found or AWS/LocalStack offline.");
        else SpectrePager.Show("SSM Parameter Store", output);
    }

    public static void ShowSnsTopics()
    {
        AnsiConsole.Write(new Rule("[bold cyan]AWS SNS Topics[/]").RuleStyle("grey"));
        var output = Helpers.ProcessRunner.RunCapture("aws", "sns list-topics");
        if (string.IsNullOrWhiteSpace(output)) output = Helpers.ProcessRunner.RunCapture("aws", $"--endpoint-url {LocalStackEndpoint} sns list-topics");
        if (string.IsNullOrWhiteSpace(output)) SpectrePanel.Warning("No SNS topics found or AWS/LocalStack offline.");
        else SpectrePager.Show("SNS Topics", output);
    }

    public static void ShowDynamoDbTables()
    {
        AnsiConsole.Write(new Rule("[bold cyan]AWS DynamoDB Tables[/]").RuleStyle("grey"));
        var output = Helpers.ProcessRunner.RunCapture("aws", "dynamodb list-tables");
        if (string.IsNullOrWhiteSpace(output)) output = Helpers.ProcessRunner.RunCapture("aws", $"--endpoint-url {LocalStackEndpoint} dynamodb list-tables");
        if (string.IsNullOrWhiteSpace(output)) SpectrePanel.Warning("No DynamoDB tables found or AWS/LocalStack offline.");
        else SpectrePager.Show("DynamoDB Tables", output);
    }

    public static void ShowLambdaFunctions()
    {
        AnsiConsole.Write(new Rule("[bold cyan]AWS Lambda Functions[/]").RuleStyle("grey"));
        var output = Helpers.ProcessRunner.RunCapture("aws", "lambda list-functions");
        if (string.IsNullOrWhiteSpace(output)) output = Helpers.ProcessRunner.RunCapture("aws", $"--endpoint-url {LocalStackEndpoint} lambda list-functions");
        if (string.IsNullOrWhiteSpace(output)) SpectrePanel.Warning("No Lambda functions found or AWS/LocalStack offline.");
        else SpectrePager.Show("Lambda Functions", output);
    }

    private static void RunLocalAwsCli(string[] args, string section)
    {
        AnsiConsole.MarkupLine($"\n[bold cyan]{section.EscapeMarkup()}[/]");
        var fullArgs = new List<string> { "--endpoint-url", LocalStackEndpoint };
        fullArgs.AddRange(args);
        var output = Helpers.ProcessRunner.RunCapture("aws", fullArgs);
        if (string.IsNullOrWhiteSpace(output))
            AnsiConsole.MarkupLine("[dim] (no results or LocalStack unavailable)[/]");
        else
            foreach (var line in output.Trim().Split('\n'))
                AnsiConsole.MarkupLine($" {line.EscapeMarkup()}");
    }
}
