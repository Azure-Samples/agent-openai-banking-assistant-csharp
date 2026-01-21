using BankingAssistant.Exceptions;
using BankingAssistant.Validators;
using FluentValidation;

namespace BankingAssistant.Controllers;

/// <summary>
/// Controller for handling chat-related requests.
/// </summary>
[Route("api/chat")]
[ApiController]
public class ChatController : ControllerBase
{
    private readonly ILogger<ChatController> _logger;
    private readonly AgentOrchestrationService _orchestrationService;
    private readonly IValidator<ChatAppRequest> _chatRequestValidator;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatController"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="orchestrationService">The agent orchestration service.</param>
    /// <param name="chatRequestValidator">The chat request validator.</param>
    public ChatController(
        ILogger<ChatController> logger, 
        AgentOrchestrationService orchestrationService,
        IValidator<ChatAppRequest> chatRequestValidator)
    {
        _logger = logger;
        _orchestrationService = orchestrationService;
        _chatRequestValidator = chatRequestValidator;
    }

    /// <summary>
    /// Health check endpoint for the chat controller.
    /// </summary>
    /// <returns>A status message indicating the controller is available.</returns>
    [HttpGet]
    public async Task<IActionResult> IndexAsync()
    {
        return await Task.FromResult(Ok("Chat Controller is available."));
    }

    /// <summary>
    /// Processes a chat request using the agent orchestration service.
    /// </summary>
    /// <param name="chatRequest">The chat request containing conversation history and context.</param>
    /// <returns>A JSON response containing the agent's reply and context.</returns>
    /// <response code="200">Returns the chat response with agent messages.</response>
    /// <response code="400">If the request is invalid or missing required data.</response>
    [HttpPost]
    [Produces("application/json")]
    public async Task<IActionResult> ChatWithOpenAIAsync([FromBody] ChatAppRequest chatRequest)
    {
        // Validate the request using FluentValidation
        var validationResult = await _chatRequestValidator.ValidateAsync(chatRequest);
        if (!validationResult.IsValid)
        {
            throw new BankingAssistant.Exceptions.ValidationException(validationResult.Errors);
        }

        // Convert request messages to Microsoft.Extensions.AI ChatMessage list
        var chatMessages = ConvertToChatMessages(chatRequest);

        _logger.LogDebug("Processing chat conversation with {MessageCount} messages", chatMessages.Count);

        // Create context dictionary for additional information
        var context = new Dictionary<string, object>
        {
            ["requestContext"] = chatRequest.Context ?? new ChatAppRequestContext(),
            ["attachments"] = chatRequest.Attachments ?? new List<string>(),
            ["approach"] = chatRequest.Approach ?? "rrr"
        };

        // Run orchestration
        var result = await _orchestrationService.RunAsync(chatMessages, context);

        // Build response
        Models.ChatResponse response = Models.ChatResponse.BuildChatResponse(result.Messages, result.Context);
        return new JsonResult(response);
    }

    /// <summary>
    /// Processes requests through the Account Agent independently without triage routing.
    /// </summary>
    /// <param name="chatRequest">The chat request containing the message to process.</param>
    /// <returns>A JSON response containing the Account Agent's reply.</returns>
    /// <response code="200">Returns the agent response.</response>
    /// <response code="400">If the request is invalid.</response>
    /// <response code="500">If an error occurs during agent execution.</response>
    [HttpPost("account")]
    [Produces("application/json")]
    public async Task<IActionResult> AccountAgentAsync([FromBody] ChatAppRequest chatRequest)
    {
        var validationResult = await _chatRequestValidator.ValidateAsync(chatRequest);
        if (!validationResult.IsValid)
        {
            throw new BankingAssistant.Exceptions.ValidationException(validationResult.Errors);
        }

        var chatMessages = ConvertToChatMessages(chatRequest);
        var result = await _orchestrationService.AccountAgentAsync(chatMessages);
        var response = Models.ChatResponse.BuildChatResponse(result.Messages, result.Context);
        return new JsonResult(response);
    }

