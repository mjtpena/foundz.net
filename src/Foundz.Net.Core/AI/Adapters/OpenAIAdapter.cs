using Foundz.Net.Shared.Interfaces;
using Foundz.Net.Shared.Models;

namespace Foundz.Net.Core.AI.Adapters;

/// <summary>
/// Provider adapter for OpenAI models (GPT-4, GPT-4o) via Azure AI Foundry.
/// </summary>
public class OpenAIAdapter : IProviderAdapter
{
    public string ProviderName => "OpenAI";

    public ModelCapabilities GetCapabilities(string modelName)
    {
        return modelName.ToLowerInvariant() switch
        {
            "gpt-4o" or "gpt-4o-2024-11-20" => new ModelCapabilities
            {
                ModelName = modelName,
                Provider = ProviderName,
                SupportsToolUse = true,
                SupportsVision = true,
                SupportsJsonMode = true,
                SupportsStreaming = true,
                MaxContextTokens = 128000,
                MaxOutputTokens = 16384,
                CostPer1kInputTokens = 2.5m,
                CostPer1kOutputTokens = 10.0m
            },
            "gpt-4o-mini" => new ModelCapabilities
            {
                ModelName = modelName,
                Provider = ProviderName,
                SupportsToolUse = true,
                SupportsVision = true,
                SupportsJsonMode = true,
                SupportsStreaming = true,
                MaxContextTokens = 128000,
                MaxOutputTokens = 16384,
                CostPer1kInputTokens = 0.15m,
                CostPer1kOutputTokens = 0.6m
            },
            "gpt-4-turbo" => new ModelCapabilities
            {
                ModelName = modelName,
                Provider = ProviderName,
                SupportsToolUse = true,
                SupportsVision = true,
                SupportsJsonMode = true,
                SupportsStreaming = true,
                MaxContextTokens = 128000,
                MaxOutputTokens = 4096,
                CostPer1kInputTokens = 10.0m,
                CostPer1kOutputTokens = 30.0m
            },
            _ => new ModelCapabilities
            {
                ModelName = modelName,
                Provider = ProviderName,
                SupportsToolUse = true,
                SupportsVision = false,
                SupportsJsonMode = true,
                SupportsStreaming = true,
                MaxContextTokens = 8192,
                MaxOutputTokens = 4096,
                CostPer1kInputTokens = 10.0m,
                CostPer1kOutputTokens = 30.0m
            }
        };
    }

    public string FormatToolResult(ToolResult result)
    {
        if (result.Success)
        {
            return result.Output ?? "Success";
        }
        return $"Error: {result.Error ?? "Unknown error"}";
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
            ModelUsed = "gpt-4o",
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
        return modelName.ToLowerInvariant().Contains("gpt");
    }
}
