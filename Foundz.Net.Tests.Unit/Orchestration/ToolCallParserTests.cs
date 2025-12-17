using Foundz.Net.Core.Orchestration;
using Foundz.Net.Shared.Interfaces;
using Foundz.Net.Shared.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Foundz.Net.Tests.Unit.Orchestration;

public class ToolCallParserTests
{
    private readonly Mock<ILogger<ToolCallParser>> _loggerMock;
    private readonly ToolCallParser _parser;

    public ToolCallParserTests()
    {
        _loggerMock = new Mock<ILogger<ToolCallParser>>();
        _parser = new ToolCallParser(_loggerMock.Object);
    }

    [Fact]
    public void ParseToolCalls_NoToolCalls_ReturnsEmptyList()
    {
        // Arrange
        var response = new AIResponse
        {
            Content = "Hello",
            TokenUsage = new TokenUsage { PromptTokens = 10, CompletionTokens = 5 },
            FinishReason = "stop",
            ToolCalls = null
        };

        // Act
        var result = _parser.ParseToolCalls(response);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void ParseToolCalls_EmptyToolCalls_ReturnsEmptyList()
    {
        // Arrange
        var response = new AIResponse
        {
            Content = "Hello",
            TokenUsage = new TokenUsage { PromptTokens = 10, CompletionTokens = 5 },
            FinishReason = "stop",
            ToolCalls = new List<ToolCall>()
        };

        // Act
        var result = _parser.ParseToolCalls(response);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void ParseToolCalls_ValidToolCall_ParsesSuccessfully()
    {
        // Arrange
        var response = new AIResponse
        {
            Content = "Using tool",
            TokenUsage = new TokenUsage { PromptTokens = 10, CompletionTokens = 5 },
            FinishReason = "tool_use",
            ToolCalls = new List<ToolCall>
            {
                new()
                {
                    Id = "call-1",
                    Name = "read_file",
                    Arguments = new Dictionary<string, object>
                    {
                        ["path"] = "test.txt"
                    }
                }
            }
        };

        // Act
        var result = _parser.ParseToolCalls(response);

        // Assert
        Assert.Single(result);
        Assert.Equal("call-1", result[0].Id);
        Assert.Equal("read_file", result[0].Name);
        Assert.Equal("test.txt", result[0].Arguments["path"]);
    }

    [Fact]
    public void ParseToolCalls_MultipleToolCalls_ParsesAll()
    {
        // Arrange
        var response = new AIResponse
        {
            Content = "",
            TokenUsage = new TokenUsage { PromptTokens = 10, CompletionTokens = 5 },
            FinishReason = "tool_use",
            ToolCalls = new List<ToolCall>
            {
                new() { Id = "1", Name = "tool1", Arguments = new Dictionary<string, object>() },
                new() { Id = "2", Name = "tool2", Arguments = new Dictionary<string, object>() },
                new() { Id = "3", Name = "tool3", Arguments = new Dictionary<string, object>() }
            }
        };

        // Act
        var result = _parser.ParseToolCalls(response);

        // Assert
        Assert.Equal(3, result.Count);
    }

    [Fact]
    public void ParseToolCalls_MissingName_SkipsToolCall()
    {
        // Arrange
        var response = new AIResponse
        {
            Content = "",
            TokenUsage = new TokenUsage { PromptTokens = 10, CompletionTokens = 5 },
            FinishReason = "tool_use",
            ToolCalls = new List<ToolCall>
            {
                new() { Id = "1", Name = "", Arguments = new Dictionary<string, object>() },
                new() { Id = "2", Name = "valid_tool", Arguments = new Dictionary<string, object>() }
            }
        };

        // Act
        var result = _parser.ParseToolCalls(response);

        // Assert
        Assert.Single(result);
        Assert.Equal("valid_tool", result[0].Name);
    }

    [Fact]
    public void ParseToolCalls_NullArguments_UsesEmptyDictionary()
    {
        // Arrange
        var response = new AIResponse
        {
            Content = "",
            TokenUsage = new TokenUsage { PromptTokens = 10, CompletionTokens = 5 },
            FinishReason = "tool_use",
            ToolCalls = new List<ToolCall>
            {
                new() { Id = "1", Name = "test_tool", Arguments = null! }
            }
        };

        // Act
        var result = _parser.ParseToolCalls(response);

        // Assert
        Assert.Single(result);
        Assert.NotNull(result[0].Arguments);
        Assert.Empty(result[0].Arguments);
    }

    [Fact]
    public void ParseOpenAIFormat_ValidFormat_ParsesSuccessfully()
    {
        // Arrange
        var json = @"{
            ""tool_calls"": [
                {
                    ""id"": ""call_123"",
                    ""type"": ""function"",
                    ""function"": {
                        ""name"": ""read_file"",
                        ""arguments"": ""{\""path\"": \""test.txt\""}""
                    }
                }
            ]
        }";

        // Act
        var result = _parser.ParseOpenAIFormat(json);

        // Assert
        Assert.Single(result);
        Assert.Equal("call_123", result[0].Id);
        Assert.Equal("read_file", result[0].Name);
    }

    [Fact]
    public void ParseOpenAIFormat_LegacyFunctionCall_ParsesSuccessfully()
    {
        // Arrange
        var json = @"{
            ""function_call"": {
                ""name"": ""read_file"",
                ""arguments"": ""{\""path\"": \""test.txt\""}""
            }
        }";

        // Act
        var result = _parser.ParseOpenAIFormat(json);

        // Assert
        Assert.Single(result);
        Assert.Equal("read_file", result[0].Name);
    }

