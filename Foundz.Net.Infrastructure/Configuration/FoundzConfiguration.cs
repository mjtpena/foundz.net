using System.Text.Json.Serialization;

namespace Foundz.Net.Infrastructure.Configuration;

/// <summary>
/// Root configuration for Foundz.Net application
/// </summary>
public class FoundzConfiguration
{
    /// <summary>
    /// Azure AI configuration
    /// </summary>
    [JsonPropertyName("azure")]
    public AzureConfiguration Azure { get; set; } = new();

    /// <summary>
    /// Model-specific configurations
    /// </summary>
    [JsonPropertyName("models")]
    public Dictionary<string, ModelConfiguration> Models { get; set; } = new();

    /// <summary>
    /// Agent behavior configuration
    /// </summary>
    [JsonPropertyName("agent")]
    public AgentConfiguration Agent { get; set; } = new();

    /// <summary>
    /// Tool configuration
    /// </summary>
    [JsonPropertyName("tools")]
    public ToolConfiguration Tools { get; set; } = new();

    /// <summary>
    /// UI configuration
    /// </summary>
    [JsonPropertyName("ui")]
    public UIConfiguration UI { get; set; } = new();

    /// <summary>
    /// Storage configuration
    /// </summary>
    [JsonPropertyName("storage")]
    public StorageConfiguration Storage { get; set; } = new();

    /// <summary>
    /// Logging configuration
    /// </summary>
    [JsonPropertyName("logging")]
    public LoggingConfiguration Logging { get; set; } = new();

    /// <summary>
    /// Telemetry configuration
    /// </summary>
    [JsonPropertyName("telemetry")]
    public TelemetryConfiguration Telemetry { get; set; } = new();

    /// <summary>
    /// Security configuration
    /// </summary>
    [JsonPropertyName("security")]
    public SecurityConfiguration Security { get; set; } = new();
}

/// <summary>
/// Azure AI Foundry configuration
/// </summary>
public class AzureConfiguration
{
    /// <summary>
    /// Azure AI Foundry endpoint URL
    /// </summary>
    [JsonPropertyName("endpoint")]
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// Authentication configuration
    /// </summary>
    [JsonPropertyName("authentication")]
    public AuthenticationConfiguration Authentication { get; set; } = new();

    /// <summary>
    /// Default model to use
    /// </summary>
    [JsonPropertyName("defaultModel")]
    public string DefaultModel { get; set; } = "gpt-4o";

    /// <summary>
    /// Request timeout in seconds
    /// </summary>
    [JsonPropertyName("timeout")]
    public int Timeout { get; set; } = 120;

    /// <summary>
    /// Maximum retry attempts
    /// </summary>
    [JsonPropertyName("maxRetries")]
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Azure regions for failover
    /// </summary>
    [JsonPropertyName("regions")]
    public List<string> Regions { get; set; } = new() { "primary" };
}

/// <summary>
/// Authentication configuration
/// </summary>
public class AuthenticationConfiguration
{
    /// <summary>
    /// Authentication type
    /// </summary>
    [JsonPropertyName("type")]
    public AuthenticationType Type { get; set; } = AuthenticationType.ApiKey;

    /// <summary>
    /// API key for authentication
    /// </summary>
    [JsonPropertyName("apiKey")]
    public string? ApiKey { get; set; }

    /// <summary>
    /// Azure Entra ID tenant ID
    /// </summary>
    [JsonPropertyName("tenantId")]
    public string? TenantId { get; set; }

    /// <summary>
    /// Azure Entra ID client ID
    /// </summary>
    [JsonPropertyName("clientId")]
    public string? ClientId { get; set; }
}

/// <summary>
/// Authentication type
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AuthenticationType
{
    ApiKey,
    EntraId,
    ManagedIdentity
}

/// <summary>
/// Model-specific configuration
/// </summary>
public class ModelConfiguration
{
    /// <summary>
    /// Temperature parameter
    /// </summary>
    [JsonPropertyName("temperature")]
    public double Temperature { get; set; } = 0.7;

    /// <summary>
    /// Maximum tokens to generate
    /// </summary>
    [JsonPropertyName("maxTokens")]
    public int MaxTokens { get; set; } = 8192;

    /// <summary>
    /// Top P parameter
    /// </summary>
    [JsonPropertyName("topP")]
    public double? TopP { get; set; }

    /// <summary>
    /// Cost per 1K input tokens (USD)
    /// </summary>
    [JsonPropertyName("costPer1kInputTokens")]
    public decimal CostPer1kInputTokens { get; set; } = 0.0m;

    /// <summary>
    /// Cost per 1K output tokens (USD)
    /// </summary>
    [JsonPropertyName("costPer1kOutputTokens")]
    public decimal CostPer1kOutputTokens { get; set; } = 0.0m;

    /// <summary>
    /// Maximum context window
    /// </summary>
    [JsonPropertyName("maxContextTokens")]
    public int MaxContextTokens { get; set; } = 128000;
}

/// <summary>
/// Agent behavior configuration
/// </summary>
public class AgentConfiguration
{
    /// <summary>
    /// Maximum iterations in agentic loop
    /// </summary>
    [JsonPropertyName("maxIterations")]
    public int MaxIterations { get; set; } = 15;

