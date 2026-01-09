namespace BankingAssistant.Agents.Orchestration;

/// <summary>
/// Service for orchestrating agent handoffs using Microsoft Agent Framework.
/// </summary>
public class AgentOrchestrationService(
	AgentFactory agentFactory,
	IConfiguration configuration,
	IDocumentScanner documentScanner,
	ILogger<AgentOrchestrationService> logger,
	ILoggerFactory loggerFactory)
{
    private readonly AgentFactory _agentFactory = agentFactory;
    private readonly IConfiguration _configuration = configuration;
    private readonly ILogger<AgentOrchestrationService> _logger = logger;
    private readonly ILoggerFactory _loggerFactory = loggerFactory;
    private readonly IDocumentScanner _documentScanner = documentScanner;

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

        // Create workflow
        var workflow = await BuildWorkflowAsync();

        // Execute workflow
        var run = await InProcessExecution.StreamAsync(workflow, messages);
        await run.TrySendMessageAsync(new TurnToken(emitEvents: true));

        List<ChatMessage> newMessages = [];
        await foreach (WorkflowEvent evt in run.WatchStreamAsync().ConfigureAwait(false))
        {
            if (evt is AgentRunUpdateEvent updateEvent)
            {
                _logger.LogInformation($"{updateEvent.ExecutorId}: {updateEvent.Data}");
            }
            else if (evt is WorkflowOutputEvent outputEvent)
            {
                newMessages = (List<ChatMessage>)outputEvent.Data!;
                break;
            }
        }

        _logger.LogInformation("Agent orchestration workflow completed");

        // Return the new messages along with the context
        return (newMessages.Skip(messages.Count).ToList(), context);
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

        // Create workflow
        var workflow = await BuildWorkflowAsync();

        // Execute workflow with streaming
        var run = await InProcessExecution.StreamAsync(workflow, messages);
        await run.TrySendMessageAsync(new TurnToken(emitEvents: true));

        await foreach (WorkflowEvent evt in run.WatchStreamAsync().ConfigureAwait(false))
        {
            if (evt is AgentRunUpdateEvent updateEvent)
            {
                // Stream agent responses as they come
                _logger.LogInformation($"{updateEvent.ExecutorId}: {updateEvent.Data}");
                yield return updateEvent.Data?.ToString() ?? string.Empty;
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
    /// Builds the handoff workflow with all agents and handoff rules.
    /// Creates triage, account, payment, and transactions agents with their respective tools,
    /// and configures bidirectional handoffs between triage and specialist agents.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains the configured workflow.</returns>
    private async Task<Workflow> BuildWorkflowAsync()
    {
        // Create triage agent (entry point)
        var triageAgent = _agentFactory.CreateTriageAgent();

        // Create account agent with MCP tools
        var accountTools = await ToolRegistrationHelper.GetMcpToolsAsync(
            clientName: "banking-assistant-client",
            apiUrl: _configuration["BackendAPIs:AccountsApiUrl"] + "/mcp",
            useStreamableHttp: true,
            logger: _logger
        );
        var accountAgent = await _agentFactory.CreateAccountAgentAsync(accountTools);

        // Create payment agent with all tools
        var paymentTools = await ToolRegistrationHelper.GetMcpToolsAsync(
            clientName: "banking-assistant-client",
            apiUrl: _configuration["BackendAPIs:PaymentsApiUrl"] + "/mcp",
            useStreamableHttp: true,
            logger: _logger
        );
        var transactionTools = await ToolRegistrationHelper.GetOpenApiToolsAsync(
            apiName: "transaction-history",
            apiUrl: _configuration["BackendAPIs:TransactionsApiUrl"] ?? throw new InvalidOperationException("TransactionsApiUrl is not configured"),
            logger: _logger
        );
        var invoiceScanTool = new InvoiceScanTool(_documentScanner, _loggerFactory.CreateLogger<InvoiceScanTool>());
        var invoiceScanTools = ToolRegistrationHelper.GetCustomPluginTools(invoiceScanTool, _logger);
        var paymentAgent = await _agentFactory.CreatePaymentAgentAsync(
            paymentTools,
            accountTools,
            transactionTools,
            invoiceScanTools
        );

        // Create transactions agent
        var transactionsAgent = await _agentFactory.CreateTransactionsAgentAsync(
            accountTools,
            transactionTools
        );

        // Build handoff workflow with routing rules
        var workflow = AgentWorkflowBuilder.CreateHandoffBuilderWith(triageAgent)
            .WithHandoffs(triageAgent, new[] { accountAgent, paymentAgent, transactionsAgent })
            .WithHandoff(accountAgent, triageAgent)
            .WithHandoff(paymentAgent, triageAgent)
            .WithHandoff(transactionsAgent, triageAgent)
            .Build();

        return workflow;
    }
}