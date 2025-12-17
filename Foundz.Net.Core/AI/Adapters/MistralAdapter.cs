using Foundz.Net.Shared.Interfaces;
using Foundz.Net.Shared.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Foundz.Net.Core.AI.Adapters;

/// <summary>
/// Provider adapter for Mistral AI models (Mistral Large, Mistral Medium, Mistral Small).
/// Converts between Foundz.Net format and Mistral's API format (similar to OpenAI).
/// </summary>
public class MistralAdapter : IProviderAdapter
{
    private readonly ILogger<MistralAdapter> _logger;

    public string ProviderName => "Mistral";

    public MistralAdapter(ILogger<MistralAdapter> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Formats messages for Mistral's chat API.
    /// Mistral uses OpenAI-compatible format: {"role": "user|assistant|system", "content": "..."}
    /// </summary>
    public object FormatMessages(List<Message> messages)
    {
        var mistralMessages = new List<object>();

        foreach (var message in messages)
        {
            var role = message.Role switch
            {
                MessageRole.User => "user",
                MessageRole.Assistant => "assistant",
                MessageRole.System => "system",
                MessageRole.Tool => "tool",
                _ => throw new ArgumentException($"Unknown role: {message.Role}")
            };

            // Mistral format (OpenAI-compatible)
            var msg = new Dictionary<string, object>
            {
                ["role"] = role,
                ["content"] = message.Content ?? string.Empty
            };

            // Add tool_calls if present
            if (message.ToolCalls != null && message.ToolCalls.Any())
            {
                msg["tool_calls"] = message.ToolCalls.Select(tc => new
                {
                    id = tc.Id,
                    type = "function",
                    function = new
                    {
                        name = tc.Name,
                        arguments = JsonSerializer.Serialize(tc.Arguments)
                    }
                }).ToArray();
            }

            // Add tool_call_id if this is a tool result
            if (message.Role == MessageRole.Tool && !string.IsNullOrEmpty(message.ToolCallId))
            {
                msg["tool_call_id"] = message.ToolCallId;
            }

            mistralMessages.Add(msg);
        }

        return mistralMessages;
    }

    /// <summary>
    /// Formats tools for Mistral's function calling API.
    /// Mistral uses OpenAI-compatible format.
    /// </summary>
    public object FormatTools(List<ToolDefinition> tools)
    {
        var mistralTools = new List<object>();

        foreach (var tool in tools)
        {
            var toolDef = new
            {
                type = "function",
                function = new
                {
                    name = tool.Name,
                    description = tool.Description,
                    parameters = tool.Parameters
                }
            };

            mistralTools.Add(toolDef);
        }

        return mistralTools;
    }

    /// <summary>
    /// Parses Mistral's response format (OpenAI-compatible).
    /// </summary>
    public AIResponse ParseResponse(object response)
    {
        try
        {
            var json = response as JsonElement? ?? JsonSerializer.SerializeToElement(response);

            // Extract message from choices[0].message
            var message = json.GetProperty("choices")[0].GetProperty("message");

            var content = message.TryGetProperty("content", out var contentProp)
                ? contentProp.GetString() ?? string.Empty
                : string.Empty;

            var toolCalls = new List<ToolCall>();

            // Parse tool_calls if present
            if (message.TryGetProperty("tool_calls", out var toolCallsProp) 
                && toolCallsProp.ValueKind == JsonValueKind.Array)
            {
                foreach (var call in toolCallsProp.EnumerateArray())
                {
                    var id = call.GetProperty("id").GetString() ?? Guid.NewGuid().ToString();
                    var function = call.GetProperty("function");
                    var name = function.GetProperty("name").GetString() ?? string.Empty;
                    var argsJson = function.GetProperty("arguments").GetString() ?? "{}";

                    var argsDoc = JsonDocument.Parse(argsJson);
                    var arguments = new Dictionary<string, object>();
                    foreach (var prop in argsDoc.RootElement.EnumerateObject())
                    {
                        arguments[prop.Name] = prop.Value.ValueKind switch
                        {
                            JsonValueKind.String => prop.Value.GetString() ?? string.Empty,
                            JsonValueKind.Number => prop.Value.GetDouble(),
                            JsonValueKind.True => true,
                            JsonValueKind.False => false,
                            JsonValueKind.Null => null!,
                            _ => prop.Value.ToString()
                        };
                    }

                    toolCalls.Add(new ToolCall
                    {
                        Id = id,
                        Name = name,
                        Arguments = arguments
                    });
                }
            }

            // Extract token usage
            int? promptTokens = null;
            int? completionTokens = null;

            if (json.TryGetProperty("usage", out var usageProp))
            {
                if (usageProp.TryGetProperty("prompt_tokens", out var promptProp))
                    promptTokens = promptProp.GetInt32();
                if (usageProp.TryGetProperty("completion_tokens", out var compProp))
                    completionTokens = compProp.GetInt32();
            }

            var finishReason = json.GetProperty("choices")[0]
                .TryGetProperty("finish_reason", out var reasonProp)
                ? reasonProp.GetString() ?? "stop"
                : "stop";

            var model = json.TryGetProperty("model", out var modelProp)
                ? modelProp.GetString()
                : null;

            return new AIResponse
            {
                Content = content,
                ToolCalls = toolCalls,
                FinishReason = finishReason,
                ModelUsed = model,
                TokenUsage = new TokenUsage
                {
                    PromptTokens = promptTokens ?? 0,
                    CompletionTokens = completionTokens ?? 0
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse Mistral response");
            throw new InvalidOperationException("Failed to parse Mistral response", ex);
        }
    }

    /// <summary>
    /// Parses streaming chunks from Mistral (OpenAI-compatible SSE format).
    /// </summary>
    public AIResponseChunk ParseStreamChunk(object chunk)
    {
        try
        {
            var json = chunk as JsonElement? ?? JsonSerializer.SerializeToElement(chunk);

            // Check for [DONE] marker
            if (json.ValueKind == JsonValueKind.String 
                && json.GetString() == "[DONE]")
            {
                return new AIResponseChunk
                {
                    ContentDelta = string.Empty,
                    FinishReason = "stop"
                };
            }

            var choice = json.GetProperty("choices")[0];
            var delta = choice.GetProperty("delta");

            var contentDelta = delta.TryGetProperty("content", out var contentProp)
                ? contentProp.GetString() ?? string.Empty
                : string.Empty;

            var finishReason = choice.TryGetProperty("finish_reason", out var reasonProp)
                && reasonProp.ValueKind != JsonValueKind.Null
                ? reasonProp.GetString()
                : null;

            return new AIResponseChunk
            {
                ContentDelta = contentDelta,
                FinishReason = finishReason
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse Mistral stream chunk, skipping");
            return new AIResponseChunk
            {
                ContentDelta = string.Empty,
                FinishReason = null
            };
        }
    }

    /// <summary>
    /// Returns Mistral model capabilities.
    /// </summary>
    public ModelCapabilities GetCapabilities(string modelName)
    {
        return new ModelCapabilities
        {
            ModelName = modelName,
            Provider = "Mistral",
            SupportsToolUse = true,
            SupportsVision = false,  // Mistral doesn't support vision (yet)
            SupportsStreaming = true,
            MaxContextTokens = 32_000,   // Mistral Large: 32K context
            MaxOutputTokens = 8_192,     // Mistral Large: 8K output
            SupportsJsonMode = true,     // Mistral supports JSON mode
            SupportsParallelToolCalls = true,
            SupportedLanguages = new List<string> { "en", "fr", "de", "es", "it", "pt", "nl", "ru", "zh", "ja", "ko" },
            Features = new List<string> { "tool_use", "function_calling", "json_mode", "multilingual", "eu_data_residency" }
        };
    }

    /// <summary>
    /// Validates if a model name is compatible with Mistral.
    /// </summary>
    public bool ValidateModel(string modelName)
    {
        var supportedModels = new[]
        {
            "mistral-large",
            "mistral-medium",
            "mistral-small",
            "mistral-tiny",
            "mistral-7b",
            "mistral-8x7b",
            "mistral-8x22b",
            "open-mistral-7b",
            "open-mixtral-8x7b",
            "open-mixtral-8x22b"
        };

        return supportedModels.Any(m => 
            modelName.Contains(m, StringComparison.OrdinalIgnoreCase));
    }
}
