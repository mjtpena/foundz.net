using Foundz.Net.Core.AI;
using Spectre.Console;

namespace Foundz.Net.Cli.Commands;

/// <summary>
/// Command to display cost breakdown and analysis.
/// </summary>
public class CostsCommand
{
    private readonly CostTracker _costTracker;

    public CostsCommand(CostTracker costTracker)
    {
        _costTracker = costTracker;
    }

    /// <summary>
    /// Executes the costs command for all sessions.
    /// </summary>
    public void Execute()
    {
        var stats = _costTracker.GetUsageStatistics();

        if (stats.TotalRequests == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No cost data available yet.[/]");
            return;
        }

        // Display header
        var rule = new Rule("[bold blue]💰 Cost Breakdown[/]");
        rule.LeftJustified();
        AnsiConsole.Write(rule);
        AnsiConsole.WriteLine();

        // Overall costs
        var totalTokens = stats.TotalPromptTokens + stats.TotalCompletionTokens;

        var summaryTable = new Table();
        summaryTable.Border(TableBorder.Rounded);
        summaryTable.AddColumn("[bold]Summary[/]");
        summaryTable.AddColumn("[bold]Value[/]");

        summaryTable.AddRow("Total Cost", $"[green]${stats.TotalCost:F4}[/]");
        summaryTable.AddRow("Total Requests", $"{stats.TotalRequests:N0}");
        summaryTable.AddRow("Total Tokens", $"{totalTokens:N0}");
        summaryTable.AddRow("Prompt Tokens", $"{stats.TotalPromptTokens:N0}");
        summaryTable.AddRow("Completion Tokens", $"{stats.TotalCompletionTokens:N0}");
        summaryTable.AddRow("Average Cost/Request", $"${(stats.TotalCost / stats.TotalRequests):F4}");
        summaryTable.AddRow("Total Sessions", $"{stats.SessionCount:N0}");

        AnsiConsole.Write(summaryTable);
        AnsiConsole.WriteLine();

        // Model costs breakdown
        if (stats.ModelBreakdown.Any())
        {
            var modelTable = new Table();
            modelTable.Border(TableBorder.Rounded);
            modelTable.AddColumn("[bold]Model[/]");
            modelTable.AddColumn("[bold]Requests[/]", c => c.RightAligned());
            modelTable.AddColumn("[bold]Tokens[/]", c => c.RightAligned());
            modelTable.AddColumn("[bold]Total Cost[/]", c => c.RightAligned());
            modelTable.AddColumn("[bold]% of Total[/]", c => c.RightAligned());

            foreach (var kvp in stats.ModelBreakdown.OrderByDescending(m => m.Value.TotalCost))
            {
                var model = kvp.Value;
                var percentage = (model.TotalCost / stats.TotalCost) * 100;
                var modelTotalTokens = model.TotalPromptTokens + model.TotalCompletionTokens;
                
                modelTable.AddRow(
                    model.ModelName,
                    $"{model.RequestCount:N0}",
                    $"{modelTotalTokens:N0}",
                    $"${model.TotalCost:F4}",
                    $"{percentage:F1}%"
                );
            }

            AnsiConsole.Write(modelTable);
        }
    }

    /// <summary>
    /// Executes the costs command for a specific session.
    /// </summary>
    public void ExecuteForSession(string sessionId)
    {
        var summary = _costTracker.GetSessionSummary(sessionId);

        if (summary.RequestCount == 0)
        {
            AnsiConsole.MarkupLine($"[red]No cost data found for session: {sessionId}[/]");
            return;
        }

        var rule = new Rule($"[bold blue]💰 Cost Details for Session[/]");
        rule.LeftJustified();
        AnsiConsole.Write(rule);
        AnsiConsole.WriteLine();

        // Session overview
        var overviewTable = new Table();
        overviewTable.Border(TableBorder.Rounded);
        overviewTable.AddColumn("[bold]Metric[/]");
        overviewTable.AddColumn("[bold]Value[/]");

        var totalTokens = summary.TotalPromptTokens + summary.TotalCompletionTokens;

        overviewTable.AddRow("Session ID", sessionId);
        overviewTable.AddRow("Total Cost", $"[green]${summary.TotalCost:F4}[/]");
        overviewTable.AddRow("Request Count", $"{summary.RequestCount:N0}");
        overviewTable.AddRow("Total Tokens", $"{totalTokens:N0}");
        overviewTable.AddRow("Prompt Tokens", $"{summary.TotalPromptTokens:N0}");
        overviewTable.AddRow("Completion Tokens", $"{summary.TotalCompletionTokens:N0}");
        
        if (summary.FirstRequest.HasValue && summary.LastRequest.HasValue)
        {
            overviewTable.AddRow("First Request", summary.FirstRequest.Value.ToString("yyyy-MM-dd HH:mm:ss"));
            overviewTable.AddRow("Last Request", summary.LastRequest.Value.ToString("yyyy-MM-dd HH:mm:ss"));
            overviewTable.AddRow("Duration", $"{(summary.LastRequest.Value - summary.FirstRequest.Value).TotalMinutes:F1} minutes");
        }

        AnsiConsole.Write(overviewTable);
        AnsiConsole.WriteLine();

        // Per-model costs
        if (summary.ModelBreakdown.Any())
        {
            var modelTable = new Table();
            modelTable.Border(TableBorder.Rounded);
            modelTable.AddColumn("[bold]Model[/]");
            modelTable.AddColumn("[bold]Requests[/]", c => c.RightAligned());
            modelTable.AddColumn("[bold]Cost[/]", c => c.RightAligned());
            modelTable.AddColumn("[bold]% of Session[/]", c => c.RightAligned());

            foreach (var kvp in summary.ModelBreakdown.OrderByDescending(m => m.Value.TotalCost))
            {
                var model = kvp.Value;
                var percentage = (model.TotalCost / summary.TotalCost) * 100;
                modelTable.AddRow(
                    model.ModelName,
                    $"{model.RequestCount:N0}",
                    $"${model.TotalCost:F4}",
                    $"{percentage:F1}%"
                );
            }

            AnsiConsole.Write(modelTable);
        }
    }

    /// <summary>
    /// Exports cost data to JSON.
    /// </summary>
    public void Export(string outputPath)
    {
        string jsonData = string.Empty;
        
        AnsiConsole.Status()
            .Start("Exporting cost data...", ctx =>
            {
                jsonData = _costTracker.ExportToJson();
            });

        File.WriteAllText(outputPath, jsonData);
        AnsiConsole.MarkupLine($"[green]✓[/] Cost data exported to: {outputPath}");
    }
}
