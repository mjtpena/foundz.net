using Foundz.Net.Shared.Interfaces;
using Foundz.Net.Shared.Models;
using LibGit2Sharp;
using System.Text;

namespace Foundz.Net.Tools.Git;

/// <summary>
/// Tool for git stash operations.
/// </summary>
public class GitStashTool : ITool
{
    public string Name => "git_stash";
    public string Description => "Stash, list, apply, pop, or drop stashed changes.";
    public ToolCategory Category => ToolCategory.GitOperations;
    public bool RequiresConfirmation => false;
    public DangerLevel DangerLevel => DangerLevel.Safe;

    public string ParametersSchema => """
        {
            "type": "object",
            "properties": {
                "operation": {
                    "type": "string",
                    "description": "Operation to perform: save, list, apply, pop, drop",
                    "enum": ["save", "list", "apply", "pop", "drop"],
                    "default": "save"
                },
                "message": {
                    "type": "string",
                    "description": "Message for the stash (for save operation)"
                },
                "index": {
                    "type": "integer",
                    "description": "Stash index (for apply, pop, drop operations)",
                    "default": 0
                },
                "includeUntracked": {
                    "type": "boolean",
                    "description": "Include untracked files in stash",
                    "default": false
                },
                "repositoryPath": {
                    "type": "string",
                    "description": "Path to the git repository (defaults to current directory)"
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
            var operation = args.ContainsKey("operation") ? args["operation"].ToString()?.ToLower() : "save";
            var message = args.ContainsKey("message") ? args["message"].ToString() : null;
            var index = args.ContainsKey("index") ? Convert.ToInt32(args["index"]) : 0;
            var includeUntracked = args.ContainsKey("includeUntracked") && Convert.ToBoolean(args["includeUntracked"]);
            var repoPath = args.ContainsKey("repositoryPath") ? args["repositoryPath"].ToString() : Directory.GetCurrentDirectory();

            using var repo = new Repository(repoPath);

            switch (operation)
            {
                case "save":
                    {
                        var signature = repo.Config.BuildSignature(DateTimeOffset.Now);
                        var stash = repo.Stashes.Add(signature, message, 
                            includeUntracked ? StashModifiers.IncludeUntracked : StashModifiers.Default);

                        return Task.FromResult(new ToolResult
                        {
                            ToolCallId = string.Empty,
                            ToolName = Name,
                            Success = true,
                            Output = $"Created stash: {stash.Message}",
                            ExecutionTimeMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds
                        });
                    }

                case "list":
                    {
                        var output = new StringBuilder();
                        output.AppendLine("Stashes:");
                        
                        var stashIndex = 0;
                        foreach (var stash in repo.Stashes)
                        {
                            output.AppendLine($"  [{stashIndex}] {stash.Message}");
                            stashIndex++;
                        }

                        if (stashIndex == 0)
                        {
                            output.AppendLine("  (no stashes found)");
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
                                ["count"] = stashIndex
                            }
                        });
                    }

                case "apply":
                    {
                        var stash = repo.Stashes.ElementAtOrDefault(index);
                        if (stash == null)
                        {
                            return Task.FromResult(new ToolResult
                            {
                                ToolCallId = string.Empty,
                                ToolName = Name,
                                Success = false,
                                Error = $"Stash not found at index {index}",
                                ExecutionTimeMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds
                            });
                        }

                        var result = repo.Stashes.Apply(index);

                        return Task.FromResult(new ToolResult
                        {
                            ToolCallId = string.Empty,
                            ToolName = Name,
                            Success = true,
                            Output = $"Applied stash {index}: {stash.Message}\nStatus: {result}",
                            ExecutionTimeMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds
                        });
                    }

                case "pop":
                    {
                        var stash = repo.Stashes.ElementAtOrDefault(index);
                        if (stash == null)
                        {
                            return Task.FromResult(new ToolResult
                            {
                                ToolCallId = string.Empty,
                                ToolName = Name,
                                Success = false,
                                Error = $"Stash not found at index {index}",
                                ExecutionTimeMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds
                            });
                        }

                        var stashMessage = stash.Message;
                        var result = repo.Stashes.Pop(index);

                        return Task.FromResult(new ToolResult
                        {
                            ToolCallId = string.Empty,
                            ToolName = Name,
                            Success = true,
                            Output = $"Popped stash {index}: {stashMessage}\nStatus: {result}",
                            ExecutionTimeMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds
                        });
                    }

                case "drop":
                    {
                        var stash = repo.Stashes.ElementAtOrDefault(index);
                        if (stash == null)
                        {
                            return Task.FromResult(new ToolResult
                            {
                                ToolCallId = string.Empty,
                                ToolName = Name,
                                Success = false,
                                Error = $"Stash not found at index {index}",
                                ExecutionTimeMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds
                            });
                        }

                        var stashMessage = stash.Message;
                        repo.Stashes.Remove(index);

                        return Task.FromResult(new ToolResult
                        {
                            ToolCallId = string.Empty,
                            ToolName = Name,
                            Success = true,
                            Output = $"Dropped stash {index}: {stashMessage}",
                            ExecutionTimeMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds
                        });
                    }

                default:
                    return Task.FromResult(new ToolResult
                    {
                        ToolCallId = string.Empty,
                        ToolName = Name,
                        Success = false,
                        Error = $"Unknown operation: {operation}",
                        ExecutionTimeMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds
                    });
            }
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
            "git_stash({\"operation\": \"save\", \"message\": \"WIP: feature\"})",
            "git_stash({\"operation\": \"list\"})",
            "git_stash({\"operation\": \"pop\", \"index\": 0})"
        ];
    }
}
