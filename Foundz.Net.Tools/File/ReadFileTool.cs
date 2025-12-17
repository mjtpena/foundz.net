using Foundz.Net.Shared.Interfaces;
using Foundz.Net.Shared.Models;
using System.Text;

namespace Foundz.Net.Tools.File;

/// <summary>
/// Tool for reading file contents.
/// </summary>
public class ReadFileTool : ITool
{
    public string Name => "read_file";
    public string Description => "Read the contents of a file. Returns the full file content or specific line ranges.";
    public ToolCategory Category => ToolCategory.FileOperations;
    public bool RequiresConfirmation => false;
    public DangerLevel DangerLevel => DangerLevel.Safe;

    public string ParametersSchema => """
        {
            "type": "object",
            "properties": {
                "path": {
                    "type": "string",
                    "description": "Path to the file to read"
                },
                "startLine": {
                    "type": "integer",
                    "description": "Optional starting line number (1-indexed)"
                },
                "endLine": {
                    "type": "integer",
                    "description": "Optional ending line number (1-indexed)"
                }
            },
            "required": ["path"]
        }
        """;

    public Task<bool> ValidateArgsAsync(Dictionary<string, object> args, CancellationToken cancellationToken = default)
    {
        if (!args.ContainsKey("path") || args["path"] is not string path || string.IsNullOrWhiteSpace(path))
        {
            return Task.FromResult(false);
        }

        return Task.FromResult(true);
    }

    public async Task<ToolResult> ExecuteAsync(Dictionary<string, object> args, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            var path = args["path"].ToString()!;
            var startLine = args.ContainsKey("startLine") ? Convert.ToInt32(args["startLine"]) : (int?)null;
            var endLine = args.ContainsKey("endLine") ? Convert.ToInt32(args["endLine"]) : (int?)null;

            if (!System.IO.File.Exists(path))
            {
                return new ToolResult
                {
                    ToolCallId = string.Empty,
                    ToolName = Name,
                    Success = false,
                    Error = $"File not found: {path}",
                    ExecutionTimeMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds
                };
            }

            var fileInfo = new FileInfo(path);
            
            // Check for binary files
            if (IsBinaryFile(path))
            {
                return new ToolResult
                {
                    ToolCallId = string.Empty,
                    ToolName = Name,
                    Success = false,
                    Error = "Cannot read binary file",
                    ExecutionTimeMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds
                };
            }

            var lines = await System.IO.File.ReadAllLinesAsync(path, cancellationToken);
            
            if (startLine.HasValue || endLine.HasValue)
            {
                var start = Math.Max(0, (startLine ?? 1) - 1);
                var end = Math.Min(lines.Length, endLine ?? lines.Length);
                lines = lines.Skip(start).Take(end - start).ToArray();
            }

            var content = new StringBuilder();
            content.AppendLine($"File: {path}");
            content.AppendLine($"Size: {fileInfo.Length} bytes");
            content.AppendLine($"Lines: {lines.Length}");
            content.AppendLine();
            
            for (int i = 0; i < lines.Length; i++)
            {
                var lineNumber = (startLine ?? 1) + i;
                content.AppendLine($"{lineNumber,4} | {lines[i]}");
            }

            return new ToolResult
            {
                ToolCallId = string.Empty,
                ToolName = Name,
                Success = true,
                Output = content.ToString(),
                ExecutionTimeMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds,
                Metadata = new Dictionary<string, object>
                {
                    ["fileSize"] = fileInfo.Length,
                    ["lineCount"] = lines.Length
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
            "read_file({\"path\": \"Program.cs\"})",
            "read_file({\"path\": \"README.md\", \"startLine\": 1, \"endLine\": 10})"
        ];
    }

    private static bool IsBinaryFile(string path)
    {
        var binaryExtensions = new[] { ".exe", ".dll", ".bin", ".dat", ".db", ".sqlite", ".zip", ".tar", ".gz", ".jpg", ".png", ".gif", ".pdf" };
        return binaryExtensions.Any(ext => path.EndsWith(ext, StringComparison.OrdinalIgnoreCase));
    }
}
