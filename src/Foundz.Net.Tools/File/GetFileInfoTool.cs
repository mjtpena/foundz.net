using Foundz.Net.Shared.Interfaces;
using Foundz.Net.Shared.Models;
using System.Text;

namespace Foundz.Net.Tools.File;

/// <summary>
/// Tool for getting detailed file information.
/// </summary>
public class GetFileInfoTool : ITool
{
    public string Name => "get_file_info";
    public string Description => "Get detailed metadata about a file including size, dates, permissions, and language detection.";
    public ToolCategory Category => ToolCategory.FileOperations;
    public bool RequiresConfirmation => false;
    public DangerLevel DangerLevel => DangerLevel.Safe;

    public string ParametersSchema => """
        {
            "type": "object",
            "properties": {
                "path": {
                    "type": "string",
                    "description": "Path to the file"
                }
            },
            "required": ["path"]
        }
        """;

    public Task<bool> ValidateArgsAsync(Dictionary<string, object> args, CancellationToken cancellationToken = default)
    {
        if (!args.ContainsKey("path") || args["path"] is not string path || string.IsNullOrWhiteSpace(path))
            return Task.FromResult(false);

        return Task.FromResult(true);
    }

    public async Task<ToolResult> ExecuteAsync(Dictionary<string, object> args, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            var path = args["path"].ToString()!;

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
            var language = DetectLanguage(path);
            var lineCount = 0;

            if (!IsBinaryFile(path))
            {
                try
                {
                    var lines = await System.IO.File.ReadAllLinesAsync(path, cancellationToken);
                    lineCount = lines.Length;
                }
                catch
                {
                    // If we can't read it, it's probably binary
                    lineCount = -1;
                }
            }

            var output = new StringBuilder();
            output.AppendLine($"File: {fileInfo.Name}");
            output.AppendLine($"Full Path: {fileInfo.FullName}");
            output.AppendLine($"Size: {FormatBytes(fileInfo.Length)}");
            output.AppendLine($"Created: {fileInfo.CreationTime:yyyy-MM-dd HH:mm:ss}");
            output.AppendLine($"Modified: {fileInfo.LastWriteTime:yyyy-MM-dd HH:mm:ss}");
            output.AppendLine($"Accessed: {fileInfo.LastAccessTime:yyyy-MM-dd HH:mm:ss}");
            output.AppendLine($"Extension: {fileInfo.Extension}");
            output.AppendLine($"Language: {language}");
            output.AppendLine($"Is Binary: {IsBinaryFile(path)}");
            output.AppendLine($"Is ReadOnly: {fileInfo.IsReadOnly}");
            
            if (lineCount >= 0)
            {
                output.AppendLine($"Lines of Code: {lineCount}");
            }

            return new ToolResult
            {
                ToolCallId = string.Empty,
                ToolName = Name,
                Success = true,
                Output = output.ToString(),
                ExecutionTimeMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds,
                Metadata = new Dictionary<string, object>
                {
                    ["path"] = fileInfo.FullName,
                    ["size"] = fileInfo.Length,
                    ["extension"] = fileInfo.Extension,
                    ["language"] = language,
                    ["lineCount"] = lineCount,
                    ["isBinary"] = IsBinaryFile(path),
                    ["isReadOnly"] = fileInfo.IsReadOnly
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

    private static string DetectLanguage(string path)
    {
        var extension = Path.GetExtension(path).ToLowerInvariant();
        
        return extension switch
        {
            ".cs" => "C#",
            ".vb" => "Visual Basic",
            ".fs" => "F#",
            ".js" => "JavaScript",
            ".ts" => "TypeScript",
            ".jsx" => "React JSX",
            ".tsx" => "React TSX",
            ".py" => "Python",
            ".java" => "Java",
            ".cpp" or ".cc" or ".cxx" => "C++",
            ".c" => "C",
            ".h" or ".hpp" => "C/C++ Header",
            ".go" => "Go",
            ".rs" => "Rust",
            ".rb" => "Ruby",
            ".php" => "PHP",
            ".swift" => "Swift",
            ".kt" => "Kotlin",
            ".scala" => "Scala",
            ".sql" => "SQL",
            ".html" or ".htm" => "HTML",
            ".css" => "CSS",
            ".scss" or ".sass" => "Sass",
            ".json" => "JSON",
            ".xml" => "XML",
            ".yaml" or ".yml" => "YAML",
            ".md" => "Markdown",
            ".sh" => "Shell Script",
            ".ps1" => "PowerShell",
            ".bat" or ".cmd" => "Batch",
            _ => "Unknown"
        };
    }

    private static bool IsBinaryFile(string path)
    {
        var binaryExtensions = new[] { ".exe", ".dll", ".bin", ".dat", ".db", ".sqlite", ".zip", ".tar", ".gz", ".jpg", ".png", ".gif", ".pdf", ".ico", ".ttf", ".woff", ".woff2" };
        return binaryExtensions.Any(ext => path.EndsWith(ext, StringComparison.OrdinalIgnoreCase));
    }

    private static string FormatBytes(long bytes)
    {
        string[] sizes = ["B", "KB", "MB", "GB", "TB"];
        double len = bytes;
        int order = 0;
        
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }

        return $"{len:0.##} {sizes[order]}";
    }

    public IEnumerable<string> GetExamples()
    {
        return
        [
            "get_file_info({\"path\": \"Program.cs\"})",
            "get_file_info({\"path\": \"README.md\"})"
        ];
    }
}
