namespace BankingAssistant.Agents;

/// <summary>
/// Manager for Transactions Reporting Agents.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="TransactionsReportingAgentManager"/> class.
/// </remarks>
/// <param name="agentFactory">The agent factory for creating ChatClientAgent instances.</param>
/// <param name="configuration">The application configuration.</param>
/// <param name="httpClientFactory">The HTTP client factory for creating HTTP clients.</param>
/// <param name="loggerFactory">The logger factory for creating additional loggers.</param>
public sealed class TransactionsReportingAgentManager(AgentFactory agentFactory, IConfiguration configuration, IHttpClientFactory httpClientFactory, ILoggerFactory loggerFactory) : ITransactionsReportingAgentManager, IAsyncDisposable
{
    private readonly ILogger<TransactionsReportingAgentManager> _logger = loggerFactory.CreateLogger<TransactionsReportingAgentManager>();
    private readonly IConfiguration _configuration = configuration;
    private readonly AgentFactory _agentFactory = agentFactory;
    private readonly List<IMcpClient> _mcpClients = [];
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

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

			// Get tools from Transactions API
			var transactionsHistoryTool = new TransactionsHistoryTool(_httpClientFactory, _configuration, loggerFactory.CreateLogger<TransactionsHistoryTool>());
			var transactionTools = ToolRegistrationHelper.GetCustomTools(transactionsHistoryTool, _logger);

			// Create agent using factory
			return _agentFactory.CreateTransactionsAgent(
                accountTools,
                transactionTools
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating TransactionsReportingAgent");
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
                _logger.LogInformation("Disposing TransactionsReportingAgent MCP client");
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

