
using Microsoft.Extensions.Logging;

public class AgentRouter
{
    private ILogger<AgentRouter> _logger;
    private IntentExtractorAgent _intentExtractorAgent;
    private PaymentAgent _paymentAgent;
    private AccountAgent _accountAgent;
    private TransactionsReportingAgent _transactionsReportingAgent;
    private Kernel _kernel;
    public AgentRouter(Kernel kernel, IConfiguration configuration, IDocumentScanner documentScanner, ILoggerFactory loggerFactory, IUserService userService)
    {
        _kernel = kernel;
        _intentExtractorAgent = new IntentExtractorAgent(kernel, configuration, loggerFactory.CreateLogger<IntentExtractorAgent>());
        _accountAgent = new AccountAgent(kernel, configuration, userService, loggerFactory.CreateLogger<AccountAgent>());
        _transactionsReportingAgent = new TransactionsReportingAgent(kernel, configuration, userService, loggerFactory.CreateLogger<TransactionsReportingAgent>());
        _paymentAgent = new PaymentAgent(kernel, configuration, documentScanner, userService, loggerFactory);
        _logger = loggerFactory.CreateLogger<AgentRouter>();
    }
#pragma warning disable SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    public async Task Run(ChatHistory chatHistory, AgentContext agentContext)
    {
        _logger.LogInformation("======== Router Agent: Starting ========");
        _logger.LogInformation(chatHistory[chatHistory.Count - 1].Content);
        IntentResponse intentResponse = _intentExtractorAgent.Run(chatHistory).Result;
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
            new(_paymentAgent.agent, _transactionsReportingAgent.agent, _accountAgent.agent)
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