    /// <summary>
    /// Processes requests through the Payment Agent independently without triage routing.
    /// </summary>
    /// <param name="chatRequest">The chat request containing the message to process.</param>
    /// <returns>A JSON response containing the Payment Agent's reply.</returns>
    /// <response code="200">Returns the agent response.</response>
    /// <response code="400">If the request is invalid.</response>
    /// <response code="500">If an error occurs during agent execution.</response>
    [HttpPost("payment")]
    [Produces("application/json")]
    public async Task<IActionResult> PaymentAgentAsync([FromBody] ChatAppRequest chatRequest)
    {
        var validationResult = await _chatRequestValidator.ValidateAsync(chatRequest);
        if (!validationResult.IsValid)
        {
            throw new BankingAssistant.Exceptions.ValidationException(validationResult.Errors);
        }

        var chatMessages = ConvertToChatMessages(chatRequest);
        var result = await _orchestrationService.PaymentAgentAsync(chatMessages);
        var response = Models.ChatResponse.BuildChatResponse(result.Messages, result.Context);
        return new JsonResult(response);
    }

    /// <summary>
    /// Processes requests through the Transactions Agent independently without triage routing.
    /// </summary>
    /// <param name="chatRequest">The chat request containing the message to process.</param>
    /// <returns>A JSON response containing the Transactions Agent's reply.</returns>
    /// <response code="200">Returns the agent response.</response>
    /// <response code="400">If the request is invalid.</response>
    /// <response code="500">If an error occurs during agent execution.</response>
    [HttpPost("transactions")]
    [Produces("application/json")]
    public async Task<IActionResult> TransactionsAgentAsync([FromBody] ChatAppRequest chatRequest)
    {
        var validationResult = await _chatRequestValidator.ValidateAsync(chatRequest);
        if (!validationResult.IsValid)
        {
            throw new BankingAssistant.Exceptions.ValidationException(validationResult.Errors);
        }

        var chatMessages = ConvertToChatMessages(chatRequest);
        var result = await _orchestrationService.TransactionsAgentAsync(chatMessages);
        var response = Models.ChatResponse.BuildChatResponse(result.Messages, result.Context);
        return new JsonResult(response);
    }

    /// <summary>
    /// Processes requests through the Triage Agent for routing determination.
    /// </summary>
    /// <param name="chatRequest">The chat request containing the message to process.</param>
    /// <returns>A JSON response containing the Triage Agent's reply.</returns>
    /// <response code="200">Returns the agent response.</response>
    /// <response code="400">If the request is invalid.</response>
    /// <response code="500">If an error occurs during agent execution.</response>
    [HttpPost("triage")]
    [Produces("application/json")]
    public async Task<IActionResult> TriageAgentAsync([FromBody] ChatAppRequest chatRequest)
    {
        var validationResult = await _chatRequestValidator.ValidateAsync(chatRequest);
        if (!validationResult.IsValid)
        {
            throw new BankingAssistant.Exceptions.ValidationException(validationResult.Errors);
        }

        var chatMessages = ConvertToChatMessages(chatRequest);
        var result = await _orchestrationService.TriageAgentAsync(chatMessages);
        var response = Models.ChatResponse.BuildChatResponse(result.Messages, result.Context);
        return new JsonResult(response);
    }

    /// <summary>
    /// Converts request messages to Microsoft.Extensions.AI ChatMessage format.
    /// </summary>
    /// <param name="chatAppRequest">The chat application request.</param>
    /// <returns>A list of ChatMessage objects.</returns>
    private List<ChatMessage> ConvertToChatMessages(ChatAppRequest chatAppRequest)
    {
        var chatMessages = new List<ChatMessage>();
        
        foreach (var historyChat in chatAppRequest.Messages)
        {
            if ("user".Equals(historyChat.Role, StringComparison.OrdinalIgnoreCase))
            {
                string content = historyChat.Content;
                if (historyChat.Attachments != null && historyChat.Attachments.Any())
                {
                    string attachmentsString = string.Join(", ", historyChat.Attachments);
                    content = $"{historyChat.Content} {attachmentsString}";
                }
                chatMessages.Add(new ChatMessage(ChatRole.User, content));
            }
            else if ("assistant".Equals(historyChat.Role, StringComparison.OrdinalIgnoreCase))
            {
                chatMessages.Add(new ChatMessage(ChatRole.Assistant, historyChat.Content));
            }
        }

        return chatMessages;
    }
}


