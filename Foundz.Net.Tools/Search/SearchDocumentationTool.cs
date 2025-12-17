using Foundz.Net.Shared.Interfaces;
using Foundz.Net.Shared.Models;
using System.Text;

namespace Foundz.Net.Tools.Search;

/// <summary>
/// Tool for searching project documentation.
/// </summary>
public class SearchDocumentationTool : ITool
{
    public string Name => "search_documentation";
    public string Description => "Search README, documentation files, and wikis for information.";
    public ToolCategory Category => ToolCategory.Search;
    public bool RequiresConfirmation => false;
    public DangerLevel DangerLevel => DangerLevel.Safe;

    public string ParametersSchema => """
        {
            "type": "object",
            "properties": {
                "query": {
                    "type": "string",
                    "description": "Search query"
                },
                "directory": {
                    "type": "string",
                    "description": "Root directory to search (defaults to current directory)"
                },
                "maxResults": {
                    "type": "integer",
                    "description": "Maximum number of results to return",
                    "default": 20
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
            var maxResults = args.ContainsKey("maxResults") ? Convert.ToInt32(args["maxResults"]) : 20;

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

            // Search patterns for documentation files
            var docPatterns = new[] { "*.md", "*.txt", "*.rst", "*.adoc" };
            var docDirs = new[] { "docs", "doc", "documentation", "wiki", "." };
            
            var results = new List<DocResult>();

            foreach (var docDir in docDirs)
            {
                var searchPath = Path.Combine(directory!, docDir);
                if (!Directory.Exists(searchPath))
                    continue;

                foreach (var pattern in docPatterns)
                {
                    var files = Directory.GetFiles(searchPath, pattern, SearchOption.AllDirectories);
                    
                    foreach (var file in files)
                    {
                        if (results.Count >= maxResults)
                            break;

                        try
                        {
                            var content = await System.IO.File.ReadAllTextAsync(file, cancellationToken);
                            var lines = content.Split('\n');
                            
                            for (int i = 0; i < lines.Length; i++)
                            {
                                if (lines[i].Contains(query, StringComparison.OrdinalIgnoreCase))
                                {
                                    // Extract surrounding context
                                    var contextStart = Math.Max(0, i - 2);
                                    var contextEnd = Math.Min(lines.Length - 1, i + 2);
                                    
                                    var context = string.Join("\n", lines[contextStart..(contextEnd + 1)]);
                                    
                                    results.Add(new DocResult
                                    {
                                        File = file,
                                        LineNumber = i + 1,
                                        Context = context,
                                        Score = CalculateRelevance(lines[i], query)
                                    });

                                    if (results.Count >= maxResults)
                                        break;
                                }
                            }
                        }
                        catch
                        {
                            // Skip files that can't be read
                            continue;
                        }
                    }
                }
            }

            // Sort by relevance
            results = [.. results.OrderByDescending(r => r.Score)];

            var output = new StringBuilder();
            output.AppendLine($"Documentation Search for: '{query}'");
            output.AppendLine($"Results found: {results.Count}");
            output.AppendLine();

            if (results.Any())
            {
                foreach (var result in results.Take(10))
                {
                    output.AppendLine($"📚 {Path.GetRelativePath(directory!, result.File)}:{result.LineNumber}");
                    output.AppendLine($"   Relevance: {result.Score:P0}");
                    output.AppendLine();
                    
                    var contextLines = result.Context.Split('\n');
                    foreach (var line in contextLines)
                    {
                        output.AppendLine($"   {line}");
                    }
                    
                    output.AppendLine();
                }

                if (results.Count > 10)
                {
                    output.AppendLine($"... and {results.Count - 10} more results");
                }
            }
            else
            {
                output.AppendLine("No documentation found matching the query.");
                output.AppendLine();
                output.AppendLine("Tip: Try different search terms or check if documentation exists in the project.");
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
                    ["resultsFound"] = results.Count
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

    private static double CalculateRelevance(string text, string query)
    {
        var lowerText = text.ToLower();
        var lowerQuery = query.ToLower();
        
        // Exact match gets highest score
        if (lowerText.Contains(lowerQuery))
            return 1.0;
        
        // Word match
        var queryWords = lowerQuery.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var matchedWords = queryWords.Count(w => lowerText.Contains(w));
        
        return (double)matchedWords / queryWords.Length;
    }

    public IEnumerable<string> GetExamples()
    {
        return
        [
            "search_documentation({\"query\": \"installation\"})",
            "search_documentation({\"query\": \"API authentication\", \"maxResults\": 10})"
        ];
    }

    private record DocResult
    {
        public required string File { get; init; }
        public required int LineNumber { get; init; }
        public required string Context { get; init; }
        public required double Score { get; init; }
    }
}
