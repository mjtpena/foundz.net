using System.Text.Json;
using Foundz.Net.Shared.Interfaces;
using Foundz.Net.Shared.Models;
using Microsoft.Extensions.Logging;

namespace Foundz.Net.Core.Orchestration;

/// <summary>
/// Parses tool calls from AI responses in different formats (OpenAI, Anthropic, etc.).
/// </summary>
public class ToolCallParser
{
    private readonly ILogger<ToolCallParser> _logger;

    public ToolCallParser(ILogger<ToolCallParser> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Parses tool calls from an AI response.
    /// </summary>
    public List<ToolCall> ParseToolCalls(AIResponse response)
    {
        if (response.ToolCalls == null || response.ToolCalls.Count == 0)
            return new List<ToolCall>();

        var parsedCalls = new List<ToolCall>();

        foreach (var toolCall in response.ToolCalls)
        {
            try
            {
                // Validate tool call
                if (string.IsNullOrWhiteSpace(toolCall.Name))
                {
                    _logger.LogWarning("Tool call missing name, skipping");
                    continue;
                }

                // Ensure arguments is not null
                var arguments = toolCall.Arguments ?? new Dictionary<string, object>();

                parsedCalls.Add(new ToolCall
                {
                    Id = toolCall.Id,
                    Name = toolCall.Name,
                    Arguments = arguments
                });

                _logger.LogDebug("Parsed tool call: {ToolName} with {ArgCount} arguments",
                    toolCall.Name, arguments.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse tool call: {ToolName}", toolCall.Name);
            }
        }

        return parsedCalls;
    }

    /// <summary>
    /// Parses tool calls from OpenAI function calling format.
    /// </summary>
    public List<ToolCall> ParseOpenAIFormat(string responseJson)
    {
        var toolCalls = new List<ToolCall>();

        try
        {
            using var doc = JsonDocument.Parse(responseJson);
            var root = doc.RootElement;

            if (root.TryGetProperty("tool_calls", out var toolCallsArray))
            {
                foreach (var toolCallElement in toolCallsArray.EnumerateArray())
                {
                    var toolCall = ParseOpenAIToolCall(toolCallElement);
                    if (toolCall != null)
                        toolCalls.Add(toolCall);
                }
            }
            // Also check for single function_call (legacy format)
            else if (root.TryGetProperty("function_call", out var functionCall))
            {
                var toolCall = ParseOpenAIFunctionCall(functionCall);
                if (toolCall != null)
                    toolCalls.Add(toolCall);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse OpenAI format tool calls");
        }

        return toolCalls;
    }

    /// <summary>
    /// Parses a single OpenAI tool call.
    /// </summary>
    private ToolCall? ParseOpenAIToolCall(JsonElement element)
    {
        try
        {
            var id = element.GetProperty("id").GetString() ?? Guid.NewGuid().ToString();
            var type = element.GetProperty("type").GetString();

            if (type != "function")
                return null;

            var function = element.GetProperty("function");
            var name = function.GetProperty("name").GetString();
            var argumentsJson = function.GetProperty("arguments").GetString();

            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(argumentsJson))
                return null;

            var arguments = JsonSerializer.Deserialize<Dictionary<string, object>>(argumentsJson)
                ?? new Dictionary<string, object>();

            return new ToolCall
            {
                Id = id,
                Name = name,
                Arguments = arguments
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse OpenAI tool call element");
            return null;
        }
    }

    /// <summary>
    /// Parses a single OpenAI function call (legacy format).
    /// </summary>
    private ToolCall? ParseOpenAIFunctionCall(JsonElement element)
    {
        try
        {
            var name = element.GetProperty("name").GetString();
            var argumentsJson = element.GetProperty("arguments").GetString();

            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(argumentsJson))
                return null;

            var arguments = JsonSerializer.Deserialize<Dictionary<string, object>>(argumentsJson)
                ?? new Dictionary<string, object>();

            return new ToolCall
            {
                Id = Guid.NewGuid().ToString(),
                Name = name,
                Arguments = arguments
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse OpenAI function call element");
            return null;
        }
    }

    /// <summary>
    /// Parses tool calls from Anthropic tool use format.
    /// </summary>
    public List<ToolCall> ParseAnthropicFormat(string responseJson)
    {
        var toolCalls = new List<ToolCall>();

        try
        {
            using var doc = JsonDocument.Parse(responseJson);
            var root = doc.RootElement;

            if (root.TryGetProperty("content", out var content))
            {
                foreach (var block in content.EnumerateArray())
                {
                    if (block.TryGetProperty("type", out var type) && 
                        type.GetString() == "tool_use")
                    {
                        var toolCall = ParseAnthropicToolUse(block);
                        if (toolCall != null)
                            toolCalls.Add(toolCall);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse Anthropic format tool calls");
        }

        return toolCalls;
    }

    /// <summary>
    /// Parses a single Anthropic tool_use block.
    /// </summary>
    private ToolCall? ParseAnthropicToolUse(JsonElement element)
    {
        try
        {
            var id = element.GetProperty("id").GetString() ?? Guid.NewGuid().ToString();
            var name = element.GetProperty("name").GetString();
            var input = element.GetProperty("input");

            if (string.IsNullOrWhiteSpace(name))
                return null;

            // Convert JsonElement to Dictionary
            var arguments = JsonSerializer.Deserialize<Dictionary<string, object>>(input.GetRawText())
                ?? new Dictionary<string, object>();

            return new ToolCall
            {
                Id = id,
                Name = name,
                Arguments = arguments
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse Anthropic tool_use block");
            return null;
        }
    }

    /// <summary>
    /// Validates that all tool calls reference registered tools.
    /// </summary>
    public bool ValidateToolCalls(List<ToolCall> toolCalls, IToolRegistry toolRegistry)
    {
        foreach (var toolCall in toolCalls)
        {
            var tool = toolRegistry.GetTool(toolCall.Name);
            if (tool == null)
            {
                _logger.LogWarning("Tool call references unknown tool: {ToolName}", toolCall.Name);
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Extracts tool names from a list of tool calls.
    /// </summary>
    public List<string> GetToolNames(List<ToolCall> toolCalls)
    {
        return toolCalls.Select(tc => tc.Name).Distinct().ToList();
    }
}
