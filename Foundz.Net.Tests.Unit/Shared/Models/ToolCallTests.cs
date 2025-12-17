using Foundz.Net.Shared.Models;

namespace Foundz.Net.Tests.Unit.Shared.Models;

public class ToolCallTests
{
    [Fact]
    public void ToolCall_Properties_CanBeSet()
    {
        // Arrange & Act
        var toolCall = new ToolCall
        {
            Id = "tool_123",
            Name = "file_read",
            Arguments = new Dictionary<string, object>
            {
                { "path", "/test/file.txt" },
                { "encoding", "utf8" }
            }
        };

        // Assert
        Assert.Equal("tool_123", toolCall.Id);
        Assert.Equal("file_read", toolCall.Name);
        Assert.Equal(2, toolCall.Arguments.Count);
        Assert.Equal("/test/file.txt", toolCall.Arguments["path"]);
        Assert.Equal("utf8", toolCall.Arguments["encoding"]);
    }

    [Fact]
    public void ToolCall_EmptyArguments_WorksCorrectly()
    {
        // Arrange & Act
        var toolCall = new ToolCall
        {
            Id = "tool_456",
            Name = "test_tool",
            Arguments = new Dictionary<string, object>()
        };

        // Assert
        Assert.Empty(toolCall.Arguments);
    }

    [Fact]
    public void ToolCall_ComplexArguments_WorksCorrectly()
    {
        // Arrange & Act
        var toolCall = new ToolCall
        {
            Id = "tool_789",
            Name = "complex_tool",
            Arguments = new Dictionary<string, object>
            {
                { "string_val", "test" },
                { "int_val", 42 },
                { "bool_val", true },
                { "array_val", new[] { 1, 2, 3 } },
                { "nested_obj", new Dictionary<string, object> { { "key", "value" } } }
            }
        };

        // Assert
        Assert.Equal(5, toolCall.Arguments.Count);
        Assert.Equal("test", toolCall.Arguments["string_val"]);
        Assert.Equal(42, toolCall.Arguments["int_val"]);
        Assert.Equal(true, toolCall.Arguments["bool_val"]);
        Assert.IsType<int[]>(toolCall.Arguments["array_val"]);
    }

    [Fact]
    public void ToolCall_RecordEquality_WorksCorrectly()
    {
        // Arrange
        var args = new Dictionary<string, object> { { "key", "value" } };
        var toolCall1 = new ToolCall { Id = "test_id", Name = "test_name", Arguments = args };
        var toolCall2 = new ToolCall { Id = "test_id", Name = "test_name", Arguments = args };

        // Act & Assert
        Assert.Equal(toolCall1, toolCall2);
        Assert.True(toolCall1 == toolCall2);
    }
}

public class ToolResultTests
{
    [Fact]
    public void ToolResult_Success_Properties_CanBeSet()
    {
        // Arrange & Act
        var result = new ToolResult
        {
            ToolCallId = "call_123",
            ToolName = "file_read",
            Success = true,
            Output = "File content here",
            ExecutionTimeMs = 150
        };

        // Assert
        Assert.Equal("call_123", result.ToolCallId);
        Assert.Equal("file_read", result.ToolName);
        Assert.True(result.Success);
        Assert.Equal("File content here", result.Output);
        Assert.Null(result.Error);
        Assert.Equal(150, result.ExecutionTimeMs);
    }

    [Fact]
    public void ToolResult_Failure_Properties_CanBeSet()
    {
        // Arrange & Act
        var result = new ToolResult
        {
            ToolCallId = "call_456",
            ToolName = "file_write",
            Success = false,
            Error = "Permission denied",
            ExecutionTimeMs = 50
        };

        // Assert
        Assert.Equal("call_456", result.ToolCallId);
        Assert.Equal("file_write", result.ToolName);
        Assert.False(result.Success);
        Assert.Null(result.Output);
        Assert.Equal("Permission denied", result.Error);
        Assert.Equal(50, result.ExecutionTimeMs);
    }

    [Fact]
    public void ToolResult_WithMetadata_WorksCorrectly()
    {
        // Arrange & Act
        var result = new ToolResult
        {
            ToolCallId = "call_789",
            ToolName = "test_tool",
            Success = true,
            Output = "Result",
            ExecutionTimeMs = 100,
            Metadata = new Dictionary<string, object>
            {
                { "attempts", 3 },
                { "cached", true }
            }
        };

        // Assert
        Assert.NotNull(result.Metadata);
        Assert.Equal(2, result.Metadata.Count);
        Assert.Equal(3, result.Metadata["attempts"]);
        Assert.Equal(true, result.Metadata["cached"]);
    }

    [Fact]
    public void ToolResult_NullMetadata_WorksCorrectly()
    {
        // Arrange & Act
        var result = new ToolResult
        {
            ToolCallId = "call_000",
            ToolName = "simple_tool",
            Success = true,
            ExecutionTimeMs = 25,
            Metadata = null
        };

        // Assert
        Assert.Null(result.Metadata);
    }

    [Fact]
    public void ToolResult_BothOutputAndError_CanExist()
    {
        // Arrange & Act
        var result = new ToolResult
        {
            ToolCallId = "call_mixed",
            ToolName = "mixed_tool",
            Success = false,
            Output = "Partial output before error",
            Error = "Failed at step 2",
            ExecutionTimeMs = 200
        };

        // Assert
        Assert.Equal("Partial output before error", result.Output);
        Assert.Equal("Failed at step 2", result.Error);
        Assert.False(result.Success);
    }

    [Fact]
    public void ToolResult_RecordEquality_WorksCorrectly()
    {
        // Arrange
        var result1 = new ToolResult
        {
            ToolCallId = "id",
            ToolName = "name",
            Success = true,
            ExecutionTimeMs = 100
        };
        var result2 = new ToolResult
        {
            ToolCallId = "id",
            ToolName = "name",
            Success = true,
            ExecutionTimeMs = 100
        };

        // Act & Assert
        Assert.Equal(result1, result2);
    }
}
