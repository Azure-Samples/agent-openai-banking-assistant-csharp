namespace BankingAssistant.Agents;

/// <summary>
/// Represents an agent responsible for managing account-related operations.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="AccountAgent"/> class.
/// </remarks>
/// <param name="agentFactory">The agent factory for creating ChatClientAgent instances.</param>
/// <param name="configuration">The application configuration.</param>
/// <param name="logger">The logger instance for logging operations.</param>
public class AccountAgent(AgentFactory agentFactory, IConfiguration configuration, ILogger<AccountAgent> logger) : IAccountAgent
{
    private ChatClientAgent? _agent;
    private readonly ILogger<AccountAgent> _logger = logger;
    private readonly IConfiguration _configuration = configuration;
    private readonly AgentFactory _agentFactory = agentFactory;

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
        _logger.LogInformation("Creating AccountAgent with MCP tools");

        // Get MCP tools from Account API
        var accountTools = await ToolRegistrationHelper.GetMcpToolsAsync(
            clientName: "banking-assistant-client",
            apiUrl: _configuration["BackendAPIs:AccountsApiUrl"] + "/mcp",
            useStreamableHttp: true,
            logger: _logger
        );

        // Create agent using factory
        return await _agentFactory.CreateAccountAgentAsync(accountTools);
    }
}