    [Fact]
    public void ParseOpenAIFormat_InvalidJson_ReturnsEmptyList()
    {
        // Arrange
        var json = "{ invalid json }";

        // Act
        var result = _parser.ParseOpenAIFormat(json);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void ParseOpenAIFormat_NoToolCalls_ReturnsEmptyList()
    {
        // Arrange
        var json = @"{ ""message"": ""Hello"" }";

        // Act
        var result = _parser.ParseOpenAIFormat(json);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void ParseAnthropicFormat_ValidFormat_ParsesSuccessfully()
    {
        // Arrange
        var json = @"{
            ""content"": [
                {
                    ""type"": ""tool_use"",
                    ""id"": ""toolu_123"",
                    ""name"": ""read_file"",
                    ""input"": {
                        ""path"": ""test.txt""
                    }
                }
            ]
        }";

        // Act
        var result = _parser.ParseAnthropicFormat(json);

        // Assert
        Assert.Single(result);
        Assert.Equal("toolu_123", result[0].Id);
        Assert.Equal("read_file", result[0].Name);
    }

    [Fact]
    public void ParseAnthropicFormat_MultipleBlocks_ParsesOnlyToolUse()
    {
        // Arrange
        var json = @"{
            ""content"": [
                {
                    ""type"": ""text"",
                    ""text"": ""Let me read that file.""
                },
                {
                    ""type"": ""tool_use"",
                    ""id"": ""toolu_123"",
                    ""name"": ""read_file"",
                    ""input"": {
                        ""path"": ""test.txt""
                    }
                }
            ]
        }";

        // Act
        var result = _parser.ParseAnthropicFormat(json);

        // Assert
        Assert.Single(result);
        Assert.Equal("read_file", result[0].Name);
    }

    [Fact]
    public void ParseAnthropicFormat_InvalidJson_ReturnsEmptyList()
    {
        // Arrange
        var json = "invalid json";

        // Act
        var result = _parser.ParseAnthropicFormat(json);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void ValidateToolCalls_AllValid_ReturnsTrue()
    {
        // Arrange
        var toolCalls = new List<ToolCall>
        {
            new() { Id = "1", Name = "read_file", Arguments = new Dictionary<string, object>() },
            new() { Id = "2", Name = "write_file", Arguments = new Dictionary<string, object>() }
        };

        var toolRegistryMock = new Mock<IToolRegistry>();
        var readTool = new Mock<ITool>();
        readTool.Setup(x => x.Name).Returns("read_file");
        var writeTool = new Mock<ITool>();
        writeTool.Setup(x => x.Name).Returns("write_file");
        
        toolRegistryMock.Setup(x => x.GetTool("read_file")).Returns(readTool.Object);
        toolRegistryMock.Setup(x => x.GetTool("write_file")).Returns(writeTool.Object);

        // Act
        var isValid = _parser.ValidateToolCalls(toolCalls, toolRegistryMock.Object);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void ValidateToolCalls_UnknownTool_ReturnsFalse()
    {
        // Arrange
        var toolCalls = new List<ToolCall>
        {
            new() { Id = "1", Name = "unknown_tool", Arguments = new Dictionary<string, object>() }
        };

        var toolRegistryMock = new Mock<IToolRegistry>();
        toolRegistryMock.Setup(x => x.GetTool("unknown_tool")).Returns((ITool?)null);

        // Act
        var isValid = _parser.ValidateToolCalls(toolCalls, toolRegistryMock.Object);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void GetToolNames_ExtractsUniqueNames()
    {
        // Arrange
        var toolCalls = new List<ToolCall>
        {
            new() { Id = "1", Name = "read_file", Arguments = new Dictionary<string, object>() },
            new() { Id = "2", Name = "write_file", Arguments = new Dictionary<string, object>() },
            new() { Id = "3", Name = "read_file", Arguments = new Dictionary<string, object>() } // Duplicate
        };

        // Act
        var names = _parser.GetToolNames(toolCalls);

        // Assert
        Assert.Equal(2, names.Count);
        Assert.Contains("read_file", names);
        Assert.Contains("write_file", names);
    }

    [Fact]
    public void GetToolNames_EmptyList_ReturnsEmpty()
    {
        // Arrange
        var toolCalls = new List<ToolCall>();

        // Act
        var names = _parser.GetToolNames(toolCalls);

        // Assert
        Assert.Empty(names);
    }
}
