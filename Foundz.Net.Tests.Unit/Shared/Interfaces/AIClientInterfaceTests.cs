using Foundz.Net.Shared.Interfaces;
using Foundz.Net.Shared.Models;

namespace Foundz.Net.Tests.Unit.Shared.Interfaces;

public class AIResponseTests
{
    [Fact]
    public void AIResponse_Properties_CanBeSet()
    {
        // Arrange & Act
        var response = new AIResponse
        {
            Content = "This is the AI response",
            ToolCalls = new List<ToolCall>
            {
                new ToolCall { Id = "call_1", Name = "tool_name", Arguments = new Dictionary<string, object>() }
            },
            TokenUsage = new TokenUsage { PromptTokens = 100, CompletionTokens = 50 },
            FinishReason = "stop",
            ModelUsed = "gpt-4"
        };

        // Assert
        Assert.Equal("This is the AI response", response.Content);
        Assert.NotNull(response.ToolCalls);
        Assert.Single(response.ToolCalls);
        Assert.NotNull(response.TokenUsage);
        Assert.Equal("stop", response.FinishReason);
        Assert.Equal("gpt-4", response.ModelUsed);
    }

    [Fact]
    public void AIResponse_NullToolCalls_WorksCorrectly()
    {
        // Arrange & Act
        var response = new AIResponse
        {
            Content = "Simple response",
            ToolCalls = null,
            TokenUsage = new TokenUsage { PromptTokens = 10, CompletionTokens = 5 },
            FinishReason = "stop"
        };

        // Assert
        Assert.Null(response.ToolCalls);
        Assert.Null(response.ModelUsed);
    }
}

public class AIResponseChunkTests
{
    [Fact]
    public void AIResponseChunk_AllProperties_CanBeSet()
    {
        // Arrange & Act
        var chunk = new AIResponseChunk
        {
            ContentDelta = "partial ",
            ToolCalls = new List<ToolCall>
            {
                new ToolCall { Id = "call_1", Name = "tool", Arguments = new Dictionary<string, object>() }
            },
            TokenUsage = new TokenUsage { PromptTokens = 100, CompletionTokens = 25 },
            FinishReason = null
        };

        // Assert
        Assert.Equal("partial ", chunk.ContentDelta);
        Assert.NotNull(chunk.ToolCalls);
        Assert.Single(chunk.ToolCalls);
        Assert.NotNull(chunk.TokenUsage);
        Assert.Null(chunk.FinishReason);
    }

    [Fact]
    public void AIResponseChunk_AllNullProperties_WorksCorrectly()
    {
        // Arrange & Act
        var chunk = new AIResponseChunk
        {
            ContentDelta = null,
            ToolCalls = null,
            TokenUsage = null,
            FinishReason = null
        };

        // Assert
        Assert.Null(chunk.ContentDelta);
        Assert.Null(chunk.ToolCalls);
        Assert.Null(chunk.TokenUsage);
        Assert.Null(chunk.FinishReason);
    }

    [Fact]
    public void AIResponseChunk_FinalChunk_HasFinishReason()
    {
        // Arrange & Act
        var chunk = new AIResponseChunk
        {
            ContentDelta = null,
            ToolCalls = null,
            TokenUsage = new TokenUsage { PromptTokens = 100, CompletionTokens = 50 },
            FinishReason = "stop"
        };

        // Assert
        Assert.Equal("stop", chunk.FinishReason);
        Assert.NotNull(chunk.TokenUsage);
    }
}

public class TokenUsageTests
{
    [Fact]
    public void TokenUsage_Properties_CanBeSet()
    {
        // Arrange & Act
        var usage = new TokenUsage
        {
            PromptTokens = 150,
            CompletionTokens = 75
        };

        // Assert
        Assert.Equal(150, usage.PromptTokens);
        Assert.Equal(75, usage.CompletionTokens);
        Assert.Equal(225, usage.TotalTokens);
    }

    [Fact]
    public void TokenUsage_TotalTokens_CalculatesCorrectly()
    {
        // Arrange & Act
        var usage1 = new TokenUsage { PromptTokens = 1000, CompletionTokens = 500 };
        var usage2 = new TokenUsage { PromptTokens = 0, CompletionTokens = 0 };
        var usage3 = new TokenUsage { PromptTokens = 123, CompletionTokens = 456 };

        // Assert
        Assert.Equal(1500, usage1.TotalTokens);
        Assert.Equal(0, usage2.TotalTokens);
        Assert.Equal(579, usage3.TotalTokens);
    }

    [Fact]
    public void TokenUsage_RecordEquality_WorksCorrectly()
    {
        // Arrange
        var usage1 = new TokenUsage { PromptTokens = 100, CompletionTokens = 50 };
        var usage2 = new TokenUsage { PromptTokens = 100, CompletionTokens = 50 };

        // Act & Assert
        Assert.Equal(usage1.PromptTokens, usage2.PromptTokens);
        Assert.Equal(usage1.CompletionTokens, usage2.CompletionTokens);
        Assert.Equal(usage1.TotalTokens, usage2.TotalTokens);
    }
}

public class ToolDefinitionTests
{
    [Fact]
    public void ToolDefinition_Properties_CanBeSet()
    {
        // Arrange & Act
        var definition = new ToolDefinition
        {
            Name = "file_read",
            Description = "Reads the contents of a file",
            Parameters = new Dictionary<string, object>
            {
                { "type", "object" },
                { "properties", new Dictionary<string, object>
                    {
                        { "path", new Dictionary<string, object> { { "type", "string" } } }
                    }
                },
                { "required", new[] { "path" } }
            }
        };

        // Assert
        Assert.Equal("file_read", definition.Name);
        Assert.Equal("Reads the contents of a file", definition.Description);
        Assert.NotNull(definition.Parameters);
        Assert.Equal(3, definition.Parameters.Count);
        Assert.Equal("object", definition.Parameters["type"]);
    }

    [Fact]
    public void ToolDefinition_EmptyParameters_WorksCorrectly()
    {
        // Arrange & Act
        var definition = new ToolDefinition
        {
            Name = "simple_tool",
            Description = "A simple tool with no parameters",
            Parameters = new Dictionary<string, object>()
        };

        // Assert
        Assert.Empty(definition.Parameters);
    }

    [Fact]
    public void ToolDefinition_ComplexParameters_WorksCorrectly()
    {
        // Arrange & Act
        var definition = new ToolDefinition
        {
            Name = "complex_tool",
            Description = "A tool with complex parameters",
            Parameters = new Dictionary<string, object>
            {
                { "type", "object" },
                { "properties", new Dictionary<string, object>
                    {
                        { "string_param", new Dictionary<string, string> { { "type", "string" } } },
                        { "number_param", new Dictionary<string, string> { { "type", "number" } } },
                        { "array_param", new Dictionary<string, object>
                            {
                                { "type", "array" },
                                { "items", new Dictionary<string, string> { { "type", "string" } } }
                            }
                        }
                    }
                },
                { "required", new[] { "string_param" } }
            }
        };

        // Assert
        Assert.Equal("complex_tool", definition.Name);
        Assert.NotNull(definition.Parameters);
        Assert.True(definition.Parameters.ContainsKey("properties"));
    }
}
