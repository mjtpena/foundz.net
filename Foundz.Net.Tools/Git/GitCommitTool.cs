using Foundz.Net.Shared.Interfaces;
using Foundz.Net.Shared.Models;
using LibGit2Sharp;
using System.Diagnostics;

namespace Foundz.Net.Tools.Git;

/// <summary>
/// Tool for creating git commits.
/// </summary>
public class GitCommitTool : ITool
{
    public string Name => "git_commit";
    public string Description => "Create a git commit with staged changes";
    public ToolCategory Category => ToolCategory.GitOperations;
    public bool RequiresConfirmation => true;
    public DangerLevel DangerLevel => DangerLevel.Warning;

    public string ParametersSchema => """
        {
            "type": "object",
            "properties": {
                "path": {
                    "type": "string",
                    "description": "Path to git repository (default: current directory)"
                },
                "message": {
                    "type": "string",
                    "description": "Commit message"
                },
                "author_name": {
                    "type": "string",
                    "description": "Author name (optional, uses git config)"
                },
                "author_email": {
                    "type": "string",
                    "description": "Author email (optional, uses git config)"
                }
            },
            "required": ["message"]
        }
        """;

    public Task<bool> ValidateArgsAsync(Dictionary<string, object> args, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(args.ContainsKey("message"));
    }

    public Task<ToolResult> ExecuteAsync(Dictionary<string, object> args, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var repoPath = args.ContainsKey("path") ? args["path"].ToString()! : Directory.GetCurrentDirectory();
            var message = args["message"].ToString()!;

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

            // Check if there are staged changes
            var status = repo.RetrieveStatus();
            if (!status.Staged.Any())
            {
                return Task.FromResult(new ToolResult
                {
                    ToolCallId = string.Empty,
                    ToolName = Name,
                    Success = false,
                    Error = "No staged changes to commit. Use git_add first.",
                    ExecutionTimeMs = (int)stopwatch.ElapsedMilliseconds
                });
            }

            // Get author from args or config
            var authorName = args.ContainsKey("author_name") 
                ? args["author_name"].ToString()! 
                : repo.Config.Get<string>("user.name")?.Value ?? "Foundz Agent";
            
            var authorEmail = args.ContainsKey("author_email") 
                ? args["author_email"].ToString()! 
                : repo.Config.Get<string>("user.email")?.Value ?? "agent@foundz.net";

            var author = new Signature(authorName, authorEmail, DateTimeOffset.Now);
            var committer = author;

            // Create commit
            var commit = repo.Commit(message, author, committer);

            stopwatch.Stop();

            return Task.FromResult(new ToolResult
            {
                ToolCallId = string.Empty,
                ToolName = Name,
                Success = true,
                Output = $"Created commit {commit.Sha.Substring(0, 7)}: {message}\n" +
                         $"Author: {author.Name} <{author.Email}>\n" +
                         $"Files: {status.Staged.Count()}",
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
                Error = $"Error creating commit: {ex.Message}",
                ExecutionTimeMs = (int)stopwatch.ElapsedMilliseconds
            });
        }
    }

    public IEnumerable<string> GetExamples()
    {
        return new[]
        {
            """git_commit({"message": "feat: add new feature"})""",
            """git_commit({"message": "fix: resolve bug", "author_name": "John Doe", "author_email": "john@example.com"})"""
        };
    }
}
