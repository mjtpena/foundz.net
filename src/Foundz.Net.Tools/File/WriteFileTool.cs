using Foundz.Net.Shared.Interfaces;
using Foundz.Net.Shared.Models;

namespace Foundz.Net.Tools.File;

/// <summary>
/// Tool for writing content to a file.
/// </summary>
public class WriteFileTool : ITool
{
    public string Name => "write_file";
    public string Description => "Write content to a file. Creates the file if it doesn't exist, overwrites if it does.";
    public ToolCategory Category => ToolCategory.FileOperations;
    public bool RequiresConfirmation => true;
    public DangerLevel DangerLevel => DangerLevel.Warning;

    public string ParametersSchema => """
        {
            "type": "object",
            "properties": {
                "path": {
                    "type": "string",
                    "description": "Path to the file to write"
                },
                "content": {
                    "type": "string",
                    "description": "Content to write to the file"
                }
            },
            "required": ["path", "content"]
        }
        """;

    public Task<bool> ValidateArgsAsync(Dictionary<string, object> args, CancellationToken cancellationToken = default)
    {
        if (!args.ContainsKey("path") || args["path"] is not string path || string.IsNullOrWhiteSpace(path))
        {
            return Task.FromResult(false);
        }

        if (!args.ContainsKey("content"))
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
            var content = args["content"].ToString() ?? string.Empty;

            // Ensure directory exists
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Backup existing file
            string? backupPath = null;
            if (System.IO.File.Exists(path))
            {
                backupPath = $"{path}.backup.{DateTime.UtcNow:yyyyMMddHHmmss}";
                System.IO.File.Copy(path, backupPath);
            }

            await System.IO.File.WriteAllTextAsync(path, content, cancellationToken);

            var fileInfo = new FileInfo(path);

            return new ToolResult
            {
                ToolCallId = string.Empty,
                ToolName = Name,
                Success = true,
                Output = $"File written successfully: {path} ({fileInfo.Length} bytes)" + 
                         (backupPath != null ? $"\nBackup created: {backupPath}" : ""),
                ExecutionTimeMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds,
                Metadata = new Dictionary<string, object>
                {
                    ["fileSize"] = fileInfo.Length,
                    ["backupPath"] = backupPath ?? string.Empty
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
            "write_file({\"path\": \"test.txt\", \"content\": \"Hello, World!\"})",
            "write_file({\"path\": \"src/NewClass.cs\", \"content\": \"public class NewClass { }\"})"
        ];
    }
}
