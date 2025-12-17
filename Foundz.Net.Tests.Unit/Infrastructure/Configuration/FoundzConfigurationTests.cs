using Foundz.Net.Infrastructure.Configuration;
using System.Text.Json;

namespace Foundz.Net.Tests.Unit.Infrastructure.Configuration;

public class FoundzConfigurationTests
{
    [Fact]
    public void FoundzConfiguration_DefaultValues_AreSetCorrectly()
    {
        // Arrange & Act
        var config = new FoundzConfiguration();

        // Assert
        Assert.NotNull(config.Azure);
        Assert.NotNull(config.Models);
        Assert.NotNull(config.Agent);
        Assert.NotNull(config.Tools);
        Assert.NotNull(config.UI);
        Assert.NotNull(config.Storage);
        Assert.NotNull(config.Logging);
        Assert.NotNull(config.Telemetry);
        Assert.NotNull(config.Security);
    }

    [Fact]
    public void AzureConfiguration_DefaultValues_AreSetCorrectly()
    {
        // Arrange & Act
        var config = new AzureConfiguration();

        // Assert
        Assert.Equal(string.Empty, config.Endpoint);
        Assert.NotNull(config.Authentication);
        Assert.Equal("gpt-4o", config.DefaultModel);
        Assert.Equal(120, config.Timeout);
        Assert.Equal(3, config.MaxRetries);
        Assert.Single(config.Regions);
        Assert.Equal("primary", config.Regions[0]);
    }

    [Fact]
    public void AuthenticationConfiguration_DefaultValues_AreSetCorrectly()
    {
        // Arrange & Act
        var config = new AuthenticationConfiguration();

        // Assert
        Assert.Equal(AuthenticationType.ApiKey, config.Type);
        Assert.Null(config.ApiKey);
        Assert.Null(config.TenantId);
        Assert.Null(config.ClientId);
    }

    [Fact]
    public void AuthenticationType_AllValues_AreValid()
    {
        // Assert
        Assert.Equal(0, (int)AuthenticationType.ApiKey);
        Assert.Equal(1, (int)AuthenticationType.EntraId);
        Assert.Equal(2, (int)AuthenticationType.ManagedIdentity);
    }

    [Fact]
    public void ModelConfiguration_DefaultValues_AreSetCorrectly()
    {
        // Arrange & Act
        var config = new ModelConfiguration();

        // Assert
        Assert.Equal(0.7, config.Temperature);
        Assert.Equal(8192, config.MaxTokens);
        Assert.Null(config.TopP);
        Assert.Equal(0.0m, config.CostPer1kInputTokens);
        Assert.Equal(0.0m, config.CostPer1kOutputTokens);
        Assert.Equal(128000, config.MaxContextTokens);
    }

    [Fact]
    public void AgentConfiguration_DefaultValues_AreSetCorrectly()
    {
        // Arrange & Act
        var config = new AgentConfiguration();

        // Assert
        Assert.Equal(15, config.MaxIterations);
        Assert.Equal(150000, config.MaxContextTokens);
        Assert.Equal("You are an AI coding assistant powered by Azure AI Foundry.", config.SystemPrompt);
        Assert.True(config.PlanningEnabled);
        Assert.True(config.ParallelToolExecution);
        Assert.Equal("hybrid", config.ContextStrategy);
    }

    [Fact]
    public void ToolConfiguration_DefaultValues_AreSetCorrectly()
    {
        // Arrange & Act
        var config = new ToolConfiguration();

        // Assert
        Assert.NotNull(config.Enabled);
        Assert.Equal(4, config.Enabled.Count);
        Assert.Contains("file", config.Enabled);
        Assert.Contains("git", config.Enabled);
        Assert.Contains("shell", config.Enabled);
        Assert.Contains("analysis", config.Enabled);
        Assert.NotNull(config.Disabled);
        Assert.Empty(config.Disabled);
        Assert.NotNull(config.Confirmations);
        Assert.NotNull(config.Shell);
    }

    [Fact]
    public void ConfirmationConfiguration_DefaultValues_AreSetCorrectly()
    {
        // Arrange & Act
        var config = new ConfirmationConfiguration();

        // Assert
        Assert.True(config.FileWrite);
        Assert.True(config.FileDelete);
        Assert.True(config.ShellExecution);
        Assert.True(config.GitPush);
    }

    [Fact]
    public void ShellConfiguration_DefaultValues_AreSetCorrectly()
    {
        // Arrange & Act
        var config = new ShellConfiguration();

        // Assert
        Assert.NotNull(config.Whitelist);
        Assert.Equal(6, config.Whitelist.Count);
        Assert.Contains("dotnet", config.Whitelist);
        Assert.Contains("npm", config.Whitelist);
        Assert.Contains("git", config.Whitelist);
        Assert.NotNull(config.Blacklist);
        Assert.Equal(2, config.Blacklist.Count);
        Assert.Contains("rm -rf", config.Blacklist);
        Assert.Contains("del /f", config.Blacklist);
        Assert.Equal(30, config.Timeout);
    }

    [Fact]
    public void UIConfiguration_DefaultValues_AreSetCorrectly()
    {
        // Arrange & Act
        var config = new UIConfiguration();

        // Assert
        Assert.Equal("dark", config.Theme);
        Assert.Equal("default", config.ColorScheme);
        Assert.True(config.Streaming);
        Assert.True(config.Animations);
        Assert.Equal("code", config.Editor);
    }

