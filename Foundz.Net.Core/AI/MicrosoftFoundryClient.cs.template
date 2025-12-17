using Azure;
using Azure.AI.Projects;
using Azure.Core;
using Foundz.Net.Shared.Interfaces;
using Foundz.Net.Shared.Models;
using Microsoft.Extensions.Logging;

namespace Foundz.Net.Core.AI;

/// <summary>
/// Microsoft Foundry (Azure AI Foundry) client implementation using the official SDK.
/// This replaces the legacy Azure.AI.Inference with the official Azure.AI.Projects package.
/// </summary>
public class MicrosoftFoundryClient : IAIClient
{
    private readonly ILogger<MicrosoftFoundryClient> _logger;
    private readonly AIProjectClient _projectClient;
    private readonly string _modelName;
    private readonly string _deploymentName;
    private readonly IProviderAdapter? _providerAdapter;

    public MicrosoftFoundryClient(
        string connectionString,
        string modelName,
        string deploymentName,
        IProviderAdapter? providerAdapter,
        ILogger<MicrosoftFoundryClient> logger)
    {
        _logger = logger;
        _modelName = modelName;
        _deploymentName = deploymentName;
        _providerAdapter = providerAdapter;

        // Initialize Microsoft Foundry Project Client
        _projectClient = new AIProjectClient(connectionString, new DefaultAzureCredential());
        
        _logger.LogInformation(
            "Initialized Microsoft Foundry client for model {Model} (deployment: {Deployment})",
            modelName,
            deploymentName);
    }

    public MicrosoftFoundryClient(
        string endpoint,
        string subscriptionId,
        string resourceGroupName,
        string projectName,
        string modelName,
        string deploymentName,
        IProviderAdapter? providerAdapter,
        ILogger<MicrosoftFoundryClient> logger)
    {
        _logger = logger;
        _modelName = modelName;
        _deploymentName = deploymentName;
        _providerAdapter = providerAdapter;

        // Initialize with endpoint and credentials
        var credential = new DefaultAzureCredential();
        _projectClient = new AIProjectClient(
            new Uri(endpoint),
            subscriptionId,
            resourceGroupName,
            projectName,
            credential);

        _logger.LogInformation(
            "Initialized Microsoft Foundry client for project {Project} in {ResourceGroup}",
            projectName,
            resourceGroupName);
    }

    public async Task<AIResponse> SendMessageAsync(
        List<Message> messages,
        List<ToolDefinition>? tools = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Sending message to Microsoft Foundry model {Model} with {MessageCount} messages",
                _modelName,
                messages.Count);

            // Use provider adapter if available for model-specific formatting
            if (_providerAdapter != null)
            {
                return await _providerAdapter.SendMessageAsync(messages, tools, cancellationToken);
            }

            // Otherwise use Microsoft Foundry SDK directly
            var inferenceClient = _projectClient.GetInferenceClient();
            
            // Convert messages to SDK format
            var chatMessages = ConvertMessages(messages);
            
            // Create chat completion options
            var options = new ChatCompletionsOptions
            {
                DeploymentName = _deploymentName,
                Messages = chatMessages,
                MaxTokens = 4096,
                Temperature = 0.7f
            };

            // Add tools if provided
            if (tools != null && tools.Any())
            {
                foreach (var tool in tools)
                {
                    options.Tools.Add(ConvertToolDefinition(tool));
                }
            }

            // Call Microsoft Foundry API
            var response = await inferenceClient.GetChatCompletionsAsync(options, cancellationToken);
            
