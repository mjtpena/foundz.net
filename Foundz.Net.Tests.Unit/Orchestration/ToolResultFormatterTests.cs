using Foundz.Net.Core.Orchestration;
using Foundz.Net.Shared.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Foundz.Net.Tests.Unit.Orchestration;

public class ToolResultFormatterTests
{
    private readonly Mock<ILogger<ToolResultFormatter>> _mockLogger;
    private readonly ToolResultFormatter _formatter;

    public ToolResultFormatterTests()
    {
        _mockLogger = new Mock<ILogger<ToolResultFormatter>>();
        _formatter = new ToolResultFormatter(_mockLogger.Object, maxOutputLength: 100);
    }

    [Fact]
    public void FormatAsMessage_WithSuccessfulResult_FormatsCorrectly()
    {
        // Arrange
        var result = new ToolResult
        {
            ToolCallId = "call-123",
            ToolName = "test_tool",
            Success = true,
            Output = "Tool output",
            ExecutionTimeMs = 150
        };

        // Act
        var message = _formatter.FormatAsMessage(result);

        // Assert
        Assert.NotNull(message);
        Assert.Equal(MessageRole.Tool, message.Role);
        Assert.Equal("call-123", message.ToolCallId);
        Assert.Equal("test_tool", message.ToolName);
        Assert.Contains("✅ Tool executed successfully", message.Content);
        Assert.Contains("Tool output", message.Content);
        Assert.Contains("150ms", message.Content);
    }

    [Fact]
    public void FormatAsMessage_WithFailedResult_FormatsCorrectly()
    {
        // Arrange
        var result = new ToolResult
        {
            ToolCallId = "call-456",
            ToolName = "test_tool",
            Success = false,
            Error = "Tool failed",
            ExecutionTimeMs = 50
        };

        // Act
        var message = _formatter.FormatAsMessage(result);

        // Assert
        Assert.NotNull(message);
        Assert.Contains("❌ Tool execution failed", message.Content);
        Assert.Contains("Tool failed", message.Content);
        Assert.Contains("50ms", message.Content);
    }

    [Fact]
    public void FormatAsMessage_WithMetadata_IncludesMetadata()
    {
        // Arrange
        var result = new ToolResult
        {
            ToolCallId = "call-789",
            ToolName = "test_tool",
            Success = true,
            Output = "Output",
            ExecutionTimeMs = 100,
            Metadata = new Dictionary<string, object>
            {
                { "key1", "value1" },
                { "key2", "value2" }
            }
        };

        // Act
        var message = _formatter.FormatAsMessage(result);

        // Assert
        Assert.Contains("Metadata:", message.Content);
        Assert.Contains("key1: value1", message.Content);
        Assert.Contains("key2: value2", message.Content);
    }

    [Fact]
    public void FormatAsMessages_FormatsMultipleResults()
    {
        // Arrange
        var results = new List<ToolResult>
        {
            new ToolResult { ToolCallId = "call-1", ToolName = "tool1", Success = true, Output = "Output 1", ExecutionTimeMs = 100 },
            new ToolResult { ToolCallId = "call-2", ToolName = "tool2", Success = true, Output = "Output 2", ExecutionTimeMs = 200 }
        };

        // Act
        var messages = _formatter.FormatAsMessages(results);

        // Assert
        Assert.Equal(2, messages.Count);
        Assert.All(messages, m => Assert.Equal(MessageRole.Tool, m.Role));
    }

    [Fact]
    public void FormatAsMessage_TruncatesLongOutput()
    {
        // Arrange
        var longOutput = new string('A', 200); // Longer than maxOutputLength (100)
        var result = new ToolResult
        {
            ToolCallId = "call-123",
            ToolName = "test_tool",
            Success = true,
            Output = longOutput,
            ExecutionTimeMs = 100
        };

        // Act
        var message = _formatter.FormatAsMessage(result);

        // Assert
        Assert.Contains("[truncated", message.Content);
        Assert.Contains("100 characters]", message.Content);
    }

