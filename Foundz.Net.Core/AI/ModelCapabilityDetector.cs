using Foundz.Net.Shared.Interfaces;
using Foundz.Net.Shared.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Foundz.Net.Core.AI;

/// <summary>
/// Detects and caches AI model capabilities for intelligent feature selection.
/// </summary>
public class ModelCapabilityDetector
{
    private readonly ILogger<ModelCapabilityDetector> _logger;
    private readonly ConcurrentDictionary<string, ModelCapabilities> _capabilityCache;
    private readonly Dictionary<string, IProviderAdapter> _adapters;

    public ModelCapabilityDetector(
        ILogger<ModelCapabilityDetector> logger,
        IEnumerable<IProviderAdapter> adapters)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _capabilityCache = new ConcurrentDictionary<string, ModelCapabilities>();
        _adapters = adapters?.ToDictionary(a => a.ProviderName, a => a)
            ?? throw new ArgumentNullException(nameof(adapters));

        _logger.LogInformation("Initialized ModelCapabilityDetector with {Count} adapters",
            _adapters.Count);
    }

    /// <summary>
    /// Gets capabilities for a model, using cache if available.
    /// </summary>
    public ModelCapabilities GetCapabilities(string modelName)
    {
        // Check cache first
        if (_capabilityCache.TryGetValue(modelName, out var cached))
        {
            _logger.LogDebug("Retrieved cached capabilities for {Model}", modelName);
            return cached;
        }

        // Detect capabilities
        var capabilities = DetectCapabilities(modelName);

        // Cache for future use
        _capabilityCache.TryAdd(modelName, capabilities);

        _logger.LogInformation("Detected and cached capabilities for {Model}", modelName);

        return capabilities;
    }

    /// <summary>
    /// Detects capabilities by analyzing model name and provider.
    /// </summary>
    private ModelCapabilities DetectCapabilities(string modelName)
    {
        // Try to find matching adapter
        var adapter = FindAdapterForModel(modelName);

        if (adapter != null)
        {
            var capabilities = adapter.GetCapabilities(modelName);
            _logger.LogDebug("Found adapter {Provider} for model {Model}",
                adapter.ProviderName, modelName);
            return capabilities;
        }

        // Fallback: Infer from model name
        _logger.LogWarning("No adapter found for {Model}, using fallback detection", modelName);
        return InferCapabilitiesFromName(modelName);
    }

    /// <summary>
    /// Finds the appropriate adapter for a model.
    /// </summary>
    private IProviderAdapter? FindAdapterForModel(string modelName)
    {
        foreach (var adapter in _adapters.Values)
        {
            if (adapter.ValidateModel(modelName))
            {
                return adapter;
            }
        }

        return null;
    }

    /// <summary>
    /// Infers capabilities from model name patterns (fallback method).
    /// </summary>
    private ModelCapabilities InferCapabilitiesFromName(string modelName)
    {
        var name = modelName.ToLowerInvariant();

        // Claude models
        if (name.Contains("claude"))
        {
            var isOpus = name.Contains("opus");
            var isSonnet = name.Contains("sonnet");
            var isHaiku = name.Contains("haiku");

            return new ModelCapabilities
            {
                ModelName = modelName,
                Provider = "Anthropic",
                SupportsToolUse = true,
                SupportsVision = isOpus || isSonnet,  // Opus and Sonnet support vision
                SupportsStreaming = true,
                MaxContextTokens = 200_000,
                MaxOutputTokens = isOpus ? 4_096 : 8_192,
                SupportsJsonMode = false,
                SupportsParallelToolCalls = true,
                Features = new List<string> { "tool_use", "vision", "long_context", "thinking" }
            };
        }

        // GPT-4 models
        if (name.Contains("gpt-4"))
        {
            var isVision = name.Contains("vision");
            var isTurbo = name.Contains("turbo");
            var isO = name.Contains("-o");

            return new ModelCapabilities
            {
                ModelName = modelName,
                Provider = "OpenAI",
                SupportsToolUse = true,
                SupportsVision = isVision || isO,
                SupportsStreaming = true,
                MaxContextTokens = isTurbo || isO ? 128_000 : 8_192,
                MaxOutputTokens = 4_096,
                SupportsJsonMode = true,
                SupportsParallelToolCalls = true,
                Features = new List<string> { "tool_use", "function_calling", "json_mode", "structured_outputs" }
            };
        }

        // GPT-3.5
        if (name.Contains("gpt-3.5"))
        {
            return new ModelCapabilities
            {
                ModelName = modelName,
                Provider = "OpenAI",
                SupportsToolUse = true,
                SupportsVision = false,
                SupportsStreaming = true,
                MaxContextTokens = 16_384,
                MaxOutputTokens = 4_096,
                SupportsJsonMode = true,
                SupportsParallelToolCalls = false,
                Features = new List<string> { "tool_use", "function_calling", "json_mode" }
            };
        }

        // Mistral models
        if (name.Contains("mistral"))
        {
            return new ModelCapabilities
            {
                ModelName = modelName,
                Provider = "Mistral",
                SupportsToolUse = true,
                SupportsVision = false,
                SupportsStreaming = true,
                MaxContextTokens = 32_000,
                MaxOutputTokens = 8_192,
                SupportsJsonMode = true,
                SupportsParallelToolCalls = true,
                Features = new List<string> { "tool_use", "function_calling", "json_mode", "multilingual" }
            };
        }

        // Cohere Command R
        if (name.Contains("command"))
        {
            return new ModelCapabilities
            {
                ModelName = modelName,
                Provider = "Cohere",
                SupportsToolUse = true,
                SupportsVision = false,
                SupportsStreaming = true,
                MaxContextTokens = 128_000,
                MaxOutputTokens = 4_000,
                SupportsJsonMode = false,
                SupportsParallelToolCalls = true,
                Features = new List<string> { "tool_use", "rag", "grounding", "citations", "multilingual" }
            };
        }

        // Llama models
        if (name.Contains("llama"))
        {
            return new ModelCapabilities
            {
                ModelName = modelName,
                Provider = "Meta",
                SupportsToolUse = name.Contains("3.1") || name.Contains("3.2"),  // Only newer versions
                SupportsVision = name.Contains("vision"),
                SupportsStreaming = true,
                MaxContextTokens = 128_000,
                MaxOutputTokens = 2_048,
                SupportsJsonMode = false,
                SupportsParallelToolCalls = false,
                Features = new List<string> { "open_source", "tool_use" }
            };
        }

        // Generic fallback
        _logger.LogWarning("Unknown model {Model}, using generic capabilities", modelName);
        return new ModelCapabilities
        {
            ModelName = modelName,
            Provider = "Unknown",
            SupportsToolUse = false,
            SupportsVision = false,
            SupportsStreaming = true,
            MaxContextTokens = 8_192,
            MaxOutputTokens = 2_048,
            SupportsJsonMode = false,
            SupportsParallelToolCalls = false,
            Features = new List<string>()
        };
    }

    /// <summary>
    /// Checks if a model supports a specific feature.
    /// </summary>
    public bool SupportsFeature(string modelName, string feature)
    {
        var capabilities = GetCapabilities(modelName);

        return feature.ToLowerInvariant() switch
        {
            "tool_use" or "tools" or "function_calling" => capabilities.SupportsToolUse,
            "vision" or "image" or "multimodal" => capabilities.SupportsVision,
            "streaming" or "stream" => capabilities.SupportsStreaming,
            "json" or "json_mode" => capabilities.SupportsJsonMode,
            "parallel_tools" or "parallel" => capabilities.SupportsParallelToolCalls,
            _ => capabilities.Features?.Contains(feature, StringComparer.OrdinalIgnoreCase) ?? false
        };
    }

    /// <summary>
    /// Checks if a model can handle a specific context size.
    /// </summary>
    public bool CanHandleContextSize(string modelName, int tokenCount)
    {
        var capabilities = GetCapabilities(modelName);
        return tokenCount <= capabilities.MaxContextTokens;
    }

    /// <summary>
    /// Selects the best model from a list based on required features.
    /// </summary>
    public string? SelectBestModel(
        IEnumerable<string> availableModels,
        ModelRequirements requirements)
    {
        var models = availableModels.ToList();

        if (!models.Any())
        {
            _logger.LogWarning("No models available for selection");
            return null;
        }

        // Filter models that meet requirements
        var suitableModels = models.Where(m =>
        {
            var capabilities = GetCapabilities(m);

            // Check required features
            if (requirements.RequireToolUse && !capabilities.SupportsToolUse)
                return false;
            if (requirements.RequireVision && !capabilities.SupportsVision)
                return false;
            if (requirements.RequireStreaming && !capabilities.SupportsStreaming)
                return false;
            if (requirements.RequireJsonMode && !capabilities.SupportsJsonMode)
                return false;

            // Check context size
            if (requirements.MinContextTokens.HasValue 
                && capabilities.MaxContextTokens < requirements.MinContextTokens.Value)
                return false;

            return true;
        }).ToList();

        if (!suitableModels.Any())
        {
            _logger.LogWarning("No models meet the specified requirements");
            return null;
        }

        // If only one suitable model, return it
        if (suitableModels.Count == 1)
            return suitableModels[0];

        // Select based on priority (prefer newer, more capable models)
        // TODO: Add more sophisticated selection logic (cost, latency, quality)
        var selected = suitableModels.First();

        _logger.LogInformation("Selected model {Model} from {Count} suitable models",
            selected, suitableModels.Count);

        return selected;
    }

    /// <summary>
    /// Gets a summary of capabilities for all known models.
    /// </summary>
    public Dictionary<string, ModelCapabilities> GetAllCapabilities()
    {
        return new Dictionary<string, ModelCapabilities>(_capabilityCache);
    }

    /// <summary>
    /// Clears the capability cache.
    /// </summary>
    public void ClearCache()
    {
        _capabilityCache.Clear();
        _logger.LogInformation("Cleared capability cache");
    }

    /// <summary>
    /// Validates if a model is compatible with required features.
    /// </summary>
    public ValidationResult ValidateModel(string modelName, ModelRequirements requirements)
    {
        var capabilities = GetCapabilities(modelName);
        var errors = new List<string>();

        if (requirements.RequireToolUse && !capabilities.SupportsToolUse)
            errors.Add("Model does not support tool use");

        if (requirements.RequireVision && !capabilities.SupportsVision)
            errors.Add("Model does not support vision");

        if (requirements.RequireStreaming && !capabilities.SupportsStreaming)
            errors.Add("Model does not support streaming");

        if (requirements.RequireJsonMode && !capabilities.SupportsJsonMode)
            errors.Add("Model does not support JSON mode");

        if (requirements.MinContextTokens.HasValue 
            && capabilities.MaxContextTokens < requirements.MinContextTokens.Value)
            errors.Add($"Model context window ({capabilities.MaxContextTokens}) is smaller than required ({requirements.MinContextTokens.Value})");

        return new ValidationResult
        {
            IsValid = !errors.Any(),
            Errors = errors,
            ModelName = modelName,
            Capabilities = capabilities
        };
    }
}

/// <summary>
/// Requirements for model selection.
/// </summary>
public record ModelRequirements
{
    public bool RequireToolUse { get; init; }
    public bool RequireVision { get; init; }
    public bool RequireStreaming { get; init; }
    public bool RequireJsonMode { get; init; }
    public int? MinContextTokens { get; init; }
    public string[]? RequiredFeatures { get; init; }
}

/// <summary>
/// Result of model validation.
/// </summary>
public record ValidationResult
{
    public required bool IsValid { get; init; }
    public required List<string> Errors { get; init; }
    public required string ModelName { get; init; }
    public required ModelCapabilities Capabilities { get; init; }
}
