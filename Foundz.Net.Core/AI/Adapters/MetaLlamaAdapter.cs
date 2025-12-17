using Foundz.Net.Shared.Interfaces;
using Foundz.Net.Shared.Models;

namespace Foundz.Net.Core.AI.Adapters;

/// <summary>
/// Provider adapter for Meta Llama models via Azure AI Foundry.
/// </summary>
public class MetaLlamaAdapter : IProviderAdapter
{
    public string ProviderName => "Meta";

    public ModelCapabilities GetCapabilities(string modelName)
    {
        return modelName.ToLowerInvariant() switch
        {
            "llama-3.3-70b-instruct" => new ModelCapabilities
            {
                ModelName = modelName,
                Provider = ProviderName,
                SupportsToolUse = true,
                SupportsVision = false,
                SupportsJsonMode = true,
                SupportsStreaming = true,
                MaxContextTokens = 128000,
                MaxOutputTokens = 4096,
                CostPer1kInputTokens = 0.9m,
                CostPer1kOutputTokens = 0.9m
            },
            "llama-3.1-405b-instruct" => new ModelCapabilities
            {
                ModelName = modelName,
                Provider = ProviderName,
                SupportsToolUse = true,
                SupportsVision = false,
                SupportsJsonMode = true,
                SupportsStreaming = true,
                MaxContextTokens = 128000,
                MaxOutputTokens = 4096,
                CostPer1kInputTokens = 5.0m,
                CostPer1kOutputTokens = 15.0m
            },
            _ => new ModelCapabilities
            {
                ModelName = modelName,
                Provider = ProviderName,
                SupportsToolUse = false,
                SupportsVision = false,
                SupportsJsonMode = true,
                SupportsStreaming = true,
                MaxContextTokens = 8192,
                MaxOutputTokens = 2048,
                CostPer1kInputTokens = 1.0m,
                CostPer1kOutputTokens = 1.0m
            }
        };
    }

    public string FormatToolResult(ToolResult result)
    {
        if (result.Success)
        {
            return result.Output ?? "Success";
        }
        return $"Error: {result.Error}";
    }

    public object FormatMessages(List<Message> messages)
    {
        return messages;
    }

    public object FormatTools(List<ToolDefinition> tools)
    {
        return tools;
    }

    public AIResponse ParseResponse(object providerResponse)
    {
        return new AIResponse
        {
            Content = providerResponse.ToString() ?? "",
            FinishReason = "stop",
            ModelUsed = "llama-3.3-70b-instruct",
            TokenUsage = new TokenUsage { PromptTokens = 0, CompletionTokens = 0 }
        };
    }

    public AIResponseChunk ParseStreamChunk(object providerChunk)
    {
        return new AIResponseChunk
        {
            ContentDelta = providerChunk.ToString(),
            FinishReason = null
        };
    }

    public bool ValidateModel(string modelName)
    {
        return modelName.ToLowerInvariant().Contains("llama");
    }
}
