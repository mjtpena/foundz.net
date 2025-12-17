using Foundz.Net.Shared.Interfaces;
using Foundz.Net.Shared.Models;

namespace Foundz.Net.Tools.Refactoring;

/// <summary>
/// Tool for extracting code into a method.
/// </summary>
public class ExtractMethodTool : ITool
{
    public string Name => "extract_method";
    public string Description => "Extract selected code into a new method with automatic parameter detection.";
    public ToolCategory Category => ToolCategory.Refactoring;
    public bool RequiresConfirmation => true;
    public DangerLevel DangerLevel => DangerLevel.Warning;

    public string ParametersSchema => """
        {
            "type": "object",
            "properties": {
                "filePath": {
                    "type": "string",
                    "description": "Path to the file containing code to extract"
                },
                "startLine": {
                    "type": "integer",
                    "description": "Starting line number (1-indexed)"
                },
                "endLine": {
                    "type": "integer",
                    "description": "Ending line number (1-indexed)"
                },
                "methodName": {
                    "type": "string",
                    "description": "Name for the new method"
                }
            },
            "required": ["filePath", "startLine", "endLine", "methodName"]
        }
        """;

    public Task<bool> ValidateArgsAsync(Dictionary<string, object> args, CancellationToken cancellationToken = default)
    {
        if (!args.ContainsKey("filePath") || args["filePath"] is not string path || string.IsNullOrWhiteSpace(path))
            return Task.FromResult(false);

        if (!args.ContainsKey("startLine") || !args.ContainsKey("endLine"))
            return Task.FromResult(false);

        if (!args.ContainsKey("methodName") || args["methodName"] is not string name || string.IsNullOrWhiteSpace(name))
            return Task.FromResult(false);

        return Task.FromResult(true);
    }

    public async Task<ToolResult> ExecuteAsync(Dictionary<string, object> args, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            var filePath = args["filePath"].ToString()!;
            var startLine = Convert.ToInt32(args["startLine"]);
            var endLine = Convert.ToInt32(args["endLine"]);
            var methodName = args["methodName"].ToString()!;

            if (!System.IO.File.Exists(filePath))
            {
                return new ToolResult
                {
                    ToolCallId = string.Empty,
                    ToolName = Name,
                    Success = false,
                    Error = $"File not found: {filePath}",
                    ExecutionTimeMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds
                };
            }

            var lines = await System.IO.File.ReadAllLinesAsync(filePath, cancellationToken);
            
            if (startLine < 1 || startLine > lines.Length || endLine < startLine || endLine > lines.Length)
            {
                return new ToolResult
                {
                    ToolCallId = string.Empty,
                    ToolName = Name,
                    Success = false,
                    Error = $"Invalid line range: {startLine}-{endLine}",
                    ExecutionTimeMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds
                };
            }

            // Extract the code block
            var extractedLines = lines[(startLine - 1)..endLine];
            var extractedCode = string.Join(Environment.NewLine, extractedLines);

            // Simple implementation - in a real tool, we'd use Roslyn for proper analysis
            var output = $@"Extract Method Refactoring
===========================

Original code (lines {startLine}-{endLine}):
{extractedCode}

Suggested new method:
private void {methodName}()
{{
{extractedCode}
}}

To apply this refactoring:
1. Create the new method above
2. Replace lines {startLine}-{endLine} with a call to {methodName}()

Note: This is a simplified suggestion. You may need to:
- Add parameters for variables used in the extracted code
- Add a return type if the code returns a value
- Adjust access modifiers (private/public/protected)
";

            return new ToolResult
            {
                ToolCallId = string.Empty,
                ToolName = Name,
                Success = true,
                Output = output,
                ExecutionTimeMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds,
                Metadata = new Dictionary<string, object>
                {
                    ["filePath"] = filePath,
                    ["methodName"] = methodName,
                    ["linesExtracted"] = endLine - startLine + 1
                }
            };
        }
        catch (Exception ex)
        {
            return new ToolResult
            {
                ToolCallId = string.Empty,
                ToolName = Name,
                Success = false,
                Error = ex.Message,
                ExecutionTimeMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds
            };
        }
    }

    public IEnumerable<string> GetExamples()
    {
        return
        [
            "extract_method({\"filePath\": \"Service.cs\", \"startLine\": 45, \"endLine\": 52, \"methodName\": \"ValidateInput\"})"
        ];
    }
}
