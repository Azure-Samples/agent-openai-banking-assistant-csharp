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

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatController"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="orchestrationService">The agent orchestration service.</param>
    public ChatController(
        ILogger<ChatController> logger, 
        AgentOrchestrationService orchestrationService)
    {
        _logger = logger;
        _orchestrationService = orchestrationService;
    }

    /// <summary>
    /// Health check endpoint for the chat controller.
    /// </summary>
    /// <returns>A status message indicating the controller is available.</returns>
    [HttpGet]
    public async Task<IActionResult> Index()
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
    public async Task<IActionResult> ChatWithOpenAI([FromBody] ChatAppRequest chatRequest)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (chatRequest.Stream)
        {
            _logger.LogWarning(
                "Requested a content-type of application/json however also requested streaming. " +
                "Please use a content-type of application/ndjson");
            return BadRequest(
                "Requested a content-type of application/json however also requested streaming. " +
                "Please use a content-type of application/ndjson");
        }

        if (chatRequest.Messages == null || !chatRequest.Messages.Any())
        {
            _logger.LogWarning("history cannot be null in Chat request");
            return BadRequest();
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


