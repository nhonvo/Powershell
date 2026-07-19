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
