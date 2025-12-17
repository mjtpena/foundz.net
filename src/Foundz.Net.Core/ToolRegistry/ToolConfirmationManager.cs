using Foundz.Net.Shared.Interfaces;
using Foundz.Net.Shared.Models;
using Microsoft.Extensions.Logging;

namespace Foundz.Net.Core.ToolRegistry;

/// <summary>
/// Manages tool confirmations for dangerous operations.
/// </summary>
public class ToolConfirmationManager
{
    private readonly IToolRegistry _toolRegistry;
    private readonly ILogger<ToolConfirmationManager> _logger;
    private readonly bool _autoConfirm;
    private readonly List<ToolConfirmation> _confirmationHistory = new();

    public ToolConfirmationManager(
        IToolRegistry toolRegistry,
        ILogger<ToolConfirmationManager> logger,
        bool autoConfirm = false)
    {
        _toolRegistry = toolRegistry;
        _logger = logger;
        _autoConfirm = autoConfirm;
    }

    /// <summary>
    /// Checks if a tool call requires confirmation.
    /// </summary>
    public bool RequiresConfirmation(ToolCall toolCall)
    {
        var tool = _toolRegistry.GetTool(toolCall.Name);
        if (tool == null)
            return false;

        return tool.RequiresConfirmation || tool.DangerLevel >= DangerLevel.Warning;
    }

    /// <summary>
    /// Requests confirmation for a tool call.
    /// </summary>
    public async Task<ConfirmationResult> RequestConfirmationAsync(
        ToolCall toolCall,
        Func<ToolConfirmationPrompt, Task<bool>> promptCallback,
        CancellationToken cancellationToken = default)
    {
        if (_autoConfirm)
        {
            _logger.LogInformation("Auto-confirming tool: {ToolName} (--yes mode)", toolCall.Name);
            return ConfirmationResult.Confirmed;
        }

        var tool = _toolRegistry.GetTool(toolCall.Name);
        if (tool == null)
        {
            _logger.LogWarning("Cannot confirm unknown tool: {ToolName}", toolCall.Name);
            return ConfirmationResult.Rejected;
        }

        var prompt = CreateConfirmationPrompt(tool, toolCall);
        
        _logger.LogInformation("Requesting confirmation for tool: {ToolName}", toolCall.Name);

        try
        {
            var confirmed = await promptCallback(prompt);
            
            var confirmation = new ToolConfirmation
            {
                ToolName = toolCall.Name,
                ToolCallId = toolCall.Id,
                Confirmed = confirmed,
                Timestamp = DateTime.UtcNow,
                Arguments = toolCall.Arguments
            };

            _confirmationHistory.Add(confirmation);
            
            _logger.LogInformation(
                "Tool {ToolName} confirmation: {Result}",
                toolCall.Name, confirmed ? "CONFIRMED" : "REJECTED");

            return confirmed ? ConfirmationResult.Confirmed : ConfirmationResult.Rejected;
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Confirmation cancelled for tool: {ToolName}", toolCall.Name);
            return ConfirmationResult.Cancelled;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during confirmation for tool: {ToolName}", toolCall.Name);
            return ConfirmationResult.Error;
        }
    }

    /// <summary>
    /// Creates a confirmation prompt with details.
    /// </summary>
    private ToolConfirmationPrompt CreateConfirmationPrompt(ITool tool, ToolCall toolCall)
    {
        return new ToolConfirmationPrompt
        {
            ToolName = tool.Name,
            Description = tool.Description,
            Category = tool.Category,
            DangerLevel = tool.DangerLevel,
            Arguments = toolCall.Arguments,
            Warning = GetWarningMessage(tool),
            Examples = tool.GetExamples().ToList()
        };
    }

    /// <summary>
    /// Gets an appropriate warning message based on danger level.
    /// </summary>
    private string GetWarningMessage(ITool tool)
    {
        return tool.DangerLevel switch
        {
            DangerLevel.Danger => "⚠️  DANGER: This operation may cause irreversible changes!",
            DangerLevel.Warning => "⚠️  Warning: This operation will modify your system.",
            DangerLevel.Safe => "ℹ️  This operation is safe and read-only.",
            _ => string.Empty
        };
    }

    /// <summary>
    /// Gets the confirmation history.
    /// </summary>
    public IReadOnlyList<ToolConfirmation> GetHistory() => _confirmationHistory.AsReadOnly();

    /// <summary>
    /// Clears the confirmation history.
    /// </summary>
    public void ClearHistory()
    {
        _confirmationHistory.Clear();
        _logger.LogDebug("Cleared confirmation history");
    }
}

/// <summary>
/// Represents a confirmation prompt for a tool.
/// </summary>
public record ToolConfirmationPrompt
{
    public required string ToolName { get; init; }
    public required string Description { get; init; }
    public required ToolCategory Category { get; init; }
    public required DangerLevel DangerLevel { get; init; }
    public required Dictionary<string, object> Arguments { get; init; }
    public required string Warning { get; init; }
    public required List<string> Examples { get; init; }
}

/// <summary>
/// Represents the result of a confirmation request.
/// </summary>
public enum ConfirmationResult
{
    Confirmed,
    Rejected,
    Cancelled,
    Error
}

/// <summary>
/// Represents a tool confirmation record.
/// </summary>
public record ToolConfirmation
{
    public required string ToolName { get; init; }
    public required string ToolCallId { get; init; }
    public required bool Confirmed { get; init; }
    public required DateTime Timestamp { get; init; }
    public required Dictionary<string, object> Arguments { get; init; }
}
