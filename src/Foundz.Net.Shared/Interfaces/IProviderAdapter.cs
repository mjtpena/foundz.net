using Foundz.Net.Shared.Models;

namespace Foundz.Net.Shared.Interfaces;

/// <summary>
/// Interface for provider-specific adapters that normalize different AI provider formats.
/// </summary>
public interface IProviderAdapter
{
    /// <summary>
    /// The provider name this adapter supports.
    /// </summary>
    string ProviderName { get; }
    
    /// <summary>
    /// Formats messages to the provider's format.
    /// </summary>
    object FormatMessages(List<Message> messages);
    
    /// <summary>
    /// Formats tool definitions to the provider's format.
    /// </summary>
    object FormatTools(List<ToolDefinition> tools);
    
    /// <summary>
    /// Parses the provider's response to a unified format.
    /// </summary>
    AIResponse ParseResponse(object providerResponse);
    
    /// <summary>
    /// Parses a streaming chunk to a unified format.
    /// </summary>
    AIResponseChunk ParseStreamChunk(object providerChunk);
    
    /// <summary>
    /// Gets the capabilities of this provider.
    /// </summary>
    ModelCapabilities GetCapabilities(string modelName);
    
    /// <summary>
    /// Validates if a model is compatible with this adapter.
    /// </summary>
    bool ValidateModel(string modelName);
}
