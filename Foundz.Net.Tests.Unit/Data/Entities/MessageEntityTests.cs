using Foundz.Net.Data.Entities;

namespace Foundz.Net.Tests.Unit.Data.Entities;

public class MessageEntityTests
{
    [Fact]
    public void MessageEntity_Constructor_InitializesCollections()
    {
        // Arrange & Act
        var entity = new MessageEntity
        {
            Id = Guid.NewGuid(),
            SessionId = Guid.NewGuid(),
            Role = "user",
            Content = "Test message",
            Timestamp = DateTime.UtcNow
        };

        // Assert
        Assert.NotNull(entity.ToolExecutions);
        Assert.Empty(entity.ToolExecutions);
    }

    [Fact]
    public void MessageEntity_Properties_CanBeSet()
    {
        // Arrange
        var id = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var parentMessageId = Guid.NewGuid();
        var timestamp = DateTime.UtcNow;

        // Act
        var entity = new MessageEntity
        {
            Id = id,
            SessionId = sessionId,
            Role = "assistant",
            Content = "This is a test message",
            TokenCount = 25,
            Timestamp = timestamp,
            ParentMessageId = parentMessageId,
            Metadata = "{\"model\": \"gpt-4\"}"
        };

        // Assert
        Assert.Equal(id, entity.Id);
        Assert.Equal(sessionId, entity.SessionId);
        Assert.Equal("assistant", entity.Role);
        Assert.Equal("This is a test message", entity.Content);
        Assert.Equal(25, entity.TokenCount);
        Assert.Equal(timestamp, entity.Timestamp);
        Assert.Equal(parentMessageId, entity.ParentMessageId);
        Assert.Equal("{\"model\": \"gpt-4\"}", entity.Metadata);
    }

    [Fact]
    public void MessageEntity_OptionalProperties_CanBeNull()
    {
        // Arrange & Act
        var entity = new MessageEntity
        {
            Id = Guid.NewGuid(),
            SessionId = Guid.NewGuid(),
            Role = "user",
            Content = "Test",
            Timestamp = DateTime.UtcNow,
            TokenCount = null,
            ParentMessageId = null,
            Metadata = null
        };

        // Assert
        Assert.Null(entity.TokenCount);
        Assert.Null(entity.ParentMessageId);
        Assert.Null(entity.Metadata);
    }

    [Fact]
    public void MessageEntity_ToolExecutions_CanBeAdded()
    {
        // Arrange
        var entity = new MessageEntity
        {
            Id = Guid.NewGuid(),
            SessionId = Guid.NewGuid(),
            Role = "assistant",
            Content = "Executing tool",
            Timestamp = DateTime.UtcNow
        };

        var toolExecution = new ToolExecutionEntity
        {
            Id = Guid.NewGuid(),
            MessageId = entity.Id,
            ToolName = "file_read",
            Arguments = "{}",
            Success = true,
            ExecutionTimeMs = 100,
            Timestamp = DateTime.UtcNow
        };

        // Act
        entity.ToolExecutions.Add(toolExecution);

        // Assert
        Assert.Single(entity.ToolExecutions);
        Assert.Contains(toolExecution, entity.ToolExecutions);
    }

    [Fact]
    public void MessageEntity_Roles_WorkCorrectly()
    {
        // Arrange & Act
        var userMessage = new MessageEntity
        {
            Id = Guid.NewGuid(),
            SessionId = Guid.NewGuid(),
            Role = "user",
            Content = "User message",
            Timestamp = DateTime.UtcNow
        };

        var assistantMessage = new MessageEntity
        {
            Id = Guid.NewGuid(),
            SessionId = Guid.NewGuid(),
            Role = "assistant",
            Content = "Assistant message",
            Timestamp = DateTime.UtcNow
        };

        var systemMessage = new MessageEntity
        {
            Id = Guid.NewGuid(),
            SessionId = Guid.NewGuid(),
            Role = "system",
            Content = "System message",
            Timestamp = DateTime.UtcNow
        };

        // Assert
        Assert.Equal("user", userMessage.Role);
        Assert.Equal("assistant", assistantMessage.Role);
        Assert.Equal("system", systemMessage.Role);
    }
}
