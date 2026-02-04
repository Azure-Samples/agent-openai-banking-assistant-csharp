namespace BankingAssistant.Agents;

/// <summary>
/// Manager for Transaction Agents.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="TransactionAgentManager"/> class.
/// </remarks>
/// <param name="agentFactory">The agent factory for creating ChatClientAgent instances.</param>
/// <param name="configuration">The application configuration.</param>
/// <param name="httpClientFactory">The HTTP client factory for creating HTTP clients.</param>
/// <param name="loggerFactory">The logger factory for creating additional loggers.</param>
public sealed class TransactionAgentManager(AgentFactory agentFactory, IConfiguration configuration, IHttpClientFactory httpClientFactory, ILoggerFactory loggerFactory) : ITransactionAgentManager, IAsyncDisposable
{
    private readonly AgentFactory _agentFactory = agentFactory;
    private readonly IConfiguration _configuration = configuration;
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly ILoggerFactory _loggerFactory = loggerFactory;
    private readonly ILogger<TransactionAgentManager> _logger = loggerFactory.CreateLogger<TransactionAgentManager>();    
    private readonly List<IMcpClient> _mcpClients = [];    

	/// <summary>
	/// Asynchronously creates a new <see cref="AIAgent"/> instance.
	/// </summary>
	/// <returns>A task that represents the asynchronous operation. The task result contains the created <see cref="AIAgent"/>.</returns>
	public async Task<AIAgent> CreateAgentAsync()
    {
        _logger.LogInformation("Creating TransactionsReportingAgent with MCP and OpenAPI tools");

        // Validate configuration
        var accountsApiUrl = _configuration["BackendAPIs:AccountsApiUrl"]
            ?? throw new InvalidOperationException("BackendAPIs:AccountsApiUrl configuration is missing or empty.");
		var transactionsApiUrl = _configuration["BackendAPIs:TransactionsApiUrl"]
			?? throw new InvalidOperationException("BackendAPIs:TransactionsApiUrl configuration is missing or empty.");

        try
        {
            // Get MCP tools from Account API
            var accountClient = await McpClientFactory.CreateAsync(
                new SseClientTransport(
                    new SseClientTransportOptions()
                    {
                        Endpoint = new Uri(accountsApiUrl + "/mcp"),
                        Name = "banking-assistant-client",
                        UseStreamableHttp = true
                    }));
            _mcpClients.Add(accountClient);

            var accountMcpTools = await accountClient.ListToolsAsync();
            List<AITool> accountTools = [..accountMcpTools.Cast<AITool>()];
            _logger.LogInformation("Transaction Agent loaded {AccountToolCount} tools from Account API", accountTools.Count);
            foreach (var tool in accountTools)
            {
                _logger.LogInformation("Account Tool - Name: {ToolName}, Description: {Description}", 
                    tool.Name, tool.Description);
            }

            // Get tools from Transactions API
            var transactionTool = new TransactionTool(_httpClientFactory, _configuration, _loggerFactory.CreateLogger<TransactionTool>());
            var transactionTools = ToolRegistrationHelper.GetCustomTools(transactionTool, _logger);
            _logger.LogInformation("Transaction Agent loaded {TransactionToolCount} custom tools from Transactions API", transactionTools.Count);

            // Create agent using factory
            _logger.LogInformation("Transaction Agent created successfully with {TotalToolCount} total tools", 
                accountTools.Count + transactionTools.Count);
            return _agentFactory.CreateTransactionsAgent(
                accountTools,
                transactionTools
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating TransactionAgent");
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
                _logger.LogInformation("Disposing TransactionAgent MCP client");
                await client.DisposeAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing MCP client");
            }
        }
        _mcpClients.Clear();
    }
}

