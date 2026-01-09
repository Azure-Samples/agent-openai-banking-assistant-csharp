namespace BankingAssistant.Agents;

/// <summary>
/// Represents an agent responsible for handling payment-related operations.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="PaymentAgent"/> class.
/// </remarks>
/// <param name="agentFactory">The agent factory for creating ChatClientAgent instances.</param>
/// <param name="configuration">The application configuration.</param>
/// <param name="documentScanner">The document scanner for scanning invoices.</param>
/// <param name="logger">The logger instance for logging operations.</param>
/// <param name="loggerFactory">The logger factory for creating additional loggers.</param>
public class PaymentAgent(AgentFactory agentFactory, IConfiguration configuration, IDocumentScanner documentScanner, ILogger<PaymentAgent> logger, ILoggerFactory loggerFactory) : IPaymentAgent
{
    private ChatClientAgent? _agent;
    private readonly ILogger<PaymentAgent> _logger = logger;
    private readonly IConfiguration _configuration = configuration;
    private readonly IDocumentScanner _documentScanner = documentScanner;
    private readonly AgentFactory _agentFactory = agentFactory;
    private readonly ILoggerFactory _loggerFactory = loggerFactory;

	/// <summary>
	/// Gets the <see cref="ChatClientAgent"/> instance, creating it if it does not already exist.
	/// </summary>
	public ChatClientAgent Agent
    {
        get
        {
            _agent ??= CreateAgentAsync().GetAwaiter().GetResult();
            return _agent;
        }
    }

    /// <summary>
    /// Asynchronously creates a new <see cref="ChatClientAgent"/> instance.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains the created <see cref="ChatClientAgent"/>.</returns>
    private async Task<ChatClientAgent> CreateAgentAsync()
    {
        _logger.LogInformation("Creating PaymentAgent with MCP, OpenAPI, and custom tools");

        // Get MCP tools from Payment API
        var paymentTools = await ToolRegistrationHelper.GetMcpToolsAsync(
            clientName: "banking-assistant-client",
            apiUrl: _configuration["BackendAPIs:PaymentsApiUrl"] + "/mcp",
            useStreamableHttp: true,
            logger: _logger
        );

        // Get MCP tools from Account API
        var accountTools = await ToolRegistrationHelper.GetMcpToolsAsync(
            clientName: "banking-assistant-client",
            apiUrl: _configuration["BackendAPIs:AccountsApiUrl"] + "/mcp",
            useStreamableHttp: true,
            logger: _logger
        );

        // Get OpenAPI tools from Transaction History API
        var transactionTools = await ToolRegistrationHelper.GetOpenApiToolsAsync(
            apiName: "transaction-history",
            apiUrl: _configuration["BackendAPIs:TransactionsApiUrl"] ?? throw new InvalidOperationException("TransactionsApiUrl is not configured"),
            logger: _logger
        );

        // Get custom InvoiceScanTool
        var invoiceScanTool = new InvoiceScanTool(_documentScanner, _loggerFactory.CreateLogger<InvoiceScanTool>());
        var invoiceScanTools = ToolRegistrationHelper.GetCustomPluginTools(invoiceScanTool, _logger);

        // Create agent using factory
        return await _agentFactory.CreatePaymentAgentAsync(
            paymentTools,
            accountTools,
            transactionTools,
            invoiceScanTools
        );
    }
}
