using Foundz.Net.Shared.Interfaces;
using Foundz.Net.Shared.Models;
using System.Text;
using System.Text.RegularExpressions;

namespace Foundz.Net.Tools.Search;

/// <summary>
/// Tool for full-text search across the codebase.
/// </summary>
public class SearchCodebaseTool : ITool
{
    public string Name => "search_codebase";
    public string Description => "Search for text or regex patterns across the entire codebase with context.";
    public ToolCategory Category => ToolCategory.Search;
    public bool RequiresConfirmation => false;
    public DangerLevel DangerLevel => DangerLevel.Safe;

    public string ParametersSchema => """
        {
            "type": "object",
            "properties": {
                "query": {
                    "type": "string",
                    "description": "Search query or regex pattern"
                },
                "directory": {
                    "type": "string",
                    "description": "Directory to search in (defaults to current directory)"
                },
                "filePattern": {
                    "type": "string",
                    "description": "File pattern to match (e.g., '*.cs', '*.js')",
                    "default": "*.*"
                },
                "useRegex": {
                    "type": "boolean",
                    "description": "Treat query as regex pattern",
                    "default": false
                },
                "caseSensitive": {
                    "type": "boolean",
                    "description": "Case sensitive search",
                    "default": false
                },
                "maxResults": {
                    "type": "integer",
                    "description": "Maximum number of results to return",
                    "default": 50
                },
                "contextLines": {
                    "type": "integer",
                    "description": "Number of context lines before and after match",
                    "default": 2
                }
            },
            "required": ["query"]
        }
        """;

    public Task<bool> ValidateArgsAsync(Dictionary<string, object> args, CancellationToken cancellationToken = default)
    {
        if (!args.ContainsKey("query") || args["query"] is not string query || string.IsNullOrWhiteSpace(query))
            return Task.FromResult(false);

        return Task.FromResult(true);
    }

    public async Task<ToolResult> ExecuteAsync(Dictionary<string, object> args, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            var query = args["query"].ToString()!;
            var directory = args.ContainsKey("directory") ? args["directory"].ToString() : Directory.GetCurrentDirectory();
            var filePattern = args.ContainsKey("filePattern") ? args["filePattern"].ToString() : "*.*";
            var useRegex = args.ContainsKey("useRegex") && Convert.ToBoolean(args["useRegex"]);
            var caseSensitive = args.ContainsKey("caseSensitive") && Convert.ToBoolean(args["caseSensitive"]);
            var maxResults = args.ContainsKey("maxResults") ? Convert.ToInt32(args["maxResults"]) : 50;
            var contextLines = args.ContainsKey("contextLines") ? Convert.ToInt32(args["contextLines"]) : 2;

            if (!Directory.Exists(directory))
            {
                return new ToolResult
                {
                    ToolCallId = string.Empty,
                    ToolName = Name,
                    Success = false,
                    Error = $"Directory not found: {directory}",
                    ExecutionTimeMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds
                };
            }

            var files = Directory.GetFiles(directory!, filePattern!, SearchOption.AllDirectories);
            var results = new List<SearchResult>();

            Regex? regex = null;
            if (useRegex)
            {
                var options = caseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase;
                regex = new Regex(query, options);
            }

            var comparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

            foreach (var file in files)
            {
                if (results.Count >= maxResults)
                    break;

                try
                {
                    var lines = await System.IO.File.ReadAllLinesAsync(file, cancellationToken);
                    
                    for (int i = 0; i < lines.Length && results.Count < maxResults; i++)
                    {
                        var line = lines[i];
                        var isMatch = useRegex 
                            ? regex!.IsMatch(line) 
                            : line.Contains(query, comparison);

                        if (isMatch)
                        {
                            var contextStart = Math.Max(0, i - contextLines);
                            var contextEnd = Math.Min(lines.Length - 1, i + contextLines);
                            
                            var context = new List<string>();
                            for (int j = contextStart; j <= contextEnd; j++)
                            {
                                var prefix = j == i ? ">" : " ";
                                context.Add($"{prefix} {j + 1:D4} | {lines[j]}");
                            }

                            results.Add(new SearchResult
                            {
                                File = file,
                                LineNumber = i + 1,
                                Line = line,
                                Context = context
                            });
                        }
                    }
                }
                catch
                {
                    // Skip files that can't be read (binary, permissions, etc.)
                    continue;
                }
            }

            var output = new StringBuilder();
            output.AppendLine($"Search Results for: '{query}'");
            output.AppendLine($"Directory: {directory}");
            output.AppendLine($"Files searched: {files.Length}");
            output.AppendLine($"Matches found: {results.Count}");
            output.AppendLine();

            if (results.Any())
            {
                foreach (var result in results)
                {
                    output.AppendLine($"📄 {Path.GetRelativePath(directory!, result.File)}:{result.LineNumber}");
                    foreach (var contextLine in result.Context)
                    {
                        output.AppendLine($"  {contextLine}");
                    }
                    output.AppendLine();
                }

                if (results.Count >= maxResults)
                {
                    output.AppendLine($"(showing first {maxResults} results, there may be more)");
                }
            }
            else
            {
                output.AppendLine("No matches found.");
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
                    ["query"] = query,
                    ["filesSearched"] = files.Length,
                    ["matchesFound"] = results.Count
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
            "search_codebase({\"query\": \"TODO\"})",
            "search_codebase({\"query\": \"class.*Repository\", \"useRegex\": true, \"filePattern\": \"*.cs\"})",
            "search_codebase({\"query\": \"async Task\", \"directory\": \"./src\", \"maxResults\": 20})"
        ];
    }

    private record SearchResult
    {
        public required string File { get; init; }
        public required int LineNumber { get; init; }
        public required string Line { get; init; }
        public required List<string> Context { get; init; }
    }
}
