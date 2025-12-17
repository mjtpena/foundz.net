using Foundz.Net.Core.AI;
using Foundz.Net.Shared.Interfaces;
using Foundz.Net.Shared.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Foundz.Net.Tests.Unit.AI;

public class ModelCapabilityDetectorTests
{
    private readonly Mock<ILogger<ModelCapabilityDetector>> _mockLogger;
    private readonly Mock<IProviderAdapter> _mockAdapter;
    private readonly ModelCapabilityDetector _detector;

    public ModelCapabilityDetectorTests()
    {
        _mockLogger = new Mock<ILogger<ModelCapabilityDetector>>();
        _mockAdapter = new Mock<IProviderAdapter>();
        _mockAdapter.Setup(a => a.ProviderName).Returns("TestProvider");
        _mockAdapter.Setup(a => a.ValidateModel(It.IsAny<string>())).Returns(false);
        
        _detector = new ModelCapabilityDetector(
            _mockLogger.Object,
            new[] { _mockAdapter.Object }
        );
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new ModelCapabilityDetector(null!, Array.Empty<IProviderAdapter>())
        );
    }

    [Fact]
    public void Constructor_WithNullAdapters_ThrowsException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new ModelCapabilityDetector(_mockLogger.Object, null!)
        );
    }

    [Theory]
    [InlineData("claude-3-5-sonnet-20241022")]
    [InlineData("claude-3-opus-20240229")]
    [InlineData("claude-3-haiku-20240307")]
    public void GetCapabilities_ForClaudeModels_ReturnsCorrectCapabilities(string modelName)
    {
        // Act
        var capabilities = _detector.GetCapabilities(modelName);

        // Assert
        Assert.Equal(modelName, capabilities.ModelName);
        Assert.Equal("Anthropic", capabilities.Provider);
        Assert.True(capabilities.SupportsToolUse);
        Assert.True(capabilities.SupportsStreaming);
        Assert.Equal(200_000, capabilities.MaxContextTokens);
        Assert.True(capabilities.SupportsParallelToolCalls);
    }

    [Fact]
    public void GetCapabilities_ForClaudeOpus_SupportsVision()
    {
        // Act
        var capabilities = _detector.GetCapabilities("claude-3-opus-20240229");

        // Assert
        Assert.True(capabilities.SupportsVision);
    }

    [Fact]
    public void GetCapabilities_ForClaudeHaiku_DoesNotSupportVision()
    {
        // Act
        var capabilities = _detector.GetCapabilities("claude-3-haiku-20240307");

        // Assert
        Assert.False(capabilities.SupportsVision);
    }

    [Theory]
    [InlineData("gpt-4")]
    [InlineData("gpt-4-turbo")]
    [InlineData("gpt-4o")]
    public void GetCapabilities_ForGPT4Models_ReturnsCorrectCapabilities(string modelName)
    {
        // Act
        var capabilities = _detector.GetCapabilities(modelName);

        // Assert
        Assert.Equal(modelName, capabilities.ModelName);
        Assert.Equal("OpenAI", capabilities.Provider);
        Assert.True(capabilities.SupportsToolUse);
        Assert.True(capabilities.SupportsJsonMode);
        Assert.True(capabilities.SupportsStreaming);
        Assert.True(capabilities.SupportsParallelToolCalls);
    }

    [Theory]
    [InlineData("gpt-4o")]
    [InlineData("gpt-4o-mini")]
    public void GetCapabilities_ForGPTOModels_HasCorrectProperties(string modelName)
    {
        // Act
        var capabilities = _detector.GetCapabilities(modelName);

        // Assert
        Assert.NotNull(capabilities);
        Assert.Equal(modelName, capabilities.ModelName);
        Assert.True(capabilities.SupportsToolUse);
    }

    [Theory]
    [InlineData("gpt-3.5-turbo")]
    public void GetCapabilities_ForGPT35_ReturnsCorrectCapabilities(string modelName)
    {
        // Act
        var capabilities = _detector.GetCapabilities(modelName);

        // Assert
        Assert.Equal("OpenAI", capabilities.Provider);
        Assert.True(capabilities.SupportsToolUse);
        Assert.True(capabilities.SupportsJsonMode);
        Assert.Equal(16_384, capabilities.MaxContextTokens);
        Assert.False(capabilities.SupportsParallelToolCalls);
    }

    [Theory]
    [InlineData("mistral-large")]
    [InlineData("mistral-medium")]
    [InlineData("mistral-small")]
    public void GetCapabilities_ForMistralModels_ReturnsCorrectCapabilities(string modelName)
    {
        // Act
        var capabilities = _detector.GetCapabilities(modelName);

        // Assert
        Assert.Equal("Mistral", capabilities.Provider);
        Assert.True(capabilities.SupportsToolUse);
        Assert.True(capabilities.SupportsJsonMode);
        Assert.Equal(32_000, capabilities.MaxContextTokens);
    }

    [Theory]
    [InlineData("command-r-plus")]
    [InlineData("command-r")]
    public void GetCapabilities_ForCohereModels_ReturnsCorrectCapabilities(string modelName)
    {
        // Act
        var capabilities = _detector.GetCapabilities(modelName);

        // Assert
        Assert.Equal("Cohere", capabilities.Provider);
        Assert.True(capabilities.SupportsToolUse);
        Assert.Equal(128_000, capabilities.MaxContextTokens);
        Assert.Contains("rag", capabilities.Features);
        Assert.Contains("citations", capabilities.Features);
    }

    [Theory]
    [InlineData("llama-3.1-405b")]
    [InlineData("llama-3.1-70b")]
    [InlineData("llama-3.2-8b")]
    public void GetCapabilities_ForLlamaModels_ReturnsCorrectCapabilities(string modelName)
    {
        // Act
        var capabilities = _detector.GetCapabilities(modelName);

        // Assert
        Assert.Equal("Meta", capabilities.Provider);
        Assert.True(capabilities.SupportsToolUse);
        Assert.Equal(128_000, capabilities.MaxContextTokens);
        Assert.Contains("open_source", capabilities.Features);
    }

    [Fact]
    public void GetCapabilities_ForUnknownModel_ReturnsGenericCapabilities()
    {
        // Act
        var capabilities = _detector.GetCapabilities("unknown-model-xyz");

        // Assert
        Assert.Equal("Unknown", capabilities.Provider);
        Assert.False(capabilities.SupportsToolUse);
        Assert.False(capabilities.SupportsVision);
        Assert.Equal(8_192, capabilities.MaxContextTokens);
    }

    [Fact]
    public void GetCapabilities_CachesResults()
    {
        // Arrange
        var modelName = "claude-3-5-sonnet-20241022";

        // Act
        var capabilities1 = _detector.GetCapabilities(modelName);
        var capabilities2 = _detector.GetCapabilities(modelName);

        // Assert
        Assert.Same(capabilities1, capabilities2);
    }

    [Theory]
    [InlineData("claude-3-5-sonnet-20241022", "tool_use", true)]
    [InlineData("claude-3-5-sonnet-20241022", "vision", true)]
    [InlineData("gpt-4o", "json_mode", true)]
    [InlineData("unknown-model", "tool_use", false)]
    public void SupportsFeature_ReturnsCorrectResult(string modelName, string feature, bool expected)
    {
        // Act
        var result = _detector.SupportsFeature(modelName, feature);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("claude-3-5-sonnet-20241022", 100_000, true)]
    [InlineData("claude-3-5-sonnet-20241022", 300_000, false)]
    [InlineData("gpt-4", 5_000, true)]
    [InlineData("gpt-4", 10_000, false)]
    public void CanHandleContextSize_ReturnsCorrectResult(string modelName, int tokenCount, bool expected)
    {
        // Act
        var result = _detector.CanHandleContextSize(modelName, tokenCount);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void SelectBestModel_WithNoModels_ReturnsNull()
    {
        // Arrange
        var models = Array.Empty<string>();
        var requirements = new ModelRequirements
        {
            RequireToolUse = true
        };

        // Act
        var selected = _detector.SelectBestModel(models, requirements);

        // Assert
        Assert.Null(selected);
    }

    [Fact]
    public void SelectBestModel_WithToolUseRequirement_SelectsSuitableModel()
    {
        // Arrange
        var models = new[] { "claude-3-5-sonnet-20241022", "gpt-4o", "unknown-model" };
        var requirements = new ModelRequirements
        {
            RequireToolUse = true
        };

        // Act
        var selected = _detector.SelectBestModel(models, requirements);

        // Assert
        Assert.NotNull(selected);
        Assert.NotEqual("unknown-model", selected);
    }

    [Fact]
    public void SelectBestModel_WithVisionRequirement_SelectsVisionModel()
    {
        // Arrange
        var models = new[] { "claude-3-haiku-20240307", "claude-3-opus-20240229", "gpt-3.5-turbo" };
        var requirements = new ModelRequirements
        {
            RequireVision = true
        };

        // Act
        var selected = _detector.SelectBestModel(models, requirements);

        // Assert
        Assert.NotNull(selected);
        Assert.Contains("opus", selected.ToLower());
    }

    [Fact]
    public void SelectBestModel_WithContextRequirement_FiltersCorrectly()
    {
        // Arrange
        var models = new[] { "gpt-4", "gpt-4-turbo", "claude-3-5-sonnet-20241022" };
        var requirements = new ModelRequirements
        {
            MinContextTokens = 100_000
        };

        // Act
        var selected = _detector.SelectBestModel(models, requirements);

        // Assert
        Assert.NotNull(selected);
        Assert.NotEqual("gpt-4", selected); // Standard GPT-4 has only 8K context
    }

    [Fact]
    public void SelectBestModel_WithNoMatchingModels_ReturnsNull()
    {
        // Arrange
        var models = new[] { "unknown-model-1", "unknown-model-2" };
        var requirements = new ModelRequirements
        {
            RequireToolUse = true,
            RequireVision = true
        };

        // Act
        var selected = _detector.SelectBestModel(models, requirements);

        // Assert
        Assert.Null(selected);
    }

    [Fact]
    public void GetAllCapabilities_ReturnsAllCachedCapabilities()
    {
        // Arrange
        _detector.GetCapabilities("claude-3-5-sonnet-20241022");
        _detector.GetCapabilities("gpt-4o");

        // Act
        var all = _detector.GetAllCapabilities();

        // Assert
        Assert.Equal(2, all.Count);
        Assert.Contains("claude-3-5-sonnet-20241022", all.Keys);
        Assert.Contains("gpt-4o", all.Keys);
    }

    [Fact]
    public void ClearCache_RemovesAllCachedCapabilities()
    {
        // Arrange
        _detector.GetCapabilities("claude-3-5-sonnet-20241022");
        _detector.GetCapabilities("gpt-4o");

        // Act
        _detector.ClearCache();
        var all = _detector.GetAllCapabilities();

        // Assert
        Assert.Empty(all);
    }

    [Fact]
    public void ValidateModel_WithValidRequirements_ReturnsValid()
    {
        // Arrange
        var modelName = "claude-3-5-sonnet-20241022";
        var requirements = new ModelRequirements
        {
            RequireToolUse = true,
            RequireStreaming = true
        };

        // Act
        var result = _detector.ValidateModel(modelName, requirements);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
        Assert.Equal(modelName, result.ModelName);
    }

    [Fact]
    public void ValidateModel_WithInvalidRequirements_ReturnsErrors()
    {
        // Arrange
        var modelName = "unknown-model";
        var requirements = new ModelRequirements
        {
            RequireToolUse = true,
            RequireVision = true,
            RequireJsonMode = true
        };

        // Act
        var result = _detector.ValidateModel(modelName, requirements);

        // Assert
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
        Assert.Contains(result.Errors, e => e.Contains("tool use"));
        Assert.Contains(result.Errors, e => e.Contains("vision"));
        Assert.Contains(result.Errors, e => e.Contains("JSON"));
    }

    [Fact]
    public void ValidateModel_WithContextRequirement_ValidatesCorrectly()
    {
        // Arrange
        var modelName = "gpt-4";
        var requirements = new ModelRequirements
        {
            MinContextTokens = 100_000
        };

        // Act
        var result = _detector.ValidateModel(modelName, requirements);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("context window"));
    }

    [Fact]
    public void GetCapabilities_WithAdapter_UsesAdapterCapabilities()
    {
        // Arrange
        var modelName = "custom-model";
        var expectedCapabilities = new ModelCapabilities
        {
            ModelName = modelName,
            Provider = "TestProvider",
            SupportsToolUse = true,
            SupportsVision = true,
            SupportsStreaming = true,
            MaxContextTokens = 50_000,
            MaxOutputTokens = 2_000,
            SupportsJsonMode = true,
            SupportsParallelToolCalls = false,
            Features = new List<string> { "custom_feature" }
        };

        _mockAdapter.Setup(a => a.ValidateModel(modelName)).Returns(true);
        _mockAdapter.Setup(a => a.GetCapabilities(modelName)).Returns(expectedCapabilities);

        // Act
        var capabilities = _detector.GetCapabilities(modelName);

        // Assert
        Assert.Equal("TestProvider", capabilities.Provider);
        Assert.True(capabilities.SupportsVision);
        Assert.Contains("custom_feature", capabilities.Features);
    }
}
