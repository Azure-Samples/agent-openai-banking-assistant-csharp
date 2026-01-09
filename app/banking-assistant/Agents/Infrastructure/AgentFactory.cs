namespace BankingAssistant.Agents.Infrastructure;

/// <summary>
/// Factory for creating ChatClientAgent instances with proper configuration.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="AgentFactory"/> class.
/// </remarks>
/// <param name="chatClient">The chat client for agent communication.</param>
/// <param name="userService">The user service for retrieving logged user information.</param>
/// <param name="logger">The logger instance.</param>
public class AgentFactory(
	IChatClient chatClient,
	IUserService userService,
	ILogger<AgentFactory> logger)
{
    private readonly IChatClient _chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));
    private readonly IUserService _userService = userService ?? throw new ArgumentNullException(nameof(userService));
    private readonly ILogger<AgentFactory> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

	/// <summary>
	/// Creates a ChatClientAgent with the specified configuration.
	/// </summary>
	/// <param name="name\">The name of the agent.</param>
	/// <param name="instructions">The system instructions for the agent.</param>
	/// <param name="tools">Optional list of tools (AIFunctions) available to the agent.</param>
	/// <returns>A configured ChatClientAgent instance.</returns>
	public ChatClientAgent CreateAgent(
        string name,
        string instructions,
        IList<AIFunction>? tools = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        ArgumentException.ThrowIfNullOrWhiteSpace(instructions, nameof(instructions));

        _logger.LogInformation("Creating agent {AgentName} with {ToolCount} tools", name, tools?.Count ?? 0);

        // Convert AIFunction list to AITool list (AIFunction implements AITool)
        IList<AITool>? aiTools = tools?.Cast<AITool>().ToList();

        var agent = new ChatClientAgent(
            chatClient: _chatClient,
            instructions: instructions,
            name: name,
            tools: aiTools);

        return agent;
    }

    /// <summary>
    /// Creates the Triage Agent that routes user requests to specialist agents.
    /// </summary>
    /// <returns>A configured triage agent.</returns>
    public ChatClientAgent CreateTriageAgent()
    {
        var instructions = AgentInstructions.TriageAgentInstructions;
        
        return CreateAgent(
            name: "TriageAgent",    
            instructions: instructions,
            tools: null);
    }

    public async Task<ChatClientAgent> CreateAccountAgentAsync(IList<AIFunction> accountTools)
    {
        var loggedUser = _userService.GetLoggedUser();
        var instructions = string.Format(AgentInstructions.AccountAgentInstructions, loggedUser);
        
        return CreateAgent(
            name: "AccountAgent",
            instructions: instructions,
            tools: accountTools);
    }

    /// <summary>
    /// Creates the Payment Agent for handling payment and bill payment operations.
    /// </summary>
    /// <param name="paymentTools">The list of payment-related tools.</param>
    /// <param name="accountTools">The list of account-related tools.</param>
    /// <param name="transactionTools">The list of transaction-related tools.</param>
    /// <param name="invoiceScanTools">The list of invoice scanning tools.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the configured payment agent.</returns>
    public async Task<ChatClientAgent> CreatePaymentAgentAsync(
        IList<AIFunction> paymentTools,
        IList<AIFunction> accountTools,
        IList<AIFunction> transactionTools,
        IList<AIFunction> invoiceScanTools)
    {
        var loggedUser = _userService.GetLoggedUser();
        var instructions = string.Format(AgentInstructions.PaymentAgentInstructions, loggedUser);
        
        var allTools = new List<AIFunction>();
        allTools.AddRange(paymentTools);
        allTools.AddRange(accountTools);
        allTools.AddRange(transactionTools);
        allTools.AddRange(invoiceScanTools);
        
        return CreateAgent(
            name: "PaymentAgent",
            instructions: instructions,
            tools: allTools);
    }

    /// <summary>
    /// Creates the Transactions Agent for handling transaction history queries.
    /// </summary>
    /// <param name="accountTools">The list of account-related tools.</param>
    /// <param name="transactionTools">The list of transaction-related tools.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the configured transactions agent.</returns>
    public async Task<ChatClientAgent> CreateTransactionsAgentAsync(
        IList<AIFunction> accountTools,
        IList<AIFunction> transactionTools)
    {
        var loggedUser = _userService.GetLoggedUser();
        var instructions = string.Format(AgentInstructions.TransactionsReportingAgentInstructions, loggedUser);
        
        var allTools = new List<AIFunction>();
        allTools.AddRange(accountTools);
        allTools.AddRange(transactionTools);
        
        return CreateAgent(
            name: "TransactionsAgent",
            instructions: instructions,
            tools: allTools);
    }
}