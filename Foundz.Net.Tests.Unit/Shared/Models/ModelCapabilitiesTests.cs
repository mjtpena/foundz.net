using Foundz.Net.Shared.Models;

namespace Foundz.Net.Tests.Unit.Shared.Models;

public class ModelCapabilitiesTests
{
    [Fact]
    public void ModelCapabilities_AllProperties_CanBeSet()
    {
        // Arrange & Act
        var capabilities = new ModelCapabilities
        {
            ModelName = "gpt-4o",
            Provider = "OpenAI",
            SupportsToolUse = true,
            SupportsVision = true,
            SupportsJsonMode = true,
            SupportsStreaming = true,
            SupportsParallelToolCalls = true,
            MaxContextTokens = 128000,
            MaxOutputTokens = 4096,
            CostPer1kInputTokens = 0.005m,
            CostPer1kOutputTokens = 0.015m,
            Features = new List<string> { "function-calling", "vision", "json-mode" },
            SupportedLanguages = new List<string> { "en", "es", "fr", "de" }
        };

        // Assert
        Assert.Equal("gpt-4o", capabilities.ModelName);
        Assert.Equal("OpenAI", capabilities.Provider);
        Assert.True(capabilities.SupportsToolUse);
        Assert.True(capabilities.SupportsVision);
        Assert.True(capabilities.SupportsJsonMode);
        Assert.True(capabilities.SupportsStreaming);
        Assert.True(capabilities.SupportsParallelToolCalls);
        Assert.Equal(128000, capabilities.MaxContextTokens);
        Assert.Equal(4096, capabilities.MaxOutputTokens);
        Assert.Equal(0.005m, capabilities.CostPer1kInputTokens);
        Assert.Equal(0.015m, capabilities.CostPer1kOutputTokens);
        Assert.Equal(3, capabilities.Features.Count);
        Assert.Equal(4, capabilities.SupportedLanguages.Count);
    }

    [Fact]
    public void ModelCapabilities_MinimalConfiguration_WorksCorrectly()
    {
        // Arrange & Act
        var capabilities = new ModelCapabilities
        {
            ModelName = "llama-3-8b",
            Provider = "Meta",
            SupportsToolUse = false,
            SupportsVision = false,
            SupportsJsonMode = false,
            SupportsStreaming = true,
            SupportsParallelToolCalls = false,
            MaxContextTokens = 8192,
            MaxOutputTokens = 2048,
            CostPer1kInputTokens = 0.0m,
            CostPer1kOutputTokens = 0.0m
        };

        // Assert
        Assert.Equal("llama-3-8b", capabilities.ModelName);
        Assert.Equal("Meta", capabilities.Provider);
        Assert.False(capabilities.SupportsToolUse);
        Assert.False(capabilities.SupportsVision);
        Assert.False(capabilities.SupportsJsonMode);
        Assert.True(capabilities.SupportsStreaming);
        Assert.False(capabilities.SupportsParallelToolCalls);
        Assert.Empty(capabilities.Features);
        Assert.Empty(capabilities.SupportedLanguages);
    }

    [Fact]
    public void ModelCapabilities_OpenAIModel_HasCorrectValues()
    {
        // Arrange & Act
        var capabilities = new ModelCapabilities
        {
            ModelName = "gpt-4-turbo",
            Provider = "OpenAI",
            SupportsToolUse = true,
            SupportsVision = true,
            SupportsJsonMode = true,
            SupportsStreaming = true,
            SupportsParallelToolCalls = true,
            MaxContextTokens = 128000,
            MaxOutputTokens = 4096,
            CostPer1kInputTokens = 0.01m,
            CostPer1kOutputTokens = 0.03m,
            Features = new List<string> { "vision", "function-calling", "json-mode" }
        };

        // Assert
        Assert.Equal("OpenAI", capabilities.Provider);
        Assert.True(capabilities.SupportsToolUse);
        Assert.True(capabilities.SupportsVision);
        Assert.Contains("vision", capabilities.Features);
        Assert.Contains("function-calling", capabilities.Features);
    }

    [Fact]
    public void ModelCapabilities_AnthropicModel_HasCorrectValues()
    {
        // Arrange & Act
        var capabilities = new ModelCapabilities
        {
            ModelName = "claude-3-5-sonnet-20241022",
            Provider = "Anthropic",
            SupportsToolUse = true,
            SupportsVision = true,
            SupportsJsonMode = false,
            SupportsStreaming = true,
            SupportsParallelToolCalls = true,
            MaxContextTokens = 200000,
            MaxOutputTokens = 8192,
            CostPer1kInputTokens = 0.003m,
            CostPer1kOutputTokens = 0.015m
        };

        // Assert
        Assert.Equal("Anthropic", capabilities.Provider);
        Assert.Equal(200000, capabilities.MaxContextTokens);
        Assert.Equal(8192, capabilities.MaxOutputTokens);
        Assert.True(capabilities.SupportsToolUse);
        Assert.False(capabilities.SupportsJsonMode);
    }

