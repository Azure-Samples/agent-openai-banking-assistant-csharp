namespace BankingAssistant.Agents.Infrastructure;

/// <summary>
/// Provides initialization and configuration for Azure OpenAI chat clients.
/// </summary>
public static class ChatClientInitialization
{
    /// <summary>
    /// Creates an IChatClient configured for Azure OpenAI.
    /// </summary>
    /// <param name="endpoint">The Azure OpenAI endpoint URL.</param>
    /// <param name="deployment">The deployment name (model deployment ID).</param>
    /// <param name="credential">Optional credential for authentication. Uses DefaultAzureCredential if null.</param>
    /// <returns>An IChatClient instance.</returns>
    public static IChatClient CreateAzureOpenAIChatClient(
        string endpoint,
        string deployment,
        Azure.Core.TokenCredential? credential = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endpoint, nameof(endpoint));
        ArgumentException.ThrowIfNullOrWhiteSpace(deployment, nameof(deployment));

        credential ??= new DefaultAzureCredential();

        var azureOpenAIClient = new AzureOpenAIClient(new Uri(endpoint), credential);
        
        // GetChatClient returns OpenAI.Chat.ChatClient which needs explicit cast to IChatClient
        IChatClient chatClient = (IChatClient)azureOpenAIClient.GetChatClient(deployment);
        
        return chatClient;
    }

    /// <summary>
    /// Creates an IChatClient configured for Azure OpenAI from IConfiguration.
    /// </summary>
    /// <param name="configuration">Configuration containing AzureOpenAI:Endpoint and AzureOpenAI:Deployment.</param>
    /// <param name="credential">Optional credential for authentication.</param>
    /// <returns>An IChatClient instance.</returns>
    public static IChatClient CreateFromConfiguration(
        IConfiguration configuration,
        Azure.Core.TokenCredential? credential = null)
    {
        var endpoint = configuration["AzureOpenAI:Endpoint"]
            ?? throw new InvalidOperationException("AzureOpenAI:Endpoint configuration is missing.");
        
        var deployment = configuration["AzureOpenAI:Deployment"]
            ?? throw new InvalidOperationException("AzureOpenAI:Deployment configuration is missing.");

        return CreateAzureOpenAIChatClient(endpoint, deployment, credential);
    }
}
