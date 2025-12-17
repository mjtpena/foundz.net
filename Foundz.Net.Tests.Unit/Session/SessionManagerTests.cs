using Foundz.Net.Core.Session;
using Foundz.Net.Shared.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Foundz.Net.Tests.Unit.Session;

public class SessionManagerTests
{
    private readonly Mock<ILogger<SessionManager>> _loggerMock;
    private readonly SessionManager _sessionManager;

    public SessionManagerTests()
    {
        _loggerMock = new Mock<ILogger<SessionManager>>();
        _sessionManager = new SessionManager(_loggerMock.Object);
    }

    [Fact]
    public void CreateSession_WithDefaultParameters_ReturnsNewSession()
    {
        // Act
        var session = _sessionManager.CreateSession();

        // Assert
        Assert.NotEqual(Guid.Empty, session.Id);
        Assert.NotNull(session.Messages);
        Assert.Empty(session.Messages);
        Assert.Equal("claude-3-5-sonnet-20241022", session.ModelName);
        Assert.True(session.CreatedAt <= DateTime.UtcNow);
        Assert.True(session.UpdatedAt <= DateTime.UtcNow);
    }

    [Fact]
    public void CreateSession_WithParameters_ReturnsConfiguredSession()
    {
        // Arrange
        var userId = "user123";
        var projectPath = "/path/to/project";
        var modelName = "gpt-4";

        // Act
        var session = _sessionManager.CreateSession(userId, projectPath, modelName);

        // Assert
        Assert.Equal(userId, session.UserId);
        Assert.Equal(projectPath, session.ProjectPath);
        Assert.Equal(modelName, session.ModelName);
    }

    [Fact]
    public void GetSession_ExistingSession_ReturnsSession()
    {
        // Arrange
        var created = _sessionManager.CreateSession();

        // Act
        var retrieved = _sessionManager.GetSession(created.Id);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(created.Id, retrieved.Id);
    }

    [Fact]
    public void GetSession_NonExistentSession_ReturnsNull()
    {
        // Act
        var result = _sessionManager.GetSession(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void UpdateSession_ValidSession_UpdatesSuccessfully()
    {
        // Arrange
        var session = _sessionManager.CreateSession();
        var originalUpdateTime = session.UpdatedAt;
        Thread.Sleep(10); // Ensure time difference

        // Act
        _sessionManager.UpdateSession(session);
        var retrieved = _sessionManager.GetSession(session.Id);

        // Assert
        Assert.NotNull(retrieved);
        Assert.True(retrieved.UpdatedAt > originalUpdateTime);
    }

    [Fact]
    public void UpdateSession_NullSession_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _sessionManager.UpdateSession(null!));
    }

    [Fact]
    public void DeleteSession_ExistingSession_ReturnsTrue()
    {
        // Arrange
        var session = _sessionManager.CreateSession();

        // Act
        var result = _sessionManager.DeleteSession(session.Id);

        // Assert
        Assert.True(result);
        Assert.Null(_sessionManager.GetSession(session.Id));
    }

