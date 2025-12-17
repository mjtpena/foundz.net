using Foundz.Net.Shared.Interfaces;
using Foundz.Net.Shared.Models;
using System.Text;

namespace Foundz.Net.Tools.File;

/// <summary>
/// Tool for listing directory contents.
/// </summary>
public class ListDirectoryTool : ITool
{
    public string Name => "list_directory";
    public string Description => "List the contents of a directory, including files and subdirectories.";
    public ToolCategory Category => ToolCategory.FileOperations;
    public bool RequiresConfirmation => false;
    public DangerLevel DangerLevel => DangerLevel.Safe;

    public string ParametersSchema => """
        {
            "type": "object",
            "properties": {
                "path": {
                    "type": "string",
                    "description": "Path to the directory to list (defaults to current directory)"
                },
                "recursive": {
                    "type": "boolean",
                    "description": "Whether to list subdirectories recursively"
                }
            }
        }
        """;

    public Task<bool> ValidateArgsAsync(Dictionary<string, object> args, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    public Task<ToolResult> ExecuteAsync(Dictionary<string, object> args, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            var path = args.ContainsKey("path") ? args["path"].ToString() : ".";
            var recursive = args.ContainsKey("recursive") && Convert.ToBoolean(args["recursive"]);

            if (string.IsNullOrWhiteSpace(path))
                path = ".";

            if (!Directory.Exists(path))
            {
                return Task.FromResult(new ToolResult
                {
                    ToolCallId = string.Empty,
                    ToolName = Name,
                    Success = false,
                    Error = $"Directory not found: {path}",
                    ExecutionTimeMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds
                });
            }

            var output = new StringBuilder();
            output.AppendLine($"Directory: {Path.GetFullPath(path)}");
            output.AppendLine();

            var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            
            // List directories
            var directories = Directory.GetDirectories(path, "*", searchOption);
            output.AppendLine($"Directories ({directories.Length}):");
            foreach (var dir in directories.Take(100))
            {
                var dirInfo = new DirectoryInfo(dir);
                output.AppendLine($"  📁 {Path.GetRelativePath(path, dir)}");
            }
            if (directories.Length > 100)
            {
                output.AppendLine($"  ... and {directories.Length - 100} more");
            }

            output.AppendLine();

            // List files
            var files = Directory.GetFiles(path, "*", searchOption);
            output.AppendLine($"Files ({files.Length}):");
            foreach (var file in files.Take(100))
            {
                var fileInfo = new FileInfo(file);
                output.AppendLine($"  📄 {Path.GetRelativePath(path, file)} ({FormatFileSize(fileInfo.Length)})");
            }
            if (files.Length > 100)
            {
                output.AppendLine($"  ... and {files.Length - 100} more");
            }

            return Task.FromResult(new ToolResult
            {
                ToolCallId = string.Empty,
                ToolName = Name,
                Success = true,
                Output = output.ToString(),
                ExecutionTimeMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds,
                Metadata = new Dictionary<string, object>
                {
                    ["directoryCount"] = directories.Length,
                    ["fileCount"] = files.Length
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
            "list_directory({\"path\": \".\"})",
            "list_directory({\"path\": \"src\", \"recursive\": true})"
        ];
    }

    private static string FormatFileSize(long bytes)
    {
        string[] sizes = ["B", "KB", "MB", "GB"];
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}
