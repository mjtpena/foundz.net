using System.Diagnostics;
using Foundz.Net.Shared.Interfaces;
using Foundz.Net.Shared.Models;
using Microsoft.Extensions.Logging;

namespace Foundz.Net.Core.ToolRegistry;

/// <summary>
/// Executes tools with timeout, resource limits, and comprehensive error handling.
/// </summary>
public class ToolExecutor
{
    private readonly IToolRegistry _toolRegistry;
    private readonly ILogger<ToolExecutor> _logger;
    private readonly int _defaultTimeoutSeconds;

    public ToolExecutor(
        IToolRegistry toolRegistry,
        ILogger<ToolExecutor> logger,
        int defaultTimeoutSeconds = 30)
    {
        _toolRegistry = toolRegistry;
        _logger = logger;
        _defaultTimeoutSeconds = defaultTimeoutSeconds;
    }

    /// <summary>
    /// Executes a tool call with timeout and error handling.
    /// </summary>
    public async Task<ToolResult> ExecuteAsync(
        ToolCall toolCall,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var tool = _toolRegistry.GetTool(toolCall.Name);

        if (tool == null)
        {
            _logger.LogError("Tool not found: {ToolName}", toolCall.Name);
            return new ToolResult
            {
                ToolCallId = toolCall.Id,
                ToolName = toolCall.Name,
                Success = false,
                Error = $"Tool '{toolCall.Name}' not found in registry",
                ExecutionTimeMs = (int)stopwatch.ElapsedMilliseconds
            };
        }

        try
        {
            _logger.LogInformation("Executing tool: {ToolName} with ID: {ToolCallId}", 
                toolCall.Name, toolCall.Id);

            // Validate arguments
            var isValid = await tool.ValidateArgsAsync(toolCall.Arguments, cancellationToken);
            if (!isValid)
            {
                _logger.LogWarning("Tool {ToolName} validation failed for arguments", toolCall.Name);
                return new ToolResult
                {
                    ToolCallId = toolCall.Id,
                    ToolName = toolCall.Name,
                    Success = false,
                    Error = "Invalid arguments provided",
                    ExecutionTimeMs = (int)stopwatch.ElapsedMilliseconds
                };
            }

            // Execute with timeout
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(_defaultTimeoutSeconds));

            var result = await tool.ExecuteAsync(toolCall.Arguments, cts.Token);
            stopwatch.Stop();

            // Enhance result with additional metadata
            var enhancedResult = result with
            {
                ToolCallId = toolCall.Id,
                ExecutionTimeMs = (int)stopwatch.ElapsedMilliseconds,
                Metadata = EnhanceMetadata(result.Metadata, tool, stopwatch.Elapsed)
            };

            _logger.LogInformation(
                "Tool {ToolName} completed: Success={Success}, Duration={Duration}ms",
                toolCall.Name, enhancedResult.Success, enhancedResult.ExecutionTimeMs);

            return enhancedResult;
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            _logger.LogWarning("Tool {ToolName} execution was cancelled", toolCall.Name);
            return new ToolResult
            {
                ToolCallId = toolCall.Id,
                ToolName = toolCall.Name,
                Success = false,
                Error = "Execution was cancelled or timed out",
                ExecutionTimeMs = (int)stopwatch.ElapsedMilliseconds
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Tool {ToolName} execution failed with exception", toolCall.Name);
            return new ToolResult
            {
                ToolCallId = toolCall.Id,
                ToolName = toolCall.Name,
                Success = false,
                Error = $"Exception during execution: {ex.Message}",
                ExecutionTimeMs = (int)stopwatch.ElapsedMilliseconds,
                Metadata = new Dictionary<string, object>
                {
                    ["ExceptionType"] = ex.GetType().Name,
                    ["StackTrace"] = ex.StackTrace ?? string.Empty
                }
            };
        }
    }

    /// <summary>
    /// Executes multiple tools in parallel where safe.
    /// </summary>
    public async Task<List<ToolResult>> ExecuteParallelAsync(
        List<ToolCall> toolCalls,
        CancellationToken cancellationToken = default)
    {
        if (toolCalls == null || toolCalls.Count == 0)
            return new List<ToolResult>();

        _logger.LogInformation("Executing {Count} tools in parallel", toolCalls.Count);

        var tasks = toolCalls.Select(tc => ExecuteAsync(tc, cancellationToken));
        var results = await Task.WhenAll(tasks);

        return results.ToList();
    }

    /// <summary>
    /// Executes tools in sequence.
    /// </summary>
    public async Task<List<ToolResult>> ExecuteSequentialAsync(
        List<ToolCall> toolCalls,
        CancellationToken cancellationToken = default)
    {
        if (toolCalls == null || toolCalls.Count == 0)
            return new List<ToolResult>();

        _logger.LogInformation("Executing {Count} tools sequentially", toolCalls.Count);

        var results = new List<ToolResult>();
        foreach (var toolCall in toolCalls)
        {
            var result = await ExecuteAsync(toolCall, cancellationToken);
            results.Add(result);

            // Stop execution if a critical tool fails
            if (!result.Success)
            {
                var tool = _toolRegistry.GetTool(toolCall.Name);
                if (tool?.DangerLevel == DangerLevel.Danger)
                {
                    _logger.LogWarning("Critical tool {ToolName} failed, stopping execution chain", 
                        toolCall.Name);
                    break;
                }
            }
        }

        return results;
    }

    /// <summary>
    /// Determines if tools can be executed in parallel safely.
    /// </summary>
    public bool CanExecuteInParallel(List<ToolCall> toolCalls)
    {
        if (toolCalls == null || toolCalls.Count <= 1)
            return false;

        // Check if any tools are dangerous or modify state
        foreach (var toolCall in toolCalls)
        {
            var tool = _toolRegistry.GetTool(toolCall.Name);
            if (tool == null)
                continue;

            // Don't parallelize dangerous operations
            if (tool.DangerLevel >= DangerLevel.Warning)
                return false;

            // Don't parallelize file write operations
            if (tool.Category == ToolCategory.FileOperations && 
                (toolCall.Name.Contains("write", StringComparison.OrdinalIgnoreCase) ||
                 toolCall.Name.Contains("delete", StringComparison.OrdinalIgnoreCase) ||
                 toolCall.Name.Contains("edit", StringComparison.OrdinalIgnoreCase)))
                return false;

            // Don't parallelize git operations
            if (tool.Category == ToolCategory.GitOperations)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Enhances result metadata with additional information.
    /// </summary>
    private Dictionary<string, object> EnhanceMetadata(
        Dictionary<string, object>? existing,
        ITool tool,
        TimeSpan duration)
    {
        var metadata = existing ?? new Dictionary<string, object>();

        metadata["Category"] = tool.Category.ToString();
        metadata["DangerLevel"] = tool.DangerLevel.ToString();
        metadata["RequiredConfirmation"] = tool.RequiresConfirmation;
        metadata["ExecutionDuration"] = duration.TotalMilliseconds;
        metadata["Timestamp"] = DateTime.UtcNow;

        return metadata;
    }
}
