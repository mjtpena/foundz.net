using Foundz.Net.Core.AI;
using Foundz.Net.Core.Orchestration;
using Foundz.Net.Core.Session;
using Foundz.Net.Shared.Models;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace Foundz.Net.Cli.Commands;

/// <summary>
/// Interactive chat command for conversational agent interactions.
/// </summary>
public class ChatCommand
{
    private readonly AgentOrchestrator _orchestrator;
    private readonly SessionManager _sessionManager;
    private readonly CostTracker? _costTracker;
    private readonly RequestMetricsCollector? _metricsCollector;
    private readonly ILogger<ChatCommand> _logger;

    public ChatCommand(
        AgentOrchestrator orchestrator,
        SessionManager sessionManager,
        ILogger<ChatCommand> logger,
        CostTracker? costTracker = null,
        RequestMetricsCollector? metricsCollector = null)
    {
        _orchestrator = orchestrator;
        _sessionManager = sessionManager;
        _logger = logger;
        _costTracker = costTracker;
        _metricsCollector = metricsCollector;
    }

    /// <summary>
    /// Executes the interactive chat command.
    /// </summary>
    public async Task ExecuteAsync(string? sessionId = null, CancellationToken cancellationToken = default)
    {
        // Display welcome banner
        DisplayWelcomeBanner();

        // Load or create session
        var session = LoadOrCreateSession(sessionId);

        AnsiConsole.MarkupLine($"[green]Session:[/] {session.Id}");
        AnsiConsole.MarkupLine("[grey]Type '/help' for commands, '/exit' to quit[/]");
        AnsiConsole.WriteLine();

        // Chat loop
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // Get user input
                var userMessage = AnsiConsole.Prompt(
                    new TextPrompt<string>("[blue]You:[/]")
                        .AllowEmpty());

                if (string.IsNullOrWhiteSpace(userMessage))
                    continue;

                // Check for slash commands
                if (userMessage.StartsWith('/'))
                {
                    if (await HandleSlashCommandAsync(userMessage, session, cancellationToken))
                        break; // Exit requested
                    continue;
                }

                // Send to agent orchestrator
                await ProcessUserMessageAsync(userMessage, session, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                AnsiConsole.MarkupLine("[yellow]Chat interrupted.[/]");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing chat message");
                AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            }
        }

        // Update session before exit
        _sessionManager.UpdateSession(session);
        
