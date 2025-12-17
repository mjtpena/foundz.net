using System.Text;
using System.Text.Json;
using Foundz.Net.Shared.Models;
using Microsoft.Extensions.Logging;

namespace Foundz.Net.Core.Orchestration;

/// <summary>
/// Formats tool results for AI consumption in different formats.
/// </summary>
public class ToolResultFormatter
{
    private readonly ILogger<ToolResultFormatter> _logger;
    private readonly int _maxOutputLength;

    public ToolResultFormatter(
        ILogger<ToolResultFormatter> logger,
        int maxOutputLength = 10000)
    {
        _logger = logger;
        _maxOutputLength = maxOutputLength;
    }

    /// <summary>
    /// Formats tool results for OpenAI format.
    /// </summary>
    public List<object> FormatForOpenAI(List<ToolResult> results)
    {
        var formatted = new List<object>();

        foreach (var result in results)
        {
            var content = FormatResultContent(result);
            
            formatted.Add(new
            {
                role = "tool",
                tool_call_id = result.ToolCallId,
                name = result.ToolName,
                content = content
            });
        }

        return formatted;
    }

    /// <summary>
    /// Formats tool results for Anthropic format.
    /// </summary>
    public List<object> FormatForAnthropic(List<ToolResult> results)
    {
        var formatted = new List<object>();

        foreach (var result in results)
        {
            var content = FormatResultContent(result);
            
            formatted.Add(new
            {
                type = "tool_result",
                tool_use_id = result.ToolCallId,
                content = content,
                is_error = !result.Success
            });
        }

        return formatted;
    }

    /// <summary>
    /// Formats a single tool result as a message.
    /// </summary>
    public Message FormatAsMessage(ToolResult result)
    {
        var content = FormatResultContent(result);

        return new Message
        {
            Role = MessageRole.Tool,
            Content = content,
            ToolCallId = result.ToolCallId,
            ToolName = result.ToolName
        };
    }

    /// <summary>
    /// Formats multiple tool results as messages.
    /// </summary>
    public List<Message> FormatAsMessages(List<ToolResult> results)
    {
        return results.Select(FormatAsMessage).ToList();
    }

    /// <summary>
    /// Formats the content of a tool result with truncation and formatting.
    /// </summary>
    private string FormatResultContent(ToolResult result)
    {
        var sb = new StringBuilder();

        if (result.Success)
        {
            sb.AppendLine("✅ Tool executed successfully");
            
            if (!string.IsNullOrWhiteSpace(result.Output))
            {
                var output = TruncateOutput(result.Output);
                sb.AppendLine();
                sb.AppendLine("Output:");
                sb.AppendLine(output);
            }
        }
        else
        {
            sb.AppendLine("❌ Tool execution failed");
            
            if (!string.IsNullOrWhiteSpace(result.Error))
            {
                sb.AppendLine();
                sb.AppendLine("Error:");
                sb.AppendLine(result.Error);
            }
        }

        // Add execution time
        sb.AppendLine();
        sb.AppendLine($"Execution time: {result.ExecutionTimeMs}ms");

        // Add metadata if present
        if (result.Metadata != null && result.Metadata.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("Metadata:");
            foreach (var kvp in result.Metadata)
            {
                sb.AppendLine($"  {kvp.Key}: {kvp.Value}");
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Truncates output to maximum length.
    /// </summary>
    private string TruncateOutput(string output)
    {
        if (string.IsNullOrEmpty(output))
            return output;

        if (output.Length <= _maxOutputLength)
            return output;

        _logger.LogDebug("Truncating output from {Original} to {Max} characters",
            output.Length, _maxOutputLength);

        var truncated = output.Substring(0, _maxOutputLength);
        var remaining = output.Length - _maxOutputLength;

        return $"{truncated}\n\n... [truncated {remaining} characters]";
    }

    /// <summary>
    /// Formats tool results as a summary for the AI.
    /// </summary>
    public string FormatSummary(List<ToolResult> results)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine($"Executed {results.Count} tool(s):");
        sb.AppendLine();

        foreach (var result in results)
        {
            var status = result.Success ? "✅ SUCCESS" : "❌ FAILED";
            sb.AppendLine($"- {result.ToolName}: {status} ({result.ExecutionTimeMs}ms)");
            
            if (!result.Success && !string.IsNullOrWhiteSpace(result.Error))
            {
                sb.AppendLine($"  Error: {TruncateOutput(result.Error)}");
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Formats tool results for user display (terminal output).
    /// </summary>
    public string FormatForDisplay(ToolResult result)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"╔══ Tool: {result.ToolName} ══╗");
        sb.AppendLine($"Status: {(result.Success ? "✅ Success" : "❌ Failed")}");
        sb.AppendLine($"Duration: {result.ExecutionTimeMs}ms");
        sb.AppendLine();

        if (result.Success && !string.IsNullOrWhiteSpace(result.Output))
        {
            sb.AppendLine("Output:");
            sb.AppendLine(result.Output);
        }
        else if (!result.Success && !string.IsNullOrWhiteSpace(result.Error))
        {
            sb.AppendLine("Error:");
            sb.AppendLine(result.Error);
        }

        sb.AppendLine("╚════════════════════════════╝");

        return sb.ToString();
    }

    /// <summary>
    /// Converts tool result to JSON.
    /// </summary>
    public string ToJson(ToolResult result)
    {
        return JsonSerializer.Serialize(result, new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }

    /// <summary>
    /// Highlights errors in tool results.
    /// </summary>
    public string HighlightErrors(List<ToolResult> results)
    {
        var errors = results.Where(r => !r.Success).ToList();
        
        if (errors.Count == 0)
            return "All tools executed successfully.";

        var sb = new StringBuilder();
        sb.AppendLine($"⚠️ {errors.Count} tool(s) failed:");
        sb.AppendLine();

        foreach (var error in errors)
        {
            sb.AppendLine($"❌ {error.ToolName}");
            if (!string.IsNullOrWhiteSpace(error.Error))
            {
                sb.AppendLine($"   {error.Error}");
            }
            sb.AppendLine();
        }

        return sb.ToString();
    }
}
