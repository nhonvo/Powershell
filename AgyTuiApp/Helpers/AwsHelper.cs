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
            RunLocalAwsCli("s3 ls", "S3 Buckets");
            RunLocalAwsCli("sqs list-queues", "SQS Queues");
            RunLocalAwsCli("lambda list-functions --query 'Functions[*].FunctionName'", "Lambda Functions");
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

    private static void RunLocalAwsCli(string args, string section)
    {
        AnsiConsole.MarkupLine($"\n[bold cyan]{section.EscapeMarkup()}[/]");
        var output = Helpers.ProcessRunner.RunCapture("aws", $"--endpoint-url {LocalStackEndpoint} {args}");
        if (string.IsNullOrWhiteSpace(output))
            AnsiConsole.MarkupLine("[dim] (no results or LocalStack unavailable)[/]");
        else
            foreach (var line in output.Trim().Split('\n'))
                AnsiConsole.MarkupLine($" {line.EscapeMarkup()}");
    }
}