    [Fact]
    public void StorageConfiguration_DefaultValues_AreSetCorrectly()
    {
        // Arrange & Act
        var config = new StorageConfiguration();

        // Assert
        Assert.Equal("~/.foundz/data.db", config.DatabasePath);
        Assert.Equal(100, config.MaxSessionHistory);
        Assert.Equal(30, config.AutoArchiveDays);
    }

    [Fact]
    public void LoggingConfiguration_DefaultValues_AreSetCorrectly()
    {
        // Arrange & Act
        var config = new LoggingConfiguration();

        // Assert
        Assert.Equal("Information", config.Level);
        Assert.True(config.Console);
        Assert.True(config.File);
        Assert.Equal("~/.foundz/logs/", config.FilePath);
        Assert.True(config.Structured);
    }

    [Fact]
    public void TelemetryConfiguration_DefaultValues_AreSetCorrectly()
    {
        // Arrange & Act
        var config = new TelemetryConfiguration();

        // Assert
        Assert.False(config.Enabled);
        Assert.Null(config.Endpoint);
        Assert.Equal(0.1, config.SampleRate);
    }

    [Fact]
    public void SecurityConfiguration_DefaultValues_AreSetCorrectly()
    {
        // Arrange & Act
        var config = new SecurityConfiguration();

        // Assert
        Assert.True(config.Sandboxing);
        Assert.True(config.FileSystemRestrictions);
        Assert.True(config.AuditLogging);
    }

    [Fact]
    public void FoundzConfiguration_Serialization_WorksCorrectly()
    {
        // Arrange
        var config = new FoundzConfiguration
        {
            Azure = new AzureConfiguration
            {
                Endpoint = "https://test.endpoint.com",
                DefaultModel = "gpt-4",
                Timeout = 60
            },
            Agent = new AgentConfiguration
            {
                MaxIterations = 10,
                PlanningEnabled = false
            }
        };

        // Act
        var json = JsonSerializer.Serialize(config);
        var deserialized = JsonSerializer.Deserialize<FoundzConfiguration>(json);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal("https://test.endpoint.com", deserialized.Azure.Endpoint);
        Assert.Equal("gpt-4", deserialized.Azure.DefaultModel);
        Assert.Equal(60, deserialized.Azure.Timeout);
        Assert.Equal(10, deserialized.Agent.MaxIterations);
        Assert.False(deserialized.Agent.PlanningEnabled);
    }

    [Fact]
    public void AuthenticationType_Serialization_WorksCorrectly()
    {
        // Arrange
        var config = new AuthenticationConfiguration
        {
            Type = AuthenticationType.EntraId,
            TenantId = "test-tenant",
            ClientId = "test-client"
        };

        // Act
        var json = JsonSerializer.Serialize(config);
        var deserialized = JsonSerializer.Deserialize<AuthenticationConfiguration>(json);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(AuthenticationType.EntraId, deserialized.Type);
        Assert.Equal("test-tenant", deserialized.TenantId);
        Assert.Equal("test-client", deserialized.ClientId);
    }

    [Fact]
    public void ModelConfiguration_CustomValues_WorkCorrectly()
    {
        // Arrange & Act
        var config = new ModelConfiguration
        {
            Temperature = 0.5,
            MaxTokens = 4096,
            TopP = 0.9,
            CostPer1kInputTokens = 0.03m,
            CostPer1kOutputTokens = 0.06m,
            MaxContextTokens = 64000
        };

        // Assert
        Assert.Equal(0.5, config.Temperature);
        Assert.Equal(4096, config.MaxTokens);
        Assert.Equal(0.9, config.TopP);
        Assert.Equal(0.03m, config.CostPer1kInputTokens);
        Assert.Equal(0.06m, config.CostPer1kOutputTokens);
        Assert.Equal(64000, config.MaxContextTokens);
    }

    [Fact]
    public void AzureConfiguration_MultipleRegions_WorkCorrectly()
    {
        // Arrange & Act
        var config = new AzureConfiguration
        {
            Regions = new List<string> { "eastus", "westus", "northeurope" }
        };

        // Assert
        Assert.Equal(3, config.Regions.Count);
        Assert.Contains("eastus", config.Regions);
        Assert.Contains("westus", config.Regions);
        Assert.Contains("northeurope", config.Regions);
    }

    [Fact]
    public void ToolConfiguration_CustomEnabled_WorksCorrectly()
    {
        // Arrange & Act
        var config = new ToolConfiguration
        {
            Enabled = new List<string> { "file", "git" },
            Disabled = new List<string> { "shell" }
        };

        // Assert
        Assert.Equal(2, config.Enabled.Count);
        Assert.Contains("file", config.Enabled);
        Assert.Contains("git", config.Enabled);
        Assert.Single(config.Disabled);
        Assert.Contains("shell", config.Disabled);
    }

    [Fact]
    public void ShellConfiguration_CustomWhitelistBlacklist_WorksCorrectly()
    {
        // Arrange & Act
        var config = new ShellConfiguration
        {
            Whitelist = new List<string> { "custom-tool" },
            Blacklist = new List<string> { "dangerous-command" },
            Timeout = 60
        };

        // Assert
        Assert.Single(config.Whitelist);
        Assert.Contains("custom-tool", config.Whitelist);
        Assert.Single(config.Blacklist);
        Assert.Contains("dangerous-command", config.Blacklist);
        Assert.Equal(60, config.Timeout);
    }
}
