namespace BankingAssistant.Agents;

/// <summary>
/// Manager for Payment Agents.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="PaymentAgentManager"/> class.
/// </remarks>
/// <param name="agentFactory">The agent factory for creating ChatClientAgent instances.</param>
/// <param name="configuration">The application configuration.</param>
/// <param name="documentScanner">The document scanner for scanning invoices.</param>
/// <param name="httpClientFactory">The HTTP client factory for creating HTTP clients.</param>
/// <param name="loggerFactory">The logger factory for creating additional loggers.</param>
public sealed class PaymentAgentManager(AgentFactory agentFactory, IConfiguration configuration, IDocumentScanner documentScanner, IHttpClientFactory httpClientFactory, ILoggerFactory loggerFactory) : IPaymentAgentManager, IAsyncDisposable
{	
	private readonly IConfiguration _configuration = configuration;
	private readonly IDocumentScanner _documentScanner = documentScanner;
	private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
	private readonly AgentFactory _agentFactory = agentFactory;	
	private readonly ILogger<PaymentAgentManager> _logger = loggerFactory.CreateLogger<PaymentAgentManager>();	
	private readonly List<IMcpClient> _mcpClients = [];	

	/// <summary>
	/// Asynchronously creates a new <see cref="AIAgent"/> instance.
	/// </summary>
	/// <returns>A task that represents the asynchronous operation. The task result contains the created <see cref="AIAgent"/>.</returns>
	public async Task<AIAgent> CreateAgentAsync()
	{
		_logger.LogInformation("Creating PaymentAgent with MCP, OpenAPI, and custom tools");

		// Validate configuration
		var paymentsApiUrl = _configuration["BackendAPIs:PaymentsApiUrl"]
			?? throw new InvalidOperationException("BackendAPIs:PaymentsApiUrl configuration is missing or empty.");
		var accountsApiUrl = _configuration["BackendAPIs:AccountsApiUrl"]
			?? throw new InvalidOperationException("BackendAPIs:AccountsApiUrl configuration is missing or empty.");
		var transactionsApiUrl = _configuration["BackendAPIs:TransactionsApiUrl"]
			?? throw new InvalidOperationException("BackendAPIs:TransactionsApiUrl configuration is missing or empty.");

		try
		{
			// Get MCP tools from Payment API
			var paymentClient = await McpClientFactory.CreateAsync(
				new SseClientTransport(
					new SseClientTransportOptions()
					{
						Endpoint = new Uri(paymentsApiUrl + "/mcp"),
						Name = "banking-assistant-client",
						UseStreamableHttp = true
					}));
			_mcpClients.Add(paymentClient);
			var paymentMcpTools = await paymentClient.ListToolsAsync();
			List<AITool> paymentTools = [.. paymentMcpTools.Cast<AITool>()];
			_logger.LogInformation("Payment Agent loaded {PaymentToolCount} tools from Payment API", paymentTools.Count);
			foreach (var tool in paymentTools)
			{
				_logger.LogInformation("Payment Tool - Name: {ToolName}, Description: {Description}", 
					tool.Name, tool.Description);
			}

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
			List<AITool> accountTools = [.. accountMcpTools.Cast<AITool>()];
			_logger.LogInformation("Payment Agent loaded {AccountToolCount} tools from Account API", accountTools.Count);
			foreach (var tool in accountTools)
			{
				_logger.LogInformation("Account Tool - Name: {ToolName}, Description: {Description}", 
					tool.Name, tool.Description);
			}

			// Get tools from Transactions API
			var transactionTool = new TransactionTool(_httpClientFactory, _configuration, loggerFactory.CreateLogger<TransactionTool>());
			var transactionTools = ToolRegistrationHelper.GetCustomTools(transactionTool, _logger);
			_logger.LogInformation("Payment Agent loaded {TransactionToolCount} custom tools from Transactions API", transactionTools.Count);

			// Get custom InvoiceScanTool
			var invoiceScanTool = new InvoiceScanTool(_documentScanner, loggerFactory.CreateLogger<InvoiceScanTool>());
			var invoiceScanTools = ToolRegistrationHelper.GetCustomTools(invoiceScanTool, _logger);
			_logger.LogInformation("Payment Agent loaded {InvoiceScanToolCount} custom invoice scan tools", invoiceScanTools.Count);
			
			// Create agent using factory
			_logger.LogInformation("Payment Agent created successfully with {TotalToolCount} total tools", 
				paymentTools.Count + accountTools.Count + transactionTools.Count + invoiceScanTools.Count);
			return _agentFactory.CreatePaymentAgent(
				paymentTools,
				accountTools,
				transactionTools,
				invoiceScanTools
			);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error creating PaymentAgent");
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
				_logger.LogInformation("Disposing PaymentAgent MCP client");
				await client.DisposeAsync();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error disposing PaymentAgent MCP client");
			}
		}
		_mcpClients.Clear();
	}
}
