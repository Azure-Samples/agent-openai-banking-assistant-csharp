
using Microsoft.Extensions.Logging;

public class AgentRouter: IAgentRouter
{
    private ILogger<AgentRouter> _logger;
    private IIntentExtractorAgent _intentExtractorAgent;
    private Kernel _kernel;

    private ChatCompletionAgent _paymentAgent, _accountAgent, _transactionsReportingAgent;
    public AgentRouter(Kernel kernel, ILogger<AgentRouter> logger, IIntentExtractorAgent intentExtractorAgent, IPaymentAgent paymentAgent, IAccountAgent accountAgent, ITransactionsReportingAgent transactionsReportingAgent)
    {
        _kernel = kernel;
        _intentExtractorAgent = intentExtractorAgent;
        _accountAgent = ((AccountAgent)accountAgent).Agent;
        _paymentAgent = ((PaymentAgent)paymentAgent).Agent;
        _transactionsReportingAgent = ((TransactionsReportingAgent)transactionsReportingAgent).Agent;

        _logger = logger;
    }

#pragma warning disable SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    public async Task Run(ChatHistory chatHistory, AgentContext agentContext)
    {
        _logger.LogInformation("======== Router Agent: Starting ========");
        _logger.LogInformation(chatHistory[chatHistory.Count - 1].Content);
        IntentResponse intentResponse = await _intentExtractorAgent.Run(chatHistory);
        _logger.LogInformation($"Intent Type for chat conversation is {intentResponse.intentType.ToString()}");

        KernelFunction selectionFunction =
        AgentGroupChat.CreatePromptFunctionForStrategy(
            $$$"""
            Determine which participant takes the next turn in a conversation based on the the intent classified.
            State only the name of the participant to take the next turn.

            Choose only from these participants:
            - 'PaymentAgent' for: BillPayment, RepeatTransaction
            - 'AccountAgent' for: AccountInfo
            - 'TransactionsReportingAgent' for: TransactionHistory

            Intent Classified:
            {{{intentResponse.intentType.ToString()}}}

            History:
            {{$history}}
            """,
            safeParameterNames: "history");

        KernelFunction terminationFunction =
            AgentGroupChat.CreatePromptFunctionForStrategy(
                $$$"""
                    Examine the RESPONSE and determine whether the content has been deemed satisfactory.
                    Only if you did what was asked of you and you received a confirmation you are done.
                    If no correction is suggested, it is satisfactory.

                    RESPONSE:
                    {{$history}}
                    """,
                safeParameterNames: "history");

        // Define the selection strategy
        KernelFunctionSelectionStrategy selectionStrategy =
          new(selectionFunction, _kernel)
          {   
              // Parse the function response.
              ResultParser = (result) => result.GetValue<string>() ?? "",
              HistoryVariableName = "history",
              // Save tokens by not including the entire history in the prompt
              HistoryReducer = new ChatHistoryTruncationReducer(5),
          };
        // Create a chat using the defined selection strategy.
        KernelFunctionTerminationStrategy terminationStrategy =
            new(terminationFunction, _kernel)
            {
                // Save tokens by only including the final response
                HistoryReducer = new ChatHistoryTruncationReducer(3),
                // The prompt variable name for the history argument.
                HistoryVariableName = "history",
                // Limit total number of turns
                MaximumIterations = 1,
                // Customer result parser to determine if the response is "yes"
                // ResultParser = (result) => result.GetValue<string>()?.Contains("success", StringComparison.OrdinalIgnoreCase) ?? false
            };

        AgentGroupChat chat =
            new( _accountAgent, _paymentAgent, _transactionsReportingAgent)
            {
                ExecutionSettings = new() { SelectionStrategy = selectionStrategy, TerminationStrategy = terminationStrategy }
            };

        chat.AddChatMessages(chatHistory);

        chat.IsComplete = false;

        try
        {
            await foreach (ChatMessageContent response in chat.InvokeAsync())
            {

                _logger.LogInformation($"{response.AuthorName.ToUpperInvariant()}:{Environment.NewLine}{response.Content}");
                chatHistory.AddAssistantMessage(response.Content);
             
            }
        }
        catch (HttpOperationException exception)
        {
            _logger.LogError(exception.Message);
            if (exception.InnerException != null)
            {
                _logger.LogError(exception.InnerException.Message);
                if (exception.InnerException.Data.Count > 0)
                {
                    _logger.LogError(JsonSerializer.Serialize(exception.InnerException.Data, new JsonSerializerOptions() { WriteIndented = true }));
                }
            }
        }
    }
}