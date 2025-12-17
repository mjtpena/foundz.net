using Foundz.Net.Shared.Interfaces;
using Foundz.Net.Shared.Models;
using System.Diagnostics;

namespace Foundz.Net.Tools.File;

/// <summary>
/// Tool for editing specific lines in a file.
/// </summary>
public class EditFileTool : ITool
{
    public string Name => "edit_file";
    public string Description => "Edit specific lines in a file by replacing content between start and end line numbers";
    public ToolCategory Category => ToolCategory.FileOperations;
    public bool RequiresConfirmation => true;
    public DangerLevel DangerLevel => DangerLevel.Warning;

    public string ParametersSchema => """
        {
            "type": "object",
            "properties": {
                "path": {
                    "type": "string",
                    "description": "Path to the file to edit"
                },
                "start_line": {
                    "type": "integer",
                    "description": "Starting line number (1-indexed)"
                },
                "end_line": {
                    "type": "integer",
                    "description": "Ending line number (1-indexed, inclusive)"
                },
                "new_content": {
                    "type": "string",
                    "description": "New content to replace the lines"
                }
            },
            "required": ["path", "start_line", "end_line", "new_content"]
        }
        """;

    public Task<bool> ValidateArgsAsync(Dictionary<string, object> args, CancellationToken cancellationToken = default)
    {
        var hasPath = args.ContainsKey("path");
        var hasStartLine = args.ContainsKey("start_line");
        var hasEndLine = args.ContainsKey("end_line");
        var hasNewContent = args.ContainsKey("new_content");

        return Task.FromResult(hasPath && hasStartLine && hasEndLine && hasNewContent);
    }

    public async Task<ToolResult> ExecuteAsync(Dictionary<string, object> args, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var path = args["path"].ToString()!;
            var startLine = Convert.ToInt32(args["start_line"]);
            var endLine = Convert.ToInt32(args["end_line"]);
            var newContent = args["new_content"].ToString()!;

            if (!System.IO.File.Exists(path))
            {
                return new ToolResult
                {
                    ToolCallId = string.Empty,
                    ToolName = Name,
                    Success = false,
                    Error = $"File not found: {path}",
                    ExecutionTimeMs = (int)stopwatch.ElapsedMilliseconds
                };
            }

            // Validate line numbers
            if (startLine < 1 || endLine < startLine)
            {
                return new ToolResult
                {
                    ToolCallId = string.Empty,
                    ToolName = Name,
                    Success = false,
                    Error = "Invalid line numbers: start_line must be >= 1 and end_line >= start_line",
                    ExecutionTimeMs = (int)stopwatch.ElapsedMilliseconds
                };
            }

            // Read all lines
            var lines = await System.IO.File.ReadAllLinesAsync(path, cancellationToken);

            if (endLine > lines.Length)
            {
                return new ToolResult
                {
                    ToolCallId = string.Empty,
                    ToolName = Name,
                    Success = false,
                    Error = $"end_line ({endLine}) exceeds file length ({lines.Length} lines)",
                    ExecutionTimeMs = (int)stopwatch.ElapsedMilliseconds
                };
            }

            // Create backup
            var backupPath = $"{path}.backup";
            await System.IO.File.WriteAllLinesAsync(backupPath, lines, cancellationToken);

            // Build new content
            var newLines = new List<string>();
            
            // Add lines before edit
            newLines.AddRange(lines.Take(startLine - 1));
            
            // Add new content (split by newlines)
            newLines.AddRange(newContent.Split('\n'));
            
            // Add lines after edit
            newLines.AddRange(lines.Skip(endLine));

            // Write updated file
            await System.IO.File.WriteAllLinesAsync(path, newLines, cancellationToken);

            stopwatch.Stop();

            return new ToolResult
            {
                ToolCallId = string.Empty,
                ToolName = Name,
                Success = true,
                Output = $"Edited {path}: replaced lines {startLine}-{endLine} with {newContent.Split('\n').Length} new lines. Backup saved to {backupPath}",
                ExecutionTimeMs = (int)stopwatch.ElapsedMilliseconds
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return new ToolResult
            {
                ToolCallId = string.Empty,
                ToolName = Name,
                Success = false,
                Error = $"Error editing file: {ex.Message}",
                ExecutionTimeMs = (int)stopwatch.ElapsedMilliseconds
            };
        }
    }

    public IEnumerable<string> GetExamples()
    {
        return new[]
        {
            """edit_file({"path": "test.txt", "start_line": 5, "end_line": 10, "new_content": "new line 5\nnew line 6"})"""
        };
    }
}
