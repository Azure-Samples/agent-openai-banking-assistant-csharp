namespace BankingAssistant.Agents;

/// <summary>
/// Manager for Account Agents.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="AccountAgent"/> class.
/// </remarks>
/// <param name="agentFactory">The agent factory for creating ChatClientAgent instances.</param>
/// <param name="configuration">The application configuration.</param>
/// <param name="logger">The logger instance for logging operations.</param>
public sealed class AccountAgentManager(AgentFactory agentFactory, IConfiguration configuration, ILoggerFactory loggerFactory) : IAccountAgentManager, IAsyncDisposable
{
    private readonly ILogger<AccountAgentManager> _logger = loggerFactory.CreateLogger<AccountAgentManager>();
    private readonly IConfiguration _configuration = configuration;
    private readonly AgentFactory _agentFactory = agentFactory;
    private readonly List<IMcpClient> _mcpClients = [];

	/// <summary>
	/// Asynchronously creates a new <see cref="AIAgent"/> instance.
	/// Manages MCP client creation and tool loading. The MCP client is kept alive
	/// until the manager is disposed to ensure the agent can use tools throughout its lifetime.
	/// </summary>
	/// <returns>A task that represents the asynchronous operation. The task result contains the created <see cref="AIAgent"/>.</returns>
	public async Task<AIAgent> CreateAgentAsync()
    {
        _logger.LogInformation("Creating Account Agent with MCP tools");
        
        // Validate configuration
        var accountsApiUrl = _configuration["BackendAPIs:AccountsApiUrl"] 
            ?? throw new InvalidOperationException("BackendAPIs:AccountsApiUrl configuration is missing or empty.");
        
        var mcpUrl = $"{accountsApiUrl}/mcp";
        _logger.LogInformation("MCP Server URL: {McpUrl}", mcpUrl);

        try
        {
            // Create and keep MCP client alive for the lifetime of the agent
            var mcpClient = await McpClientFactory.CreateAsync(
                new SseClientTransport(
                    new SseClientTransportOptions()
                    {
                        Endpoint = new Uri(mcpUrl),
                        Name = "banking-assistant-client",
                        UseStreamableHttp = true
                    }));
            _mcpClients.Add(mcpClient);

            // Get MCP tools from Account API
            var mcpTools = await mcpClient.ListToolsAsync();
            List<AITool> accountTools = [..mcpTools.Cast<AITool>()];

            _logger.LogInformation("Account Agent Builder loaded {ToolCount} tools", accountTools.Count);

            foreach (var tool in accountTools)
            {
                _logger.LogInformation("- Tool: {ToolName}, Description: {ToolDescription}", tool.Name, tool.Description);
            }

            if (accountTools.Count == 0)
            {
                _logger.LogWarning("WARNING: No tools loaded for Account Agent. The agent will not be able to call any functions.");
            }

            // Create agent using factory
            var agent = _agentFactory.CreateAccountAgent(accountTools);
            _logger.LogInformation("Account Agent created successfully with {ToolCount} tools", accountTools.Count);
            return agent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating Account Agent: {ErrorMessage}. Verify that the Account MCP server is running at {McpUrl}", ex.Message, mcpUrl);
            throw;
        }
    }

    /// <summary>
    /// Disposes all MCP client connections when the agent is no longer needed.
    /// </summary>
    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        foreach (var client in _mcpClients)
        {
            try
            {
                _logger.LogInformation("Disposing Account Agent MCP client");
                await client.DisposeAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing Account Agent MCP client");
            }
        }
        _mcpClients.Clear();
    }
}
