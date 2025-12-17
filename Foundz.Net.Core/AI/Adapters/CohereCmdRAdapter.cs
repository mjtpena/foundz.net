using Foundz.Net.Shared.Interfaces;
using Foundz.Net.Shared.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Foundz.Net.Core.AI.Adapters;

/// <summary>
/// Provider adapter for Cohere Command R and Command R+ models.
/// Converts between Foundz.Net format and Cohere's conversation API format.
/// </summary>
public class CohereCmdRAdapter : IProviderAdapter
{
    private readonly ILogger<CohereCmdRAdapter> _logger;

    public string ProviderName => "Cohere";

    public CohereCmdRAdapter(ILogger<CohereCmdRAdapter> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Formats messages for Cohere's conversation API.
    /// Cohere uses: {"role": "USER|CHATBOT|SYSTEM", "message": "..."}
    /// </summary>
    public object FormatMessages(List<Message> messages)
    {
        var cohereMessages = new List<object>();

        foreach (var message in messages)
        {
            var role = message.Role switch
            {
                MessageRole.User => "USER",
                MessageRole.Assistant => "CHATBOT",
                MessageRole.System => "SYSTEM",
                MessageRole.Tool => "TOOL",
                _ => throw new ArgumentException($"Unknown role: {message.Role}")
            };

            // Cohere format
            cohereMessages.Add(new
            {
                role = role,
                message = message.Content ?? string.Empty
            });
        }

        return cohereMessages;
    }

    /// <summary>
    /// Formats tools for Cohere's tool use API.
    /// Cohere uses tool definitions with parameter schemas.
    /// </summary>
    public object FormatTools(List<ToolDefinition> tools)
    {
        var cohereTools = new List<object>();

        foreach (var tool in tools)
        {
            var toolDef = new
            {
                name = tool.Name,
                description = tool.Description,
                parameter_definitions = FormatParameters(tool.Parameters)
            };

            cohereTools.Add(toolDef);
        }

        return cohereTools;
    }

    /// <summary>
    /// Parses Cohere's response format.
    /// Cohere returns: {"text": "...", "tool_calls": [...]}
    /// </summary>
    public AIResponse ParseResponse(object response)
    {
        try
        {
            var json = response as JsonElement? ?? JsonSerializer.SerializeToElement(response);

            var text = json.TryGetProperty("text", out var textProp) 
                ? textProp.GetString() ?? string.Empty 
                : string.Empty;

            var toolCalls = new List<ToolCall>();

            if (json.TryGetProperty("tool_calls", out var toolCallsProp) 
                && toolCallsProp.ValueKind == JsonValueKind.Array)
            {
                foreach (var call in toolCallsProp.EnumerateArray())
                {
                    var name = call.GetProperty("name").GetString() ?? string.Empty;
                    var paramsProp = call.TryGetProperty("parameters", out var p)
                        ? p
                        : JsonDocument.Parse("{}").RootElement;

                    var arguments = new Dictionary<string, object>();
                    foreach (var prop in paramsProp.EnumerateObject())
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
                        Id = Guid.NewGuid().ToString(),
                        Name = name,
                        Arguments = arguments
                    });
                }
            }

            // Extract token usage if available
            int? promptTokens = null;
            int? completionTokens = null;

            if (json.TryGetProperty("meta", out var metaProp))
            {
                if (metaProp.TryGetProperty("tokens", out var tokensProp))
                {
                    if (tokensProp.TryGetProperty("input_tokens", out var inputTokens))
                        promptTokens = inputTokens.GetInt32();
                    if (tokensProp.TryGetProperty("output_tokens", out var outputTokens))
                        completionTokens = outputTokens.GetInt32();
                }
            }

            return new AIResponse
            {
                Content = text,
                ToolCalls = toolCalls,
                FinishReason = ExtractFinishReason(json),
                ModelUsed = ExtractModel(json),
                TokenUsage = new TokenUsage
                {
                    PromptTokens = promptTokens ?? 0,
                    CompletionTokens = completionTokens ?? 0
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse Cohere response");
            throw new InvalidOperationException("Failed to parse Cohere response", ex);
        }
    }

    /// <summary>
    /// Parses streaming chunks from Cohere.
    /// Cohere sends: {"event_type": "...", "text": "..."} via SSE
    /// </summary>
    public AIResponseChunk ParseStreamChunk(object chunk)
    {
        try
        {
            var json = chunk as JsonElement? ?? JsonSerializer.SerializeToElement(chunk);

            var eventType = json.TryGetProperty("event_type", out var typeProp)
                ? typeProp.GetString()
                : null;

            if (eventType == "text-generation")
            {
                var text = json.GetProperty("text").GetString() ?? string.Empty;

                return new AIResponseChunk
                {
                    ContentDelta = text,
                    FinishReason = null
                };
            }
            else if (eventType == "stream-end")
            {
                var finishReason = json.TryGetProperty("finish_reason", out var reasonProp)
                    ? reasonProp.GetString()
                    : "stop";

                return new AIResponseChunk
                {
                    ContentDelta = string.Empty,
                    FinishReason = finishReason
                };
            }
            else if (eventType == "tool-calls-generation")
            {
                // Tool call in progress
                return new AIResponseChunk
                {
                    ContentDelta = string.Empty,
                    FinishReason = null
                };
            }

            // Unknown event type, skip
            return new AIResponseChunk
            {
                ContentDelta = string.Empty,
                FinishReason = null
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse Cohere stream chunk, skipping");
            return new AIResponseChunk
            {
                ContentDelta = string.Empty,
                FinishReason = null
            };
        }
    }

    /// <summary>
    /// Returns Cohere Command R capabilities.
    /// </summary>
    public ModelCapabilities GetCapabilities(string modelName)
    {
        return new ModelCapabilities
        {
            ModelName = modelName,
            Provider = "Cohere",
            SupportsToolUse = true,
            SupportsVision = false,  // Command R doesn't support vision
            SupportsStreaming = true,
            MaxContextTokens = 128_000,  // Command R: 128K context
            MaxOutputTokens = 4_000,     // Command R: 4K output
            SupportsJsonMode = false,
            SupportsParallelToolCalls = true,
            SupportedLanguages = new List<string> { "en", "fr", "es", "de", "it", "pt", "ja", "ko", "zh", "ar" },
            Features = new List<string> { "tool_use", "rag", "grounding", "citations", "multilingual" }
        };
    }

    /// <summary>
    /// Validates if a model name is compatible with Cohere.
    /// </summary>
    public bool ValidateModel(string modelName)
    {
        var supportedModels = new[]
        {
            "command-r",
            "command-r-plus",
            "command-r-08-2024",
            "command-r-plus-08-2024"
        };

        return supportedModels.Any(m => 
            modelName.Contains(m, StringComparison.OrdinalIgnoreCase));
    }

    // Private helper methods

    private object FormatParameters(Dictionary<string, object> parametersSchema)
    {
        var parameters = new Dictionary<string, object>();

        if (!parametersSchema.TryGetValue("properties", out var propertiesObj))
            return parameters;

        // Convert to JsonElement for easier parsing
        var jsonText = JsonSerializer.Serialize(propertiesObj);
        var properties = JsonDocument.Parse(jsonText).RootElement;

        foreach (var prop in properties.EnumerateObject())
        {
            var paramDef = new Dictionary<string, object>
            {
                ["type"] = prop.Value.TryGetProperty("type", out var typeProp)
                    ? typeProp.GetString() ?? "string"
                    : "string"
            };

            if (prop.Value.TryGetProperty("description", out var descProp))
                paramDef["description"] = descProp.GetString() ?? string.Empty;

            if (prop.Value.TryGetProperty("required", out var reqProp) && reqProp.GetBoolean())
                paramDef["required"] = true;

            parameters[prop.Name] = paramDef;
        }

        return parameters;
    }

    private string ExtractFinishReason(JsonElement json)
    {
        if (json.TryGetProperty("finish_reason", out var reasonProp))
            return reasonProp.GetString() ?? "stop";

        if (json.TryGetProperty("meta", out var metaProp) 
            && metaProp.TryGetProperty("finish_reason", out var metaReasonProp))
            return metaReasonProp.GetString() ?? "stop";

        return "stop";
    }

    private string? ExtractModel(JsonElement json)
    {
        if (json.TryGetProperty("model", out var modelProp))
            return modelProp.GetString();

        if (json.TryGetProperty("meta", out var metaProp) 
            && metaProp.TryGetProperty("model", out var metaModelProp))
            return metaModelProp.GetString();

        return null;
    }
}
