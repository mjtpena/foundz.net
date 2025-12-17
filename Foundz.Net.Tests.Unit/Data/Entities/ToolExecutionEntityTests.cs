using Foundz.Net.Data.Entities;

namespace Foundz.Net.Tests.Unit.Data.Entities;

public class ToolExecutionEntityTests
{
    [Fact]
    public void ToolExecutionEntity_Properties_CanBeSet()
    {
        // Arrange
        var id = Guid.NewGuid();
        var messageId = Guid.NewGuid();
        var timestamp = DateTime.UtcNow;

        // Act
        var entity = new ToolExecutionEntity
        {
            Id = id,
            MessageId = messageId,
            ToolName = "file_read",
            Arguments = "{\"path\": \"/test/file.txt\"}",
            Result = "File content here",
            Success = true,
            ExecutionTimeMs = 245,
            Timestamp = timestamp
        };

        // Assert
        Assert.Equal(id, entity.Id);
        Assert.Equal(messageId, entity.MessageId);
        Assert.Equal("file_read", entity.ToolName);
        Assert.Equal("{\"path\": \"/test/file.txt\"}", entity.Arguments);
        Assert.Equal("File content here", entity.Result);
        Assert.True(entity.Success);
        Assert.Equal(245, entity.ExecutionTimeMs);
        Assert.Equal(timestamp, entity.Timestamp);
    }

    [Fact]
    public void ToolExecutionEntity_Result_CanBeNull()
    {
        // Arrange & Act
        var entity = new ToolExecutionEntity
        {
            Id = Guid.NewGuid(),
            MessageId = Guid.NewGuid(),
            ToolName = "test_tool",
            Arguments = "{}",
            Result = null,
            Success = false,
            ExecutionTimeMs = 100,
            Timestamp = DateTime.UtcNow
        };

        // Assert
        Assert.Null(entity.Result);
    }

    [Fact]
    public void ToolExecutionEntity_Success_CanBeFalse()
    {
        // Arrange & Act
        var entity = new ToolExecutionEntity
        {
            Id = Guid.NewGuid(),
            MessageId = Guid.NewGuid(),
            ToolName = "failing_tool",
            Arguments = "{}",
            Result = "Error: Tool failed",
            Success = false,
            ExecutionTimeMs = 50,
            Timestamp = DateTime.UtcNow
        };

        // Assert
        Assert.False(entity.Success);
        Assert.Equal("Error: Tool failed", entity.Result);
    }

    [Fact]
    public void ToolExecutionEntity_DifferentTools_WorkCorrectly()
    {
        // Arrange & Act
        var fileReadTool = new ToolExecutionEntity
        {
            Id = Guid.NewGuid(),
            MessageId = Guid.NewGuid(),
            ToolName = "file_read",
            Arguments = "{\"path\": \"test.txt\"}",
            Success = true,
            ExecutionTimeMs = 100,
            Timestamp = DateTime.UtcNow
        };

        var fileWriteTool = new ToolExecutionEntity
        {
            Id = Guid.NewGuid(),
            MessageId = Guid.NewGuid(),
            ToolName = "file_write",
            Arguments = "{\"path\": \"output.txt\", \"content\": \"data\"}",
            Success = true,
            ExecutionTimeMs = 150,
            Timestamp = DateTime.UtcNow
        };

        var shellTool = new ToolExecutionEntity
        {
            Id = Guid.NewGuid(),
            MessageId = Guid.NewGuid(),
            ToolName = "shell_execute",
            Arguments = "{\"command\": \"ls -la\"}",
            Success = true,
            ExecutionTimeMs = 200,
            Timestamp = DateTime.UtcNow
        };

        // Assert
        Assert.Equal("file_read", fileReadTool.ToolName);
        Assert.Equal("file_write", fileWriteTool.ToolName);
        Assert.Equal("shell_execute", shellTool.ToolName);
    }

    [Fact]
    public void ToolExecutionEntity_ExecutionTime_TracksCorrectly()
    {
        // Arrange & Act
        var fastTool = new ToolExecutionEntity
        {
            Id = Guid.NewGuid(),
            MessageId = Guid.NewGuid(),
            ToolName = "fast_tool",
            Arguments = "{}",
            Success = true,
            ExecutionTimeMs = 10,
            Timestamp = DateTime.UtcNow
        };

        var slowTool = new ToolExecutionEntity
        {
            Id = Guid.NewGuid(),
            MessageId = Guid.NewGuid(),
            ToolName = "slow_tool",
            Arguments = "{}",
            Success = true,
            ExecutionTimeMs = 5000,
            Timestamp = DateTime.UtcNow
        };

        // Assert
        Assert.Equal(10, fastTool.ExecutionTimeMs);
        Assert.Equal(5000, slowTool.ExecutionTimeMs);
        Assert.True(slowTool.ExecutionTimeMs > fastTool.ExecutionTimeMs);
    }

    [Fact]
    public void ToolExecutionEntity_Arguments_SupportsComplexJson()
    {
        // Arrange & Act
        var entity = new ToolExecutionEntity
        {
            Id = Guid.NewGuid(),
            MessageId = Guid.NewGuid(),
            ToolName = "complex_tool",
            Arguments = "{\"nested\": {\"value\": 123, \"array\": [1, 2, 3]}}",
            Success = true,
            ExecutionTimeMs = 100,
            Timestamp = DateTime.UtcNow
        };

        // Assert
        Assert.Contains("nested", entity.Arguments);
        Assert.Contains("array", entity.Arguments);
        Assert.Contains("[1, 2, 3]", entity.Arguments);
    }
}