    [Fact]
    public void ModelCapabilities_MistralModel_HasCorrectValues()
    {
        // Arrange & Act
        var capabilities = new ModelCapabilities
        {
            ModelName = "Mistral-large-2411",
            Provider = "Mistral",
            SupportsToolUse = true,
            SupportsVision = false,
            SupportsJsonMode = true,
            SupportsStreaming = true,
            SupportsParallelToolCalls = true,
            MaxContextTokens = 128000,
            MaxOutputTokens = 4096,
            CostPer1kInputTokens = 0.002m,
            CostPer1kOutputTokens = 0.006m
        };

        // Assert
        Assert.Equal("Mistral", capabilities.Provider);
        Assert.True(capabilities.SupportsToolUse);
        Assert.False(capabilities.SupportsVision);
        Assert.True(capabilities.SupportsJsonMode);
    }

    [Fact]
    public void ModelCapabilities_CostCalculation_WorksCorrectly()
    {
        // Arrange
        var capabilities = new ModelCapabilities
        {
            ModelName = "gpt-4",
            Provider = "OpenAI",
            SupportsToolUse = true,
            SupportsVision = false,
            SupportsJsonMode = false,
            SupportsStreaming = true,
            SupportsParallelToolCalls = true,
            MaxContextTokens = 8192,
            MaxOutputTokens = 8192,
            CostPer1kInputTokens = 0.03m,
            CostPer1kOutputTokens = 0.06m
        };

        // Act
        var inputTokens = 5000;
        var outputTokens = 2000;
        var inputCost = (inputTokens / 1000m) * capabilities.CostPer1kInputTokens;
        var outputCost = (outputTokens / 1000m) * capabilities.CostPer1kOutputTokens;
        var totalCost = inputCost + outputCost;

        // Assert
        Assert.Equal(0.15m, inputCost); // 5 * 0.03
        Assert.Equal(0.12m, outputCost); // 2 * 0.06
        Assert.Equal(0.27m, totalCost);
    }

    [Fact]
    public void ModelCapabilities_EmptyCollections_InitializeCorrectly()
    {
        // Arrange & Act
        var capabilities = new ModelCapabilities
        {
            ModelName = "test-model",
            Provider = "TestProvider",
            SupportsToolUse = false,
            SupportsVision = false,
            SupportsJsonMode = false,
            SupportsStreaming = false,
            SupportsParallelToolCalls = false,
            MaxContextTokens = 4096,
            MaxOutputTokens = 1024,
            CostPer1kInputTokens = 0.0m,
            CostPer1kOutputTokens = 0.0m
        };

        // Assert
        Assert.NotNull(capabilities.Features);
        Assert.Empty(capabilities.Features);
        Assert.NotNull(capabilities.SupportedLanguages);
        Assert.Empty(capabilities.SupportedLanguages);
    }

    [Fact]
    public void ModelCapabilities_RecordEquality_WorksCorrectly()
    {
        // Arrange
        var capabilities1 = new ModelCapabilities
        {
            ModelName = "test-model",
            Provider = "Provider",
            SupportsToolUse = true,
            SupportsVision = false,
            SupportsJsonMode = false,
            SupportsStreaming = true,
            SupportsParallelToolCalls = false,
            MaxContextTokens = 8192,
            MaxOutputTokens = 2048,
            CostPer1kInputTokens = 0.01m,
            CostPer1kOutputTokens = 0.02m
        };
        var capabilities2 = new ModelCapabilities
        {
            ModelName = "test-model",
            Provider = "Provider",
            SupportsToolUse = true,
            SupportsVision = false,
            SupportsJsonMode = false,
            SupportsStreaming = true,
            SupportsParallelToolCalls = false,
            MaxContextTokens = 8192,
            MaxOutputTokens = 2048,
            CostPer1kInputTokens = 0.01m,
            CostPer1kOutputTokens = 0.02m
        };

        // Act & Assert
        Assert.Equal(capabilities1.ModelName, capabilities2.ModelName);
        Assert.Equal(capabilities1.Provider, capabilities2.Provider);
        Assert.Equal(capabilities1.MaxContextTokens, capabilities2.MaxContextTokens);
    }
}