    [Fact]
    public void FormatSummary_CreatesCorrectSummary()
    {
        // Arrange
        var results = new List<ToolResult>
        {
            new ToolResult { ToolCallId = "call-1", ToolName = "tool1", Success = true, Output = "Output", ExecutionTimeMs = 100 },
            new ToolResult { ToolCallId = "call-2", ToolName = "tool2", Success = false, Error = "Failed", ExecutionTimeMs = 50 },
            new ToolResult { ToolCallId = "call-3", ToolName = "tool3", Success = true, Output = "Output", ExecutionTimeMs = 150 }
        };

        // Act
        var summary = _formatter.FormatSummary(results);

        // Assert
        Assert.Contains("Executed 3 tool(s)", summary);
        Assert.Contains("tool1: ✅ SUCCESS (100ms)", summary);
        Assert.Contains("tool2: ❌ FAILED (50ms)", summary);
        Assert.Contains("tool3: ✅ SUCCESS (150ms)", summary);
        Assert.Contains("Error: Failed", summary);
    }

    [Fact]
    public void FormatForDisplay_CreatesFormattedOutput()
    {
        // Arrange
        var result = new ToolResult
        {
            ToolCallId = "call-123",
            ToolName = "test_tool",
            Success = true,
            Output = "Tool output",
            ExecutionTimeMs = 150
        };

        // Act
        var display = _formatter.FormatForDisplay(result);

        // Assert
        Assert.Contains("╔══ Tool: test_tool ══╗", display);
        Assert.Contains("Status: ✅ Success", display);
        Assert.Contains("Duration: 150ms", display);
        Assert.Contains("Tool output", display);
        Assert.Contains("╚════════════════════════════╝", display);
    }

    [Fact]
    public void FormatForDisplay_WithFailure_ShowsError()
    {
        // Arrange
        var result = new ToolResult
        {
            ToolCallId = "call-456",
            ToolName = "test_tool",
            Success = false,
            Error = "Something went wrong",
            ExecutionTimeMs = 50
        };

        // Act
        var display = _formatter.FormatForDisplay(result);

        // Assert
        Assert.Contains("Status: ❌ Failed", display);
        Assert.Contains("Error:", display);
        Assert.Contains("Something went wrong", display);
    }

    [Fact]
    public void ToJson_SerializesCorrectly()
    {
        // Arrange
        var result = new ToolResult
        {
            ToolCallId = "call-123",
            ToolName = "test_tool",
            Success = true,
            Output = "Output",
            ExecutionTimeMs = 100
        };

        // Act
        var json = _formatter.ToJson(result);

        // Assert
        Assert.NotEmpty(json);
        Assert.Contains("call-123", json);
        Assert.Contains("test_tool", json);
        Assert.Contains("true", json.ToLower());
        Assert.Contains("Output", json);
    }

    [Fact]
    public void HighlightErrors_WithNoErrors_ReturnsSuccessMessage()
    {
        // Arrange
        var results = new List<ToolResult>
        {
            new ToolResult { ToolCallId = "call-1", ToolName = "tool1", Success = true, Output = "Output", ExecutionTimeMs = 100 },
            new ToolResult { ToolCallId = "call-2", ToolName = "tool2", Success = true, Output = "Output", ExecutionTimeMs = 100 }
        };

        // Act
        var highlight = _formatter.HighlightErrors(results);

        // Assert
        Assert.Equal("All tools executed successfully.", highlight);
    }

