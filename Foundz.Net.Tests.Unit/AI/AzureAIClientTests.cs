using Foundz.Net.Core.AI;
using Foundz.Net.Shared.Interfaces;
using Foundz.Net.Shared.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Foundz.Net.Tests.Unit.AI;

public class AzureAIClientTests
{
    private readonly Mock<ILogger<AzureAIClient>> _mockLogger;
    private readonly Mock<IProviderAdapter> _mockAdapter;
    private readonly AzureAIClient _client;

    public AzureAIClientTests()
    {
        _mockLogger = new Mock<ILogger<AzureAIClient>>();
        _mockAdapter = new Mock<IProviderAdapter>();
        _client = new AzureAIClient(
            "https://test.endpoint.azure.com",
            "test-api-key",
            "test-model",
            _mockAdapter.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task SendMessageAsync_ReturnsPlaceholderResponse()
    {
        // Arrange
        var messages = new List<Message>
        {
            new Message { Role = MessageRole.User, Content = "Test message" }
        };

        // Act
        var response = await _client.SendMessageAsync(messages);

        // Assert
        Assert.NotNull(response);
        Assert.Contains("placeholder", response.Content);
        Assert.Equal("test-model", response.ModelUsed);
        Assert.NotNull(response.TokenUsage);
        Assert.Equal(100, response.TokenUsage.PromptTokens);
        Assert.Equal(50, response.TokenUsage.CompletionTokens);
    }

    [Fact]
    public async Task SendMessageAsync_WithTools_ReturnsResponse()
    {
        // Arrange
        var messages = new List<Message>
        {
            new Message { Role = MessageRole.User, Content = "Test message" }
        };
        var tools = new List<ToolDefinition>
        {
            new ToolDefinition { Name = "test_tool", Description = "A test tool", Parameters = new Dictionary<string, object>() }
        };

        // Act
        var response = await _client.SendMessageAsync(messages, tools);

        // Assert
        Assert.NotNull(response);
        Assert.Equal("stop", response.FinishReason);
    }

    [Fact]
    public async Task StreamMessageAsync_ReturnsChunks()
    {
        // Arrange
        var messages = new List<Message>
        {
            new Message { Role = MessageRole.User, Content = "Test message" }
        };
        var chunks = new List<AIResponseChunk>();

        // Act
        await foreach (var chunk in _client.StreamMessageAsync(messages))
        {
            chunks.Add(chunk);
        }

        // Assert
        Assert.NotEmpty(chunks);
        Assert.True(chunks.Count > 1);
        Assert.Contains(chunks, c => c.ContentDelta != null);
        Assert.Contains(chunks, c => c.FinishReason == "stop");
    }

    [Fact]
    public async Task StreamMessageAsync_WithCancellation_StopsStreaming()
    {
        // Arrange
        var messages = new List<Message>
        {
            new Message { Role = MessageRole.User, Content = "Test message" }
        };
        var cts = new CancellationTokenSource();
        var chunks = new List<AIResponseChunk>();

        // Act
        try
        {
            await foreach (var chunk in _client.StreamMessageAsync(messages, cancellationToken: cts.Token))
            {
                chunks.Add(chunk);
                if (chunks.Count >= 2)
                    cts.Cancel();
            }
        }
        catch (TaskCanceledException)
        {
            // Expected when cancellation token is triggered
        }

        // Assert
        Assert.NotEmpty(chunks);
    }

    [Fact]
    public async Task ListModelsAsync_ReturnsConfiguredModel()
    {
        // Act
        var models = await _client.ListModelsAsync();

        // Assert
        Assert.Single(models);
        Assert.Contains("test-model", models);
    }

    [Fact]
    public async Task GetModelCapabilitiesAsync_ReturnsCapabilities()
    {
        // Act
        var capabilities = await _client.GetModelCapabilitiesAsync("test-model");

        // Assert
        Assert.NotNull(capabilities);
        Assert.Equal("test-model", capabilities.ModelName);
        Assert.Equal("Azure AI Foundry", capabilities.Provider);
        Assert.True(capabilities.SupportsToolUse);
        Assert.True(capabilities.SupportsStreaming);
        Assert.Equal(200000, capabilities.MaxContextTokens);
    }

    [Fact]
    public async Task ValidateConnectionAsync_WithValidCredentials_ReturnsTrue()
    {
        // Act
        var result = await _client.ValidateConnectionAsync();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ValidateConnectionAsync_WithEmptyEndpoint_ReturnsFalse()
    {
        // Arrange
        var client = new AzureAIClient(
            "",
            "test-api-key",
            "test-model",
            null,
            _mockLogger.Object
        );

        // Act
        var result = await client.ValidateConnectionAsync();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateConnectionAsync_WithEmptyApiKey_ReturnsFalse()
    {
        // Arrange
        var client = new AzureAIClient(
            "https://test.endpoint.azure.com",
            "",
            "test-model",
            null,
            _mockLogger.Object
        );

        // Act
        var result = await client.ValidateConnectionAsync();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Act
        var client = new AzureAIClient(
            "https://test.endpoint.azure.com",
            "test-api-key",
            "test-model",
            null,
            _mockLogger.Object
        );

        // Assert
        Assert.NotNull(client);
    }
}
