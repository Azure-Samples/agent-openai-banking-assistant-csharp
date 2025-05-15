
/// <summary>
/// Represents an agent responsible for handling payment-related operations.
/// </summary>
public class PaymentAgent : IPaymentAgent
{
    private ChatCompletionAgent? _agent; // Marked as nullable
    private ILogger<PaymentAgent> _logger;
    private readonly IUserService _userService;
    private IOptions<BackendApisOption> _backendApisOptions;
    private readonly Kernel _kernel;

    /// <summary>
    /// Initializes a new instance of the <see cref="PaymentAgent"/> class.
    /// </summary>
    /// <param name="kernel">The kernel instance for managing plugins and functions.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="documentScanner">The document scanner for scanning invoices.</param>
    /// <param name="userService">The user service for retrieving logged-in user information.</param>
    /// <param name="logger">The logger instance for logging operations.</param>
    public PaymentAgent(Kernel kernel, IOptions<BackendApisOption> backendApisOptions, IDocumentScanner documentScanner, IUserService userService, ILogger<PaymentAgent> logger)
    {
        _logger = logger;
        _userService = userService;
        _backendApisOptions = backendApisOptions;
        _kernel = kernel.Clone();
    }

    /// <summary>
    /// Gets the <see cref="ChatCompletionAgent"/> instance, creating it if it does not already exist.
    /// </summary>
    public ChatCompletionAgent Agent
    {
        get
        {
            if (_agent == null)
            {
                _agent = CreateAgentAsync().GetAwaiter().GetResult();
            }
            return _agent;
        }
    }

    /// <summary>
    /// Asynchronously creates a new <see cref="ChatCompletionAgent"/> instance.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains the created <see cref="ChatCompletionAgent"/>.</returns>
    private async Task<ChatCompletionAgent> CreateAgentAsync()
    {

        var paymentTools = await AgenticUtils.AddMcpServerPluginAsync(
            clientName: "banking-assistant-client",
            pluginName: "PaymentsPlugins",
            apiUrl: _backendApisOptions.Value.PaymentsApiUrl + "/mcp",
            useStreamableHttp: true
        );

        var accountTools = await AgenticUtils.AddMcpServerPluginAsync(
            clientName: "banking-assistant-client",
            pluginName: "AccountPlugins",
            apiUrl: _backendApisOptions.Value.AccountsApiUrl + "/mcp",
            useStreamableHttp: true
        );

        _kernel.Plugins.AddFromFunctions("PaymentsPlugins", paymentTools.Select(mcpTools => mcpTools.AsKernelFunction()));
        _kernel.Plugins.AddFromFunctions("AccountPlugins", accountTools.Select(mcpTools => mcpTools.AsKernelFunction()));

        AgenticUtils.AddOpenAPIPlugin(
           kernel: _kernel,
           pluginName: "TransactionHistoryPlugin",
           apiName: "transaction-history",
           apiUrl: _backendApisOptions.Value.TransactionsApiUrl
        );

        _kernel.ImportPluginFromType<InvoiceScanPlugin>(nameof(InvoiceScanPlugin));

        // Special call out of RetainArugumentTypes. The Payments plug takes object as input instead of string.
        var executionSettigs = new AzureOpenAIPromptExecutionSettings()
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(options: new() { RetainArgumentTypes = true })
        };

        return new()
        {
            Name = "PaymentAgent",
            Instructions = String.Format(AgentInstructions.PaymentAgentInstructions, _userService.GetLoggedUser()),
            Kernel = _kernel,
            Arguments =
            new KernelArguments(
                executionSettings: executionSettigs
            )
        };
    }
}
