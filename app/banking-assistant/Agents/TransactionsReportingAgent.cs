
public class TransactionsReportingAgent
{
    public ChatCompletionAgent agent;
    private ILogger _logger;
    public TransactionsReportingAgent(Kernel kernel, IConfiguration configuration, IUserService userService, ILogger<TransactionsReportingAgent> logger)
    {
        _logger = logger;
        Kernel toolKernel = kernel.Clone();

        var transactionApiURL = configuration["BackendAPIs:TransactionsApiUrl"];
        var accountsApiURL = configuration["BackendAPIs:AccountsApiUrl"];
        // sse is required to enable the mcp client to communicate using server-sent events (http)
        accountsApiURL += "/sse";

        AgenticUtils.AddOpenAPIPlugin(
           kernel: toolKernel,
           pluginName: "TransactionHistoryPlugin",
           apiName: "transaction-history",
           apiUrl: transactionApiURL
        );

        AgenticUtils.AddMcpServerPlugin(
            kernel: toolKernel,
            clientName: "banking-assistant-client",
            pluginName: "AccountPlugins",
            apiUrl: accountsApiURL
        );

        this.agent =
        new()
        {
            Name = "TransactionsReportingAgent",
            Instructions = String.Format(AgentInstructions.TransactionsReportingAgentInstructions, userService.GetLoggedUser()),
            Kernel = toolKernel,
            Arguments =
            new KernelArguments(
                new AzureOpenAIPromptExecutionSettings() { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() }
            )
        };
    }
}

