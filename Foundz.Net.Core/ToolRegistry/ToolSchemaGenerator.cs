using System.Text.Json;
using Foundz.Net.Shared.Interfaces;
using Microsoft.Extensions.Logging;

namespace Foundz.Net.Core.ToolRegistry;

/// <summary>
/// Generates tool schemas in different formats for various AI providers.
/// </summary>
public class ToolSchemaGenerator
{
    private readonly ILogger<ToolSchemaGenerator> _logger;

    public ToolSchemaGenerator(ILogger<ToolSchemaGenerator> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Converts a list of tools to OpenAI function calling format.
    /// </summary>
    public List<object> ConvertToOpenAIFormat(List<ToolDefinition> tools)
    {
        var functions = new List<object>();

        foreach (var tool in tools)
        {
            try
            {
                var function = new
                {
                    type = "function",
                    function = new
                    {
                        name = tool.Name,
                        description = tool.Description,
                        parameters = tool.Parameters
                    }
                };

                functions.Add(function);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to convert tool {ToolName} to OpenAI format", tool.Name);
            }
        }

        return functions;
    }

    /// <summary>
    /// Converts a list of tools to Anthropic tool use format.
    /// </summary>
    public List<object> ConvertToAnthropicFormat(List<ToolDefinition> tools)
    {
        var anthropicTools = new List<object>();

        foreach (var tool in tools)
        {
            try
            {
                var anthropicTool = new
                {
                    name = tool.Name,
                    description = tool.Description,
                    input_schema = tool.Parameters
                };

                anthropicTools.Add(anthropicTool);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to convert tool {ToolName} to Anthropic format", tool.Name);
            }
        }

        return anthropicTools;
    }

    /// <summary>
    /// Validates a JSON schema.
    /// </summary>
    public bool ValidateSchema(string schemaJson)
    {
        try
        {
            var schema = JsonSerializer.Deserialize<Dictionary<string, object>>(schemaJson);
            
            // Basic validation - should have "type" and "properties"
            if (schema == null)
                return false;

            if (!schema.ContainsKey("type"))
            {
                _logger.LogWarning("Schema missing 'type' field");
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Schema validation failed");
            return false;
        }
    }

    /// <summary>
    /// Generates human-readable documentation from schema.
    /// </summary>
    public string GenerateDocumentation(ToolDefinition tool)
    {
        var doc = new System.Text.StringBuilder();
        
        doc.AppendLine($"## {tool.Name}");
        doc.AppendLine();
        doc.AppendLine(tool.Description);
        doc.AppendLine();
        doc.AppendLine("### Parameters");
        doc.AppendLine();

        try
        {
            if (tool.Parameters.ContainsKey("properties"))
            {
                var properties = tool.Parameters["properties"] as Dictionary<string, object>;
                if (properties != null)
                {
                    foreach (var prop in properties)
                    {
                        doc.AppendLine($"- **{prop.Key}**: {GetPropertyDescription(prop.Value)}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate documentation for tool {ToolName}", tool.Name);
        }

        return doc.ToString();
    }

    /// <summary>
    /// Extracts property description from schema property.
    /// </summary>
    private string GetPropertyDescription(object propertyValue)
    {
        try
        {
            if (propertyValue is JsonElement element)
            {
                if (element.TryGetProperty("description", out var desc))
                    return desc.GetString() ?? "No description";
                
                if (element.TryGetProperty("type", out var type))
                    return $"Type: {type.GetString()}";
            }

            return "No description available";
        }
        catch
        {
            return "No description available";
        }
    }

    /// <summary>
    /// Generates a sample request for a tool.
    /// </summary>
    public string GenerateSampleRequest(ToolDefinition tool)
    {
        var sample = new
        {
            tool_call_id = "call_123",
            name = tool.Name,
            arguments = GenerateSampleArguments(tool.Parameters)
        };

        return JsonSerializer.Serialize(sample, new JsonSerializerOptions 
        { 
            WriteIndented = true 
        });
    }

    /// <summary>
    /// Generates sample arguments based on schema.
    /// </summary>
    private Dictionary<string, object> GenerateSampleArguments(Dictionary<string, object> parameters)
    {
        var args = new Dictionary<string, object>();

        try
        {
            if (parameters.ContainsKey("properties"))
            {
                var properties = parameters["properties"] as Dictionary<string, object>;
                if (properties != null)
                {
                    foreach (var prop in properties)
                    {
                        args[prop.Key] = GetSampleValue(prop.Value);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate sample arguments");
        }

        return args;
    }

    /// <summary>
    /// Gets a sample value based on property type.
    /// </summary>
    private object GetSampleValue(object propertyValue)
    {
        try
        {
            if (propertyValue is JsonElement element)
            {
                if (element.TryGetProperty("type", out var type))
                {
                    var typeStr = type.GetString();
                    return typeStr switch
                    {
                        "string" => "example_string",
                        "number" => 42,
                        "integer" => 42,
                        "boolean" => true,
                        "array" => new[] { "item1", "item2" },
                        "object" => new { key = "value" },
                        _ => "unknown"
                    };
                }
            }

            return "example_value";
        }
        catch
        {
            return "example_value";
        }
    }
}
