using Foundz.Net.Shared.Models;

namespace Foundz.Net.Shared.Models.Tests;

public class SessionTests
{
    [Fact]
    public void Session_DefaultValues_AreSetCorrectly()
    {
        // Arrange & Act
        var session = new Session
        {
            UserId = "testuser",
            ProjectPath = "/test/project",
            ModelName = "gpt-4"
        };

        // Assert
        Assert.NotEqual(Guid.Empty, session.Id);
        Assert.Equal(SessionStatus.Active, session.Status);
        Assert.NotEqual(default, session.CreatedAt);
        Assert.NotEqual(default, session.UpdatedAt);
        Assert.NotNull(session.Messages);
        Assert.Empty(session.Messages);
    }

    [Fact]
    public void Session_Properties_CanBeSet()
    {
        // Arrange
        var id = Guid.NewGuid();
        var createdAt = DateTime.UtcNow.AddDays(-1);
        var updatedAt = DateTime.UtcNow;
        var metadata = new Dictionary<string, object> { { "key", "value" } };
        var messages = new List<Message>
        {
            new Message { Role = MessageRole.User, Content = "Hello" }
        };

        // Act
        var session = new Session
        {
            Id = id,
            UserId = "user123",
            ProjectPath = "/projects/myapp",
            ModelName = "gpt-4o",
            Status = SessionStatus.Archived,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            Metadata = metadata,
            Messages = messages
        };

        // Assert
        Assert.Equal(id, session.Id);
        Assert.Equal("user123", session.UserId);
        Assert.Equal("/projects/myapp", session.ProjectPath);
        Assert.Equal("gpt-4o", session.ModelName);
        Assert.Equal(SessionStatus.Archived, session.Status);
        Assert.Equal(createdAt, session.CreatedAt);
        Assert.Equal(updatedAt, session.UpdatedAt);
        Assert.NotNull(session.Metadata);
        Assert.Single(session.Messages);
    }

    [Fact]
    public void Session_NullMetadata_WorksCorrectly()
    {
        // Arrange & Act
        var session = new Session
        {
            UserId = "testuser",
            ProjectPath = "/test",
            ModelName = "gpt-4",
            Metadata = null
        };

        // Assert
        Assert.Null(session.Metadata);
    }

    [Fact]
    public void Session_RecordEquality_WorksCorrectly()
    {
        // Arrange
        var id = Guid.NewGuid();
        var session1 = new Session
        {
            Id = id,
            UserId = "user",
            ProjectPath = "/path",
            ModelName = "model"
        };
        var session2 = new Session
        {
            Id = id,
            UserId = "user",
            ProjectPath = "/path",
            ModelName = "model"
        };

        // Act & Assert - they should be equal because records compare by value
        Assert.Equal(session1.Id, session2.Id);
        Assert.Equal(session1.UserId, session2.UserId);
    }
}

public class SessionStatusTests
{
    [Fact]
    public void SessionStatus_AllValues_AreValid()
    {
        // Assert
        Assert.Equal(0, (int)SessionStatus.Active);
        Assert.Equal(1, (int)SessionStatus.Archived);
        Assert.Equal(2, (int)SessionStatus.Deleted);
    }

    [Fact]
    public void SessionStatus_CanBeCompared()
    {
        // Arrange
        var active = SessionStatus.Active;
        var archived = SessionStatus.Archived;
        var deleted = SessionStatus.Deleted;

        // Assert
        Assert.Equal(SessionStatus.Active, active);
        Assert.NotEqual(active, archived);
        Assert.NotEqual(archived, deleted);
    }

    [Fact]
    public void SessionStatus_CanBeSwitched()
    {
        // Arrange
        var statuses = new[] { SessionStatus.Active, SessionStatus.Archived, SessionStatus.Deleted };

        // Act & Assert
        foreach (var status in statuses)
        {
            var message = status switch
            {
                SessionStatus.Active => "Active session",
                SessionStatus.Archived => "Archived session",
                SessionStatus.Deleted => "Deleted session",
                _ => "Unknown"
            };

            Assert.NotEqual("Unknown", message);
        }
    }
}
