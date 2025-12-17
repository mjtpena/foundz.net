using Foundz.Net.Core.Orchestration;
using Foundz.Net.Core.Session;
using Foundz.Net.Shared.Interfaces;
using Foundz.Net.Shared.Models;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace Foundz.Net.Cli.Commands;

/// <summary>
/// One-shot task execution command.
/// </summary>
public class TaskCommand
{
    private readonly AgentOrchestrator _orchestrator;
    private readonly SessionManager _sessionManager;
    private readonly ILogger<TaskCommand> _logger;

    public TaskCommand(
        AgentOrchestrator orchestrator,
        SessionManager sessionManager,
        ILogger<TaskCommand> logger)
    {
        _orchestrator = orchestrator;
        _sessionManager = sessionManager;
        _logger = logger;
    }

    /// <summary>
    /// Executes a one-shot task.
    /// </summary>
    public async Task ExecuteAsync(
        string task,
        string? sessionId = null,
        bool verbose = false,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(task))
        {
            AnsiConsole.MarkupLine("[red]Error:[/] Task description is required.");
            return;
        }

        try
        {
            // Load or create session
            var session = LoadOrCreateSession(sessionId, task);

            if (verbose)
            {
                AnsiConsole.MarkupLine($"[grey]Session:[/] {session.Id}");
                AnsiConsole.MarkupLine($"[grey]Task:[/] {task}");
                AnsiConsole.WriteLine();
            }

            // Execute task
            AgentResponse? response = null;
            
            await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .StartAsync("[yellow]Executing task...[/]", async ctx =>
                {
                    response = await _orchestrator.ProcessMessageAsync(
                        task,
                        session,
                        cancellationToken);

                    ctx.Status("[yellow]Processing response...[/]");
                    
                    // Display response
                    if (response != null)
                    {
                        DisplayResponse(response, verbose);
                    }
                });

            // Update session
            _sessionManager.UpdateSession(session);

            if (verbose)
            {
                AnsiConsole.MarkupLine("[green]Task completed successfully.[/]");
            }
        }
        catch (OperationCanceledException)
        {
            AnsiConsole.MarkupLine("[yellow]Task execution cancelled.[/]");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Task execution failed");
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Executes a task from a file.
    /// </summary>
    public async Task ExecuteFromFileAsync(
        string filePath,
        string? sessionId = null,
        bool verbose = false,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] File not found: {filePath}");
            return;
        }

