using Foundz.Net.Data.Entities;

namespace Foundz.Net.Tests.Unit.Data.Entities;

public class SessionEntityTests
{
    [Fact]
    public void SessionEntity_Constructor_InitializesCollections()
    {
        // Arrange & Act
        var entity = new SessionEntity
        {
            Id = Guid.NewGuid(),
            UserId = "testuser",
            ProjectPath = "/test/path",
            ModelName = "gpt-4",
            Status = "active",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Assert
        Assert.NotNull(entity.Messages);
        Assert.Empty(entity.Messages);
    }

    [Fact]
    public void SessionEntity_Properties_CanBeSet()
    {
        // Arrange
        var id = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;
        var updatedAt = DateTime.UtcNow.AddMinutes(5);

        // Act
        var entity = new SessionEntity
        {
            Id = id,
            UserId = "testuser123",
            ProjectPath = "/projects/test",
            ModelName = "gpt-4o",
            Status = "completed",
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            Metadata = "{\"key\": \"value\"}"
        };

        // Assert
        Assert.Equal(id, entity.Id);
        Assert.Equal("testuser123", entity.UserId);
        Assert.Equal("/projects/test", entity.ProjectPath);
        Assert.Equal("gpt-4o", entity.ModelName);
        Assert.Equal("completed", entity.Status);
        Assert.Equal(createdAt, entity.CreatedAt);
        Assert.Equal(updatedAt, entity.UpdatedAt);
        Assert.Equal("{\"key\": \"value\"}", entity.Metadata);
    }

    [Fact]
    public void SessionEntity_Metadata_CanBeNull()
    {
        // Arrange & Act
        var entity = new SessionEntity
        {
            Id = Guid.NewGuid(),
            UserId = "testuser",
            ProjectPath = "/test/path",
            ModelName = "gpt-4",
            Status = "active",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Metadata = null
        };

        // Assert
        Assert.Null(entity.Metadata);
    }

    [Fact]
    public void SessionEntity_Messages_CanBeAdded()
    {
        // Arrange
        var entity = new SessionEntity
        {
            Id = Guid.NewGuid(),
            UserId = "testuser",
            ProjectPath = "/test/path",
            ModelName = "gpt-4",
            Status = "active",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var message = new MessageEntity
        {
            Id = Guid.NewGuid(),
            SessionId = entity.Id,
            Role = "user",
            Content = "Hello",
            Timestamp = DateTime.UtcNow
        };

        // Act
        entity.Messages.Add(message);

        // Assert
        Assert.Single(entity.Messages);
        Assert.Contains(message, entity.Messages);
    }
}
