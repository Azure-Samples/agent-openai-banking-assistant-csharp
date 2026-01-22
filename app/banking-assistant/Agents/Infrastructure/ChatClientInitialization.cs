namespace BankingAssistant.Agents.Infrastructure;

using System;
using Azure.AI.OpenAI;
using Azure.AI.Agents.Persistent;
using Azure.Identity;

/// <summary>
/// Provides initialization and configuration for chat clients.
/// </summary>
public static class ChatClientInitialization
{
	/// <summary>
	/// Creates a PersistentAgentsClient for Azure AI Agents runtime.
	/// </summary>
	/// <param name="endpoint">The Azure AI Agents endpoint URL.</param>
    /// <param name="deployment">The deployment name (model deployment ID).</param>
	/// <param name="agentId">The agent identifier.</param>
	/// <param name="credential">Optional credential for authentication. Uses DefaultAzureCredential if null.</param>
	/// <returns>A ChatClientAgent instance.</returns>
	public static IChatClient CreatePersistentAgentsClient(
		string endpoint,
		string deployment,
        string agentId,
		Azure.Core.TokenCredential? credential = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endpoint, nameof(endpoint));
        ArgumentException.ThrowIfNullOrWhiteSpace(deployment, nameof(deployment));
		ArgumentException.ThrowIfNullOrWhiteSpace(agentId, nameof(agentId));

		credential ??= new DefaultAzureCredential();
        var agentClient = new PersistentAgentsClient(endpoint, credential).AsIChatClient(agentId);       
        return agentClient;
    }
}
