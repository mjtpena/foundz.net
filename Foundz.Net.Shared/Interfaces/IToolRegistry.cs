namespace Foundz.Net.Shared.Interfaces;

/// <summary>
/// Interface for tool registry.
/// </summary>
public interface IToolRegistry
{
    /// <summary>
    /// Registers a tool.
    /// </summary>
    void RegisterTool(ITool tool);
    
    /// <summary>
    /// Gets a tool by name.
    /// </summary>
    ITool? GetTool(string name);
    
    /// <summary>
    /// Lists all registered tools.
    /// </summary>
    IEnumerable<ITool> ListTools(ToolCategory? category = null);
    
    /// <summary>
    /// Unregisters a tool.
    /// </summary>
    bool UnregisterTool(string name);
    
    /// <summary>
    /// Gets tool schemas for AI consumption.
    /// </summary>
    List<ToolDefinition> GetToolSchemas();
    
    /// <summary>
    /// Validates a tool implementation.
    /// </summary>
    Task<bool> ValidateToolAsync(ITool tool, CancellationToken cancellationToken = default);
}