            // Convert response to our model
            return ConvertResponse(response.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Microsoft Foundry API");
            throw;
        }
    }

    public async IAsyncEnumerable<AIResponseChunk> StreamMessageAsync(
        List<Message> messages,
        List<ToolDefinition>? tools = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Streaming message to Microsoft Foundry model {Model}",
            _modelName);

        // Use provider adapter if available
        if (_providerAdapter != null)
        {
            await foreach (var chunk in _providerAdapter.StreamMessageAsync(messages, tools, cancellationToken))
            {
                yield return chunk;
            }
            yield break;
        }

        // Use Microsoft Foundry SDK streaming
        var inferenceClient = _projectClient.GetInferenceClient();
        var chatMessages = ConvertMessages(messages);
        
        var options = new ChatCompletionsOptions
        {
            DeploymentName = _deploymentName,
            Messages = chatMessages,
            MaxTokens = 4096,
            Temperature = 0.7f
        };

        if (tools != null && tools.Any())
        {
            foreach (var tool in tools)
            {
                options.Tools.Add(ConvertToolDefinition(tool));
            }
        }

        await foreach (var streamingResponse in inferenceClient.GetChatCompletionsStreamingAsync(options, cancellationToken))
        {
            if (cancellationToken.IsCancellationRequested)
                yield break;

            yield return new AIResponseChunk
            {
                ContentDelta = streamingResponse.ContentUpdate,
                ToolCalls = null, // TODO: Handle streaming tool calls
                FinishReason = streamingResponse.FinishReason?.ToString()
            };
        }
    }

    public async Task<List<string>> ListModelsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var models = new List<string>();
            
            // Get deployments from Microsoft Foundry
            var deployments = _projectClient.GetDeploymentsAsync(cancellationToken);
            
            await foreach (var deployment in deployments)
            {
                models.Add(deployment.Model);
            }

            return models;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to list models from Microsoft Foundry");
            return new List<string> { _modelName };
        }
    }

    public Task<ModelCapabilities> GetModelCapabilitiesAsync(
        string modelName,
        CancellationToken cancellationToken = default)
    {
        // TODO: Query Microsoft Foundry for model capabilities
        var capabilities = new ModelCapabilities
        {
            ModelName = modelName,
            Provider = "Microsoft Foundry",
            SupportsToolUse = true,
            SupportsVision = modelName.Contains("gpt-4") && modelName.Contains("vision"),
            SupportsJsonMode = true,
            SupportsStreaming = true,
            MaxContextTokens = GetContextWindowSize(modelName),
            MaxOutputTokens = 4096,
            CostPer1kInputTokens = GetInputCost(modelName),
            CostPer1kOutputTokens = GetOutputCost(modelName)
        };

        return Task.FromResult(capabilities);
    }

    public async Task<bool> ValidateConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Validating Microsoft Foundry connection");
            
            // Try to list deployments as a connection test
            var deployments = _projectClient.GetDeploymentsAsync(cancellationToken);
            await foreach (var _ in deployments.WithCancellation(cancellationToken))
            {
                // Connection successful if we can enumerate
                return true;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate Microsoft Foundry connection");
            return false;
        }
    }

    #region Helper Methods

    private List<ChatMessage> ConvertMessages(List<Message> messages)
    {
        var chatMessages = new List<ChatMessage>();
        
        foreach (var message in messages)
        {
            ChatMessage chatMessage = message.Role switch
            {
                MessageRole.System => new ChatMessage(ChatRole.System, message.Content),
                MessageRole.User => new ChatMessage(ChatRole.User, message.Content),
                MessageRole.Assistant => new ChatMessage(ChatRole.Assistant, message.Content),
                MessageRole.Tool => new ChatMessage(ChatRole.Tool, message.Content)
                {
                    ToolCallId = message.ToolCallId
                },
                _ => new ChatMessage(ChatRole.User, message.Content)
            };

            chatMessages.Add(chatMessage);
        }

        return chatMessages;
    }

    private ChatCompletionsFunctionToolDefinition ConvertToolDefinition(ToolDefinition tool)
    {
        return new ChatCompletionsFunctionToolDefinition
        {
            Name = tool.Name,
            Description = tool.Description,
            Parameters = BinaryData.FromObjectAsJson(tool.Parameters)
        };
    }

    private AIResponse ConvertResponse(ChatCompletions response)
    {
        var choice = response.Choices.FirstOrDefault();
        if (choice == null)
        {
            throw new InvalidOperationException("No response choices returned from Microsoft Foundry");
        }

        var toolCalls = choice.Message.ToolCalls?
            .Select(tc => new Shared.Models.ToolCall
            {
                Id = tc.Id,
                Name = tc.Function.Name,
                Arguments = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(
                    tc.Function.Arguments) ?? new Dictionary<string, object>()
            })
            .ToList();

        return new AIResponse
        {
            Content = choice.Message.Content ?? string.Empty,
            ToolCalls = toolCalls,
            TokenUsage = new TokenUsage
            {
                PromptTokens = response.Usage?.PromptTokens ?? 0,
                CompletionTokens = response.Usage?.CompletionTokens ?? 0
            },
            FinishReason = choice.FinishReason?.ToString() ?? "stop",
            ModelUsed = response.Model ?? _modelName
        };
    }

    private int GetContextWindowSize(string modelName)
    {
        return modelName.ToLowerInvariant() switch
        {
            var m when m.Contains("gpt-4-turbo") => 128000,
            var m when m.Contains("gpt-4o") => 128000,
            var m when m.Contains("gpt-4") => 8192,
            var m when m.Contains("gpt-3.5") => 16385,
            var m when m.Contains("claude-3-5") => 200000,
            var m when m.Contains("claude-3") => 200000,
            _ => 128000 // Default for modern models
        };
    }

    private decimal GetInputCost(string modelName)
    {
        return modelName.ToLowerInvariant() switch
        {
            var m when m.Contains("gpt-4o") => 2.50m,
            var m when m.Contains("gpt-4-turbo") => 10.00m,
            var m when m.Contains("gpt-4") => 30.00m,
            var m when m.Contains("gpt-3.5") => 0.50m,
            var m when m.Contains("claude-3-5-sonnet") => 3.00m,
            var m when m.Contains("claude-3-opus") => 15.00m,
            _ => 3.00m
        };
    }

    private decimal GetOutputCost(string modelName)
    {
        return modelName.ToLowerInvariant() switch
        {
            var m when m.Contains("gpt-4o") => 10.00m,
            var m when m.Contains("gpt-4-turbo") => 30.00m,
            var m when m.Contains("gpt-4") => 60.00m,
            var m when m.Contains("gpt-3.5") => 1.50m,
            var m when m.Contains("claude-3-5-sonnet") => 15.00m,
            var m when m.Contains("claude-3-opus") => 75.00m,
            _ => 15.00m
        };
    }

    #endregion
}
