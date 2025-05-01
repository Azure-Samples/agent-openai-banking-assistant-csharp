
using Microsoft.Extensions.Logging;

public class PaymentAgent
{
    public ChatCompletionAgent agent;
    private ILogger<PaymentAgent> _logger;
    
    public PaymentAgent(Kernel kernel, IConfiguration configuration, IDocumentScanner documentScanner, IUserService userService, ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<PaymentAgent>();

        Kernel toolKernel = kernel.Clone();

        var transactionApiURL = configuration["BackendAPIs:TransactionsApiUrl"];
        var accountsApiURL = configuration["BackendAPIs:AccountsApiUrl"];
        var paymentsApiURL = configuration["BackendAPIs:PaymentsApiUrl"];
        // sse is required to enable the mcp client to communicate using server-sent events (http)
        paymentsApiURL += "/sse";
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
            pluginName: "PaymentsPlugins",
            apiUrl: paymentsApiURL
        );

        AgenticUtils.AddMcpServerPlugin(
            kernel: toolKernel,
            clientName: "banking-assistant-client",
            pluginName: "AccountPlugins",
            apiUrl: accountsApiURL
        );


        toolKernel.Plugins.AddFromObject(new InvoiceScanPlugin(documentScanner, loggerFactory.CreateLogger<InvoiceScanPlugin>()), "InvoiceScanPlugin");

        this.agent =
        new()
        {
            Name = "PaymentAgent",
            Instructions = String.Format(AgentInstructions.PaymentAgentInstructions, userService.GetLoggedUser()),
            Kernel = toolKernel,
            Arguments =
            new KernelArguments(
                new AzureOpenAIPromptExecutionSettings(){ FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() }
            )
        };
    }
}
