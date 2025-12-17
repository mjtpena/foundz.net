using Foundz.Net.Shared.Interfaces;
using Foundz.Net.Shared.Models;
using LibGit2Sharp;
using System.Diagnostics;
using System.Text;

namespace Foundz.Net.Tools.Git;

/// <summary>
/// Tool for viewing git commit history.
/// </summary>
public class GitLogTool : ITool
{
    public string Name => "git_log";
    public string Description => "View git commit history with optional filtering";
    public ToolCategory Category => ToolCategory.GitOperations;
    public bool RequiresConfirmation => false;
    public DangerLevel DangerLevel => DangerLevel.Safe;

    public string ParametersSchema => """
        {
            "type": "object",
            "properties": {
                "path": {
                    "type": "string",
                    "description": "Path to git repository (default: current directory)"
                },
                "max_count": {
                    "type": "integer",
                    "description": "Maximum number of commits to show (default: 10)"
                },
                "author": {
                    "type": "string",
                    "description": "Filter by author name or email"
                },
                "since": {
                    "type": "string",
                    "description": "Show commits since date (ISO format)"
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
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var repoPath = args.ContainsKey("path") ? args["path"].ToString()! : Directory.GetCurrentDirectory();
            var maxCount = args.ContainsKey("max_count") ? Convert.ToInt32(args["max_count"]) : 10;
            var author = args.ContainsKey("author") ? args["author"].ToString() : null;
            var since = args.ContainsKey("since") ? args["since"].ToString() : null;

            var repoRoot = Repository.Discover(repoPath);
            if (repoRoot == null)
            {
                return Task.FromResult(new ToolResult
                {
                    ToolCallId = string.Empty,
                    ToolName = Name,
                    Success = false,
                    Error = "Not a git repository",
                    ExecutionTimeMs = (int)stopwatch.ElapsedMilliseconds
                });
            }

            using var repo = new Repository(repoRoot);
            var output = new StringBuilder();

            output.AppendLine($"Commit History ({repo.Head.FriendlyName}):");
            output.AppendLine();

            var commits = repo.Commits.QueryBy(new CommitFilter
            {
                SortBy = CommitSortStrategies.Time
            });

            // Apply filters
            IEnumerable<Commit> filteredCommits = commits;

            if (!string.IsNullOrEmpty(author))
            {
                filteredCommits = filteredCommits.Where(c => 
                    c.Author.Name.Contains(author, StringComparison.OrdinalIgnoreCase) ||
                    c.Author.Email.Contains(author, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrEmpty(since) && DateTime.TryParse(since, out var sinceDate))
            {
                filteredCommits = filteredCommits.Where(c => c.Author.When >= sinceDate);
            }

            var displayCommits = filteredCommits.Take(maxCount).ToList();

            foreach (var commit in displayCommits)
            {
                output.AppendLine($"commit {commit.Sha}");
                output.AppendLine($"Author: {commit.Author.Name} <{commit.Author.Email}>");
                output.AppendLine($"Date:   {commit.Author.When:yyyy-MM-dd HH:mm:ss}");
                output.AppendLine();
                
                // Format commit message with indentation
                var messageLines = commit.Message.Split('\n');
                foreach (var line in messageLines)
                {
                    output.AppendLine($"    {line}");
                }
                
                output.AppendLine();
            }

            if (!displayCommits.Any())
            {
                output.AppendLine("No commits found matching the criteria.");
            }
            else if (filteredCommits.Count() > maxCount)
            {
                output.AppendLine($"... and {filteredCommits.Count() - maxCount} more commits");
            }

            stopwatch.Stop();

            return Task.FromResult(new ToolResult
            {
                ToolCallId = string.Empty,
                ToolName = Name,
                Success = true,
                Output = output.ToString(),
                ExecutionTimeMs = (int)stopwatch.ElapsedMilliseconds
            });
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return Task.FromResult(new ToolResult
            {
                ToolCallId = string.Empty,
                ToolName = Name,
                Success = false,
                Error = $"Error getting log: {ex.Message}",
                ExecutionTimeMs = (int)stopwatch.ElapsedMilliseconds
            });
        }
    }

    public IEnumerable<string> GetExamples()
    {
        return new[]
        {
            """git_log({})""",
            """git_log({"max_count": 5})""",
            """git_log({"author": "john.doe", "max_count": 20})"""
        };
    }
}
