/// <summary>
/// Represents an agent responsible for handling transaction history and reporting-related operations.
/// </summary>
public class TransactionsReportingAgent : ITransactionsReportingAgent
{
    public ChatCompletionAgent? _agent;
    private ILogger _logger;
    private readonly IUserService _userService;
    private IOptions<BackendApisOption> _backendApisOptions;
    private readonly Kernel _kernel;

    /// <summary>
    /// Initializes a new instance of the <see cref="TransactionsReportingAgent"/> class.
    /// </summary>
    /// <param name="kernel">The kernel instance for managing plugins and functions.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="userService">The user service for retrieving logged-in user information.</param>
    /// <param name="logger">The logger instance for logging operations.</param>
    public TransactionsReportingAgent(Kernel kernel, IOptions<BackendApisOption> backendApisOptions, IUserService userService, ILogger<TransactionsReportingAgent> logger)
    {
        _userService = userService;
        _backendApisOptions = backendApisOptions;
        _kernel = kernel.Clone();
        _logger = logger;
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

        var accountTools = await AgenticUtils.AddMcpServerPluginAsync(
            clientName: "banking-assistant-client",
            pluginName: "AccountPlugins",
            apiUrl: _backendApisOptions.Value.AccountsApiUrl + "/mcp",
            useStreamableHttp: true
        );

        _kernel.Plugins.AddFromFunctions("AccountPlugins", accountTools.Select(mcpTools => mcpTools.AsKernelFunction()));

        AgenticUtils.AddOpenAPIPlugin(
           kernel: _kernel,
           pluginName: "TransactionHistoryPlugin",
           apiName: "transaction-history",
           apiUrl: _backendApisOptions.Value.TransactionsApiUrl
        );

        return new()
        {
            Name = "TransactionsReportingAgent",
            Instructions = String.Format(AgentInstructions.TransactionsReportingAgentInstructions, _userService.GetLoggedUser()),
            Kernel = _kernel,
            Arguments =
            new KernelArguments(
                new AzureOpenAIPromptExecutionSettings() { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() }
            )
        };
    }
}

