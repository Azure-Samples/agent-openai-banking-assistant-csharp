namespace BankingAssistant.Agents.Infrastructure;

using System;
using Azure.AI.OpenAI;
using Azure.Identity;

/// <summary>
/// Provides initialization and configuration for Azure OpenAI chat clients.
/// </summary>
public static class ChatClientInitialization
{
    /// <summary>
    /// Creates a ChatClient configured for Azure OpenAI.
    /// </summary>
    /// <param name="endpoint">The Azure OpenAI endpoint URL.</param>
    /// <param name="deployment">The deployment name (model deployment ID).</param>
    /// <param name="credential">Optional credential for authentication. Uses DefaultAzureCredential if null.</param>
    /// <param name="environment">Optional environment name. Defaults to Development if not specified.</param>
    /// <returns>An IChatClient instance.</returns>
    public static IChatClient CreateAzureOpenAIChatClient(
        string endpoint,
        string deployment,
        Azure.Core.TokenCredential? credential = null,
        string environment = "Development")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endpoint, nameof(endpoint));
        ArgumentException.ThrowIfNullOrWhiteSpace(deployment, nameof(deployment));

        credential ??= new DefaultAzureCredential();

        var chatClient = new AzureOpenAIClient(new Uri(endpoint), new AzureCliCredential())
            .GetChatClient(deployment)
            .AsIChatClient() // Converts a native OpenAI SDK ChatClient into a Microsoft.Extensions.AI.IChatClient
            .AsBuilder()
            .UseOpenTelemetry(sourceName: "BankingAssistant", configure: (cfg) => cfg.EnableSensitiveData = false)
            .Build();
        
        return chatClient;
    }

    /// <summary>
    /// Creates a ChatClient configured for Azure OpenAI from IConfiguration.
    /// </summary>
    /// <param name="configuration">Configuration containing AzureOpenAI:Endpoint and AzureOpenAI:Deployment.</param>
    /// <param name="credential">Optional credential for authentication.</param>
    /// <param name="environment">Optional environment name. Defaults to Development if not specified.</param>
    /// <returns>An IChatClient instance.</returns>
    public static IChatClient CreateFromConfiguration(
        IConfiguration configuration,
        Azure.Core.TokenCredential? credential = null,
        string environment = "Development")
    {
        var endpoint = configuration["AzureOpenAI:Endpoint"]
            ?? throw new InvalidOperationException("AzureOpenAI:Endpoint configuration is missing.");
        
        var deployment = configuration["AzureOpenAI:Deployment"]
            ?? throw new InvalidOperationException("AzureOpenAI:Deployment configuration is missing.");

        return CreateAzureOpenAIChatClient(endpoint, deployment, credential, environment);
    }
}
