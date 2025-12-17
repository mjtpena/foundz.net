using Foundz.Net.Shared.Interfaces;
using Foundz.Net.Shared.Models;
using System.Diagnostics;

namespace Foundz.Net.Tools.File;

/// <summary>
/// Tool for deleting files or directories.
/// </summary>
public class DeleteFileTool : ITool
{
    public string Name => "delete_file";
    public string Description => "Delete a file or directory (with confirmation for safety)";
    public ToolCategory Category => ToolCategory.FileOperations;
    public bool RequiresConfirmation => true;
    public DangerLevel DangerLevel => DangerLevel.Danger;

    public string ParametersSchema => """
        {
            "type": "object",
            "properties": {
                "path": {
                    "type": "string",
                    "description": "Path to the file or directory to delete"
                },
                "recursive": {
                    "type": "boolean",
                    "description": "If true, delete directories recursively (default: false)"
                }
            },
            "required": ["path"]
        }
        """;

    public Task<bool> ValidateArgsAsync(Dictionary<string, object> args, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(args.ContainsKey("path"));
    }

    public Task<ToolResult> ExecuteAsync(Dictionary<string, object> args, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var path = args["path"].ToString()!;
            var recursive = args.ContainsKey("recursive") && Convert.ToBoolean(args["recursive"]);

            if (System.IO.File.Exists(path))
            {
                System.IO.File.Delete(path);
                stopwatch.Stop();

                return Task.FromResult(new ToolResult
                {
                    ToolCallId = string.Empty,
                    ToolName = Name,
                    Success = true,
                    Output = $"Deleted file: {path}",
                    ExecutionTimeMs = (int)stopwatch.ElapsedMilliseconds
                });
            }
            else if (Directory.Exists(path))
            {
                if (recursive)
                {
                    Directory.Delete(path, recursive: true);
                    stopwatch.Stop();

                    return Task.FromResult(new ToolResult
                    {
                        ToolCallId = string.Empty,
                        ToolName = Name,
                        Success = true,
                        Output = $"Deleted directory recursively: {path}",
                        ExecutionTimeMs = (int)stopwatch.ElapsedMilliseconds
                    });
                }
                else
                {
                    Directory.Delete(path, recursive: false);
                    stopwatch.Stop();

                    return Task.FromResult(new ToolResult
                    {
                        ToolCallId = string.Empty,
                        ToolName = Name,
                        Success = true,
                        Output = $"Deleted empty directory: {path}",
                        ExecutionTimeMs = (int)stopwatch.ElapsedMilliseconds
                    });
                }
            }
            else
            {
                stopwatch.Stop();
                return Task.FromResult(new ToolResult
                {
                    ToolCallId = string.Empty,
                    ToolName = Name,
                    Success = false,
                    Error = $"Path not found: {path}",
                    ExecutionTimeMs = (int)stopwatch.ElapsedMilliseconds
                });
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return Task.FromResult(new ToolResult
            {
                ToolCallId = string.Empty,
                ToolName = Name,
                Success = false,
                Error = $"Error deleting: {ex.Message}",
                ExecutionTimeMs = (int)stopwatch.ElapsedMilliseconds
            });
        }
    }

    public IEnumerable<string> GetExamples()
    {
        return new[]
        {
            """delete_file({"path": "temp.txt"})""",
            """delete_file({"path": "old_dir", "recursive": true})"""
        };
    }
}
