using Foundz.Net.Core.Orchestration;
using Foundz.Net.Shared.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Foundz.Net.Tests.Unit.Orchestration;

public class ContextManagerTests
{
    private readonly Mock<ILogger<ContextManager>> _loggerMock;
    private readonly ContextManager _contextManager;

    public ContextManagerTests()
    {
        _loggerMock = new Mock<ILogger<ContextManager>>();
        _contextManager = new ContextManager(_loggerMock.Object, maxMessages: 10, maxTokens: 1000, tokenReserve: 100);
    }

    [Fact]
    public void BuildContext_WithSystemPrompt_AddsSystemMessage()
    {
        // Arrange
        var history = new List<Message>();
        var systemPrompt = "You are a helpful assistant.";

        // Act
        var context = _contextManager.BuildContext(history, systemPrompt);

        // Assert
        Assert.Single(context);
        Assert.Equal(MessageRole.System, context[0].Role);
        Assert.Equal(systemPrompt, context[0].Content);
    }

    [Fact]
    public void BuildContext_WithoutSystemPrompt_StartsEmpty()
    {
        // Arrange
        var history = new List<Message>();

        // Act
        var context = _contextManager.BuildContext(history, null);

        // Assert
        Assert.Empty(context);
    }

    [Fact]
    public void BuildContext_AppliesSlidingWindow()
    {
        // Arrange
        var history = new List<Message>();
        for (int i = 0; i < 20; i++)
        {
            history.Add(new Message { Role = MessageRole.User, Content = $"Message {i}" });
        }

        // Act
        var context = _contextManager.BuildContext(history);

        // Assert (maxMessages is 10)
        Assert.Equal(10, context.Count);
        Assert.Equal("Message 10", context[0].Content); // First should be message 10 (last 10 messages)
    }

    [Fact]
    public void BuildContext_PreservesRecentMessages()
    {
        // Arrange
        var history = new List<Message>
        {
            new() { Role = MessageRole.User, Content = "Old message" },
            new() { Role = MessageRole.Assistant, Content = "Old response" },
            new() { Role = MessageRole.User, Content = "Recent message" }
        };

        // Act
        var context = _contextManager.BuildContext(history);

        // Assert
        Assert.Equal(3, context.Count);
        Assert.Equal("Recent message", context[2].Content);
    }

    [Fact]
    public void EstimateTokens_SingleMessage_ReturnsEstimate()
    {
        // Arrange
        var message = new Message
        {
            Role = MessageRole.User,
            Content = "This is a test message with some content."
        };

        // Act
        var tokens = _contextManager.EstimateTokens(message);

        // Assert
        Assert.True(tokens > 0);
        Assert.True(tokens > 10); // Should include overhead
    }

    [Fact]
    public void EstimateTokens_NullMessage_ReturnsZero()
    {
        // Act
        var tokens = _contextManager.EstimateTokens((Message)null!);

        // Assert
        Assert.Equal(0, tokens);
    }

    [Fact]
    public void EstimateTokens_MessageList_ReturnsSumOfAll()
    {
        // Arrange
        var messages = new List<Message>
        {
            new() { Role = MessageRole.User, Content = "First message" },
            new() { Role = MessageRole.Assistant, Content = "Second message" },
            new() { Role = MessageRole.User, Content = "Third message" }
        };

        // Act
        var totalTokens = _contextManager.EstimateTokens(messages);
        var individual = messages.Sum(m => _contextManager.EstimateTokens(m));

        // Assert
        Assert.Equal(individual, totalTokens);
    }

    [Fact]
    public void AddRelevantFiles_AddsFileToContext()
    {
        // Arrange
        var context = new List<Message>();
        var filePaths = new List<string> { "test.cs" };
        string FileReader(string path) => "public class Test { }";

        // Act
        _contextManager.AddRelevantFiles(context, filePaths, FileReader);

        // Assert
        Assert.Single(context);
        Assert.Contains("test.cs", context[0].Content);
        Assert.Contains("public class Test", context[0].Content);
    }

    [Fact]
    public void AddRelevantFiles_SkipsEmptyFiles()
    {
        // Arrange
        var context = new List<Message>();
        var filePaths = new List<string> { "empty.cs" };
        string FileReader(string path) => "";

        // Act
        _contextManager.AddRelevantFiles(context, filePaths, FileReader);

        // Assert
        Assert.Empty(context);
    }

    [Fact]
    public void AddRelevantFiles_RespectsTokenBudget()
    {
        // Arrange
        var context = new List<Message>();
        // Fill context to near limit
        for (int i = 0; i < 5; i++)
        {
            context.Add(new Message
            {
                Role = MessageRole.User,
                Content = new string('a', 200) // Large messages
            });
        }

        var filePaths = new List<string> { "large.cs" };
        string FileReader(string path) => new string('x', 5000); // Very large file

        // Act
        _contextManager.AddRelevantFiles(context, filePaths, FileReader);

        // Assert (should skip due to budget)
        Assert.Equal(5, context.Count); // No file added
    }