        var task = await File.ReadAllTextAsync(filePath, cancellationToken);
        await ExecuteAsync(task, sessionId, verbose, cancellationToken);
    }

    /// <summary>
    /// Executes multiple tasks in sequence.
    /// </summary>
    public async Task ExecuteBatchAsync(
        IEnumerable<string> tasks,
        string? sessionId = null,
        bool stopOnError = false,
        bool verbose = false,
        CancellationToken cancellationToken = default)
    {
        var taskList = tasks.ToList();
        
        if (!taskList.Any())
        {
            AnsiConsole.MarkupLine("[red]Error:[/] No tasks provided.");
            return;
        }

        AnsiConsole.MarkupLine($"[blue]Executing {taskList.Count} task(s)...[/]");
        AnsiConsole.WriteLine();

        var session = LoadOrCreateSession(sessionId, "Batch Execution");
        var results = new List<(string Task, bool Success, string? Error)>();

        for (int i = 0; i < taskList.Count; i++)
        {
            var task = taskList[i];
            
            AnsiConsole.MarkupLine($"[grey]Task {i + 1}/{taskList.Count}:[/] {task}");

            try
            {
                await ExecuteAsync(task, session.Id.ToString(), verbose, cancellationToken);
                results.Add((task, true, null));
                AnsiConsole.MarkupLine("[green]✓ Completed[/]");
            }
            catch (Exception ex)
            {
                results.Add((task, false, ex.Message));
                AnsiConsole.MarkupLine($"[red]✗ Failed:[/] {ex.Message}");

                if (stopOnError)
                {
                    AnsiConsole.MarkupLine("[yellow]Stopping batch execution due to error.[/]");
                    break;
                }
            }

            AnsiConsole.WriteLine();
        }

        // Display summary
        DisplayBatchSummary(results);
    }

    /// <summary>
    /// Loads or creates a session for task execution.
    /// </summary>
    private Foundz.Net.Shared.Models.Session LoadOrCreateSession(
        string? sessionId,
        string taskDescription)
    {
        if (!string.IsNullOrEmpty(sessionId) && Guid.TryParse(sessionId, out var guid))
        {
            var existing = _sessionManager.GetSession(guid);
            if (existing != null)
            {
                return existing;
            }
        }

        return _sessionManager.CreateSession(
            $"Task: {taskDescription.Substring(0, Math.Min(50, taskDescription.Length))}...");
    }

    /// <summary>
    /// Displays the agent response.
    /// </summary>
    private void DisplayResponse(AgentResponse response, bool verbose)
    {
        AnsiConsole.WriteLine();

        // Main content
        var panel = new Panel(response.Content)
        {
            Header = new PanelHeader("📋 Result", Justify.Left),
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Green)
        };
        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();

        // Tool results if verbose
        if (verbose && response.ToolResults.Any())
        {
            DisplayToolResults(response.ToolResults);
        }

        // Token usage
        if (verbose && response.TokenUsage != null)
        {
            AnsiConsole.MarkupLine(
                $"[grey]Tokens: {response.TokenUsage.PromptTokens} in, " +
                $"{response.TokenUsage.CompletionTokens} out " +
                $"({response.TokenUsage.TotalTokens} total)[/]");
            AnsiConsole.WriteLine();
        }
    }

    /// <summary>
    /// Displays tool execution results.
    /// </summary>
    private void DisplayToolResults(List<ToolResult> results)
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Grey);

        table.AddColumn("[bold]Tool[/]");
        table.AddColumn("[bold]Status[/]");
        table.AddColumn("[bold]Duration[/]");
        table.AddColumn("[bold]Output[/]");

        foreach (var result in results)
        {
            var status = result.Success
                ? "[green]✓[/]"
                : "[red]✗[/]";

            var output = result.Success
                ? result.Output?.Substring(0, Math.Min(50, result.Output?.Length ?? 0))
                : result.Error?.Substring(0, Math.Min(50, result.Error?.Length ?? 0));

            if (output?.Length == 50)
                output += "...";

            table.AddRow(
                result.ToolName,
                status,
                $"{result.ExecutionTimeMs}ms",
                output ?? ""
            );
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
    }

    /// <summary>
    /// Displays batch execution summary.
    /// </summary>
    private void DisplayBatchSummary(List<(string Task, bool Success, string? Error)> results)
    {
        AnsiConsole.WriteLine();
        var rule = new Rule("[bold blue]Batch Execution Summary[/]")
        {
            Justification = Justify.Left
        };
        AnsiConsole.Write(rule);
        AnsiConsole.WriteLine();

        var successful = results.Count(r => r.Success);
        var failed = results.Count(r => !r.Success);

        var table = new Table()
            .Border(TableBorder.Rounded);

        table.AddColumn("[bold]Metric[/]");
        table.AddColumn("[bold]Value[/]");

        table.AddRow("Total Tasks", results.Count.ToString());
        table.AddRow("Successful", $"[green]{successful}[/]");
        table.AddRow("Failed", failed > 0 ? $"[red]{failed}[/]" : "0");
        table.AddRow("Success Rate", $"{(successful * 100.0 / results.Count):F1}%");

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();

        // Show failed tasks
        var failedTasks = results.Where(r => !r.Success).ToList();
        if (failedTasks.Any())
        {
            AnsiConsole.MarkupLine("[red]Failed Tasks:[/]");
            foreach (var (task, _, error) in failedTasks)
            {
                AnsiConsole.MarkupLine($"  • {task}");
                AnsiConsole.MarkupLine($"    [grey]Error: {error}[/]");
            }
            AnsiConsole.WriteLine();
        }
    }
}
