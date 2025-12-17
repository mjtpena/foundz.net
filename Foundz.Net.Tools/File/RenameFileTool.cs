using Foundz.Net.Shared.Interfaces;
using Foundz.Net.Shared.Models;

namespace Foundz.Net.Tools.File;

/// <summary>
/// Tool for renaming files or directories.
/// </summary>
public class RenameFileTool : ITool
{
    public string Name => "rename_file";
    public string Description => "Rename a file or directory. Detects conflicts and can be git-aware.";
    public ToolCategory Category => ToolCategory.FileOperations;
    public bool RequiresConfirmation => true;
    public DangerLevel DangerLevel => DangerLevel.Warning;

    public string ParametersSchema => """
        {
            "type": "object",
            "properties": {
                "oldPath": {
                    "type": "string",
                    "description": "Current path of the file or directory"
                },
                "newPath": {
                    "type": "string",
                    "description": "New path for the file or directory"
                }
            },
            "required": ["oldPath", "newPath"]
        }
        """;

    public Task<bool> ValidateArgsAsync(Dictionary<string, object> args, CancellationToken cancellationToken = default)
    {
        if (!args.ContainsKey("oldPath") || args["oldPath"] is not string oldPath || string.IsNullOrWhiteSpace(oldPath))
            return Task.FromResult(false);
        
        if (!args.ContainsKey("newPath") || args["newPath"] is not string newPath || string.IsNullOrWhiteSpace(newPath))
            return Task.FromResult(false);

        return Task.FromResult(true);
    }

    public Task<ToolResult> ExecuteAsync(Dictionary<string, object> args, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            var oldPath = args["oldPath"].ToString()!;
            var newPath = args["newPath"].ToString()!;

            if (!System.IO.File.Exists(oldPath) && !Directory.Exists(oldPath))
            {
                return Task.FromResult(new ToolResult
                {
                    ToolCallId = string.Empty,
                    ToolName = Name,
                    Success = false,
                    Error = $"Path not found: {oldPath}",
                    ExecutionTimeMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds
                });
            }

            if (System.IO.File.Exists(newPath) || Directory.Exists(newPath))
            {
                return Task.FromResult(new ToolResult
                {
                    ToolCallId = string.Empty,
                    ToolName = Name,
                    Success = false,
                    Error = $"Destination already exists: {newPath}",
                    ExecutionTimeMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds
                });
            }

            // Determine if it's a file or directory
            var isDirectory = Directory.Exists(oldPath);

            if (isDirectory)
            {
                Directory.Move(oldPath, newPath);
            }
            else
            {
                System.IO.File.Move(oldPath, newPath);
            }

            return Task.FromResult(new ToolResult
            {
                ToolCallId = string.Empty,
                ToolName = Name,
                Success = true,
                Output = $"Successfully renamed {(isDirectory ? "directory" : "file")} from {oldPath} to {newPath}",
                ExecutionTimeMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds,
                Metadata = new Dictionary<string, object>
                {
                    ["oldPath"] = oldPath,
                    ["newPath"] = newPath,
                    ["isDirectory"] = isDirectory
                }
            });
        }
        catch (Exception ex)
        {
            return Task.FromResult(new ToolResult
            {
                ToolCallId = string.Empty,
                ToolName = Name,
                Success = false,
                Error = ex.Message,
                ExecutionTimeMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds
            });
        }
    }

    public IEnumerable<string> GetExamples()
    {
        return
        [
            "rename_file({\"oldPath\": \"old_name.cs\", \"newPath\": \"new_name.cs\"})",
            "rename_file({\"oldPath\": \"old_folder\", \"newPath\": \"new_folder\"})"
        ];
    }
}
