using System.Collections.Concurrent;
using System.Reflection;
using Foundz.Net.Shared.Interfaces;
using Microsoft.Extensions.Logging;

namespace Foundz.Net.Core.ToolRegistry;

/// <summary>
/// Production-grade tool registry with thread-safe registration and auto-discovery.
/// </summary>
public class ToolRegistry : IToolRegistry
{
    private readonly ConcurrentDictionary<string, ITool> _tools = new();
    private readonly ILogger<ToolRegistry> _logger;

    public ToolRegistry(ILogger<ToolRegistry> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Registers a tool in the registry.
    /// </summary>
    public void RegisterTool(ITool tool)
    {
        if (tool == null)
            throw new ArgumentNullException(nameof(tool));

        if (string.IsNullOrWhiteSpace(tool.Name))
            throw new ArgumentException("Tool name cannot be empty.", nameof(tool));

        if (_tools.TryAdd(tool.Name, tool))
        {
            _logger.LogInformation("Registered tool: {ToolName} (Category: {Category}, Danger: {DangerLevel})", 
                tool.Name, tool.Category, tool.DangerLevel);
        }
        else
        {
            _logger.LogWarning("Tool {ToolName} is already registered. Skipping.", tool.Name);
        }
    }

    /// <summary>
    /// Gets a tool by name.
    /// </summary>
    public ITool? GetTool(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        _tools.TryGetValue(name, out var tool);
        return tool;
    }

    /// <summary>
    /// Lists all registered tools, optionally filtered by category.
    /// </summary>
    public IEnumerable<ITool> ListTools(ToolCategory? category = null)
    {
        var tools = _tools.Values;
        
        if (category.HasValue)
            tools = tools.Where(t => t.Category == category.Value).ToList();

        return tools.OrderBy(t => t.Category).ThenBy(t => t.Name);
    }

    /// <summary>
    /// Unregisters a tool by name.
    /// </summary>
    public bool UnregisterTool(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;

        if (_tools.TryRemove(name, out var tool))
        {
            _logger.LogInformation("Unregistered tool: {ToolName}", name);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Gets tool schemas in AI-compatible format.
    /// </summary>
    public List<ToolDefinition> GetToolSchemas()
    {
        var schemas = new List<ToolDefinition>();

        foreach (var tool in _tools.Values)
        {
            try
            {
                var parameters = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(
                    tool.ParametersSchema) ?? new Dictionary<string, object>();

                schemas.Add(new ToolDefinition
                {
                    Name = tool.Name,
                    Description = tool.Description,
                    Parameters = parameters
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse schema for tool: {ToolName}", tool.Name);
            }
        }

        return schemas;
    }

    /// <summary>
    /// Validates a tool implementation.
    /// </summary>
    public async Task<bool> ValidateToolAsync(ITool tool, CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate basic properties
            if (string.IsNullOrWhiteSpace(tool.Name))
            {
                _logger.LogError("Tool validation failed: Name is empty");
                return false;
            }

            if (string.IsNullOrWhiteSpace(tool.Description))
            {
                _logger.LogError("Tool {ToolName} validation failed: Description is empty", tool.Name);
                return false;
            }

            if (string.IsNullOrWhiteSpace(tool.ParametersSchema))
            {
                _logger.LogError("Tool {ToolName} validation failed: ParametersSchema is empty", tool.Name);
                return false;
            }

            // Validate JSON schema is valid
            try
            {
                System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(tool.ParametersSchema);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Tool {ToolName} validation failed: Invalid JSON schema", tool.Name);
                return false;
            }

            // Test with empty args (should either succeed or fail gracefully)
            var testArgs = new Dictionary<string, object>();
            var isValid = await tool.ValidateArgsAsync(testArgs, cancellationToken);

            _logger.LogDebug("Tool {ToolName} validation completed: {IsValid}", tool.Name, isValid);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Tool {ToolName} validation threw exception", tool.Name);
            return false;
        }
    }

    /// <summary>
    /// Auto-discovers and registers all tools from the specified assembly.
    /// </summary>
    public void DiscoverAndRegisterTools(Assembly assembly)
    {
        _logger.LogInformation("Discovering tools in assembly: {AssemblyName}", assembly.GetName().Name);

        var toolTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(ITool).IsAssignableFrom(t));

        int registered = 0;
        foreach (var toolType in toolTypes)
        {
            try
            {
                // Try to create instance (requires parameterless constructor)
                if (Activator.CreateInstance(toolType) is ITool tool)
                {
                    RegisterTool(tool);
                    registered++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to instantiate tool: {ToolType}", toolType.Name);
            }
        }

        _logger.LogInformation("Discovered and registered {Count} tools", registered);
    }

    /// <summary>
    /// Gets count of registered tools.
    /// </summary>
    public int Count => _tools.Count;

    /// <summary>
    /// Clears all registered tools.
    /// </summary>
    public void Clear()
    {
        _tools.Clear();
        _logger.LogInformation("Cleared all registered tools");
    }
}