    [Fact]
    public void HighlightErrors_WithErrors_ShowsOnlyErrors()
    {
        // Arrange
        var results = new List<ToolResult>
        {
            new ToolResult { ToolCallId = "call-1", ToolName = "tool1", Success = true, Output = "Output", ExecutionTimeMs = 100 },
            new ToolResult { ToolCallId = "call-2", ToolName = "tool2", Success = false, Error = "Error 1", ExecutionTimeMs = 50 },
            new ToolResult { ToolCallId = "call-3", ToolName = "tool3", Success = false, Error = "Error 2", ExecutionTimeMs = 75 }
        };

        // Act
        var highlight = _formatter.HighlightErrors(results);

        // Assert
        Assert.Contains("⚠️ 2 tool(s) failed:", highlight);
        Assert.Contains("❌ tool2", highlight);
        Assert.Contains("Error 1", highlight);
        Assert.Contains("❌ tool3", highlight);
        Assert.Contains("Error 2", highlight);
        Assert.DoesNotContain("tool1", highlight); // Successful tool should not appear
    }

    [Fact]
    public void FormatForOpenAI_FormatsCorrectly()
    {
        // Arrange
        var results = new List<ToolResult>
        {
            new ToolResult { ToolCallId = "call-1", ToolName = "tool1", Success = true, Output = "Output", ExecutionTimeMs = 100 }
        };

        // Act
        var formatted = _formatter.FormatForOpenAI(results);

        // Assert
        Assert.Single(formatted);
        var first = formatted[0];
        var dict = first as dynamic;
        Assert.NotNull(dict);
    }

    [Fact]
    public void FormatForAnthropic_FormatsCorrectly()
    {
        // Arrange
        var results = new List<ToolResult>
        {
            new ToolResult { ToolCallId = "call-1", ToolName = "tool1", Success = true, Output = "Output", ExecutionTimeMs = 100 },
            new ToolResult { ToolCallId = "call-2", ToolName = "tool2", Success = false, Error = "Error", ExecutionTimeMs = 50 }
        };

        // Act
        var formatted = _formatter.FormatForAnthropic(results);

        // Assert
        Assert.Equal(2, formatted.Count);
    }

    [Fact]
    public void Constructor_WithCustomMaxLength_UsesCustomLength()
    {
        // Arrange
        var formatter = new ToolResultFormatter(_mockLogger.Object, maxOutputLength: 50);
        var longOutput = new string('B', 100);
        var result = new ToolResult
        {
            ToolCallId = "call-123",
            ToolName = "test_tool",
            Success = true,
            Output = longOutput,
            ExecutionTimeMs = 100
        };

        // Act
        var message = formatter.FormatAsMessage(result);

        // Assert
        Assert.Contains("[truncated 50 characters]", message.Content);
    }

    [Fact]
    public void FormatAsMessage_WithEmptyOutput_DoesNotIncludeOutput()
    {
        // Arrange
        var result = new ToolResult
        {
            ToolCallId = "call-123",
            ToolName = "test_tool",
            Success = true,
            Output = "",
            ExecutionTimeMs = 100
        };

        // Act
        var message = _formatter.FormatAsMessage(result);

        // Assert
        Assert.Contains("✅ Tool executed successfully", message.Content);
        Assert.DoesNotContain("Output:", message.Content);
    }

    [Fact]
    public void FormatAsMessage_WithNullOutput_DoesNotIncludeOutput()
    {
        // Arrange
        var result = new ToolResult
        {
            ToolCallId = "call-123",
            ToolName = "test_tool",
            Success = true,
            Output = null,
            ExecutionTimeMs = 100
        };

        // Act
        var message = _formatter.FormatAsMessage(result);

        // Assert
        Assert.Contains("✅ Tool executed successfully", message.Content);
        Assert.DoesNotContain("Output:", message.Content);
    }

    [Fact]
    public void FormatSummary_TruncatesLongErrors()
    {
        // Arrange
        var longError = new string('E', 200);
        var results = new List<ToolResult>
        {
            new ToolResult { ToolCallId = "call-1", ToolName = "tool1", Success = false, Error = longError, ExecutionTimeMs = 50 }
        };

        // Act
        var summary = _formatter.FormatSummary(results);

        // Assert
        Assert.Contains("[truncated", summary);
    }
}