        DisplayGoodbyeMessage(session);
    }

    /// <summary>
    /// Processes a user message through the agent.
    /// </summary>
    private async Task ProcessUserMessageAsync(
        string userMessage,
        Foundz.Net.Shared.Models.Session session,
        CancellationToken cancellationToken)
    {
        AnsiConsole.WriteLine();

        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync("[grey]Thinking...[/]", async ctx =>
            {
                var response = await _orchestrator.ProcessMessageAsync(
                    userMessage,
                    session,
                    cancellationToken);

                ctx.Status("[grey]Processing response...[/]");

                // Display agent response
                AnsiConsole.WriteLine();
                var panel = new Panel(response.Content)
                {
                    Header = new PanelHeader("🤖 Assistant", Justify.Left),
                    Border = BoxBorder.Rounded,
                    BorderStyle = new Style(Color.Blue)
                };
                AnsiConsole.Write(panel);
                AnsiConsole.WriteLine();

                // Display tool results if any
                if (response.ToolResults.Any())
                {
                    DisplayToolResults(response.ToolResults);
                }

                // Display token usage
                if (response.TokenUsage != null)
                {
                    AnsiConsole.MarkupLine(
                        $"[grey]Tokens: {response.TokenUsage.PromptTokens} in, {response.TokenUsage.CompletionTokens} out " +
                        $"({response.TokenUsage.TotalTokens} total)[/]");
                }

                // Display cost if available
                if (_costTracker != null)
                {
                    var sessionCost = _costTracker.GetSessionCost(session.Id.ToString());
                    if (sessionCost > 0)
                    {
                        AnsiConsole.MarkupLine($"[grey]Session cost: ${sessionCost:F4}[/]");
                    }
                }

                AnsiConsole.WriteLine();
            });
    }

    /// <summary>
    /// Handles slash commands.
    /// </summary>
    private async Task<bool> HandleSlashCommandAsync(
        string command,
        Foundz.Net.Shared.Models.Session session,
        CancellationToken cancellationToken)
    {
        var parts = command.TrimStart('/').Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var cmd = parts[0].ToLowerInvariant();

        switch (cmd)
        {
            case "help":
                DisplayHelp();
                return false;

            case "exit" or "quit":
                return true;

            case "clear":
                AnsiConsole.Clear();
                DisplayWelcomeBanner();
                return false;

            case "history":
                DisplayHistory(session);
                return false;

            case "stats":
                DisplaySessionStats(session);
                return false;

            case "cost":
                DisplaySessionCost(session);
                return false;

            case "save":
                _sessionManager.UpdateSession(session);
                AnsiConsole.MarkupLine("[green]Session saved successfully.[/]");
                return false;

            case "reset":
                session.Messages.Clear();
                AnsiConsole.MarkupLine("[yellow]Conversation reset. Starting fresh.[/]");
                return false;

            default:
                AnsiConsole.MarkupLine($"[red]Unknown command:[/] /{cmd}");
                AnsiConsole.MarkupLine("[grey]Type '/help' for available commands.[/]");
                return false;
        }
    }

    /// <summary>
    /// Loads an existing session or creates a new one.
    /// </summary>
    private Foundz.Net.Shared.Models.Session LoadOrCreateSession(string? sessionId)
    {
        if (!string.IsNullOrEmpty(sessionId) && Guid.TryParse(sessionId, out var guid))
        {
            var existing = _sessionManager.GetSession(guid);
            if (existing != null)
            {
                AnsiConsole.MarkupLine("[green]Loaded existing session.[/]");
                return existing;
            }
        }

        var newSession = _sessionManager.CreateSession("Interactive Chat");
        AnsiConsole.MarkupLine("[green]Created new session.[/]");
        return newSession;
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

        foreach (var result in results)
        {
            var status = result.Success
                ? "[green]✓ Success[/]"
                : "[red]✗ Failed[/]";

            table.AddRow(
                result.ToolName,
                status,
                $"{result.ExecutionTimeMs}ms"
            );
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
    }

    /// <summary>
    /// Displays conversation history.
    /// </summary>
    private void DisplayHistory(Foundz.Net.Shared.Models.Session session)
    {
        if (!session.Messages.Any())
        {
            AnsiConsole.MarkupLine("[grey]No messages in history.[/]");
            return;
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold]Conversation History:[/]");
        AnsiConsole.WriteLine();

        foreach (var message in session.Messages.TakeLast(10))
        {
            var roleColor = message.Role switch
            {
                MessageRole.User => "blue",
                MessageRole.Assistant => "green",
                MessageRole.System => "grey",
                _ => "white"
            };

            var roleText = message.Role switch
            {
                MessageRole.User => "You",
                MessageRole.Assistant => "Assistant",
                MessageRole.System => "System",
                _ => message.Role.ToString()
            };

            AnsiConsole.MarkupLine($"[{roleColor}]{roleText}:[/] {message.Content}");
            AnsiConsole.WriteLine();
        }
    }

    /// <summary>
    /// Displays session statistics.
    /// </summary>
    private void DisplaySessionStats(Foundz.Net.Shared.Models.Session session)
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Blue);

        table.AddColumn("[bold]Metric[/]");
        table.AddColumn("[bold]Value[/]");

        table.AddRow("Session ID", session.Id.ToString());
        table.AddRow("Messages", session.Messages.Count.ToString());
        table.AddRow("Created", session.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"));
        table.AddRow("Last Activity", session.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss"));

        var totalTokens = session.Messages.Sum(m => m.TokenCount ?? 0);
        table.AddRow("Total Tokens", totalTokens.ToString("N0"));

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
    }

    /// <summary>
    /// Displays session cost information.
    /// </summary>
    private void DisplaySessionCost(Foundz.Net.Shared.Models.Session session)
    {
        if (_costTracker == null)
        {
            AnsiConsole.MarkupLine("[yellow]Cost tracking not enabled.[/]");
            return;
        }

        var summary = _costTracker.GetSessionSummary(session.Id.ToString());

        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Green);

        table.AddColumn("[bold]Cost Metric[/]");
        table.AddColumn("[bold]Value[/]");

        table.AddRow("Total Cost", $"[green]${summary.TotalCost:F4}[/]");
        table.AddRow("Requests", summary.RequestCount.ToString());
        table.AddRow("Prompt Tokens", summary.TotalPromptTokens.ToString("N0"));
        table.AddRow("Completion Tokens", summary.TotalCompletionTokens.ToString("N0"));

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
    }

    /// <summary>
    /// Displays welcome banner.
    /// </summary>
    private void DisplayWelcomeBanner()
    {
        AnsiConsole.Clear();
        
        var rule = new Rule("[bold blue]🤖 Foundz.Net Interactive Chat[/]")
        {
            Justification = Justify.Left
        };
        AnsiConsole.Write(rule);
        AnsiConsole.WriteLine();
    }

    /// <summary>
    /// Displays help information.
    /// </summary>
    private void DisplayHelp()
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Blue);

        table.AddColumn("[bold]Command[/]");
        table.AddColumn("[bold]Description[/]");

        table.AddRow("/help", "Show this help message");
        table.AddRow("/exit, /quit", "Exit the chat");
        table.AddRow("/clear", "Clear the screen");
        table.AddRow("/history", "Show conversation history");
        table.AddRow("/stats", "Show session statistics");
        table.AddRow("/cost", "Show session cost");
        table.AddRow("/save", "Save current session");
        table.AddRow("/reset", "Reset conversation (clear history)");

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
    }

    /// <summary>
    /// Displays goodbye message with session summary.
    /// </summary>
    private void DisplayGoodbyeMessage(Foundz.Net.Shared.Models.Session session)
    {
        AnsiConsole.WriteLine();
        var rule = new Rule("[bold blue]Session Complete[/]")
        {
            Justification = Justify.Left
        };
        AnsiConsole.Write(rule);
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine($"[grey]Session ID:[/] {session.Id}");
        AnsiConsole.MarkupLine($"[grey]Messages:[/] {session.Messages.Count}");

        if (_costTracker != null)
        {
            var cost = _costTracker.GetSessionCost(session.Id.ToString());
            if (cost > 0)
            {
                AnsiConsole.MarkupLine($"[grey]Total Cost:[/] ${cost:F4}");
            }
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[blue]Thank you for using Foundz.Net![/]");
    }
}
