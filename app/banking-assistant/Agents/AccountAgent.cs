
/// <summary>
/// Represents an agent responsible for managing account-related operations.
/// </summary>
public class AccountAgent : IAccountAgent
{
    private ChatCompletionAgent? _agent; // Marked as nullable
    private ILogger<AccountAgent> _logger;
    private readonly IUserService _userService;
    private IOptions<BackendApisOption> _backendApisOptions;
    private readonly Kernel _kernel;
    private readonly string _pluginName = "AccountPlugins";

    /// <summary>
    /// Initializes a new instance of the <see cref="AccountAgent"/> class.
    /// </summary>
    /// <param name="kernel">The kernel instance for managing plugins and functions.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="userService">The user service for retrieving logged-in user information.</param>
    /// <param name="logger">The logger instance for logging operations.</param>
    public AccountAgent(Kernel kernel, IOptions<BackendApisOption> backendApisOptions, IUserService userService, ILogger<AccountAgent> logger)
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
        // Add mcp plugins
        var tools = await AgenticUtils.AddMcpServerPluginAsync(
            clientName: "banking-assistant-client",
            pluginName: _pluginName,
            apiUrl: _backendApisOptions.Value.AccountsApiUrl + "/mcp",
            useStreamableHttp: true
        );

        _kernel.Plugins.AddFromFunctions(_pluginName, tools.Select(mcpTools => mcpTools.AsKernelFunction()));

        return new ChatCompletionAgent
        {
            Name = nameof(AccountAgent),
            Instructions = String.Format(AgentInstructions.AccountAgentInstructions, _userService.GetLoggedUser()),
            Kernel = _kernel,
            Arguments =
            new KernelArguments(
                new AzureOpenAIPromptExecutionSettings() { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() }
            )
        };
    }
}

