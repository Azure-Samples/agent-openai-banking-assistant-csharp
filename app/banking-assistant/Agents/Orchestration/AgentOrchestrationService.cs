namespace BankingAssistant.Agents.Orchestration;

/// <summary>
/// Service for orchestrating agent handoffs using Microsoft Agent Framework.
/// </summary>
public class AgentOrchestrationService(
	AgentFactory agentFactory,
	ILogger<AgentOrchestrationService> logger,
	IAccountAgentManager accountAgentManager,
	IPaymentAgentManager paymentAgentManager,
	ITransactionAgentManager transactionAgentManager)
{
    private readonly AgentFactory _agentFactory = agentFactory;
    private readonly ILogger<AgentOrchestrationService> _logger = logger;
    private readonly IAccountAgentManager _accountAgentManager = accountAgentManager;
    private readonly IPaymentAgentManager _paymentAgentManager = paymentAgentManager;
    private readonly ITransactionAgentManager _transactionAgentManager = transactionAgentManager;

	/// <summary>
	/// Executes the agent workflow with handoff orchestration.
	/// </summary>
	/// <param name="messages">The conversation history.</param>
	/// <param name="context">Additional context (attachments, approach, etc.).</param>
	/// <returns>The result containing final messages and updated context.</returns>
	public async Task<(List<ChatMessage> Messages, Dictionary<string, object> Context)> RunAsync(
        List<ChatMessage> messages,
        Dictionary<string, object> context)
    {
        _logger.LogInformation("Starting agent orchestration workflow");
        var userMessage = messages.Last()?.ToString() ?? "Unknown";
        _logger.LogInformation("User message: {Message}", userMessage);
        
        var workflow = await BuildWorkflowAsync(_accountAgentManager, _paymentAgentManager, _transactionAgentManager);

        // Execute workflow
        var run = await InProcessExecution.StreamAsync(workflow, messages);
        await run.TrySendMessageAsync(new TurnToken(emitEvents: true));

        List<ChatMessage> newMessages = [];
        await foreach (WorkflowEvent evt in run.WatchStreamAsync().ConfigureAwait(false))
        {
            if (evt is AgentRunUpdateEvent runUpdateEvent)
            {
                _logger.LogInformation("{ExecutorId}: {Data}", runUpdateEvent.ExecutorId, runUpdateEvent.Data);
            }
            else if (evt is WorkflowOutputEvent outputEvent)
            {
                newMessages = (List<ChatMessage>)outputEvent.Data!;
                _logger.LogInformation("Workflow completed with {MessageCount} total messages (input had {InputCount})", newMessages.Count, messages.Count);
                break;
            }
        }

        _logger.LogInformation("Agent orchestration workflow completed");

        // Add new messages to conversation history (multi-turn accumulation)
        messages.AddRange(newMessages.Skip(messages.Count));

        // Return updated messages with full conversation history
        return (messages, context);
    }

    /// <summary>
    /// Executes the agent workflow with streaming support.
    /// </summary>
    /// <param name="messages">The conversation history.</param>
    /// <param name="context">Additional context (attachments, approach, etc.).</param>
    /// <returns>An async enumerable of string chunks representing the streaming response.</returns>
    public async IAsyncEnumerable<string> RunStreamingAsync(
        List<ChatMessage> messages,
        Dictionary<string, object> context)
    {
        _logger.LogInformation("Starting agent orchestration workflow (streaming)");
        
        var workflow = await BuildWorkflowAsync(_accountAgentManager, _paymentAgentManager, _transactionAgentManager);

        // Execute workflow
        var run = await InProcessExecution.StreamAsync(workflow, messages);
        await run.TrySendMessageAsync(new TurnToken(emitEvents: true));

        await foreach (WorkflowEvent evt in run.WatchStreamAsync().ConfigureAwait(false))
        {
            if (evt is AgentRunUpdateEvent runUpdateEvent)
            {
                // Stream agent responses as they come
                _logger.LogInformation($"{runUpdateEvent.ExecutorId}: {runUpdateEvent.Data}");
                yield return runUpdateEvent.Data?.ToString() ?? string.Empty;
            }
            else if (evt is WorkflowOutputEvent outputEvent)
            {
                // Final output
                _logger.LogInformation("Workflow output received");
                var outputMessages = (List<ChatMessage>)outputEvent.Data!;

                if (outputMessages.Count != 0)
                {
                    var lastMessage = outputMessages.Last();

                    if (lastMessage.Text != null)
                    {
                        yield return lastMessage.Text;
                    }
                }

                break;
            }
        }

        _logger.LogInformation("Agent orchestration workflow completed (streaming)");
    }

    /// <summary>
    /// Tests the Account Agent independently without triage routing.
    /// Useful for debugging agent-specific issues or MCP tool connectivity.
    /// </summary>
    /// <param name="messages">The conversation history to test with.</param>
    /// <returns>The result containing agent response messages and context.</returns>
    public async Task<(List<ChatMessage> Messages, Dictionary<string, object> Context)> AccountAgentAsync(
        List<ChatMessage> messages)
    {
        _logger.LogInformation("Testing Account Agent independently");
        var userMessage = messages.Last()?.ToString() ?? "Unknown";
        _logger.LogInformation("User message: {Message}", userMessage);

        try
        {
            _logger.LogInformation("Creating Account Agent with MCP tools...");
            var accountAgent = await _accountAgentManager.CreateAgentAsync();
            _logger.LogInformation("Account Agent created successfully");

            _logger.LogInformation("Invoking agent...");
            var response = await accountAgent.RunAsync(messages);
            
            _logger.LogInformation("Agent response received");
            
            var responseMessages = new List<ChatMessage> { response.Messages.Last() };
            return (responseMessages, new Dictionary<string, object>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing Account Agent: {ErrorMessage}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Tests the Payment Agent independently without triage routing.
    /// Useful for debugging agent-specific issues or MCP tool connectivity.
    /// </summary>
    /// <param name="messages">The conversation history to test with.</param>
    /// <returns>The result containing agent response messages and context.</returns>
    public async Task<(List<ChatMessage> Messages, Dictionary<string, object> Context)> PaymentAgentAsync(
        List<ChatMessage> messages)
    {
        _logger.LogInformation("Testing Payment Agent independently");
        var userMessage = messages.Last()?.ToString() ?? "Unknown";
        _logger.LogInformation("User message: {Message}", userMessage);

        try
        {
            _logger.LogInformation("Creating Payment Agent with MCP tools...");
            var paymentAgent = await _paymentAgentManager.CreateAgentAsync();
            _logger.LogInformation("Payment Agent created successfully");

            _logger.LogInformation("Invoking agent...");
            var response = await paymentAgent.RunAsync(messages);
            
            _logger.LogInformation("Agent response received");
            
            var responseMessages = new List<ChatMessage> { response.Messages.Last() };
            return (responseMessages, new Dictionary<string, object>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing Payment Agent: {ErrorMessage}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Tests the Transactions Agent independently without triage routing.
    /// Useful for debugging agent-specific issues or MCP tool connectivity.
    /// </summary>
    /// <param name="messages">The conversation history to test with.</param>
    /// <returns>The result containing agent response messages and context.</returns>
    public async Task<(List<ChatMessage> Messages, Dictionary<string, object> Context)> TransactionsAgentAsync(
        List<ChatMessage> messages)
    {
        _logger.LogInformation("Testing Transactions Agent independently");
        var userMessage = messages.Last()?.ToString() ?? "Unknown";
        _logger.LogInformation("User message: {Message}", userMessage);

        try
        {
            _logger.LogInformation("Creating Transaction Agent with MCP tools...");            
            var transactionAgent = await _transactionAgentManager.CreateAgentAsync();
            _logger.LogInformation("Transaction Agent created successfully");

            _logger.LogInformation("Invoking agent...");
            var response = await transactionAgent.RunAsync(messages);
            
            _logger.LogInformation("Agent response received");
            
            var responseMessages = new List<ChatMessage> { response.Messages.Last() };
            return (responseMessages, new Dictionary<string, object>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing Transactions Agent: {ErrorMessage}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Tests the Triage Agent independently to verify routing logic.
    /// </summary>
    /// <param name="messages">The conversation history to test with.</param>
    /// <returns>The result containing agent response messages and context.</returns>
    public async Task<(List<ChatMessage> Messages, Dictionary<string, object> Context)> TriageAgentAsync(
        List<ChatMessage> messages)
    {
        _logger.LogInformation("Testing Triage Agent independently");
        var userMessage = messages.Last()?.ToString() ?? "Unknown";
        _logger.LogInformation("User message: {Message}", userMessage);

        try
        {
            _logger.LogInformation("Creating Triage Agent...");
            var triageAgent = _agentFactory.CreateTriageAgent();
            _logger.LogInformation("Triage Agent created successfully");

            _logger.LogInformation("Invoking agent...");
            var response = await triageAgent.RunAsync(messages);
            
            _logger.LogInformation("Agent response received");
            
            var responseMessages = new List<ChatMessage> { response.Messages.Last() };
            return (responseMessages, new Dictionary<string, object>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing Triage Agent: {ErrorMessage}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Builds the handoff workflow with all agents and handoff rules.
    /// Creates triage, account, payment, and transactions agents with their respective tools,
    /// and configures bidirectional handoffs between triage and specialist agents.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains the configured workflow.</returns>
    private async Task<Workflow> BuildWorkflowAsync(
        IAccountAgentManager accountAgentManager,
        IPaymentAgentManager paymentAgentManager,
        ITransactionAgentManager transactionAgentManager)
    {
        _logger.LogInformation("Building agent workflow...");
        
        // Create triage agent (entry point)
        var triageAgent = _agentFactory.CreateTriageAgent();
        _logger.LogInformation("Triage Agent created");

        // Get individual agents asynchronously (they manage their own tool loading)
        _logger.LogInformation("Loading Account Agent...");
        var accountAgent = await accountAgentManager.CreateAgentAsync();
		_logger.LogInformation("Account Agent loaded");
        
        _logger.LogInformation("Loading Payment Agent...");
        var paymentAgent = await paymentAgentManager.CreateAgentAsync();
        _logger.LogInformation("Payment Agent loaded");
        
        _logger.LogInformation("Loading Transaction Agent...");
        var transactionAgent = await transactionAgentManager.CreateAgentAsync();
        _logger.LogInformation("Transaction Agent loaded");

        // Build handoff workflow with routing rules
        _logger.LogInformation("Configuring handoff workflow...");
        var workflow = AgentWorkflowBuilder.CreateHandoffBuilderWith(triageAgent)
			.WithHandoffs(triageAgent, [accountAgent, paymentAgent, transactionAgent])
            .WithHandoff(accountAgent, triageAgent)
            .WithHandoff(paymentAgent, triageAgent)
            .WithHandoff(transactionAgent, triageAgent)
            .Build();

        _logger.LogInformation("Workflow built successfully");
        _logger.LogInformation("Workflow agents: Triage + Account + Payment + Transactions");
        
        return workflow;
    }
}