using Foundz.Net.Core.Orchestration;
using Foundz.Net.Shared.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Foundz.Net.Tests.Unit.Orchestration;

public class ConversationHistoryTests
{
    private readonly Mock<ILogger<ConversationHistory>> _mockLogger;
    private readonly ConversationHistory _history;

    public ConversationHistoryTests()
    {
        _mockLogger = new Mock<ILogger<ConversationHistory>>();
        _history = new ConversationHistory(_mockLogger.Object);
    }

    [Fact]
    public void AddMessage_AddsToHistory()
    {
        // Arrange
        var message = new Message
        {
            Role = MessageRole.User,
            Content = "Test message",
            Timestamp = DateTime.UtcNow
        };

        // Act
        _history.AddMessage(message);

        // Assert
        Assert.Equal(1, _history.Count);
        var messages = _history.GetAll();
        Assert.Single(messages);
        Assert.Equal("Test message", messages[0].Content);
    }

    [Fact]
    public void AddMessage_WithNull_ThrowsException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _history.AddMessage(null!));
    }

    [Fact]
    public void AddMessages_AddsMultiple()
    {
        // Arrange
        var messages = new List<Message>
        {
            new Message { Role = MessageRole.User, Content = "Message 1" },
            new Message { Role = MessageRole.Assistant, Content = "Message 2" },
            new Message { Role = MessageRole.User, Content = "Message 3" }
        };

        // Act
        _history.AddMessages(messages);

        // Assert
        Assert.Equal(3, _history.Count);
    }

    [Fact]
    public void AddMessages_WithNull_ThrowsException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _history.AddMessages(null!));
    }

    [Fact]
    public void GetAll_ReturnsAllMessages()
    {
        // Arrange
        _history.AddMessage(new Message { Role = MessageRole.User, Content = "1" });
        _history.AddMessage(new Message { Role = MessageRole.Assistant, Content = "2" });

        // Act
        var all = _history.GetAll();

        // Assert
        Assert.Equal(2, all.Count);
    }

    [Fact]
    public void GetRecent_ReturnsLastNMessages()
    {
        // Arrange
        for (int i = 1; i <= 10; i++)
        {
            _history.AddMessage(new Message { Role = MessageRole.User, Content = $"Message {i}" });
        }

        // Act
        var recent = _history.GetRecent(3);

        // Assert
        Assert.Equal(3, recent.Count);
        Assert.Equal("Message 8", recent[0].Content);
        Assert.Equal("Message 9", recent[1].Content);
        Assert.Equal("Message 10", recent[2].Content);
    }

    [Fact]
    public void GetRecent_WithZeroOrNegative_ReturnsEmpty()
    {
        // Arrange
        _history.AddMessage(new Message { Role = MessageRole.User, Content = "Test" });

        // Act
        var result1 = _history.GetRecent(0);
        var result2 = _history.GetRecent(-5);

        // Assert
        Assert.Empty(result1);
        Assert.Empty(result2);
    }

    [Fact]
    public void GetByRole_FiltersCorrectly()
    {
        // Arrange
        _history.AddMessage(new Message { Role = MessageRole.User, Content = "User 1" });
        _history.AddMessage(new Message { Role = MessageRole.Assistant, Content = "Assistant 1" });
        _history.AddMessage(new Message { Role = MessageRole.User, Content = "User 2" });
        _history.AddMessage(new Message { Role = MessageRole.Tool, Content = "Tool 1" });

        // Act
        var userMessages = _history.GetByRole(MessageRole.User);
        var assistantMessages = _history.GetByRole(MessageRole.Assistant);
        var toolMessages = _history.GetByRole(MessageRole.Tool);

        // Assert
        Assert.Equal(2, userMessages.Count);
        Assert.Single(assistantMessages);
        Assert.Single(toolMessages);
    }

    [Fact]
    public void Search_FindsMatchingMessages()
    {
        // Arrange
        _history.AddMessage(new Message { Role = MessageRole.User, Content = "Hello world" });
        _history.AddMessage(new Message { Role = MessageRole.Assistant, Content = "Hi there" });
        _history.AddMessage(new Message { Role = MessageRole.User, Content = "How are you?" });

        // Act
        var results = _history.Search("world");

        // Assert
        Assert.Single(results);
        Assert.Contains("Hello world", results[0].Content);
    }

    [Fact]
    public void Search_CaseInsensitive_FindsMatches()
    {
        // Arrange
        _history.AddMessage(new Message { Role = MessageRole.User, Content = "Hello World" });

        // Act
        var results = _history.Search("world", caseSensitive: false);

        // Assert
        Assert.Single(results);
    }

    [Fact]
    public void Search_CaseSensitive_RespectsCasing()
    {
        // Arrange
        _history.AddMessage(new Message { Role = MessageRole.User, Content = "Hello World" });

        // Act
        var results = _history.Search("world", caseSensitive: true);

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public void Search_WithEmptyQuery_ReturnsEmpty()
    {
        // Arrange
        _history.AddMessage(new Message { Role = MessageRole.User, Content = "Test" });

        // Act
        var results = _history.Search("");

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public void Clear_RemovesAllMessages()
    {
        // Arrange
        _history.AddMessage(new Message { Role = MessageRole.User, Content = "Test 1" });
        _history.AddMessage(new Message { Role = MessageRole.User, Content = "Test 2" });

        // Act
        _history.Clear();

        // Assert
        Assert.Equal(0, _history.Count);
    }

    [Fact]
    public void ExportToJson_ReturnsValidJson()
    {
        // Arrange
        _history.AddMessage(new Message { Role = MessageRole.User, Content = "Test message" });

        // Act
        var json = _history.ExportToJson();

        // Assert
        Assert.NotEmpty(json);
        Assert.Contains("Test message", json);
    }

    [Fact]
    public void ImportFromJson_LoadsMessages()
    {
        // Arrange
        var json = @"[
            {""Role"": 0, ""Content"": ""Message 1""},
            {""Role"": 1, ""Content"": ""Message 2""}
        ]";

        // Act
        _history.ImportFromJson(json);

        // Assert
        Assert.Equal(2, _history.Count);
    }

    [Fact]
    public void ImportFromJson_ClearsExistingMessages()
    {
        // Arrange
        _history.AddMessage(new Message { Role = MessageRole.User, Content = "Old message" });
        var json = @"[{""Role"": 0, ""Content"": ""New message""}]";

        // Act
        _history.ImportFromJson(json);

        // Assert
        Assert.Equal(1, _history.Count);
        Assert.Equal("New message", _history.GetAll()[0].Content);
    }

    [Fact]
    public void GetSummary_ReturnsCorrectCounts()
    {
        // Arrange
        _history.AddMessage(new Message { Role = MessageRole.User, Content = "User 1", Timestamp = DateTime.UtcNow });
        _history.AddMessage(new Message { Role = MessageRole.Assistant, Content = "Assistant 1", Timestamp = DateTime.UtcNow });
        _history.AddMessage(new Message { Role = MessageRole.User, Content = "User 2", Timestamp = DateTime.UtcNow });
        _history.AddMessage(new Message { Role = MessageRole.Tool, Content = "Tool 1", Timestamp = DateTime.UtcNow });
        _history.AddMessage(new Message { Role = MessageRole.System, Content = "System 1", Timestamp = DateTime.UtcNow });

        // Act
        var summary = _history.GetSummary();

        // Assert
        Assert.Equal(5, summary.TotalMessages);
        Assert.Equal(2, summary.UserMessages);
        Assert.Equal(1, summary.AssistantMessages);
        Assert.Equal(1, summary.ToolMessages);
        Assert.Equal(1, summary.SystemMessages);
        Assert.NotNull(summary.FirstMessageTime);
        Assert.NotNull(summary.LastMessageTime);
    }

    [Fact]
    public void Truncate_KeepsRecentMessages()
    {
        // Arrange
        for (int i = 1; i <= 10; i++)
        {
            _history.AddMessage(new Message { Role = MessageRole.User, Content = $"Message {i}" });
        }

        // Act
        _history.Truncate(5);

        // Assert
        Assert.Equal(5, _history.Count);
        var messages = _history.GetAll();
        Assert.Equal("Message 6", messages[0].Content);
        Assert.Equal("Message 10", messages[4].Content);
    }

    [Fact]
    public void Truncate_WithZero_ClearsAll()
    {
        // Arrange
        _history.AddMessage(new Message { Role = MessageRole.User, Content = "Test" });

        // Act
        _history.Truncate(0);

        // Assert
        Assert.Equal(1, _history.Count); // Should not remove anything with keepCount <= 0
    }

    [Fact]
    public void Truncate_WhenCountLessThanKeep_DoesNothing()
    {
        // Arrange
        _history.AddMessage(new Message { Role = MessageRole.User, Content = "Test 1" });
        _history.AddMessage(new Message { Role = MessageRole.User, Content = "Test 2" });

        // Act
        _history.Truncate(10);

        // Assert
        Assert.Equal(2, _history.Count);
    }

    [Fact]
    public void GetByTimeRange_FiltersCorrectly()
    {
        // Arrange
        var baseTime = DateTime.UtcNow;
        _history.AddMessage(new Message { Role = MessageRole.User, Content = "Message 1", Timestamp = baseTime });
        _history.AddMessage(new Message { Role = MessageRole.User, Content = "Message 2", Timestamp = baseTime.AddMinutes(5) });
        _history.AddMessage(new Message { Role = MessageRole.User, Content = "Message 3", Timestamp = baseTime.AddMinutes(10) });

        // Act
        var results = _history.GetByTimeRange(baseTime.AddMinutes(2), baseTime.AddMinutes(8));

        // Assert
        Assert.Single(results);
        Assert.Equal("Message 2", results[0].Content);
    }

    [Fact]
    public void GetByTimeRange_WithNoMatchingMessages_ReturnsEmpty()
    {
        // Arrange
        var baseTime = DateTime.UtcNow;
        _history.AddMessage(new Message { Role = MessageRole.User, Content = "Test", Timestamp = baseTime });

        // Act
        var results = _history.GetByTimeRange(baseTime.AddDays(1), baseTime.AddDays(2));

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public void Count_ReflectsMessageCount()
    {
        // Assert initial count
        Assert.Equal(0, _history.Count);

        // Add messages
        _history.AddMessage(new Message { Role = MessageRole.User, Content = "1" });
        Assert.Equal(1, _history.Count);

        _history.AddMessage(new Message { Role = MessageRole.User, Content = "2" });
        Assert.Equal(2, _history.Count);

        // Clear
        _history.Clear();
        Assert.Equal(0, _history.Count);
    }
}