    [Fact]
    public void DeleteSession_NonExistentSession_ReturnsFalse()
    {
        // Act
        var result = _sessionManager.DeleteSession(Guid.NewGuid());

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ListSessions_NoSessions_ReturnsEmptyList()
    {
        // Act
        var sessions = _sessionManager.ListSessions();

        // Assert
        Assert.Empty(sessions);
    }

    [Fact]
    public void ListSessions_MultipleSessions_ReturnsAllSessions()
    {
        // Arrange
        _sessionManager.CreateSession();
        _sessionManager.CreateSession();
        _sessionManager.CreateSession();

        // Act
        var sessions = _sessionManager.ListSessions();

        // Assert
        Assert.Equal(3, sessions.Count);
    }

    [Fact]
    public void ListSessions_WithUserIdFilter_ReturnsFilteredSessions()
    {
        // Arrange
        var userId = "user123";
        _sessionManager.CreateSession(userId);
        _sessionManager.CreateSession("otherUser");
        _sessionManager.CreateSession(userId);

        // Act
        var sessions = _sessionManager.ListSessions(userId);

        // Assert
        Assert.Equal(2, sessions.Count);
        Assert.All(sessions, s => Assert.Equal(userId, s.UserId));
    }

    [Fact]
    public void ListSessions_WithLimit_ReturnsLimitedResults()
    {
        // Arrange
        for (int i = 0; i < 5; i++)
        {
            _sessionManager.CreateSession();
        }

        // Act
        var sessions = _sessionManager.ListSessions(limit: 3);

        // Assert
        Assert.Equal(3, sessions.Count);
    }

    [Fact]
    public void ListSessions_OrderedByUpdateTime_ReturnsInDescendingOrder()
    {
        // Arrange
        var first = _sessionManager.CreateSession();
        Thread.Sleep(10);
        var second = _sessionManager.CreateSession();
        Thread.Sleep(10);
        var third = _sessionManager.CreateSession();

        // Act
        var sessions = _sessionManager.ListSessions();

        // Assert
        Assert.Equal(third.Id, sessions[0].Id);
        Assert.Equal(second.Id, sessions[1].Id);
        Assert.Equal(first.Id, sessions[2].Id);
    }

    [Fact]
    public void AddMessage_ValidSession_AddsMessageSuccessfully()
    {
        // Arrange
        var session = _sessionManager.CreateSession();
        var message = new Message
        {
            Role = MessageRole.User,
            Content = "Test message"
        };

        // Act
        _sessionManager.AddMessage(session.Id, message);
        var retrieved = _sessionManager.GetSession(session.Id);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Single(retrieved.Messages);
        Assert.Equal(message.Content, retrieved.Messages[0].Content);
    }

    [Fact]
    public void AddMessage_NonExistentSession_DoesNothing()
    {
        // Arrange
        var message = new Message { Role = MessageRole.User, Content = "Test" };

        // Act (should not throw)
        _sessionManager.AddMessage(Guid.NewGuid(), message);

        // Assert - no exception
        Assert.True(true);
    }

    [Fact]
    public void AddMessage_MultipleMessages_MaintainsOrder()
    {
        // Arrange
        var session = _sessionManager.CreateSession();
        var msg1 = new Message { Role = MessageRole.User, Content = "First" };
        var msg2 = new Message { Role = MessageRole.Assistant, Content = "Second" };
        var msg3 = new Message { Role = MessageRole.User, Content = "Third" };

        // Act
        _sessionManager.AddMessage(session.Id, msg1);
        _sessionManager.AddMessage(session.Id, msg2);
        _sessionManager.AddMessage(session.Id, msg3);
        var retrieved = _sessionManager.GetSession(session.Id);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(3, retrieved.Messages.Count);
        Assert.Equal("First", retrieved.Messages[0].Content);
        Assert.Equal("Second", retrieved.Messages[1].Content);
        Assert.Equal("Third", retrieved.Messages[2].Content);
    }

    [Fact]
    public void ClearMessages_ValidSession_RemovesAllMessages()
    {
        // Arrange
        var session = _sessionManager.CreateSession();
        _sessionManager.AddMessage(session.Id, new Message { Role = MessageRole.User, Content = "Test1" });
        _sessionManager.AddMessage(session.Id, new Message { Role = MessageRole.User, Content = "Test2" });

        // Act
        _sessionManager.ClearMessages(session.Id);
        var retrieved = _sessionManager.GetSession(session.Id);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Empty(retrieved.Messages);
    }

    [Fact]
    public void ClearMessages_NonExistentSession_DoesNothing()
    {
        // Act (should not throw)
        _sessionManager.ClearMessages(Guid.NewGuid());

        // Assert - no exception
        Assert.True(true);
    }

    [Fact]
    public void ArchiveOldSessions_WithOldSessions_RemovesThem()
    {
        // Arrange
        var oldSession = _sessionManager.CreateSession();
        var recentSession = _sessionManager.CreateSession();
        
        // Manually set old update time (using reflection or creating new instance)
        var sessions = typeof(SessionManager)
            .GetField("_sessions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .GetValue(_sessionManager) as Dictionary<Guid, Foundz.Net.Shared.Models.Session>;
        
        sessions![oldSession.Id] = oldSession with { UpdatedAt = DateTime.UtcNow.AddHours(-2) };

        // Act
        var archived = _sessionManager.ArchiveOldSessions(TimeSpan.FromHours(1));

        // Assert
        Assert.Equal(1, archived);
        Assert.Null(_sessionManager.GetSession(oldSession.Id));
        Assert.NotNull(_sessionManager.GetSession(recentSession.Id));
    }

    [Fact]
    public void ArchiveOldSessions_NoOldSessions_ReturnsZero()
    {
        // Arrange
        _sessionManager.CreateSession();

        // Act
        var archived = _sessionManager.ArchiveOldSessions(TimeSpan.FromHours(1));

        // Assert
        Assert.Equal(0, archived);
    }

    [Fact]
    public void ExportSession_ValidSession_ReturnsJson()
    {
        // Arrange
        var session = _sessionManager.CreateSession("user123", "/project");
        _sessionManager.AddMessage(session.Id, new Message { Role = MessageRole.User, Content = "Test" });

        // Act
        var json = _sessionManager.ExportSession(session.Id);

        // Assert
        Assert.NotNull(json);
        Assert.Contains("user123", json);
        Assert.Contains("Test", json);
    }

    [Fact]
    public void ExportSession_NonExistentSession_ReturnsNull()
    {
        // Act
        var json = _sessionManager.ExportSession(Guid.NewGuid());

        // Assert
        Assert.Null(json);
    }

    [Fact]
    public void ImportSession_ValidJson_ImportsSuccessfully()
    {
        // Arrange
        var session = _sessionManager.CreateSession("user123");
        var json = _sessionManager.ExportSession(session.Id)!;
        _sessionManager.DeleteSession(session.Id);

        // Act
        var imported = _sessionManager.ImportSession(json);

        // Assert
        Assert.NotNull(imported);
        Assert.Equal(session.Id, imported.Id);
        Assert.Equal("user123", imported.UserId);
    }

    [Fact]
    public void ImportSession_InvalidJson_ReturnsNull()
    {
        // Act
        var result = _sessionManager.ImportSession("invalid json");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Count_ReturnsCorrectSessionCount()
    {
        // Arrange
        Assert.Equal(0, _sessionManager.Count);
        
        _sessionManager.CreateSession();
        _sessionManager.CreateSession();

        // Act & Assert
        Assert.Equal(2, _sessionManager.Count);
        
        _sessionManager.DeleteSession(_sessionManager.ListSessions()[0].Id);
        Assert.Equal(1, _sessionManager.Count);
    }
}
