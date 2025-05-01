
public class AccountAgent
{
    public ChatCompletionAgent agent;
    private readonly ILogger<AccountAgent> _logger;


    public AccountAgent(Kernel kernel, IConfiguration configuration, IUserService userService, ILogger<AccountAgent> logger)
    {
        _logger = logger;
        Kernel toolKernel = kernel.Clone();

        var accountsApiURL = configuration["BackendAPIs:AccountsApiUrl"];

        // sse is required to enable the mcp client to communicate using server-sent events (http)
        accountsApiURL += "/sse";

        // Add mcp plugins
        AgenticUtils.AddMcpServerPlugin(
            kernel: toolKernel,
            clientName: "banking-assistant-client",
            pluginName: "AccountPlugins",
            apiUrl: accountsApiURL
        );

        agent =
        new()
        {
            Name = "AccountAgent",
            Instructions = String.Format(AgentInstructions.AccountAgentInstructions, userService.GetLoggedUser()),
            Kernel = toolKernel,
            Arguments =
            new KernelArguments(
                new AzureOpenAIPromptExecutionSettings() { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() }
            )
        };
    }
}
