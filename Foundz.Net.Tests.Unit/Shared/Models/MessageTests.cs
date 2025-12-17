using Foundz.Net.Shared.Models;

namespace Foundz.Net.Tests.Unit.Shared.Models;

public class MessageTests
{
    [Fact]
    public void Message_DefaultValues_AreSetCorrectly()
    {
        // Arrange & Act
        var message = new Message
        {
            Role = MessageRole.User,
            Content = "Hello"
        };

        // Assert
        Assert.NotEqual(Guid.Empty, message.Id);
        Assert.NotNull(message.Timestamp);
    }

    [Fact]
    public void Message_UserMessage_Properties_CanBeSet()
    {
        // Arrange
        var id = Guid.NewGuid();
        var timestamp = DateTime.UtcNow;
        var metadata = new Dictionary<string, object> { { "client", "cli" } };

        // Act
        var message = new Message
        {
            Id = id,
            Role = MessageRole.User,
            Content = "Write a function to calculate fibonacci",
            TokenCount = 15,
            Timestamp = timestamp,
            Metadata = metadata
        };

        // Assert
        Assert.Equal(id, message.Id);
        Assert.Equal(MessageRole.User, message.Role);
        Assert.Equal("Write a function to calculate fibonacci", message.Content);
        Assert.Equal(15, message.TokenCount);
        Assert.Equal(timestamp, message.Timestamp);
        Assert.NotNull(message.Metadata);
    }

    [Fact]
    public void Message_AssistantMessage_WithToolCalls_WorksCorrectly()
    {
        // Arrange & Act
        var toolCalls = new List<ToolCall>
        {
            new ToolCall
            {
                Id = "call_1",
                Name = "file_read",
                Arguments = new Dictionary<string, object> { { "path", "test.txt" } }
            },
            new ToolCall
            {
                Id = "call_2",
                Name = "file_write",
                Arguments = new Dictionary<string, object> { { "path", "output.txt" }, { "content", "data" } }
            }
        };

        var message = new Message
        {
            Role = MessageRole.Assistant,
            Content = "I'll read the file and write output",
            ToolCalls = toolCalls
        };

        // Assert
        Assert.Equal(MessageRole.Assistant, message.Role);
        Assert.NotNull(message.ToolCalls);
        Assert.Equal(2, message.ToolCalls.Count);
        Assert.Equal("call_1", message.ToolCalls[0].Id);
        Assert.Equal("file_read", message.ToolCalls[0].Name);
    }

    [Fact]
    public void Message_ToolMessage_Properties_WorkCorrectly()
    {
        // Arrange & Act
        var message = new Message
        {
            Role = MessageRole.Tool,
            Content = "File content: Hello World",
            ToolCallId = "call_123",
            ToolName = "file_read"
        };

        // Assert
        Assert.Equal(MessageRole.Tool, message.Role);
        Assert.Equal("call_123", message.ToolCallId);
        Assert.Equal("file_read", message.ToolName);
        Assert.Equal("File content: Hello World", message.Content);
    }

    [Fact]
    public void Message_SystemMessage_WorksCorrectly()
    {
        // Arrange & Act
        var message = new Message
        {
            Role = MessageRole.System,
            Content = "You are a helpful AI assistant"
        };

        // Assert
        Assert.Equal(MessageRole.System, message.Role);
        Assert.Equal("You are a helpful AI assistant", message.Content);
    }

    [Fact]
    public void Message_ParentMessageId_WorksCorrectly()
    {
        // Arrange
        var parentId = Guid.NewGuid();

        // Act
        var message = new Message
        {
            Role = MessageRole.Assistant,
            Content = "Follow-up response",
            ParentMessageId = parentId
        };

        // Assert
        Assert.Equal(parentId, message.ParentMessageId);
    }

    [Fact]
    public void Message_NullOptionalProperties_WorkCorrectly()
    {
        // Arrange & Act
        var message = new Message
        {
            Role = MessageRole.User,
            Content = "Test",
            TokenCount = null,
            ParentMessageId = null,
            Metadata = null,
            ToolCalls = null,
            ToolCallId = null,
            ToolName = null
        };

        // Assert
        Assert.Null(message.TokenCount);
        Assert.Null(message.ParentMessageId);
        Assert.Null(message.Metadata);
        Assert.Null(message.ToolCalls);
        Assert.Null(message.ToolCallId);
        Assert.Null(message.ToolName);
    }

    [Fact]
    public void Message_RecordEquality_WorksCorrectly()
    {
        // Arrange
        var id = Guid.NewGuid();
        var message1 = new Message
        {
            Id = id,
            Role = MessageRole.User,
            Content = "Test"
        };
        var message2 = new Message
        {
            Id = id,
            Role = MessageRole.User,
            Content = "Test"
        };

        // Act & Assert
        Assert.Equal(message1.Id, message2.Id);
        Assert.Equal(message1.Role, message2.Role);
        Assert.Equal(message1.Content, message2.Content);
    }
}

public class MessageRoleTests
{
    [Fact]
    public void MessageRole_AllValues_AreValid()
    {
        // Assert
        Assert.Equal(0, (int)MessageRole.System);
        Assert.Equal(1, (int)MessageRole.User);
        Assert.Equal(2, (int)MessageRole.Assistant);
        Assert.Equal(3, (int)MessageRole.Tool);
    }

    [Fact]
    public void MessageRole_CanBeCompared()
    {
        // Arrange
        var system = MessageRole.System;
        var user = MessageRole.User;
        var assistant = MessageRole.Assistant;
        var tool = MessageRole.Tool;

        // Assert
        Assert.Equal(MessageRole.System, system);
        Assert.NotEqual(system, user);
        Assert.NotEqual(user, assistant);
        Assert.NotEqual(assistant, tool);
    }

    [Fact]
    public void MessageRole_CanBeSwitched()
    {
        // Arrange
        var roles = new[] { MessageRole.System, MessageRole.User, MessageRole.Assistant, MessageRole.Tool };

        // Act & Assert
        foreach (var role in roles)
        {
            var description = role switch
            {
                MessageRole.System => "System prompt",
                MessageRole.User => "User input",
                MessageRole.Assistant => "AI response",
                MessageRole.Tool => "Tool result",
                _ => "Unknown"
            };

            Assert.NotEqual("Unknown", description);
        }
    }
}
