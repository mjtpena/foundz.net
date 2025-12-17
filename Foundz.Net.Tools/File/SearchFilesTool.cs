using Foundz.Net.Shared.Interfaces;
using Foundz.Net.Shared.Models;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace Foundz.Net.Tools.File;

/// <summary>
/// Tool for searching files by pattern and content.
/// </summary>
public class SearchFilesTool : ITool
{
    public string Name => "search_files";
    public string Description => "Search for files by name pattern and optionally by content using regex";
    public ToolCategory Category => ToolCategory.FileOperations;
    public bool RequiresConfirmation => false;
    public DangerLevel DangerLevel => DangerLevel.Safe;

    public string ParametersSchema => """
        {
            "type": "object",
            "properties": {
                "path": {
                    "type": "string",
                    "description": "Root directory to search (default: current directory)"
                },
                "pattern": {
                    "type": "string",
                    "description": "File name pattern (e.g., *.cs, test*.txt)"
                },
                "content_regex": {
                    "type": "string",
                    "description": "Optional regex pattern to search within file contents"
                },
                "max_results": {
                    "type": "integer",
                    "description": "Maximum number of results to return (default: 50)"
                }
            },
            "required": ["pattern"]
        }
        """;

    public Task<bool> ValidateArgsAsync(Dictionary<string, object> args, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(args.ContainsKey("pattern"));
    }

    public async Task<ToolResult> ExecuteAsync(Dictionary<string, object> args, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var rootPath = args.ContainsKey("path") ? args["path"].ToString()! : Directory.GetCurrentDirectory();
            var pattern = args["pattern"].ToString()!;
            var contentRegex = args.ContainsKey("content_regex") ? args["content_regex"].ToString() : null;
            var maxResults = args.ContainsKey("max_results") ? Convert.ToInt32(args["max_results"]) : 50;

            if (!Directory.Exists(rootPath))
            {
                return new ToolResult
                {
                    ToolCallId = string.Empty,
                    ToolName = Name,
                    Success = false,
                    Error = $"Directory not found: {rootPath}",
                    ExecutionTimeMs = (int)stopwatch.ElapsedMilliseconds
                };
            }

            var results = new List<string>();
            var files = Directory.GetFiles(rootPath, pattern, SearchOption.AllDirectories);

            foreach (var file in files.Take(maxResults))
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                if (string.IsNullOrEmpty(contentRegex))
                {
                    // Just list matching files
                    results.Add(file);
                }
                else
                {
                    // Search content
                    try
                    {
                        var content = await System.IO.File.ReadAllTextAsync(file, cancellationToken);
                        var regex = new Regex(contentRegex, RegexOptions.Multiline);
                        var matches = regex.Matches(content);

                        if (matches.Count > 0)
                        {
                            results.Add($"{file} ({matches.Count} matches)");
                        }
                    }
                    catch
                    {
                        // Skip files that can't be read (binary, permissions, etc.)
                    }
                }
            }

            stopwatch.Stop();

            var output = new StringBuilder();
            output.AppendLine($"Found {results.Count} matching file(s):");
            foreach (var result in results)
            {
                output.AppendLine($"  - {result}");
            }

            return new ToolResult
            {
                ToolCallId = string.Empty,
                ToolName = Name,
                Success = true,
                Output = output.ToString(),
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
                Error = $"Error searching files: {ex.Message}",
                ExecutionTimeMs = (int)stopwatch.ElapsedMilliseconds
            };
        }
    }

    public IEnumerable<string> GetExamples()
    {
        return new[]
        {
            """search_files({"pattern": "*.cs"})""",
            """search_files({"path": "/src", "pattern": "*.txt", "content_regex": "TODO"})"""
        };
    }
}