    /// <summary>
    /// Maximum context tokens
    /// </summary>
    [JsonPropertyName("maxContextTokens")]
    public int MaxContextTokens { get; set; } = 150000;

    /// <summary>
    /// System prompt
    /// </summary>
    [JsonPropertyName("systemPrompt")]
    public string SystemPrompt { get; set; } = "You are an AI coding assistant powered by Azure AI Foundry.";

    /// <summary>
    /// Enable planning capabilities
    /// </summary>
    [JsonPropertyName("planningEnabled")]
    public bool PlanningEnabled { get; set; } = true;

    /// <summary>
    /// Enable parallel tool execution
    /// </summary>
    [JsonPropertyName("parallelToolExecution")]
    public bool ParallelToolExecution { get; set; } = true;

    /// <summary>
    /// Context selection strategy
    /// </summary>
    [JsonPropertyName("contextStrategy")]
    public string ContextStrategy { get; set; } = "hybrid";
}

/// <summary>
/// Tool configuration
/// </summary>
public class ToolConfiguration
{
    /// <summary>
    /// Enabled tool categories
    /// </summary>
    [JsonPropertyName("enabled")]
    public List<string> Enabled { get; set; } = new() { "file", "git", "shell", "analysis" };

    /// <summary>
    /// Disabled tool categories
    /// </summary>
    [JsonPropertyName("disabled")]
    public List<string> Disabled { get; set; } = new();

    /// <summary>
    /// Confirmation settings
    /// </summary>
    [JsonPropertyName("confirmations")]
    public ConfirmationConfiguration Confirmations { get; set; } = new();

    /// <summary>
    /// Shell configuration
    /// </summary>
    [JsonPropertyName("shell")]
    public ShellConfiguration Shell { get; set; } = new();
}

/// <summary>
/// Confirmation configuration
/// </summary>
public class ConfirmationConfiguration
{
    [JsonPropertyName("fileWrite")]
    public bool FileWrite { get; set; } = true;

    [JsonPropertyName("fileDelete")]
    public bool FileDelete { get; set; } = true;

    [JsonPropertyName("shellExecution")]
    public bool ShellExecution { get; set; } = true;

    [JsonPropertyName("gitPush")]
    public bool GitPush { get; set; } = true;
}

/// <summary>
/// Shell execution configuration
/// </summary>
public class ShellConfiguration
{
    /// <summary>
    /// Whitelisted commands
    /// </summary>
    [JsonPropertyName("whitelist")]
    public List<string> Whitelist { get; set; } = new() { "dotnet", "npm", "git", "cargo", "python", "node" };

    /// <summary>
    /// Blacklisted commands/patterns
    /// </summary>
    [JsonPropertyName("blacklist")]
    public List<string> Blacklist { get; set; } = new() { "rm -rf", "del /f" };

    /// <summary>
    /// Timeout in seconds
    /// </summary>
    [JsonPropertyName("timeout")]
    public int Timeout { get; set; } = 30;
}

/// <summary>
/// UI configuration
/// </summary>
public class UIConfiguration
{
    [JsonPropertyName("theme")]
    public string Theme { get; set; } = "dark";

    [JsonPropertyName("colorScheme")]
    public string ColorScheme { get; set; } = "default";

    [JsonPropertyName("streaming")]
    public bool Streaming { get; set; } = true;

    [JsonPropertyName("animations")]
    public bool Animations { get; set; } = true;

    [JsonPropertyName("editor")]
    public string Editor { get; set; } = "code";
}

/// <summary>
/// Storage configuration
/// </summary>
public class StorageConfiguration
{
    [JsonPropertyName("databasePath")]
    public string DatabasePath { get; set; } = "~/.foundz/data.db";

    [JsonPropertyName("maxSessionHistory")]
    public int MaxSessionHistory { get; set; } = 100;

    [JsonPropertyName("autoArchiveDays")]
    public int AutoArchiveDays { get; set; } = 30;
}

/// <summary>
/// Logging configuration
/// </summary>
public class LoggingConfiguration
{
    [JsonPropertyName("level")]
    public string Level { get; set; } = "Information";

    [JsonPropertyName("console")]
    public bool Console { get; set; } = true;

    [JsonPropertyName("file")]
    public bool File { get; set; } = true;

    [JsonPropertyName("filePath")]
    public string FilePath { get; set; } = "~/.foundz/logs/";

    [JsonPropertyName("structured")]
    public bool Structured { get; set; } = true;
}

/// <summary>
/// Telemetry configuration
/// </summary>
public class TelemetryConfiguration
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = false;

    [JsonPropertyName("endpoint")]
    public string? Endpoint { get; set; }

    [JsonPropertyName("sampleRate")]
    public double SampleRate { get; set; } = 0.1;
}

/// <summary>
/// Security configuration
/// </summary>
public class SecurityConfiguration
{
    [JsonPropertyName("sandboxing")]
    public bool Sandboxing { get; set; } = true;

    [JsonPropertyName("fileSystemRestrictions")]
    public bool FileSystemRestrictions { get; set; } = true;

    [JsonPropertyName("auditLogging")]
    public bool AuditLogging { get; set; } = true;
}
