using Foundz.Net.Core.AI;
using Spectre.Console;

namespace Foundz.Net.Cli.Commands;

/// <summary>
/// Command to display usage statistics.
/// </summary>
public class StatsCommand
{
    private readonly RequestMetricsCollector _metricsCollector;

    public StatsCommand(RequestMetricsCollector metricsCollector)
    {
        _metricsCollector = metricsCollector;
    }

    /// <summary>
    /// Executes the stats command.
    /// </summary>
    public void Execute()
    {
        var metrics = _metricsCollector.GetAggregatedMetrics();

        // Display header
        var rule = new Rule("[bold blue]📊 Usage Statistics[/]");
        rule.LeftJustified();
        AnsiConsole.Write(rule);
        AnsiConsole.WriteLine();

        // Overall stats
        var overallTable = new Table();
        overallTable.Border(TableBorder.Rounded);
        overallTable.AddColumn("[bold]Metric[/]");
        overallTable.AddColumn("[bold]Value[/]");

        var totalTokens = metrics.TotalPromptTokens + metrics.TotalCompletionTokens;
        
        overallTable.AddRow("Total Requests", $"{metrics.TotalRequests:N0}");
        overallTable.AddRow("Successful", $"[green]{metrics.SuccessfulRequests:N0}[/]");
        overallTable.AddRow("Failed", metrics.FailedRequests > 0 
            ? $"[red]{metrics.FailedRequests:N0}[/]" 
            : $"{metrics.FailedRequests:N0}");
        overallTable.AddRow("Success Rate", $"{metrics.SuccessRate:F1}%");
        overallTable.AddRow("Total Tokens", $"{totalTokens:N0}");
        overallTable.AddRow("Prompt Tokens", $"{metrics.TotalPromptTokens:N0}");
        overallTable.AddRow("Completion Tokens", $"{metrics.TotalCompletionTokens:N0}");

        AnsiConsole.Write(overallTable);
        AnsiConsole.WriteLine();

        // Performance stats
        var perfTable = new Table();
        perfTable.Border(TableBorder.Rounded);
        perfTable.AddColumn("[bold]Performance[/]");
        perfTable.AddColumn("[bold]Duration (ms)[/]");

        perfTable.AddRow("Average", $"{metrics.AverageDurationMs:F0}");
        perfTable.AddRow("Median (P50)", $"{metrics.MedianDurationMs:F0}");
        perfTable.AddRow("P95", $"{metrics.P95DurationMs:F0}");
        perfTable.AddRow("P99", $"{metrics.P99DurationMs:F0}");
        perfTable.AddRow("Min", $"[green]{metrics.MinDurationMs:F0}[/]");
        perfTable.AddRow("Max", $"[yellow]{metrics.MaxDurationMs:F0}[/]");

        AnsiConsole.Write(perfTable);
        AnsiConsole.WriteLine();

        // Per-model stats
        if (metrics.ModelMetrics.Any())
        {
            var modelTable = new Table();
            modelTable.Border(TableBorder.Rounded);
            modelTable.AddColumn("[bold]Model[/]");
            modelTable.AddColumn("[bold]Requests[/]", c => c.RightAligned());
            modelTable.AddColumn("[bold]Success Rate[/]", c => c.RightAligned());
            modelTable.AddColumn("[bold]Avg Duration (ms)[/]", c => c.RightAligned());
            modelTable.AddColumn("[bold]Total Tokens[/]", c => c.RightAligned());

            foreach (var kvp in metrics.ModelMetrics.OrderByDescending(m => m.Value.TotalRequests))
            {
                var model = kvp.Key;
                var stats = kvp.Value;

                var successRateFormatted = stats.SuccessRate >= 95
                    ? $"[green]{stats.SuccessRate:F1}%[/]"
                    : stats.SuccessRate >= 80
                        ? $"[yellow]{stats.SuccessRate:F1}%[/]"
                        : $"[red]{stats.SuccessRate:F1}%[/]";

                var modelTokens = stats.TotalPromptTokens + stats.TotalCompletionTokens;

                modelTable.AddRow(
                    model,
                    $"{stats.TotalRequests:N0}",
                    successRateFormatted,
                    $"{stats.AverageDurationMs:F0}",
                    $"{modelTokens:N0}"
                );
            }

            AnsiConsole.Write(modelTable);
        }
        else
        {
            AnsiConsole.MarkupLine("[grey]No per-model statistics available yet.[/]");
        }
    }

    /// <summary>
    /// Executes the stats command for a specific model.
    /// </summary>
    public void ExecuteForModel(string modelName)
    {
        var metrics = _metricsCollector.GetModelMetrics(modelName);

        if (metrics == null)
        {
            AnsiConsole.MarkupLine($"[red]No statistics found for model: {modelName}[/]");
            return;
        }

        var rule = new Rule($"[bold blue]📊 Statistics for {modelName}[/]");
        rule.LeftJustified();
        AnsiConsole.Write(rule);
        AnsiConsole.WriteLine();

        var table = new Table();
        table.Border(TableBorder.Rounded);
        table.AddColumn("[bold]Metric[/]");
        table.AddColumn("[bold]Value[/]");

        var modelTokens = metrics.TotalPromptTokens + metrics.TotalCompletionTokens;
        
        table.AddRow("Total Requests", $"{metrics.TotalRequests:N0}");
        table.AddRow("Successful", $"[green]{metrics.SuccessfulRequests:N0}[/]");
        table.AddRow("Failed", metrics.FailedRequests > 0
            ? $"[red]{metrics.FailedRequests:N0}[/]"
            : $"{metrics.FailedRequests:N0}");
        table.AddRow("Success Rate", $"{metrics.SuccessRate:F1}%");
        table.AddRow("Total Tokens", $"{modelTokens:N0}");
        table.AddRow("Average Duration", $"{metrics.AverageDurationMs:F0} ms");
        table.AddRow("First Request", metrics.FirstRequest.ToString("yyyy-MM-dd HH:mm:ss"));
        table.AddRow("Last Request", metrics.LastRequest.ToString("yyyy-MM-dd HH:mm:ss"));

        AnsiConsole.Write(table);
    }
}