    [Fact]
    public void AddRelevantFiles_HandlesFileReadErrors()
    {
        // Arrange
        var context = new List<Message>();
        var filePaths = new List<string> { "error.cs" };
        string FileReader(string path) => throw new Exception("File not found");

        // Act (should not throw)
        _contextManager.AddRelevantFiles(context, filePaths, FileReader);

        // Assert
        Assert.Empty(context);
    }

    [Fact]
    public void AddProjectMetadata_AddsMetadataMessage()
    {
        // Arrange
        var context = new List<Message>();
        var metadata = new Dictionary<string, string>
        {
            ["Language"] = "C#",
            ["Framework"] = ".NET 10",
            ["ProjectType"] = "Library"
        };

        // Act
        _contextManager.AddProjectMetadata(context, metadata);

        // Assert
        Assert.Single(context);
        Assert.Equal(MessageRole.System, context[0].Role);
        Assert.Contains("Language: C#", context[0].Content);
        Assert.Contains("Framework: .NET 10", context[0].Content);
    }

    [Fact]
    public void AddProjectMetadata_EmptyMetadata_DoesNothing()
    {
        // Arrange
        var context = new List<Message>();
        var metadata = new Dictionary<string, string>();

        // Act
        _contextManager.AddProjectMetadata(context, metadata);

        // Assert
        Assert.Empty(context);
    }

    [Fact]
    public void AddProjectMetadata_NullMetadata_DoesNothing()
    {
        // Arrange
        var context = new List<Message>();

        // Act
        _contextManager.AddProjectMetadata(context, null!);

        // Assert
        Assert.Empty(context);
    }

    [Fact]
    public async Task SummarizeMessagesAsync_CreatesSummaryMessage()
    {
        // Arrange
        var messages = new List<Message>
        {
            new() { Role = MessageRole.User, Content = "Question 1" },
            new() { Role = MessageRole.Assistant, Content = "Answer 1" }
        };
        
        Task<string> Summarizer(string input) => Task.FromResult("Summary of conversation");

        // Act
        var summary = await _contextManager.SummarizeMessagesAsync(messages, Summarizer);

        // Assert
        Assert.Equal(MessageRole.System, summary.Role);
        Assert.Contains("Summary of conversation", summary.Content);
    }

    [Fact]
    public async Task SummarizeMessagesAsync_EmptyMessages_ReturnsPlaceholder()
    {
        // Arrange
        var messages = new List<Message>();
        Task<string> Summarizer(string input) => Task.FromResult("Summary");

        // Act
        var summary = await _contextManager.SummarizeMessagesAsync(messages, Summarizer);

        // Assert
        Assert.Contains("No messages to summarize", summary.Content);
    }

    [Fact]
    public async Task SummarizeMessagesAsync_SummarizerThrows_ReturnsErrorMessage()
    {
        // Arrange
        var messages = new List<Message>
        {
            new() { Role = MessageRole.User, Content = "Test" }
        };
        
        Task<string> Summarizer(string input) => throw new Exception("API error");

        // Act
        var summary = await _contextManager.SummarizeMessagesAsync(messages, Summarizer);

        // Assert
        Assert.Contains("Failed to generate summary", summary.Content);
    }

    [Fact]
    public void GetRemainingBudget_CalculatesCorrectly()
    {
        // Arrange
        var context = new List<Message>
        {
            new() { Role = MessageRole.User, Content = "Test message" }
        };

        // Act
        var remaining = _contextManager.GetRemainingBudget(context);

        // Assert
        Assert.True(remaining > 0);
        Assert.True(remaining < 900); // maxTokens (1000) - tokenReserve (100) = 900
    }

    [Fact]
    public void GetRemainingBudget_EmptyContext_ReturnsFullBudget()
    {
        // Arrange
        var context = new List<Message>();

        // Act
        var remaining = _contextManager.GetRemainingBudget(context);

        // Assert
        Assert.Equal(900, remaining); // maxTokens (1000) - tokenReserve (100)
    }

    [Fact]
    public void IsNearTokenLimit_BelowThreshold_ReturnsFalse()
    {
        // Arrange
        var context = new List<Message>
        {
            new() { Role = MessageRole.User, Content = "Short message" }
        };

        // Act
        var isNear = _contextManager.IsNearTokenLimit(context, 0.8);

        // Assert
        Assert.False(isNear);
    }

    [Fact]
    public void IsNearTokenLimit_AboveThreshold_ReturnsTrue()
    {
        // Arrange
        var context = new List<Message>();
        // Fill with large messages
        for (int i = 0; i < 50; i++)
        {
            context.Add(new Message
            {
                Role = MessageRole.User,
                Content = new string('a', 100)
            });
        }

        // Act
        var isNear = _contextManager.IsNearTokenLimit(context, 0.5); // Lower threshold

        // Assert
        Assert.True(isNear);
    }
}
